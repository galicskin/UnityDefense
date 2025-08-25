#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(TowerBase), true)]
public class TowerBaseEditor : Editor
{


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //bluePreviewMat = (Material)EditorGUILayout.ObjectField("Blue Preview Mat", bluePreviewMat, typeof(Material), false);
        //redPreviewMat = (Material)EditorGUILayout.ObjectField("Red Preview Mat", redPreviewMat, typeof(Material), false);

        if (GUILayout.Button("Create Blue/Red Preview Prefabs"))
        {
            var go = ((TowerBase)target).gameObject;
            TowerPreviewGenerator.CreatePreviewPrefabs(go);
        }
    }
}


public static class TowerPreviewGenerator
{
    public static void CreatePreviewPrefabs(GameObject sourcePrefab)
    {
        var cfg = PreviewMatConfig.Instance;
        if (cfg == null || cfg.blueMat == null || cfg.redMat == null)
        {
            EditorUtility.DisplayDialog("Preview Prefab",
                "PreviewMatConfig이 없거나 Blue/Red 머티리얼이 비어 있습니다.\n" +
                "메뉴: Tools > Build Preview > Open or Create Config 에서 설정하세요.",
                "OK");
            return;
        }

        var srcPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
            PrefabUtility.GetCorrespondingObjectFromSource(sourcePrefab) ?? sourcePrefab
        );
        if (string.IsNullOrEmpty(srcPath))
        {
            EditorUtility.DisplayDialog("Preview Prefab", "프로젝트의 프리팹 자산을 선택하세요.", "OK");
            return;
        }

        var dir = Path.GetDirectoryName(srcPath);
        var name = Path.GetFileNameWithoutExtension(srcPath);

        CreateOne(srcPath, Path.Combine(dir, $"{name}_Preview_Blue.prefab"), cfg.blueMat);
        CreateOne(srcPath, Path.Combine(dir, $"{name}_Preview_Red.prefab"), cfg.redMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Preview Prefab", "Blue/Red 프리뷰 프리팹 생성 완료!", "OK");
    }

    private static void CreateOne(string srcPrefabPath, string dstPrefabPath, Material mat)
    {
        var root = PrefabUtility.LoadPrefabContents(srcPrefabPath);
        try
        {
            PrepareAsPreview(root, mat);
            PrefabUtility.SaveAsPrefabAsset(root, dstPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PrepareAsPreview(GameObject root, Material mat)
    {
        // 레이어: Ignore Raycast(있으면)
        int ignore = LayerMask.NameToLayer("Ignore Raycast");
        if (ignore >= 0) SetLayerRecursively(root, ignore);

        // 콜라이더 끄기
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        // 렌더러에 전역 Blue/Red 머티리얼 적용
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;

            // 그림자 OFF(선택)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // (선택) 렌더러 외 스크립트 비활성화
        foreach (var b in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (b is Renderer || b is Animator) continue;
            b.enabled = false;
        }
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }
}
#endif
