using UnityEngine;
using UnityEngine.InputSystem;

public class TitleSecretCommand : MonoBehaviour
{
    [Header("非表示にするUIの親オブジェクト（Canvasなど）")]
    public GameObject titleUI;

    [Header("操作を有効にするプレイヤーのスクリプト")]
    public MonoBehaviour playerController;

    [Header("有効にするカメラのスクリプト")]
    public MonoBehaviour cameraController;

    private bool isSecretActivated = false;

    void Update()
    {
        if (isSecretActivated) return;

        // キーボードの「P」が押されたか
        bool isPKeyPressed = Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;

        // Xboxコントローラーの「LT（左トリガー）」が押されたか
        bool isLTPressed = Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame;

        // どちらか一方が押されたら発動
        if (isPKeyPressed || isLTPressed)
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

        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
    }
}
