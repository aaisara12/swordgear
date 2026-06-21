#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Shop;

/// <summary>
/// Central rhythm-style combo and per-level scoring system.
/// Listens to global enemy hit/kill events and exposes data/events
/// for UI, shop quality, and ultimate meter.
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem? Instance { get; private set; }

    [Header("Combo Timing")]
    [SerializeField] private float comboDuration = 3f;

    private float GetEffectiveComboDuration()
    {
        float bonus = PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.ComboDurationBonus : 0f;
        return comboDuration + bonus;
    }

    [Header("Scoring")]
    [SerializeField] private int hitBasePoints = 1;
    [SerializeField] private int killBonusPoints = 3;
    [SerializeField] private int rapidStreakBonusPoints = 5;

    [Header("Rapid Hit Streak")]
    [SerializeField] private float rapidWindowSeconds = 1f;
    [SerializeField] private int rapidHitsRequired = 3;

    [Header("Multiplier")]
    [SerializeField] private int maxMultiplier = 5;

    [Header("Augment Quality Thresholds (by level points)")]
    [SerializeField] private int mediumPointsThreshold = 20;
    [SerializeField] private int highPointsThreshold = 50;
    [SerializeField] private int elitePointsThreshold = 100;

    // Current combo state
    private int _currentComboCount;
    private int _maxComboThisLevel;
    private float _comboTimer;
    private int _currentMultiplier = 1;

    // Per-node scoring (reset each node, used for the Stage Complete summary)
    private int _totalPointsThisLevel;

    // Accumulates across combat nodes until an Augment node consumes it; drives augment quality tier.
    private int _pointsSinceLastAugment;

    // Rapid streak tracking
    private float _lastHitTime = -999f;
    private int _hitsInCurrentWindow;

    // Simple flag if we want to treat the current level as finished/frozen.
    private bool _levelFinished;

    public bool IsComboActive => _currentComboCount > 0 && _comboTimer > 0f;

    // Events for UI and other systems
    // (currentCombo, currentMultiplier)
    public event Action<int, int>? OnComboChanged;
    // (currentTimer, comboDuration)
    public event Action<float, float>? OnComboTimerChanged;
    public event Action<int>? OnMultiplierChanged;
    public event Action? OnComboBroken;
    public event Action<int>? OnLevelPointsChanged;

    // Fired whenever logical points are awarded so UltimateMeter (and others)
    // can react with per-element contributions.
    public event Action<int, Element>? OnPointsAwarded;

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ComboSystem detected, destroying this instance.");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EnemyController.OnAnyEnemyHit += HandleEnemyHit;
        EnemyController.OnAnyEnemyDeath += HandleEnemyDeath;
        ElementManager.OnActiveElementChanged += HandleElementChanged;
    }

    private void OnDisable()
    {
        EnemyController.OnAnyEnemyHit -= HandleEnemyHit;
        EnemyController.OnAnyEnemyDeath -= HandleEnemyDeath;
        ElementManager.OnActiveElementChanged -= HandleElementChanged;
    }

    private void Update()
    {
        if (!IsComboActive)
        {
            return;
        }

        _comboTimer -= Time.deltaTime;
        if (_comboTimer < 0f)
        {
            _comboTimer = 0f;
        }

        float effectiveDuration = GetEffectiveComboDuration();
        OnComboTimerChanged?.Invoke(_comboTimer, effectiveDuration);

        if (_comboTimer <= 0f)
        {
            BreakCombo();
        }
    }

    #endregion

    #region Event Handlers

    private void HandleEnemyHit(EnemyController enemy, float damage, Element element)
    {
        if (_levelFinished)
        {
            return;
        }

        float time = Time.time;

        if (_currentComboCount == 0)
        {
            _currentComboCount = 1;
            _currentMultiplier = 1;
        }
        else
        {
            _currentComboCount++;
        }

        _comboTimer = GetEffectiveComboDuration();

        // Rapid streak tracking
        if (time - _lastHitTime <= rapidWindowSeconds)
        {
            _hitsInCurrentWindow++;
        }
        else
        {
            _hitsInCurrentWindow = 1;
        }

        _lastHitTime = time;

        if (_hitsInCurrentWindow >= rapidHitsRequired)
        {
            ApplyMultiplierBonus();
            // Award rapid streak points with the NEW multiplier (after bonus applied)
            int rapidPoints = rapidStreakBonusPoints * _currentMultiplier;
            AwardPointsInternal(rapidPoints, element);
            _hitsInCurrentWindow = 0;
        }

        // Base hit points - always multiplied by current multiplier
        int basePoints = hitBasePoints * _currentMultiplier;
        AwardPointsInternal(basePoints, element);

        if (_currentComboCount > _maxComboThisLevel)
        {
            _maxComboThisLevel = _currentComboCount;
        }

        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
    }

    private void HandleEnemyDeath(EnemyController enemy)
    {
        if (_levelFinished || !IsComboActive)
        {
            return;
        }

        // Kills grant multiplier bonus first, then award points with the NEW multiplier
        ApplyMultiplierBonus();

        Element element = GameManager.Instance != null
            ? GameManager.Instance.currentElement
            : Element.Physical;

        // Use the multiplier AFTER applying the bonus
        int killPoints = killBonusPoints * _currentMultiplier;
        AwardPointsInternal(killPoints, element);

        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
    }

    private void HandleElementChanged(Element newElement)
    {
        if (_levelFinished) return;
        if (!IsComboActive) return;

        // Bouncing between embues resets the combo countdown but grants no points or multiplier
        float effectiveDuration = GetEffectiveComboDuration();
        _comboTimer = effectiveDuration;
        OnComboTimerChanged?.Invoke(_comboTimer, effectiveDuration);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Resets combo state for a new node/level. Per-node points reset here; the cross-node
    /// points-since-last-augment accumulator is preserved (see <see cref="ResetPointsSinceLastAugment"/>).
    /// </summary>
    public void ResetForNewLevel()
    {
        _levelFinished = false;
        _currentComboCount = 0;
        _maxComboThisLevel = 0;
        _comboTimer = 0f;
        _currentMultiplier = 1;
        _totalPointsThisLevel = 0;
        _hitsInCurrentWindow = 0;
        _lastHitTime = -999f;

        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
        // Still notify UI of current total points (they persist across levels)
        OnLevelPointsChanged?.Invoke(_totalPointsThisLevel);
    }

    /// <summary>
    /// Resets everything for the start of a brand-new run, including the points-since-last-augment accumulator.
    /// </summary>
    public void ResetForNewRun()
    {
        _levelFinished = false;
        _currentComboCount = 0;
        _maxComboThisLevel = 0;
        _comboTimer = 0f;
        _currentMultiplier = 1;
        _totalPointsThisLevel = 0;
        _pointsSinceLastAugment = 0;
        _hitsInCurrentWindow = 0;
        _lastHitTime = -999f;

        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
        OnLevelPointsChanged?.Invoke(_totalPointsThisLevel);
    }

    /// <summary>
    /// Clears the cross-node points accumulator. Called when an Augment node consumes performance
    /// to determine augment quality, so the next augment reflects only subsequent combat.
    /// </summary>
    public void ResetPointsSinceLastAugment()
    {
        _pointsSinceLastAugment = 0;
    }

    /// <summary>
    /// Marks the current level as finished so further hits don't change its stats.
    /// Data remains available for things like the augment shop.
    /// </summary>
    public void OnLevelFinished()
    {
        _levelFinished = true;
        BreakCombo();
    }

    public ComboPerformance GetCurrentLevelPerformance()
    {
        return new ComboPerformance(
            _totalPointsThisLevel,
            _maxComboThisLevel
        );
    }

    public AugmentQualityTier GetAugmentQualityTier()
    {
        // Tiered thresholds based on points earned since the last Augment node.
        if (_pointsSinceLastAugment >= elitePointsThreshold)
        {
            return AugmentQualityTier.Elite;
        }

        if (_pointsSinceLastAugment >= highPointsThreshold)
        {
            return AugmentQualityTier.High;
        }

        if (_pointsSinceLastAugment >= mediumPointsThreshold)
        {
            return AugmentQualityTier.Medium;
        }

        return AugmentQualityTier.Low;
    }

    #endregion

    #region Internal helpers

    private void BreakCombo()
    {
        if (_currentComboCount == 0)
        {
            return;
        }

        _currentComboCount = 0;
        _currentMultiplier = 1;
        _comboTimer = 0f;
        _hitsInCurrentWindow = 0;

        OnComboBroken?.Invoke();
        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
        OnMultiplierChanged?.Invoke(_currentMultiplier);
    }

    private void ApplyMultiplierBonus()
    {
        int previousMultiplier = _currentMultiplier;
        _currentMultiplier = Mathf.Clamp(_currentMultiplier + 1, 1, maxMultiplier);

        if (_currentMultiplier != previousMultiplier)
        {
            OnMultiplierChanged?.Invoke(_currentMultiplier);
        }
    }

    private void AwardPointsInternal(int points, Element element)
    {
        if (points <= 0)
        {
            return;
        }

        _totalPointsThisLevel += points;
        _pointsSinceLastAugment += points;
        OnLevelPointsChanged?.Invoke(_totalPointsThisLevel);
        OnPointsAwarded?.Invoke(points, element);
    }

    #endregion
}

/// <summary>
/// Snapshot of combo-related performance for a level, used by shop quality logic.
/// </summary>
public readonly struct ComboPerformance
{
    public int TotalPointsThisLevel { get; }
    public int MaxComboThisLevel { get; }

    public ComboPerformance(int totalPointsThisLevel, int maxComboThisLevel)
    {
        TotalPointsThisLevel = totalPointsThisLevel;
        MaxComboThisLevel = maxComboThisLevel;
    }
}

