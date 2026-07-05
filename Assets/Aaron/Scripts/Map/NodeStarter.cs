#nullable enable

using UnityEngine;

/// <summary>
/// Loads the Arena for the current linear run step via <see cref="RunManager.CurrentStep"/>.
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

        _levelLoader = LevelLoader.Instance;
        if (_levelLoader == null)
        {
            Debug.LogError("NodeStarter: LevelLoader.Instance is null.");
            return;
        }

        LevelBlueprint? blueprint = runManager.BuildBlueprintForCurrentStep();
        if (blueprint == null)
        {
            return;
        }

        if (step.Type == RunStepType.Combat)
        {
            combatHudVisibilityChannel?.RaiseDataChanged(true);
            _levelLoader.OnExitPortalEntered += HandleCombatExitPortalEntered;
            _levelLoader.OnLevelClear += HandleCombatCleared;
        }
        else if (step.Type == RunStepType.Upgrade)
        {
            combatHudVisibilityChannel?.RaiseDataChanged(false);
            _levelLoader.OnExitPortalEntered += HandleUpgradeExitPortalEntered;
        }
        else
        {
            Debug.LogError($"NodeStarter: unsupported step type {step.Type}.");
            return;
        }

        _levelLoader.LoadLevel(blueprint);
    }

    private void HandleCombatCleared()
    {
        ShowStageComplete();
    }

    private void HandleCombatExitPortalEntered()
    {
        RunManager.Instance?.HandleCombatPortalExited();
    }

    private void HandleUpgradeExitPortalEntered()
    {
        RunManager.Instance?.HandleUpgradeComplete();
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
        _levelLoader.OnExitPortalEntered -= HandleCombatExitPortalEntered;
        _levelLoader.OnExitPortalEntered -= HandleUpgradeExitPortalEntered;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
