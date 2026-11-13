using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
public class AttackTimingController : MonoBehaviour
{
    [Header("画面の設定")]
    [SerializeField] private GameObject timingBar;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private EnemyController enemy;



    [Header("カーソルの設定")]
    [SerializeField] private float speed = 300f;
    [SerializeField] private float leftLimit = -290f;
    [SerializeField] private float rightLimit = 290f;

    private bool isAttacking = false;
    private float direction = 1f;
    private string currentTechnique = "";

    private void Start()
    {
        // ゲーム開始時はバーを隠す
        timingBar.SetActive(false);
        resultText.text = "技を選んでください";
    }

    private void Update()
    {
        if (!isAttacking)
        {
            return;
        }

        // カーソルを左右に動かす
        Vector2 position = cursor.anchoredPosition;
        position.x += speed * direction * Time.deltaTime;

        if (position.x >= rightLimit)
        {
            position.x = rightLimit;
            direction = -1f;
        }
        else if (position.x <= leftLimit)
        {
            position.x = leftLimit;
            direction = 1f;
        }

        cursor.anchoredPosition = position;

        // マウスの左クリックでタイミングを決める
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            JudgeAttack();
        }
    }

    // 技を選んだときに呼び出す
    public void StartAttackTiming(string techniqueName)
    {
        if (isAttacking)
        {
            return;
        }

        currentTechnique = techniqueName;
        direction = 1f;

        Vector2 position = cursor.anchoredPosition;
        position.x = leftLimit;
        cursor.anchoredPosition = position;

        timingBar.SetActive(true);
        resultText.text = currentTechnique + "：タイミングを決めてください";

        isAttacking = true;
    }

    // クリックした位置から攻撃タイミングを判定する
    private void JudgeAttack()
    {
        isAttacking = false;

        float x = cursor.anchoredPosition.x;

        // 中央付近なら攻撃成功
        if (x >= -40f && x <= 40f)
        {
            resultText.text = currentTechnique + "：攻撃成功！";

            Debug.Log("敵にダメージを与えました");

            // 敵にダメージを与える
            enemy.TakeDamage();
        }
        else
        {
            resultText.text = currentTechnique + "：攻撃失敗...";
        }

        // 2秒後にバーを消す
        Invoke(nameof(HideTimingBar), 2f);

        // 攻撃判定が終わったら防御を開始
        Invoke(nameof(StartEnemyDefense), 2f);
    }


    private void StartEnemyDefense()
    {
        EnemyAttackController enemyAttack =
            FindFirstObjectByType<EnemyAttackController>();

        if (enemyAttack != null)
        {
            enemyAttack.StartEnemyAttack();
        }
    }
    // タイミングバーを消す
    private void HideTimingBar()
    {
        timingBar.SetActive(false);
    }
}
