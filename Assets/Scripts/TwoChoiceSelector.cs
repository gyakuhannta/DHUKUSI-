using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
        if (Gamepad.current == null)
            return;

        float stickY = Gamepad.current.leftStick.ReadValue().y;
        float dpadY = Gamepad.current.dpad.ReadValue().y;

        // 点滅中は選択変更しない
        if (!isBlinking)
        {
            // 上
            if (canMove && (stickY > 0.5f || dpadY > 0.5f))
            {
                MoveSelection(-1);
                canMove = false;
            }

            // 下
            if (canMove && (stickY < -0.5f || dpadY < -0.5f))
            {
                MoveSelection(1);
                canMove = false;
            }

            // スティック・十字キーを離した
            if (Mathf.Abs(stickY) < 0.2f && Mathf.Abs(dpadY) < 0.2f)
            {
                canMove = true;
            }

            // ○ボタン
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                StartCoroutine(BlinkSelectedImage());
            }
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

        // 現在選択されている画像を取得
        if (currentSelection == 0)
        {
            selectedImage = Selectimage1;
        }
        else
        {
            selectedImage = Selectimage2;
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

        // 最後は必ず表示
        selectedImage.SetActive(true);

        isBlinking = false;
    }
}
