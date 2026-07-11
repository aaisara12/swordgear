#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Creates shared EnemySpawn animation assets, EliteAura prefab, ElitePresentation SO,
/// and wires VisualRoot + Animator + EnemySpawnPresentation onto all 20 catalog enemy prefabs.
/// Menu: Henry/Wire Enemy Spawn Presentation
/// </summary>
public static class EnemySpawnPresentationSetup
{
    private const string AnimFolder = "Assets/Visuals/Animations/EnemySpawn";
    private const string SpawnClipPath = AnimFolder + "/EnemySpawn_Spawn.anim";
    private const string IdleClipPath = AnimFolder + "/EnemySpawn_Idle.anim";
    private const string ControllerPath = AnimFolder + "/EnemySpawn.controller";
    private const string AuraPrefabPath = "Assets/Visuals/Prefabs/Enemies/EliteAura.prefab";
    private const string SpawnBurstPrefabPath = "Assets/Visuals/Prefabs/Enemies/EnemySpawnBurst.prefab";
    private const string ElitePresentationPath = "Assets/Aaron/ScriptableObjects/ElitePresentation.asset";
    private const string CoreSystemsPrefabPath = "Assets/Aaron/Prefabs/CoreSystems.prefab";
    private const string PrefabFolder = "Assets/Visuals/Prefabs/Enemies";

    private static readonly string[] EnemyPrefabNames =
    {
        "MeleeEnemyPhysical", "MeleeEnemyFire", "MeleeEnemyIce", "MeleeEnemyLightning",
        "RangedEnemyPhysical", "RangedEnemyFire", "RangedEnemyIce", "RangedEnemyLightning",
        "BeamSniper_Physical", "BeamSniper_Fire", "BeamSniper_Ice", "BeamSniper_Lightning",
        "Shotgun_Physical", "Shotgun_Fire", "Shotgun_Ice", "Shotgun_Lightning",
        "Turret_Physical", "Turret_Fire", "Turret_Ice", "Turret_Lightning",
    };

    [MenuItem("Henry/Wire Enemy Spawn Presentation")]
    public static void WireFromMenu()
    {
        EnsureFolder(AnimFolder);
        EnsureFolder("Assets/Visuals/Prefabs/Enemies");
        EnsureFolder("Assets/Aaron/ScriptableObjects");

        AnimationClip spawnClip = CreateOrUpdateSpawnClip();
        AnimationClip idleClip = CreateOrUpdateIdleClip();
        AnimatorController controller = CreateOrUpdateController(spawnClip, idleClip);
        GameObject auraPrefab = CreateOrUpdateEliteAuraPrefab();
        GameObject spawnBurstPrefab = CreateOrUpdateSpawnBurstPrefab();
        ElitePresentation elitePresentation = CreateOrUpdateElitePresentation(auraPrefab);
        WireElitePresentationToCoreSystems(elitePresentation);

        int wired = 0;
        foreach (string prefabName in EnemyPrefabNames)
        {
            string path = $"{PrefabFolder}/{prefabName}.prefab";
            if (WireEnemyPrefab(path, controller, auraPrefab, spawnBurstPrefab))
            {
                wired++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"EnemySpawnPresentationSetup: wired {wired}/{EnemyPrefabNames.Length} enemy prefabs. " +
            $"Anim={ControllerPath}, Aura={AuraPrefabPath}, Burst={SpawnBurstPrefabPath}.");
    }

    private static AnimationClip CreateOrUpdateSpawnClip()
    {
        AnimationClip? clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SpawnClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "EnemySpawn_Spawn", frameRate = 60f };
            AssetDatabase.CreateAsset(clip, SpawnClipPath);
        }

        // Punchier pop: tiny → big overshoot → settle, with a quick spin flourish.
        AnimationCurve scaleXY = new AnimationCurve(
            new Keyframe(0f, 0.05f),
            new Keyframe(0.12f, 1.45f),
            new Keyframe(0.28f, 0.88f),
            new Keyframe(0.5f, 1.08f),
            new Keyframe(0.75f, 1f));
        AnimationCurve scaleZ = AnimationCurve.Constant(0f, 0.75f, 1f);
        AnimationCurve rotZ = new AnimationCurve(
            new Keyframe(0f, -18f),
            new Keyframe(0.2f, 12f),
            new Keyframe(0.45f, -4f),
            new Keyframe(0.75f, 0f));

        clip.ClearCurves();
        clip.SetCurve("", typeof(Transform), "localScale.x", scaleXY);
        clip.SetCurve("", typeof(Transform), "localScale.y", scaleXY);
        clip.SetCurve("", typeof(Transform), "localScale.z", scaleZ);
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", rotZ);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateOrUpdateIdleClip()
    {
        AnimationClip? clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "EnemySpawn_Idle", frameRate = 60f };
            AssetDatabase.CreateAsset(clip, IdleClipPath);
        }

        AnimationCurve one = AnimationCurve.Constant(0f, 0.1f, 1f);
        clip.ClearCurves();
        clip.SetCurve("", typeof(Transform), "localScale.x", one);
        clip.SetCurve("", typeof(Transform), "localScale.y", one);
        clip.SetCurve("", typeof(Transform), "localScale.z", one);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(AnimationClip spawnClip, AnimationClip idleClip)
    {
        AnimatorController? controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        // Rebuild a simple Spawn → Idle machine.
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        while (root.states.Length > 0)
        {
            root.RemoveState(root.states[0].state);
        }

        while (root.anyStateTransitions.Length > 0)
        {
            root.RemoveAnyStateTransition(root.anyStateTransitions[0]);
        }

        AnimatorState spawnState = root.AddState("Spawn", new Vector3(200f, 0f, 0f));
        spawnState.motion = spawnClip;
        AnimatorState idleState = root.AddState("Idle", new Vector3(450f, 0f, 0f));
        idleState.motion = idleClip;
        root.defaultState = spawnState;

        AnimatorStateTransition transition = spawnState.AddTransition(idleState);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.duration = 0f;
        transition.hasFixedDuration = true;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateOrUpdateEliteAuraPrefab()
    {
        CombatVfxPrefabBuilder.EnsureSoftParticleMaterial();

        GameObject root = new GameObject("EliteAura");
        try
        {
            // Soft under-glow — above floor (0), beneath enemy body (2).
            CreateAuraLayer(
                root.transform,
                "UnderGlow",
                rate: 22f,
                lifetime: 1.1f,
                startSize: 3.4f,
                startSpeed: 0.01f,
                radius: 0.24f,
                orbital: 0.05f,
                color: new Color(1f, 0.55f, 0.12f, 0.75f),
                sortingOrder: 1);

            // Wider soft bloom around the feet/body.
            CreateAuraLayer(
                root.transform,
                "Bloom",
                rate: 18f,
                lifetime: 0.95f,
                startSize: 2.1f,
                startSpeed: 0.03f,
                radius: 0.55f,
                orbital: 0.25f,
                color: new Color(1f, 0.75f, 0.22f, 0.55f),
                sortingOrder: 1);

            // Subtle bright core flecks still under the sprite.
            CreateAuraLayer(
                root.transform,
                "CoreGlow",
                rate: 14f,
                lifetime: 0.55f,
                startSize: 0.85f,
                startSpeed: 0.08f,
                radius: 0.3f,
                orbital: 0.45f,
                color: new Color(1f, 0.92f, 0.6f, 0.8f),
                sortingOrder: 1);

            PrefabUtility.SaveAsPrefabAsset(root, AuraPrefabPath, out bool success);
            if (!success)
            {
                Debug.LogError($"EnemySpawnPresentationSetup: failed to write {AuraPrefabPath}");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(AuraPrefabPath)!;
    }

    private static GameObject CreateOrUpdateSpawnBurstPrefab()
    {
        CombatVfxPrefabBuilder.EnsureSoftParticleMaterial();

        GameObject root = new GameObject("EnemySpawnBurst");
        try
        {
            // Silent root driver so Play(withChildren: true) fires FlashRing + Sparks together.
            ParticleSystem driver = root.AddComponent<ParticleSystem>();
            driver.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule driverMain = driver.main;
            driverMain.loop = false;
            driverMain.playOnAwake = false;
            driverMain.duration = 0.45f;
            driverMain.maxParticles = 0;
            driverMain.startLifetime = 0.01f;
            ParticleSystem.EmissionModule driverEmission = driver.emission;
            driverEmission.rateOverTime = 0f;
            driver.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ConfigureSoftParticleRenderer(driver, sortingOrder: 3);

            // Expanding soft flash ring — the eye-catching "pop".
            // Parent is enemy root (not VisualRoot) so spawn scale anim does not crush particles.
            CreateSpawnBurstLayer(
                root.transform,
                "FlashRing",
                burstCount: 1,
                lifetime: 0.4f,
                startSpeed: 0f,
                startSize: 1.35f,
                endSizeMultiplier: 3.2f,
                radius: 0.06f,
                color: new Color(1f, 0.95f, 0.6f, 1f),
                sortingOrder: 5);

            // Outward spark burst.
            CreateSpawnBurstLayer(
                root.transform,
                "Sparks",
                burstCount: 26,
                lifetime: 0.4f,
                startSpeed: 4.5f,
                startSize: 0.42f,
                endSizeMultiplier: 0.25f,
                radius: 0.24f,
                color: new Color(1f, 0.8f, 0.3f, 1f),
                sortingOrder: 6);

            PrefabUtility.SaveAsPrefabAsset(root, SpawnBurstPrefabPath, out bool success);
            if (!success)
            {
                Debug.LogError($"EnemySpawnPresentationSetup: failed to write {SpawnBurstPrefabPath}");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(SpawnBurstPrefabPath)!;
    }

    private static void CreateSpawnBurstLayer(
        Transform parent,
        string name,
        short burstCount,
        float lifetime,
        float startSpeed,
        float startSize,
        float endSizeMultiplier,
        float radius,
        Color color,
        int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.45f;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed > 0f
            ? new ParticleSystem.MinMaxCurve(startSpeed * 0.55f, startSpeed)
            : 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.75f, startSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 64;
        main.scalingMode = ParticleSystemScalingMode.Local;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, endSizeMultiplier));

        ConfigureSoftParticleRenderer(particles, sortingOrder);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void CreateAuraLayer(
        Transform parent,
        string name,
        float rate,
        float lifetime,
        float startSize,
        float startSpeed,
        float radius,
        float orbital,
        Color color,
        int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.prewarm = true; // fills the ring immediately — no empty gap on enable / loop seams
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.85f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed * 0.35f, startSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.7f, startSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 128;
        // Local: keep glow size stable; elite root scale still enlarges the whole aura slightly via transform.
        main.scalingMode = ParticleSystemScalingMode.Local;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f; // fill the disc — under-glow, not a hollow distant ring

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 0f;
        velocity.orbitalZ = orbital;
        velocity.radial = 0.02f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        // Soft pulse without dropping to zero (avoids loop gaps).
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f),
            },
            new[]
            {
                new GradientAlphaKey(color.a * 0.5f, 0f),
                new GradientAlphaKey(color.a, 0.4f),
                new GradientAlphaKey(color.a * 0.5f, 1f),
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 0.85f, 1f, 1.1f));

        ConfigureSoftParticleRenderer(particles, sortingOrder);
    }

    private static void ConfigureSoftParticleRenderer(ParticleSystem particles, int sortingOrder)
    {
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Material? material = CombatVfxPrefabBuilder.LoadParticleMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
    }

    private static ElitePresentation CreateOrUpdateElitePresentation(GameObject auraPrefab)
    {
        ElitePresentation? presentation = AssetDatabase.LoadAssetAtPath<ElitePresentation>(ElitePresentationPath);
        if (presentation == null)
        {
            presentation = ScriptableObject.CreateInstance<ElitePresentation>();
            AssetDatabase.CreateAsset(presentation, ElitePresentationPath);
        }

        SerializedObject serialized = new SerializedObject(presentation);
        serialized.FindProperty("auraPrefab").objectReferenceValue = auraPrefab;
        serialized.FindProperty("hpMultiplier").floatValue = 2f;
        serialized.FindProperty("damageMultiplier").floatValue = 1.5f;
        serialized.FindProperty("speedMultiplier").floatValue = 1.1f;
        serialized.FindProperty("scaleMultiplier").floatValue = 1.4f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(presentation);
        return presentation;
    }

    private static void WireElitePresentationToCoreSystems(ElitePresentation presentation)
    {
        GameObject? root = PrefabUtility.LoadPrefabContents(CoreSystemsPrefabPath);
        if (root == null)
        {
            Debug.LogError($"EnemySpawnPresentationSetup: could not load {CoreSystemsPrefabPath}");
            return;
        }

        try
        {
            LevelLoader? loader = root.GetComponentInChildren<LevelLoader>(true);
            if (loader == null)
            {
                Debug.LogError("EnemySpawnPresentationSetup: LevelLoader not found on CoreSystems.");
                return;
            }

            SerializedObject serialized = new SerializedObject(loader);
            SerializedProperty property = serialized.FindProperty("elitePresentation");
            if (property == null)
            {
                Debug.LogError("EnemySpawnPresentationSetup: LevelLoader.elitePresentation missing (compile?).");
                return;
            }

            property.objectReferenceValue = presentation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CoreSystemsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool WireEnemyPrefab(
        string prefabPath,
        AnimatorController controller,
        GameObject auraPrefab,
        GameObject spawnBurstPrefab)
    {
        GameObject? root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogError($"EnemySpawnPresentationSetup: missing {prefabPath}");
            return false;
        }

        try
        {
            Transform visualRoot = EnsureVisualRoot(root);
            Animator animator = visualRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visualRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // Replace elite aura so regenerated soft layers always apply.
            Transform? existingAura = root.transform.Find("EliteAura");
            if (existingAura != null)
            {
                Object.DestroyImmediate(existingAura.gameObject);
            }

            GameObject? auraInstance = PrefabUtility.InstantiatePrefab(auraPrefab, root.transform) as GameObject;
            if (auraInstance != null)
            {
                auraInstance.name = "EliteAura";
                auraInstance.SetActive(false);
            }

            // Remove burst from VisualRoot (old) or root (current).
            Transform? existingBurst = visualRoot.Find("EnemySpawnBurst");
            if (existingBurst == null)
            {
                existingBurst = root.transform.Find("EnemySpawnBurst");
            }

            if (existingBurst != null)
            {
                Object.DestroyImmediate(existingBurst.gameObject);
            }

            // Parent to enemy root — VisualRoot scales 0.05→1 during spawn and would hide the burst.
            GameObject? burstInstance = PrefabUtility.InstantiatePrefab(spawnBurstPrefab, root.transform) as GameObject;
            ParticleSystem? burstParticles = null;
            if (burstInstance != null)
            {
                burstInstance.name = "EnemySpawnBurst";
                burstInstance.transform.localPosition = Vector3.zero;
                burstInstance.transform.localScale = Vector3.one;
                burstParticles = burstInstance.GetComponent<ParticleSystem>();
                burstParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            EnemySpawnPresentation presentation = root.GetComponent<EnemySpawnPresentation>();
            if (presentation == null)
            {
                presentation = root.AddComponent<EnemySpawnPresentation>();
            }

            SerializedObject serialized = new SerializedObject(presentation);
            serialized.FindProperty("visualAnimator").objectReferenceValue = animator;
            serialized.FindProperty("spawnStateName").stringValue = "Spawn";
            serialized.FindProperty("spawnDurationFallback").floatValue = 0.75f;
            serialized.FindProperty("eliteAuraChild").objectReferenceValue = auraInstance;
            serialized.FindProperty("spawnBurstParticles").objectReferenceValue = burstParticles;
            serialized.FindProperty("bodyCollider").objectReferenceValue = root.GetComponent<Collider2D>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// Ensures a VisualRoot child owns a visible SpriteRenderer so spawn scale does not fight
    /// EnemyController facing flips on the root transform.
    /// </summary>
    private static Transform EnsureVisualRoot(GameObject root)
    {
        Transform? existing = root.transform.Find("VisualRoot");
        if (existing != null)
        {
            SyncVisualSpriteFromRoot(root, existing);
            return existing;
        }

        GameObject visualRootGo = new GameObject("VisualRoot");
        visualRootGo.transform.SetParent(root.transform, false);
        visualRootGo.transform.localPosition = Vector3.zero;
        visualRootGo.transform.localRotation = Quaternion.identity;
        visualRootGo.transform.localScale = Vector3.one;

        SpriteRenderer? rootSprite = root.GetComponent<SpriteRenderer>();
        if (rootSprite != null)
        {
            SpriteRenderer visualSprite = visualRootGo.AddComponent<SpriteRenderer>();
            EditorUtility.CopySerialized(rootSprite, visualSprite);
            visualSprite.sortingOrder = Mathf.Max(2, visualSprite.sortingOrder);
            rootSprite.enabled = false;
            visualSprite.enabled = true;
        }

        List<Transform> toMove = new();
        for (int i = 0; i < root.transform.childCount; i++)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name == "VisualRoot" || child.name == "EliteAura" || child.name == "EnemySpawnBurst")
            {
                continue;
            }

            toMove.Add(child);
        }

        for (int i = 0; i < toMove.Count; i++)
        {
            toMove[i].SetParent(visualRootGo.transform, true);
        }

        return visualRootGo.transform;
    }

    private static void SyncVisualSpriteFromRoot(GameObject root, Transform visualRoot)
    {
        SpriteRenderer? rootSprite = root.GetComponent<SpriteRenderer>();
        SpriteRenderer? visualSprite = visualRoot.GetComponent<SpriteRenderer>();
        if (rootSprite == null)
        {
            return;
        }

        if (visualSprite == null)
        {
            visualSprite = visualRoot.gameObject.AddComponent<SpriteRenderer>();
        }

        // Prefab variants often override tint on the root SpriteRenderer — copy that onto VisualRoot.
        Color tint = rootSprite.color;
        Sprite? sprite = rootSprite.sprite;
        EditorUtility.CopySerialized(rootSprite, visualSprite);
        visualSprite.color = tint;
        visualSprite.sprite = sprite;
        // Floor/walls are sortingOrder 0; elite under-glow uses 1; body sits above at 2.
        if (visualSprite.sortingOrder < 2)
        {
            visualSprite.sortingOrder = 2;
        }

        rootSprite.enabled = false;
        visualSprite.enabled = true;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
