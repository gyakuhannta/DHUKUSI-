using UnityEngine;
using TMPro;
public class PlayerLifeController : MonoBehaviour
{
    [SerializeField] private int playerLife = 2;
    [SerializeField] private TMP_Text lifeText;

    private void Start()
    {
        UpdateLifeText();
    }

    public void TakeDamage()
    {
        if (playerLife <= 0)
        {
            return;
        }

        playerLife--;

        UpdateLifeText();

        if (playerLife <= 0)
        {
            lifeText.text = "敗北！";
        }
    }

    private void UpdateLifeText()
    {
        lifeText.text = "プレイヤーのライフ：" + playerLife;
    }
}
