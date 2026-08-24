#nullable enable

using System.Collections.Generic;
using Shop;
using Testing;
using UnityEngine;

/// <summary>
/// Responsible for computing and sending the next set of augments to display to the player.
/// </summary>
public class InGameAugmentsManager : InitializeableUnrestrictedGameComponent
{
    [Header("Input")]
    [SerializeField] private TriggerEventChannelSO? triggerAugmentGenerationEventChannel;

    [Header("Output")]
    [SerializeField] private BoolEventChannelSO? uiVisibilityEventChannel;
    [SerializeField] private ItemShopModelEventChannelSO? uiDataEventChannel;

    [Header("Other Dependencies")]
    [SerializeField] private LoadableStoreItemCatalog? augmentsCatalog;
    [SerializeField] private AugmentTierRollSettings? tierRollSettings;

    [Header("Debug (test scenes)")]
    [SerializeField] private bool useDebugComboFloor;
    [SerializeField] private AugmentQualityTier debugComboFloor = AugmentQualityTier.Low;

    private ItemStorefront? itemStorefront;
    private IItemPurchaser itemPurchaser = new TestPurchaser(1000);

    public override void InitializeOnGameStart_Dangerous(PlayerBlob playerBlob)
    {
        itemPurchaser = playerBlob;
    }

    private void Awake()
    {
        if (augmentsCatalog == null)
        {
            Debug.LogError("AugmentsCatalog is null");
            return;
        }

        if (triggerAugmentGenerationEventChannel == null)
        {
            Debug.LogError("TriggerAugmentGenerationEventChannel is null");
            return;
        }

        if (uiVisibilityEventChannel == null)
        {
            Debug.LogError("UIVisibilityEventChannel is null");
            return;
        }

        if (uiDataEventChannel == null)
        {
            Debug.LogError("UIDataEventChannel is null");
            return;
        }

        itemStorefront = new ItemStorefront(augmentsCatalog);
        itemStorefront.GetPurchasableItems();

        triggerAugmentGenerationEventChannel.OnEventTriggered += HandleTriggerAugmentGeneration;
    }

    private void OnDestroy()
    {
        if (triggerAugmentGenerationEventChannel != null)
        {
            triggerAugmentGenerationEventChannel.OnEventTriggered -= HandleTriggerAugmentGeneration;
        }
    }

    private void HandleTriggerAugmentGeneration()
    {
        if (itemStorefront == null || uiVisibilityEventChannel == null || uiDataEventChannel == null || augmentsCatalog == null)
        {
            return;
        }

        AugmentQualityTier tier = ResolveOfferTier();
        var offerItems = augmentsCatalog.GetRandomItemsForExactTier(3, tier);

        // Each offer is stamped with an element: element upgrades keep their own, stat buffs roll one. The element
        // rides along into the player's inventory, where AugmentElementLedger counts it toward ultimate unlocks.
        var offers = new List<IStoreItem>(offerItems.Count);
        foreach (IStoreItem offerItem in offerItems)
        {
            offers.Add(AugmentElementRoller.Tag(offerItem));
        }

        Debug.Log($"[InGameAugmentsManager] Offering {offers.Count} augment(s), all at tier {tier}.");

        itemStorefront.ClearItems();
        itemStorefront.StockItems(offers);
        var availableAugments = itemStorefront.GetPurchasableItems();

        var model = new ItemShopModel(availableAugments, itemPurchaser);

        uiVisibilityEventChannel.RaiseDataChanged(true);
        uiDataEventChannel.RaiseDataChanged(model);
    }

    public void ConfigureDebugComboFloor(AugmentQualityTier comboFloor)
    {
        useDebugComboFloor = true;
        debugComboFloor = comboFloor;
    }

    private AugmentQualityTier ResolveOfferTier()
    {
        if (RunManager.Instance != null
            && RunManager.Instance.TryConsumeAugmentTierOverride(out AugmentQualityTier forcedTier))
        {
            Debug.Log($"[InGameAugmentsManager] Using forced offer tier {forcedTier}.");
            return forcedTier;
        }

        AugmentTierRollWeights weights = tierRollSettings != null
            ? tierRollSettings.Weights
            : AugmentTierRollWeights.Default;

        AugmentQualityTier comboFloor = useDebugComboFloor
            ? debugComboFloor
            : ComboSystem.Instance != null
                ? ComboSystem.Instance.GetAugmentQualityTier()
                : AugmentQualityTier.Low;

        AugmentQualityTier rolled = weights.RollTier();
        return AugmentTierRollWeights.ApplyComboFloor(rolled, comboFloor);
    }
}
