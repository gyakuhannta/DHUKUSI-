using UnityEngine;

public class TimingCursor : MonoBehaviour
{
    [SerializeField] private float speed = 300f;

    [SerializeField] private float LeftLimit = -290f;
    [SerializeField] private float rightLimit = 290f;

    private float direction = 1f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    void Update()
    {
        Vector2 position = rectTransform.anchoredPosition;

        position.x += speed * direction * Time.deltaTime;

        if (position.x >= rightLimit)
        {
            position.x = rightLimit;
            direction = -1f;
        }
        if (position.x <= LeftLimit)
        {
            position.x = LeftLimit;
            direction = 1f;
        }
        rectTransform.anchoredPosition = position;
    }
}
