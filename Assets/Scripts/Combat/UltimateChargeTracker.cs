#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's ultimate. The active ability unlocks only while the player's augment collection covers its
/// element requirements (see <see cref="AugmentElementLedger"/>); each extra full set on top of that is one level
/// of overcharge, handed to the effect so it can scale itself. Overcharge is capped by the ability's
/// <see cref="UltimateAbilitySO.MaxOverchargeLevel"/>. While unlocked, the meter fills from damage the player
/// deals — any element counts. While locked it holds at empty and cannot be activated.
/// </summary>
public class UltimateChargeTracker : MonoBehaviour
{
    public static UltimateChargeTracker? Instance { get; private set; }

    [SerializeField] private UltimateAbilitySO? _activeUltimate;

    private float _charge;
    private int _completedSets;

    // Last value handed to OnOverchargeChanged. Starts invalid so the first refresh always broadcasts, and
    // catches an ability swap that keeps the set count but changes the cap.
    private int _broadcastOvercharge = -1;

    private bool _isUltimateAvailable;
    private bool _isExecuting;
    private bool _subscribed;

    public bool IsUltimateAvailable => _isUltimateAvailable;
    public UltimateAbilitySO? ActiveUltimate => _activeUltimate;

    /// <summary> True once the player's augments cover the ability's element requirements at least once. </summary>
    public bool IsUnlocked => _completedSets > 0;

    /// <summary> How many full sets of the requirements the player's augments cover. 0 while locked. </summary>
    public int CompletedSets => _completedSets;

    /// <summary> 0 for the base ultimate, +1 per extra full set of requirements, capped by the ability. </summary>
    public int OverchargeLevel => _activeUltimate != null
        ? Mathf.Clamp(_completedSets - 1, 0, _activeUltimate.MaxOverchargeLevel)
        : 0;

    /// <summary> True once extra sets stop counting, so the readout can stop advertising the next level. </summary>
    public bool IsOverchargeCapped => _activeUltimate != null && _completedSets - 1 >= _activeUltimate.MaxOverchargeLevel;

    public float ChargeProgress =>
        _activeUltimate != null ? Mathf.Clamp01(_charge / _activeUltimate.ChargeRequired) : 0f;

    // (normalized meter progress 0-1)
    public event Action<float>? OnProgressChanged;
    public event Action? OnUltimateAvailable;
    public event Action? OnUltimateUnavailable;

    /// <summary>
    /// Fired when the overcharge level changes (0 = base ultimate), including when the ult locks or unlocks and
    /// the level happens to stay 0 — subscribers get the current level either way.
    /// </summary>
    public event Action<int>? OnOverchargeChanged;

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        TrySubscribe();
        RefreshSets();
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshSets();
    }

    private void OnDisable()
    {
        if (!_subscribed) return;
        EnemyController.OnAnyEnemyHit -= HandleEnemyHit;
        AugmentElementLedger.OnCountsChanged -= RefreshSets;
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        EnemyController.OnAnyEnemyHit += HandleEnemyHit;
        AugmentElementLedger.OnCountsChanged += RefreshSets;
        _subscribed = true;
    }

    #endregion

    #region Event Handlers

    // Damage-over-time ticks pass feedsCombo:false and never reach this event, so a burning enemy can't charge
    // the ult while the player does nothing.
    private void HandleEnemyHit(EnemyController enemy, float damage, MoveType moveType)
    {
        if (_isExecuting || _isUltimateAvailable || !IsUnlocked || _activeUltimate == null)
            return;

        if (damage <= 0f)
            return;

        // UltimateChargeMultiplier is 1.0 base; +10% spark => 1.1x charge per point of damage.
        float chargeMultiplier = PlayerStatModifiers.Instance != null
            ? Mathf.Max(0.01f, PlayerStatModifiers.Instance.UltimateChargeMultiplier)
            : 1f;

        _charge += damage * chargeMultiplier;

        float required = _activeUltimate.ChargeRequired;
        if (_charge >= required)
        {
            _charge = required;
            _isUltimateAvailable = true;
            OnProgressChanged?.Invoke(ChargeProgress);
            OnUltimateAvailable?.Invoke();
            return;
        }

        OnProgressChanged?.Invoke(ChargeProgress);
    }

    /// <summary>
    /// Recomputes how many requirement sets the augments cover. Dropping back to 0 (e.g. a run reset wiping the
    /// inventory) revokes an unspent ult, since the ability is no longer unlocked at all.
    /// </summary>
    private void RefreshSets()
    {
        _completedSets = _activeUltimate != null ? _activeUltimate.GetCompletedSets(AugmentElementLedger.Counts) : 0;

        if (!IsUnlocked && _charge > 0f)
        {
            _charge = 0f;
        }

        if (!IsUnlocked && _isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }

        int overcharge = OverchargeLevel;
        if (overcharge != _broadcastOvercharge)
        {
            _broadcastOvercharge = overcharge;
            OnOverchargeChanged?.Invoke(overcharge);
        }

        // Requirement fills move even when the set count doesn't, so the readout always refreshes.
        OnProgressChanged?.Invoke(ChargeProgress);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called by player input. Executes the ult at its current overcharge level and empties the meter.
    /// Returns false if the ult is locked, still charging, or has nothing to run.
    /// </summary>
    public bool TryActivate()
    {
        if (!_isUltimateAvailable || !IsUnlocked || _activeUltimate?.Effect == null)
            return false;

        Transform? player = GameManager.Instance?.player?.transform;
        if (player == null)
            return false;

        _isExecuting = true;
        _isUltimateAvailable = false;
        _charge = 0f;

        OnUltimateUnavailable?.Invoke();
        OnProgressChanged?.Invoke(0f);

        _activeUltimate.Effect.ExecuteUlt(OverchargeLevel, player);
        return true;
    }

    public void EndExecution() => _isExecuting = false;

    public void SetActiveUltimate(UltimateAbilitySO? ultimate)
    {
        _activeUltimate = ultimate;
        _charge = 0f;

        if (_isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }

        RefreshSets();
    }

    public void ResetForNewRun()
    {
        _charge = 0f;

        if (_isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }

        RefreshSets();
    }

    /// <summary>
    /// Fills <paramref name="results"/> with one entry per element requirement of the active ultimate, each
    /// reporting how far that element's augments have come toward completing the *next* set: the unlock set while
    /// the ult is locked, the next overcharge level once it isn't. Returns nothing once overcharge is capped, or
    /// for an ultimate that declares no requirements (e.g. the tutorial's) — in both cases there is no next set
    /// to work toward and the readout falls back to the shared charge meter alone.
    /// </summary>
    public void GetRequirementSegments(List<(Element element, float fill)> results)
    {
        results.Clear();
        if (_activeUltimate == null || IsOverchargeCapped) return;

        foreach (UltimateAbilitySO.ElementRequirement requirement in _activeUltimate.Requirements)
        {
            float fill = _activeUltimate.GetRequirementFill(requirement, _completedSets, AugmentElementLedger.Counts);
            results.Add((requirement.element, fill));
        }
    }

    #endregion
}
