using UnityEngine;

public class TimingCursor : MonoBehaviour
{
    [SerializeField] private float speed = 300f;

    [SerializeField] private float LeftLimit = -290f;
    [SerializeField] private float rightLimit = 290f;

    private float direction = 1f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    void Update()
    {
        MoveCursor();
    }

    private void MoveCursor()
    {
        Vector2 position = rectTransform.anchoredPosition;

        position.x += speed * direction * Time.deltaTime;

        if (position.x >= rightLimit)
        {
            return;
        }
        else if (position.x <= LeftLimit)
        {
            return;
        }

        rectTransform.anchoredPosition = position;
    }
}
