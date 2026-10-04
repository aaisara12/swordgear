#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the active ultimate's readout in two states.
/// While the ult is locked it is a pie-chart ring: one slice per element requirement, each showing how far that
/// element's augments have come toward unlocking it. Filled portions are opaque, the unfilled track runs at low
/// alpha, and a partially filled slice uses _partialAlpha until it snaps to full alpha when complete.
/// Once unlocked the slices give way to an unbroken ring in _chargeRingColor that sweeps from the top as damage
/// charge accumulates, and the element slices move to a thinner band just outside it, where they now track
/// progress toward the next overcharge level. Each overcharge level earned nests one more charge ring inside the
/// first; every ring carries the same shared charge, so the meter reads as one bar that thickens with power.
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

    [Header("Unlocked Layout")]
    [Tooltip("Colour of the unbroken charge ring shown once the ultimate is unlocked.")]
    [SerializeField] private Color _chargeRingColor = Color.white;
    [Tooltip("Gap between concentric bands — between stacked charge rings, and between the outermost charge " +
             "ring and the overcharge slices outside it.")]
    [SerializeField] private float _ringSpacing = 3f;
    [Tooltip("Thickness of the element slice band that tracks progress toward the next overcharge level.")]
    [SerializeField] private float _overchargeRingThickness = 8f;
    [Tooltip("Stacked charge rings nest inward and are thinned to fit rather than collapsing past this radius.")]
    [SerializeField] private float _minRingRadius = 6f;

    [SerializeField] private GameObject? _ultimateReadyIndicator;
    [Tooltip("Optional. Shows \"Lv N\" while the ult is overcharged past level 1. Leave unassigned to skip.")]
    [SerializeField] private TMPro.TMP_Text? _levelLabel;
    [SerializeField] private Transform? _halo;
    [SerializeField] private float _haloSpinSpeed = 90f;
    [SerializeField] private float _haloPulseDelta = 0.08f;
    [SerializeField] private float _haloPulseSpeed = 3f;

    private readonly List<(Element element, float fill)> _currentFills = new();
    private bool _isUnlocked;
    private float _chargeFill;
    private int _overchargeLevel;
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
        if (!_subscribed) return;

        if (UltimateChargeTracker.Instance != null)
        {
            UltimateChargeTracker.Instance.OnProgressChanged -= HandleProgressChanged;
            UltimateChargeTracker.Instance.OnUltimateAvailable -= HandleUltimateAvailable;
            UltimateChargeTracker.Instance.OnUltimateUnavailable -= HandleUltimateUnavailable;
            UltimateChargeTracker.Instance.OnOverchargeChanged -= HandleOverchargeChanged;
        }

        // Cleared even when the tracker is already gone, so re-enabling re-subscribes to whichever tracker is
        // live then. Leaving it set would strand the readout on a destroyed tracker's events.
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
        UltimateChargeTracker.Instance.OnOverchargeChanged += HandleOverchargeChanged;
        _subscribed = true;
        SetUltimateIndicator(UltimateChargeTracker.Instance.IsUltimateAvailable);
        HandleOverchargeChanged(UltimateChargeTracker.Instance.OverchargeLevel);
        RefreshFills();
    }

    private void HandleProgressChanged(float _) => RefreshFills();

    private void HandleUltimateAvailable() => SetUltimateIndicator(true);

    private void HandleUltimateUnavailable() => SetUltimateIndicator(false);

    private void HandleOverchargeChanged(int overchargeLevel)
    {
        if (_levelLabel == null) return;
        _levelLabel.gameObject.SetActive(overchargeLevel > 0);
        _levelLabel.text = $"Lv {overchargeLevel + 1}";
    }

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
        // The tracker usually lives in an additively-loaded systems scene, so it can appear after this UI — and
        // be replaced on a scene swap. Retrying here costs a static null check and keeps the readout attached.
        if (!_subscribed)
            TrySubscribe();

        if (!_isUltimateReady || _halo == null) return;
        _halo.Rotate(0f, 0f, _haloSpinSpeed * Time.deltaTime, Space.Self);
        float pulse = 1f + _haloPulseDelta * Mathf.Sin(Time.time * _haloPulseSpeed);
        _halo.localScale = _haloBaseScale * pulse;
    }

    private void RefreshFills()
    {
        UltimateChargeTracker? tracker = UltimateChargeTracker.Instance;
        _isUnlocked = tracker != null && tracker.IsUnlocked;
        _chargeFill = tracker != null ? tracker.ChargeProgress : 0f;
        _overchargeLevel = tracker != null ? tracker.OverchargeLevel : 0;

        _currentFills.Clear();
        tracker?.GetRequirementSegments(_currentFills);

        SetVerticesDirty();
    }

    [UnityEngine.ContextMenu("Debug: Force Refresh")]
    private void DebugForceRefresh()
    {
        RefreshFills();
        Debug.Log($"[UltimateChargeCircleUI] unlocked={_isUnlocked}, charge={_chargeFill:0.00}, " +
                  $"overcharge={_overchargeLevel}, fills={_currentFills.Count}, " +
                  $"tracker={(UltimateChargeTracker.Instance != null ? "found" : "NULL")}, " +
                  $"activeUlt={(UltimateChargeTracker.Instance?.ActiveUltimate != null ? "assigned" : "NULL")}");
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (!_isUnlocked)
        {
            // Locked: the requirement slices are the whole readout and own the main band.
            AddRequirementSlices(vh, _innerRadius, _outerRadius);
            return;
        }

        AddChargeRings(vh);

        // Unlocked: the slices step outside the charge rings and now count toward the next overcharge level.
        float sliceInner = _outerRadius + Mathf.Max(0f, _ringSpacing);
        AddRequirementSlices(vh, sliceInner, sliceInner + Mathf.Max(0.01f, _overchargeRingThickness));
    }

    /// <summary>
    /// One unbroken ring per earned level — the base ult plus one per overcharge — nested inward from the main
    /// band, all sweeping clockwise from the top with the same shared charge. The stack thins to fit rather than
    /// running past _minRingRadius, so a high overcharge cap never collapses the ring through the centre.
    /// </summary>
    private void AddChargeRings(VertexHelper vh)
    {
        int ringCount = Mathf.Max(1, _overchargeLevel + 1);
        float thickness = Mathf.Max(0.01f, _outerRadius - _innerRadius);
        float spacing = Mathf.Max(0f, _ringSpacing);

        float required = ringCount * thickness + (ringCount - 1) * spacing;
        float available = Mathf.Max(0.01f, _outerRadius - Mathf.Max(0f, _minRingRadius));
        if (required > available)
        {
            float scale = available / required;
            thickness *= scale;
            spacing *= scale;
        }

        Color color = _chargeRingColor;
        Color trackColor = new(color.r, color.g, color.b, color.a * _trackAlpha);

        int segs = Mathf.Max(8, _segmentsPerSlice * 4);
        float fill = Mathf.Clamp01(_chargeFill);
        // Scale the segment count with the arc so a sliver stays cheap and a near-full ring stays smooth.
        int fillSegs = Mathf.Max(1, Mathf.CeilToInt(segs * fill));

        for (int ring = 0; ring < ringCount; ring++)
        {
            float outer = _outerRadius - ring * (thickness + spacing);
            float inner = outer - thickness;

            AddAnnularSector(vh, 0f, 360f, segs, trackColor, inner, outer);

            if (fill > 0.001f)
                AddAnnularSector(vh, 0f, 360f * fill, fillSegs, color, inner, outer);
        }
    }

    // One slice per element requirement, each filling toward the next set of that element's augments.
    private void AddRequirementSlices(VertexHelper vh, float innerRadius, float outerRadius)
    {
        int n = _currentFills.Count;
        if (n == 0) return;

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
                new Color(baseColor.r, baseColor.g, baseColor.b, _trackAlpha), innerRadius, outerRadius);

            // Filled arc drawn on top of the track
            if (fill > 0.001f)
            {
                float alpha = fill >= 1f - 0.001f ? 1f : _partialAlpha;
                AddAnnularSector(vh, startAngle, startAngle + sectorAngle * fill, segs,
                    new Color(baseColor.r, baseColor.g, baseColor.b, alpha), innerRadius, outerRadius);
            }
        }
    }

    // Draws an annular sector (ring segment). Angles are clockwise from top (0 = 12 o'clock).
    private void AddAnnularSector(VertexHelper vh, float startDeg, float endDeg, int segments, Color color,
        float innerRadius, float outerRadius)
    {
        if (endDeg <= startDeg || outerRadius <= innerRadius) return;

        int baseIndex = vh.currentVertCount;

        for (int i = 0; i <= segments; i++)
        {
            float angleDeg = Mathf.Lerp(startDeg, endDeg, (float)i / segments);
            float rad = angleDeg * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);

            // Clockwise-from-top in Unity UI coords: x = sin(a), y = cos(a)
            vh.AddVert(new Vector3(s * innerRadius, c * innerRadius), color, Vector2.zero);
            vh.AddVert(new Vector3(s * outerRadius, c * outerRadius), color, Vector2.zero);
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
