#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks ultimate ability charge based on points awarded by the ComboSystem,
/// and records how much each Element contributed.
/// </summary>
public class UltimateMeter : MonoBehaviour
{
    [SerializeField] private int pointsRequiredForFullCharge = 200;

    private int _totalPoints;
    private readonly Dictionary<Element, int> _elementPoints = new Dictionary<Element, int>();
    private bool _isSubscribed;

    public float NormalizedCharge => pointsRequiredForFullCharge <= 0
        ? 0f
        : Mathf.Clamp01((float)_totalPoints / pointsRequiredForFullCharge);

    public bool IsReady => NormalizedCharge >= 1f;

    public event Action<float>? OnUltimateChargeChanged;
    public event Action? OnUltimateReady;

    private void Start()
    {
        // Subscribe in Start to ensure ComboSystem.Instance is available
        SubscribeToComboSystem();
    }

    private void OnEnable()
    {
        // Also subscribe in OnEnable in case Start hasn't run yet
        SubscribeToComboSystem();
    }

    private void OnDisable()
    {
        if (ComboSystem.Instance != null && _isSubscribed)
        {
            ComboSystem.Instance.OnPointsAwarded -= HandlePointsAwarded;
            _isSubscribed = false;
        }
    }

    private void SubscribeToComboSystem()
    {
        if (ComboSystem.Instance != null && !_isSubscribed)
        {
            ComboSystem.Instance.OnPointsAwarded += HandlePointsAwarded;
            _isSubscribed = true;
            // Initialize charge display
            OnUltimateChargeChanged?.Invoke(NormalizedCharge);
        }
    }

    private void Update()
    {
        // Retry subscription if we're not subscribed yet and ComboSystem is now available
        if (!_isSubscribed)
        {
            SubscribeToComboSystem();
        }
    }

    private void HandlePointsAwarded(int points, Element element)
    {
        if (points <= 0)
        {
            return;
        }

        float mult = PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.UltimateChargeMultiplier : 1f;
        int effectivePoints = Mathf.RoundToInt(points * mult);
        _totalPoints += effectivePoints;

        if (_elementPoints.ContainsKey(element))
        {
            _elementPoints[element] += points;
        }
        else
        {
            _elementPoints[element] = points;
        }

        float charge = NormalizedCharge;
        OnUltimateChargeChanged?.Invoke(charge);

        if (charge >= 1f)
        {
            OnUltimateReady?.Invoke();
        }
    }

    /// <summary>
    /// Resets the ultimate meter for a new round.
    /// </summary>
    public void ResetForNewRound()
    {
        _totalPoints = 0;
        _elementPoints.Clear();

        float charge = NormalizedCharge;
        OnUltimateChargeChanged?.Invoke(charge);
    }

    /// <summary>
    /// Attempts to consume a full ultimate charge.
    /// For now this just logs usage and resets the meter.
    /// </summary>
    public bool TryUseUltimate()
    {
        if (!IsReady)
        {
            return false;
        }

        Debug.Log($"Using ultimate. Total points: {_totalPoints}");

        foreach (Element element in Enum.GetValues(typeof(Element)))
        {
            int value = _elementPoints.TryGetValue(element, out var pts) ? pts : 0;
            Debug.Log($"Ultimate contribution - {element}: {value}");
        }

        // Reset the meter after use.
        _totalPoints = 0;
        _elementPoints.Clear();

        float charge = NormalizedCharge;
        OnUltimateChargeChanged?.Invoke(charge);

        return true;
    }
}

