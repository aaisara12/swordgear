#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks per-element hit charges accumulated during an active combo and makes the
/// current ultimate ability available when its element requirements are met.
/// Charges are discarded when the combo ends without the ult being activated.
/// </summary>
public class UltimateChargeTracker : MonoBehaviour
{
    public static UltimateChargeTracker? Instance { get; private set; }

    [SerializeField] private UltimateAbilitySO? _activeUltimate;
    [SerializeField] private float _ultAvailableTimerBonus = 2f;

    private readonly Dictionary<Element, int> _comboCharges = new();
    private bool _isUltimateAvailable;
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
        Element element = moveType.Element;
        _comboCharges[element] = (_comboCharges.TryGetValue(element, out int c) ? c : 0) + 1;

        float progress = ComputeProgress();
        OnProgressChanged?.Invoke(progress);

        if (!_isUltimateAvailable && _activeUltimate != null && _activeUltimate.IsSatisfiedBy(_comboCharges))
        {
            _isUltimateAvailable = true;
            OnUltimateAvailable?.Invoke();
            ComboSystem.Instance?.ExtendTimer(_ultAvailableTimerBonus);
        }
    }

    private void HandleComboBroken()
    {
        _comboCharges.Clear();

        if (_isUltimateAvailable)
        {
            _isUltimateAvailable = false;
            OnUltimateUnavailable?.Invoke();
        }

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

        Transform? player = GameManager.Instance?.player?.transform;
        if (player != null)
            _activeUltimate.Effect?.Execute(player);

        _isUltimateAvailable = false;
        _comboCharges.Clear();
        OnUltimateUnavailable?.Invoke();
        OnProgressChanged?.Invoke(0f);
        return true;
    }

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
    /// Returns a 0-1 value representing how close the player is to satisfying all
    /// element charge requirements for the active ultimate.
    /// </summary>
    public float ComputeProgress()
    {
        if (_activeUltimate == null || _activeUltimate.Requirements.Count == 0)
            return 0f;

        int total = 0;
        int met = 0;
        foreach (var req in _activeUltimate.Requirements)
        {
            total += req.count;
            int held = _comboCharges.TryGetValue(req.element, out int c) ? c : 0;
            met += Mathf.Min(held, req.count);
        }

        return total > 0 ? (float)met / total : 0f;
    }

    #endregion
}
