using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;

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
            Success();
        }
        else
        {
            Failed();
        }
    }
    void Success() 
    {
         Debug.Log("¬Œ÷");

        resultText.text = "SUCCELSS";
    }
    void Failed() 
    {
        Debug.Log("Ž¸”s");

        resultText.text = "MISS";
    }
    
}

