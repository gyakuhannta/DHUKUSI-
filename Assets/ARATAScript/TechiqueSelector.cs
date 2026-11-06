using TMPro;
using UnityEngine;

public class TechiqueSelector : MonoBehaviour
{
    [SerializeField] private TMP_Text selectedText;
    [SerializeField] private AttackTimingController attackTiming;

    // 防御用のTimingJudgeを追加
    [SerializeField] private TimingJudge timingJudge;

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

        // 攻撃のタイミングを開始
        attackTiming.StartAttackTiming(techniqueName);
    }

    // 防御を開始する
    public void StartDefense()
    {
        timingJudge.StartDefense();
    }
}
