#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// mineSeen をアルファ版にする（コントローラー操作とコストを足す）
///
/// メニュー「DHUKUSI → mineSeen をアルファ版にする」
///  ・Gamemanager / Canvas / Player はそのまま使う（中身は変えない）
///  ・AlphaSystem（DhukusiInput・CostManager・AlphaBattleController）を足す
///  ・AlphaHUD（残りコストとメッセージの表示）を足す
///  ・もう一度押すと、足した 2つだけ作り直す
/// </summary>
public static class DhukusiAlphaSetup
{
    const string ScenePath = "Assets/Main_game/mineSeen.unity";
    const string FontPath  = "Assets/Fonts/DelaSukoGothicOne-R.ttf";

    static readonly Color Yamabuki = new Color32(0xf8, 0xb5, 0x00, 0xff);

    [MenuItem("DHUKUSI/mineSeen をアルファ版にする")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("DHUKUSI", ScenePath + " が見つかりません", "OK");
            return;
        }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 前に足したものは消して作り直す
        foreach (var n in new[] { "AlphaSystem", "AlphaHUD" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---- 表示（残りコスト・メッセージ）----
        var hud = new GameObject("AlphaHUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var cost = Txt("Cost_Text", hud.transform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(600, 80),
                       "残りコスト 10 / 10", 52, Yamabuki, font, TextAnchor.UpperRight);
        var msg = Txt("Message_Text", hud.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1200, 100),
                      "", 64, Color.white, font, TextAnchor.MiddleCenter);
        msg.AddComponent<Outline>().effectColor = Color.black;

        // ---- しくみ ----
        var system = new GameObject("AlphaSystem");
        system.AddComponent<DhukusiInput>();
        system.AddComponent<CostManager>();
        var ctrl = system.AddComponent<AlphaBattleController>();

        var so = new SerializedObject(ctrl);
        so.FindProperty("techniqueSelector").objectReferenceValue = Object.FindFirstObjectByType<TechiqueSelector>();
        so.FindProperty("timingBar").objectReferenceValue = FindTimingBar();
        so.FindProperty("costText").objectReferenceValue = cost.GetComponent<Text>();
        so.FindProperty("messageText").objectReferenceValue = msg.GetComponent<Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = system;
        Debug.Log("[DHUKUSI] mineSeen に AlphaSystem と AlphaHUD を足しました");
    }

    // AttackTimingController に登録されているタイミングバーを探す
    static GameObject FindTimingBar()
    {
        var attack = Object.FindFirstObjectByType<AttackTimingController>();
        if (attack != null)
        {
            var p = new SerializedObject(attack).FindProperty("timingBar");
            if (p != null && p.objectReferenceValue is GameObject g) return g;
        }
        Debug.LogWarning("[DHUKUSI] タイミングバーが見つかりません。AlphaSystem の Timing Bar に手で入れてください");
        return null;
    }

    static GameObject Txt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
                          string text, int fontSize, Color color, Font font, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return go;
    }
}
#endif
