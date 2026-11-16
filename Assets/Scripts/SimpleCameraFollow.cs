using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("追いかける対象（Player）")]
    public Transform target;

    [Header("プレイヤーからの距離と高さ")]
    public Vector3 offset = new Vector3(0f, 3.0f, -6.0f);

    [Header("追従のなめらかさ")]
    public float smoothSpeed = 5.0f;

    void LateUpdate()
    {
        if (target == null) return;

        // 目標となるカメラの位置を計算
        Vector3 desiredPosition = target.position + offset;

        // なめらかに移動させる
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 常にプレイヤーの少し上（胸〜頭あたり）を見る
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}
