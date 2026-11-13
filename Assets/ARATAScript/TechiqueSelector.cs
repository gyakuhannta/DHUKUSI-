using TMPro;
using UnityEngine;

public class TechiqueSelector : MonoBehaviour
{
    [SerializeField] private TMP_Text selectedText;
    [SerializeField] private AttackTimingController attackTiming;

    // 通常デュクシ
    public void SelectNormal()
    {
        SelectTechnique("通常デュクシ");
    }

    // フェイントデュクシ
    public void SelectFeint()
    {
        SelectTechnique("フェイントデュクシ");
    }

    // 連続デュクシ
    public void SelectCombo()
    {
        SelectTechnique("連続デュクシ");
    }

    // 激重デュクシ
    public void SelectHeavy()
    {
        SelectTechnique("激重デュクシ");
    }

    // 技を選択する共通処理
    private void SelectTechnique(string techniqueName)
    {
        selectedText.text = "選択した技：" + techniqueName;

        // 攻撃のタイミングを開始
        attackTiming.StartAttackTiming(techniqueName);
    }
}
