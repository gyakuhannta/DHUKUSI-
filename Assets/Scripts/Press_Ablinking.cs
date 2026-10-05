using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Press_Ablinking : MonoBehaviour
{
    [Header("対象画像")]
    [SerializeField] private Image targetImage;

    [Header("透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.6f;//最大透明度

    [Header("時間（秒）")]
    [SerializeField] private float fadeInDuration = 1.5f;//フェードイン
    [SerializeField] private float holdZeroAlphaDuration = 0f;//ゼロの時間
    [SerializeField] private float fadeOutDuration = 1.5f;//フェードアウト
    [SerializeField] private float holdMaxAlphaDuration = 2f;//最大値の時間

    private void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        StartCoroutine(FadeLoop());
    }

    private IEnumerator FadeLoop()
    {
        while (true)
        {
            // 透明度 0
            SetAlpha(0f);

            // 透明度0をキープ
            yield return new WaitForSeconds(holdZeroAlphaDuration);

            // 0 → 指定透明度
            yield return FadeAlpha(0f, maxAlpha, fadeInDuration);

            // 指定透明度をキープ
            yield return new WaitForSeconds(holdMaxAlphaDuration);

            // 指定透明度 → 0
            yield return FadeAlpha(maxAlpha, 0f, fadeOutDuration);

            // 次のループへ
        }
    }

    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(endAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (targetImage == null) return;

        Color color = targetImage.color;
        color.a = alpha;
        targetImage.color = color;
    }
}
