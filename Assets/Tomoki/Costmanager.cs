using System;
using UnityEngine;

public class CostManager : MonoBehaviour
{
    public static CostManager Instance { get; private set; }

    [Header("コストの最大値")]
    [SerializeField] private int maxCost = 10;

    [Header("各デュクシのコスト（仮の値）")]
    [SerializeField] private int normalCost = 1;   // 通常デュクシ
    [SerializeField] private int feintCost = 3;    // フェイントデュクシ
    [SerializeField] private int renzokuCost = 5;  // 連続デュクシ
    [SerializeField] private int gekiomoCost = 7;  // 激重デュクシ

    [Header("状態（確認用）")]
    [Tooltip("今の攻撃側 0 = 1P, 1 = 2P")]
    [SerializeField] private int attacker = 0;
    [SerializeField] private int[] currentCost = new int[2];  

    //コストが変わった（プレイヤー番号, 残りコスト, 最大コスト）
    public event Action<int, int, int> OnCostChanged;

    // <summary>コストが足りなくて選べなかった
    public event Action<DhukusiInput.DhukusiType> OnNotEnoughCost;

    // コストを払ってデュクシが決まった
    public event Action<DhukusiInput.DhukusiType> OnDhukusiDecided;

    public int Attacker
    {
        get => attacker;
        set => attacker = value;
    }

    public int MaxCost => maxCost;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResetAll();
    }

    void Start()
    {
        // デュクシが選ばれたらコストのチェックをする
        DhukusiInput.Instance.OnSelect += TryUse;
    }

    void OnDestroy()
    {
        if (DhukusiInput.Instance != null)
            DhukusiInput.Instance.OnSelect -= TryUse;
    }

 
    public int GetCost(DhukusiInput.DhukusiType type)
    {
        switch (type)
        {
            case DhukusiInput.DhukusiType.Normal: return normalCost;
            case DhukusiInput.DhukusiType.Feint: return feintCost;
            case DhukusiInput.DhukusiType.Renzoku: return renzokuCost;
            default: return gekiomoCost;
        }
    }

    //そのプレイヤーの残りコスト
    public int GetCurrentCost(int player) => currentCost[player];

    //コストが足りるか
    public bool CanUse(int player, DhukusiInput.DhukusiType type)
    {
        return currentCost[player] >= GetCost(type);
    }

    // 選択フェーズのまま。別のデュクシを選び直す
    private void TryUse(DhukusiInput.DhukusiType type)
    {
        int cost = GetCost(type);

        if (!CanUse(attacker, type))
        {
            Debug.Log($"[コスト] {attacker + 1}P：{type} はコスト{cost}必要、残り{currentCost[attacker]}なので選べない");
            OnNotEnoughCost?.Invoke(type);
            return;   
        }

        currentCost[attacker] -= cost;
        Debug.Log($"[コスト] {attacker + 1}P：{type} を使用（-{cost}）残り {currentCost[attacker]}/{maxCost}");
        OnCostChanged?.Invoke(attacker, currentCost[attacker], maxCost);
        OnDhukusiDecided?.Invoke(type);
    }

    //1P・2P 両方のコストを満タンに戻す
    public void ResetAll()
    {
        for (int i = 0; i < currentCost.Length; i++)
        {
            currentCost[i] = maxCost;
            OnCostChanged?.Invoke(i, currentCost[i], maxCost);
        }
        Debug.Log($"[コスト] リセット：1P {currentCost[0]}/{maxCost}  2P {currentCost[1]}/{maxCost}");
    }
}
