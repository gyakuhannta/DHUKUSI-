using UnityEngine;

public class EnemyAttackController : MonoBehaviour
{
    [SerializeField] private TimingJudge timingJudge;
  

    // 敵が使用する4種類の技
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
        // 0～3の中からランダムに選ぶ
        int randomIndex = Random.Range(0, 4);

        // 選ばれた技の名前を取得
        string techniqueName = enemyTechniques[randomIndex];

        Debug.Log("敵の技：" + techniqueName);
      
        // TimingJudgeに正解ゾーンと技名を渡す
        timingJudge.StartDefense(randomIndex, techniqueName);

    }
}
