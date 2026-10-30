using UnityEngine;
using TMPro;
public class TechniqueSelector : MonoBehaviour
{
    // 選んだ技を表示するテキスト
    [SerializeField] private TMP_Text selectedText;
    // 現在選択している技
    private string selectedTechnique = "";
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
    // 選択結果を反映する共通処理
    private void SelectTechnique(string techniqueName)
    {
        selectedTechnique = techniqueName;
        selectedText.text = "選択した技：" + selectedTechnique;
        Debug.Log("選択した技：" + selectedTechnique);
    }
}
