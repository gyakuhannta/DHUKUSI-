#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Teams の共有からダウンロードした素材（フォント・イラスト）を取り込んで、UI に反映する
///
/// 【使い方】
///  1. Teams の共有からフォントとイラストをダウンロードしておく（ダウンロードフォルダに入る）
///       ・01.全体連絡 → 03.フォント → DelaSukoGothic → DelaSukoGothicOne-R.ttf
///       ・01.全体連絡 → 02.イラスト関連 → 02.文字系統 → 全部（zip のままでOK）
///  2. メニュー「DHUKUSI → 素材を取り込む（ダウンロードフォルダから）」
///  3. メニュー「DHUKUSI → フォントと画像をUIに反映してプレハブ保存」
///
///  ・フォント  → Assets/Fonts
///  ・イラスト  → Assets/Tomoki/Images（Sprite として取り込む）
///  ・プレハブ  → Assets/Tomoki/Prefabs/DHUKUSI_UI.prefab
///    ほかの人はこのプレハブをシーンにドラッグするだけで UI が使える
/// </summary>
public static class DhukusiAssetImporter
{
    public const string FontPath   = "Assets/Fonts/DelaSukoGothicOne-R.ttf";
    const string ImageDir          = "Assets/Tomoki/Images";
    const string PrefabPath        = "Assets/Tomoki/Prefabs/DHUKUSI_UI.prefab";

    // UI の名前 → 画像の名前（仕様書のとおり）
    static readonly Dictionary<string, string> SpriteMap = new Dictionary<string, string>
    {
        { "Title",          "Title" },
        { "Press_A",        "Press_A" },
        { "Decision",       "Decision" },
        { "Three_choice",   "Three_choice" },
        { "One_people",     "One_people" },
        { "Two_people",     "Two_people" },
        { "Choose_finger1", "Choose_finger" },
        { "Choose_finger2", "Choose_finger" },
    };

    // 届いた画像のファイル名が仕様書の名前と違うもの
    static readonly Dictionary<string, string> FileNameAlias = new Dictionary<string, string>
    {
        { "Two_people", "Twe_people" },
    };

    // ===================== 取り込み =====================
    [MenuItem("DHUKUSI/素材を取り込む（ダウンロードフォルダから）")]
    public static void ImportFromDownloads()
    {
        string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        int count = 0;

        // ---- フォント ----
        string fontSrc = Path.Combine(downloads, "DelaSukoGothicOne-R.ttf");
        if (File.Exists(fontSrc))
        {
            Directory.CreateDirectory("Assets/Fonts");
            File.Copy(fontSrc, FontPath, true);
            count++;
            Debug.Log("[DHUKUSI] フォントを取り込みました: " + FontPath);
        }
        else Debug.LogWarning("[DHUKUSI] ダウンロードフォルダに DelaSukoGothicOne-R.ttf がありません");

        // ---- イラスト（PNG がそのまま置いてある場合） ----
        Directory.CreateDirectory(ImageDir);
        var wanted = new HashSet<string>(SpriteMap.Values) { "Impact1", "Impact2" };
        foreach (var name in wanted)
        {
            string src = Path.Combine(downloads, name + ".png");
            if (!File.Exists(src)) continue;
            File.Copy(src, Path.Combine(ImageDir, name + ".png"), true);
            count++;
        }

        // ---- イラスト（Teams でまとめてダウンロードした zip の場合。一番新しい zip を見る） ----
        var zip = new DirectoryInfo(downloads).GetFiles("OneDrive_*.zip")
                    .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
        if (zip != null)
        {
            using (var fs = File.OpenRead(zip.FullName))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    string fileName = Path.GetFileName(entry.FullName);
                    if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                    using (var input = entry.Open())
                    using (var output = File.Create(Path.Combine(ImageDir, fileName)))
                        input.CopyTo(output);
                    count++;
                }
            }
            Debug.Log("[DHUKUSI] zip からイラストを取り込みました: " + zip.Name);
        }

        AssetDatabase.Refresh();

        // ---- PNG を Sprite にする ----
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ImageDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        Debug.Log($"[DHUKUSI] 素材を {count} 個 取り込みました。次は「フォントと画像をUIに反映してプレハブ保存」");
    }

    // ===================== UI に反映 =====================
    [MenuItem("DHUKUSI/フォントと画像をUIに反映してプレハブ保存")]
    public static void ApplyAndSavePrefab()
    {
        var canvas = GameObject.Find("DHUKUSI_Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("DHUKUSI", "シーンに DHUKUSI_Canvas がありません。\n先に「UIパネルを全部作る」を実行してください。", "OK");
            return;
        }

        // ---- フォント ----
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        int texts = 0;
        if (font != null)
        {
            foreach (var t in canvas.GetComponentsInChildren<Text>(true))
            {
                Undo.RecordObject(t, "フォント変更");
                t.font = font;
                texts++;
            }
        }
        else Debug.LogWarning("[DHUKUSI] フォントがありません。先に「素材を取り込む」を実行してください");

        // ---- 画像 ----
        int images = 0;
        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (!SpriteMap.TryGetValue(img.gameObject.name, out var spriteName)) continue;
            string path = $"{ImageDir}/{spriteName}.png";
            // 画像のファイル名が仕様書と違うもの（Two_people が Twe_people になっている など）
            if (!File.Exists(path) && FileNameAlias.TryGetValue(spriteName, out var alt))
                path = $"{ImageDir}/{alt}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                // まだ Sprite になっていなければ、ここで Sprite にする
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
            }

            Undo.RecordObject(img, "画像変更");
            img.sprite = sprite;
            img.color = Color.white;      // 仮の色が画像に混ざらないように
            img.preserveAspect = true;    // 縦横の比率を保つ

            // 仮の文字（〇〇_Text）は非表示にする
            var placeholder = img.transform.Find(img.gameObject.name + "_Text");
            if (placeholder != null) placeholder.gameObject.SetActive(false);
            images++;
        }

        // ---- プレハブとして保存 ----
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, PrefabPath, InteractionMode.UserAction);
        EditorSceneManager.MarkSceneDirty(canvas.scene);

        Debug.Log($"[DHUKUSI] 文字 {texts} 個にフォント、画像 {images} 個を反映して、{PrefabPath} に保存しました");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
    }
}
#endif
