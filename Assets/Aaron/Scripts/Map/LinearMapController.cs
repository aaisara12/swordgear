#nullable enable

using System.Collections.Generic;
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
    [SerializeField] private float nodeSpacing = 180f;
    [SerializeField] private float tokenYOffset = -58f;

    [Header("Other Scenes")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    private readonly List<Image> _spawnedNodes = new List<Image>();
    private readonly List<Color> _nodeBaseColors = new List<Color>();
    private readonly List<RawImage?> _spawnedMinimapPreviews = new List<RawImage?>();
    private readonly Dictionary<int, Texture2D> _layoutMinimapCache = new Dictionary<int, Texture2D>();
    private int _lastAppliedStepIndex = -1;
    private int _lastBuiltStepCount = -1;
    private bool _railBuilt;

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
    }

    private void OnDisable()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnRunChanged -= HandleRunChanged;
        }

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

        _lastAppliedStepIndex = currentIndex;

        if (railContainer != null)
        {
            float scroll = currentIndex * nodeSpacing;
            railContainer.anchoredPosition = new Vector2(-scroll, railContainer.anchoredPosition.y);
        }

        for (int i = 0; i < _spawnedNodes.Count; i++)
        {
            Image node = _spawnedNodes[i];
            if (node == null)
            {
                continue;
            }

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
            float x = currentIndex * nodeSpacing;
            playerToken.anchoredPosition = new Vector2(x, tokenYOffset);
        }
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
        foreach (Image img in _spawnedNodes)
        {
            if (img != null)
            {
                Destroy(img.gameObject);
            }
        }

        _spawnedNodes.Clear();
        _nodeBaseColors.Clear();
        _spawnedMinimapPreviews.Clear();
        _railBuilt = false;
        _lastBuiltStepCount = -1;
        _lastAppliedStepIndex = -1;

        foreach (Texture2D texture in _layoutMinimapCache.Values)
        {
            Destroy(texture);
        }

        _layoutMinimapCache.Clear();
    }
}
