#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds glow + trail visuals to the shared enemy projectile prefab.
/// Menu: Henry/Enhance Enemy Projectile Visuals
/// </summary>
public static class EnemyProjectilePrefabGenerator
{
    private const string ArrowPrefabPath = "Assets/Visuals/Prefabs/Arrow.prefab";

    // Bullets read bigger without hitting harder: the transform scales up by VisualScale while the
    // collider radius divides by it, holding the effective world radius at 0.18.
    private const float VisualScale = 1.6f;
    private const float BaseColliderRadius = 0.5f;
    private static readonly Vector2 BaseScale = new Vector2(0.17f, 0.36f);

    [MenuItem("Henry/Enhance Enemy Projectile Visuals")]
    public static void EnhanceFromMenu()
    {
        GameObject? prefabRoot = PrefabUtility.LoadPrefabContents(ArrowPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"EnemyProjectilePrefabGenerator: could not load {ArrowPrefabPath}");
            return;
        }

        try
        {
            Sprite? sprite = CombatVfxPrefabBuilder.LoadCircleSprite();
            if (sprite == null)
            {
                Debug.LogError("EnemyProjectilePrefabGenerator: could not resolve projectile sprite.");
                return;
            }

            // Original prefab used tiny root scale (0.008, 0.02) for the circle sprite — reset so visuals are visible.
            prefabRoot.transform.localScale = new Vector3(BaseScale.x * VisualScale, BaseScale.y * VisualScale, 1f);

            CircleCollider2D? projectileCollider = prefabRoot.GetComponent<CircleCollider2D>();
            if (projectileCollider != null)
            {
                projectileCollider.radius = BaseColliderRadius / VisualScale;
            }

            SpriteRenderer? coreRenderer = prefabRoot.GetComponent<SpriteRenderer>();
            if (coreRenderer != null)
            {
                coreRenderer.sprite = sprite;
                coreRenderer.drawMode = SpriteDrawMode.Simple;
                coreRenderer.sortingOrder = 3;
            }

            Transform? existingGlow = prefabRoot.transform.Find("Glow");
            if (existingGlow != null)
            {
                Object.DestroyImmediate(existingGlow.gameObject);
            }

            Transform? existingTrail = prefabRoot.transform.Find("Trail");
            if (existingTrail != null)
            {
                Object.DestroyImmediate(existingTrail.gameObject);
            }

            SpriteRenderer glow = CombatVfxPrefabBuilder.CreateSpriteChild(
                prefabRoot.transform,
                "Glow",
                sprite,
                new Color(1f, 1f, 1f, 0.35f));
            glow.sortingOrder = 2;
            glow.transform.localScale = new Vector3(1.45f, 1.45f, 1f);

            ParticleSystem trail = CombatVfxPrefabBuilder.CreateProjectileTrail(
                prefabRoot.transform,
                "Trail",
                Color.white);

            EnemyProjectileVisual? visual = prefabRoot.GetComponent<EnemyProjectileVisual>();
            if (visual == null)
            {
                visual = prefabRoot.AddComponent<EnemyProjectileVisual>();
            }

            SerializedObject serializedVisual = new SerializedObject(visual);
            serializedVisual.FindProperty("glowRenderer").objectReferenceValue = glow;
            serializedVisual.FindProperty("trailParticles").objectReferenceValue = trail;
            serializedVisual.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ArrowPrefabPath);
            Debug.Log($"EnemyProjectilePrefabGenerator: enhanced {ArrowPrefabPath} (visible core + glow + trail).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
