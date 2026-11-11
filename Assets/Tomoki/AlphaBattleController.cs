using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// アルファ版：mineSeen の戦闘を、コントローラーとコストで遊べるようにする
///
/// Gamemanager の戦闘（技選択 → 攻撃タイミング → 敵の攻撃 → 防御タイミング）はそのまま使い、外から呼び出しているだけ。
///
///  ・技の選択（仕様書「デュクシ選択フェーズ(攻撃側)」）
///      画面下の 4つのデュクシ（DHUKUSI_Canvas の Panel_DhukusiSelect_Attack）を使う
///      Gamemanager の仮ボタン（ComboButton など）は隠す
///      Y（上）＝通常 / X（左）＝フェイント / B（右）＝連続 / A（下）＝激重
///      離したときに決定、2秒長押しでキャンセル。決定したら 0.1秒消える・0.1秒出るを 4回くり返してから攻撃へ
///  ・コスト：選んだ技のコストを払う。足りない技は選べない（暗く表示）
///  ・攻撃：カーソルが真ん中に来たら B（右）。真ん中なら敵のライフが減る
///  ・防御：敵がどのデュクシを出すか読んで、同じデュクシのボタンを押す。当たれば防御成功、外れたらライフが減る
///  ・テスト用キーボード：1〜4 で技・防御（通常/フェイント/連続/激重）、Space で攻撃のタイミング
/// </summary>
public class AlphaBattleController : MonoBehaviour
{
    [Header("戦闘（自動で探す）")]
    [SerializeField] private TechiqueSelector techniqueSelector;
    [Tooltip("攻撃・防御のタイミングバー。これが出ている間はタイミング入力、消えている間は技の選択")]
    [SerializeField] private GameObject timingBar;

    [Header("技選択の画面（Panel_DhukusiSelect_Attack）")]
    [SerializeField] private GameObject selectPanel;
    [Tooltip("決定してから攻撃が始まるまで（点滅 0.1秒 × 2 × 4回）")]
    [SerializeField] private float decideDelay = 0.8f;

    [Header("表示")]
    [SerializeField] private Text costText;
    [SerializeField] private Text messageText;
    [SerializeField] private float messageSeconds = 1.5f;

    private readonly List<GameObject> oldButtons = new List<GameObject>();

    // 攻撃中・防御中かどうか（AttackTimingController / TimingJudge の中の状態を読む）
    private AttackTimingController attackTiming;
    private TimingJudge timingJudge;
    private System.Reflection.FieldInfo attackingField, defendingField;
    private bool wasTiming, wasDefending, wasAttacking, noneByDefense;
    private GameObject defensePanel;
    private bool deciding;

    // ---- コスト（仕様書「コスト仕様」）----
    //  ・表示は「デュクシ選択を始めたときのコスト」（払った後も、次の選択まではそのまま）
    //  ・防御中は相手（攻撃側）のコストを出す
    //  ・コストが 0 になったら、次の攻撃は「選択不可」で飛ばし、その次の攻撃で 10 に戻る
    //  ・セットが変わったら両方 10 から
    public static string CostDisplay;   // HUD の「5/10」の所に出す文字（AlphaGameFlow が使う）
    public static string CostLabel;     // HUD の「残りコスト」の所に出す文字
    private readonly bool[] skipped = new bool[2];   // [0]=自分 [1]=相手：コスト0で 1回休んだか
    private bool selectReadyBefore, defendingBefore, playerSkipping;
    private static readonly DhukusiInput.DhukusiType[] ZoneTypes =
    {
        DhukusiInput.DhukusiType.Normal, DhukusiInput.DhukusiType.Feint,
        DhukusiInput.DhukusiType.Renzoku, DhukusiInput.DhukusiType.Gekiomo
    };
    private static readonly string[] ZoneNames = { "通常デュクシ", "フェイントデュクシ", "連続デュクシ", "激重デュクシ" };
    private static readonly string[] ZoneShort = { "通常", "フェイント", "連続", "激重" };
    private int attackType = -1;   // 今の攻撃で選んだデュクシ（0=通常 1=フェイント 2=連続 3=激重）

    void Start()
    {
        CostDisplay = null;
        CostLabel = null;
        if (techniqueSelector == null) techniqueSelector = FindFirstObjectByType<TechiqueSelector>();

        DhukusiInput.Instance.ActivePlayer = -1;            // どのコントローラーでも操作できる
        DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.Select;
        DhukusiInput.Instance.OnTiming += OnTiming;
        DhukusiInput.Instance.OnCancel += () => ShowMessage("キャンセル");

        CostManager.Instance.OnDhukusiDecided += OnDecided;
        CostManager.Instance.OnNotEnoughCost += type => ShowMessage("コストが足りない！");
        CostManager.Instance.OnCostChanged += (player, cost, max) => UpdateCostText();

        UpdateCostText();
        if (messageText != null)
        {
            // 案内・警告（コストが足りない など）は、画面の真ん中の下（4つのデュクシの上）に赤で出す
            messageText.text = "";
            var rt = (RectTransform)messageText.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -75);   // 下の木の板の枠より上
            messageText.fontSize = 48;
            messageText.color = new Color32(0xff, 0x5a, 0x4a, 0xff);
        }
        HideOldButtons();

        // 防御のときに出す 4つのデュクシ（Panel_DhukusiSelect_Defense）
        foreach (var ui in FindObjectsByType<DhukusiSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ui.name == "Panel_DhukusiSelect_Defense")
            {
                defensePanel = ui.gameObject;
                ui.UseCost = false;   // 防御ではコストを使わない（暗くしない）
                // 「コスト○」の行を消して、技の名前だけにする
                foreach (var t in ui.GetComponentsInChildren<Text>(true))
                {
                    int i = t.text.IndexOf("コスト");
                    if (i > 0) t.text = t.text.Substring(0, i).TrimEnd('\n', ' ');
                }
            }

        // 長押しキャンセルのゲージは、木の板の間に出ないように枠の下のふちへ
        foreach (var r in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r.name == "CancelGauge_BG") r.anchoredPosition = new Vector2(0, 102);

        const System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        attackTiming = FindFirstObjectByType<AttackTimingController>();
        timingJudge = FindFirstObjectByType<TimingJudge>();
        attackingField = typeof(AttackTimingController).GetField("isAttacking", f);
        defendingField = typeof(TimingJudge).GetField("isDefending", f);
    }

    // Gamemanager の仮ボタン（ComboButton など）は使わないので隠す
    private void HideOldButtons()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) is TechiqueSelector)
                {
                    button.gameObject.SetActive(false);
                    oldButtons.Add(button.gameObject);
                    break;
                }
            }
        }
    }

    void Update()
    {
        var input = DhukusiInput.Instance;
        if (input == null) input = FindFirstObjectByType<DhukusiInput>();
        if (input == null || CostManager.Instance == null)
        {
            if (!warnedMissing) Debug.LogWarning("[アルファ] DhukusiInput か CostManager がシーンにありません。メニュー「DHUKUSI → mineSeen をアルファ版にする」を押し直してください");
            warnedMissing = true;
            return;
        }
        bool attacking = IsAttacking();
        bool defending = IsDefending();
        bool isTiming = attacking || defending;

        // 入力の受け付けを切り替える（攻撃中＝タイミング、防御中＝自分で読む、それ以外＝技選択）
        bool battleInput = input.Phase != DhukusiInput.InputPhase.None || noneByDefense;   // 攻守決めの間（None）は何もしない
        if (isTiming != wasTiming && battleInput)
        {
            input.Phase = isTiming ? DhukusiInput.InputPhase.Timing : DhukusiInput.InputPhase.Select;
            if (!isTiming) noneByDefense = false;
            wasTiming = isTiming;
        }
        if (defending && input.Phase == DhukusiInput.InputPhase.Timing)
        {
            input.Phase = DhukusiInput.InputPhase.None;   // 防御はこのスクリプトでボタンを読む（コストを払わない）
            noneByDefense = true;
        }
        if (!defending && noneByDefense && input.Phase != DhukusiInput.InputPhase.None) noneByDefense = false;
        if (defending != wasDefending)
        {
            ShowGuide("");   // 防御の案内は右の吹き出しに出ている
            wasDefending = defending;
        }
        if (attacking != wasAttacking)
        {
            ShowGuide(attacking && attackType >= 0 ? $"カーソルが「{ZoneShort[attackType]}」の所に来たら B ボタン！" : "");
            // 攻撃中はバーと重ならないように、案内をバーの下に出す
            if (messageText != null)
                ((RectTransform)messageText.transform).anchoredPosition = new Vector2(0, attacking ? -190 : -75);
            wasAttacking = attacking;
        }

        // 技を選べるときだけ、下の 4つのデュクシを出す
        // 攻撃の結果を出してから敵の攻撃が始まるまで（約2秒）は選べない
        bool waitingEnemy = attackTiming != null && attackTiming.IsInvoking("StartEnemyDefense");
        bool selectReady = input.Phase == DhukusiInput.InputPhase.Select && !isTiming && !waitingEnemy && !deciding;

        // 自分の攻撃ターン／相手の攻撃ターンが始まったとき、コストの処理をする
        if (selectReady && !selectReadyBefore) BeginPlayerTurn();
        selectReadyBefore = selectReady;
        if (defending && !defendingBefore)
        {
            defendingBefore = true;
            if (!BeginEnemyTurn()) return;   // 相手がコスト0で休んだ
        }
        if (!defending) defendingBefore = false;

        bool canSelect = selectReady && !playerSkipping;
        if (selectPanel != null && selectPanel.activeSelf != (canSelect || deciding))
            selectPanel.SetActive(canSelect || deciding);
        if (defensePanel != null && defensePanel.activeSelf != defending)
            defensePanel.SetActive(defending);
        // タイミングバーは攻撃のときだけ出す（技選択中と、ボタンで読み合う防御中は隠す）
        if ((canSelect || defending) && timingBar != null && timingBar.activeSelf) timingBar.SetActive(false);

        TidyTexts(defending);

        // ---- 防御：Y=通常 X=フェイント B=連続 A=激重（キーボードは 1〜4）----
        if (defending)
        {
            int guard = ReadGuard();
            if (guard >= 0) Defend(guard);
        }

        // ---- テスト用キーボード ----
        var kb = Keyboard.current;
        if (kb == null) return;
        if (canSelect && !deciding)
        {
            if (kb.digit1Key.wasPressedThisFrame) SelectWithCost(DhukusiInput.DhukusiType.Normal);
            if (kb.digit2Key.wasPressedThisFrame) SelectWithCost(DhukusiInput.DhukusiType.Feint);
            if (kb.digit3Key.wasPressedThisFrame) SelectWithCost(DhukusiInput.DhukusiType.Renzoku);
            if (kb.digit4Key.wasPressedThisFrame) SelectWithCost(DhukusiInput.DhukusiType.Gekiomo);
        }
        if (attacking && kb.spaceKey.wasPressedThisFrame) OnTiming();
    }

    // 文字が重ならないように整理する
    //  ・防御中：「たいき」などの文字と「技を選んでください」は消して、案内だけ出す
    //  ・それ以外：「防御成功！」「防御失敗！」のときだけ防御の文字を出す
    private TMPro.TMP_Text actionTmp, resultTmp;
    private float defenseEndTime = -100f;
    private bool warnedMissing;
    private void TidyTexts(bool defending)
    {
        const System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        if (actionTmp == null && timingJudge != null)
            actionTmp = typeof(TimingJudge).GetField("actionText", f)?.GetValue(timingJudge) as TMPro.TMP_Text;
        if (resultTmp == null && attackTiming != null)
            resultTmp = typeof(AttackTimingController).GetField("resultText", f)?.GetValue(attackTiming) as TMPro.TMP_Text;

        if (actionTmp != null)
        {
            // 「防御成功！」「防御失敗！」は防御が終わってから 1.5秒だけ出す
            if (defending) defenseEndTime = -1f;
            else if (defenseEndTime < 0f) defenseEndTime = Time.time;
            bool showAction = !defending && actionTmp.text.StartsWith("防御") && Time.time - defenseEndTime < 1.5f;
            if (actionTmp.enabled != showAction) actionTmp.enabled = showAction;
        }
        if (resultTmp != null && defending && resultTmp.text != "") resultTmp.text = "";
        // 「技を選んでください」は右の吹き出しと同じことなので出さない
        if (resultTmp != null && resultTmp.text.StartsWith("技を選んで")) resultTmp.text = "";
    }

    private bool IsAttacking() => attackTiming != null && attackingField != null && (bool)attackingField.GetValue(attackTiming);
    private bool IsDefending() => timingJudge != null && defendingField != null && (bool)defendingField.GetValue(timingJudge);

    // 防御で押したボタン（0=通常 1=フェイント 2=連続 3=激重、押していなければ -1）
    private int ReadGuard()
    {
        foreach (var pad in Gamepad.all)
        {
            if (pad.buttonNorth.wasPressedThisFrame) return 0;
            if (pad.buttonWest.wasPressedThisFrame)  return 1;
            if (pad.buttonEast.wasPressedThisFrame)  return 2;
            if (pad.buttonSouth.wasPressedThisFrame) return 3;
        }
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) return 0;
            if (kb.digit2Key.wasPressedThisFrame) return 1;
            if (kb.digit3Key.wasPressedThisFrame) return 2;
            if (kb.digit4Key.wasPressedThisFrame) return 3;
        }
        return -1;
    }

    // 選んだデュクシの場所にカーソルを置いて判定する（敵と同じデュクシなら防御成功）
    private void Defend(int guard)
    {
        const System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var t = typeof(TimingJudge);
        var cursor = t.GetField("cursor", f)?.GetValue(timingJudge) as RectTransform;
        var zones = t.GetField("successZones", f)?.GetValue(timingJudge) as RectTransform[];
        if (cursor == null || zones == null || guard >= zones.Length || zones[guard] == null) return;

        var pos = cursor.anchoredPosition;
        pos.x = zones[guard].anchoredPosition.x;
        cursor.anchoredPosition = pos;
        t.GetMethod("CheckCursorZone", f)?.Invoke(timingJudge, null);
        t.GetMethod("JudgeAction", f)?.Invoke(timingJudge, null);
        ShowGuide("");
    }

    private void ShowGuide(string text)
    {
        if (messageText == null) return;
        CancelInvoke(nameof(ClearMessage));
        messageText.text = text;
    }

    // ===================== コスト =====================
    private void BeginPlayerTurn()
    {
        var cm = CostManager.Instance;
        if (cm.GetCurrentCost(0) <= 0)
        {
            if (!skipped[0])
            {
                // コストを使い果たした次のターンは選べない
                skipped[0] = true;
                CostLabel = "残りコスト";
                CostDisplay = "選択不可";
                playerSkipping = true;
                ShowGuide("コストが 0 なので、このターンは攻撃できない！");
                StartCoroutine(SkipPlayerTurn());
                return;
            }
            skipped[0] = false;
            cm.RecoverIfEmpty(0);   // 1ターン休んだので 10 に戻る
        }
        CostLabel = "残りコスト";
        CostDisplay = $"{cm.GetCurrentCost(0)}/{cm.MaxCost}";
    }

    private IEnumerator SkipPlayerTurn()
    {
        yield return new WaitForSeconds(2f);
        ShowGuide("");
        playerSkipping = false;
        var enemyAttack = FindFirstObjectByType<EnemyAttackController>();
        if (enemyAttack != null) enemyAttack.StartEnemyAttack();
    }

    // 相手の攻撃が始まったとき。相手もコストを払う（足りない技なら、払える技に変える）
    // 相手がコスト0で休むときは false
    private bool BeginEnemyTurn()
    {
        var cm = CostManager.Instance;
        const System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var t = typeof(TimingJudge);

        if (cm.GetCurrentCost(1) <= 0)
        {
            if (!skipped[1])
            {
                skipped[1] = true;
                CostLabel = "相手の残りコスト";
                CostDisplay = "選択不可";
                t.GetMethod("EndDefense", f)?.Invoke(timingJudge, null);   // ダメージなしで防御を終わる
                ShowMessage("相手はコスト切れで攻撃できない！");
                return false;
            }
            skipped[1] = false;
            cm.RecoverIfEmpty(1);
        }

        int before = cm.GetCurrentCost(1);
        var zoneField = t.GetField("correctZone", f);
        int zone = zoneField != null ? (int)zoneField.GetValue(timingJudge) : 0;
        if (zone < 0 || zone >= ZoneTypes.Length || !cm.CanUse(1, ZoneTypes[zone]))
        {
            var ok = new List<int>();
            for (int i = 0; i < ZoneTypes.Length; i++) if (cm.CanUse(1, ZoneTypes[i])) ok.Add(i);
            zone = ok.Count > 0 ? ok[Random.Range(0, ok.Count)] : 0;
            zoneField?.SetValue(timingJudge, zone);
            t.GetField("enemyTechnique", f)?.SetValue(timingJudge, ZoneNames[zone]);
        }
        cm.Pay(1, cm.GetCost(ZoneTypes[zone]));
        CostLabel = "相手の残りコスト";
        CostDisplay = $"{before}/{cm.MaxCost}";
        return true;
    }

    /// <summary>セットが変わったとき（AlphaGameFlow から呼ぶ）</summary>
    public void OnSetReset()
    {
        skipped[0] = skipped[1] = false;
        CostDisplay = null;
    }

    private void SelectWithCost(DhukusiInput.DhukusiType type)
    {
        var cost = CostManager.Instance;
        if (!cost.CanUse(0, type))
        {
            ShowMessage("コストが足りない！");
            return;
        }
        cost.Pay(0, cost.GetCost(type));
        OnDecided(type);
    }

    // コストを払えた技を、点滅させてから Gamemanager の技選択に渡す
    private void OnDecided(DhukusiInput.DhukusiType type)
    {
        if (deciding) return;
        StartCoroutine(Decide(type));
    }

    private IEnumerator Decide(DhukusiInput.DhukusiType type)
    {
        deciding = true;
        attackType = System.Array.IndexOf(ZoneTypes, type);
        DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.None;   // 点滅中は入力しない
        yield return new WaitForSeconds(decideDelay);
        deciding = false;
        DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.Select;

        if (techniqueSelector == null) yield break;
        switch (type)
        {
            case DhukusiInput.DhukusiType.Normal:  techniqueSelector.SelectNormal(); break;
            case DhukusiInput.DhukusiType.Feint:   techniqueSelector.SelectFeint();  break;
            case DhukusiInput.DhukusiType.Renzoku: techniqueSelector.SelectCombo();  break;
            case DhukusiInput.DhukusiType.Gekiomo: techniqueSelector.SelectHeavy();  break;
        }
    }

    // タイミングボタン → マウスの左クリックとして送る（タイミング判定がクリックを見ているため）
    private void OnTiming()
    {
        if (IsAttacking()) StartCoroutine(AttackClick());
        else StartCoroutine(Click());
    }

    // 仕様書：選んだデュクシの範囲（バーの 4つの色の 1つ）にカーソルがあるときに押すと攻撃成功
    //  攻撃の判定のスクリプトは「真ん中 -40〜40 なら成功」なので、押した瞬間だけカーソルを
    //  成功なら真ん中、失敗なら真ん中の外に置いてからクリックを送り、判定のあとで元の位置に戻す
    private bool attackClicking;
    private IEnumerator AttackClick()
    {
        if (attackClicking) yield break;
        const System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var cursor = typeof(AttackTimingController).GetField("cursor", f)?.GetValue(attackTiming) as RectTransform;
        var zones = timingJudge != null ? typeof(TimingJudge).GetField("successZones", f)?.GetValue(timingJudge) as RectTransform[] : null;
        if (cursor == null) { yield return Click(); yield break; }

        attackClicking = true;
        float x = cursor.anchoredPosition.x;
        int hit = ZoneAt(zones, x);
        bool success = attackType >= 0 && hit == attackType;
        Debug.Log($"[攻撃] 押した場所：{(hit >= 0 ? ZoneShort[hit] : "範囲の外")}　選んだデュクシ：{(attackType >= 0 ? ZoneShort[attackType] : "?")} → {(success ? "成功" : "失敗")}");

        var p = cursor.anchoredPosition;
        p.x = success ? 0f : (x >= 0f ? 200f : -200f);
        cursor.anchoredPosition = p;
        yield return Click();
        for (int i = 0; i < 10 && IsAttacking(); i++)
        {
            p = cursor.anchoredPosition; p.x = success ? 0f : (x >= 0f ? 200f : -200f);
            cursor.anchoredPosition = p;
            yield return null;
        }
        p = cursor.anchoredPosition; p.x = x;   // 見た目は押した場所に戻す
        cursor.anchoredPosition = p;
        attackClicking = false;
    }

    // カーソルがどのデュクシの範囲にあるか（無ければ -1）
    private static int ZoneAt(RectTransform[] zones, float x)
    {
        if (zones == null) return -1;
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null) continue;
            float half = zones[i].rect.width * Mathf.Abs(zones[i].localScale.x) / 2f;
            float cx = zones[i].anchoredPosition.x;
            if (x >= cx - half && x <= cx + half) return i;
        }
        return -1;
    }

    private IEnumerator Click()
    {
        var mouse = Mouse.current;
        if (mouse == null) yield break;
        Vector2 pos = mouse.position.ReadValue();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pos }.WithButton(MouseButton.Left, true));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pos }.WithButton(MouseButton.Left, false));
    }

    private void UpdateCostText()
    {
        if (costText == null || CostManager.Instance == null) return;
        costText.text = $"残りコスト {CostManager.Instance.GetCurrentCost(0)} / {CostManager.Instance.MaxCost}";
    }

    private void ShowMessage(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), messageSeconds);
    }

    private void ClearMessage()
    {
        if (messageText != null) messageText.text = "";
    }

    void OnDestroy()
    {
        if (DhukusiInput.Instance != null) DhukusiInput.Instance.OnTiming -= OnTiming;
        if (CostManager.Instance != null) CostManager.Instance.OnDhukusiDecided -= OnDecided;
    }
}
