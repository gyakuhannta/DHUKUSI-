using TMPro;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private int enemyLife = 2;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text resultText;

    private void Start()
    {
        UpdateLifeText();
    }

    // 敵がダメージを受ける
    public void TakeDamage()
    {
        Debug.Log("TakeDamageが呼ばれました！");

        if (enemyLife <= 0)
        {
            return;
        }

        enemyLife--;

        UpdateLifeText();

        Debug.Log("現在の敵ライフ：" + enemyLife);

        if (enemyLife <= 0)
        {
            resultText.text = "勝利！";
        }
    }

    // ライフの表示を更新
    private void UpdateLifeText()
    {
        lifeText.text = "敵のライフ：" + enemyLife;
    }
}
