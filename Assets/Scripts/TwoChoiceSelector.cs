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
    [SerializeField] private float minHoldDuration = 0.3f;
    [SerializeField] private float cancelHoldDuration = 3f;
    [SerializeField] private Image holdProgressImage;

    [Header("--- 連動するフェードスクリプト ---")]
    [SerializeField] private SceneFader sceneFader; // ★ここに追加

    private int currentSelection;
    private bool canMove = true;
    private bool isBlinking = false;

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
            if (canMove && moveY > 0.5f) { MoveSelection(-1); canMove = false; }
            else if (canMove && moveY < -0.5f) { MoveSelection(1); canMove = false; }

            if (Mathf.Abs(moveY) < 0.2f) canMove = true;
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
        if (holdProgressImage != null) holdProgressImage.fillAmount = 0f;
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
        GameObject selectedImage = (currentSelection == 0) ? Selectimage1 : Selectimage2;
        string targetSceneName = (currentSelection == 0) ? sceneForSelection1 : sceneForSelection2;

        if (selectedImage == null) { isBlinking = false; yield break; }

        for (int i = 0; i < blinkCount; i++)
        {
            selectedImage.SetActive(false);
            yield return new WaitForSeconds(blinkOffTime);
            selectedImage.SetActive(true);
            yield return new WaitForSeconds(blinkOnTime);
        }
        selectedImage.SetActive(true);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            // ★修正：SceneFader側のフェードアウトを待つ
            if (sceneFader != null)
            {
                yield return StartCoroutine(sceneFader.FadeOutRoutine());
            }
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            isBlinking = false;
        }
    }
}

