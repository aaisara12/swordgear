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

    // All multiplier stats add percent to base 1.0; independent +X% bonuses stack additively.
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;
    public float MaxHpMultiplier { get; private set; } = 1f;
    public float RangedDamageMultiplierBonus { get; private set; }
    public float ProjectileSpeedMultiplier { get; private set; } = 1f;
    public float UltimateChargeMultiplier { get; private set; } = 1f;
    public float LifestealPercent { get; private set; }
    public float RegenPercentPerSecond { get; private set; }
    public float MeleeRangeMultiplier { get; private set; } = 1f;
    public float AttackSpeedMultiplier { get; private set; } = 1f;

    private PlayerBlob? _mutablePlayerBlob;
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
        _mutablePlayerBlob = playerBlob as PlayerBlob;
        if (playerBlob.InventoryItems is IReadOnlyObservableDictionary<string, int> obs)
            obs.DictionaryChanged += HandleInventoryChanged;

        ReapplyFromBlob();
    }

    private void HandleInventoryChanged(ObservableDictionaryChangedEventArgs<string, int> _)
    {
        ConsumeInstantHealItems();
        ReapplyFromBlob();
    }

    private void ConsumeInstantHealItems()
    {
        if (_mutablePlayerBlob == null || _playerBlob == null)
        {
            return;
        }

        var healIdsToRemove = new List<string>();
        foreach (var kvp in _playerBlob.InventoryItems)
        {
            if (!InstantHealSerializer.TryDeserialize(kvp.Key, out float percentOfMaxHp))
            {
                continue;
            }

            int stacks = kvp.Value;
            for (int i = 0; i < stacks; i++)
            {
                ApplyInstantHeal(percentOfMaxHp);
            }

            healIdsToRemove.Add(kvp.Key);
        }

        foreach (string healId in healIdsToRemove)
        {
            _mutablePlayerBlob.TryRemoveItem(healId);
        }
    }

    private static void ApplyInstantHeal(float percentOfMaxHp)
    {
        PlayerGameplayManager? manager = PlayerGameplayManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[PlayerStatModifiers] Instant heal skipped — PlayerGameplayManager is not available.");
            return;
        }

        float amount = manager.MaxHp * (percentOfMaxHp / 100f);
        manager.Heal(amount);
        Debug.Log($"[PlayerStatModifiers] Instant heal restored {amount:0.#} HP ({percentOfMaxHp}% of max).");
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

    /// <summary>
    /// Wipes all augment-derived stats for a fresh run: clears the player's inventory and recomputes to
    /// baseline. Prevents a previous run's augments/stats from leaking into the next run (e.g. back-to-back
    /// players at a booth). Call from the run-start reset before health is initialised.
    /// </summary>
    public void ClearForNewRun()
    {
        _mutablePlayerBlob?.ClearInventory();
        if (_playerBlob != null)
        {
            ReapplyFromBlob();
        }
        else
        {
            Reset();
            OnStatsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Independent +X% bonuses stack additively onto a 1.0 base: 50% + 50% => 2.0x (+100% total).
    /// </summary>
    public static float AddPercentBonus(float multiplier, float percentBonus, int stacks = 1) =>
        multiplier + (percentBonus * stacks) / 100f;

    private void Reset()
    {
        MoveSpeedMultiplier = 1f;
        DamageMultiplier = 1f;
        MaxHpMultiplier = 1f;
        RangedDamageMultiplierBonus = 0f;
        ProjectileSpeedMultiplier = 1f;
        UltimateChargeMultiplier = 1f;
        LifestealPercent = 0f;
        RegenPercentPerSecond = 0f;
        MeleeRangeMultiplier = 1f;
        AttackSpeedMultiplier = 1f;
    }

    private void ApplyStatBoost(StatBoostKind kind, float value, int stacks)
    {
        float total = value * stacks;
        switch (kind)
        {
            case StatBoostKind.MoveSpeed:
                MoveSpeedMultiplier += (total / 100f);
                break;
            case StatBoostKind.DamageMultiplier:
                DamageMultiplier = AddPercentBonus(DamageMultiplier, value, stacks);
                break;
            case StatBoostKind.MaxHp:
                MaxHpMultiplier += (total / 100f);
                break;
            case StatBoostKind.RangedDamage:
                RangedDamageMultiplierBonus += (total / 100f);
                break;
            case StatBoostKind.ProjectileSpeed:
                ProjectileSpeedMultiplier += total / 100f;
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
            case StatBoostKind.MeleeRange:
                MeleeRangeMultiplier += total / 100f;
                break;
            case StatBoostKind.AttackSpeed:
                AttackSpeedMultiplier += total / 100f;
                break;
        }
    }
}
