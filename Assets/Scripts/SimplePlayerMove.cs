using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerMove : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 10.0f;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float h = 0f;
        float v = 0f;

        // 1. キーボード入力（WASD と 矢印キー）
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
        }

        // 2. ゲームパッド入力（左スティック）
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            h += stick.x;
            v += stick.y;
        }

        // キーボードとスティックを同時に触った時に速度が2倍にならないよう制限
        Vector3 moveDir = Vector3.ClampMagnitude(new Vector3(h, 0, v), 1.0f);

        if (moveDir.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

            Vector3 velocity = moveDir * moveSpeed;
            velocity.y = -9.8f;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
