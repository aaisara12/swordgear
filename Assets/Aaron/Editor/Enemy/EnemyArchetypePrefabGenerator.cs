#if UNITY_EDITOR
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds 12 new enemy archetype prefabs (turret, shotgun, beam sniper × 4 elements).
/// Menu: Henry/Generate New Enemy Archetype Prefabs
/// </summary>
public static class EnemyArchetypePrefabGenerator
{
    private const string OutputFolder = "Assets/Visuals/Prefabs/Enemies";
    private const string ProjectileGuid = "12b1879effb6ca744b78903d8ec9dcb5";
    private const string BeamLaserPrefabPath = "Assets/Visuals/Prefabs/Enemies/EnemyBeamLaser.prefab";

    private enum ArchetypeRole
    {
        Turret,
        Shotgun,
        BeamSniper,
    }

    private readonly struct ElementVisual
    {
        public ElementVisual(Element element, string sourcePrefabName, Color tint)
        {
            Element = element;
            SourcePrefabName = sourcePrefabName;
            Tint = tint;
        }

        public Element Element { get; }
        public string SourcePrefabName { get; }
        public Color Tint { get; }
    }

    private static readonly ElementVisual[] Elements =
    {
        new(Element.Physical, "RangedEnemyPhysical", new Color(0.75f, 0.75f, 0.75f, 1f)),
        new(Element.Fire, "RangedEnemyFire", new Color(0.85f, 0.35f, 0.2f, 1f)),
        new(Element.Ice, "RangedEnemyIce", new Color(0.45f, 0.7f, 1f, 1f)),
        new(Element.Lightning, "RangedEnemyLightning", new Color(0.95f, 0.9f, 0.35f, 1f)),
    };

    [MenuItem("Henry/Generate New Enemy Archetype Prefabs")]
    public static void GenerateFromMenu()
    {
        GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            AssetDatabase.GUIDToAssetPath(ProjectileGuid));
        if (projectilePrefab == null)
        {
            Debug.LogError("EnemyArchetypePrefabGenerator: Arrow projectile prefab not found.");
            return;
        }

        GameObject? beamLaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeamLaserPrefabPath);
        if (beamLaserPrefab == null)
        {
            Debug.LogError(
                $"EnemyArchetypePrefabGenerator: missing {BeamLaserPrefabPath}. Run Henry → Generate Enemy Beam Laser Prefab first.");
            return;
        }

        EnsureFolder(OutputFolder);
        int created = 0;

        foreach (ElementVisual elementVisual in Elements)
        {
            string sourcePath = $"{OutputFolder}/{elementVisual.SourcePrefabName}.prefab";
            GameObject? sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"EnemyArchetypePrefabGenerator: missing source prefab {sourcePath}");
                continue;
            }

            foreach (ArchetypeRole role in Enum.GetValues(typeof(ArchetypeRole)))
            {
                string prefabName = $"{role}_{elementVisual.Element}";
                string outputPath = $"{OutputFolder}/{prefabName}.prefab";
                if (CreateArchetypePrefab(sourcePrefab, outputPath, prefabName, role, elementVisual, projectilePrefab, beamLaserPrefab))
                {
                    created++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"EnemyArchetypePrefabGenerator: wrote/updated {created} archetype prefab(s) in {OutputFolder}.");
    }

    private static bool CreateArchetypePrefab(
        GameObject sourcePrefab,
        string outputPath,
        string prefabName,
        ArchetypeRole role,
        ElementVisual elementVisual,
        GameObject projectilePrefab,
        GameObject beamLaserPrefab)
    {
        GameObject? instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (instance == null)
        {
            return false;
        }

        try
        {
            instance.name = prefabName;

            EnemyController controller = instance.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.element = elementVisual.Element;
            }

            SpriteRenderer? spriteRenderer = instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = elementVisual.Tint;
            }

            if (instance.GetComponent<EnemyAttackDamage>() == null)
            {
                instance.AddComponent<EnemyAttackDamage>();
            }

            ConfigureRole(instance, role, projectilePrefab, beamLaserPrefab);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            return saved != null;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ConfigureRole(GameObject instance, ArchetypeRole role, GameObject projectilePrefab, GameObject beamLaserPrefab)
    {
        RemoveComponent<RangedAttackStrategy>(instance);
        RemoveComponent<ShotgunAttackStrategy>(instance);
        RemoveComponent<BeamSniperAttackStrategy>(instance);
        RemoveComponent<TurretAttackStrategy>(instance);
        RemoveComponent<StrafeMovementStrategy>(instance);
        RemoveComponent<StationaryMovementStrategy>(instance);
        // Base RangedEnemy prefab carries a FollowPlayerStrategy; remove it so EnemyController's
        // GetComponent<IMovementStrategy>() resolves to the role's intended movement, not chase.
        RemoveComponent<FollowPlayerStrategy>(instance);

        switch (role)
        {
            case ArchetypeRole.Turret:
                instance.AddComponent<StationaryMovementStrategy>();
                TurretAttackStrategy turret = instance.AddComponent<TurretAttackStrategy>();
                SetProjectilePrefab(turret, projectilePrefab);
                SetTurret(turret);
                SetEnemyStats(instance, hp: 24f, speed: 0f);
                break;

            case ArchetypeRole.Shotgun:
                StrafeMovementStrategy shotgunMove = instance.AddComponent<StrafeMovementStrategy>();
                SetStrafe(shotgunMove, distance: 4.5f, speed: 2.5f);
                ShotgunAttackStrategy shotgun = instance.AddComponent<ShotgunAttackStrategy>();
                SetProjectilePrefab(shotgun, projectilePrefab);
                SetEnemyStats(instance, hp: 22f, speed: 2f);
                break;

            case ArchetypeRole.BeamSniper:
                StrafeMovementStrategy sniperMove = instance.AddComponent<StrafeMovementStrategy>();
                SetStrafe(sniperMove, distance: 7f, speed: 1.2f);
                BeamSniperAttackStrategy sniper = instance.AddComponent<BeamSniperAttackStrategy>();
                SetBeamLaserPrefab(sniper, beamLaserPrefab);
                SetEnemyStats(instance, hp: 18f, speed: 1f);
                break;
        }
    }

    private static void SetEnemyStats(GameObject instance, float hp, float speed)
    {
        SerializedObject serialized = new SerializedObject(instance.GetComponent<EnemyController>());
        serialized.FindProperty("hp").floatValue = hp;
        serialized.FindProperty("speed").floatValue = speed;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStrafe(StrafeMovementStrategy strategy, float distance, float speed)
    {
        SerializedObject serialized = new SerializedObject(strategy);
        serialized.FindProperty("strafeDistance").floatValue = distance;
        serialized.FindProperty("strafeSpeed").floatValue = speed;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetProjectilePrefab(Component attackStrategy, GameObject projectilePrefab)
    {
        SerializedObject serialized = new SerializedObject(attackStrategy);
        serialized.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetTurret(TurretAttackStrategy strategy)
    {
        SerializedObject serialized = new SerializedObject(strategy);
        serialized.FindProperty("attackFrequency").floatValue = 3f;
        serialized.FindProperty("burstSize").intValue = 6;
        serialized.FindProperty("reloadDuration").floatValue = 2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBeamLaserPrefab(BeamSniperAttackStrategy strategy, GameObject beamLaserPrefab)
    {
        SerializedObject serialized = new SerializedObject(strategy);
        serialized.FindProperty("beamLaserPrefab").objectReferenceValue = beamLaserPrefab;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveComponent<T>(GameObject instance) where T : Component
    {
        T? existing = instance.GetComponent<T>();
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        const string parent = "Assets/Visuals/Prefabs";
        if (!AssetDatabase.IsValidFolder(parent))
        {
            AssetDatabase.CreateFolder("Assets/Visuals", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parent, "Enemies");
        }
    }
}
#endif
