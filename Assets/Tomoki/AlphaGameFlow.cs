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
    [SerializeField] private Text timerText;   // 経過時間（空なら Timer_Text を自動で探す）

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
    [SerializeField] private Color lifeColor = new Color32(0xe5, 0x39, 0x35, 0xff);   // 残っているライフは赤

    private FieldInfo playerLifeField, enemyLifeField;
    private MethodInfo playerUpdateText, enemyUpdateText;
    private FieldInfo enemyResultTextField;

    private int sets1P, sets2P;
    private float elapsed;
    private bool inBattle;
    /// <summary>攻撃・防御をしている間だけ true（攻守決めや勝敗の間は false）。AlphaBattleController が見る</summary>
    public static bool InBattle { get; private set; }
    private bool setEnding;

    void Awake()
    {
        InBattle = false;
        FixAllTmpMaterials();
    }

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

        if (timerText == null && ui != null)
            foreach (var t in ui.GetComponentsInChildren<Text>(true))
                if (t.name == "Timer_Text") timerText = t;

        if (koushuSelect != null) koushuSelect.OnDecided += OnKoushuDecided;
        if (CostManager.Instance != null) CostManager.Instance.OnCostChanged += (p, c, m) => UpdateHud();

        StartCoroutine(BeginKoushu(0.1f));
    }

    // ===================== 1. 攻守決め =====================
    private IEnumerator BeginKoushu(float delay)
    {
        inBattle = false; InBattle = false;
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
        CostManager.Instance?.Pay(1, koushuCosts[cpu]);   // 相手も払う
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
        Arrange(messageText, new Vector2(0, 70), 40, Color.white);
        Arrange(actionText, new Vector2(0, 150), 48, onColor);   // 防御成功／失敗は山吹色
        inBattle = true; InBattle = true;
        setEnding = false;
        if (DhukusiInput.Instance != null) DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.Select;

        if (!playerFirst && enemyAttack != null) enemyAttack.StartEnemyAttack();   // 後攻なら敵の攻撃から
        UpdateHud();
    }

    void Update()
    {
        if (!inBattle) return;

        // 経過時間（戦闘が始まってから。勝敗が決まるまで数える）
        elapsed += Time.deltaTime;
        if (timerText != null) timerText.text = $"経過時間 {(int)elapsed / 60:00}:{(int)elapsed % 60:00}";

        if (setEnding) return;
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
            inBattle = false; InBattle = false;
            ResultView.ShowPvE(sets1P >= setsToWin);   // 4. 勝敗判定へ
            yield break;
        }

        // 次のセット：ライフとコストを元に戻す
        SetLife(maxLife, maxLife);
        CostManager.Instance?.ResetAll();
        FindFirstObjectByType<AlphaBattleController>()?.OnSetReset();
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
        // 仕様書：ライフは目（Eyes）。減ったら消える
        for (int i = 0; i < life1P.Length; i++) if (life1P[i] != null) { life1P[i].enabled = i < pl; life1P[i].color = Color.white; }
        for (int i = 0; i < life2P.Length; i++) if (life2P[i] != null) { life2P[i].enabled = i < en; life2P[i].color = Color.white; }
        // セット（はちまき）：取るまでは出さない。取ったら出てくる
        for (int i = 0; i < set1P.Length; i++) if (set1P[i] != null) { set1P[i].enabled = i < sets1P; set1P[i].color = Color.white; }
        for (int i = 0; i < set2P.Length; i++) if (set2P[i] != null) { set2P[i].enabled = i < sets2P; set2P[i].color = Color.white; }
        // 仕様書「コスト仕様」：選択を始めたときのコスト（防御中は相手のコスト）。戦闘前は今のコスト
        if (costText != null && CostManager.Instance != null)
        {
            costText.text = inBattle && AlphaBattleController.CostDisplay != null
                ? AlphaBattleController.CostDisplay
                : $"{CostManager.Instance.GetCurrentCost(0)}/{CostManager.Instance.MaxCost}";
            if (costLabel == null && costText.transform.parent != null)
                costLabel = costText.transform.parent.Find("Cost_Label")?.GetComponent<Text>();
            if (costLabel != null) costLabel.text = inBattle && AlphaBattleController.CostLabel != null ? AlphaBattleController.CostLabel : "残りコスト";
        }
    }

    // 文字が折り返して重ならないように、1行で決まった位置に出す
    private static void Arrange(RectTransform rt, Vector2 pos, float size, Color color)
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
        t.color = color;
        if (!FixTmpMaterial(t)) return;   // マテリアルが無いと縁取りでエラーになるので、そのときは縁取りしない
        t.outlineWidth = 0.25f;
        t.outlineColor = Color.black;
    }

    // TextMeshPro の文字のマテリアルが消えていたら、フォントのマテリアルを入れ直す
    //  （パソコンによってフォントの読み込み直しでマテリアルが外れることがある）
    private static bool FixTmpMaterial(TMPro.TMP_Text t)
    {
        if (t == null) return false;
        if (t.fontSharedMaterial != null) return true;
        var font = t.font;
        if (font == null || font.material == null) font = TMPro.TMP_Settings.defaultFontAsset;
        if (font == null || font.material == null) return false;
        t.font = font;
        t.fontSharedMaterial = font.material;
        return true;
    }

    // シーンの中の TextMeshPro の文字を全部チェックする
    private static void FixAllTmpMaterials()
    {
        foreach (var t in FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            FixTmpMaterial(t);
    }

    private Text costLabel;

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
