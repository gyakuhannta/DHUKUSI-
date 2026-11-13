using UnityEngine;

public class EnemyAttackController : MonoBehaviour
{
    [SerializeField] private TimingJudge timingJudge;

    private readonly string[] enemyTechniques =
    {
        "通常デュクシ",
        "フェイントデュクシ",
        "連続デュクシ",
        "激重デュクシ"
    };

    // 敵の攻撃を開始する
    public void StartEnemyAttack()
    {
        int randomIndex = Random.Range(0, 4);

        string techniqueName = enemyTechniques[randomIndex];

        Debug.Log("敵の技：" + techniqueName);

        timingJudge.StartDefense(randomIndex, techniqueName);
    }
}
