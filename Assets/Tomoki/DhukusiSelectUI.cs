using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// デュクシ選択フェーズの UI（通常時 / ボタン長押し時 / 選択時）
///
///  ・ボタンを押している間、そのデュクシの Image を 1.3倍にする
///  ・長押しキャンセルのゲージ（Image の FillAmount）を伸ばす
///  ・コストが足りないデュクシは暗くする（選択不可能）
///  ・選んだら、そのデュクシの Image を点滅させる
///
/// 【使い方】
///  Panel_DhukusiSelect_Attack にアタッチして、Inspector で 4つの Image を登録
///  DhukusiInput・CostManager がシーンにあれば勝手に連動する
///
///  ※ 仕様書のこのページはまだ空なので、見た目は仮。デザインが来たら差し替える
/// </summary>
public class DhukusiSelectUI : MonoBehaviour
{
    [Header("4つのデュクシの Image  [0]=通常 [1]=フェイント [2]=連続 [3]=激重")]
    [SerializeField] private Image[] dhukusiImages = new Image[4];

    [Header("長押しキャンセルのゲージ（Image Type を Filled にしておく）なくてもOK")]
    [SerializeField] private Image cancelGauge;

    [Header("設定")]
    [SerializeField] private float pressScale = 1.3f;
    [SerializeField] private Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private int blinkCount = 3;

    private int blinkIndex = -1;
    private float blinkTimer;
    private int blinkDone;

    void OnEnable()
    {
        blinkIndex = -1;
        for (int i = 0; i < dhukusiImages.Length; i++)
        {
            if (dhukusiImages[i] == null) continue;
            dhukusiImages[i].enabled = true;
            dhukusiImages[i].transform.localScale = Vector3.one;
        }
        if (cancelGauge != null) cancelGauge.fillAmount = 0f;

        if (DhukusiInput.Instance != null) DhukusiInput.Instance.OnSelect += OnSelect;
    }

    void OnDisable()
    {
        if (DhukusiInput.Instance != null) DhukusiInput.Instance.OnSelect -= OnSelect;
    }

    void Update()
    {
        var input = DhukusiInput.Instance;
        if (input == null) return;

        // ---- コストが足りないデュクシを暗くする ----
        if (CostManager.Instance != null)
        {
            int player = input.ActivePlayer >= 0 ? input.ActivePlayer : input.LastPlayer;
            for (int i = 0; i < dhukusiImages.Length; i++)
            {
                if (dhukusiImages[i] == null) continue;
                bool canUse = CostManager.Instance.CanUse(player, (DhukusiInput.DhukusiType)i);
                dhukusiImages[i].color = canUse ? Color.white : disabledColor;
            }
        }

        // ---- 押している間だけ 1.3倍 ----
        int held = input.HeldDhukusi;   // 押しているデュクシ（-1 = なし）
        for (int i = 0; i < dhukusiImages.Length; i++)
        {
            if (dhukusiImages[i] == null) continue;
            dhukusiImages[i].transform.localScale = Vector3.one * (i == held ? pressScale : 1f);
        }

        // ---- 長押しキャンセルのゲージ ----
        if (cancelGauge != null) cancelGauge.fillAmount = input.CancelProgress;

        // ---- 選んだデュクシを点滅 ----
        if (blinkIndex >= 0) Blink();
    }

    private void OnSelect(DhukusiInput.DhukusiType type)
    {
        blinkIndex = (int)type;
        blinkTimer = 0f;
        blinkDone = 0;
    }

    private void Blink()
    {
        blinkTimer += Time.deltaTime;
        if (blinkTimer < blinkInterval) return;
        blinkTimer = 0f;

        var img = dhukusiImages[blinkIndex];
        if (img == null) { blinkIndex = -1; return; }

        img.enabled = !img.enabled;
        if (img.enabled) blinkDone++;
        if (blinkDone >= blinkCount) blinkIndex = -1;   // 点滅おわり
    }
}
