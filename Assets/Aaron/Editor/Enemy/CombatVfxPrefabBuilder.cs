#if UNITY_EDITOR
#nullable enable

using UnityEditor;
using UnityEngine;

/// <summary>Shared editor helpers for combat VFX prefab authoring.</summary>
public static class CombatVfxPrefabBuilder
{
    private const string SquareSpritePath = "Assets/Visuals/UI/SolidWhite.png";
    private const string CircleSpriteGuid = "a86470a33a6bf42c4b3595704624658b";
    private const string ParticleMaterialGuid = "9834be5f44d4d44c1a70dbcffd44063e";
    private const int ParticleSortingOrder = 20;

    public static Sprite? LoadSquareSprite()
    {
        Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (sprite != null)
        {
            return sprite;
        }

        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    public static Sprite? LoadCircleSprite()
    {
        Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(CircleSpriteGuid));
        return sprite ?? LoadSquareSprite();
    }

    public static Material? LoadParticleMaterial()
    {
        Material? projectMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            AssetDatabase.GUIDToAssetPath(ParticleMaterialGuid));
        if (projectMaterial != null)
        {
            return projectMaterial;
        }

        return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
    }

    public static SpriteRenderer CreateSpriteChild(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        SpriteDrawMode drawMode = SpriteDrawMode.Simple)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = drawMode;
        renderer.sortingOrder = 1;
        if (drawMode == SpriteDrawMode.Sliced)
        {
            // Sliced size is in absolute world units, independent of the sprite's tiny native size.
            renderer.size = Vector2.one;
        }

        return renderer;
    }

    public static ParticleSystem CreateBeamParticleSystem(
        Transform parent,
        string name,
        Color color,
        bool looping,
        float rate,
        Vector3 shapeScale,
        float startSize = 0.2f,
        float startSpeed = 1.5f,
        float lifetime = 0.25f)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ConfigureParticleRenderer(particles);

        ParticleSystem.MainModule main = particles.main;
        main.loop = looping;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 128;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = shapeScale;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    public static ParticleSystem CreateMuzzleFlash(Transform parent, string name, Color color)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ConfigureParticleRenderer(particles);

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.16f;
        main.startSpeed = 5f;
        main.startSize = 0.28f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    public static ParticleSystem CreateProjectileTrail(Transform parent, string name, Color color)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        ConfigureParticleRenderer(particles);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 0.1f;
        main.startSize = 0.14f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 18f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;

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
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = gradient;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    private static void ConfigureParticleRenderer(ParticleSystem particles)
    {
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Material? material = LoadParticleMaterial();
        if (material != null)
        {
            renderer.material = material;
        }

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = ParticleSortingOrder;
    }
}
#endif
