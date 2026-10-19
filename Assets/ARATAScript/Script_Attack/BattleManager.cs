using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("ライフ")]
    [SerializeField] private int playerLife = 2;
    [SerializeField] private int enemyLife = 2;
    [Header("テキスト")]
    [SerializeField] private TMP_Text playerLifeText;
    [SerializeField] private TMP_Text enemyLifeText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text messageText;
    [Header("攻撃ボタン")]
    [SerializeField] private Button[] attackButtons;
    // 正解の攻撃番号
    private int correctAttack;
    // ゲーム終了判定
    private bool gameEnded = false;
    private void Start()
    {
        // 4つのボタンに攻撃処理を登録する
        for (int i = 0; i < attackButtons.Length; i++)
        {
            int attackNumber = i + 1;
            attackButtons[i].onClick.AddListener(
                () => Attack(attackNumber)
            );
        }
        UpdateLifeUI();
        SetNextQuestion();
    }
    // 次の正解ボタンを決める
    private void SetNextQuestion()
    {
        correctAttack = Random.Range(1, 5);
        // 動作確認用：正解を表示する
        questionText.text = "正解：Atacck" + correctAttack;
        messageText.text = "正しいボタンを押そう！";
    }
    // ボタンが押されたときの処理
    private void Attack(int attackNumber)
    {
        if (gameEnded)
        {
            return;
        }
        if (attackNumber == correctAttack)
        {
            // 正解なら敵のライフを減らす
            enemyLife--;
            messageText.text = "攻撃成功！";
        }
        else
        {
            // 不正解ならプレイヤーのライフを減らす
            playerLife--;
            messageText.text = "攻撃失敗！";
        }
        UpdateLifeUI();
        // 勝敗を確認する
        if (enemyLife <= 0)
        {
            gameEnded = true;
            messageText.text = "Win！";
            questionText.text = "Enemydown！";
            SetButtonsInteractable(false);
            return;
        }
        if (playerLife <= 0)
        {
            gameEnded = true;
            messageText.text = "Loser！";
            questionText.text = "プレイヤーが倒れた！";
            SetButtonsInteractable(false);
            return;
        }
        // 次の攻撃問題へ
        SetNextQuestion();
    }
    // ライフ表示を更新する
    private void UpdateLifeUI()
    {
        playerLifeText.text = "Player"
            + Mathf.Max(0, playerLife);
        enemyLifeText.text = "Enemy"
            + Mathf.Max(0, enemyLife);
    }
    // ゲーム終了時にボタンを無効にする
    private void SetButtonsInteractable(bool enabledState)
    {
        foreach (Button button in attackButtons)
        {
            button.interactable = enabledState;
        }
    }
}
