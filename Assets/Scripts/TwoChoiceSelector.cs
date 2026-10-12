using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class TwoChoiceSelector : MonoBehaviour
{
    [Header("選択肢1の白い画像")]
    [SerializeField] private GameObject image1;

    [Header("選択肢2の白い画像")]
    [SerializeField] private GameObject image2;

    [Header("最初に選択する項目")]
    [SerializeField] private int startSelection = 0;
    [Header("点滅する画像１")]
    [SerializeField] private GameObject Selectimage1;
    [Header("点滅する画像２")]
    [SerializeField] private GameObject Selectimage2;
    [Header("○ボタン点滅設定")]
    [SerializeField] private float blinkOffTime = 0.3f;
    [SerializeField] private float blinkOnTime = 0.3f;
    [SerializeField]
    private int blinkCount = 3;
    [Header("選択肢1の遷移先シーン名")]
    [SerializeField] private string sceneForSelection1;

    [Header("選択肢2の遷移先シーン名")]
    [SerializeField] private string sceneForSelection2;

    private int currentSelection;
    private bool canMove = true;
    private bool isBlinking = false;

    private void Start()
    {
        currentSelection = Mathf.Clamp(startSelection, 0, 1);

        UpdateSelection();
    }

    private void Update()
    {
        // 点滅中は一切の入力を受け付けない
        if (isBlinking) return;

        // --- 1. 上下の移動入力を取得 ---
        float moveY = 0f;

        // ゲームパッドの入力
        if (Gamepad.current != null)
        {
            float stickY = Gamepad.current.leftStick.ReadValue().y;
            float dpadY = Gamepad.current.dpad.ReadValue().y;
            if (Mathf.Abs(stickY) > 0.5f) moveY = stickY;
            else if (Mathf.Abs(dpadY) > 0.5f) moveY = dpadY;
        }

        // キーボードの入力（矢印キーの上/下 または W/S）も追加
        if (Keyboard.current != null && moveY == 0f)
        {
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) moveY = 1f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) moveY = -1f;
        }

        // --- 2. 選択肢の移動処理 ---
        if (canMove && moveY > 0.5f)
        {
            MoveSelection(-1); // 上
            canMove = false;
        }
        else if (canMove && moveY < -0.5f)
        {
            MoveSelection(1); // 下
            canMove = false;
        }

        // 入力が離されたら再度移動可能にする
        if (Mathf.Abs(moveY) < 0.2f)
        {
            canMove = true;
        }

        // --- 3. 決定（○ボタン / マウス左クリック / Enterキー） ---
        bool isSubmitPressed = false;

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) isSubmitPressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isSubmitPressed = true; // マウス左クリック
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) isSubmitPressed = true; // Enterキー

        if (isSubmitPressed)
        {
            StartCoroutine(BlinkSelectedImage());
        }
    }

    private void MoveSelection(int direction)
    {
        currentSelection += direction;

        // 0と1をループ
        if (currentSelection < 0)
            currentSelection = 1;

        if (currentSelection > 1)
            currentSelection = 0;

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        // 選択中の白い画像だけ表示
        if (image1 != null)
            image1.SetActive(currentSelection == 0);

        if (image2 != null)
            image2.SetActive(currentSelection == 1);
    }
    private IEnumerator BlinkSelectedImage()
    {
        isBlinking = true;

        GameObject selectedImage = null;
        string targetSceneName = ""; 

        
        if (currentSelection == 0)
        {
            selectedImage = Selectimage1;
            targetSceneName = sceneForSelection1;
        }
        else
        {
            selectedImage = Selectimage2;
            targetSceneName = sceneForSelection2;
        }

        if (selectedImage == null)
        {
            isBlinking = false;
            yield break;
        }

        // 3回繰り返す
        for (int i = 0; i < blinkCount; i++)
        {
            // 0.3秒 非表示
            selectedImage.SetActive(false);
            yield return new WaitForSeconds(blinkOffTime);

            // 0.3秒 表示
            selectedImage.SetActive(true);
            yield return new WaitForSeconds(blinkOnTime);
        }

       
        selectedImage.SetActive(true);

        
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("遷移先のシーン名が設定されていません。");
            isBlinking = false; 
        }
    }
}
