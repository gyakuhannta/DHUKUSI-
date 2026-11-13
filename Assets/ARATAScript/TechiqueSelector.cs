using TMPro;
using UnityEngine;

public class TechiqueSelector : MonoBehaviour
{
    [Header("画面表示")]
    [SerializeField] private TMP_Text selectedText;

    [Header("攻撃処理")]
    [SerializeField] private AttackTimingController attackTiming;

    [Header("コスト管理")]
    [SerializeField] private CostController costController;

    // 通常デュクシ
    public void SelectNormal()
    {
        SelectTechnique("通常デュクシ", 10);
    }

    // フェイントデュクシ
    public void SelectFeint()
    {
        SelectTechnique("フェイントデュクシ", 15);
    }

    // 連続デュクシ
    public void SelectCombo()
    {
        SelectTechnique("連続デュクシ", 20);
    }

    // 激重デュクシ
    public void SelectHeavy()
    {
        SelectTechnique("激重デュクシ", 30);
    }

    // 技を選択する共通処理
    private void SelectTechnique(string techniqueName, int cost)
    {
        // すでに攻撃中なら選択できない
        if (!attackTiming.CanStartAttackTiming())
        {
            return;
        }

        // コストを消費できるか確認
        if (!costController.TryConsumeCost(cost))
        {
            selectedText.text = "コスト不足！";
            return;
        }

        selectedText.text =
            "選択した技：" + techniqueName;

        // 攻撃タイミングを開始
        attackTiming.StartAttackTiming(techniqueName);
    }
}
