#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

// Builds each enemy's visual stack: greyscale body (element-tinted), lights overlay, outline behind.
public static class EnemyVisualSetup
{
    private const string EnemyPrefabFolder = "Assets/Visuals/Prefabs/Enemies/";

    private const string MeleeBaseSprite = "Assets/Visuals/SG_melee_guy_1_greyscale.png";
    private const string MeleeLightsSprite = "Assets/Visuals/SG_melee_guy_1_greyscale_lights.png";
    private const string MeleeOutlineMaterial = "Assets/Visuals/Materials/EnemyOutlineMelee.mat";

    private const string RangedBaseSprite = "Assets/Visuals/swordgear_basic_ranged_enemy_greyscale.png";
    private const string RangedLightsSprite = "Assets/Visuals/swordgear_basic_ranged_enemy_greyscale_lights_v1.png";
    private const string RangedOutlineMaterial = "Assets/Visuals/Materials/EnemyOutlineRanged.mat";

    private const string OutlineChildName = "Outline";
    private const string LightsChildName = "Lights";

    private const int BodyOrder = 2;
    private const int OutlineOrder = 1;
    private const int LightsOrder = 3;

    private static readonly string[] Elements = { "Fire", "Ice", "Lightning", "Physical", "Wind" };

    [MenuItem("Henry/Setup Enemy Visuals")]
    public static void SetupFromMenu()
    {
        int updated = 0;
        foreach (string element in Elements)
        {
            updated += Apply($"{EnemyPrefabFolder}MeleeEnemy{element}.prefab", MeleeBaseSprite, MeleeLightsSprite, MeleeOutlineMaterial) ? 1 : 0;
            updated += Apply($"{EnemyPrefabFolder}RangedEnemy{element}.prefab", RangedBaseSprite, RangedLightsSprite, RangedOutlineMaterial) ? 1 : 0;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"EnemyVisualSetup: rebuilt visuals on {updated} enemy prefabs.");
    }

    private static bool Apply(string prefabPath, string basePath, string lightsPath, string materialPath)
    {
        Sprite? baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath);
        Sprite? lightsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(lightsPath);
        Material? outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (baseSprite == null || lightsSprite == null || outlineMaterial == null)
        {
            Debug.LogError($"EnemyVisualSetup: missing asset for {prefabPath} (base/lights/material)");
            return false;
        }

        GameObject? prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"EnemyVisualSetup: could not load {prefabPath}");
            return false;
        }

        try
        {
            Transform? visualRoot = prefabRoot.transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError($"EnemyVisualSetup: {prefabPath} has no VisualRoot");
                return false;
            }

            SpriteRenderer? body = visualRoot.GetComponent<SpriteRenderer>();
            if (body == null)
            {
                Debug.LogError($"EnemyVisualSetup: {prefabPath} VisualRoot has no SpriteRenderer");
                return false;
            }

            RemoveStrayLights(prefabRoot.transform, lightsSprite, visualRoot);

            Color elementColor = body.color;
            body.sprite = baseSprite;
            body.sortingOrder = BodyOrder;

            SpriteRenderer outline = EnsureChild(visualRoot, OutlineChildName);
            outline.sprite = baseSprite;
            outline.sharedMaterial = outlineMaterial;
            outline.sortingLayerID = body.sortingLayerID;
            outline.sortingOrder = OutlineOrder;
            outline.color = elementColor;

            SpriteRenderer lights = EnsureChild(visualRoot, LightsChildName);
            lights.sprite = lightsSprite;
            lights.sharedMaterial = body.sharedMaterial;
            lights.sortingLayerID = body.sortingLayerID;
            lights.sortingOrder = LightsOrder;
            // Lights are emissive highlights, so they stay untinted while the body takes the element colour.
            lights.color = Color.white;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static SpriteRenderer EnsureChild(Transform parent, string name)
    {
        Transform? existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
        }

        go.layer = parent.gameObject.layer;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        return sr != null ? sr : go.AddComponent<SpriteRenderer>();
    }

    private static void RemoveStrayLights(Transform root, Sprite lightsSprite, Transform visualRoot)
    {
        Texture2D? lightsTexture = lightsSprite.texture;
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null)
            {
                continue;
            }

            GameObject go = sr.gameObject;
            if (go.transform == root || go.transform == visualRoot)
            {
                continue;
            }

            bool isManaged = go.transform.parent == visualRoot
                && (go.name == LightsChildName || go.name == OutlineChildName);
            if (isManaged)
            {
                continue;
            }

            bool matchesLights = sr.sprite != null && sr.sprite.texture == lightsTexture;
            // A sprite-only object with a dangling sprite is an orphaned stub from earlier authoring.
            bool orphanStub = sr.sprite == null
                && go.transform.childCount == 0
                && go.GetComponents<Component>().Length == 2;

            if (matchesLights || orphanStub)
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
