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
///  ・AlphaHUD（メッセージの表示）を足す
///  ・DHUKUSI_Canvas（攻守決め・ライフ・セット・コストの画面）を足して、AlphaGameFlow で流れをつなぐ
///  ・もう一度押すと、足した 3つだけ作り直す
/// </summary>
public static class DhukusiAlphaSetup
{
    const string ScenePath = "Assets/Main_game/mineSeen.unity";
    const string FontPath  = "Assets/Fonts/DelaSukoGothicOne-R.ttf";
    const string PrefabPath = "Assets/Tomoki/Prefabs/DHUKUSI_UI.prefab";

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
        foreach (var n in new[] { "AlphaSystem", "AlphaHUD", "DHUKUSI_Canvas" })
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
        so.FindProperty("costText").objectReferenceValue = null;   // コストは DHUKUSI_Canvas の HUD に出す
        so.FindProperty("messageText").objectReferenceValue = msg.GetComponent<Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- 攻守決め・HUD（DHUKUSI_Canvas）----
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null)
        {
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ui.name = "DHUKUSI_Canvas";
            var canvasUI = ui.GetComponent<Canvas>();
            if (canvasUI != null) canvasUI.sortingOrder = 5;

            var manager = ui.GetComponentInChildren<PhaseUIManager>(true);
            var mso = new SerializedObject(manager);
            mso.FindProperty("startPhase").enumValueIndex = (int)PhaseUIManager.UIPhase.None;
            mso.ApplyModifiedPropertiesWithoutUndo();

            var flow = system.AddComponent<AlphaGameFlow>();
            var fso = new SerializedObject(flow);
            fso.FindProperty("ui").objectReferenceValue = manager;
            fso.FindProperty("koushuSelect").objectReferenceValue = ui.GetComponentInChildren<KoushuSelectUI>(true);
            var battle = Find(ui.transform, "Panel_KoushuBattle");
            fso.FindProperty("battle1PText").objectReferenceValue = Comp<Text>(battle, "Battle_1P_Text");
            fso.FindProperty("battle2PText").objectReferenceValue = Comp<Text>(battle, "Battle_2P_Text");
            fso.FindProperty("judgeText").objectReferenceValue = Comp<Text>(battle, "Judge_Text");
            fso.FindProperty("waitText").objectReferenceValue = Comp<Text>(battle, "Wait_Text");
            SetImages(fso.FindProperty("life1P"), ui.transform, "Life_1P_1", "Life_1P_2");
            SetImages(fso.FindProperty("life2P"), ui.transform, "Life_2P_1", "Life_2P_2");
            SetImages(fso.FindProperty("set1P"), ui.transform, "Set_1P_1", "Set_1P_2");
            SetImages(fso.FindProperty("set2P"), ui.transform, "Set_2P_1", "Set_2P_2");
            fso.FindProperty("costText").objectReferenceValue = Comp<Text>(ui.transform, "Cost_Text");
            fso.FindProperty("player").objectReferenceValue = Object.FindFirstObjectByType<PlayerLifeController>();
            fso.FindProperty("enemy").objectReferenceValue = Object.FindFirstObjectByType<EnemyController>();
            fso.FindProperty("enemyAttack").objectReferenceValue = Object.FindFirstObjectByType<EnemyAttackController>();
            var canvasArata = FindTimingBar() != null ? FindTimingBar().GetComponentInParent<Canvas>(true) : null;
            if (canvasArata != null) fso.FindProperty("battleCanvas").objectReferenceValue = canvasArata.rootCanvas.gameObject;
            var hide = new System.Collections.Generic.List<GameObject>();
            foreach (var c in new MonoBehaviour[] { Object.FindFirstObjectByType<PlayerLifeController>(), Object.FindFirstObjectByType<EnemyController>() })
            {
                if (c == null) continue;
                var lt = new SerializedObject(c).FindProperty("lifeText");
                if (lt != null && lt.objectReferenceValue is Component comp) hide.Add(comp.gameObject);
            }
            var msgRect = SerializedRect(Object.FindFirstObjectByType<AttackTimingController>(), "resultText");
            fso.FindProperty("messageText").objectReferenceValue = msgRect;
            fso.FindProperty("actionText").objectReferenceValue = SerializedRect(Object.FindFirstObjectByType<TimingJudge>(), "actionText");
            // 仮の文字（SelectedText など、どこからも使われていない文字）も戦闘中は隠す
            if (canvasArata != null)
                foreach (var tmp in canvasArata.rootCanvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if (tmp.name == "SelectedText" && tmp.transform != msgRect) hide.Add(tmp.gameObject);
            var hp = fso.FindProperty("hideInBattle");
            hp.arraySize = hide.Count;
            for (int i = 0; i < hide.Count; i++) hp.GetArrayElementAtIndex(i).objectReferenceValue = hide[i];
            fso.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogWarning("[DHUKUSI] " + PrefabPath + " がないので、攻守決めと HUD は入れていません");

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

    static RectTransform SerializedRect(Object owner, string field)
    {
        if (owner == null) return null;
        var p = new SerializedObject(owner).FindProperty(field);
        return p != null && p.objectReferenceValue is Component c ? c.transform as RectTransform : null;
    }

    static Transform Find(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static T Comp<T>(Transform parent, string name) where T : Component
    {
        var t = Find(parent, name);
        return t != null ? t.GetComponent<T>() : null;
    }

    static void SetImages(SerializedProperty prop, Transform root, params string[] names)
    {
        prop.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = Comp<Image>(root, names[i]);
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
