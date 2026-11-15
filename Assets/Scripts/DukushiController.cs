using UnityEngine;
using UnityEngine.InputSystem; // Input Systemを使うために必要

public class DukushiController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // ゲームパッドが接続されているか確認
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // Xbox配列を基準としたボタン取得
        // buttonWest = Xボタン (PSなら□)
        if (gamepad.buttonWest.wasPressedThisFrame)
        {
            animator.SetTrigger("1"); // フェイントデュクシ
        }
        // buttonNorth = Yボタン (PSなら△)
        else if (gamepad.buttonNorth.wasPressedThisFrame)
        {
            animator.SetTrigger("2"); // 通常デュクシ
        }
        // buttonSouth = Aボタン (PSなら×)
        else if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            animator.SetTrigger("3"); // 激重デュクシ
        }
        // buttonEast = Bボタン (PSなら○)
        else if (gamepad.buttonEast.wasPressedThisFrame)
        {
            animator.SetTrigger("4"); // 連続デュクシ
        }
    }
}
