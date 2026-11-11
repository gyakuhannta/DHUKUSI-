#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 仕様書「デュクシ選択フェーズ(攻撃側)」の見た目に DHUKUSI_UI プレハブをそろえる
///
/// メニュー「DHUKUSI → 画像（木の板など）を反映する」
///  ・左上：青い枠の中に Life_1 / Life_2（Eyes）、その右に Set1（Set）
///  ・右上：青い枠の中に Life_3 / Life_4（Eyes）、その左に Set2（Set）
///  ・上のまん中：灰色の枠に「残りコスト」と「5/10」
///  ・下：薄い灰色の枠の中に木の板 4枚（2×2）
///  ・右：青い吹き出し「繰り出す予定のデュクシを選ぼう！」
///  ・攻守決めの板（Choose_*）は Dhukushi_wood、決定したら Crush_wood
///  ・何回押しても同じ結果になる
/// </summary>
public static class DhukusiSkinApply
{
    const string PrefabPath = "Assets/Tomoki/Prefabs/DHUKUSI_UI.prefab";
    const string ImageDir   = "Assets/Tomoki/Images";

    // 仕様書の色
    static readonly Color Blue      = new Color32(0x44, 0x72, 0xC4, 0xff);
    static readonly Color CostGray  = new Color32(0xAE, 0xAA, 0xAA, 0xff);
    static readonly Color PanelGray = new Color32(0xD9, 0xD9, 0xD9, 0xff);

    [MenuItem("DHUKUSI/画像（木の板など）を反映する")]
    public static void Apply()
    {
        AssetDatabase.Refresh();
        var wood  = LoadSprite("Dhukushi_wood");
        var crush = LoadSprite("Crush_wood");
        var set   = LoadSprite("Set");
        var eyes  = LoadSprite("Eyes");
        var round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");   // 角丸の枠

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // ---- 木の板 ----
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                string n = img.name;
                if ((n.StartsWith("Choose_") && !n.StartsWith("Choose_finger")) || n == "Battle_1P" || n == "Battle_2P"
                    || (n.StartsWith("Dhukusi_") && !n.EndsWith("_Text") && !n.EndsWith("_Button")))
                {
                    SetSprite(img, wood);
                    SetChildTextColor(img.transform, Color.black);
                }
                else if (n.StartsWith("Crush_") && !n.EndsWith("_Text"))
                {
                    SetSprite(img, crush);
                    var t = img.transform.Find(n + "_Text");
                    if (t != null) t.gameObject.SetActive(false);
                }
            }

            // 攻守決め：上の文字がコストの枠と重ならないように下げる
            var koushu = Find(root.transform, "Panel_KoushuSelect");
            if (koushu != null)
            {
                MoveY(koushu, "Decision", -350);
                MoveY(koushu, "Three_choice", -425);
                foreach (var n in new[] { "Choose_One", "Crush_One", "Cost_One" }) MoveY(koushu, n, -40);
            }

            var hud = Find(root.transform, "HUD");
            if (hud != null) LayoutHud(hud, eyes, set, round);

            LayoutSelect(Find(root.transform, "Panel_DhukusiSelect_Attack"), round, "繰り出す予定のデュクシを選ぼう！");
            LayoutSelect(Find(root.transform, "Panel_DhukusiSelect_Defense"), round, "相手のデュクシを読んで、同じボタンで防ごう！");

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        if (eyes == null) Debug.LogWarning("[DHUKUSI] Eyes.png がないので、ライフは仮の四角のままです");
        Debug.Log("[DHUKUSI] 仕様書のレイアウトにしました（" + PrefabPath + "）");
    }

    // ===================== 上の HUD =====================
    static void LayoutHud(Transform hud, Sprite eyes, Sprite set, Sprite round)
    {
        // ライフの枠（左上・右上）
        LifeBox(Find(hud, "Life_1P"), true,  eyes, round);
        LifeBox(Find(hud, "Life_2P"), false, eyes, round);

        // セット（枠のすぐ内側寄り。1人 1つ）
        SetMark(Find(hud, "Set_1P_1"), true,  set, true);
        SetMark(Find(hud, "Set_2P_1"), false, set, true);
        SetMark(Find(hud, "Set_1P_2"), true,  set, false);   // 2セット先取なので 2つめは出番がない
        SetMark(Find(hud, "Set_2P_2"), false, set, false);

        // 残りコスト（上のまん中、灰色の角丸）
        var cost = Find(hud, "Cost");
        if (cost != null)
        {
            var rt = Place(cost, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(580, 215));
            var img = cost.GetComponent<Image>();
            if (img != null) Rounded(img, round, CostGray);
            var label = Find(cost, "Cost_Label");
            if (label != null) { Place(label, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(560, 50)); Style(label, "残りコスト", 40, Color.black); }
            var text = Find(cost, "Cost_Text");
            if (text != null) { Place(text, new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(560, 130)); Style(text, null, 110, Color.black); }
        }

        // 経過時間（仕様書の画面には無いので、右下の角に小さく）
        var timer = Find(hud, "Timer");
        if (timer != null)
        {
            Place(timer, new Vector2(1, 0), new Vector2(-20, 20), new Vector2(300, 46));
            var img = timer.GetComponent<Image>();
            if (img != null) Rounded(img, round, new Color(0, 0, 0, 0.55f));
            var t = Find(timer, "Timer_Text");
            if (t != null)
            {
                Place(t, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 46));
                Style(t, null, 30, Color.white);
                var o = t.GetComponent<Outline>();
                if (o != null) Object.DestroyImmediate(o);
            }
        }
    }

    static void LifeBox(Transform box, bool left, Sprite eyes, Sprite round)
    {
        if (box == null) return;
        Place(box, new Vector2(left ? 0 : 1, 1), new Vector2(left ? 66 : -66, -66), new Vector2(380, 160));
        var img = box.GetComponent<Image>();
        if (img == null) img = box.gameObject.AddComponent<Image>();
        Rounded(img, round, Blue);

        string p = box.name;   // Life_1P / Life_2P
        // 左上は左から、右上は右から並べる（減ったら内側から消える）
        float[] xs = left ? new[] { -88f, 88f } : new[] { 88f, -88f };
        for (int i = 0; i < 2; i++)
        {
            var life = Find(box, $"{p}_{i + 1}");
            if (life == null) continue;
            Place(life, new Vector2(0.5f, 0.5f), new Vector2(xs[i], 0), new Vector2(165, 121), new Vector2(0.5f, 0.5f));
            var li = life.GetComponent<Image>();
            if (li != null && eyes != null)
            {
                li.sprite = eyes;
                li.type = Image.Type.Simple;
                li.preserveAspect = true;
                li.color = Color.white;
            }
        }
        // 「あなた ライフ 2/2」の文字は仕様書に無いので消す
        var label = Find(box, p + "_Label");
        if (label != null) label.gameObject.SetActive(false);
    }

    static void SetMark(Transform t, bool left, Sprite set, bool show)
    {
        if (t == null) return;
        t.gameObject.SetActive(show);
        if (!show) return;
        // 枠の右上（2P は左上）の外側
        Place(t, new Vector2(left ? 0 : 1, 1), new Vector2(left ? 500 : -500, -95), new Vector2(110, 68), new Vector2(0.5f, 0.5f));
        var img = t.GetComponent<Image>();
        if (img != null && set != null)
        {
            img.sprite = set;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }
    }

    // ===================== 下の 4つのデュクシ =====================
    // ボタンの位置に合わせて並べる（左上 X、左下 Y、右上 A、右下 B）
    static void LayoutSelect(Transform panel, Sprite round, string guide)
    {
        if (panel == null) return;

        var bar = Find(panel, "Select_Bar");
        if (bar != null)
        {
            Place(bar, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(1680, 320), new Vector2(0.5f, 0.5f));
            var img = bar.GetComponent<Image>();
            if (img != null) Rounded(img, round, PanelGray);

            //  名前, x, y, 文字（いまのボタン割り当てのとおり：Y＝通常 / X＝フェイント / B＝連続 / A＝激重）
            Plank(bar, "Dhukusi_Feint",   -400,  72, "Xボタンでフェイントデュクシ");
            Plank(bar, "Dhukusi_Normal",  -400, -72, "Yボタンで通常デュクシ");
            Plank(bar, "Dhukusi_Gekiomo",  400,  72, "Aボタンで激重デュクシ");
            Plank(bar, "Dhukusi_Renzoku",  400, -72, "Bボタンで連続デュクシ");
        }

        // 右の青い吹き出し
        var g = Find(panel, "Guide_Bubble");
        if (g == null)
        {
            var go = new GameObject("Guide_Bubble", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            g = go.transform;
        }
        Place(g, new Vector2(1, 1), new Vector2(-8, -340), new Vector2(560, 190), new Vector2(1, 1));
        Rounded(g.GetComponent<Image>(), round, Blue);
        g.GetComponent<Image>().raycastTarget = false;

        var gt = Find(panel, "Guide_Text");
        if (gt != null)
        {
            gt.SetParent(g, false);
            Place(gt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 160), new Vector2(0.5f, 0.5f));
            var t = gt.GetComponent<Text>();
            if (t != null)
            {
                t.text = guide;
                t.fontSize = 40;
                t.color = Color.white;
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }
    }

    static void Plank(Transform bar, string name, float x, float y, string label)
    {
        var p = Find(bar, name);
        if (p == null) return;
        Place(p, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(620, 130), new Vector2(0.5f, 0.5f));
        var t = Find(p, name + "_Text");
        if (t != null)
        {
            Place(t, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(580, 110), new Vector2(0.5f, 0.5f));
            var tx = t.GetComponent<Text>();
            if (tx != null)
            {
                tx.text = label;
                tx.fontSize = 40;
                tx.color = Color.black;
                tx.alignment = TextAnchor.MiddleCenter;
                tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }
        // 左上の小さい「上」「左」は文字にボタン名が入ったので消す
        var b = Find(p, name + "_Button");
        if (b != null) b.gameObject.SetActive(false);
    }

    // ===================== 小物 =====================
    static void MoveY(Transform parent, string name, float y)
    {
        var t = Find(parent, name) as RectTransform;
        if (t == null) return;
        t.anchoredPosition = new Vector2(t.anchoredPosition.x, y);
    }

    static RectTransform Place(Transform t, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        return rt;
    }

    static void Rounded(Image img, Sprite round, Color color)
    {
        if (img == null) return;
        img.sprite = round;
        img.type = round != null ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 0.35f;   // 角を大きめに丸く
        img.color = color;
    }

    static void Style(Transform t, string text, int size, Color color)
    {
        var tx = t.GetComponent<Text>();
        if (tx == null) return;
        if (text != null) tx.text = text;
        tx.fontSize = size;
        tx.color = color;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        var o = tx.GetComponent<Outline>();
        if (o != null && color == Color.black) Object.DestroyImmediate(o);
    }

    static Transform Find(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void SetSprite(Image img, Sprite sprite)
    {
        if (sprite == null) return;
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.preserveAspect = false;
    }

    static void SetChildTextColor(Transform parent, Color color)
    {
        foreach (var t in parent.GetComponentsInChildren<Text>(true))
        {
            if (t.name.EndsWith("_Button")) continue;
            t.color = color;
        }
    }

    static Sprite LoadSprite(string name)
    {
        string path = $"{ImageDir}/{name}.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[DHUKUSI] " + path + " がありません");
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
