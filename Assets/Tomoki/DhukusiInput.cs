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

    //ボタンの位置
    public enum PadButton { North, West, East, South }

    [Header("状態")]
    [SerializeField] private InputPhase phase = InputPhase.None;
    public InputPhase Phase
    {
        get => phase;
        set
        {
            phase = value;
            ResetHold();   //フェーズが変わったら長押しをリセット
        }
    }

    //長押しキャンセルの秒数
    [Header("長押しキャンセル")]
    [Tooltip("この秒数以上 長押しするとキャンセル")]
    [SerializeField] private float cancelHoldSeconds = 2f;

    // 【変更】gamepadIndex → activePlayer
    [Header("2人対戦：どのプレイヤーの入力を受け付けるか")]
    [Tooltip("-1 = 1P・2P どちらでも反応 / 0 = 1Pだけ / 1 = 2Pだけ\n1台目に挿したのが1P、2台目が2P")]
    [SerializeField] private int activePlayer = -1;
    public int ActivePlayer
    {
        get => activePlayer;
        set
        {
            activePlayer = value;
            ResetHold();   //プレイヤーが変わったら長押しをリセット
        }
    }

    // 【追加】最後にボタンを押したプレイヤー（0 = 1P, 1 = 2P）
    public int LastPlayer { get; private set; } = 0;

    //デュクシが選ばれた
    public event Action<DhukusiType> OnSelect;

    //タイミングボタンが押された
    public event Action OnTiming;

    //長押しでキャンセルされた
    public event Action OnCancel;

    //ボタンを離した（どのボタンを何秒押していたか）
    public event Action<PadButton, float> OnReleased;

    //長押しの状態
    private PadButton? heldButton = null;   // 今押しているボタン（押してなければ null）
    private int heldPlayer = -1;            // 【追加】そのボタンを押しているプレイヤー
    private float holdStartTime;            // 押し始めた時刻
    private bool cancelFired;               // この長押しでもうキャンセルしたか

    //今のボタンを何秒押しているか（押してなければ 0）
    public float HoldTime => heldButton.HasValue ? Time.time - holdStartTime : 0f;

    //今押しているデュクシの番号（0=通常 1=フェイント 2=連続 3=激重、押してなければ -1）UI用
    public int HeldDhukusi => heldButton.HasValue ? (int)ToDhukusi(heldButton.Value) : -1;

    //キャンセルまでの進み具合 0〜1（長押しゲージ用）
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

    // 【追加】起動時に、つながっているコントローラーを表示
    void Start()
    {
        LogGamepads();
    }

    // 【追加】コントローラーを抜き挿ししたら表示
    void OnEnable()  { InputSystem.onDeviceChange += OnDeviceChange; }
    void OnDisable() { InputSystem.onDeviceChange -= OnDeviceChange; }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Gamepad)) return;

        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            Debug.Log($"[入力] コントローラー接続: {device.displayName}");
        else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            Debug.Log($"[入力] コントローラー切断: {device.displayName}");
        else
            return;

        ResetHold();
        LogGamepads();
    }

    private void LogGamepads()
    {
        var all = Gamepad.all;
        Debug.Log($"[入力] コントローラー {all.Count} 台");
        for (int i = 0; i < all.Count && i < 2; i++)
        {
            Debug.Log($"[入力]   {i + 1}P = {all[i].displayName}");
        }
        if (all.Count < 2) Debug.Log("[入力]   ※ 2P のコントローラーがまだありません");
    }

    void Update()
    {
        if (phase == InputPhase.None) return;

        var all = Gamepad.all;

        // 【追加】コントローラーが1台だけのときは、その1台で今のプレイヤーを操作する（テスト用）
        if (all.Count == 1 && activePlayer >= 0)
        {
            CheckPad(all[0], activePlayer);
        }
        else
        {
            //1P・2P それぞれのコントローラーを見る
            for (int player = 0; player < all.Count && player < 2; player++)
            {
                // 受け付けないプレイヤーは飛ばす
                if (activePlayer >= 0 && player != activePlayer) continue;
                CheckPad(all[player], player);
            }
        }

        //長押しが一定秒数を超えたらキャンセル（選択フェーズだけ）
        if (phase == InputPhase.Select && heldButton.HasValue && !cancelFired
            && HoldTime >= cancelHoldSeconds)
        {
            cancelFired = true;
            Debug.Log($"[入力] {heldPlayer + 1}P 長押しキャンセル（{HoldTime:F2}秒）");
            OnCancel?.Invoke();
        }
    }

    //1台のコントローラーの4つのボタンを調べる
    private void CheckPad(Gamepad pad, int player)
    {
        CheckButton(pad.buttonNorth, PadButton.North, player);
        CheckButton(pad.buttonWest, PadButton.West, player);
        CheckButton(pad.buttonEast, PadButton.East, player);
        CheckButton(pad.buttonSouth, PadButton.South, player);
    }

    //1つのボタンの「押した」「離した」を調べる
    private void CheckButton(UnityEngine.InputSystem.Controls.ButtonControl control, PadButton button, int player)
    {
        // 押した瞬間
        if (control.wasPressedThisFrame && !heldButton.HasValue)
        {
            heldButton = button;
            heldPlayer = player;
            holdStartTime = Time.time;
            cancelFired = false;
            LastPlayer = player;

            // 繰り出しフェーズは「押した瞬間」で決定
            if (phase == InputPhase.Timing && button == PadButton.East)
            {
                Debug.Log($"[入力] {player + 1}P タイミング押下 ({Time.time:F2}秒)");
                OnTiming?.Invoke();
            }
        }

        // 離した瞬間（押したのと同じプレイヤーの同じボタンだけ）
        if (control.wasReleasedThisFrame && heldButton == button && heldPlayer == player)
        {
            float held = Time.time - holdStartTime;
            Debug.Log($"[入力] {player + 1}P {button} を離した（{held:F2}秒 押していた）");
            OnReleased?.Invoke(button, held);

            // 選択フェーズ：キャンセルしてなければ「離したとき」に選択決定
            if (phase == InputPhase.Select && !cancelFired)
            {
                Select(ToDhukusi(button));
            }

            ResetHold();
        }
    }

    private void Select(DhukusiType type)
    {
        Debug.Log($"[入力] {LastPlayer + 1}P デュクシ選択: {type}");
        OnSelect?.Invoke(type);
    }

    //ボタンの位置 → デュクシの種類
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

    //長押し状態をリセット
    private void ResetHold()
    {
        heldButton = null;
        heldPlayer = -1;
        cancelFired = false;
    }
}
