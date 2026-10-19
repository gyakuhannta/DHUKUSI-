#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DHUKUSI!!!!! の UI パネルを全部 自動で作る（見た目だけ・仮の色と文字）
///
/// 【使い方】
///  1. このファイルを Assets/Editor フォルダに入れる（Editor フォルダがなければ作る）
///  2. メインのシーンを開く
///  3. 上のメニュー「DHUKUSI」→「UIパネルを全部作る」
///  4. Hierarchy に「DHUKUSI_Canvas」ができる
///
/// 【確認するとき】
///  メニュー「DHUKUSI」→「プレビュー」→ 見たいフェーズ を選ぶと、そのパネルだけ表示される
///
///  ※ 名前は仕様書（Image[Choose_One] など）に合わせてある
///  ※ デザインが届いたら、Image の Source Image に画像を入れて、仮の文字を消すだけ
/// </summary>
public static class DhukusiUIBuilder
{
    // ---- 色（企画書・仕様書から） ----
    static readonly Color Yamabuki = Hex("#f8b500");   // キーカラー 山吹色
    static readonly Color Wood = Hex("#fdcf9f");   // 木の板
    static readonly Color WoodDark = Hex("#a87a4a");   // 砕けた木の板（仮）
    static readonly Color Blue = Hex("#1f6fd1");   // 通常
    static readonly Color Green = Hex("#2fa84f");   // フェイント
    static readonly Color Yellow = Hex("#f2d21b");   // 連続
    static readonly Color Red = Hex("#e53935");   // 激重
    static readonly Color Panel = new Color(1f, 1f, 1f, 0.85f);
    static readonly Color Gray = new Color(0.8f, 0.8f, 0.8f, 1f);

    static Font font;

    [MenuItem("DHUKUSI/UIパネルを全部作る")]
    public static void Build()
    {
        if (GameObject.Find("DHUKUSI_Canvas") != null &&
            !EditorUtility.DisplayDialog("DHUKUSI", "DHUKUSI_Canvas がもうあります。もう1つ作りますか？", "作る", "やめる"))
            return;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ===== Canvas =====
        var canvasGO = new GameObject("DHUKUSI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGO.transform;

        // ===== 共通HUD（ライフ・コスト） =====
        var hud = BuildHUD(root);

        // ===== フェーズごとのパネル =====
        var pKoushuSelect = BuildKoushuSelect(root, out var choose, out var crush, out var cost);
        var pKoushuBattle = BuildKoushuBattle(root);
        var pSelectAtk = BuildDhukusiSelect(root, "Panel_DhukusiSelect_Attack", "デュクシを選べ！（長押しでキャンセル）", out var atkImgs, out var atkGauge);
        var pSelectDef = BuildDhukusiSelect(root, "Panel_DhukusiSelect_Defense", "相手のデュクシを予想しろ！（長押しでキャンセル）", out var defImgs, out var defGauge);
        var pKuriAtk = BuildKuridashi(root, "Panel_Kuridashi_Attack", "好きなタイミングでデュクシ！");
        var pKuriDef = BuildKuridashi(root, "Panel_Kuridashi_Defense", "予備動作を見て防御しろ！");
        var pResult = BuildResult(root);
        var pTitle = BuildTitle(root);
        var pModeSelect = BuildModeSelect(root);

        // ===== PhaseUIManager に登録 =====
        var manager = canvasGO.AddComponent<PhaseUIManager>();
        var so = new SerializedObject(manager);
        var list = so.FindProperty("panels");
        // UIPhase の並び（None の次から）と同じ順番
        GameObject[] panels = { pKoushuSelect, pKoushuBattle, pSelectAtk, pSelectDef, pKuriAtk, pKuriDef, pResult, pTitle, pModeSelect };
        list.arraySize = panels.Length;
        for (int i = 0; i < panels.Length; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("phase").enumValueIndex = i + 1;   // None の次から順番
            e.FindPropertyRelative("panel").objectReferenceValue = panels[i];
        }
        so.FindProperty("hud").objectReferenceValue = hud;
        so.FindProperty("startPhase").enumValueIndex = (int)PhaseUIManager.UIPhase.Title;   // 最初はタイトル画面
        so.ApplyModifiedPropertiesWithoutUndo();

        // ===== 攻守決め選択画面の UI スクリプト =====
        var ks = pKoushuSelect.AddComponent<KoushuSelectUI>();
        var kso = new SerializedObject(ks);
        SetArray(kso.FindProperty("chooseImages"), choose);
        SetArray(kso.FindProperty("crushImages"), crush);
        SetArray(kso.FindProperty("costImages"), cost);
        kso.ApplyModifiedPropertiesWithoutUndo();

        // ===== デュクシ選択画面の UI スクリプト =====
        AddSelectUI(pSelectAtk, atkImgs, atkGauge);
        AddSelectUI(pSelectDef, defImgs, defGauge);

        // 最初はタイトル画面だけ表示（タイトルでは HUD も隠す）
        foreach (var p in panels) p.SetActive(p == pTitle);
        hud.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvasGO, "DHUKUSI UI 作成");
        Selection.activeGameObject = canvasGO;
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log("[DHUKUSI] UIパネルを作りました。メニュー DHUKUSI → プレビュー で切り替えて確認できます");
    }

    // ===================== 共通HUD =====================
    static GameObject BuildHUD(Transform root)
    {
        var hudGO = Rect("HUD", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
        var hud = hudGO.transform;

        // ライフ（目のアイコン×2）とセット
        for (int p = 0; p < 2; p++)
        {
            float side = p == 0 ? 0f : 1f;
            float dir = p == 0 ? 1f : -1f;
            var anchor = new Vector2(side, 1f);

            var box = Img($"Life_{p + 1}P", hud, anchor, new Vector2(dir * 250, -80), new Vector2(400, 110), Yamabuki);
            Txt($"Life_{p + 1}P_Label", box.transform, new Vector2(0.5f, 0f), new Vector2(0, -25), new Vector2(400, 40), $"{p + 1}P  現在のライフ", 24, Color.black);
            for (int i = 0; i < 2; i++)
                Img($"Life_{p + 1}P_{i + 1}", box.transform, new Vector2(0.5f, 0.5f), new Vector2(-90 + i * 180, 0), new Vector2(150, 80), Color.white);

            for (int i = 0; i < 2; i++)
                Img($"Set_{p + 1}P_{i + 1}", hud, anchor, new Vector2(dir * (70 + i * 60), -205), new Vector2(40, 40), Blue);
        }

        // 残りコスト（攻撃側のコストを表示）
        var costBox = Img("Cost", hud, new Vector2(0.5f, 1f), new Vector2(0, -85), new Vector2(340, 130), Gray);
        Txt("Cost_Label", costBox.transform, new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(340, 40), "残りコスト", 26, Color.black);
        Txt("Cost_Text", costBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(340, 80), "10/10", 60, Color.black);

        // 経過時間
        var timeBox = Img("Timer", hud, new Vector2(1f, 1f), new Vector2(-340, -205), new Vector2(260, 50), Gray);
        Txt("Timer_Text", timeBox.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 50), "経過時間 00:00", 26, Color.black);
        return hudGO;
    }

    // ===================== タイトル画面 =====================
    static GameObject BuildTitle(Transform root)
    {
        var panel = PanelRoot("Panel_Title", root);
        var t = panel.transform;

        var title = Img("Title", t, Center, new Vector2(0, 180), new Vector2(1100, 260), Yamabuki);
        Txt("Title_Text", title.transform, Center, Vector2.zero, new Vector2(1100, 260), "DHUKUSI!!!!!", 150, Color.black);

        var press = Img("Press_A", t, Center, new Vector2(0, -280), new Vector2(700, 90), Yamabuki);
        Txt("Press_A_Text", press.transform, Center, Vector2.zero, new Vector2(700, 90), "PRESS A TO START", 52, Color.black);
        return panel;
    }

    // ===================== モード選択画面 =====================
    static GameObject BuildModeSelect(Transform root)
    {
        var panel = PanelRoot("Panel_ModeSelect", root);
        var t = panel.transform;
        var finger = Hex("#fce9cb");

        var one = Img("One_people", t, Center, new Vector2(-250, 140), new Vector2(620, 150), Yamabuki);
        Txt("One_people_Text", one.transform, Center, Vector2.zero, new Vector2(620, 150), "一人で遊ぶ", 80, Color.black);
        var two = Img("Two_people", t, Center, new Vector2(-250, -140), new Vector2(620, 150), Yamabuki);
        Txt("Two_people_Text", two.transform, Center, Vector2.zero, new Vector2(620, 150), "二人で遊ぶ", 80, Color.black);

        var f1 = Img("Choose_finger1", t, Center, new Vector2(-680, 140), new Vector2(160, 110), finger);
        Txt("Choose_finger1_Text", f1.transform, Center, Vector2.zero, new Vector2(160, 110), "☞", 80, Color.black);
        var f2 = Img("Choose_finger2", t, Center, new Vector2(-680, -140), new Vector2(160, 110), finger);
        Txt("Choose_finger2_Text", f2.transform, Center, Vector2.zero, new Vector2(160, 110), "☞", 80, Color.black);
        f2.SetActive(false);   // 最初は「一人で遊ぶ」を選択中
        return panel;
    }

    // ===================== 攻守決めフェーズ選択画面 =====================
    static GameObject BuildKoushuSelect(Transform root, out GameObject[] choose, out GameObject[] crush, out GameObject[] cost)
    {
        var panel = PanelRoot("Panel_KoushuSelect", root);
        var t = panel.transform;

        var dec = Img("Decision", t, new Vector2(0.5f, 1f), new Vector2(0, -270), new Vector2(900, 100), Yamabuki);
        Txt("Decision_Text", dec.transform, Center, Vector2.zero, new Vector2(900, 100), "先攻、後攻を決めるぞ！", 56, Color.black);
        var three = Img("Three_choice", t, new Vector2(0.5f, 1f), new Vector2(0, -370), new Vector2(700, 70), Yamabuki);
        Txt("Three_choice_Text", three.transform, Center, Vector2.zero, new Vector2(700, 70), "3つの中から一つ選ぼう", 40, Color.black);

        string[] names = { "One", "Two", "Three" };
        string[] labels = { "疾風デュクシ", "剛腕デュクシ", "鉄壁デュクシ" };
        string[] costs = { "コスト3", "コスト5", "コスト4" };
        Vector2[] pos = { new Vector2(0, 20), new Vector2(-400, -260), new Vector2(400, -260) };

        choose = new GameObject[3]; crush = new GameObject[3]; cost = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            choose[i] = Img($"Choose_{names[i]}", t, Center, pos[i], new Vector2(320, 170), Wood);
            Txt($"Choose_{names[i]}_Text", choose[i].transform, Center, Vector2.zero, new Vector2(320, 170), labels[i], 40, Color.black);

            crush[i] = Img($"Crush_{names[i]}", t, Center, pos[i], new Vector2(320, 170), WoodDark);
            Txt($"Crush_{names[i]}_Text", crush[i].transform, Center, Vector2.zero, new Vector2(320, 170), "バキッ！", 48, Color.white);
            crush[i].SetActive(false);

            cost[i] = Img($"Cost_{names[i]}", t, Center, pos[i] + new Vector2(270, 0), new Vector2(160, 70), Color.white);
            Txt($"Cost_{names[i]}_Text", cost[i].transform, Center, Vector2.zero, new Vector2(160, 70), costs[i], 32, Color.black);
            cost[i].SetActive(false);
        }
        return panel;
    }

    // ===================== 攻守決めフェーズ勝負画面 =====================
    static GameObject BuildKoushuBattle(Transform root)
    {
        var panel = PanelRoot("Panel_KoushuBattle", root);
        var t = panel.transform;

        var r1 = Img("Battle_1P", t, Center, new Vector2(-420, -40), new Vector2(320, 170), Wood);
        Txt("Battle_1P_Text", r1.transform, Center, Vector2.zero, new Vector2(320, 170), "1Pの選択", 40, Color.black);
        var r2 = Img("Battle_2P", t, Center, new Vector2(420, -40), new Vector2(320, 170), Wood);
        Txt("Battle_2P_Text", r2.transform, Center, Vector2.zero, new Vector2(320, 170), "2Pの選択", 40, Color.black);
        Txt("VS_Text", t, Center, new Vector2(0, -40), new Vector2(200, 120), "VS", 90, Yamabuki);

        Txt("Wait_Text", t, Center, new Vector2(0, 200), new Vector2(1200, 90), "相手の選択を待っています…", 52, Color.black);
        var ready = Txt("Ready_Text", t, Center, new Vector2(0, 200), new Vector2(1200, 90), "両者そろった！ 勝負！", 64, Yamabuki);
        ready.SetActive(false);
        return panel;
    }

    // ===================== デュクシ選択フェーズ =====================
    static GameObject BuildDhukusiSelect(Transform root, string name, string guide, out Image[] imgs, out Image gauge)
    {
        var panel = PanelRoot(name, root);
        var t = panel.transform;

        Txt("Guide_Text", t, new Vector2(0.5f, 0f), new Vector2(0, 280), new Vector2(1400, 70), guide, 44, Color.black);

        var bar = Img("Select_Bar", t, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1560, 170), Panel);
        string[] names = { "Normal", "Feint", "Renzoku", "Gekiomo" };   // DhukusiType と同じ順番
        string[] labels = { "通常デュクシ\nコスト1", "フェイントデュクシ\nコスト5", "連続デュクシ\nコスト3", "激重デュクシ\nコスト7" };
        Color[] colors = { Blue, Green, Yellow, Red };
        string[] btn = { "上", "左", "右", "下" };

        imgs = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var go = Img($"Dhukusi_{names[i]}", bar.transform, Center, new Vector2(-570 + i * 380, 0), new Vector2(340, 120), colors[i]);
            Txt($"Dhukusi_{names[i]}_Text", go.transform, Center, new Vector2(0, 8), new Vector2(340, 100), labels[i], 32, i == 2 ? Color.black : Color.white);
            Txt($"Dhukusi_{names[i]}_Button", go.transform, new Vector2(0f, 1f), new Vector2(30, -20), new Vector2(60, 40), btn[i], 26, Color.black);
            imgs[i] = go.GetComponent<Image>();
        }

        // 長押しキャンセルのゲージ
        var gaugeBg = Img("CancelGauge_BG", t, new Vector2(0.5f, 0f), new Vector2(0, 225), new Vector2(600, 18), new Color(0, 0, 0, 0.3f));
        var g = Img("CancelGauge", gaugeBg.transform, Center, Vector2.zero, new Vector2(600, 18), Red);
        gauge = g.GetComponent<Image>();
        gauge.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        gauge.type = Image.Type.Filled;
        gauge.fillMethod = Image.FillMethod.Horizontal;
        gauge.fillAmount = 0f;
        return panel;
    }

    // ===================== デュクシ繰り出しフェーズ =====================
    static GameObject BuildKuridashi(Transform root, string name, string guide)
    {
        var panel = PanelRoot(name, root);
        var t = panel.transform;

        Txt("Guide_Text", t, new Vector2(0.5f, 0f), new Vector2(0, 230), new Vector2(1400, 70), guide, 44, Color.black);

        // 発生タイミングバー（企画書の並び：通常 → フェイント → 連続 → 激重）
        var bg = Img("TimingBar", t, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1500, 110), Panel);
        string[] labels = { "つうじょう", "フェイント", "れんぞく", "げきおも" };
        Color[] colors = { Blue, Green, Yellow, Red };
        float[] widths = { 280, 280, 420, 420 };   // 仮：0〜1秒 / 1〜2秒 / 2〜3.5秒 / 3.5〜5秒
        float x = -700;
        for (int i = 0; i < 4; i++)
        {
            var seg = Img($"Bar_{i}", bg.transform, Center, new Vector2(x + widths[i] / 2, 10), new Vector2(widths[i], 50), colors[i]);
            Txt($"Bar_{i}_Text", seg.transform, Center, Vector2.zero, new Vector2(widths[i], 50), labels[i], 26, i == 2 ? Color.black : Color.white);
            x += widths[i];
        }
        Img("Bar_Cursor", bg.transform, Center, new Vector2(-700, 10), new Vector2(8, 70), Color.black);
        Txt("Bar_Time_Label", bg.transform, new Vector2(0f, 0f), new Vector2(100, 18), new Vector2(200, 30), "経過時間↑", 22, Color.black);

        // 判定（デュクシ！ / 防御成功！）
        var judge = Txt("Judge_Text", t, Center, new Vector2(0, 120), new Vector2(1200, 180), "デュクシ！", 140, Yamabuki);
        judge.SetActive(false);
        return panel;
    }

    // ===================== 勝敗判定画面 =====================
    static GameObject BuildResult(Transform root)
    {
        var panel = PanelRoot("Panel_Result", root);
        var t = panel.transform;

        var dark = Img("Result_BG", t, Center, Vector2.zero, new Vector2(1920, 1080), new Color(0, 0, 0, 0.6f));
        dark.transform.SetAsFirstSibling();
        Txt("Result_Text", t, Center, new Vector2(0, 80), new Vector2(1400, 220), "1P WIN!", 180, Yamabuki);
        Txt("Result_Sub_Text", t, Center, new Vector2(0, -120), new Vector2(1400, 80), "セット 2 - 1", 56, Color.white);
        Txt("Result_Next_Text", t, Center, new Vector2(0, -300), new Vector2(1400, 60), "Aボタンでタイトルへ", 40, Color.white);
        return panel;
    }

    // ===================== プレビュー =====================
    [MenuItem("DHUKUSI/プレビュー/タイトル")] static void P8() => Preview(PhaseUIManager.UIPhase.Title);
    [MenuItem("DHUKUSI/プレビュー/モード選択")] static void P9() => Preview(PhaseUIManager.UIPhase.ModeSelect);
    [MenuItem("DHUKUSI/プレビュー/攻守決め 選択")] static void P1() => Preview(PhaseUIManager.UIPhase.KoushuSelect);
    [MenuItem("DHUKUSI/プレビュー/攻守決め 勝負")] static void P2() => Preview(PhaseUIManager.UIPhase.KoushuBattle);
    [MenuItem("DHUKUSI/プレビュー/デュクシ選択 攻撃側")] static void P3() => Preview(PhaseUIManager.UIPhase.DhukusiSelectAttack);
    [MenuItem("DHUKUSI/プレビュー/デュクシ選択 防御側")] static void P4() => Preview(PhaseUIManager.UIPhase.DhukusiSelectDefense);
    [MenuItem("DHUKUSI/プレビュー/繰り出し 攻撃側")] static void P5() => Preview(PhaseUIManager.UIPhase.KuridashiAttack);
    [MenuItem("DHUKUSI/プレビュー/繰り出し 防御側")] static void P6() => Preview(PhaseUIManager.UIPhase.KuridashiDefense);
    [MenuItem("DHUKUSI/プレビュー/勝敗判定")] static void P7() => Preview(PhaseUIManager.UIPhase.Result);

    static void Preview(PhaseUIManager.UIPhase phase)
    {
        var m = Object.FindFirstObjectByType<PhaseUIManager>();
        if (m == null) { Debug.LogWarning("[DHUKUSI] PhaseUIManager がシーンにありません。先に「UIパネルを全部作る」を実行してください"); return; }

        var so = new SerializedObject(m);
        var list = so.FindProperty("panels");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            var go = e.FindPropertyRelative("panel").objectReferenceValue as GameObject;
            if (go != null) go.SetActive(e.FindPropertyRelative("phase").enumValueIndex == (int)phase);
        }
        var hud = so.FindProperty("hud").objectReferenceValue as GameObject;
        if (hud != null) hud.SetActive(phase != PhaseUIManager.UIPhase.Title && phase != PhaseUIManager.UIPhase.ModeSelect);
        EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
    }

    // ===================== 作るための関数 =====================
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static GameObject PanelRoot(string name, Transform parent)
    {
        var go = Rect(name, parent, Center, Vector2.zero, Vector2.zero);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;   // 画面いっぱい
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = Center;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static GameObject Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = Rect(name, parent, anchor, pos, size);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject Txt(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, Color color)
    {
        var go = Rect(name, parent, anchor, pos, size);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return go;
    }

    static void AddSelectUI(GameObject panel, Image[] imgs, Image gauge)
    {
        var ui = panel.AddComponent<DhukusiSelectUI>();
        var so = new SerializedObject(ui);
        var arr = so.FindProperty("dhukusiImages");
        arr.arraySize = imgs.Length;
        for (int i = 0; i < imgs.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = imgs[i];
        so.FindProperty("cancelGauge").objectReferenceValue = gauge;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray(SerializedProperty prop, GameObject[] objs)
    {
        prop.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
#endif