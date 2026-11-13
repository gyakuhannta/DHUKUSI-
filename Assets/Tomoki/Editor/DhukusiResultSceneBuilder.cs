#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 勝敗判定シーンを作る
///
/// メニュー「DHUKUSI → 勝敗判定シーンを作る」
///  ・Assets/Main_game/ResultScene.unity を作る
///  ・ボタンの選択は TwoChoiceSelector を使う（操作もタイトルのモード選択と同じ）
///      もう一度プレイする → mineSeen ／ タイトルに戻る → Titl
///  ・ほかの人のスクリプトやシーンには一切さわらない
///  ・試合が終わったら ResultView.ShowPvE(勝ったら true) を呼べば、このシーンに移る
///  ・Titl / mineSeen / ResultScene を Build Settings に追加する
///  ・もう一度押すと作り直す
/// </summary>
public static class DhukusiResultSceneBuilder
{
    const string Folder     = "Assets/Main_game/";
    const string ScenePath  = Folder + ResultView.SceneName + ".unity";
    const string MainScene  = "mineSeen";
    const string TitleScene = "Titl";
    const string FontPath   = "Assets/Fonts/DelaSukoGothicOne-R.ttf";
    const string FingerPath = "Assets/Tomoki/Images/Choose_finger.png";

    static readonly Color Yamabuki = new Color32(0xf8, 0xb5, 0x00, 0xff);

    [MenuItem("DHUKUSI/勝敗判定シーンを作る")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        var finger = AssetDatabase.LoadAssetAtPath<Sprite>(FingerPath);

        // ---- Canvas ----
        var canvasGO = new GameObject("Result_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var c = canvasGO.transform;

        // 背景（ステージ背景の画像が来たら差し替える）
        var bg = Img("Background", c, Vector2.zero, new Vector2(1920, 1080), new Color(0.08f, 0.06f, 0.1f));
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var result = Txt("Result_Text", c, new Vector2(0, 200), new Vector2(1400, 220), "勝利！", 180, Yamabuki, font);
        var sets   = Txt("Set_Text", c, new Vector2(0, 30), new Vector2(1400, 80), "敵を倒した！", 56, Color.white, font);

        var replay = Button("Replay", c, new Vector2(0, -230), "もう一度プレイする", font);
        var title  = Button("Back_Title", c, new Vector2(0, -380), "タイトルに戻る", font);
        var f1 = Finger("Choose_finger1", c, new Vector2(-470, -230), finger);
        var f2 = Finger("Choose_finger2", c, new Vector2(-470, -380), finger);

        // ---- 結果の表示 ----
        var view = canvasGO.AddComponent<ResultView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("resultText").objectReferenceValue = result.GetComponent<Text>();
        vso.FindProperty("subText").objectReferenceValue = sets.GetComponent<Text>();
        vso.ApplyModifiedPropertiesWithoutUndo();

        // ---- 選択（TwoChoiceSelector）----
        var manager = new GameObject("SelectManager");
        var selector = manager.AddComponent<TwoChoiceSelector>();
        var so = new SerializedObject(selector);
        so.FindProperty("image1").objectReferenceValue = f1;         // 選択中の印（上）
        so.FindProperty("image2").objectReferenceValue = f2;         // 選択中の印（下）
        so.FindProperty("Selectimage1").objectReferenceValue = replay; // 決定で点滅（上）
        so.FindProperty("Selectimage2").objectReferenceValue = title;  // 決定で点滅（下）
        so.FindProperty("sceneForSelection1").stringValue = MainScene;
        so.FindProperty("sceneForSelection2").stringValue = TitleScene;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);

        AddToBuildSettings(Folder + TitleScene + ".unity");
        AddToBuildSettings(Folder + MainScene + ".unity");
        AddToBuildSettings(ScenePath);

        Debug.Log("[DHUKUSI] " + ScenePath + " を作りました");
    }

    static void AddToBuildSettings(string path)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static GameObject Img(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static GameObject Txt(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    static GameObject Button(string name, Transform parent, Vector2 pos, string label, Font font)
    {
        var go = Img(name, parent, pos, new Vector2(720, 120), Yamabuki);
        var t = Txt(name + "_Text", go.transform, Vector2.zero, Vector2.zero, label, 64, Color.black, font);
        var rt = (RectTransform)t.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject Finger(string name, Transform parent, Vector2 pos, Sprite sprite)
    {
        var go = Img(name, parent, pos, new Vector2(160, 110), new Color32(0xfc, 0xe9, 0xcb, 0xff));
        if (sprite != null)
        {
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        return go;
    }
}
#endif
