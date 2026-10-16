using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CanvasSwitchByKey : MonoBehaviour
{
    [Header("非表示にするCanvas")]
    [SerializeField] private Canvas currentCanvas;

    [Header("表示するCanvas")]
    [SerializeField] private Canvas targetCanvas;

    [Header("表示するcontroller")]
    [SerializeField] private GameObject controller;

    [Header("移動させるカメラ")]
    [SerializeField] private Camera targetCamera;

    [Header("カメラの移動先")]
    [SerializeField] private Transform cameraTarget;

    [Header("カメラ移動時間")]
    [SerializeField] private float cameraMoveDuration = 1f;

    [Header("--- 長押し判定設定 ---")]
    [Tooltip("入力を受け付ける最小の押し込み時間（秒）")]
    [SerializeField] private float minHoldDuration = 0.3f;

    [Tooltip("この時間以上押し続けると入力をキャンセルする（秒）")]
    [SerializeField] private float cancelHoldDuration = 3f;

    [Tooltip("長押し進捗を表示するUI（Image TypeをFilledにしたもの。空欄でも動作します）")]
    [SerializeField] private Image holdProgressImage;

    private bool isMovingCamera = false;
    private bool isSwitched = false; // 二重実行防止フラグ

    // 長押し判定用の内部変数
    private float buttonPressedTime = 0f;
    private bool isHolding = false;
    private bool isCancelled = false;

    private void Start()
    {
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(false);
        }

        if (holdProgressImage != null)
        {
            holdProgressImage.fillAmount = 0f;
        }
    }

    private void Update()
    {
        // すでに切り替え済みの場合は入力を受け付けない
        if (isSwitched) return;

        // マウスの左クリックは従来通りの単押し挙動（※もしマウスも制限したい場合はここを書き換えます）
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SwitchCanvas();
            return;
        }

        // --- ゲームパッド（Bボタン）の長押し判定処理 ---
        if (Gamepad.current != null)
        {
            // Bボタンが押された瞬間
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                isHolding = true;
                isCancelled = false;
                buttonPressedTime = 0f;
            }

            // Bボタンが押され続けている間
            if (isHolding && Gamepad.current.buttonEast.isPressed)
            {
                buttonPressedTime += Time.deltaTime;

                // ゲージUIの更新（0.5秒〜3秒の間でゲージをチャージする演出）
                if (holdProgressImage != null)
                {
                    // 0.5秒未満のときは0、それ以降3秒にかけて1.0に近づく
                    float progress = Mathf.InverseLerp(minHoldDuration, cancelHoldDuration, buttonPressedTime);
                    holdProgressImage.fillAmount = progress;
                }

                // 3秒以上押し続けたらキャンセル確定
                if (buttonPressedTime >= cancelHoldDuration && !isCancelled)
                {
                    isCancelled = true;
                    Debug.Log("3秒以上長押しされたため、入力をキャンセルしました。");
                }
            }

            // Bボタンが離された瞬間
            if (Gamepad.current.buttonEast.wasReleasedThisFrame)
            {
                if (isHolding)
                {
                    // 【修正点】0.5秒以上、かつ3秒未満で離された場合のみ実行
                    if (!isCancelled && buttonPressedTime >= minHoldDuration && buttonPressedTime < cancelHoldDuration)
                    {
                        SwitchCanvas();
                    }
                    else
                    {
                        Debug.Log($"条件外のため不発：押していた時間 {buttonPressedTime:F2} 秒");
                    }

                    // 状態をリセット
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

    private void SwitchCanvas()
    {
        isSwitched = true; // 1度実行したらロック

        if (currentCanvas != null)
        {
            currentCanvas.gameObject.SetActive(false);
        }

        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(true);
        }

        if (controller != null)
        {
            controller.SetActive(true);
        }

        if (targetCamera != null && cameraTarget != null && !isMovingCamera)
        {
            StartCoroutine(MoveCamera());
        }
    }

    private System.Collections.IEnumerator MoveCamera()
    {
        isMovingCamera = true;

        Vector3 startPosition = targetCamera.transform.position;
        Quaternion startRotation = targetCamera.transform.rotation;

        Vector3 endPosition = cameraTarget.position;
        Quaternion endRotation = cameraTarget.rotation;

        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cameraMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            targetCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            targetCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            yield return null;
        }

        targetCamera.transform.position = endPosition;
        targetCamera.transform.rotation = endRotation;

        isMovingCamera = false;
    }
}
