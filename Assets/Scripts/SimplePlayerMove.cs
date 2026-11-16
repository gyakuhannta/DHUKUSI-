using UnityEngine;
using UnityEngine.InputSystem; // Å© Ç±ÇÍÇí«â¡

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
        if (Keyboard.current == null) return;

        // WASD Ç∆ ñÓàÛÉLÅ[ ÇÃì¸óÕÇéÊìæ
        float h = 0f;
        float v = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;

        Vector3 moveDir = new Vector3(h, 0, v).normalized;

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
