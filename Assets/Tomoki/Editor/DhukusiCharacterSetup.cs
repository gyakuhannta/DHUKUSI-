#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 相手キャラ（Mixamo のモデルとアニメーション 5つ）を動くようにする
///
/// メニュー「DHUKUSI → 相手キャラを動くようにする」
///  ・taki / Nomal / feint / rennzoku / gekiomo の FBX を、プロジェクトのどこにあっても探す
///  ・FBX を Humanoid にする（taki＝待機はループ、その場で動く）
///  ・アニメーターを作る：待機 → 技のトリガー（Normal / Feint / Renzoku / Gekiomo）で技 → 終わったら待機
///  ・今開いているシーンのキャラに付ける
///      Hierarchy でキャラを選んでいればそれに、選んでいなければシーンの中の taki を探す
///      シーンに無ければ、ステージのまん中にカメラの方を向けて置く
///  ・EnemyCharacterAnim を付ける（相手の攻撃に合わせて技のアニメーションをする）
///  ・何回押しても同じ結果になる
/// </summary>
public static class DhukusiCharacterSetup
{
    const string ObjectName = "EnemyCharacter";

    //  FBX の名前, ステートの名前, トリガー
    static readonly (string file, string state, string trigger)[] Moves =
    {
        ("Nomal",    "通常",       "Normal"),
        ("feint",    "フェイント", "Feint"),
        ("rennzoku", "連続",       "Renzoku"),
        ("gekiomo",  "激重",       "Gekiomo"),
    };

    [MenuItem("DHUKUSI/相手キャラを動くようにする")]
    public static void Setup()
    {
        AssetDatabase.Refresh();

        // ---- 1. FBX をさがして設定する ----
        string takiPath = FindModel("taki");
        if (takiPath == null)
        {
            EditorUtility.DisplayDialog("DHUKUSI", "taki.fbx がプロジェクトにありません", "OK");
            return;
        }
        string folder = System.IO.Path.GetDirectoryName(takiPath).Replace('\\', '/');
        string controllerPath = folder + "/EnemyCharacter.controller";

        SetupModel(takiPath, true);
        foreach (var (file, _, _) in Moves)
        {
            var p = FindModel(file);
            if (p != null) SetupModel(p, false);
        }
        var idle = LoadClip(takiPath);
        if (idle == null)
        {
            EditorUtility.DisplayDialog("DHUKUSI", "taki.fbx にアニメーションがありません", "OK");
            return;
        }

        // ---- 2. アニメーター ----
        AssetDatabase.DeleteAsset(controllerPath);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var sm = ac.layers[0].stateMachine;
        var idleState = sm.AddState("待機");
        idleState.motion = idle;
        sm.defaultState = idleState;
        foreach (var (file, state, trigger) in Moves)
        {
            var path = FindModel(file);
            var clip = path != null ? LoadClip(path) : null;
            if (clip == null) { Debug.LogWarning($"[DHUKUSI] {file}.fbx のアニメーションがありません"); continue; }
            ac.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
            var s = sm.AddState(state);
            s.motion = clip;
            var to = sm.AddAnyStateTransition(s);
            to.AddCondition(AnimatorConditionMode.If, 0, trigger);
            to.hasExitTime = false;
            to.duration = 0.1f;
            to.canTransitionToSelf = false;
            var back = s.AddTransition(idleState);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.15f;
        }
        AssetDatabase.SaveAssets();

        // ---- 3. 今のシーンのキャラに付ける ----
        var scene = EditorSceneManager.GetActiveScene();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(takiPath);
        var go = FindCharacterInScene(model);
        if (go == null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = ObjectName;
            Place(go);
        }
        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = ac;
        animator.applyRootMotion = false;
        if (go.GetComponent<EnemyCharacterAnim>() == null) go.AddComponent<EnemyCharacterAnim>();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = go;
        Debug.Log($"[DHUKUSI] 「{go.name}」が相手の攻撃に合わせて動くようにしました。Ctrl+S でシーンを保存してください");
    }

    static string FindModel(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p).ToLower() == name.ToLower()) return p;
        }
        return null;
    }

    // シーンの中のキャラ：選んでいる物 → taki のモデルから作った物 → 名前が EnemyCharacter の物
    static GameObject FindCharacterInScene(GameObject model)
    {
        var sel = Selection.activeGameObject;
        if (sel != null && sel.scene.IsValid() && sel.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            return sel;
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
                if (src == model && PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) == t.gameObject) return t.gameObject;
                if (t.name == ObjectName) return t.gameObject;
            }
        return null;
    }

    // Humanoid にする。待機はループさせる
    static void SetupModel(string path, bool loop)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) { Debug.LogWarning("[DHUKUSI] " + path + " がありません"); return; }
        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (var c in clips)
        {
            if (c.loopTime != loop) { c.loopTime = loop; changed = true; }
            // その場で動く（体の向き・高さ・前後がずれない）
            if (!c.lockRootRotation || !c.lockRootHeightY || !c.lockRootPositionXZ)
            {
                c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = c.keepOriginalPositionY = c.keepOriginalPositionXZ = true;
                changed = true;
            }
        }
        importer.clipAnimations = clips;
        if (changed) importer.SaveAndReimport();
    }

    static AnimationClip LoadClip(string path)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
        return null;
    }

    // カメラの画面のまん中の少し下に見える床の上に、カメラの方を向けて立たせる
    static void Place(GameObject go)
    {
        var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (cam == null) return;

        // 床の高さ（GameStage の地面）
        float floorY = 0f;
        var stage = GameObject.Find("GameStage");
        if (stage != null)
        {
            Bounds b = default; bool has = false;
            foreach (var r in stage.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.Contains("Ground"))
                    {
                        if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
                    }
            if (has) floorY = b.size.y > Mathf.Max(b.extents.x, b.extents.z) * 0.2f ? b.center.y : b.max.y;
        }

        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.4f, 0f));
        var plane = new Plane(Vector3.up, new Vector3(0, floorY, 0));
        Vector3 pos;
        if (plane.Raycast(ray, out float hit)) pos = ray.GetPoint(hit);
        else pos = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 5f;
        pos.y = floorY;

        go.transform.position = pos;
        var look = cam.transform.position - pos; look.y = 0;
        if (look.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(look);

        // 大きさ：画面の高さの 4割くらいに見えるように
        float dist = Vector3.Distance(cam.transform.position, pos + Vector3.up * 0.9f);
        float visible = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float height = 1.8f;
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds mb = rends[0].bounds;
            foreach (var r in rends) mb.Encapsulate(r.bounds);
            if (mb.size.y > 0.01f) height = mb.size.y;
        }
        go.transform.localScale *= (visible * 0.4f) / height;
    }
}
#endif
