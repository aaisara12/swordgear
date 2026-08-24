#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's ultimate. The active ability unlocks only while the player's augment collection covers its
/// element requirements (see <see cref="AugmentElementLedger"/>); stacking extra full sets overcharges it to a
/// higher level, which is handed to the effect. While unlocked, the meter fills from damage the player deals —
/// any element counts. While locked it holds at empty and cannot be activated.
/// </summary>
public class UltimateChargeTracker : MonoBehaviour
{
    public static UltimateChargeTracker? Instance { get; private set; }

    [SerializeField] private UltimateAbilitySO? _activeUltimate;

    private float _charge;
    private int _level;
    private bool _isUltimateAvailable;
    private bool _isExecuting;
    private bool _subscribed;

    public bool IsUltimateAvailable => _isUltimateAvailable;
    public UltimateAbilitySO? ActiveUltimate => _activeUltimate;

    /// <summary> 0 while the augment requirements are unmet. 1 is the base ultimate; each extra set adds a level. </summary>
    public int CurrentLevel => _level;

    public float ChargeProgress =>
        _activeUltimate != null ? Mathf.Clamp01(_charge / _activeUltimate.ChargeRequired) : 0f;

    // (normalized meter progress 0-1)
    public event Action<float>? OnProgressChanged;
    public event Action? OnUltimateAvailable;
    public event Action? OnUltimateUnavailable;

    /// <summary> Fired when the overcharge level changes, including to and from 0 (locked). </summary>
    public event Action<int>? OnLevelChanged;

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
        RefreshLevel();
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshLevel();
    }

    private void OnDisable()
    {
        if (!_subscribed) return;
        EnemyController.OnAnyEnemyHit -= HandleEnemyHit;
        AugmentElementLedger.OnCountsChanged -= RefreshLevel;
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        EnemyController.OnAnyEnemyHit += HandleEnemyHit;
        AugmentElementLedger.OnCountsChanged += RefreshLevel;
        _subscribed = true;
    }

    #endregion

    #region Event Handlers

    // Damage-over-time ticks pass feedsCombo:false and never reach this event, so a burning enemy can't charge
    // the ult while the player does nothing.
    private void HandleEnemyHit(EnemyController enemy, float damage, MoveType moveType)
    {
        if (_isExecuting || _isUltimateAvailable || _level <= 0 || _activeUltimate == null)
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
    /// Recomputes the overcharge level from the player's augments. A level drop to 0 (e.g. a run reset wiping the
    /// inventory) revokes an unspent ult, since the ability is no longer unlocked at all.
    /// </summary>
    private void RefreshLevel()
    {
        int previousLevel = _level;
        _level = _activeUltimate != null ? _activeUltimate.GetLevel(AugmentElementLedger.Counts) : 0;

        if (_level <= 0 && _charge > 0f)
        {
            _charge = 0f;
        }

        if (_level <= 0 && _isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }

        if (_level != previousLevel)
        {
            OnLevelChanged?.Invoke(_level);
        }

        // Requirement fills move even when the level doesn't, so the readout always refreshes.
        OnProgressChanged?.Invoke(ChargeProgress);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called by player input. Executes the ult at its current level and empties the meter.
    /// Returns false if the ult is locked, still charging, or has nothing to run.
    /// </summary>
    public bool TryActivate()
    {
        if (!_isUltimateAvailable || _level <= 0 || _activeUltimate?.Effect == null)
            return false;

        Transform? player = GameManager.Instance?.player?.transform;
        if (player == null)
            return false;

        _isExecuting = true;
        _isUltimateAvailable = false;
        _charge = 0f;

        OnUltimateUnavailable?.Invoke();
        OnProgressChanged?.Invoke(0f);

        _activeUltimate.Effect.ExecuteUlt(_level, player);
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

        RefreshLevel();
    }

    public void ResetForNewRun()
    {
        _charge = 0f;

        if (_isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }

        RefreshLevel();
    }

    /// <summary>
    /// Fills <paramref name="results"/> with one entry per element requirement of the active ultimate.
    /// While the ult is locked each entry reports how far that element's augments have come toward unlocking the
    /// next level, so the readout shows what the player still needs. Once unlocked every entry mirrors the shared
    /// damage charge, so the ring reads as a single meter.
    /// </summary>
    public void GetMeterSegments(List<(Element element, float fill)> results)
    {
        results.Clear();
        if (_activeUltimate == null) return;

        float charge = ChargeProgress;

        // An ultimate with no element requirements (e.g. the tutorial's) is always unlocked and has no slots to
        // show, so it gets a single ring carrying the charge instead of rendering nothing at all.
        if (_activeUltimate.Requirements.Count == 0)
        {
            results.Add((ElementVisuals.GetCurrentElement(), charge));
            return;
        }

        foreach (UltimateAbilitySO.ElementRequirement requirement in _activeUltimate.Requirements)
        {
            float fill = _level > 0
                ? charge
                : _activeUltimate.GetRequirementFill(requirement, _level, AugmentElementLedger.Counts);

            results.Add((requirement.element, fill));
        }
    }

    #endregion
}
