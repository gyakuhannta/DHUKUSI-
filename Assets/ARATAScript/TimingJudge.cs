using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TimingJudge : MonoBehaviour
{
    [SerializeField] private RectTransform cursor;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private TMP_Text resultText;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            judge();
        }
    }
    void judge()
    {
        float cursorX = cursor.anchoredPosition.x;

        float successX = successZone.anchoredPosition.x;

        float successWidth = successZone.rect.width / 2f;

        if (cursorX >= successX - successWidth &&
            cursorX <= successX + successWidth)
        {
            Debug.Log("¬Œ÷");
        }
        else
        {
            Debug.Log("Ž¸”s");
        }
    }
}

