using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フェーズごとの UI パネルを切り替える
///
/// 【使い方】
///  1. Canvas の下に、フェーズごとのパネル（空の GameObject）を作る
///       Panel_KoushuSelect / Panel_KoushuBattle / Panel_DhukusiSelect_Attack ... など
///  2. この スクリプトを Canvas か空の GameObject にアタッチ
///  3. Inspector の Panels に「フェーズ」と「パネル」の組み合わせを登録
///  4. ゲームの進行管理スクリプトから呼ぶ
///       PhaseUIManager.Instance.ShowPhase(PhaseUIManager.UIPhase.DhukusiSelectAttack);
///
///  ※ 指定したフェーズのパネルだけ表示して、ほかは全部非表示にする
///  ※ ライフ・コストなど「ずっと出しておくUI」はパネルに入れずに Canvas 直下に置く
/// </summary>
public class PhaseUIManager : MonoBehaviour
{
    public static PhaseUIManager Instance { get; private set; }

    //仕様書のシーン一覧と同じ並び
    public enum UIPhase
    {
        None,
        KoushuSelect,          // 攻守決めフェーズ選択画面
        KoushuBattle,          // 攻守決めフェーズ勝負画面
        DhukusiSelectAttack,   // デュクシ選択フェーズ（攻撃側）
        DhukusiSelectDefense,  // デュクシ選択フェーズ（防御側）
        KuridashiAttack,       // デュクシ繰り出しフェーズ（攻撃側）
        KuridashiDefense,      // デュクシ繰り出しフェーズ（防御側）
        Result,                // 勝敗判定画面
        Title,                 // タイトル画面（あとから追加したので最後）
        ModeSelect             // モード選択画面
    }

    [Serializable]
    public class PhasePanel
    {
        public UIPhase phase;
        public GameObject panel;
    }

    [Header("フェーズとパネルの組み合わせ")]
    [SerializeField] private List<PhasePanel> panels = new List<PhasePanel>();

    [Header("共通HUD（ライフ・コスト）タイトルとモード選択では非表示")]
    [SerializeField] private GameObject hud;

    [Header("最初に表示するフェーズ")]
    [SerializeField] private UIPhase startPhase = UIPhase.None;

    public UIPhase CurrentPhase { get; private set; } = UIPhase.None;

    //フェーズが切り替わった（ほかのUIスクリプトが使う）
    public event Action<UIPhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        ShowPhase(startPhase);
    }

    /// <summary>指定したフェーズのパネルだけ表示する</summary>
    public void ShowPhase(UIPhase phase)
    {
        foreach (var p in panels)
        {
            if (p.panel != null) p.panel.SetActive(p.phase == phase);
        }

        if (hud != null) hud.SetActive(phase != UIPhase.Title && phase != UIPhase.ModeSelect);

        CurrentPhase = phase;
        Debug.Log($"[UI] フェーズ切り替え: {phase}");
        OnPhaseChanged?.Invoke(phase);
    }
}