using UnityEngine;
using UnityEngine.InputSystem;

public class TitleSecretCommand : MonoBehaviour
{
    [Header("非表示にするUIの親オブジェクト（Canvasなど）")]
    public GameObject titleUI;

    [Header("操作を有効にするプレイヤーのスクリプト")]
    public MonoBehaviour playerController;

    [Header("有効にするカメラのスクリプト")] // ← 追加
    public MonoBehaviour cameraController;

    private bool isSecretActivated = false;

    void Update()
    {
        if (!isSecretActivated && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ActivateSecretMode();
        }
    }

    void ActivateSecretMode()
    {
        isSecretActivated = true;

        if (titleUI != null)
        {
            titleUI.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (cameraController != null) // ← 追加
        {
            cameraController.enabled = true;
        }
    }
}
