using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class DhukusiInput : MonoBehaviour
{
    public static DhukusiInput Instance { get; private set; }

    //デュクシの種類
    public enum DhukusiType
    {
        Normal,   // 通常デュクシ
        Feint,    // フェイントデュクシ
        Renzoku,  // 連続デュクシ
        Gekiomo   // 激重デュクシ
    }

    public enum InputPhase
    {
        None,    // 何も受け付けない（演出中・判定表示中など）
        Select,  // デュクシ選択フェーズ
        Timing   // デュクシ繰り出しフェーズ
    }

    // 【追加】ボタンの位置
    public enum PadButton { North, West, East, South }

    [Header("状態")]
    [SerializeField] private InputPhase phase = InputPhase.None;
    public InputPhase Phase
    {
        get => phase;
        set
        {
            phase = value;
            // フェーズが変わったら長押しをリセット
            ResetHold(); 
        }
    }

    // 長押しキャンセルの秒数
    [Header("長押しキャンセル")]
    [Tooltip("この秒数以上 長押しするとキャンセル")]
    [SerializeField] private float cancelHoldSeconds = 2f;

    [Header("PvP用：どのプレイヤーの入力か")]
    [Tooltip("-1 = 全部のコントローラーを受け付ける / 0 = 1台目だけ / 1 = 2台目だけ")]
    [SerializeField] private int gamepadIndex = -1;

    //デュクシが選ばれた
    public event Action<DhukusiType> OnSelect;

    //タイミングボタンが押された
    public event Action OnTiming;

    // 長押しでキャンセルされた
    public event Action OnCancel;

    // ボタンを離した
    public event Action<PadButton, float> OnReleased;

    // 長押しの状態
    private PadButton? heldButton = null;   // 押してなければ null
    private float holdStartTime;            // 押し始めた時刻
    private bool cancelFired;               // この長押しでもうキャンセルしたか

    // 今のボタンを何秒押しているか
    public float HoldTime => heldButton.HasValue ? Time.time - holdStartTime : 0f;

    //キャンセルまでの進み具合 0〜1
    public float CancelProgress => Mathf.Clamp01(HoldTime / cancelHoldSeconds);

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
            CheckButton(pad.buttonNorth, PadButton.North);
            CheckButton(pad.buttonWest, PadButton.West);
            CheckButton(pad.buttonEast, PadButton.East);
            CheckButton(pad.buttonSouth, PadButton.South);
        }

        // 長押しが一定秒数を超えたらキャンセル
        if (phase == InputPhase.Select && heldButton.HasValue && !cancelFired
            && HoldTime >= cancelHoldSeconds)
        {
            cancelFired = true;
            Debug.Log($"[入力] 長押しキャンセル（{HoldTime:F1}秒）");
            OnCancel?.Invoke();
        }
    }

    // 押した　離した
    private void CheckButton(UnityEngine.InputSystem.Controls.ButtonControl control, PadButton button)
    {
        // 押した瞬間
        if (control.wasPressedThisFrame && !heldButton.HasValue)
        {
            heldButton = button;
            holdStartTime = Time.time;
            cancelFired = false;

            // 繰り出しフェーズ
            if (phase == InputPhase.Timing && button == PadButton.East)
            {
                Debug.Log($"[入力] タイミング押下 ({Time.time:F2}秒)");
                OnTiming?.Invoke();
            }
        }

        // 離した瞬間
        if (control.wasReleasedThisFrame && heldButton == button)
        {
            float held = Time.time - holdStartTime;
            Debug.Log($"[入力] {button} を離した（{held:F1}秒 押していた）");
            OnReleased?.Invoke(button, held);

            // 選択フェーズ
            if (phase == InputPhase.Select && !cancelFired)
            {
                Select(ToDhukusi(button));
            }

            ResetHold();
        }
    }

    private void Select(DhukusiType type)
    {
        Debug.Log($"[入力] デュクシ選択: {type}");
        OnSelect?.Invoke(type);
    }

    // デュクシの種類
    private static DhukusiType ToDhukusi(PadButton b)
    {
        switch (b)
        {
            case PadButton.North: return DhukusiType.Normal;
            case PadButton.West: return DhukusiType.Feint;
            case PadButton.East: return DhukusiType.Renzoku;
            default: return DhukusiType.Gekiomo;
        }
    }

    // 状態をリセット
    private void ResetHold()
    {
        heldButton = null;
        cancelFired = false;
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
