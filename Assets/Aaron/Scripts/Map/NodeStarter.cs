#nullable enable

using UnityEngine;

/// <summary>
/// Drives a single Arena node from <see cref="RunManager.CurrentNode"/> (replaces RoundStarter's 3-level loop).
/// Builds a one-off level blueprint and loads it; on clear it shows Stage Complete (Combat) or completes
/// the run (Boss). Shop nodes load with no waves and are exited via the shop UI.
/// </summary>
public class NodeStarter : MonoBehaviour
{
    [Header("Combat HUD")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    [Header("Stage Complete")]
    [SerializeField] private BoolEventChannelSO? stageCompleteVisibilityChannel;
    [SerializeField] private ComboPerformanceEventChannelSO? stageCompletePerformanceChannel;

    private LevelLoader? _levelLoader;
    private MapNode? _node;

    private void Start()
    {
        if (RunManager.Instance == null)
        {
            Debug.LogError("NodeStarter: RunManager.Instance is null.");
            return;
        }

        _node = RunManager.Instance.CurrentNode;
        if (_node == null)
        {
            Debug.LogError("NodeStarter: no current node to load.");
            return;
        }

        _levelLoader = LevelLoader.Instance;
        if (_levelLoader == null)
        {
            Debug.LogError("NodeStarter: LevelLoader.Instance is null.");
            return;
        }

        if (_node.Layout == null)
        {
            Debug.LogError($"NodeStarter: node {_node.Id} ({_node.Type}) has no arena layout assigned.");
            return;
        }

        // Returning to a combat context: make sure the HUD is visible.
        combatHudVisibilityChannel?.RaiseDataChanged(true);

        var blueprint = new LevelBlueprint
        {
            Layout = _node.Layout,
            Waves = _node.Waves,
            IsShopLevel = _node.Type == NodeType.Shop
        };

        if (_node.Type == NodeType.Boss)
        {
            _levelLoader.OnLevelClear += HandleBossCleared;
        }
        else if (_node.Type == NodeType.Combat)
        {
            _levelLoader.OnLevelClear += HandleCombatCleared;
        }
        // Shop: no clear handler; exit is driven by the shop UI -> RunManager.ReturnToMapAfterNode().

        _levelLoader.LoadLevel(blueprint);
    }

    private void HandleCombatCleared()
    {
        Unsubscribe();
        ShowStageComplete();
    }

    private void HandleBossCleared()
    {
        Unsubscribe();
        RunManager.Instance?.CompleteRun();
    }

    private void ShowStageComplete()
    {
        if (stageCompletePerformanceChannel != null && ComboSystem.Instance != null)
        {
            stageCompletePerformanceChannel.RaiseDataChanged(ComboSystem.Instance.GetCurrentLevelPerformance());
        }

        stageCompleteVisibilityChannel?.RaiseDataChanged(true);
    }

    private void Unsubscribe()
    {
        if (_levelLoader == null)
        {
            return;
        }

        _levelLoader.OnLevelClear -= HandleCombatCleared;
        _levelLoader.OnLevelClear -= HandleBossCleared;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
