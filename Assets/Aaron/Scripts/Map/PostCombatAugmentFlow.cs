#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// After a combat stage clears, offers a standard-tier augment pick (existing % split) before the exit portal spawns.
/// Upgrade hub diamond offers are handled by <see cref="UpgradeFlowController"/>.
/// </summary>
public class PostCombatAugmentFlow : MonoBehaviour
{
    private const string AugmentShopSceneName = "AugmentShop";
    private const float AugmentShopLoadTimeoutSeconds = 10f;

    [SerializeField] private BoolEventChannelSO? augmentVisibilityChannel;
    [SerializeField] private float delayAfterClearSeconds = 0.75f;

    private LevelLoader? _levelLoader;
    private bool _augmentOffered;
    private bool _augmentChosen;
    private bool _pausedForAugmentPick;

    private void Start()
    {
        RunStep? step = RunManager.Instance?.CurrentStep;
        if (step == null || step.Type != RunStepType.Combat)
        {
            return;
        }

        _levelLoader = LevelLoader.Instance;
        if (_levelLoader == null)
        {
            Debug.LogError("PostCombatAugmentFlow: LevelLoader.Instance is null.");
            return;
        }

        if (augmentVisibilityChannel == null)
        {
            Debug.LogError("PostCombatAugmentFlow: augmentVisibilityChannel is null.");
            return;
        }

        augmentVisibilityChannel.OnDataChanged += HandleAugmentVisibilityChanged;
        _levelLoader.OnLevelClear += HandleCombatCleared;
    }

    private void HandleCombatCleared()
    {
        if (_augmentOffered)
        {
            return;
        }

        StartCoroutine(OfferAugmentAfterClear());
    }

    private IEnumerator OfferAugmentAfterClear()
    {
        // Brief beat after stage-complete UI, then freeze for the pick.
        if (delayAfterClearSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delayAfterClearSeconds);
        }

        PauseForAugmentPick();

        float elapsed = 0f;
        while (!SceneManager.GetSceneByName(AugmentShopSceneName).isLoaded)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= AugmentShopLoadTimeoutSeconds)
            {
                Debug.LogError(
                    $"PostCombatAugmentFlow: timed out waiting for '{AugmentShopSceneName}'; spawning portal without augment.");
                FinishAndSpawnPortal();
                yield break;
            }

            yield return null;
        }

        // One frame so ItemShopStateController can subscribe (same as UpgradeFlowController).
        yield return null;

        _augmentOffered = true;
        Debug.Log("[PostCombatAugmentFlow] Offering standard-tier post-combat augment.");
        RunManager.Instance?.OfferStandardAugmentPick();
    }

    private void HandleAugmentVisibilityChanged(bool isVisible)
    {
        if (!_augmentOffered || _augmentChosen)
        {
            return;
        }

        if (isVisible)
        {
            PauseForAugmentPick();
            return;
        }

        FinishAndSpawnPortal();
        Debug.Log("[PostCombatAugmentFlow] Augment chosen — resumed gameplay and spawned exit portal.");
    }

    private void FinishAndSpawnPortal()
    {
        _augmentChosen = true;
        ComboSystem.Instance?.ResetPointsSinceLastAugment();
        ResumeGameplay();
        _levelLoader?.RequestExitPortal();
    }

    private void PauseForAugmentPick()
    {
        if (_pausedForAugmentPick)
        {
            return;
        }

        _pausedForAugmentPick = true;
        Time.timeScale = 0f;
        Debug.Log("[PostCombatAugmentFlow] Paused gameplay for augment pick.");
    }

    private void ResumeGameplay()
    {
        Time.timeScale = 1f;
        _pausedForAugmentPick = false;
    }

    private void OnDestroy()
    {
        if (_levelLoader != null)
        {
            _levelLoader.OnLevelClear -= HandleCombatCleared;
        }

        if (augmentVisibilityChannel != null)
        {
            augmentVisibilityChannel.OnDataChanged -= HandleAugmentVisibilityChanged;
        }

        if (_pausedForAugmentPick && !_augmentChosen)
        {
            Time.timeScale = 1f;
        }
    }
}
