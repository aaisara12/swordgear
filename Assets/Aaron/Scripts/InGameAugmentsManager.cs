#nullable enable

using System.Collections.Generic;
using Shop;
using UnityEngine;

/// <summary>
/// Responsible for computing and sending the next set of augments to display to the player.
/// </summary>
public class InGameAugmentsManager : InitializeableUnrestrictedGameComponent
{
    [SerializeField] private TriggerEventChannelSO? triggerAugmentGenerationEventChannel;
    [SerializeField] private ItemShopModelEventChannelSO? uiOutputEventChannel;
    [SerializeField] private LoadableStoreItemCatalog? augmentsCatalog;
    
    private ItemStorefront? itemStorefront;
    private IItemPurchaser? itemPurchaser;
    
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
        if (itemPurchaser == null)
        {
            return;
        }

        if (itemStorefront == null)
        {
            Debug.LogError("ItemStorefront is null. This shouldn't happen because it is initialized in Awake at the same time as this subscription.");
            return;
        }

        if (uiOutputEventChannel == null)
        {
            // We already have an error log elsewhere, so no need to log again.
            return;
        }

        if (augmentsCatalog == null)
        {
            return;
        }

        // TODO: aisara => Add logic to clear and set up new augments based on game state, augments player already has, etc.
        var mysteryItem = augmentsCatalog.GetAnItem();
        itemStorefront.ClearItems();
        itemStorefront.TryStockItems(new Dictionary<string, int>{ { mysteryItem.Id, 1 } });
        var availableAugments = itemStorefront.GetPurchasableItems();
        
        var model = new ItemShopModel(availableAugments, itemPurchaser);
        
        uiOutputEventChannel.RaiseDataChanged(model);
    }
}
