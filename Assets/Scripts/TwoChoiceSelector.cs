using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TwoChoiceSelector : MonoBehaviour
{
    [Header("選択肢1の白い画像")]
    [SerializeField] private GameObject image1;

    [Header("選択肢2の白い画像")]
    [SerializeField] private GameObject image2;

    [Header("最初に選択する項目")]
    [SerializeField] private int startSelection = 0;

    [Header("点滅する画像１")]
    [SerializeField] private GameObject Selectimage1;

    [Header("点滅する画像２")]
    [SerializeField] private GameObject Selectimage2;

    [Header("○ボタン点滅設定")]
    [SerializeField] private float blinkOffTime = 0.3f;
    [SerializeField] private float blinkOnTime = 0.3f;
    [SerializeField] private int blinkCount = 3;

    [Header("選択肢1の遷移先シーン名")]
    [SerializeField] private string sceneForSelection1;

    [Header("選択肢2の遷移先シーン名")]
    [SerializeField] private string sceneForSelection2;

    [Header("--- 長押し判定設定 ---")]
    [Tooltip("入力を受け付ける最小の押し込み時間（秒）")]
    [SerializeField] private float minHoldDuration = 0.3f;

    [Tooltip("この時間以上押し続けると入力をキャンセルする（秒）")]
    [SerializeField] private float cancelHoldDuration = 3f;

    [Tooltip("長押し進捗を表示するUI（Image TypeをFilledにしたもの。空欄でも動作します）")]
    [SerializeField] private Image holdProgressImage;

    // ★追加：フェードアウト用の設定
    [Header("--- フェードアウト設定 ---")]
    [Tooltip("画面を暗くするための全画面Image（黒など）")]
    [SerializeField] private Image fadeImage;
    [Tooltip("フェードアウトにかかる時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;
    [Tooltip("フェードインにかかる時間（秒）")]
    [SerializeField] private float fadeInDuration = 1.0f;

    private int currentSelection;
    private bool canMove = true;
    private bool isBlinking = false;

    // 長押し判定用の内部変数
    private float buttonPressedTime = 0f;
    private bool isHolding = false;
    private bool isCancelled = false;

    private void Start()
    {
        currentSelection = Mathf.Clamp(startSelection, 0, 1);
        UpdateSelection();

        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0f;
        }

        // ★追加：開始時はフェード用画像を透明にしておく
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false); // 念のため非アクティブ
        }
    }

    private void Update()
    {
        if (isBlinking) return;

        float moveY = 0f;

        if (Gamepad.current != null)
        {
            float stickY = Gamepad.current.leftStick.ReadValue().y;
            float dpadY = Gamepad.current.dpad.ReadValue().y;
            if (Mathf.Abs(stickY) > 0.5f) moveY = stickY;
            else if (Mathf.Abs(dpadY) > 0.5f) moveY = dpadY;
        }

        if (Keyboard.current != null && moveY == 0f)
        {
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) moveY = 1f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) moveY = -1f;
        }

        if (!isHolding)
        {
            if (canMove && moveY > 0.5f)
            {
                MoveSelection(-1);
                canMove = false;
            }
            else if (canMove && moveY < -0.5f)
            {
                MoveSelection(1);
                canMove = false;
            }

            if (Mathf.Abs(moveY) < 0.2f)
            {
                canMove = true;
            }
        }

        bool isSubmitTriggered = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isSubmitTriggered = true;
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) isSubmitTriggered = true;

        if (isSubmitTriggered)
        {
            StartCoroutine(BlinkSelectedImage());
            return;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                isHolding = true;
                isCancelled = false;
                buttonPressedTime = 0f;
            }

            if (isHolding && Gamepad.current.buttonEast.isPressed)
            {
                buttonPressedTime += Time.deltaTime;

                if (holdProgressImage != null)
                {
                    float progress = Mathf.InverseLerp(minHoldDuration, cancelHoldDuration, buttonPressedTime);
                    holdProgressImage.fillAmount = progress;
                }

                if (buttonPressedTime >= cancelHoldDuration && !isCancelled)
                {
                    isCancelled = true;
                    Debug.Log("3秒以上長押しされたため、選択をキャンセルしました。");
                }
            }

            if (Gamepad.current.buttonEast.wasReleasedThisFrame)
            {
                if (isHolding)
                {
                    if (!isCancelled && buttonPressedTime >= minHoldDuration && buttonPressedTime < cancelHoldDuration)
                    {
                        StartCoroutine(BlinkSelectedImage());
                    }
                    else
                    {
                        Debug.Log($"条件外のため不発：押していた時間 {buttonPressedTime:F2} 秒");
                    }

                    ResetHoldState();
                }
            }
        }
    }

    private void ResetHoldState()
    {
        isHolding = false;
        isCancelled = false;
        buttonPressedTime = 0f;
        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0f;
        }
    }

    private void MoveSelection(int direction)
    {
        currentSelection += direction;

        if (currentSelection < 0) currentSelection = 1;
        if (currentSelection > 1) currentSelection = 0;

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (image1 != null) image1.SetActive(currentSelection == 0);
        if (image2 != null) image2.SetActive(currentSelection == 1);
    }

    private IEnumerator BlinkSelectedImage()
    {
        isBlinking = true;

        GameObject selectedImage = null;
        string targetSceneName = "";

        if (currentSelection == 0)
        {
            selectedImage = Selectimage1;
            targetSceneName = sceneForSelection1;
        }
        else
        {
            selectedImage = Selectimage2;
            targetSceneName = sceneForSelection2;
        }

        if (selectedImage == null)
        {
            isBlinking = false;
            yield break;
        }

        // 3回繰り返す
        for (int i = 0; i < blinkCount; i++)
        {
            selectedImage.SetActive(false);
            yield return new WaitForSeconds(blinkOffTime);

            selectedImage.SetActive(true);
            yield return new WaitForSeconds(blinkOnTime);
        }

        selectedImage.SetActive(true);

        // ★変更：点滅終了後、フェードアウト処理を挟んでからシーン遷移する
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            if (fadeImage != null)
            {
                // フェードアウトコルーチンの終了を待つ
                yield return StartCoroutine(FadeOutRoutine());
            }
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("遷移先のシーン名が設定されていません。");
            isBlinking = false;
        }
    }

    // ★追加：徐々に画面を暗くするコルーチン
    private IEnumerator FadeOutRoutine()
    {
        Debug.Log("開始");
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // アルファ値を0から1へ徐々に変化させる
            c.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        // 確実に不透明度を1にする
        c.a = 1f;
        fadeImage.color = c;
        Debug.Log("終了");
    }
    private IEnumerator FadeInRoutine()
    {
        Debug.Log("フェードイン開始");
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        float elapsedTime = 0f;

        // まず確実に真っ暗（アルファ値1）にする
        c.a = 1f;
        fadeImage.color = c;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            // アルファ値を1から0へ徐々に変化させる
            c.a = Mathf.Clamp01(1f - (elapsedTime / fadeInDuration));
            fadeImage.color = c;
            yield return null;
        }

        // 完全に透明にする
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false); // 完全に消えたら非アクティブにする
        Debug.Log("フェードイン終了");
    }
}
