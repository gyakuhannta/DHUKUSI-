using UnityEngine;
using UnityEngine.InputSystem;

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

    private bool isMovingCamera = false;

    private void Start()
    {
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Gamepad.current != null &&
            Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            SwitchCanvas();
        }
    }

    private void SwitchCanvas()
    {
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

        // カメラ移動開始
        if (targetCamera != null && cameraTarget != null && !isMovingCamera)
        {
            StartCoroutine(MoveCamera());
        }
    }

    private System.Collections.IEnumerator MoveCamera()
    {
        isMovingCamera = true;

        Vector3 startPosition = targetCamera.transform.position;
        Vector3 endPosition = cameraTarget.position;

        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / cameraMoveDuration);

            // なめらかに移動
            t = Mathf.SmoothStep(0f, 1f, t);

            targetCamera.transform.position =
                Vector3.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        targetCamera.transform.position = endPosition;

        isMovingCamera = false;
    }
}
