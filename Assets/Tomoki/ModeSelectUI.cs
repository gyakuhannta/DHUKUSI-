using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// モード選択画面（仕様書「モード選択画面」シート）
///
///  ・上下入力で Choose_finger1 と Choose_finger2 の表示／非表示を切り替える
///  ・A ボタンで決定
///  ・決定したら、選んだ画像を 0.3秒非表示・0.3秒表示 を3回くり返す（秒数と回数は Inspector で変更可）
///  ・そのあとメイン画面（攻守決め選択）へ
///
/// 【使い方】
///  1. Panel_ModeSelect にアタッチ
///  2. Inspector で
///       Options … One_people, Two_people（上から順に）
///       Fingers … Choose_finger1, Choose_finger2（Options と同じ順）
///  3. 決まったモードは OnDecided で受け取れる（1 = 一人で遊ぶ, 2 = 二人で遊ぶ）
///       SelectedPlayerCount にも残る
///
///  ※ どのコントローラーでも操作できる（モード選択ではまだ 1P/2P が決まっていないため）
/// </summary>
public class ModeSelectUI : MonoBehaviour
{
    [Header("選択肢（上から順に）")]
    [SerializeField] private GameObject[] options = new GameObject[2];

    [Header("指（選択肢と同じ順）")]
    [SerializeField] private GameObject[] fingers = new GameObject[2];

    [Header("操作")]
    [SerializeField] private float stickThreshold = 0.5f;   // これ以上倒したら入力とみなす
    [SerializeField] private DecideButton aButton = DecideButton.South;

    // Xbox の A は下、Switch の A は右
    public enum DecideButton { South, East }

    [Header("決定したときの点滅")]
    [SerializeField] private float hideSeconds = 0.3f;   // 非表示の時間
    [SerializeField] private float showSeconds = 0.3f;   // 表示の時間
    [SerializeField] private int blinkCount = 3;         // くり返す回数

    [Header("点滅のあとメイン画面へ進む")]
    [SerializeField] private bool goToMain = true;

    /// <summary>モードが決まった（点滅が終わったあと）1 = 一人, 2 = 二人</summary>
    public event Action<int> OnDecided;

    /// <summary>最後に決まったモード（ほかのスクリプトから見る用）</summary>
    public static int SelectedPlayerCount { get; private set; } = 1;

    public int Current { get; private set; } = 0;   // 0 = 一人, 1 = 二人

    private bool stickWasTilted = false;   // スティックを倒しっぱなしで連続で動かないように
    private bool decided = false;
    private int enabledFrame;              // タイトルで押した A を拾わないように

    void OnEnable()
    {
        // パネルが表示されるたびに「一人で遊ぶ」から始める
        decided = false;
        enabledFrame = Time.frameCount;
        stickWasTilted = true;   // 前の画面で倒していた入力を拾わない
        foreach (var o in options) if (o != null) o.SetActive(true);
        SetSelected(0);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (decided || Time.frameCount == enabledFrame) return;

        int move = ReadUpDown();
        if (move != 0)
        {
            SetSelected(Mathf.Clamp(Current + move, 0, options.Length - 1));
        }

        foreach (var pad in Gamepad.all)
        {
            var button = aButton == DecideButton.South ? pad.buttonSouth : pad.buttonEast;
            if (button.wasPressedThisFrame)
            {
                decided = true;
                StartCoroutine(DecideAndBlink());
                return;
            }
        }
    }

    /// <summary>上なら -1、下なら +1、何もなければ 0（どのコントローラーでも）</summary>
    private int ReadUpDown()
    {
        float y = 0f;   // 一番大きく倒しているスティック
        foreach (var pad in Gamepad.all)
        {
            // 十字キー
            if (pad.dpad.up.wasPressedThisFrame)   return -1;
            if (pad.dpad.down.wasPressedThisFrame) return +1;

            float py = pad.leftStick.ReadValue().y;
            if (Mathf.Abs(py) > Mathf.Abs(y)) y = py;
        }

        // 左スティック（倒した瞬間だけ反応）
        bool tilted = Mathf.Abs(y) >= stickThreshold;
        int result = 0;
        if (tilted && !stickWasTilted) result = y > 0 ? -1 : +1;
        stickWasTilted = tilted;
        return result;
    }

    // 選んでいる方の指だけ表示、もう片方は非表示
    private void SetSelected(int index)
    {
        Current = index;
        for (int i = 0; i < fingers.Length; i++)
        {
            if (fingers[i] != null) fingers[i].SetActive(i == index);
        }
    }

    private IEnumerator DecideAndBlink()
    {
        int count = Current + 1;
        Debug.Log($"[モード選択] {(count == 1 ? "一人で遊ぶ" : "二人で遊ぶ")} に決定");

        // 0.3秒非表示 → 0.3秒表示 を3回
        var target = options[Current];
        for (int i = 0; i < blinkCount; i++)
        {
            if (target != null) target.SetActive(false);
            yield return new WaitForSeconds(hideSeconds);
            if (target != null) target.SetActive(true);
            yield return new WaitForSeconds(showSeconds);
        }

        SelectedPlayerCount = count;
        OnDecided?.Invoke(count);

        if (goToMain)
        {
            PhaseUIManager.Instance.ShowPhase(PhaseUIManager.UIPhase.KoushuSelect);
        }
    }
}
