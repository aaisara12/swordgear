#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Shop;

/// <summary> A stat a streak can boost. </summary>
public enum StreakStat
{
    Damage,
    AttackSpeed,
    MoveSpeed,
}

/// <summary> One stat a streak raises, per stack. </summary>
[Serializable]
public struct StreakBonus
{
    public StreakStat stat;
    [Tooltip("Percent added per stack: 20 means +20% per stack, stacking additively like augments.")]
    public float percentPerStack;
}

/// <summary> Why a streak changed, so feedback can tell a lucky stack from a bust. </summary>
public enum StreakChange
{
    Gained,
    Expired,
    Busted,
    Cleared,
}

/// <summary> A live streak: a stackable, temporary stat boost that busts when the player is hit. </summary>
public sealed class StreakState
{
    public string Id { get; }
    public IReadOnlyList<StreakBonus> Bonuses { get; }
    public int MaxStacks { get; }
    public float CapSeconds { get; }
    public int Stacks { get; internal set; }
    public float ExpiresAt { get; internal set; }

    public float RemainingSeconds => Mathf.Max(0f, ExpiresAt - Time.time);

    public StreakState(string id, IReadOnlyList<StreakBonus> bonuses, int maxStacks, float capSeconds)
    {
        Id = id;
        Bonuses = new List<StreakBonus>(bonuses);
        MaxStacks = Mathf.Max(1, maxStacks);
        CapSeconds = capSeconds;
    }

    /// <summary> Total percent this streak currently adds to <paramref name="stat"/>. </summary>
    public float PercentFor(StreakStat stat)
    {
        float percent = 0f;
        foreach (StreakBonus bonus in Bonuses)
        {
            if (bonus.stat == stat)
            {
                percent += bonus.percentPerStack * Stacks;
            }
        }

        return percent;
    }
}

/// <summary>
/// Holds player stat modifiers from stat-boost augments. Populated from PlayerBlob at game start
/// and re-applied whenever the blob's inventory changes (e.g. after purchasing an augment).
/// </summary>
/// <remarks>
/// Also holds <b>streaks</b>: temporary, stackable boosts (Light's Crescendo and Allegro) layered on top
/// of the augment values. They live in their own list rather than in the augment fields because
/// <see cref="ReapplyFromBlob"/> rebuilds those from scratch on every pickup, which would silently wipe a
/// streak. Streaks are player-level rather than weapon-level because Light is a timed imbue: a buff owned
/// by the weapon would die the moment the imbue ended.
/// </remarks>
public class PlayerStatModifiers : InitializeableGameComponent
{
    public static PlayerStatModifiers? Instance { get; private set; }

    /// <summary> Fired after modifiers are re-applied (e.g. after a purchase). Use to refresh MaxHp, start Regen, etc. </summary>
    public static event Action? OnStatsChanged;

    /// <summary>
    /// Fired when a streak gains a stack, expires, busts or is cleared. Kept separate from
    /// <see cref="OnStatsChanged"/>, whose listeners restart regen and re-read max HP, neither of which a
    /// streak touches.
    /// </summary>
    public static event Action<StreakChange, string>? OnStreakChanged;

    // All multiplier stats add percent to base 1.0; independent +X% bonuses stack additively.
    // Move speed, damage and attack speed are the augment value plus any live streak.
    public float MoveSpeedMultiplier => _moveSpeedMultiplier + StreakPercent(StreakStat.MoveSpeed) / 100f;
    public float DamageMultiplier => _damageMultiplier + StreakPercent(StreakStat.Damage) / 100f;
    public float MaxHpMultiplier { get; private set; } = 1f;
    public float RangedDamageMultiplierBonus { get; private set; }
    public float ProjectileSpeedMultiplier { get; private set; } = 1f;
    public float UltimateChargeMultiplier { get; private set; } = 1f;
    public float LifestealPercent { get; private set; }
    public float RegenPercentPerSecond { get; private set; }
    public float MeleeRangeMultiplier { get; private set; } = 1f;
    public float AttackSpeedMultiplier => _attackSpeedMultiplier + StreakPercent(StreakStat.AttackSpeed) / 100f;
    public float DashCooldownMultiplier { get; private set; } = 1f;

    /// <summary> Live streaks, oldest first. </summary>
    public IReadOnlyList<StreakState> Streaks => _streaks;

    // Augment-derived values for the stats a streak can also raise.
    private float _moveSpeedMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;

    private readonly List<StreakState> _streaks = new();

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

    private void OnEnable()
    {
        PlayerGameplayManager.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        PlayerGameplayManager.OnHealthChanged -= HandleHealthChanged;
    }

    private void OnDestroy()
    {
        if (_playerBlob?.InventoryItems is IReadOnlyObservableDictionary<string, int> observable)
            observable.DictionaryChanged -= HandleInventoryChanged;
        if (Instance == this)
            Instance = null;
        OnStatsChanged = null;
        OnStreakChanged = null;
    }

    private void Update()
    {
        // Safety cap: a streak nobody busts still ends, so a lucky run of stacks can't last the whole node.
        for (int i = _streaks.Count - 1; i >= 0; i--)
        {
            if (Time.time < _streaks[i].ExpiresAt)
            {
                continue;
            }

            string id = _streaks[i].Id;
            _streaks.RemoveAt(i);
            OnStreakChanged?.Invoke(StreakChange.Expired, id);
        }
    }

    // ---------- Streaks ----------

    /// <summary>
    /// Adds a stack to the streak <paramref name="id"/>, starting it if needed, and refreshes its timer.
    /// At max stacks the timer still refreshes, so a lucky roll is never wasted.
    /// </summary>
    public void AddStreakStack(string id, IReadOnlyList<StreakBonus> bonuses, int maxStacks, float capSeconds)
    {
        StreakState? streak = GetStreak(id);
        if (streak == null)
        {
            streak = new StreakState(id, bonuses, maxStacks, capSeconds);
            _streaks.Add(streak);
        }

        streak.Stacks = Mathf.Min(streak.Stacks + 1, streak.MaxStacks);
        streak.ExpiresAt = Time.time + capSeconds;
        OnStreakChanged?.Invoke(StreakChange.Gained, id);
    }

    public StreakState? GetStreak(string id)
    {
        foreach (StreakState streak in _streaks)
        {
            if (streak.Id == id)
            {
                return streak;
            }
        }

        return null;
    }

    /// <summary> Ends every streak at once. <paramref name="reason"/> tells feedback whether it was a bust. </summary>
    public void ClearStreaks(StreakChange reason = StreakChange.Cleared)
    {
        if (_streaks.Count == 0)
        {
            return;
        }

        var ended = new List<string>(_streaks.Count);
        foreach (StreakState streak in _streaks)
        {
            ended.Add(streak.Id);
        }

        _streaks.Clear();
        foreach (string id in ended)
        {
            OnStreakChanged?.Invoke(reason, id);
        }
    }

    /// <summary> Getting hit busts every streak. That's the gamble: ride a hot streak, but don't get touched. </summary>
    private void HandleHealthChanged(PlayerHealthSnapshot snapshot)
    {
        if (snapshot.Delta < 0f)
        {
            ClearStreaks(StreakChange.Busted);
        }
    }

    private float StreakPercent(StreakStat stat)
    {
        float percent = 0f;
        foreach (StreakState streak in _streaks)
        {
            percent += streak.PercentFor(stat);
        }

        return percent;
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
        ClearStreaks();
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

    // Resets augment-derived values only. Streaks are deliberately untouched: this runs on every augment
    // pickup, and a pickup must not end a streak.
    private void Reset()
    {
        _moveSpeedMultiplier = 1f;
        _damageMultiplier = 1f;
        MaxHpMultiplier = 1f;
        RangedDamageMultiplierBonus = 0f;
        ProjectileSpeedMultiplier = 1f;
        UltimateChargeMultiplier = 1f;
        LifestealPercent = 0f;
        RegenPercentPerSecond = 0f;
        MeleeRangeMultiplier = 1f;
        _attackSpeedMultiplier = 1f;
        DashCooldownMultiplier = 1f;
    }

    private void ApplyStatBoost(StatBoostKind kind, float value, int stacks)
    {
        float total = value * stacks;
        switch (kind)
        {
            case StatBoostKind.MoveSpeed:
                _moveSpeedMultiplier += (total / 100f);
                break;
            case StatBoostKind.DamageMultiplier:
                _damageMultiplier = AddPercentBonus(_damageMultiplier, value, stacks);
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
                _attackSpeedMultiplier += total / 100f;
                break;
            case StatBoostKind.DashCooldown:
                DashCooldownMultiplier -= total / 100f;
                break;
        }
    }
}
