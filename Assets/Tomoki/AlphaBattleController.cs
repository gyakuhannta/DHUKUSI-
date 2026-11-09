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
///  ・タイミング：バーが出ている間に B（右）でタイミングを決める
///  ・テスト用キーボード：1〜4 で技（通常/フェイント/連続/激重）、Space でタイミング
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
    private bool wasTiming;
    private bool deciding;

    void Start()
    {
        if (techniqueSelector == null) techniqueSelector = FindFirstObjectByType<TechiqueSelector>();

        DhukusiInput.Instance.ActivePlayer = -1;            // どのコントローラーでも操作できる
        DhukusiInput.Instance.Phase = DhukusiInput.InputPhase.Select;
        DhukusiInput.Instance.OnTiming += OnTiming;
        DhukusiInput.Instance.OnCancel += () => ShowMessage("キャンセル");

        CostManager.Instance.OnDhukusiDecided += OnDecided;
        CostManager.Instance.OnNotEnoughCost += type => ShowMessage("コストが足りない！");
        CostManager.Instance.OnCostChanged += (player, cost, max) => UpdateCostText();

        UpdateCostText();
        if (messageText != null) messageText.text = "";
        HideOldButtons();

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

        // タイミングバーが出ているかどうかで、入力の受け付けを切り替える
        bool isTiming = IsTiming();
        if (isTiming != wasTiming && input.Phase != DhukusiInput.InputPhase.None)
        {
            input.Phase = isTiming ? DhukusiInput.InputPhase.Timing : DhukusiInput.InputPhase.Select;

            // 自分の攻撃ターンが戻ってきたとき、コストを使い切っていたら全回復（仕様書のコスト仕様）
            if (!isTiming) CostManager.Instance.RecoverIfEmpty(0);
            wasTiming = isTiming;
        }

        // 技を選べるときだけ、下の 4つのデュクシを出す
        // 攻撃の結果を出してから敵の攻撃が始まるまで（約2秒）は選べない
        bool waitingEnemy = attackTiming != null && attackTiming.IsInvoking("StartEnemyDefense");
        bool canSelect = input.Phase == DhukusiInput.InputPhase.Select && !isTiming && !waitingEnemy;
        if (selectPanel != null && selectPanel.activeSelf != (canSelect || deciding))
            selectPanel.SetActive(canSelect || deciding);
        // 技を選んでいる間はタイミングバーを隠す（攻撃・防御が始まると自動で出る）
        if (canSelect && timingBar != null && timingBar.activeSelf) timingBar.SetActive(false);

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
        if (isTiming && kb.spaceKey.wasPressedThisFrame) OnTiming();
    }

    // バーが動いていて、タイミングを押すのを待っているとき
    private bool IsTiming()
    {
        bool attacking = attackTiming != null && attackingField != null && (bool)attackingField.GetValue(attackTiming);
        bool defending = timingJudge != null && defendingField != null && (bool)defendingField.GetValue(timingJudge);
        if (attackingField == null && defendingField == null) return timingBar != null && timingBar.activeInHierarchy;
        return attacking || defending;
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
        StartCoroutine(Click());
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
