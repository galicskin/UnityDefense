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
                "PreviewMatConfig�� ���ų� Blue/Red ��Ƽ������ ��� �ֽ��ϴ�.\n" +
                "�޴�: Tools > Build Preview > Open or Create Config ���� �����ϼ���.",
                "OK");
            return;
        }

        var srcPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
            PrefabUtility.GetCorrespondingObjectFromSource(sourcePrefab) ?? sourcePrefab
        );
        if (string.IsNullOrEmpty(srcPath))
        {
            EditorUtility.DisplayDialog("Preview Prefab", "������Ʈ�� ������ �ڻ��� �����ϼ���.", "OK");
            return;
        }

        var dir = Path.GetDirectoryName(srcPath);
        var name = Path.GetFileNameWithoutExtension(srcPath);

        CreateOne(srcPath, Path.Combine(dir, $"{name}_Preview_Blue.prefab"), cfg.blueMat);
        CreateOne(srcPath, Path.Combine(dir, $"{name}_Preview_Red.prefab"), cfg.redMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Preview Prefab", "Blue/Red ������ ������ ���� �Ϸ�!", "OK");
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
        // ���̾�: Ignore Raycast(������)
        int ignore = LayerMask.NameToLayer("Ignore Raycast");
        if (ignore >= 0) SetLayerRecursively(root, ignore);

        // �ݶ��̴� ����
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        // �������� ���� Blue/Red ��Ƽ���� ����
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;

            // �׸��� OFF(����)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // (����) ������ �� ��ũ��Ʈ ��Ȱ��ȭ
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
