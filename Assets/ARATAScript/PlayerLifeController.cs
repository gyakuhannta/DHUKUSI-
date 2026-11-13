using UnityEngine;
using TMPro;
public class PlayerLifeController : MonoBehaviour
{
    [Header("ライフ設定")]
    [SerializeField] private int maxLife = 2;

    [Header("画面表示")]
    [SerializeField] private TMP_Text lifeText;

    private int currentLife;

    private void Start()
    {
        currentLife = maxLife;
        UpdateLifeText();
    }

    // ダメージを受ける
    public void TakeDamage()
    {
        currentLife--;

        if (currentLife < 0)
        {
            currentLife = 0;
        }

        UpdateLifeText();

        Debug.Log("プレイヤーの残りライフ：" + currentLife);

        if (currentLife <= 0)
        {
            Debug.Log("プレイヤーの敗北！");
        }
    }

    // ライフ表示
    private void UpdateLifeText()
    {
        if (lifeText != null)
        {
            lifeText.text = "Player Life：" + currentLife;
        }
    }
}
