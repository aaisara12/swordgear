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
    private const string SoftParticleMaterialPath = "Assets/Visuals/Materials/SoftParticleGlow.mat";
    private const string SoftParticleTexturePath = "Assets/NamuFX/_CommonAssets/Images/Common/glow01.psd";
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
        // Prefer soft glow texture — ParticleAdditiveGlow has no _BaseMap and renders as hard quads.
        Material? soft = EnsureSoftParticleMaterial();
        if (soft != null)
        {
            return soft;
        }

        Material? projectMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            AssetDatabase.GUIDToAssetPath(ParticleMaterialGuid));
        if (projectMaterial != null)
        {
            return projectMaterial;
        }

        return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
    }

    /// <summary>
    /// Additive particle material with a soft glow texture.
    /// NamuFX glow maps are black-background / no alpha — they only look correct with Additive blend
    /// (black contributes nothing). Alpha blend produces the "black box with orange circle" look.
    /// </summary>
    public static Material? EnsureSoftParticleMaterial()
    {
        Material? existing = AssetDatabase.LoadAssetAtPath<Material>(SoftParticleMaterialPath);
        Texture2D? glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SoftParticleTexturePath);

        // Prefer Unity's soft Default-Particle (has real alpha). Fall back to NamuFX glow01.
        Texture? softTex = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat")?.mainTexture;
        if (softTex == null)
        {
            softTex = glowTex;
        }

        if (softTex == null)
        {
            return existing;
        }

        if (existing == null)
        {
            Material? template = AssetDatabase.LoadAssetAtPath<Material>(
                AssetDatabase.GUIDToAssetPath(ParticleMaterialGuid));
            if (template == null)
            {
                return null;
            }

            existing = new Material(template) { name = "SoftParticleGlow" };
            AssetDatabase.CreateAsset(existing, SoftParticleMaterialPath);
        }

        if (existing.HasProperty("_BaseMap"))
        {
            existing.SetTexture("_BaseMap", softTex);
        }

        if (existing.HasProperty("_MainTex"))
        {
            existing.SetTexture("_MainTex", softTex);
        }

        // URP Particles/Unlit — Additive so soft glows don't draw opaque quads.
        if (existing.HasProperty("_Surface"))
        {
            existing.SetFloat("_Surface", 1f); // Transparent
        }

        if (existing.HasProperty("_Blend"))
        {
            existing.SetFloat("_Blend", 2f); // Additive
        }

        if (existing.HasProperty("_SrcBlend"))
        {
            existing.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (existing.HasProperty("_DstBlend"))
        {
            existing.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (existing.HasProperty("_SrcBlendAlpha"))
        {
            existing.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (existing.HasProperty("_DstBlendAlpha"))
        {
            existing.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (existing.HasProperty("_ZWrite"))
        {
            existing.SetFloat("_ZWrite", 0f);
        }

        if (existing.HasProperty("_ColorMode"))
        {
            existing.SetFloat("_ColorMode", 0f); // Multiply — particle startColor tints the glow
        }

        existing.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        existing.renderQueue = 3000;
        EditorUtility.SetDirty(existing);
        return existing;
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
        // Trail scales Local in World space, so it cannot inherit the projectile's enlarged root scale.
        main.startSize = 0.224f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 18f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.096f;

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
