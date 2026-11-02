using TMPro;
using UnityEngine;

public class TechiqueSelector : MonoBehaviour
{
    [SerializeField] private TMP_Text selectedText;
    [SerializeField] private AttackTimingController attackTiming;

    public void SelectNormal()
    {
        SelectTechnique("通常デュクシ");
    }

    public void SelectFeint()
    {
        SelectTechnique("フェイントデュクシ");
    }

    public void SelectCombo()
    {
        SelectTechnique("連続デュクシ");
    }

    public void SelectHeavy()
    {
        SelectTechnique("激重デュクシ");
    }

    private void SelectTechnique(string techniqueName)
    {
        selectedText.text = "選択した技：" + techniqueName;

        // 選んだ技の攻撃タイミングを開始する
        attackTiming.StartAttackTiming(techniqueName);
    }
}
