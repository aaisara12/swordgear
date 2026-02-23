#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Shop;

/// <summary>
/// Holds player stat modifiers from stat-boost augments. Populated from PlayerBlob at game start
/// and re-applied whenever the blob's inventory changes (e.g. after purchasing an augment).
/// </summary>
public class PlayerStatModifiers : InitializeableGameComponent
{
    public static PlayerStatModifiers? Instance { get; private set; }

    /// <summary> Fired after modifiers are re-applied (e.g. after a purchase). Use to refresh MaxHp, start Regen, etc. </summary>
    public static event Action? OnStatsChanged;

    // Additive multipliers (1 = no change). Stacks from multiple same augment.
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float DamageFlatBonus { get; private set; }
    public float MaxHpMultiplier { get; private set; } = 1f;
    public float RangedDamageMultiplierBonus { get; private set; }
    public float ProjectileSpeedMultiplier { get; private set; } = 1f;
    public float ComboDurationBonus { get; private set; }
    public float UltimateChargeMultiplier { get; private set; } = 1f;
    public float LifestealPercent { get; private set; }
    public float RegenPercentPerSecond { get; private set; }

    private IReadOnlyPlayerBlob? _playerBlob;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (_playerBlob?.InventoryItems is IReadOnlyObservableDictionary<string, int> observable)
            observable.DictionaryChanged -= HandleInventoryChanged;
        if (Instance == this)
            Instance = null;
        OnStatsChanged = null;
    }

    public override void InitializeOnGameStart(IReadOnlyPlayerBlob playerBlob)
    {
        if (_playerBlob?.InventoryItems is IReadOnlyObservableDictionary<string, int> oldObs)
            oldObs.DictionaryChanged -= HandleInventoryChanged;

        _playerBlob = playerBlob;
        if (playerBlob.InventoryItems is IReadOnlyObservableDictionary<string, int> obs)
            obs.DictionaryChanged += HandleInventoryChanged;

        ReapplyFromBlob();
    }

    private void HandleInventoryChanged(ObservableDictionaryChangedEventArgs<string, int> _)
    {
        ReapplyFromBlob();
    }

    private void ReapplyFromBlob()
    {
        if (_playerBlob == null) return;
        Reset();
        foreach (var kvp in _playerBlob.InventoryItems)
        {
            if (!StatBoostSerializer.TryDeserializeEntries(kvp.Key, out List<StatBoostEntry> entries))
                continue;
            int count = kvp.Value;
            if (count <= 0) continue;
            foreach (var entry in entries)
                ApplyStatBoost(entry.kind, entry.value, count);
        }
        OnStatsChanged?.Invoke();
    }

    private void Reset()
    {
        MoveSpeedMultiplier = 1f;
        DamageFlatBonus = 0f;
        MaxHpMultiplier = 1f;
        RangedDamageMultiplierBonus = 0f;
        ProjectileSpeedMultiplier = 1f;
        ComboDurationBonus = 0f;
        UltimateChargeMultiplier = 1f;
        LifestealPercent = 0f;
        RegenPercentPerSecond = 0f;
    }

    private void ApplyStatBoost(StatBoostKind kind, float value, int stacks)
    {
        float total = value * stacks;
        switch (kind)
        {
            case StatBoostKind.MoveSpeed:
                MoveSpeedMultiplier += (total / 100f);
                break;
            case StatBoostKind.BaseDamage:
                DamageFlatBonus += total;
                break;
            case StatBoostKind.MaxHp:
                MaxHpMultiplier += (total / 100f);
                break;
            case StatBoostKind.RangedDamage:
                RangedDamageMultiplierBonus += (total / 100f);
                break;
            case StatBoostKind.ProjectileSpeed:
                ProjectileSpeedMultiplier += (total / 100f);
                break;
            case StatBoostKind.ComboDuration:
                ComboDurationBonus += total;
                break;
            case StatBoostKind.UltimateCharge:
                UltimateChargeMultiplier += (total / 100f);
                break;
            case StatBoostKind.Lifesteal:
                LifestealPercent += total;
                break;
            case StatBoostKind.Regen:
                RegenPercentPerSecond += total;
                break;
        }
    }
}
