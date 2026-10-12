using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;

public class TimingJudge : MonoBehaviour
{
    [Header("カーソル")]
    [SerializeField] private RectTransform cursor;

    [Header("4つの成功ゾーン")]
    [SerializeField]
    private RectTransform[] successZones =
        new RectTransform[4];

    [Header("画面に表示する文字")]
    [SerializeField] private TMP_Text actionText;

    // 現在カーソルが触れているゾーン
    private int currentZone = -1;

    void Start()
    {
        actionText.text = "タイミングを合わせよう！";
    }

    void Update()
    {
        // カーソルがゾーンに触れているか確認
        CheckCursorZone();

        // 左クリックで行動を実行
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentZone != -1)
            {
                ExecuteAction(currentZone + 1);
            }
            else
            {
                actionText.text = "失敗！";
                Debug.Log("失敗！");
            }
        }
    }

    // カーソルがどのゾーンに触れているか確認
    void CheckCursorZone()
    {
        float cursorX = cursor.anchoredPosition.x;
        int detectedZone = -1;

        for (int i = 0; i < successZones.Length; i++)
        {
            RectTransform zone = successZones[i];

            if (zone == null)
                continue;

            float zoneX = zone.anchoredPosition.x;
            float halfWidth = zone.rect.width / 2f;

            if (cursorX >= zoneX - halfWidth &&
                cursorX <= zoneX + halfWidth)
            {
                detectedZone = i;
                break;
            }
        }

        // ゾーンが変わったときだけ表示を更新
        if (detectedZone != currentZone)
        {
            currentZone = detectedZone;

            switch (currentZone)
            {
                case 0:
                    actionText.text = "攻撃ゾーン！";
                    break;

                case 1:
                    actionText.text = "回避ゾーン！";
                    break;

                case 2:
                    actionText.text = "回復ゾーン！";
                    break;

                case 3:
                    actionText.text = "特殊技ゾーン！";
                    break;

                default:
                    actionText.text = "タイミングを合わせよう！";
                    break;
            }
        }
    }

    // ゾーンごとの行動
    void ExecuteAction(int zoneNumber)
    {
        switch (zoneNumber)
        {
            case 1:
                Attack();
                break;

            case 2:
                Dodge();
                break;

            case 3:
                Heal();
                break;

            case 4:
                SpecialAttack();
                break;
        }
    }

    // ゾーン1：攻撃
    void Attack()
    {
        Debug.Log("a");
        actionText.text = "a";
    }

    // ゾーン2：回避
    void Dodge()
    {
        Debug.Log("i");
        actionText.text = "i";
    }

    // ゾーン3：回復
    void Heal()
    {
        Debug.Log("u");
        actionText.text = "u";
    }

    // ゾーン4：特殊技
    void SpecialAttack()
    {
        Debug.Log("e");
        actionText.text = "e";
    }

}

