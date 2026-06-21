#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Shop;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem? Instance { get; private set; }

    [Header("Combo Timing")]
    [SerializeField] private float comboDuration = 3f;
    [SerializeField] private float comboTimerExtension = 3f;
    [SerializeField] private float stalenessFalloff = 1f;

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

    // Staleness tracking — resets when combo breaks
    private MoveType? _lastMoveType;
    private readonly Dictionary<MoveType, int> _stalenessCounts = new();
    private readonly List<MoveType> _stalenessKeys = new(); // scratch list to avoid alloc in hot path

    // Per-level scoring
    private int _totalPointsThisLevel;

    // Rapid streak tracking
    private float _lastHitTime = -999f;
    private int _hitsInCurrentWindow;

    private bool _levelFinished;

    public bool IsComboActive => _currentComboCount > 0 && _comboTimer > 0f;

    // Events
    public event Action<int, int>? OnComboChanged;
    public event Action<float, float>? OnComboTimerChanged;
    public event Action<int>? OnMultiplierChanged;
    public event Action? OnComboBroken;
    public event Action<int>? OnLevelPointsChanged;
    // Fired on every hit while a combo is active. UltimateChargeTracker listens to this.
    public event Action<MoveType>? OnComboHit;
    // Kept for scoring consumers (shop quality etc.)
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
    }

    private void OnDisable()
    {
        EnemyController.OnAnyEnemyHit -= HandleEnemyHit;
        EnemyController.OnAnyEnemyDeath -= HandleEnemyDeath;
    }

    private void Update()
    {
        if (!IsComboActive)
            return;

        _comboTimer -= Time.deltaTime;
        if (_comboTimer < 0f)
            _comboTimer = 0f;

        float effectiveDuration = GetEffectiveComboDuration();
        OnComboTimerChanged?.Invoke(_comboTimer, effectiveDuration);

        if (_comboTimer <= 0f)
            BreakCombo();
    }

    #endregion

    #region Event Handlers

    private void HandleEnemyHit(EnemyController enemy, float damage, MoveType moveType)
    {
        if (_levelFinished)
            return;

        float time = Time.time;
        bool isFirstHit = _currentComboCount == 0;

        if (isFirstHit)
        {
            _currentComboCount = 1;
            _currentMultiplier = 1;
            _comboTimer = GetEffectiveComboDuration();
        }
        else
        {
            _currentComboCount++;

            // Extend timer only when move differs from the immediately previous one
            if (_lastMoveType.HasValue && moveType != _lastMoveType.Value)
            {
                int staleness = _stalenessCounts.TryGetValue(moveType, out int s) ? s : 0;
                float extensionMult = 1f / (1f + staleness * stalenessFalloff);
                float extension = comboTimerExtension * extensionMult;
                _comboTimer = Mathf.Min(_comboTimer + extension, GetEffectiveComboDuration());
            }
        }

        _lastMoveType = moveType;

        // Staleness: used move gets more stale, all others decay by 1
        _stalenessCounts[moveType] = (_stalenessCounts.TryGetValue(moveType, out int cur) ? cur : 0) + 1;

        _stalenessKeys.Clear();
        _stalenessKeys.AddRange(_stalenessCounts.Keys);
        foreach (var key in _stalenessKeys)
        {
            if (key != moveType && _stalenessCounts[key] > 0)
                _stalenessCounts[key]--;
        }

        // Rapid streak (multiplier / scoring only)
        if (time - _lastHitTime <= rapidWindowSeconds)
            _hitsInCurrentWindow++;
        else
            _hitsInCurrentWindow = 1;

        _lastHitTime = time;

        if (_hitsInCurrentWindow >= rapidHitsRequired)
        {
            ApplyMultiplierBonus();
            AwardPointsInternal(rapidStreakBonusPoints * _currentMultiplier, moveType.Element);
            _hitsInCurrentWindow = 0;
        }

        AwardPointsInternal(hitBasePoints * _currentMultiplier, moveType.Element);

        if (_currentComboCount > _maxComboThisLevel)
            _maxComboThisLevel = _currentComboCount;

        OnComboHit?.Invoke(moveType);
        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
    }

    private void HandleEnemyDeath(EnemyController enemy)
    {
        if (_levelFinished || !IsComboActive)
            return;

        ApplyMultiplierBonus();

        Element element = GameManager.Instance != null
            ? GameManager.Instance.currentElement
            : Element.Physical;

        AwardPointsInternal(killBonusPoints * _currentMultiplier, element);
        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Adds time to the combo timer, capped at comboDuration. No-op if no combo is active.
    /// Called by UltimateChargeTracker when the ult becomes available.
    /// </summary>
    public void ExtendTimer(float amount)
    {
        if (!IsComboActive)
            return;

        _comboTimer = Mathf.Min(_comboTimer + amount, GetEffectiveComboDuration());
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
    }

    public void ResetForNewLevel()
    {
        _levelFinished = false;
        _currentComboCount = 0;
        _maxComboThisLevel = 0;
        _comboTimer = 0f;
        _currentMultiplier = 1;
        _hitsInCurrentWindow = 0;
        _lastHitTime = -999f;
        _lastMoveType = null;
        _stalenessCounts.Clear();

        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
        OnLevelPointsChanged?.Invoke(_totalPointsThisLevel);
    }

    public void ResetForNewRound()
    {
        _levelFinished = false;
        _currentComboCount = 0;
        _maxComboThisLevel = 0;
        _comboTimer = 0f;
        _currentMultiplier = 1;
        _totalPointsThisLevel = 0;
        _hitsInCurrentWindow = 0;
        _lastHitTime = -999f;
        _lastMoveType = null;
        _stalenessCounts.Clear();

        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
        OnLevelPointsChanged?.Invoke(_totalPointsThisLevel);
    }

    public void OnLevelFinished()
    {
        _levelFinished = true;
        BreakCombo();
    }

    public ComboPerformance GetCurrentLevelPerformance()
    {
        return new ComboPerformance(_totalPointsThisLevel, _maxComboThisLevel);
    }

    public AugmentQualityTier GetAugmentQualityTier()
    {
        if (_totalPointsThisLevel >= elitePointsThreshold) return AugmentQualityTier.Elite;
        if (_totalPointsThisLevel >= highPointsThreshold)  return AugmentQualityTier.High;
        if (_totalPointsThisLevel >= mediumPointsThreshold) return AugmentQualityTier.Medium;
        return AugmentQualityTier.Low;
    }

    #endregion

    #region Internal helpers

    private void BreakCombo()
    {
        if (_currentComboCount == 0)
            return;

        _currentComboCount = 0;
        _currentMultiplier = 1;
        _comboTimer = 0f;
        _hitsInCurrentWindow = 0;
        _lastMoveType = null;
        _stalenessCounts.Clear();

        OnComboBroken?.Invoke();
        OnComboChanged?.Invoke(_currentComboCount, _currentMultiplier);
        OnComboTimerChanged?.Invoke(_comboTimer, GetEffectiveComboDuration());
        OnMultiplierChanged?.Invoke(_currentMultiplier);
    }

    private void ApplyMultiplierBonus()
    {
        int prev = _currentMultiplier;
        _currentMultiplier = Mathf.Clamp(_currentMultiplier + 1, 1, maxMultiplier);

        if (_currentMultiplier != prev)
            OnMultiplierChanged?.Invoke(_currentMultiplier);
    }

    private void AwardPointsInternal(int points, Element element)
    {
        if (points <= 0)
            return;

        _totalPointsThisLevel += points;
        OnLevelPointsChanged?.Invoke(_totalPointsThisLevel);
        OnPointsAwarded?.Invoke(points, element);
    }

    #endregion
}

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
