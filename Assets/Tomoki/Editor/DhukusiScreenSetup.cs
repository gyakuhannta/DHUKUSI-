#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレハブ DHUKUSI_UI に、タイトル・モード選択のスクリプトと背景を付ける
///
/// メニュー「DHUKUSI → タイトルとモード選択を動くようにする」
///  ・Panel_Title に TitleUI を付けて、Press_A を登録
///  ・Panel_ModeSelect に ModeSelectUI を付けて、One_people / Two_people と指を登録
///  ・Assets/Tomoki/Images/Background.png があれば、全画面の一番後ろに背景として敷く
///    （Teams の共有から背景画像をダウンロードして、この名前で置いてから実行）
/// </summary>
public static class DhukusiScreenSetup
{
    const string PrefabPath     = "Assets/Tomoki/Prefabs/DHUKUSI_UI.prefab";
    const string BackgroundPath = "Assets/Tomoki/Images/Background.png";

    [MenuItem("DHUKUSI/タイトルとモード選択を動くようにする")]
    public static void Setup()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // ---- タイトル ----
            var title = Find(root.transform, "Panel_Title");
            if (title != null)
            {
                var ui = title.GetComponent<TitleUI>() ?? title.gameObject.AddComponent<TitleUI>();
                var so = new SerializedObject(ui);
                var pressA = Find(title, "Press_A");
                so.FindProperty("pressA").objectReferenceValue = pressA != null ? pressA.gameObject : null;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[DHUKUSI] Panel_Title に TitleUI を付けました");
            }

            // ---- モード選択 ----
            var mode = Find(root.transform, "Panel_ModeSelect");
            if (mode != null)
            {
                var ui = mode.GetComponent<ModeSelectUI>() ?? mode.gameObject.AddComponent<ModeSelectUI>();
                var so = new SerializedObject(ui);
                SetArray(so.FindProperty("options"), Find(mode, "One_people"), Find(mode, "Two_people"));
                SetArray(so.FindProperty("fingers"), Find(mode, "Choose_finger1"), Find(mode, "Choose_finger2"));
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[DHUKUSI] Panel_ModeSelect に ModeSelectUI を付けました");
            }

            // ---- 背景 ----
            if (File.Exists(BackgroundPath))
            {
                var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                var bg = root.transform.Find("Background");
                if (bg == null)
                {
                    var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
                    bg = go.transform;
                    bg.SetParent(root.transform, false);
                }
                bg.SetAsFirstSibling();   // 一番後ろ
                var rt = (RectTransform)bg;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = bg.GetComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
                img.color = Color.white;
                img.raycastTarget = false;
                Debug.Log("[DHUKUSI] 背景を敷きました");
            }
            else
            {
                Debug.LogWarning("[DHUKUSI] 背景画像がまだありません。Teams から背景をダウンロードして " + BackgroundPath + " に置いてから、もう一度実行してください");
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[DHUKUSI] " + PrefabPath + " を保存しました");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Transform Find(Transform parent, string name)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        Debug.LogWarning("[DHUKUSI] " + name + " が見つかりません");
        return null;
    }

    static void SetArray(SerializedProperty prop, params Transform[] items)
    {
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i] != null ? items[i].gameObject : null;
    }
}
#endif
