using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class DhukusiInput : MonoBehaviour
{
    public static DhukusiInput Instance { get; private set; }

    //デュクシの種類
    public enum DhukusiType
    {
        Normal,   // 通常デュクシ（青）
        Feint,    // フェイントデュクシ（緑）
        Renzoku,  // 連続デュクシ（黄）
        Gekiomo   // 激重デュクシ（赤）
    }

    /// <summary>今どの入力を受け付けるか</summary>
    public enum InputPhase
    {
        None,    // 何も受け付けない（演出中・判定表示中など）
        Select,  // デュクシ選択フェーズ
        Timing   // デュクシ繰り出しフェーズ
    }

    [Header("状態")]
    [SerializeField] private InputPhase phase = InputPhase.None;
    public InputPhase Phase
    {
        get => phase;
        set => phase = value;
    }

    [Header("PvP用：どのプレイヤーの入力か")]
    [Tooltip("-1 = 全部のコントローラーを受け付ける / 0 = 1台目だけ / 1 = 2台目だけ")]
    [SerializeField] private int gamepadIndex = -1;

    //デュクシが選ばれた
    public event Action<DhukusiType> OnSelect;

    //タイミングボタンが押された
    public event Action OnTiming;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (phase == InputPhase.None) return;

        foreach (Gamepad pad in GetGamepads())
        {
            if (phase == InputPhase.Select)
            {
                if (pad.buttonNorth.wasPressedThisFrame) { Select(DhukusiType.Normal); return; }
                if (pad.buttonWest.wasPressedThisFrame) { Select(DhukusiType.Feint); return; }
                if (pad.buttonEast.wasPressedThisFrame) { Select(DhukusiType.Renzoku); return; }
                if (pad.buttonSouth.wasPressedThisFrame) { Select(DhukusiType.Gekiomo); return; }
            }
            else if (phase == InputPhase.Timing)
            {
                if (pad.buttonEast.wasPressedThisFrame)
                {
                    Debug.Log($"[入力] タイミング押下 ({Time.time:F2}秒)");
                    OnTiming?.Invoke();
                    return;
                }
            }
        }
    }

    private void Select(DhukusiType type)
    {
        Debug.Log($"[入力] デュクシ選択: {type}");
        OnSelect?.Invoke(type);
    }

    //受け付けるコントローラーの一覧
    private Gamepad[] GetGamepads()
    {
        var all = Gamepad.all;
        if (gamepadIndex < 0)
        {
            var list = new Gamepad[all.Count];
            for (int i = 0; i < all.Count; i++) list[i] = all[i];
            return list;
        }
        if (gamepadIndex < all.Count) return new[] { all[gamepadIndex] };
        return Array.Empty<Gamepad>();
    }
}

