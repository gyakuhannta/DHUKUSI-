using UnityEngine;
using TMPro;
public class ButtonMessageContoroller : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    public void ShowAttackMessage()
    {
        messageText.text = "UŒ‚I";
    }
    public void ShowDefenseMessage()
    {
        messageText.text = "–hŒäI";
    }
    public void ShowHealMessage()
    {
        messageText.text = "‰ñ•œI";
    }
    public void ShowSpecialMessage()
    {
        messageText.text = "“Áê‹ZI";
    }
}
