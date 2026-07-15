#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a pie-chart ring showing per-element ultimate charge.
/// Each slice corresponds to one element requirement of the active ultimate.
/// Filled portions are opaque; the unfilled track runs at low alpha.
/// When an element is partially charged its fill arc uses _partialAlpha;
/// when fully charged it snaps to full alpha.
/// </summary>
public class UltimateChargeCircleUI : MaskableGraphic
{
    [Serializable]
    public struct ElementColorEntry
    {
        public Element element;
        public Color color;
    }

    [SerializeField] private float _outerRadius = 50f;
    [SerializeField] private float _innerRadius = 35f;
    [SerializeField] private float _gapAngle = 4f;
    [SerializeField] private int _segmentsPerSlice = 20;
    [SerializeField] private float _trackAlpha = 0.15f;
    [SerializeField] private float _partialAlpha = 0.5f;
    [SerializeField] private List<ElementColorEntry> _elementColors = new();
    [SerializeField] private GameObject? _ultimateReadyIndicator;
    [SerializeField] private Transform? _halo;
    [SerializeField] private float _haloSpinSpeed = 90f;
    [SerializeField] private float _haloPulseDelta = 0.08f;
    [SerializeField] private float _haloPulseSpeed = 3f;

    private readonly List<(Element element, float fill)> _currentFills = new();
    private bool _subscribed;
    private bool _isUltimateReady;
    private Vector3 _haloBaseScale = Vector3.one;

    protected override void OnEnable()
    {
        base.OnEnable();
        TrySubscribe();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (!_subscribed || UltimateChargeTracker.Instance == null) return;
        UltimateChargeTracker.Instance.OnProgressChanged -= HandleProgressChanged;
        UltimateChargeTracker.Instance.OnUltimateAvailable -= HandleUltimateAvailable;
        UltimateChargeTracker.Instance.OnUltimateUnavailable -= HandleUltimateUnavailable;
        _subscribed = false;
    }

    protected override void Start()
    {
        base.Start();
        if (_halo != null)
            _haloBaseScale = _halo.localScale;
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed || UltimateChargeTracker.Instance == null)
        {
            return;
        }
        UltimateChargeTracker.Instance.OnProgressChanged += HandleProgressChanged;
        UltimateChargeTracker.Instance.OnUltimateAvailable += HandleUltimateAvailable;
        UltimateChargeTracker.Instance.OnUltimateUnavailable += HandleUltimateUnavailable;
        _subscribed = true;
        SetUltimateIndicator(UltimateChargeTracker.Instance.IsUltimateAvailable);
        RefreshFills();
    }

    private void HandleProgressChanged(float _) => RefreshFills();

    private void HandleUltimateAvailable() => SetUltimateIndicator(true);

    private void HandleUltimateUnavailable() => SetUltimateIndicator(false);

    private void SetUltimateIndicator(bool active)
    {
        _isUltimateReady = active;
        if (_ultimateReadyIndicator != null)
            _ultimateReadyIndicator.SetActive(active);
        if (!active && _halo != null)
            _halo.localScale = _haloBaseScale;
    }

    private void Update()
    {
        if (!_isUltimateReady || _halo == null) return;
        _halo.Rotate(0f, 0f, _haloSpinSpeed * Time.deltaTime, Space.Self);
        float pulse = 1f + _haloPulseDelta * Mathf.Sin(Time.time * _haloPulseSpeed);
        _halo.localScale = _haloBaseScale * pulse;
    }

    private void RefreshFills()
    {
        _currentFills.Clear();
        UltimateChargeTracker.Instance?.GetChargeFills(_currentFills);
        SetVerticesDirty();
    }

    [UnityEngine.ContextMenu("Debug: Force Refresh")]
    private void DebugForceRefresh()
    {
        RefreshFills();
        Debug.Log($"[UltimateChargeCircleUI] fills={_currentFills.Count}, " +
                  $"tracker={(UltimateChargeTracker.Instance != null ? "found" : "NULL")}, " +
                  $"activeUlt={(UltimateChargeTracker.Instance?.ActiveUltimate != null ? "assigned" : "NULL")}");
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_currentFills.Count == 0) return;

        int n = _currentFills.Count;
        int segs = Mathf.Max(1, _segmentsPerSlice);
        float sectorAngle = Mathf.Max(0f, (360f - _gapAngle * n) / n);

        for (int i = 0; i < n; i++)
        {
            var (element, fill) = _currentFills[i];
            float startAngle = i * (sectorAngle + _gapAngle);
            float endAngle = startAngle + sectorAngle;
            Color baseColor = GetElementColor(element);

            // Background track — always visible at low alpha so the slot shape is readable
            AddAnnularSector(vh, startAngle, endAngle, segs,
                new Color(baseColor.r, baseColor.g, baseColor.b, _trackAlpha));

            // Filled arc drawn on top of the track
            if (fill > 0.001f)
            {
                float alpha = fill >= 1f - 0.001f ? 1f : _partialAlpha;
                AddAnnularSector(vh, startAngle, startAngle + sectorAngle * fill, segs,
                    new Color(baseColor.r, baseColor.g, baseColor.b, alpha));
            }
        }
    }

    // Draws an annular sector (ring segment). Angles are clockwise from top (0 = 12 o'clock).
    private void AddAnnularSector(VertexHelper vh, float startDeg, float endDeg, int segments, Color color)
    {
        if (endDeg <= startDeg) return;

        int baseIndex = vh.currentVertCount;

        for (int i = 0; i <= segments; i++)
        {
            float angleDeg = Mathf.Lerp(startDeg, endDeg, (float)i / segments);
            float rad = angleDeg * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);

            // Clockwise-from-top in Unity UI coords: x = sin(a), y = cos(a)
            vh.AddVert(new Vector3(s * _innerRadius, c * _innerRadius), color, Vector2.zero);
            vh.AddVert(new Vector3(s * _outerRadius, c * _outerRadius), color, Vector2.zero);
        }

        for (int i = 0; i < segments; i++)
        {
            int b = baseIndex + i * 2;
            // inner[i], outer[i], outer[i+1]
            vh.AddTriangle(b, b + 1, b + 3);
            // inner[i], outer[i+1], inner[i+1]
            vh.AddTriangle(b, b + 3, b + 2);
        }
    }

    private Color GetElementColor(Element element)
    {
        foreach (var entry in _elementColors)
        {
            if (entry.element == element)
                return entry.color;
        }
        return Color.white;
    }
}
