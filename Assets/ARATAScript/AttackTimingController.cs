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
    [SerializeField] private EnemyAttackController enemyAttack;

    [Header("カーソルの設定")]
    [SerializeField] private float speed = 300f;
    [SerializeField] private float leftLimit = -290f;
    [SerializeField] private float rightLimit = 290f;

    private bool isAttacking = false;
    private float direction = 1f;
    private string currentTechnique = "";

    private void Start()
    {
        timingBar.SetActive(false);
        resultText.text = "技を選んでください";
    }

    private void Update()
    {
        if (!isAttacking)
        {
            return;
        }

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

        // マウス左クリック
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            JudgeAttack();
        }
    }

    // 攻撃タイミング開始
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

    // 攻撃判定
    private void JudgeAttack()
    {
        isAttacking = false;

        float x = cursor.anchoredPosition.x;

        if (x >= -40f && x <= 40f)
        {
            resultText.text = currentTechnique + "：攻撃成功！";

            Debug.Log("敵にダメージを与えました");

            enemy.TakeDamage();
        }
        else
        {
            resultText.text = currentTechnique + "：攻撃失敗...";
        }

        Invoke(nameof(HideTimingBar), 2f);
        Invoke(nameof(StartEnemyDefense), 2f);
    }

    // 敵の防御・攻撃開始
    private void StartEnemyDefense()
    {
        if (enemyAttack != null)
        {
            enemyAttack.StartEnemyAttack();
        }
        else
        {
            Debug.LogError("EnemyAttackControllerが登録されていません！");
        }
    }

    private void HideTimingBar()
    {
        timingBar.SetActive(false);
    }

    public bool CanStartAttackTiming()
    {
        return !isAttacking;
    }

}
