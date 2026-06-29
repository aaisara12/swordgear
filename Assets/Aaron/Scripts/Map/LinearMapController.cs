#nullable enable

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Linear map interstitial UI: scrolling rail + player token.
/// Commit 01: uses a hardcoded mock run (C×3 → U) — no RunManager yet.
/// </summary>
public class LinearMapController : MonoBehaviour
{
    private enum MockStepType
    {
        Combat,
        Upgrade
    }

    private readonly struct MockStep
    {
        public readonly MockStepType Type;
        public readonly int StepIndex;

        public MockStep(MockStepType type, int stepIndex)
        {
            Type = type;
            StepIndex = stepIndex;
        }
    }

    [Header("Rail")]
    [SerializeField] private RectTransform? railContainer;
    [SerializeField] private RectTransform? playerToken;
    [SerializeField] private Image? combatNodePrefab;
    [SerializeField] private Image? upgradeNodePrefab;
    [SerializeField] private float nodeSpacing = 180f;
    [SerializeField] private float tokenYOffset = -58f;

    [Header("Mock preview (commit 01)")]
    [SerializeField] private ArenaLayoutTemplate? mockCombatLayout;
    [SerializeField, Range(0, 3)] private int mockCurrentStepIndex;

    [Header("Other Scenes")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    private static readonly MockStep[] MockRun =
    {
        new MockStep(MockStepType.Combat, 0),
        new MockStep(MockStepType.Combat, 1),
        new MockStep(MockStepType.Combat, 2),
        new MockStep(MockStepType.Upgrade, 3),
    };

    private readonly List<Image> _spawnedNodes = new List<Image>();
    private readonly List<Color> _nodeBaseColors = new List<Color>();
    private readonly List<RawImage?> _spawnedMinimapPreviews = new List<RawImage?>();
    private int _lastAppliedMockStepIndex = -1;
    private bool _railBuilt;
    private Texture2D? _mockCombatMinimap;

    private void OnEnable()
    {
        combatHudVisibilityChannel?.RaiseDataChanged(false);
        PlayerGameplayManager.Instance?.DespawnPawn();
        Time.timeScale = 1f;

        _lastAppliedMockStepIndex = -1;
        EnsureRailBuilt();
        ApplyMockStepPresentation();
    }

    private void Update()
    {
        ApplyMockStepPresentation();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        mockCurrentStepIndex = Mathf.Clamp(mockCurrentStepIndex, 0, MockRun.Length - 1);
        if (Application.isPlaying && isActiveAndEnabled)
        {
            EnsureRailBuilt();
            _lastAppliedMockStepIndex = -1;
            ApplyMockStepPresentation();
        }
    }
#endif

    private void EnsureRailBuilt()
    {
        if (_railBuilt || railContainer == null) return;

        for (int i = 0; i < MockRun.Length; i++)
        {
            MockStep step = MockRun[i];
            Image? prefab = step.Type == MockStepType.Upgrade ? upgradeNodePrefab : combatNodePrefab;
            if (prefab == null) continue;

            Image node = Instantiate(prefab, railContainer);
            var rt = node.rectTransform;
            rt.anchoredPosition = new Vector2(step.StepIndex * nodeSpacing, 0f);
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
            if (step.Type == MockStepType.Combat)
            {
                preview = node.GetComponentInChildren<RawImage>();
                Texture2D? minimap = GetOrCreateMockCombatMinimap();
                if (preview != null && minimap != null)
                {
                    preview.texture = minimap;
                }
            }

            _spawnedMinimapPreviews.Add(preview);
        }

        _railBuilt = true;
    }

    private void ApplyMockStepPresentation()
    {
        mockCurrentStepIndex = Mathf.Clamp(mockCurrentStepIndex, 0, MockRun.Length - 1);
        if (mockCurrentStepIndex == _lastAppliedMockStepIndex) return;

        _lastAppliedMockStepIndex = mockCurrentStepIndex;

        if (railContainer != null)
        {
            float scroll = mockCurrentStepIndex * nodeSpacing;
            railContainer.anchoredPosition = new Vector2(-scroll, railContainer.anchoredPosition.y);
        }

        for (int i = 0; i < _spawnedNodes.Count; i++)
        {
            Image node = _spawnedNodes[i];
            if (node == null) continue;
            node.color = ApplyNodeStateColor(_nodeBaseColors[i], i, mockCurrentStepIndex);

            RawImage? preview = _spawnedMinimapPreviews[i];
            if (preview != null)
            {
                preview.color = ApplyMinimapStateColor(i, mockCurrentStepIndex);
            }
        }

        if (playerToken != null)
        {
            playerToken.SetAsLastSibling();
            float x = mockCurrentStepIndex * nodeSpacing;
            playerToken.anchoredPosition = new Vector2(x, tokenYOffset);
        }
    }

    private Texture2D? GetOrCreateMockCombatMinimap()
    {
        if (_mockCombatMinimap != null)
        {
            return _mockCombatMinimap;
        }

        if (mockCombatLayout?.LevelPrefab == null)
        {
            return null;
        }

        // Keep the temp room active — MinimapMapGenerator skips inactive tilemaps.
        GameObject tempRoom = Instantiate(mockCombatLayout.LevelPrefab);
        tempRoom.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            (Texture2D? map, _, bool hasContent) = MinimapMapGenerator.Generate(tempRoom);
            if (hasContent && map != null)
            {
                _mockCombatMinimap = map;
            }
            else
            {
                Debug.LogWarning(
                    $"LinearMapController: Failed to generate mock combat minimap from '{mockCombatLayout.name}'.");
            }
        }
        finally
        {
            Destroy(tempRoom);
        }

        return _mockCombatMinimap;
    }

    /// <summary>Cycle-combat label: combats are {cycle}-1..3, upgrades are {cycle+1}-0.</summary>
    private static string FormatStepLabel(int stepIndex, MockStepType type)
    {
        const int combatsPerBlock = 3;
        const int stepsPerBlock = combatsPerBlock + 1;

        if (type == MockStepType.Upgrade)
        {
            int blockIndex = stepIndex / stepsPerBlock;
            return $"{blockIndex + 2}-0";
        }

        int cycle = (stepIndex / stepsPerBlock) + 1;
        int combatNumber = (stepIndex % stepsPerBlock) + 1;
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

    private void OnDisable()
    {
        foreach (Image img in _spawnedNodes)
        {
            if (img != null) Destroy(img.gameObject);
        }
        _spawnedNodes.Clear();
        _nodeBaseColors.Clear();
        _spawnedMinimapPreviews.Clear();
        _railBuilt = false;
        _lastAppliedMockStepIndex = -1;

        if (_mockCombatMinimap != null)
        {
            Destroy(_mockCombatMinimap);
            _mockCombatMinimap = null;
        }
    }
}
