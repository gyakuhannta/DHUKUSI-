using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アルファ版：mineSeen のゲーム全体の流れ（仕様書「ゲーム概要」「コスト仕様」「攻守決めフェーズ」）
///
///  1. 攻守決め：疾風・剛腕・鉄壁から 1つ選ぶ（L スティック＋A）。相手（CPU）はランダム
///     勝てば先攻（攻撃から）、負ければ後攻（防御から）。あいこはやり直し
///  2. 戦闘：技選択 → 攻撃タイミング → 敵の攻撃 → 防御タイミング（Gamemanager の流れをそのまま使う）
///  3. 1セット＝ライフ 2。ライフが 0 になったら相手に 1セット
///     セットが変わったら、ライフとコストを元に戻す。次の攻撃はセットを取られた側から
///  4. 2セット先取で勝ち → 勝敗判定へ
///
/// 【置き方】メニュー「DHUKUSI → mineSeen をアルファ版にする」で自動で置かれる
/// </summary>
public class AlphaGameFlow : MonoBehaviour
{
    [Header("画面")]
    [SerializeField] private PhaseUIManager ui;
    [SerializeField] private KoushuSelectUI koushuSelect;
    [SerializeField] private Text battle1PText;
    [SerializeField] private Text battle2PText;
    [SerializeField] private Text judgeText;
    [SerializeField] private Text waitText;

    [Header("HUD（ライフ・セット・コスト）")]
    [SerializeField] private Image[] life1P = new Image[2];
    [SerializeField] private Image[] life2P = new Image[2];
    [SerializeField] private Image[] set1P = new Image[2];
    [SerializeField] private Image[] set2P = new Image[2];
    [SerializeField] private Text costText;

    [Header("戦闘（Gamemanager / Player）")]
    [SerializeField] private PlayerLifeController player;
    [SerializeField] private EnemyController enemy;
    [SerializeField] private EnemyAttackController enemyAttack;

    [Header("攻守決めの間は隠すもの（Gamemanager の Canvas）")]
    [SerializeField] private GameObject battleCanvas;
    [Header("戦闘中も隠すもの（HUD とかぶるライフの文字）")]
    [SerializeField] private GameObject[] hideInBattle;
    [Header("戦闘の文字の位置を整える（技・結果の文字 / 防御の行動の文字）")]
    [SerializeField] private RectTransform messageText;
    [SerializeField] private RectTransform actionText;

    [Header("攻守決め  [0]=疾風 [1]=剛腕 [2]=鉄壁（コストは仕様書が空欄なので仮）")]
    [SerializeField] private string[] koushuNames = { "疾風", "剛腕", "鉄壁" };
    [SerializeField] private int[] koushuCosts = { 3, 5, 4 };

    [Header("ルール")]
    [SerializeField] private int maxLife = 2;
    [SerializeField] private int setsToWin = 2;
    [SerializeField] private float resultSeconds = 2f;

    [Header("色")]
    [SerializeField] private Color onColor = new Color32(0xf8, 0xb5, 0x00, 0xff);
    [SerializeField] private Color offColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);

    private FieldInfo playerLifeField, enemyLifeField;
    private MethodInfo playerUpdateText, enemyUpdateText;
    private FieldInfo enemyResultTextField;

    private int sets1P, sets2P;
    private bool inBattle;
    private bool setEnding;

    void Start()
    {
        const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        playerLifeField = typeof(PlayerLifeController).GetField("playerLife", f);
        enemyLifeField = typeof(EnemyController).GetField("enemyLife", f);
        playerUpdateText = typeof(PlayerLifeController).GetMethod("UpdateLifeText", f);
        enemyUpdateText = typeof(EnemyController).GetMethod("UpdateLifeText", f);
        enemyResultTextField = typeof(EnemyController).GetField("resultText", f);

        if (player == null) player = FindFirstObjectByType<PlayerLifeController>();
        if (enemy == null) enemy = FindFirstObjectByType<EnemyController>();
        if (enemyAttack == null) enemyAttack = FindFirstObjectByType<EnemyAttackController>();

        if (koushuSelect != null) koushuSelect.OnDecided += OnKoushuDecided;
        if (CostManager.Instance != null) CostManager.Instance.OnCostChanged += (p, c, m) => UpdateHud();

        StartCoroutine(BeginKoushu(0.1f));
    }

    // ===================== 1. 攻守決め =====================
    private IEnumerator BeginKoushu(float delay)
    {
        inBattle = false;
        yield return new WaitForSeconds(delay);
        if (DhukusiInput.Instance != null) DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.None;
        if (battleCanvas != null) battleCanvas.SetActive(false);
        ui.ShowPhase(PhaseUIManager.UIPhase.KoushuSelect);
        UpdateHud();
    }

    private void OnKoushuDecided(int p, int mine)
    {
        StartCoroutine(KoushuBattle(mine));
    }

    private IEnumerator KoushuBattle(int mine)
    {
        yield return new WaitForSeconds(0.6f);   // 砕ける画像を少し見せる

        int cpu = Random.Range(0, 3);

        ui.ShowPhase(PhaseUIManager.UIPhase.KoushuBattle);
        var resultLabel = judgeText != null ? judgeText : waitText;   // 結果の文字（Judge がなければ Wait の文字に出す）
        if (waitText != null && waitText != resultLabel) waitText.text = "";
        if (battle1PText != null) battle1PText.text = "あなた\n" + koushuNames[mine];
        if (battle2PText != null) battle2PText.text = "相手\n" + koushuNames[cpu];

        int result = Judge(mine, cpu);   // 1 = 勝ち, -1 = 負け, 0 = あいこ
        if (resultLabel != null) resultLabel.text = result > 0 ? "先攻！" : result < 0 ? "後攻…" : "あいこ！もう一度";
        Debug.Log($"[流れ] 攻守決め：自分 {koushuNames[mine]} / 相手 {koushuNames[cpu]} → {(result > 0 ? "先攻" : result < 0 ? "後攻" : "あいこ")}");

        yield return new WaitForSeconds(resultSeconds);

        if (result == 0)
        {
            StartCoroutine(BeginKoushu(0f));   // あいこはコストを払わずにやり直し
            yield break;
        }
        CostManager.Instance?.Pay(0, koushuCosts[mine]);
        StartBattle(result > 0);
    }

    // 疾風は剛腕に勝つ、剛腕は鉄壁に勝つ、鉄壁は疾風に勝つ（仮のルール）
    private int Judge(int a, int b)
    {
        if (a == b) return 0;
        return (b == (a + 1) % 3) ? 1 : -1;
    }

    // ===================== 2. 戦闘 =====================
    private void StartBattle(bool playerFirst)
    {
        ui.ShowPhase(PhaseUIManager.UIPhase.None);   // パネルを閉じて HUD だけ出す
        if (battleCanvas != null) battleCanvas.SetActive(true);
        if (hideInBattle != null) foreach (var g in hideInBattle) if (g != null) g.SetActive(false);
        Arrange(messageText, new Vector2(0, 110), 48);
        Arrange(actionText, new Vector2(0, 175), 60);
        inBattle = true;
        setEnding = false;
        if (DhukusiInput.Instance != null) DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.Select;

        if (!playerFirst && enemyAttack != null) enemyAttack.StartEnemyAttack();   // 後攻なら敵の攻撃から
        UpdateHud();
    }

    void Update()
    {
        if (!inBattle || setEnding) return;
        UpdateHud();

        int pl = PlayerLife, en = EnemyLife;
        if (en <= 0) StartCoroutine(EndSet(true));
        else if (pl <= 0) StartCoroutine(EndSet(false));
    }

    // ===================== 3. セット =====================
    private IEnumerator EndSet(bool playerWon)
    {
        setEnding = true;
        if (playerWon) sets1P++; else sets2P++;
        UpdateHud();
        Debug.Log($"[流れ] セット終了：{(playerWon ? "あなた" : "相手")} がセット獲得（{sets1P} - {sets2P}）");

        yield return new WaitForSeconds(resultSeconds);

        if (sets1P >= setsToWin || sets2P >= setsToWin)
        {
            inBattle = false;
            ResultView.ShowPvE(sets1P >= setsToWin);   // 4. 勝敗判定へ
            yield break;
        }

        // 次のセット：ライフとコストを元に戻す
        SetLife(maxLife, maxLife);
        CostManager.Instance?.ResetAll();
        if (enemyResultTextField?.GetValue(enemy) is TMPro.TMP_Text rt) rt.text = $"第{sets1P + sets2P + 1}セット！";
        setEnding = false;
        UpdateHud();
        // 次の攻撃はセットを取られた側から：Gamemanager の流れがそのとおりに進む
        //  ・自分が取った（敵のライフを削った）→ 2秒後に敵の攻撃が始まる
        //  ・相手が取った（防御に失敗した）→ 自分の技選択から
    }

    // ===================== 表示 =====================
    private void UpdateHud()
    {
        int pl = player != null ? PlayerLife : maxLife;
        int en = enemy != null ? EnemyLife : maxLife;
        for (int i = 0; i < life1P.Length; i++) if (life1P[i] != null) life1P[i].color = i < pl ? onColor : offColor;
        for (int i = 0; i < life2P.Length; i++) if (life2P[i] != null) life2P[i].color = i < en ? onColor : offColor;
        for (int i = 0; i < set1P.Length; i++) if (set1P[i] != null) set1P[i].color = i < sets1P ? onColor : offColor;
        for (int i = 0; i < set2P.Length; i++) if (set2P[i] != null) set2P[i].color = i < sets2P ? onColor : offColor;
        if (costText != null && CostManager.Instance != null)
            costText.text = $"{CostManager.Instance.GetCurrentCost(0)}/{CostManager.Instance.MaxCost}";
    }

    // 文字が折り返して重ならないように、1行で決まった位置に出す
    private static void Arrange(RectTransform rt, Vector2 pos, float size)
    {
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1600, 100);
        var t = rt.GetComponent<TMPro.TMP_Text>();
        if (t == null) return;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        t.enableAutoSizing = false;
        t.fontSize = size;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.color = Color.white;
        t.outlineWidth = 0.25f;
        t.outlineColor = Color.black;
    }

    private int PlayerLife => (int)playerLifeField.GetValue(player);
    private int EnemyLife => (int)enemyLifeField.GetValue(enemy);

    private void SetLife(int pl, int en)
    {
        playerLifeField.SetValue(player, pl);
        enemyLifeField.SetValue(enemy, en);
        playerUpdateText?.Invoke(player, null);
        enemyUpdateText?.Invoke(enemy, null);
    }
}
