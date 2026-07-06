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
    [SerializeField] private bool useDebugMinimumTier = true;
    [SerializeField] private AugmentQualityTier debugMinimumTier = AugmentQualityTier.Low;

    public AugmentQualityTier DebugMinimumTier
    {
        get => debugMinimumTier;
        set => debugMinimumTier = value;
    }

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
            augmentsManager.ConfigureDebugMinimumTier(useDebugMinimumTier, debugMinimumTier);
        }

        refreshAugmentsChannel.RaiseEventTriggered();
        ReapplyCardVisuals();
    }

    public void SetDebugMinimumTier(int tierIndex)
    {
        debugMinimumTier = (AugmentQualityTier)Mathf.Clamp(tierIndex, 0, 3);
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
