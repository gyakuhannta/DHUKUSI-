using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeDuration = 1.0f;

    // Startより前に確実に実行し、最初に真っ暗にする
    private void Awake()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 1f; // 完全に真っ黒
            fadeImage.color = c;
        }
    }

    private void Start()
    {
        if (fadeImage != null)
        {
            StartCoroutine(FadeInRoutine());
        }
    }

    public IEnumerator FadeOutRoutine()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        float elapsedTime = 0f;

        // 開始時は確実に真っ黒（アルファ1）からスタート
        c.a = 1f;
        fadeImage.color = c;

        // 少しだけ待ってからフェードを開始（シーン読み込み直後の負荷によるカクつき対策）
        yield return new WaitForSeconds(0.1f);

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsedTime / fadeInDuration));
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false); // 完全に透明になったら非アクティブ化
    }
}
