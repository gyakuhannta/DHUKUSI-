using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;

public class TimingJudge : MonoBehaviour
{
    [Header("画面の設定")]
    [SerializeField] private GameObject timingBar;

    [Header("カーソル")]
    [SerializeField] private RectTransform cursor;

    [Header("4つの成功ゾーン")]
    [SerializeField]
    private RectTransform[] successZones =
        new RectTransform[4];

    [Header("画面に表示する文字")]
    [SerializeField] private TMP_Text actionText;

    [Header("プレイヤーのライフ")]
    [SerializeField] private PlayerLifeController player;

    private readonly string[] actionNames =
    {
        "通常",
        "フェイント",
        "れんぞく",
        "げきおも"
    };

    private int currentZone = -1;
    private int correctZone = -1;
    private string enemyTechnique = "";

    private bool isDefending = false;

    private void Start()
    {
        timingBar.SetActive(false);
        UpdateActionText();
    }

    private void Update()
    {
        if (!isDefending)
        {
            return;
        }

        CheckCursorZone();

        bool isClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (isClicked)
        {
            JudgeAction();
        }
    }

    // 防御開始
    public void StartDefense(int zone, string techniqueName)
    {
        correctZone = zone;
        enemyTechnique = techniqueName;

        isDefending = true;

        timingBar.SetActive(true);

        actionText.text = "敵の技：" + enemyTechnique;
    }

    // 防御終了
    private void EndDefense()
    {
        isDefending = false;
        timingBar.SetActive(false);
    }

    // カーソルがどのゾーンにあるか判定
    private void CheckCursorZone()
    {
        currentZone = -1;

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

    // 行動名を表示
    private void UpdateActionText()
    {
        if (actionText == null)
        {
            return;
        }

        if (currentZone >= 0)
        {
            actionText.text = actionNames[currentZone];
        }
        else
        {
            actionText.text = "たいき";
        }
    }

    // クリックした瞬間に判定
    private void JudgeAction()
    {
        if (currentZone == correctZone)
        {
            actionText.text = "防御成功！";

            Debug.Log("防御成功！");
        }
        else
        {
            actionText.text = "防御失敗！";

            Debug.Log("防御失敗！");

            player.TakeDamage();
        }

        EndDefense();
    }

    // ゾーンに対応した行動
    private void ExecuteAction(int zoneIndex)
    {
        switch (zoneIndex)
        {
            case 0:
                actionText.text = "通常防御成功！";
                break;

            case 1:
                actionText.text = "フェイント防御成功！";
                break;

            case 2:
                actionText.text = "連続防御成功！";
                break;

            case 3:
                actionText.text = "激重防御成功！";
                break;
        }

        Debug.Log(actionText.text);
    }
}

