using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 新しいInput Systemに対応

public class SceneNavigator : MonoBehaviour
{
    [Header("エスケープキーを押したときに移動するシーン名")]
    [SerializeField] private string targetSceneName;

    void Update()
    {
        // エスケープキー（Esc）が押された瞬間を検知
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            MoveToTargetScene();
        }
    }

    /// <summary>
    /// 指定されたシーンへ移動する処理
    /// </summary>
    public void MoveToTargetScene()
    {
        // インスペクターでシーン名がちゃんと入力されているかチェック
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("移動先のシーン名（Target Scene Name）が設定されていません！インスペクターを確認してください。");
        }
    }
}
