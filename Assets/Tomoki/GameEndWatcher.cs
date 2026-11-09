using System.Reflection;
using UnityEngine;

/// <summary>
/// 勝ち負けが決まったら、勝敗判定シーン（ResultScene）へ移る
/// プレイヤーと敵のライフを見て、どちらかが 0 になったら少し待ってから移る
/// </summary>
public class GameEndWatcher : MonoBehaviour
{
    [Header("ライフを見るオブジェクト")]
    [SerializeField] private PlayerLifeController player;
    [SerializeField] private EnemyController enemy;

    [Header("勝敗が決まってから画面が移るまでの秒数")]
    [SerializeField] private float waitSeconds = 1.5f;

    private FieldInfo playerLifeField;
    private FieldInfo enemyLifeField;
    private bool finished;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerLifeController>();
        if (enemy == null) enemy = FindFirstObjectByType<EnemyController>();

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        playerLifeField = typeof(PlayerLifeController).GetField("playerLife", flags);
        enemyLifeField = typeof(EnemyController).GetField("enemyLife", flags);

        if (player == null || enemy == null || playerLifeField == null || enemyLifeField == null)
        {
            Debug.LogWarning("[勝敗] プレイヤーか敵のライフが見つかりません");
            enabled = false;
        }
    }

    void Update()
    {
        if (finished) return;

        int playerLife = (int)playerLifeField.GetValue(player);
        int enemyLife = (int)enemyLifeField.GetValue(enemy);

        if (enemyLife <= 0) Finish(true);
        else if (playerLife <= 0) Finish(false);
    }

    private void Finish(bool playerWon)
    {
        finished = true;
        Invoke(playerWon ? nameof(GoWin) : nameof(GoLose), waitSeconds);
    }

    private void GoWin() => ResultView.ShowPvE(true);
    private void GoLose() => ResultView.ShowPvE(false);
}
