#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 文字が □ になる（フォントに無い文字がある）のを直して、チームのフォント（でらすこゴシック）にそろえる
///
/// メニュー「DHUKUSI → フォントを直す（でらすこゴシック）」
///  ・DelaSukoGothicOne-R.ttf から TextMeshPro 用のフォント（Dynamic）を作る
///      Dynamic なので、使った文字はその場で追加される（「ー」「：」なども □ にならない）
///  ・Assets/ARATAScript と Assets/Main_game の中のプレハブ・シーンの TextMeshPro の文字を全部このフォントにする
///  ・TextMeshPro の初期フォントもこれにする（これから作る文字もこのフォントになる）
/// </summary>
public static class DhukusiFontFixer
{
    const string TtfPath   = "Assets/Fonts/DelaSukoGothicOne-R.ttf";
    const string AssetPath = "Assets/Fonts/DelaSukoGothicOne-R SDF.asset";
    static readonly string[] Folders = { "Assets/ARATAScript", "Assets/Main_game" };

    [MenuItem("DHUKUSI/フォントを直す（でらすこゴシック）")]
    public static void Fix()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var fontAsset = GetOrCreateFontAsset();
        if (fontAsset == null) return;

        int texts = 0;

        // ---- プレハブ ----
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", Folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            int n = Apply(root.GetComponentsInChildren<TMP_Text>(true), fontAsset);
            if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            texts += n;
        }

        // ---- シーン ----
        string current = EditorSceneManager.GetActiveScene().path;
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", Folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var list = new List<TMP_Text>();
            foreach (var go in scene.GetRootGameObjects()) list.AddRange(go.GetComponentsInChildren<TMP_Text>(true));
            int n = Apply(list, fontAsset);
            if (n > 0) EditorSceneManager.SaveScene(scene);
            texts += n;
        }
        if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);

        // ---- 初期フォント ----
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            var p = so.FindProperty("m_defaultFontAsset");
            if (p != null) { p.objectReferenceValue = fontAsset; so.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DHUKUSI] TextMeshPro の文字 {texts} 個を でらすこゴシック にしました");
    }

    static int Apply(IEnumerable<TMP_Text> list, TMP_FontAsset fontAsset)
    {
        int n = 0;
        foreach (var t in list)
        {
            if (t.font == fontAsset) continue;
            t.font = fontAsset;
            t.fontSharedMaterial = fontAsset.material;
            EditorUtility.SetDirty(t);
            n++;
        }
        return n;
    }

    static TMP_FontAsset GetOrCreateFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("DHUKUSI", TtfPath + " がありません", "OK");
            return null;
        }

        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
                                               AtlasPopulationMode.Dynamic, true);
        fa.name = Path.GetFileNameWithoutExtension(AssetPath);
        AssetDatabase.CreateAsset(fa, AssetPath);

        // アトラス画像とマテリアルをフォントの中に保存する
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
    }
}
#endif
