using UnityEngine;

public class TimingCursor : MonoBehaviour
{
    [SerializeField] private float speed = 300f;
    [SerializeField] private float leftLimit = -290f;
    [SerializeField] private float rightLimit = 290f;

    private float direction = 1f;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        MoveCursor();
    }

    private void MoveCursor()
    {
        Vector2 position = rectTransform.anchoredPosition;

        position.x += speed * direction * Time.deltaTime;

        if (position.x >= rightLimit)
        {
            position.x = rightLimit;
            direction = -1f;
        }
        else if (position.x <= leftLimit)
        {
            position.x = leftLimit;
            direction = 1f;
        }

        rectTransform.anchoredPosition = position;
    }
}
