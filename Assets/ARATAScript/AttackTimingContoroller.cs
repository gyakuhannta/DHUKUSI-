using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class AttackTimingContoroller : MonoBehaviour
{
    [Header("画面の設定")]
    [SerializeField] private GameObject timingBar;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private TMP_Text resultText;
    [Header("カーソルの設定")]
    [SerializeField] private float speed = 300f;
    [SerializeField] private float leftLimit = -290f;
    [SerializeField] private float rightLimit = 290f;
    private bool isAttacking = false;
    private float direction = 1f;
    private string currentTechnique = "";
    private void Start()
    {
        // 最初はタイミングバーを非表示
        timingBar.SetActive(false);
        Debug.Log("AttackTimingController 起動");
    }
    private void Update()
    {
        // 攻撃中でなければ何もしない
        if (!isAttacking)
        {
            return;
        }
        // カーソルを移動させる
        Vector2 position = cursor.anchoredPosition;
        position.x += speed * direction * Time.deltaTime;
        // 右端に到達したら左へ
        if (position.x >= rightLimit)
        {
            position.x = rightLimit;
            direction = -1f;
        }
        // 左端に到達したら右へ
        else if (position.x <= leftLimit)
        {
            position.x = leftLimit;
            direction = 1f;
        }
        cursor.anchoredPosition = position;
        // 左クリックされたら判定
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            JudgeAttack();
        }
    }
    // 攻撃開始
    public void StartAttackTiming(string techniqueName)
    {
        if (isAttacking)
        {
            return;
        }
        Debug.Log("攻撃開始：" + techniqueName);
        currentTechnique = techniqueName;
        direction = 1f;
        // カーソルを左端に戻す
        Vector2 position = cursor.anchoredPosition;
        position.x = leftLimit;
        cursor.anchoredPosition = position;
        // タイミングバーを表示
        timingBar.SetActive(true);
        // 攻撃状態にする
        isAttacking = true;
        resultText.text = currentTechnique + "：タイミングを決めてください";
    }
    // 攻撃判定
    private void JudgeAttack()
    {
        isAttacking = false;
        float x = cursor.anchoredPosition.x;
        if (x >= -40f && x <= 40f)
        {
            resultText.text = currentTechnique + "：攻撃成功！";
        }
        else
        {
            resultText.text = currentTechnique + "：攻撃失敗...";
        }
        timingBar.SetActive(false);
        Debug.Log("攻撃判定終了");
    }
}
