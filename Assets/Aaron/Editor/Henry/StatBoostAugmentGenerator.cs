#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shop;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates tiered stat-boost augment ScriptableObjects. Menu: Henry/Generate Stat Boost Augments
/// </summary>
public static class StatBoostAugmentGenerator
{
    private const string OutputFolder = "Assets/Aaron/ScriptableObjects/Items/StatBoosts";
    private const string CatalogPath = "Assets/Aaron/ScriptableObjects/AugmentCatalog.asset";
    private const string ElementItemsFolder = "Assets/Aaron/ScriptableObjects/Items";

    private readonly struct AugmentDef
    {
        public AugmentDef(
            string fileName,
            string displayName,
            string description,
            AugmentQualityTier tier,
            params StatBoostEntry[] boosts)
        {
            FileName = fileName;
            DisplayName = displayName;
            Description = description;
            Tier = tier;
            Boosts = boosts;
        }

        public string FileName { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public AugmentQualityTier Tier { get; }
        public StatBoostEntry[] Boosts { get; }
    }

    private readonly struct HealDef
    {
        public HealDef(string fileName, string displayName, string description, AugmentQualityTier tier, float healPercent)
        {
            FileName = fileName;
            DisplayName = displayName;
            Description = description;
            Tier = tier;
            HealPercent = healPercent;
        }

        public string FileName { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public AugmentQualityTier Tier { get; }
        public float HealPercent { get; }
    }

    [MenuItem("Henry/Generate Stat Boost Augments")]
    public static void GenerateFromMenu()
    {
        EnsureFolder(OutputFolder);
        List<AugmentDef> definitions = BuildDefinitions();
        List<HealDef> healDefinitions = BuildHealDefinitions();
        var activeFileNames = new HashSet<string>(definitions.Select(d => d.FileName));
        foreach (HealDef healDef in healDefinitions)
        {
            activeFileNames.Add(healDef.FileName);
        }

        int created = 0;
        int updated = 0;

        foreach (AugmentDef def in definitions)
        {
            if (CreateOrUpdateAsset(def))
            {
                created++;
            }
            else
            {
                updated++;
            }
        }

        foreach (HealDef healDef in healDefinitions)
        {
            if (CreateOrUpdateHealAsset(healDef))
            {
                created++;
            }
            else
            {
                updated++;
            }
        }

        int removed = RemoveOrphanedAssets(activeFileNames);
        int elementUpgrades = SetElementUpgradesToDiamond();
        ReloadCatalog();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"StatBoostAugmentGenerator: {created} created, {updated} updated, {removed} removed. " +
            $"{healDefinitions.Count} instant heal augment(s). " +
            $"{elementUpgrades} element upgrade(s) set to Diamond tier. Catalog refreshed.");
    }

    private static List<AugmentDef> BuildDefinitions()
    {
        var defs = new List<AugmentDef>(40);

        // Bronze (~10% for percent stats)
        defs.Add(Def("bronze_swift_step", "Swift Step", "Move 10% faster.", AugmentQualityTier.Low, E(StatBoostKind.MoveSpeed, 10)));
        defs.Add(Def("bronze_heavy_edge", "Heavy Edge", "Deal 10% more damage.", AugmentQualityTier.Low, E(StatBoostKind.DamageMultiplier, 10)));
        defs.Add(Def("bronze_vitality", "Vitality", "+10% max HP.", AugmentQualityTier.Low, E(StatBoostKind.MaxHp, 10)));
        defs.Add(Def("bronze_ranged_edge", "Ranged Edge", "Ranged attacks deal 10% more damage.", AugmentQualityTier.Low, E(StatBoostKind.RangedDamage, 10)));
        defs.Add(Def("bronze_quick_release", "Quick Release", "Projectiles fly 10% faster.", AugmentQualityTier.Low, E(StatBoostKind.ProjectileSpeed, 10)));
        defs.Add(Def("bronze_ultimate_spark", "Ultimate Spark", "Ultimate charges 10% faster.", AugmentQualityTier.Low, E(StatBoostKind.UltimateCharge, 10)));
        defs.Add(Def("bronze_vampiric_touch", "Vampiric Touch", "Heal for 10% of damage dealt.", AugmentQualityTier.Low, E(StatBoostKind.Lifesteal, 10)));
        defs.Add(Def("bronze_passive_heal", "Passive Heal", "Regenerate 1% of max HP per second.", AugmentQualityTier.Low, E(StatBoostKind.Regen, 1f)));

        // Silver (~30%)
        defs.Add(Def("silver_wind_runner", "Wind Runner", "+30% movement speed.", AugmentQualityTier.Medium, E(StatBoostKind.MoveSpeed, 30)));
        defs.Add(Def("silver_tempered_steel", "Tempered Steel", "Deal 30% more damage.", AugmentQualityTier.Medium, E(StatBoostKind.DamageMultiplier, 30)));
        defs.Add(Def("silver_stout_heart", "Stout Heart", "+30% max health.", AugmentQualityTier.Medium, E(StatBoostKind.MaxHp, 30)));
        defs.Add(Def("silver_sharpshooter", "Sharpshooter", "+30% ranged damage.", AugmentQualityTier.Medium, E(StatBoostKind.RangedDamage, 30)));
        defs.Add(Def("silver_overcharged_shot", "Overcharged Shot", "+30% projectile speed.", AugmentQualityTier.Medium, E(StatBoostKind.ProjectileSpeed, 30)));
        defs.Add(Def("silver_fury_accumulation", "Fury Accumulation", "+30% ultimate charge rate.", AugmentQualityTier.Medium, E(StatBoostKind.UltimateCharge, 30)));
        defs.Add(Def("silver_blood_pact", "Blood Pact", "30% of damage dealt returns as HP.", AugmentQualityTier.Medium, E(StatBoostKind.Lifesteal, 30)));
        defs.Add(Def("silver_steady_recovery", "Steady Recovery", "3% max HP per second regen.", AugmentQualityTier.Medium, E(StatBoostKind.Regen, 3f)));

        // Gold (~50%)
        defs.Add(Def("gold_featherweight", "Featherweight", "+50% move speed.", AugmentQualityTier.High, E(StatBoostKind.MoveSpeed, 50)));
        defs.Add(Def("gold_executioner", "Executioner", "Deal 50% more damage.", AugmentQualityTier.High, E(StatBoostKind.DamageMultiplier, 50)));
        defs.Add(Def("gold_titans_blood", "Titan's Blood", "+50% max HP.", AugmentQualityTier.High, E(StatBoostKind.MaxHp, 50)));
        defs.Add(Def("gold_snipers_mark", "Sniper's Mark", "+50% ranged damage.", AugmentQualityTier.High, E(StatBoostKind.RangedDamage, 50)));
        defs.Add(Def("gold_lightning_cast", "Lightning Cast", "+50% projectile speed.", AugmentQualityTier.High, E(StatBoostKind.ProjectileSpeed, 50)));
        defs.Add(Def("gold_overdrive", "Overdrive", "+50% ultimate charge gain.", AugmentQualityTier.High, E(StatBoostKind.UltimateCharge, 50)));
        defs.Add(Def("gold_soul_leech", "Soul Leech", "50% lifesteal.", AugmentQualityTier.High, E(StatBoostKind.Lifesteal, 50)));
        defs.Add(Def("gold_phoenix_ash", "Phoenix Ash", "5% max HP per second.", AugmentQualityTier.High, E(StatBoostKind.Regen, 5f)));

        // Diamond — multi-stat / trade-offs
        defs.Add(Def("diamond_berserker", "Berserker", "More damage, slower movement.", AugmentQualityTier.Elite,
            E(StatBoostKind.DamageMultiplier, 30), E(StatBoostKind.MoveSpeed, -15)));
        defs.Add(Def("diamond_glass_cannon", "Glass Cannon", "Big damage, less HP.", AugmentQualityTier.Elite,
            E(StatBoostKind.DamageMultiplier, 50), E(StatBoostKind.MaxHp, -25)));
        defs.Add(Def("diamond_tank", "Tank", "Much more HP, slower and less damage.", AugmentQualityTier.Elite,
            E(StatBoostKind.MaxHp, 50), E(StatBoostKind.MoveSpeed, -15), E(StatBoostKind.DamageMultiplier, -10)));
        defs.Add(Def("diamond_swift_striker", "Swift Striker", "Faster and stronger.", AugmentQualityTier.Elite,
            E(StatBoostKind.MoveSpeed, 30), E(StatBoostKind.DamageMultiplier, 20)));
        defs.Add(Def("diamond_ranged_specialist", "Ranged Specialist", "Ranged damage and projectile speed.", AugmentQualityTier.Elite,
            E(StatBoostKind.RangedDamage, 30), E(StatBoostKind.ProjectileSpeed, 40)));
        defs.Add(Def("diamond_fury_surge", "Fury Surge", "Faster ultimate charge and more damage.", AugmentQualityTier.Elite,
            E(StatBoostKind.UltimateCharge, 30), E(StatBoostKind.DamageMultiplier, 20)));
        defs.Add(Def("diamond_vampire", "Vampire", "Lifesteal and passive regen.", AugmentQualityTier.Elite,
            E(StatBoostKind.Lifesteal, 20), E(StatBoostKind.Regen, 2f)));
        defs.Add(Def("diamond_brittle_edge", "Brittle Edge", "High damage, less max HP.", AugmentQualityTier.Elite,
            E(StatBoostKind.DamageMultiplier, 40), E(StatBoostKind.MaxHp, -20)));
        defs.Add(Def("diamond_turtle", "Turtle", "Much more HP, less move speed.", AugmentQualityTier.Elite,
            E(StatBoostKind.MaxHp, 50), E(StatBoostKind.MoveSpeed, -20)));
        defs.Add(Def("diamond_haste_penalty", "Haste Penalty", "Very fast, less damage.", AugmentQualityTier.Elite,
            E(StatBoostKind.MoveSpeed, 50), E(StatBoostKind.DamageMultiplier, -15)));

        return defs;
    }

    private static List<HealDef> BuildHealDefinitions() => new()
    {
        new("bronze_first_aid", "First Aid", "Instantly restore 10% of max HP.", AugmentQualityTier.Low, 10f),
        new("silver_field_dressing", "Field Dressing", "Instantly restore 30% of max HP.", AugmentQualityTier.Medium, 30f),
        new("gold_panacea", "Panacea", "Instantly restore 50% of max HP.", AugmentQualityTier.High, 50f),
        new("diamond_rebirth", "Rebirth", "Instantly restore 75% of max HP.", AugmentQualityTier.Elite, 75f),
    };

    private static AugmentDef Def(
        string fileName,
        string displayName,
        string description,
        AugmentQualityTier tier,
        params StatBoostEntry[] boosts) =>
        new(fileName, displayName, description, tier, boosts);

    private static StatBoostEntry E(StatBoostKind kind, float value) =>
        new() { kind = kind, value = value };

    private static bool CreateOrUpdateAsset(AugmentDef def)
    {
        string path = $"{OutputFolder}/{def.FileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<StatBoostLoadableStoreItem>(path);
        bool isNew = existing == null;
        var asset = isNew
            ? ScriptableObject.CreateInstance<StatBoostLoadableStoreItem>()
            : existing;

        ApplyDefinition(asset, def);

        if (isNew)
        {
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        return isNew;
    }

    private static void ApplyDefinition(StatBoostLoadableStoreItem asset, AugmentDef def)
    {
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("displayName").stringValue = def.DisplayName;
        serialized.FindProperty("description").stringValue = def.Description;
        serialized.FindProperty("cost").intValue = 0;
        serialized.FindProperty("qualityTier").enumValueIndex = (int)def.Tier;

        SerializedProperty boostsProp = serialized.FindProperty("statBoosts");
        boostsProp.arraySize = def.Boosts.Length;
        for (int i = 0; i < def.Boosts.Length; i++)
        {
            SerializedProperty entry = boostsProp.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("kind").enumValueIndex = (int)def.Boosts[i].kind;
            entry.FindPropertyRelative("value").floatValue = def.Boosts[i].value;
        }

        serialized.FindProperty("id").stringValue = StatBoostSerializer.Serialize(def.Boosts);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool CreateOrUpdateHealAsset(HealDef def)
    {
        string path = $"{OutputFolder}/{def.FileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<InstantHealLoadableStoreItem>(path);
        bool isNew = existing == null;
        var asset = isNew
            ? ScriptableObject.CreateInstance<InstantHealLoadableStoreItem>()
            : existing;

        var serialized = new SerializedObject(asset);
        serialized.FindProperty("displayName").stringValue = def.DisplayName;
        serialized.FindProperty("description").stringValue = def.Description;
        serialized.FindProperty("cost").intValue = 0;
        serialized.FindProperty("qualityTier").enumValueIndex = (int)def.Tier;
        serialized.FindProperty("healPercentOfMaxHp").floatValue = def.HealPercent;
        serialized.FindProperty("id").stringValue = InstantHealSerializer.Serialize(def.HealPercent);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (isNew)
        {
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        return isNew;
    }

    private static int RemoveOrphanedAssets(HashSet<string> activeFileNames)
    {
        int removed = 0;
        removed += RemoveOrphanedAssetsOfType<StatBoostLoadableStoreItem>(activeFileNames);
        removed += RemoveOrphanedAssetsOfType<InstantHealLoadableStoreItem>(activeFileNames);
        return removed;
    }

    private static int RemoveOrphanedAssetsOfType<T>(HashSet<string> activeFileNames) where T : LoadableStoreItem
    {
        int removed = 0;
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { OutputFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (activeFileNames.Contains(fileName))
            {
                continue;
            }

            if (AssetDatabase.DeleteAsset(path))
            {
                removed++;
            }
        }

        return removed;
    }

    private static int SetElementUpgradesToDiamond()
    {
        string[] guids = AssetDatabase.FindAssets("t:ElementUpgradeLoadableStoreItem", new[] { ElementItemsFolder });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ElementUpgradeLoadableStoreItem>(path);
            if (asset == null)
            {
                continue;
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("qualityTier").enumValueIndex = (int)AugmentQualityTier.Elite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            count++;
        }

        return count;
    }

    private static void ReloadCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<LoadableStoreItemCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning($"StatBoostAugmentGenerator: catalog not found at {CatalogPath}");
            return;
        }

        catalog.Load();
        EditorUtility.SetDirty(catalog);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        const string root = "Assets/Aaron/ScriptableObjects/Items";
        if (!AssetDatabase.IsValidFolder($"{root}/StatBoosts"))
        {
            AssetDatabase.CreateFolder(root, "StatBoosts");
        }
    }
}

#endif
