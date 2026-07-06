#nullable enable

using Shop;
using UnityEngine;

/// <summary>
/// Test-scene helper: raises the augment generation channel so <see cref="InGameAugmentsManager"/>
/// rolls a fresh set of three augments. Wire to a UI button's OnClick.
/// </summary>
public class AugmentPickerTestHarness : MonoBehaviour
{
    [SerializeField] private TriggerEventChannelSO? refreshAugmentsChannel;
    [SerializeField] private InGameAugmentsManager? augmentsManager;
    [SerializeField] private bool refreshOnStart = true;
    [SerializeField] private AugmentQualityTier debugComboFloor = AugmentQualityTier.Low;

    private void Awake()
    {
        if (augmentsManager == null)
        {
            augmentsManager = FindFirstObjectByType<InGameAugmentsManager>();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (refreshOnStart)
        {
            RefreshAugmentPicker();
        }
    }

    public void RefreshAugmentPicker()
    {
        if (refreshAugmentsChannel == null)
        {
            Debug.LogError("AugmentPickerTestHarness: refreshAugmentsChannel is not assigned.");
            return;
        }

        if (augmentsManager != null)
        {
            augmentsManager.ConfigureDebugComboFloor(debugComboFloor);
        }

        refreshAugmentsChannel.RaiseEventTriggered();
        ReapplyCardVisuals();
    }

    public void SetComboFloorTier(int tierIndex)
    {
        debugComboFloor = (AugmentQualityTier)Mathf.Clamp(tierIndex, 0, 3);
        RefreshAugmentPicker();
    }

    public void ReapplyCardVisuals()
    {
        AugmentShopElementViewModel[] cards = FindObjectsByType<AugmentShopElementViewModel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (AugmentShopElementViewModel card in cards)
        {
            card.RefreshTierVisuals();
        }
    }
}
