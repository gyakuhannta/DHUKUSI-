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
    private RectTransform[] successZones = new RectTransform[4];

    [Header("画面に表示する文字")]
    [SerializeField] private TMP_Text actionText;

    private readonly string[] actionNames =
    {
        "通常",
        "フェイント",
        "れんぞく",
        "げきおも"
    };

    private int currentZone = -1;

    private void Start()
    {
        UpdateActionText();
    }

    private void Update()
    {
        CheckCursorZone();

        // 左クリックされた瞬間だけ true になる
        bool isClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (isClicked)
        {
            JudgeAction();
        }
    }

    // カーソルがどのゾーンにあるか判定
    private void CheckCursorZone()
    {
        currentZone = -1;

        if (cursor == null)
        {
            UpdateActionText();
            return;
        }

        float cursorX = cursor.anchoredPosition.x;

        for (int i = 0; i < successZones.Length; i++)
        {
            RectTransform zone = successZones[i];

            if (zone == null)
            {
                continue;
            }

            float zoneX = zone.anchoredPosition.x;
            float halfWidth = zone.rect.width / 2f;

            if (cursorX >= zoneX - halfWidth &&
                cursorX <= zoneX + halfWidth)
            {
                currentZone = i;
                break;
            }
        }

        UpdateActionText();
    }

    // 現在の状態に合わせて文字を表示
    private void UpdateActionText()
    {
        if (actionText == null)
        {
            return;
        }

        if (currentZone >= 0 &&
            currentZone < actionNames.Length)
        {
            actionText.text = actionNames[currentZone];
        }
        else
        {
            actionText.text = "たいき";
        }
    }

    // クリックした瞬間の判定
    private void JudgeAction()
    {
        if (currentZone == -1)
        {
            actionText.text = "ドンマイｗ";
            Debug.Log("ドンマイｗ");
            return;
        }

        ExecuteAction(currentZone);
    }

    // ゾーンに対応した行動を実行
    private void ExecuteAction(int zoneIndex)
    {
        switch (zoneIndex)
        {
            case 0:
                Attack();
                break;

            case 1:
                Dodge();
                break;

            case 2:
                Heal();
                break;

            case 3:
                SpecialAttack();
                break;
        }
    }

    private void Attack()
    {
        Debug.Log("つうじおう");
        actionText.text = "つうじおう";
    }

    private void Dodge()
    {
        Debug.Log("ふえいんと");
        actionText.text = "ふえいんと";
    }

    private void Heal()
    {
        Debug.Log("れんぞく");
        actionText.text = "れんぞく";
    }

    private void SpecialAttack()
    {
        Debug.Log("げきおも");
        actionText.text = "げきおも";
    }
}

