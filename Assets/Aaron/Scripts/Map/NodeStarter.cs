#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the Arena for the current linear run combat step via <see cref="RunManager.CurrentStep"/>.
/// </summary>
public class NodeStarter : MonoBehaviour
{
    [Header("Combat HUD")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    [Header("Stage Complete")]
    [SerializeField] private BoolEventChannelSO? stageCompleteVisibilityChannel;
    [SerializeField] private ComboPerformanceEventChannelSO? stageCompletePerformanceChannel;

    private LevelLoader? _levelLoader;

    private void Start()
    {
        RunManager? runManager = RunManager.Instance;
        if (runManager == null)
        {
            Debug.LogError("NodeStarter: RunManager.Instance is null.");
            return;
        }

        RunStep? step = runManager.CurrentStep;
        if (step == null)
        {
            Debug.LogError("NodeStarter: no current run step to load.");
            return;
        }

        if (step.Type != RunStepType.Combat)
        {
            Debug.LogError($"NodeStarter: expected combat step but got {step.Type}.");
            return;
        }

        _levelLoader = LevelLoader.Instance;
        if (_levelLoader == null)
        {
            Debug.LogError("NodeStarter: LevelLoader.Instance is null.");
            return;
        }

        ArenaLayoutTemplate? layout = runManager.ResolveCombatLayout();
        if (layout == null || layout.LevelPrefab == null)
        {
            Debug.LogError("NodeStarter: no combat arena layout assigned.");
            return;
        }

        List<EnemyWaveConfig> waves = runManager.BuildCombatWaves();
        if (waves.Count == 0)
        {
            Debug.LogError("NodeStarter: combat wave pool is empty.");
            return;
        }

        combatHudVisibilityChannel?.RaiseDataChanged(true);

        var blueprint = new LevelBlueprint
        {
            Layout = layout,
            Waves = waves,
            IsShopLevel = false
        };

        _levelLoader.OnLevelClear += HandleCombatCleared;
        _levelLoader.LoadLevel(blueprint);
    }

    private void HandleCombatCleared()
    {
        Unsubscribe();
        ShowStageComplete();
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
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
