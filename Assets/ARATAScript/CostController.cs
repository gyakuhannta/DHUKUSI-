using UnityEngine;
using TMPro;
public class CostController : MonoBehaviour
{
    [Header("コスト設定")]
    [SerializeField] private int maxCost = 100;

    [Header("画面表示")]
    [SerializeField] private TMP_Text costText;

    private int currentCost;

    private void Start()
    {
        currentCost = maxCost;
        UpdateCostText();
    }

    // コストを消費する
    public bool TryConsumeCost(int amount)
    {
        // コストが足りない場合
        if (currentCost < amount)
        {
            Debug.Log("コストが足りません！");
            return false;
        }

        // コストを減らす
        currentCost -= amount;

        UpdateCostText();

        Debug.Log("残りコスト：" + currentCost);

        return true;
    }

    // コスト表示を更新する
    private void UpdateCostText()
    {
        if (costText != null)
        {
            costText.text = "コスト：" + currentCost
                + " / " + maxCost;
        }
    }
}
