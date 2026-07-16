#nullable enable

using System;
using UnityEngine;

/// <summary>
/// Tunable encounter difficulty curve, theme weights, role mix, and elite rules for WaveComposer.
/// </summary>
[CreateAssetMenu(fileName = "WaveComposerSettings", menuName = "Scriptable Objects/WaveComposerSettings")]
public class WaveComposerSettings : ScriptableObject
{
    [Serializable]
    public class CombatProfile
    {
        [Tooltip("Threat budget spent on EACH wave (not split across the fight).")]
        [Min(1f)] public float threatBudget = 80f;
        [Min(1)] public int minWaves = 2;
        [Min(1)] public int maxWaves = 3;
        [HideInInspector] public float hpMultiplier = 1f;
        [HideInInspector] public float damageMultiplier = 1f;
        public bool guaranteeElite;
    }

    [Header("Per-combat profiles (index 0/1/2 within a block)")]
    [Tooltip("Combat 1 / 2 / 3 in each Combat×3 block. threatBudget is per wave; HP/damage stay flat within a block.")]
    public CombatProfile[] combatProfiles =
    {
        new CombatProfile { threatBudget = 80f, minWaves = 2, maxWaves = 2, hpMultiplier = 1f, damageMultiplier = 1f, guaranteeElite = false },
        new CombatProfile { threatBudget = 110f, minWaves = 2, maxWaves = 3, hpMultiplier = 1f, damageMultiplier = 1f, guaranteeElite = false },
        new CombatProfile { threatBudget = 140f, minWaves = 3, maxWaves = 3, hpMultiplier = 1f, damageMultiplier = 1f, guaranteeElite = true },
    };

    [Header("Later-block scaling")]
    [Tooltip("Scales per-wave threat in later Combat×3 blocks (combat slots within a block still share the same HP/damage).")]
    [Min(0f)] public float blockThreatScale = 0.35f;
    [Min(0f)] public float blockHpScale = 0.25f;
    [Min(0f)] public float blockDamageScale = 0.1f;

    [Header("Theme weights (Mixed, Physical, Fire, Ice, Lightning, Wind)")]
    public float[] themeWeightsCombat0 = { 40f, 25f, 12f, 12f, 11f, 10f };
    public float[] themeWeightsCombat1 = { 25f, 15f, 20f, 20f, 20f, 18f };
    public float[] themeWeightsCombat2 = { 15f, 10f, 25f, 25f, 25f, 22f };

    [Header("Role weights (Melee, Ranged, BeamSniper, Shotgun, Turret)")]
    public float[] roleWeightsCombat0 = { 45f, 30f, 5f, 10f, 10f };
    public float[] roleWeightsCombat1 = { 25f, 20f, 15f, 20f, 20f };
    public float[] roleWeightsCombat2 = { 15f, 15f, 25f, 25f, 20f };

    [Header("Composition limits")]
    [Min(1)] public int maxEnemiesPerWave = 6;
    [Min(1)] public int minEnemiesPerWave = 1;
    [Min(0f)] public float delayAfterClear = 1.5f;
    [Range(0.5f, 1f)] public float themedElementBias = 0.72f;

    public CombatProfile ResolveProfile(int blockIndex, int combatIndexInBlock)
    {
        CombatProfile? baseProfile = null;
        if (combatProfiles != null && combatProfiles.Length > 0)
        {
            int idx = Mathf.Clamp(combatIndexInBlock, 0, combatProfiles.Length - 1);
            baseProfile = combatProfiles[idx];
        }

        baseProfile ??= new CombatProfile();

        float block = Mathf.Max(0, blockIndex);
        // aisara => Within a block, HP/damage stay identical; only threat + wave count differ by combat slot.
        // Block index still scales HP/damage so later blocks get tankier/spicier enemies.
        float sharedHp = 1f * (1f + block * blockHpScale);
        float sharedDamage = 1f * (1f + block * blockDamageScale);
        return new CombatProfile
        {
            threatBudget = baseProfile.threatBudget * (1f + block * blockThreatScale),
            minWaves = baseProfile.minWaves,
            maxWaves = Mathf.Max(baseProfile.minWaves, baseProfile.maxWaves),
            hpMultiplier = sharedHp,
            damageMultiplier = sharedDamage,
            guaranteeElite = baseProfile.guaranteeElite,
        };
    }

    public float[] GetThemeWeights(int combatIndexInBlock) =>
        combatIndexInBlock switch
        {
            0 => themeWeightsCombat0,
            1 => themeWeightsCombat1,
            _ => themeWeightsCombat2,
        };

    public float[] GetRoleWeights(int combatIndexInBlock) =>
        combatIndexInBlock switch
        {
            0 => roleWeightsCombat0,
            1 => roleWeightsCombat1,
            _ => roleWeightsCombat2,
        };

    public SpawnModifiers ToDifficultyModifiers(CombatProfile profile) =>
        new SpawnModifiers
        {
            HpMultiplier = profile.hpMultiplier,
            DamageMultiplier = profile.damageMultiplier,
            SpeedMultiplier = 1f,
            AttackRateMultiplier = 1f,
            ChargeTimeMultiplier = 1f,
            ProjectileSpeedMultiplier = 1f,
            ScaleMultiplier = 1f,
        };
}
