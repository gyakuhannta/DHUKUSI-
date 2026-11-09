#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DhukusiResultSetup
{
    const string PrefabPath = "Assets/Tomoki/Prefabs/DHUKUSI_UI.prefab";
    const string FontPath = "Assets/Fonts/DelaSukoGothicOne-R.ttf";
    const string FingerPath = "Assets/Tomoki/Images/Choose_finger.png";

    static readonly Color Yamabuki = new Color32(0xf8, 0xb5, 0x00, 0xff);

    [MenuItem("DHUKUSI/勝敗判定画面を動くようにする")]
    public static void Setup()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var panel = Find(root.transform, "Panel_Result");
            if (panel == null) { Debug.LogError("[DHUKUSI] Panel_Result が見つかりません"); return; }

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var finger = AssetDatabase.LoadAssetAtPath<Sprite>(FingerPath);

            // 古い「Aボタンでタイトルへ」は消す
            var old = panel.Find("Result_Next_Text");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // 作り直すので、前に作ったものは消す
            foreach (var n in new[] { "Replay", "Back_Title", "Choose_finger_Replay", "Choose_finger_Title" })
            {
                var t = panel.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var replay = Button(panel, "Replay", new Vector2(0, -230), "もう一度プレイする", font);
            var title = Button(panel, "Back_Title", new Vector2(0, -380), "タイトルに戻る", font);
            var f1 = Finger(panel, "Choose_finger_Replay", new Vector2(-470, -230), finger, font);
            var f2 = Finger(panel, "Choose_finger_Title", new Vector2(-470, -380), finger, font);

            // 勝ち表示とセット表示を少し上へ
            var resultText = panel.Find("Result_Text");
            var setText = panel.Find("Result_Sub_Text");
            if (resultText != null) ((RectTransform)resultText).anchoredPosition = new Vector2(0, 200);
            if (setText != null) ((RectTransform)setText).anchoredPosition = new Vector2(0, 30);

            // ResultUI を付けて登録
            var ui = panel.GetComponent<ResultUI>() ?? panel.gameObject.AddComponent<ResultUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("resultText").objectReferenceValue = resultText != null ? resultText.GetComponent<Text>() : null;
            so.FindProperty("setText").objectReferenceValue = setText != null ? setText.GetComponent<Text>() : null;
            SetArray(so.FindProperty("options"), replay, title);
            SetArray(so.FindProperty("fingers"), f1, f2);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[DHUKUSI] 勝敗判定画面を作り直して " + PrefabPath + " に保存しました");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 山吹色の四角＋黒い文字のボタン
    static GameObject Button(Transform parent, string name, Vector2 pos, string label, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(720, 120);
        var img = go.GetComponent<Image>();
        img.color = Yamabuki;
        img.raycastTarget = false;

        var txt = new GameObject(name + "_Text", typeof(RectTransform), typeof(Text));
        txt.transform.SetParent(go.transform, false);
        var trt = (RectTransform)txt.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = txt.GetComponent<Text>();
        t.text = label;
        t.fontSize = 64;
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return go;
    }

    static GameObject Finger(Transform parent, string name, Vector2 pos, Sprite sprite, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160, 110);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else
        {
            img.color = new Color32(0xfc, 0xe9, 0xcb, 0xff);
        }
        return go;
    }

    static Transform Find(Transform parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void SetArray(SerializedProperty prop, params GameObject[] items)
    {
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
#endif