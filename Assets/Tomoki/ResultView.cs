using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 勝敗判定シーン（ResultScene）の表示
///   もう一度プレイする → mineSeen ／ タイトルに戻る → Titl
///   1人用： ResultView.ShowPvE(勝ったら true);
///   2人用： ResultView.ShowPvP(勝ったプレイヤー, 1Pのセット数, 2Pのセット数);
/// </summary>
public class ResultView : MonoBehaviour
{
    public const string SceneName = "ResultScene";

    [SerializeField] private Text resultText;
    [SerializeField] private Text subText;

    private static string mainMessage = "勝利！";
    private static string subMessage = "";

    public static void ShowPvE(bool playerWon)
    {
        mainMessage = playerWon ? "勝利！" : "敗北…";
        subMessage = playerWon ? "敵を倒した！" : "また挑戦しよう";
        Open();
    }

    public static void ShowPvP(int winner, int sets1P, int sets2P)
    {
        mainMessage = $"{winner + 1}P WIN!!";
        subMessage = $"セット {sets1P} - {sets2P}";
        Open();
    }

    private static void Open()
    {
        Debug.Log($"[勝敗] {mainMessage} {subMessage} → {SceneName} へ");
        SceneManager.LoadScene(SceneName);
    }

    void Start()
    {
        if (resultText != null) resultText.text = mainMessage;
        if (subText != null) subText.text = subMessage;
    }
}
