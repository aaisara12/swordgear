#nullable enable

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Linear map interstitial UI: scrolling rail + player token driven by <see cref="RunManager.Run"/>.
/// </summary>
public class LinearMapController : MonoBehaviour
{
    [Header("Rail")]
    [SerializeField] private RectTransform? railContainer;
    [SerializeField] private RectTransform? playerToken;
    [SerializeField] private Image? combatNodePrefab;
    [SerializeField] private Image? upgradeNodePrefab;
    [SerializeField] private Image? trailSegmentPrefab;
    [SerializeField] private float nodeSpacing = 270f;
    [SerializeField] private float nodeHalfWidth = 60f;
    [SerializeField] private float trailThickness = 8f;
    [SerializeField] private float trailGlowExtraThickness = 6f;
    [SerializeField] private float tokenYOffset = -88f;
    [SerializeField] private float railOffsetX = -110f;
    [SerializeField] private float interstitialHoldSeconds = 1.5f;

    [Header("Trail animation")]
    [SerializeField] private float trailPulseWidth = 28f;
    [SerializeField] private float trailPulseDuration = 1.15f;
    [SerializeField] private float trailPulseFadeStart = 0.7f;

    [Header("Other Scenes")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    private readonly List<Image> _spawnedNodes = new List<Image>();
    private readonly List<Color> _nodeBaseColors = new List<Color>();
    private readonly List<RawImage?> _spawnedMinimapPreviews = new List<RawImage?>();
    private readonly List<Image> _spawnedTrailGlows = new List<Image>();
    private readonly List<Image> _spawnedTrailCores = new List<Image>();
    private readonly List<Image> _spawnedTrailPulses = new List<Image>();
    private readonly Dictionary<int, Texture2D> _layoutMinimapCache = new Dictionary<int, Texture2D>();
    private int _lastAppliedStepIndex = -1;
    private int _lastBuiltStepCount = -1;
    private bool _railBuilt;
    private Coroutine? _interstitialCoroutine;

    private void OnEnable()
    {
        combatHudVisibilityChannel?.RaiseDataChanged(false);
        PlayerGameplayManager.Instance?.DespawnPawn();
        Time.timeScale = 1f;

        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunChanged += HandleRunChanged;
            RunManager.Instance.EnsureRunStarted();
        }

        _lastAppliedStepIndex = -1;
        RebuildRailFromRun();
        ApplyStepPresentation();
        _interstitialCoroutine = StartCoroutine(CompleteInterstitialAfterHold());
    }

    private IEnumerator CompleteInterstitialAfterHold()
    {
        if (interstitialHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(interstitialHoldSeconds);
        }

        RunManager.Instance?.OnMapInterstitialComplete();
        _interstitialCoroutine = null;
    }

    private void OnDisable()
    {
        if (_interstitialCoroutine != null)
        {
            StopCoroutine(_interstitialCoroutine);
            _interstitialCoroutine = null;
        }

        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunChanged -= HandleRunChanged;
        }

        StopPresentationAnimations();
        ClearRail();
    }

    private void HandleRunChanged()
    {
        _lastAppliedStepIndex = -1;
        RebuildRailFromRun();
        ApplyStepPresentation();
    }

    private void RebuildRailFromRun()
    {
        LinearRunState? run = RunManager.Instance?.Run;
        int stepCount = run?.Steps.Count ?? 0;
        if (_railBuilt && stepCount == _lastBuiltStepCount)
        {
            return;
        }

        ClearRail();

        if (railContainer == null || run == null || stepCount == 0)
        {
            return;
        }

        float halfWidth = ResolveNodeHalfWidth();
        BuildTrailSegments(run.Steps.Count, halfWidth);

        for (int i = 0; i < run.Steps.Count; i++)
        {
            RunStep step = run.Steps[i];
            Image? prefab = step.Type == RunStepType.Upgrade ? upgradeNodePrefab : combatNodePrefab;
            if (prefab == null)
            {
                continue;
            }

            Image node = Instantiate(prefab, railContainer);
            node.rectTransform.anchoredPosition = new Vector2(step.StepIndex * nodeSpacing, 0f);
            _nodeBaseColors.Add(prefab.color);
            _spawnedNodes.Add(node);

            TextMeshProUGUI? label = node.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = FormatStepLabel(i, step.Type);
                label.outlineWidth = 0.22f;
                label.outlineColor = Color.black;
                label.transform.SetAsLastSibling();
            }

            RawImage? preview = null;
            if (step.Type == RunStepType.Combat)
            {
                preview = node.GetComponentInChildren<RawImage>();
                Texture2D? minimap = GetOrCreateLayoutMinimap(step.Layout);
                if (preview != null && minimap != null)
                {
                    preview.texture = minimap;
                }
            }

            _spawnedMinimapPreviews.Add(preview);
        }

        _railBuilt = true;
        _lastBuiltStepCount = stepCount;
        ApplyStepPresentation();
    }

    private float ResolveNodeHalfWidth()
    {
        if (nodeHalfWidth > 0f)
        {
            return nodeHalfWidth;
        }

        if (combatNodePrefab != null)
        {
            return combatNodePrefab.rectTransform.rect.width * 0.5f;
        }

        return 60f;
    }

    private void BuildTrailSegments(int stepCount, float halfWidth)
    {
        if (trailSegmentPrefab == null || railContainer == null || stepCount < 2)
        {
            return;
        }

        for (int i = 0; i < stepCount - 1; i++)
        {
            float fromX = i * nodeSpacing + halfWidth;
            float length = nodeSpacing - (halfWidth * 2f);
            if (length <= 0f)
            {
                continue;
            }

            Image glow = Instantiate(trailSegmentPrefab, railContainer);
            ConfigureTrailSegment(glow.rectTransform, fromX, length, trailThickness + trailGlowExtraThickness);
            glow.raycastTarget = false;
            _spawnedTrailGlows.Add(glow);

            Image core = Instantiate(trailSegmentPrefab, railContainer);
            ConfigureTrailSegment(core.rectTransform, fromX, length, trailThickness);
            core.raycastTarget = false;
            _spawnedTrailCores.Add(core);

            Image pulse = Instantiate(trailSegmentPrefab, railContainer);
            ConfigureTrailPulse(pulse.rectTransform, fromX, length);
            pulse.raycastTarget = false;
            pulse.gameObject.SetActive(false);
            _spawnedTrailPulses.Add(pulse);
        }
    }

    private void ConfigureTrailSegment(RectTransform rt, float fromX, float length, float thickness)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(fromX, 0f);
        rt.sizeDelta = new Vector2(length, thickness);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    private void ConfigureTrailPulse(RectTransform rt, float fromX, float length)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(trailPulseWidth, trailThickness + 10f);
        rt.anchoredPosition = new Vector2(fromX + (trailPulseWidth * 0.5f), 0f);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    private void ApplyStepPresentation()
    {
        LinearRunState? run = RunManager.Instance?.Run;
        if (run == null)
        {
            return;
        }

        int currentIndex = run.CurrentStepIndex;
        if (currentIndex == _lastAppliedStepIndex)
        {
            return;
        }

        int transitionSegment = RunManager.Instance?.GetTrailTransitionSegmentIndex() ?? -1;
        _lastAppliedStepIndex = currentIndex;
        ApplyStaticPresentation(currentIndex, transitionSegment < 0);
        RestartPresentationAnimations(currentIndex, transitionSegment);
    }

    private void ApplyStaticPresentation(int currentIndex, bool snapPositions)
    {
        if (snapPositions && railContainer != null)
        {
            // Nodes/token are children of the rail, so their local positions are multiplied by the rail's
            // localScale. The scroll lives in the rail's PARENT space, so it must match that scale to keep
            // the current node anchored at railOffsetX.
            float scroll = currentIndex * nodeSpacing * railContainer.localScale.x;
            railContainer.anchoredPosition = new Vector2(railOffsetX - scroll, railContainer.anchoredPosition.y);
        }

        float halfWidth = ResolveNodeHalfWidth();

        for (int i = 0; i < _spawnedTrailGlows.Count; i++)
        {
            Image? glow = _spawnedTrailGlows[i];
            Image? core = _spawnedTrailCores[i];
            if (glow != null)
            {
                glow.color = ApplyTrailGlowColor(i, currentIndex);
            }

            if (core != null)
            {
                core.color = ApplyTrailCoreColor(i, currentIndex);
            }
        }

        for (int i = 0; i < _spawnedNodes.Count; i++)
        {
            Image node = _spawnedNodes[i];
            if (node == null)
            {
                continue;
            }

            node.rectTransform.localScale = Vector3.one;
            node.color = ApplyNodeStateColor(_nodeBaseColors[i], i, currentIndex);

            RawImage? preview = _spawnedMinimapPreviews[i];
            if (preview != null)
            {
                preview.color = ApplyMinimapStateColor(i, currentIndex);
            }
        }

        if (playerToken != null)
        {
            playerToken.SetAsLastSibling();
            if (snapPositions)
            {
                float x = currentIndex * nodeSpacing;
                playerToken.anchoredPosition = new Vector2(x, tokenYOffset);
                playerToken.localScale = Vector3.one;
            }
        }
    }

    private void RestartPresentationAnimations(int currentIndex, int transitionSegment)
    {
        StopPresentationAnimations();
        if (transitionSegment < 0)
        {
            foreach (Image pulse in _spawnedTrailPulses)
            {
                if (pulse != null)
                {
                    pulse.gameObject.SetActive(false);
                }
            }

            return;
        }

        float halfWidth = ResolveNodeHalfWidth();
        AnimateTokenTransition(transitionSegment, currentIndex);

        for (int i = 0; i < _spawnedTrailPulses.Count; i++)
        {
            Image pulse = _spawnedTrailPulses[i];
            if (pulse == null)
            {
                continue;
            }

            if (i != transitionSegment)
            {
                pulse.gameObject.SetActive(false);
                continue;
            }

            float fromX = i * nodeSpacing + halfWidth;
            float length = nodeSpacing - (halfWidth * 2f);
            float startX = fromX + (trailPulseWidth * 0.5f);
            float endX = fromX + length - (trailPulseWidth * 0.5f);

            StartOneWayTrailPulse(pulse, startX, endX);
        }
    }

    private void AnimateTokenTransition(int previousIndex, int currentIndex)
    {
        if (railContainer != null)
        {
            float railScale = railContainer.localScale.x; // scroll must match the rail's scale (see ApplyStaticPresentation)
            float startScroll = previousIndex * nodeSpacing * railScale;
            float endScroll = currentIndex * nodeSpacing * railScale;
            railContainer.anchoredPosition = new Vector2(railOffsetX - startScroll, railContainer.anchoredPosition.y);

            DOTween.To(
                    () => railContainer.anchoredPosition.x,
                    value => railContainer.anchoredPosition = new Vector2(value, railContainer.anchoredPosition.y),
                    railOffsetX - endScroll,
                    trailPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetId(this);
        }

        if (playerToken != null)
        {
            float startX = previousIndex * nodeSpacing;
            float endX = currentIndex * nodeSpacing;
            playerToken.SetAsLastSibling();
            playerToken.anchoredPosition = new Vector2(startX, tokenYOffset);
            playerToken.localScale = Vector3.one * 0.96f;

            DOTween.To(
                    () => playerToken.anchoredPosition.x,
                    value => playerToken.anchoredPosition = new Vector2(value, tokenYOffset),
                    endX,
                    trailPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetId(this);

            playerToken.DOScale(1.05f, trailPulseDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .SetId(this);
        }
    }

    private void StartOneWayTrailPulse(Image pulse, float startX, float endX)
    {
        RectTransform pulseRt = pulse.rectTransform;
        const float peakAlpha = 0.95f;
        Color pulseRgb = new Color(1f, 0.95f, 0.45f, peakAlpha);

        pulse.gameObject.SetActive(true);
        pulseRt.localScale = Vector3.one;
        pulseRt.anchoredPosition = new Vector2(startX, 0f);
        pulse.color = pulseRgb;

        DOTween.To(
                () => 0f,
                t =>
                {
                    pulseRt.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, t), 0f);

                    float alpha = t <= trailPulseFadeStart
                        ? peakAlpha
                        : Mathf.Lerp(peakAlpha, 0f, (t - trailPulseFadeStart) / (1f - trailPulseFadeStart));

                    Color color = pulseRgb;
                    color.a = alpha;
                    pulse.color = color;
                },
                1f,
                trailPulseDuration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetId(this);
    }

    private void StopPresentationAnimations()
    {
        DOTween.Kill(this);
    }

    private Texture2D? GetOrCreateLayoutMinimap(ArenaLayoutTemplate? layout)
    {
        if (layout == null || layout.LevelPrefab == null)
        {
            return null;
        }

        int cacheKey = layout.GetInstanceID();
        if (_layoutMinimapCache.TryGetValue(cacheKey, out Texture2D cached))
        {
            return cached;
        }

        GameObject tempRoom = Instantiate(layout.LevelPrefab);
        tempRoom.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            (Texture2D? map, _, bool hasContent) = MinimapMapGenerator.Generate(tempRoom);
            if (hasContent && map != null)
            {
                _layoutMinimapCache[cacheKey] = map;
                return map;
            }

            Debug.LogWarning(
                $"LinearMapController: Failed to generate minimap for layout '{layout.name}'.");
        }
        finally
        {
            Destroy(tempRoom);
        }

        return null;
    }

    /// <summary>Cycle-combat label: combats are {cycle}-1..3, upgrades are {cycle+1}-0.</summary>
    private static string FormatStepLabel(int stepIndex, RunStepType type)
    {
        if (type == RunStepType.Upgrade)
        {
            int blockIndex = stepIndex / LinearRunGenerator.StepsPerBlock;
            return $"{blockIndex + 2}-0";
        }

        int cycle = (stepIndex / LinearRunGenerator.StepsPerBlock) + 1;
        int combatNumber = (stepIndex % LinearRunGenerator.StepsPerBlock) + 1;
        return $"{cycle}-{combatNumber}";
    }

    private static Color ApplyTrailGlowColor(int segmentIndex, int currentIndex)
    {
        if (segmentIndex < currentIndex)
        {
            return new Color(0.25f, 0.75f, 0.45f, 0.45f);
        }

        return new Color(0.18f, 0.22f, 0.32f, 0.35f);
    }

    private static Color ApplyTrailCoreColor(int segmentIndex, int currentIndex)
    {
        if (segmentIndex < currentIndex)
        {
            return new Color(0.45f, 0.95f, 0.58f, 0.95f);
        }

        return new Color(0.42f, 0.48f, 0.62f, 0.85f);
    }

    private static Color ApplyMinimapStateColor(int stepIndex, int currentIndex)
    {
        if (stepIndex < currentIndex)
        {
            return new Color(0.7f, 0.7f, 0.7f, 0.9f);
        }

        if (stepIndex == currentIndex)
        {
            return Color.white;
        }

        return new Color(0.5f, 0.5f, 0.55f, 0.8f);
    }

    private static Color ApplyNodeStateColor(Color baseColor, int stepIndex, int currentIndex)
    {
        if (stepIndex < currentIndex)
        {
            return new Color(
                baseColor.r * 0.55f,
                Mathf.Min(1f, baseColor.g * 1.15f),
                baseColor.b * 0.55f,
                baseColor.a);
        }

        if (stepIndex == currentIndex)
        {
            return baseColor;
        }

        return new Color(
            baseColor.r * 0.5f,
            baseColor.g * 0.5f,
            baseColor.b * 0.55f,
            baseColor.a * 0.9f);
    }

    private void ClearRail()
    {
        StopPresentationAnimations();

        DestroySpawnedImages(_spawnedTrailPulses);
        DestroySpawnedImages(_spawnedTrailGlows);
        DestroySpawnedImages(_spawnedTrailCores);
        DestroySpawnedImages(_spawnedNodes);

        _spawnedNodes.Clear();
        _nodeBaseColors.Clear();
        _spawnedMinimapPreviews.Clear();
        _spawnedTrailGlows.Clear();
        _spawnedTrailCores.Clear();
        _spawnedTrailPulses.Clear();
        _railBuilt = false;
        _lastBuiltStepCount = -1;
        _lastAppliedStepIndex = -1;

        foreach (Texture2D texture in _layoutMinimapCache.Values)
        {
            Destroy(texture);
        }

        _layoutMinimapCache.Clear();
    }

    private static void DestroySpawnedImages(List<Image> images)
    {
        foreach (Image img in images)
        {
            if (img != null)
            {
                Destroy(img.gameObject);
            }
        }
    }
}
