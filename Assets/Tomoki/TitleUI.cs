using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// タイトル画面（仕様書「タイトル画面」シート）
///
///  ・Image[Press_A] を 透明度0 → 40（1.5秒）→ 0（1.5秒）→ 0のまま2秒 をくり返す
///  ・透明度と秒数は Inspector で変えられる
///  ・A ボタンでモード選択画面へ
///
/// 【使い方】
///  1. Panel_Title にアタッチ
///  2. Inspector の Press A に Press_A を入れる
///
///  ※ 透明度は Unity のカラーピッカーの A と同じ 0〜255 の数字
///    （仕様書の「40」がこの意味で合っているか、プランナーに確認すること）
/// </summary>
public class TitleUI : MonoBehaviour
{
    [Header("点滅させる画像")]
    [SerializeField] private GameObject pressA;

    [Header("透明度（0〜255）")]
    [SerializeField, Range(0, 255)] private float minAlpha = 0f;
    [SerializeField, Range(0, 255)] private float maxAlpha = 40f;

    [Header("秒数")]
    [SerializeField] private float fadeInSeconds = 1.5f;    // 0 → 40
    [SerializeField] private float fadeOutSeconds = 1.5f;   // 40 → 0
    [SerializeField] private float keepSeconds = 2f;        // 0 のままキープ

    [Header("A ボタンで進む")]
    [SerializeField] private DecideButton aButton = DecideButton.South;
    [SerializeField] private bool goToModeSelect = true;

    // Xbox の A は下、Switch の A は右
    public enum DecideButton { South, East }

    private CanvasGroup group;

    void Awake()
    {
        if (pressA == null) return;
        // 画像と仮の文字をまとめて透明にするため CanvasGroup を使う
        group = pressA.GetComponent<CanvasGroup>();
        if (group == null) group = pressA.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (group != null) StartCoroutine(Blink());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator Blink()
    {
        while (true)
        {
            yield return Fade(minAlpha, maxAlpha, fadeInSeconds);
            yield return Fade(maxAlpha, minAlpha, fadeOutSeconds);
            SetAlpha(minAlpha);
            yield return new WaitForSeconds(keepSeconds);
        }
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a255)
    {
        group.alpha = a255 / 255f;
    }

    void Update()
    {
        if (!goToModeSelect) return;

        // コントローラーが無いときのテスト用：Enter / スペース
        var kb = Keyboard.current;
        bool key = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);

        foreach (var pad in Gamepad.all)
        {
            var button = aButton == DecideButton.South ? pad.buttonSouth : pad.buttonEast;
            if (button.wasPressedThisFrame) key = true;
        }
        {
            if (key)
            {
                Debug.Log("[タイトル] A ボタン → モード選択へ");
                PhaseUIManager.Instance.ShowPhase(PhaseUIManager.UIPhase.ModeSelect);
                return;
            }
        }
    }
}
