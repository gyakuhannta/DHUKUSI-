using System;
using UnityEngine;

/// <summary>
/// コスト管理（攻撃側のデュクシのコスト）
///
/// 【やること】
///  ・1P と 2P それぞれの残りコストを持つ（最大 10）
///  ・デュクシを選んだとき、コストが足りるかチェックして、足りれば減らす
///  ・足りなければ選べない（選択フェーズのまま）
///  ・セットが変わったらコストを満タンに戻す
///
/// 【使い方】
///  1. DhukusiInput と同じ GameObject か、別の空の GameObject にアタッチ
///  2. コストが減るのは「ボタンを押したプレイヤー」
///     攻撃側だけ押せるようにするには DhukusiInput 側で
///       DhukusiInput.Instance.ActivePlayer = 1;   // 0 = 1P, 1 = 2P
///  3. セットが変わったら
///       CostManager.Instance.ResetAll();
///
///  4. 攻撃ターンの最初に（コストが0なら10に戻る：仕様書「コスト仕様」）
///       CostManager.Instance.RecoverIfEmpty(攻撃側);
///  5. 攻守決めフェーズのコストを払うとき（疾風3・鉄壁4・剛腕5）
///       CostManager.Instance.Pay(プレイヤー, コスト);
/// </summary>
public class CostManager : MonoBehaviour
{
    public static CostManager Instance { get; private set; }

    [Header("コストの最大値")]
    [SerializeField] private int maxCost = 10;

    [Header("各デュクシのコスト（仕様書「コスト仕様」の値）")]
    [SerializeField] private int normalCost = 1;   // 通常デュクシ
    [SerializeField] private int feintCost = 5;    // フェイントデュクシ
    [SerializeField] private int renzokuCost = 3;  // 連続デュクシ
    [SerializeField] private int gekiomoCost = 7;  // 激重デュクシ

    [Header("状態（確認用）")]
    [SerializeField] private int[] currentCost = new int[2];   // [0] = 1P, [1] = 2P

    /// <summary>コストが変わった（プレイヤー番号, 残りコスト, 最大コスト）UI表示用</summary>
    public event Action<int, int, int> OnCostChanged;

    /// <summary>コストが足りなくて選べなかった（選ぼうとしたデュクシ）</summary>
    public event Action<DhukusiInput.DhukusiType> OnNotEnoughCost;

    /// <summary>コストを払ってデュクシが決まった（決まったデュクシ）</summary>
    public event Action<DhukusiInput.DhukusiType> OnDhukusiDecided;

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

    // ===================== コストの処理 =====================

    /// <summary>デュクシのコストを返す</summary>
    public int GetCost(DhukusiInput.DhukusiType type)
    {
        switch (type)
        {
            case DhukusiInput.DhukusiType.Normal:  return normalCost;
            case DhukusiInput.DhukusiType.Feint:   return feintCost;
            case DhukusiInput.DhukusiType.Renzoku: return renzokuCost;
            default:                               return gekiomoCost;
        }
    }

    /// <summary>そのプレイヤーの残りコスト</summary>
    public int GetCurrentCost(int player) => currentCost[player];

    /// <summary>コストが足りるか</summary>
    public bool CanUse(int player, DhukusiInput.DhukusiType type)
    {
        return currentCost[player] >= GetCost(type);
    }

    /// <summary>デュクシが選ばれたとき：足りればコストを払う、足りなければ選べない</summary>
    private void TryUse(DhukusiInput.DhukusiType type)
    {
        int cost = GetCost(type);
        int attacker = DhukusiInput.Instance.LastPlayer;   // 押したプレイヤー

        if (!CanUse(attacker, type))
        {
            Debug.Log($"[コスト] {attacker + 1}P：{type} はコスト{cost}必要、残り{currentCost[attacker]}なので選べない");
            OnNotEnoughCost?.Invoke(type);
            return;   // 選択フェーズのまま。別のデュクシを選び直す
        }

        currentCost[attacker] -= cost;
        Debug.Log($"[コスト] {attacker + 1}P：{type} を使用（-{cost}）残り {currentCost[attacker]}/{maxCost}");
        OnCostChanged?.Invoke(attacker, currentCost[attacker], maxCost);
        OnDhukusiDecided?.Invoke(type);
    }

    /// <summary>コストを払う（攻守決めフェーズ用）。足りなければ false</summary>
    public bool Pay(int player, int amount)
    {
        if (currentCost[player] < amount) return false;
        currentCost[player] -= amount;
        Debug.Log($"[コスト] {player + 1}P：-{amount} 残り {currentCost[player]}/{maxCost}");
        OnCostChanged?.Invoke(player, currentCost[player], maxCost);
        return true;
    }

    /// <summary>コストを使い果たしていたら最大値に戻す（攻撃ターンの最初に呼ぶ）</summary>
    public void RecoverIfEmpty(int player)
    {
        if (currentCost[player] > 0) return;
        currentCost[player] = maxCost;
        Debug.Log($"[コスト] {player + 1}P：コストを使い果たしたので全回復 {maxCost}/{maxCost}");
        OnCostChanged?.Invoke(player, currentCost[player], maxCost);
    }

    /// <summary>1P・2P 両方のコストを満タンに戻す（セットが変わったとき）</summary>
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
