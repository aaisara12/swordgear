#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

// Adds a dilated-silhouette outline child under each enemy's VisualRoot. Menu: Henry/Setup Enemy Outlines
public static class EnemyOutlineSetup
{
    private const string EnemyPrefabFolder = "Assets/Visuals/Prefabs/Enemies/";
    private const string MeleeOutlineMaterial = "Assets/Visuals/Materials/EnemyOutlineMelee.mat";
    private const string RangedOutlineMaterial = "Assets/Visuals/Materials/EnemyOutlineRanged.mat";
    private const string OutlineChildName = "Outline";

    private static readonly string[] Elements = { "Fire", "Ice", "Lightning", "Physical", "Wind" };

    [MenuItem("Henry/Setup Enemy Outlines")]
    public static void SetupFromMenu()
    {
        int updated = 0;
        foreach (string element in Elements)
        {
            updated += ApplyOutline($"{EnemyPrefabFolder}MeleeEnemy{element}.prefab", MeleeOutlineMaterial) ? 1 : 0;
            updated += ApplyOutline($"{EnemyPrefabFolder}RangedEnemy{element}.prefab", RangedOutlineMaterial) ? 1 : 0;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"EnemyOutlineSetup: outlined {updated} enemy prefabs.");
    }

    private static bool ApplyOutline(string prefabPath, string materialPath)
    {
        Material? outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (outlineMaterial == null)
        {
            Debug.LogError($"EnemyOutlineSetup: missing material {materialPath}");
            return false;
        }

        GameObject? prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"EnemyOutlineSetup: could not load {prefabPath}");
            return false;
        }

        try
        {
            Transform? visualRoot = prefabRoot.transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError($"EnemyOutlineSetup: {prefabPath} has no VisualRoot");
                return false;
            }

            SpriteRenderer? source = visualRoot.GetComponent<SpriteRenderer>();
            if (source == null || source.sprite == null)
            {
                Debug.LogError($"EnemyOutlineSetup: {prefabPath} VisualRoot has no sprite");
                return false;
            }

            Transform? existing = visualRoot.Find(OutlineChildName);
            GameObject outlineGo = existing != null ? existing.gameObject : new GameObject(OutlineChildName);
            if (existing == null)
            {
                outlineGo.transform.SetParent(visualRoot, false);
            }

            outlineGo.layer = visualRoot.gameObject.layer;
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one;

            SpriteRenderer outline = outlineGo.GetComponent<SpriteRenderer>();
            if (outline == null)
            {
                outline = outlineGo.AddComponent<SpriteRenderer>();
            }

            outline.sprite = source.sprite;
            outline.sharedMaterial = outlineMaterial;
            outline.sortingLayerID = source.sortingLayerID;
            // One below the body so only the dilated rim shows; floor sits at 0.
            outline.sortingOrder = source.sortingOrder - 1;
            // Shader lerps the rim toward this by _TintAmount, so it picks up the element colour.
            outline.color = source.color;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
#endif
