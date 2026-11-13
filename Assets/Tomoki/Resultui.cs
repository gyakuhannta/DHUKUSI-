using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class ResultUI : MonoBehaviour
{
    [Header("文字")]
    [SerializeField] private Text resultText;   // 「1P WIN!!」
    [SerializeField] private Text setText;      // 「セット 2 - 1」

    [Header("ボタン（上から：もう一度プレイする, タイトルに戻る）")]
    [SerializeField] private GameObject[] options = new GameObject[2];

    [Header("指（ボタンと同じ順）")]
    [SerializeField] private GameObject[] fingers = new GameObject[2];

    [Header("操作")]
    [SerializeField] private float stickThreshold = 0.5f;
    [SerializeField] private DecideButton aButton = DecideButton.South;
    public enum DecideButton { South, East }   // Xbox の A は下、Switch の A は右

    [Header("決定したときの点滅（モード選択と同じ）")]
    [SerializeField] private float hideSeconds = 0.3f;
    [SerializeField] private float showSeconds = 0.3f;
    [SerializeField] private int blinkCount = 3;

    /// <summary>「もう一度プレイする」が押された</summary>
    public static event Action OnReplay;
    /// <summary>「タイトルに戻る」が押された</summary>
    public static event Action OnBackToTitle;

    // 表示する結果（パネルが非表示でも受け取れるよう static で持つ）
    public static int Winner { get; private set; } = 0;   // 0 = 1P, 1 = 2P
    public static int Sets1P { get; private set; } = 0;
    public static int Sets2P { get; private set; } = 0;

    private const int Replay = 0;
    private const int BackToTitle = 1;

    private int current = Replay;
    private bool decided;
    private bool stickWasTilted;
    private int enabledFrame;

    /// <summary>勝敗判定画面を出す（試合が終わったら呼ぶ）</summary>
    public static void Show(int winner, int sets1P, int sets2P)
    {
        Winner = winner;
        Sets1P = sets1P;
        Sets2P = sets2P;
        Debug.Log($"[勝敗] {winner + 1}P の勝ち（{sets1P} - {sets2P}）");
        PhaseUIManager.Instance.ShowPhase(PhaseUIManager.UIPhase.Result);
    }

    void OnEnable()
    {
        if (resultText != null) resultText.text = $"{Winner + 1}P WIN!!";
        if (setText != null) setText.text = $"セット {Sets1P} - {Sets2P}";

        decided = false;
        stickWasTilted = true;            // 前の画面で倒していた入力を拾わない
        enabledFrame = Time.frameCount;   // 前の画面で押した A を拾わない
        foreach (var o in options) if (o != null) o.SetActive(true);
        SetSelected(Replay);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (decided || Time.frameCount == enabledFrame) return;

        int move = ReadUpDown();
        if (move != 0) SetSelected(Mathf.Clamp(current + move, 0, options.Length - 1));

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

    /// <summary>上なら -1、下なら +1</summary>
    private int ReadUpDown()
    {
        float y = 0f;
        foreach (var pad in Gamepad.all)
        {
            if (pad.dpad.up.wasPressedThisFrame) return -1;
            if (pad.dpad.down.wasPressedThisFrame) return +1;
            float py = pad.leftStick.ReadValue().y;
            if (Mathf.Abs(py) > Mathf.Abs(y)) y = py;
        }
        bool tilted = Mathf.Abs(y) >= stickThreshold;
        int result = (tilted && !stickWasTilted) ? (y > 0 ? -1 : +1) : 0;
        stickWasTilted = tilted;
        return result;
    }

    // 選んでいる方の指だけ表示
    private void SetSelected(int index)
    {
        current = index;
        for (int i = 0; i < fingers.Length; i++)
            if (fingers[i] != null) fingers[i].SetActive(i == index);
    }

    private IEnumerator DecideAndBlink()
    {
        var target = options[current];
        for (int i = 0; i < blinkCount; i++)
        {
            if (target != null) target.SetActive(false);
            yield return new WaitForSeconds(hideSeconds);
            if (target != null) target.SetActive(true);
            yield return new WaitForSeconds(showSeconds);
        }

        if (current == Replay)
        {
            Debug.Log("[勝敗] もう一度プレイする");
            if (CostManager.Instance != null) CostManager.Instance.ResetAll();
            OnReplay?.Invoke();
            PhaseUIManager.Instance.ShowPhase(PhaseUIManager.UIPhase.KoushuSelect);
        }
        else
        {
            Debug.Log("[勝敗] タイトルに戻る");
            OnBackToTitle?.Invoke();
            PhaseUIManager.Instance.ShowPhase(PhaseUIManager.UIPhase.Title);
        }
    }
}