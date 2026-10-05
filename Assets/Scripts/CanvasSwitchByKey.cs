using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasSwitchByKey : MonoBehaviour
{
    [Header("非表示にするCanvas")]
    [SerializeField] private Canvas currentCanvas;

    [Header("表示するCanvas")]
    [SerializeField] private Canvas targetCanvas;

    [Header("切り替えキー")]
    [SerializeField] private Key switchKey = Key.Space;

    private void Start()
    {
        // 最初は切り替え先を非表示
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            SwitchCanvas();
        }
    }

    private void SwitchCanvas()
    {
        if ( currentCanvas != null)
        {
            currentCanvas.gameObject.SetActive(false);
        }

        if ( targetCanvas != null )
        {
            targetCanvas.gameObject.SetActive(true);
        }
    }
}
