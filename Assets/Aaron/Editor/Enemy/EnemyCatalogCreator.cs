#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates/refreshes EnemyCatalog.asset with all 20 archetypes and default elemental knobs,
/// then wires it onto CoreSystems → RunManager.
/// Menu: Henry/Generate Enemy Catalog
/// </summary>
public static class EnemyCatalogCreator
{
    private const string CatalogFolder = "Assets/Aaron/ScriptableObjects";
    private const string CatalogPath = CatalogFolder + "/EnemyCatalog.asset";
    private const string PrefabFolder = "Assets/Visuals/Prefabs/Enemies";
    private const string CoreSystemsPrefabPath = "Assets/Aaron/Prefabs/CoreSystems.prefab";

    private readonly struct ArchetypeSpec
    {
        public ArchetypeSpec(string id, string prefabFileName, Element element, EnemyRole role, float threat, bool applyKnobs)
        {
            Id = id;
            PrefabFileName = prefabFileName;
            Element = element;
            Role = role;
            Threat = threat;
            ApplyKnobs = applyKnobs;
        }

        public string Id { get; }
        public string PrefabFileName { get; }
        public Element Element { get; }
        public EnemyRole Role { get; }
        public float Threat { get; }
        public bool ApplyKnobs { get; }
    }

    private static readonly ArchetypeSpec[] Specs =
    {
        // Legacy — elemental variance baked into prefabs; difficulty-only at spawn.
        new("melee_physical", "MeleeEnemyPhysical", Element.Physical, EnemyRole.Melee, 10f, false),
        new("melee_fire", "MeleeEnemyFire", Element.Fire, EnemyRole.Melee, 11f, false),
        new("melee_ice", "MeleeEnemyIce", Element.Ice, EnemyRole.Melee, 14f, false),
        new("melee_lightning", "MeleeEnemyLightning", Element.Lightning, EnemyRole.Melee, 11f, false),
        new("ranged_physical", "RangedEnemyPhysical", Element.Physical, EnemyRole.Ranged, 12f, false),
        new("ranged_fire", "RangedEnemyFire", Element.Fire, EnemyRole.Ranged, 13f, false),
        new("ranged_ice", "RangedEnemyIce", Element.Ice, EnemyRole.Ranged, 16f, false),
        new("ranged_lightning", "RangedEnemyLightning", Element.Lightning, EnemyRole.Ranged, 13f, false),

        // New archetypes — Physical-baseline prefabs; ElementStatKnobs applied at spawn.
        new("beamsniper_physical", "BeamSniper_Physical", Element.Physical, EnemyRole.BeamSniper, 18f, true),
        new("beamsniper_fire", "BeamSniper_Fire", Element.Fire, EnemyRole.BeamSniper, 19f, true),
        new("beamsniper_ice", "BeamSniper_Ice", Element.Ice, EnemyRole.BeamSniper, 22f, true),
        new("beamsniper_lightning", "BeamSniper_Lightning", Element.Lightning, EnemyRole.BeamSniper, 19f, true),
        new("shotgun_physical", "Shotgun_Physical", Element.Physical, EnemyRole.Shotgun, 14f, true),
        new("shotgun_fire", "Shotgun_Fire", Element.Fire, EnemyRole.Shotgun, 15f, true),
        new("shotgun_ice", "Shotgun_Ice", Element.Ice, EnemyRole.Shotgun, 18f, true),
        new("shotgun_lightning", "Shotgun_Lightning", Element.Lightning, EnemyRole.Shotgun, 15f, true),
        new("turret_physical", "Turret_Physical", Element.Physical, EnemyRole.Turret, 16f, true),
        new("turret_fire", "Turret_Fire", Element.Fire, EnemyRole.Turret, 17f, true),
        new("turret_ice", "Turret_Ice", Element.Ice, EnemyRole.Turret, 20f, true),
        new("turret_lightning", "Turret_Lightning", Element.Lightning, EnemyRole.Turret, 17f, true),
    };

    [MenuItem("Henry/Generate Enemy Catalog")]
    public static void GenerateFromMenu()
    {
        EnsureFolder(CatalogFolder);

        EnemyCatalog catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var entries = new List<EnemyArchetype>(Specs.Length);
        int missing = 0;
        foreach (ArchetypeSpec spec in Specs)
        {
            string prefabPath = $"{PrefabFolder}/{spec.PrefabFileName}.prefab";
            GameObject? prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"EnemyCatalogCreator: missing prefab {prefabPath}");
                missing++;
                continue;
            }

            entries.Add(new EnemyArchetype
            {
                id = spec.Id,
                prefab = prefab,
                element = spec.Element,
                role = spec.Role,
                baseThreatCost = spec.Threat,
                applyElementKnobsAtSpawn = spec.ApplyKnobs,
            });
        }

        var knobs = new List<ElementStatKnobs>
        {
            ElementStatKnobs.DefaultFor(Element.Physical),
            ElementStatKnobs.DefaultFor(Element.Fire),
            ElementStatKnobs.DefaultFor(Element.Ice),
            ElementStatKnobs.DefaultFor(Element.Lightning),
        };

        catalog.EditorSetArchetypes(entries);
        catalog.EditorSetElementKnobs(knobs);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        WireCatalogToCoreSystems(catalog);

        Debug.Log(
            $"EnemyCatalogCreator: wrote {CatalogPath} with {entries.Count}/20 archetypes" +
            (missing > 0 ? $" ({missing} missing)." : ".") +
            " Wired to CoreSystems → RunManager.");
    }

    private static void WireCatalogToCoreSystems(EnemyCatalog catalog)
    {
        GameObject? root = PrefabUtility.LoadPrefabContents(CoreSystemsPrefabPath);
        if (root == null)
        {
            Debug.LogError($"EnemyCatalogCreator: could not load {CoreSystemsPrefabPath}");
            return;
        }

        try
        {
            RunManager? runManager = root.GetComponentInChildren<RunManager>(true);
            if (runManager == null)
            {
                Debug.LogError("EnemyCatalogCreator: RunManager not found on CoreSystems.");
                return;
            }

            SerializedObject serialized = new SerializedObject(runManager);
            SerializedProperty property = serialized.FindProperty("enemyCatalog");
            if (property == null)
            {
                Debug.LogError("EnemyCatalogCreator: RunManager.enemyCatalog field not found (script compile?).");
                return;
            }

            property.objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CoreSystemsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/') ?? "Assets";
        string leaf = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            AssetDatabase.CreateFolder("Assets", "Aaron");
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
