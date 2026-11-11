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
        foreach (var n in new[] { "AlphaSystem", "AlphaHUD", "DHUKUSI_Canvas", "GameStage" })
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
            // 技選択の画面（下の 4つのデュクシ）。仕様書：選択中は 0.8倍、決定で 0.1秒ずつ 4回点滅
            var selectPanel = Find(ui.transform, "Panel_DhukusiSelect_Attack");
            var cso = new SerializedObject(ctrl);
            cso.FindProperty("selectPanel").objectReferenceValue = selectPanel != null ? selectPanel.gameObject : null;
            cso.ApplyModifiedPropertiesWithoutUndo();
            var selUI = selectPanel != null ? selectPanel.GetComponent<DhukusiSelectUI>() : null;
            if (selUI != null)
            {
                var dso = new SerializedObject(selUI);
                dso.FindProperty("pressScale").floatValue = 0.8f;
                dso.FindProperty("blinkInterval").floatValue = 0.1f;
                dso.FindProperty("blinkCount").intValue = 4;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }
            var hp = fso.FindProperty("hideInBattle");
            hp.arraySize = hide.Count;
            for (int i = 0; i < hide.Count; i++) hp.GetArrayElementAtIndex(i).objectReferenceValue = hide[i];
            fso.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogWarning("[DHUKUSI] " + PrefabPath + " がないので、攻守決めと HUD は入れていません");

        PlaceStage();
        FixTimingBar();
        TidyScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = system;
        Debug.Log("[DHUKUSI] mineSeen に AlphaSystem と AlphaHUD を足しました");
    }

    // ===================== ステージ（Teams：背景モデリング「GameStage」）=====================
    //  ・GameStage.fbx をシーンに置く
    //  ・FBX にカメラが入っていれば、その角度を Main Camera に写す（無ければ、ステージの端から真ん中を見る角度）
    static void PlaceStage()
    {
        string path = null;
        foreach (var guid in AssetDatabase.FindAssets("GameStage t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == "GameStage") { path = p; break; }
        }
        var model = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        if (model == null)
        {
            Debug.LogWarning("[DHUKUSI] GameStage.fbx が見つかりません（Teams の 背景モデリング → GameStage.unitypackage を入れてください）");
            return;
        }
        var stage = (GameObject)PrefabUtility.InstantiatePrefab(model);
        stage.name = "GameStage";
        stage.transform.position = Vector3.zero;

        var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        Camera fbxCam = null;
        foreach (var c in stage.GetComponentsInChildren<Camera>(true))
        {
            if (fbxCam == null) fbxCam = c;
            c.gameObject.SetActive(false);   // カメラは Main Camera 1台だけにする
        }
        foreach (var l in stage.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(l);
        if (cam == null) return;

        if (fbxCam != null)
        {
            cam.transform.SetPositionAndRotation(fbxCam.transform.position, fbxCam.transform.rotation);
            Debug.Log("[DHUKUSI] GameStage の中のカメラの角度を Main Camera に写しました");
            return;
        }

        // ステージの大きさから、写真のような角度（端の少し上から、奥の手すりが見えるように）
        var rends = stage.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;
        Bounds all = rends[0].bounds;
        Bounds ground = default; bool hasGround = false;
        foreach (var r in rends)
        {
            all.Encapsulate(r.bounds);
            foreach (var m in r.sharedMaterials)
                if (m != null && m.name.Contains("Ground"))
                {
                    if (!hasGround) { ground = r.bounds; hasGround = true; } else ground.Encapsulate(r.bounds);
                }
        }
        if (!hasGround) ground = all;
        float radius = Mathf.Max(ground.extents.x, ground.extents.z);
        float floorY = ground.max.y;
        if (ground.size.y > radius * 0.2f) floorY = ground.center.y;   // 地面が分厚いときは真ん中くらい
        var c0 = ground.center;
        cam.transform.position = new Vector3(c0.x, floorY + radius * 0.18f, c0.z - radius * 0.95f);
        cam.transform.LookAt(new Vector3(c0.x, floorY + radius * 0.05f, c0.z + radius * 0.3f));
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, radius * 6f);
        Debug.Log($"[DHUKUSI] GameStage を置きました（半径 約{radius:0.0}）。角度は Main Camera で調整できます");
    }

    // ===================== タイミングバー =====================
    //  攻撃の判定は「カーソルの位置が -40〜40」で成功（AttackTimingController に書いてある）。
    //  仕様書は「選んだデュクシの範囲で押す」なので、判定は AlphaBattleController がやり直す（下の 4つの範囲を使う）
    //  でもバーが x=140〜740 に置いてあって、カーソルも 140〜740 しか動かないので、絶対に成功しなかった。
    //  さらに Cursor に付いている TimingCursor（左右の端が逆）が同時にカーソルを動かして、カーソルが飛んでいた。
    //  → スクリプトは変えずに、シーンの中だけ直す
    //    ・バー・カーソル・4つのゾーンを左に、まとめている親を右に動かして、バーの真ん中を x=0 にする（見た目の場所は前と同じ）
    //    ・攻撃のカーソルが動く範囲をバーの端から端にする
    //    ・TimingCursor を切る（攻撃は AttackTimingController が動かす。防御はボタンで読むので不要）
    static void FixTimingBar()
    {
        var attack = Object.FindFirstObjectByType<AttackTimingController>();
        if (attack == null) return;
        var so = new SerializedObject(attack);
        var cursor = so.FindProperty("cursor").objectReferenceValue as RectTransform;
        if (cursor == null || cursor.parent == null) return;

        RectTransform bar = null;
        foreach (Transform c in cursor.parent) if (c.name == "BarImage") bar = (RectTransform)c;
        if (bar == null) return;

        float dx = -bar.anchoredPosition.x;
        if (Mathf.Abs(dx) > 1f)
        {
            foreach (Transform c in cursor.parent)
            {
                if (c.name != "BarImage" && c.name != "Cursor" && !c.name.StartsWith("SuccessZone")) continue;
                var rt = (RectTransform)c;
                rt.anchoredPosition += new Vector2(dx, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
            }
        }
        // 親（バーをまとめている物）も動かして、バーが画面の真ん中に来るようにする（見た目は前と同じ場所）
        var holder = cursor.parent;
        if (Mathf.Abs(holder.localPosition.x) > 1f)
        {
            holder.localPosition = new Vector3(0f, holder.localPosition.y, holder.localPosition.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(holder);
        }

        // 仕様書「デュクシ繰り出しフェーズ」：バーを 4つに分けて、左から 通常・フェイント・連続・激重
        //  攻撃は「選んだデュクシの範囲」にカーソルがあるときに押すと成功（判定は AlphaBattleController）
        var judge = Object.FindFirstObjectByType<TimingJudge>();
        var zonesProp = judge != null ? new SerializedObject(judge).FindProperty("successZones") : null;
        if (zonesProp != null && zonesProp.arraySize == 4)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            string[] names = { "通常", "フェイント", "連続", "激重" };
            Color[] colors =
            {
                new Color32(0x8C, 0x8C, 0x8C, 0xff),   // 通常：灰
                new Color32(0x4F, 0x6E, 0x96, 0xff),   // フェイント：青
                new Color32(0x4E, 0x8C, 0x45, 0xff),   // 連続：緑
                new Color32(0xB5, 0x3A, 0x33, 0xff),   // 激重：赤
            };
            float seg = bar.rect.width / 4f;
            for (int i = 0; i < 4; i++)
            {
                var z = zonesProp.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
                if (z == null) continue;
                z.localScale = Vector3.one;
                z.sizeDelta = new Vector2(seg, bar.rect.height);
                z.anchoredPosition = new Vector2(bar.anchoredPosition.x - bar.rect.width / 2f + seg * (i + 0.5f), bar.anchoredPosition.y);
                var img = z.GetComponent<Image>();
                if (img != null) { img.color = colors[i]; EditorUtility.SetDirty(img); }
                PrefabUtility.RecordPrefabInstancePropertyModifications(z);

                var label = z.Find("Label");
                if (label == null)
                {
                    var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
                    go.transform.SetParent(z, false);
                    label = go.transform;
                }
                var lrt = (RectTransform)label;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                var t = label.GetComponent<Text>();
                t.text = names[i];
                t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 30;
                t.color = Color.white;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.raycastTarget = false;
                label.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            }
            cursor.SetAsLastSibling();   // カーソルは色の上に出す
        }

        float half = bar.rect.width / 2f - cursor.rect.width / 2f;
        so.FindProperty("leftLimit").floatValue = -half;
        so.FindProperty("rightLimit").floatValue = half;
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var tc in cursor.GetComponents<TimingCursor>())
        {
            tc.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tc);
        }
        Debug.Log($"[DHUKUSI] タイミングバーを仕様書の 4つの範囲にしました（カーソル {-half:0}〜{half:0}）");
    }

    // ===================== シーンの整理 =====================
    //  シーンを開いたときに重なって見える仮の物を隠す（スクリプトは変えない。ゲーム中の動きも変わらない）
    //  ・PlayerLife / life の文字、eranndene の文字（ライフは HUD の目で出す）
    //  ・仮のボタン 4つ（デュクシは下の木の板で選ぶ）
    //  ・タイミングバー（攻撃・防御のときに自動で出る）
    //  ・aaa / New Text の文字は中身を空にする（ゲーム中に自動で入る）
    static void TidyScene()
    {
        int n = 0;
        foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            switch (t.name)
            {
                case "PlayerlLifeText": case "EnemyLifeText": case "SelectedText":
                case "NormalButton": case "FeintButton": case "ComboButton": case "HeavyButton":
                    if (t.gameObject.activeSelf) { t.gameObject.SetActive(false); n++; }
                    break;
                case "ActionText": case "ResultText":
                    var tmp = t.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null && tmp.text != "") { tmp.text = ""; EditorUtility.SetDirty(tmp); n++; }
                    break;
            }
        }
        foreach (var owner in new Object[] { Object.FindFirstObjectByType<AttackTimingController>(), Object.FindFirstObjectByType<TimingJudge>() })
        {
            if (owner == null) continue;
            var p = new SerializedObject(owner).FindProperty("timingBar");
            if (p != null && p.objectReferenceValue is GameObject bar && bar.activeSelf) { bar.SetActive(false); n++; }
        }
        Debug.Log($"[DHUKUSI] シーンを整理しました（{n} か所）");
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
