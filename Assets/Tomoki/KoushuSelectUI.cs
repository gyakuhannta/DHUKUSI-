using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 攻守決めフェーズ選択画面の UI（仕様書「攻守決めフェーズ選択画面」のとおり）
///
///  ・Lスティック 上 → Choose_One（疾風）、左下 → Choose_Two（剛腕）、右下 → Choose_Three（鉄壁）を選択
///  ・選択中の Image を 1.3倍にして、そのデュクシの Cost を表示
///  ・決定ボタンで Choose を非表示にして Crush（砕けた木の板）を表示
///
/// 【使い方】
///  Panel_KoushuSelect にアタッチして、Inspector で各 Image を登録
///  決まったら OnDecided(プレイヤー, 選んだ番号 0〜2) が呼ばれる → 進行管理側で受け取る
///
///  ※ 疾風・剛腕・鉄壁の並びは仮。違ったら Inspector で Image を入れ替えるだけでOK
/// </summary>
public class KoushuSelectUI : MonoBehaviour
{
    public enum DecideButton { South, East, West, North }

    [Header("Choose（木の板）  [0]=One 上  [1]=Two 左下  [2]=Three 右下")]
    [SerializeField] private GameObject[] chooseImages = new GameObject[3];

    [Header("Crush（砕けた木の板） 並びは Choose と同じ")]
    [SerializeField] private GameObject[] crushImages = new GameObject[3];

    [Header("Cost（選択中だけ表示） 並びは Choose と同じ")]
    [SerializeField] private GameObject[] costImages = new GameObject[3];

    [Header("設定")]
    [SerializeField] private float selectScale = 1.3f;
    [Tooltip("スティックをどれだけ倒したら反応するか")]
    [SerializeField] private float stickThreshold = 0.5f;
    [Tooltip("決定ボタン（Xbox の A = South）")]
    [SerializeField] private DecideButton decideButton = DecideButton.South;
    [Tooltip("操作するプレイヤー 0 = 1P, 1 = 2P")]
    [SerializeField] private int player = 0;

    public int Player { get => player; set => player = value; }

    //決定した（プレイヤー, 選んだ番号 0〜2）
    public event Action<int, int> OnDecided;

    private int selected = -1;     // 選択中（-1 = なし）
    private bool decided = false;

    void OnEnable()
    {
        // パネルが表示されるたびに通常時に戻す
        selected = -1;
        decided = false;
        for (int i = 0; i < 3; i++)
        {
            SetActive(chooseImages, i, true);
            SetActive(crushImages, i, false);
            SetActive(costImages, i, false);
            SetScale(i, 1f);
        }
    }

    void Update()
    {
        if (decided) return;

        Gamepad pad = GetPad();
        if (pad == null) return;

        // ---- スティックで選択 ----
        Vector2 stick = pad.leftStick.ReadValue();
        if (stick.magnitude >= stickThreshold)
        {
            int next;
            if (stick.y > 0 && Mathf.Abs(stick.x) < stick.y) next = 0;  // 上
            else if (stick.x < 0) next = 1;                              // 左下
            else next = 2;                                               // 右下

            if (next != selected) Select(next);
        }

        // ---- 決定 ----
        if (selected >= 0 && GetDecide(pad).wasPressedThisFrame)
        {
            Decide();
        }
    }

    private void Select(int index)
    {
        // 前の選択を等倍に戻す
        if (selected >= 0)
        {
            SetScale(selected, 1f);
            SetActive(costImages, selected, false);
        }

        selected = index;
        SetScale(selected, selectScale);
        SetActive(costImages, selected, true);
        Debug.Log($"[攻守決めUI] {player + 1}P 選択中: {index}");
    }

    private void Decide()
    {
        decided = true;
        SetActive(chooseImages, selected, false);
        SetActive(crushImages, selected, true);
        Debug.Log($"[攻守決めUI] {player + 1}P 決定: {selected}");
        OnDecided?.Invoke(player, selected);
    }

    // ===================== 共通 =====================

    private Gamepad GetPad()
    {
        var all = Gamepad.all;
        if (all.Count == 0) return null;
        return player < all.Count ? all[player] : all[0];   // 1台しかなければ その1台
    }

    private UnityEngine.InputSystem.Controls.ButtonControl GetDecide(Gamepad pad)
    {
        switch (decideButton)
        {
            case DecideButton.East:  return pad.buttonEast;
            case DecideButton.West:  return pad.buttonWest;
            case DecideButton.North: return pad.buttonNorth;
            default:                 return pad.buttonSouth;
        }
    }

    private void SetActive(GameObject[] arr, int i, bool active)
    {
        if (arr != null && i < arr.Length && arr[i] != null) arr[i].SetActive(active);
    }

    private void SetScale(int i, float scale)
    {
        if (i < chooseImages.Length && chooseImages[i] != null)
            chooseImages[i].transform.localScale = Vector3.one * scale;
    }
}
