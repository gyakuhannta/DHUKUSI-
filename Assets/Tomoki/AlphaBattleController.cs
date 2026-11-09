using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// アルファ版：mineSeen の戦闘を、コントローラーとコストで遊べるようにする
///
/// 新井くんの戦闘（技選択 → 攻撃タイミング → 敵の攻撃 → 防御タイミング）はそのまま使う。
/// 新井くんのスクリプトは変えずに、外から呼び出しているだけ。
///
///  ・技の選択：コントローラーの 4つのボタン（DhukusiInput）
///      上＝通常 / 左＝フェイント / 右＝連続 / 下＝激重（離したときに決定、2秒長押しでキャンセル）
///  ・コスト：選んだ技のコストを払う。足りない技は選べない（CostManager）
///  ・タイミング：右ボタンを押すと、マウスの左クリックと同じ扱いになる
///  ・マウスでの操作も今までどおり使える
///
/// 【置き方】メニュー「DHUKUSI → mineSeen をアルファ版にする」で自動で置かれる
/// </summary>
public class AlphaBattleController : MonoBehaviour
{
    [Header("戦闘（自動で探す）")]
    [SerializeField] private TechiqueSelector techniqueSelector;
    [Tooltip("攻撃・防御のタイミングバー。これが出ている間はタイミング入力、消えている間は技の選択")]
    [SerializeField] private GameObject timingBar;

    [Header("コストの表示")]
    [SerializeField] private Text costText;
    [SerializeField] private Text messageText;
    [SerializeField] private float messageSeconds = 1.5f;

    private bool wasTiming;

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
        RouteMouseButtons();
    }

    // マウスで押す 4つのボタンも、コストを払ってから技を出すようにつなぎ替える（シーンのデータは変えない）
    private void RouteMouseButtons()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (!(button.onClick.GetPersistentTarget(i) is TechiqueSelector)) continue;
                DhukusiInput.DhukusiType type;
                switch (button.onClick.GetPersistentMethodName(i))
                {
                    case "SelectNormal": type = DhukusiInput.DhukusiType.Normal; break;
                    case "SelectFeint":  type = DhukusiInput.DhukusiType.Feint; break;
                    case "SelectCombo":  type = DhukusiInput.DhukusiType.Renzoku; break;
                    case "SelectHeavy":  type = DhukusiInput.DhukusiType.Gekiomo; break;
                    default: continue;
                }
                button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                button.onClick.AddListener(() => SelectWithCost(type));
            }
        }
    }

    private void SelectWithCost(DhukusiInput.DhukusiType type)
    {
        if (DhukusiInput.Instance.Phase != DhukusiInput.InputPhase.Select) return;   // 攻撃中・防御中は選べない
        var cost = CostManager.Instance;
        if (!cost.CanUse(0, type))
        {
            ShowMessage("コストが足りない！");
            return;
        }
        cost.Pay(0, cost.GetCost(type));
        OnDecided(type);
    }

    void Update()
    {
        // タイミングバーが出ているかどうかで、入力の受け付けを切り替える
        bool isTiming = timingBar != null && timingBar.activeInHierarchy;
        if (isTiming != wasTiming)
        {
            DhukusiInput.Instance.Phase = isTiming ? DhukusiInput.InputPhase.Timing : DhukusiInput.InputPhase.Select;

            // 自分の攻撃ターンが戻ってきたとき、コストを使い切っていたら全回復（仕様書のコスト仕様）
            if (!isTiming) CostManager.Instance.RecoverIfEmpty(0);
            wasTiming = isTiming;
        }
    }

    // コストを払えた技を、新井くんの技選択に渡す
    private void OnDecided(DhukusiInput.DhukusiType type)
    {
        if (techniqueSelector == null) return;
        switch (type)
        {
            case DhukusiInput.DhukusiType.Normal:  techniqueSelector.SelectNormal(); break;
            case DhukusiInput.DhukusiType.Feint:   techniqueSelector.SelectFeint();  break;
            case DhukusiInput.DhukusiType.Renzoku: techniqueSelector.SelectCombo();  break;
            case DhukusiInput.DhukusiType.Gekiomo: techniqueSelector.SelectHeavy();  break;
        }
    }

    // タイミングボタン → マウスの左クリックとして送る（新井くんのタイミング判定はクリックを見ているため）
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
