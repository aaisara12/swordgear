#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Linear upgrade step flow: offer an augment pick when the hub loads, then free roam until exit portal.
/// </summary>
public class UpgradeFlowController : MonoBehaviour
{
    private const string AugmentShopSceneName = "AugmentShop";
    private const float AugmentShopLoadTimeoutSeconds = 10f;

    [SerializeField] private TriggerEventChannelSO? showAugmentChannel;
    [SerializeField] private BoolEventChannelSO? augmentVisibilityChannel;

    private bool _augmentOffered;
    private bool _augmentChosen;
    private bool _pausedForAugmentPick;

    private void Start()
    {
        RunStep? step = RunManager.Instance?.CurrentStep;
        if (step == null || step.Type != RunStepType.Upgrade)
        {
            return;
        }

        Debug.Log("[UpgradeFlowController] Upgrade step detected — starting augment offer flow.");

        if (showAugmentChannel == null)
        {
            Debug.LogError("UpgradeFlowController: showAugmentChannel is null");
            return;
        }

        if (augmentVisibilityChannel == null)
        {
            Debug.LogError("UpgradeFlowController: augmentVisibilityChannel is null");
            return;
        }

        augmentVisibilityChannel.OnDataChanged += HandleAugmentVisibilityChanged;
        StartCoroutine(OfferAugmentAfterLevelLoad());
    }

    private IEnumerator OfferAugmentAfterLevelLoad()
    {
        // Let NodeStarter / LevelLoader finish instantiating ShopLevel first.
        yield return null;

        // AugmentShop loads additively in Awake and may not be ready on the next frame.
        float elapsed = 0f;
        while (!SceneManager.GetSceneByName(AugmentShopSceneName).isLoaded)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= AugmentShopLoadTimeoutSeconds)
            {
                Debug.LogError(
                    $"UpgradeFlowController: timed out waiting for '{AugmentShopSceneName}' to load; skipping augment offer.");
                yield break;
            }

            yield return null;
        }

        // One more frame so ItemShopStateController can subscribe to event channels.
        yield return null;

        _augmentOffered = true;
        Debug.Log("[UpgradeFlowController] Raising showAugmentChannel.");
        showAugmentChannel!.RaiseEventTriggered();
    }

    private void HandleAugmentVisibilityChanged(bool isVisible)
    {
        if (!_augmentOffered || _augmentChosen)
        {
            return;
        }

        if (isVisible)
        {
            if (!_pausedForAugmentPick)
            {
                _pausedForAugmentPick = true;
                Time.timeScale = 0f;
                Debug.Log("[UpgradeFlowController] Augment UI visible — paused gameplay (timeScale=0).");
            }

            return;
        }

        _augmentChosen = true;
        ComboSystem.Instance?.ResetPointsSinceLastAugment();
        Time.timeScale = 1f;
        Debug.Log("[UpgradeFlowController] Augment chosen — resumed gameplay (timeScale=1).");
    }

    private void OnDestroy()
    {
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
