#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds placeholder VFX prefabs for the Wind element's empowered-throw effects
/// (wisp swirl for partial charges, tornado for max charges).
/// Menu: Henry/Generate Wind VFX Prefabs
/// </summary>
public static class WindVfxPrefabGenerator
{
    private const string WispPrefabPath = "Assets/Visuals/FX/WindWisp.prefab";
    private const string TornadoPrefabPath = "Assets/Visuals/FX/WindTornado.prefab";

    private static readonly Color WispColor = new Color(0.56f, 0.93f, 0.56f, 0.9f);
    private static readonly Color TornadoOuterColor = new Color(0.5f, 0.95f, 0.55f, 0.85f);
    private static readonly Color TornadoInnerColor = new Color(0.75f, 1f, 0.8f, 1f);

    [MenuItem("Henry/Generate Wind VFX Prefabs")]
    public static void Generate()
    {
        GenerateWisp();
        GenerateTornado();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void GenerateWisp()
    {
        GameObject root = new GameObject("WindWisp");
        try
        {
            ParticleSystem swirl = CreateSwirlRing(root.transform, "Swirl", WispColor, radius: 1.5f, orbitalSpeed: 2.5f, rate: 16f, size: 0.16f);

            WindWispEffect effect = root.AddComponent<WindWispEffect>();
            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("visualRadiusReference").floatValue = 1.5f;
            SerializedProperty particlesProp = serialized.FindProperty("wispParticles");
            particlesProp.arraySize = 1;
            particlesProp.GetArrayElementAtIndex(0).objectReferenceValue = swirl;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, WispPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void GenerateTornado()
    {
        GameObject root = new GameObject("WindTornado");
        try
        {
            ParticleSystem outer = CreateSwirlRing(root.transform, "OuterRing", TornadoOuterColor, radius: 4f, orbitalSpeed: 3.5f, rate: 26f, size: 0.22f);
            ParticleSystem inner = CreateSwirlRing(root.transform, "InnerRing", TornadoInnerColor, radius: 1.6f, orbitalSpeed: -6f, rate: 20f, size: 0.14f);

            GameObject blockerObj = new GameObject("ProjectileBlocker");
            blockerObj.transform.SetParent(root.transform, false);
            blockerObj.tag = "ProjectileBlocking";
            CircleCollider2D blocker = blockerObj.AddComponent<CircleCollider2D>();
            blocker.isTrigger = true;
            blocker.radius = 4f;

            WindTornadoEffect effect = root.AddComponent<WindTornadoEffect>();
            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("visualRadiusReference").floatValue = 4f;
            SerializedProperty particlesProp = serialized.FindProperty("tornadoParticles");
            particlesProp.arraySize = 2;
            particlesProp.GetArrayElementAtIndex(0).objectReferenceValue = outer;
            particlesProp.GetArrayElementAtIndex(1).objectReferenceValue = inner;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, TornadoPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static ParticleSystem CreateSwirlRing(Transform parent, string name, Color color, float radius, float orbitalSpeed, float rate, float size)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        Material? material = CombatVfxPrefabBuilder.LoadParticleMaterial();
        if (material != null)
        {
            renderer.material = material;
        }
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 20;

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 1.4f;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 96;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f; // fill the whole disc, not just the edge

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(orbitalSpeed);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(color.a, 0.2f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = gradient;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log($"WindVfxPrefabGenerator: saved {path}");
    }
}
#endif
