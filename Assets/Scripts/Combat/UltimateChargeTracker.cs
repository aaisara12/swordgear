#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks per-element hit charges accumulated during an active combo and makes the
/// current ultimate ability available when its element requirements are met.
/// Unspent charges are discarded when the combo ends; a granted ult persists until used.
/// </summary>
public class UltimateChargeTracker : MonoBehaviour
{
    public static UltimateChargeTracker? Instance { get; private set; }

    [SerializeField] private UltimateAbilitySO? _activeUltimate;
    [SerializeField] private float _ultAvailableTimerBonus = 2f;

    // Fractional charges so UltimateChargeMultiplier can speed fill rate (e.g. 1.1 per hit).
    private readonly Dictionary<Element, float> _comboCharges = new();
    private bool _isUltimateAvailable;
    private bool _isExecuting;
    private bool _subscribed;

    public bool IsUltimateAvailable => _isUltimateAvailable;
    public UltimateAbilitySO? ActiveUltimate => _activeUltimate;

    // (normalizedProgress 0-1 toward satisfying all requirements)
    public event Action<float>? OnProgressChanged;
    public event Action? OnUltimateAvailable;
    public event Action? OnUltimateUnavailable;

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
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (!_subscribed || ComboSystem.Instance == null) return;
        ComboSystem.Instance.OnComboHit -= HandleComboHit;
        ComboSystem.Instance.OnComboBroken -= HandleComboBroken;
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed || ComboSystem.Instance == null) return;
        ComboSystem.Instance.OnComboHit += HandleComboHit;
        ComboSystem.Instance.OnComboBroken += HandleComboBroken;
        _subscribed = true;
    }

    #endregion

    #region Event Handlers

    private void HandleComboHit(MoveType moveType)
    {
        if (_isExecuting) return;

        Element element = moveType.Element;
        // UltimateChargeMultiplier is 1.0 base; +10% spark => 1.1 charges per hit.
        float chargeGain = PlayerStatModifiers.Instance != null
            ? Mathf.Max(0.01f, PlayerStatModifiers.Instance.UltimateChargeMultiplier)
            : 1f;

        float previous = _comboCharges.TryGetValue(element, out float existing) ? existing : 0f;
        _comboCharges[element] = previous + chargeGain;

        float progress = ComputeProgress();
        OnProgressChanged?.Invoke(progress);

        if (!_isUltimateAvailable && _activeUltimate != null && IsRequirementsSatisfied())
        {
            _isUltimateAvailable = true;
            OnUltimateAvailable?.Invoke();
            ComboSystem.Instance?.ExtendTimer(_ultAvailableTimerBonus);
        }
    }

    private void HandleComboBroken()
    {
        // Once earned, the ult is the player's until they spend it — keep the charges too so the
        // meter keeps reading full rather than emptying under a still-usable ability.
        if (_isUltimateAvailable)
        {
            return;
        }

        _comboCharges.Clear();
        OnProgressChanged?.Invoke(0f);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called by player input. Executes the ult and clears the charge state.
    /// Returns false if the ult is not currently available.
    /// </summary>
    public bool TryActivate()
    {
        Debug.Log("Trying ultimate");
        if (!_isUltimateAvailable || _activeUltimate == null)
            return false;

        _isExecuting = true;

        Transform? player = GameManager.Instance?.player?.transform;
        if (player != null)
            _activeUltimate.Effect?.Execute(player);

        _isUltimateAvailable = false;
        _comboCharges.Clear();
        OnUltimateUnavailable?.Invoke();
        OnProgressChanged?.Invoke(0f);
        return true;
    }

    public void EndExecution() => _isExecuting = false;

    public void SetActiveUltimate(UltimateAbilitySO? ultimate)
    {
        _activeUltimate = ultimate;
        _comboCharges.Clear();
        if (_isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }
        OnProgressChanged?.Invoke(0f);
    }

    public void ResetForNewRun()
    {
        _comboCharges.Clear();
        if (_isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }
        OnProgressChanged?.Invoke(0f);
    }

    /// <summary>
    /// Fills <paramref name="results"/> with one entry per element requirement,
    /// each carrying the element and its fill progress (0–1).
    /// </summary>
    public void GetChargeFills(List<(Element element, float fill)> results)
    {
        results.Clear();
        if (_activeUltimate == null) return;

        foreach (var req in _activeUltimate.Requirements)
        {
            float held = _comboCharges.TryGetValue(req.element, out float c) ? c : 0f;
            float fill = req.count > 0 ? Mathf.Clamp01(held / req.count) : 0f;
            results.Add((req.element, fill));
        }
    }

    /// <summary>
    /// Returns a 0-1 value representing how close the player is to satisfying all
    /// element charge requirements for the active ultimate.
    /// </summary>
    public float ComputeProgress()
    {
        if (_activeUltimate == null || _activeUltimate.Requirements.Count == 0)
            return 0f;

        float total = 0f;
        float met = 0f;
        foreach (var req in _activeUltimate.Requirements)
        {
            total += req.count;
            float held = _comboCharges.TryGetValue(req.element, out float c) ? c : 0f;
            met += Mathf.Min(held, req.count);
        }

        return total > 0f ? met / total : 0f;
    }

    #endregion

    private bool IsRequirementsSatisfied()
    {
        if (_activeUltimate == null)
        {
            return false;
        }

        // Floor fractional charges for the int-based requirement API.
        var intCharges = new Dictionary<Element, int>(_comboCharges.Count);
        foreach (KeyValuePair<Element, float> kvp in _comboCharges)
        {
            intCharges[kvp.Key] = Mathf.FloorToInt(kvp.Value);
        }

        return _activeUltimate.IsSatisfiedBy(intCharges);
    }
}
