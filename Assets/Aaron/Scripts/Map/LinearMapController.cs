#nullable enable

using System.Collections.Generic;
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
    [SerializeField] private float nodeSpacing = 120f;
    [SerializeField] private int visibleNodeCount = 12;
    [SerializeField] private float tokenYOffset = -42f;

    [Header("Mock preview (commit 01)")]
    [SerializeField] private int mockCurrentStepIndex;

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
    private float _scrollOffset;

    private void OnEnable()
    {
        combatHudVisibilityChannel?.RaiseDataChanged(false);
        PlayerGameplayManager.Instance?.DespawnPawn();
        Time.timeScale = 1f;

        RebuildRail();
    }

    private void RebuildRail()
    {
        ClearNodes();
        if (railContainer == null) return;

        int anchor = Mathf.Max(0, mockCurrentStepIndex - 1);
        int end = Mathf.Min(MockRun.Length, anchor + visibleNodeCount);

        _scrollOffset = anchor * nodeSpacing;
        railContainer.anchoredPosition = new Vector2(-_scrollOffset, railContainer.anchoredPosition.y);

        for (int i = anchor; i < end; i++)
        {
            MockStep step = MockRun[i];
            Image? prefab = step.Type == MockStepType.Upgrade ? upgradeNodePrefab : combatNodePrefab;
            if (prefab == null) continue;

            Image node = Instantiate(prefab, railContainer);
            var rt = node.rectTransform;
            rt.anchoredPosition = new Vector2(step.StepIndex * nodeSpacing, 0f);

            if (i < mockCurrentStepIndex)
            {
                node.color = new Color(0.35f, 0.75f, 0.4f, 1f);
            }
            else if (i == mockCurrentStepIndex)
            {
                node.color = Color.white;
            }
            else
            {
                node.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            }

            _spawnedNodes.Add(node);
        }

        if (playerToken != null)
        {
            playerToken.SetAsLastSibling();
            float x = mockCurrentStepIndex * nodeSpacing;
            playerToken.anchoredPosition = new Vector2(x, tokenYOffset);
        }
    }

    private void ClearNodes()
    {
        foreach (Image img in _spawnedNodes)
        {
            if (img != null) Destroy(img.gameObject);
        }
        _spawnedNodes.Clear();
    }
}
