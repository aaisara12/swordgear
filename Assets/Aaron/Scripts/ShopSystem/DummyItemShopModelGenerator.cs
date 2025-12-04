#nullable enable

using System.Collections.Generic;
using Shop;
using Testing;
using UnityEngine;

public class DummyItemShopModelGenerator : MonoBehaviour
{
    [SerializeField] private ItemShopModelEventChannelSO? _eventChannel;
    
    private ItemStorefront? _itemStorefront;

    private IItemPurchaser _purchaser = new TestPurchaser(walletLedger: 40);
    
    private void Awake()
    {
        _eventChannel.ThrowIfNull(nameof(_eventChannel));
        
        var catalog = new TestItemCatalog(new List<IStoreItem>()
        {
            new TestStoreItem("itemB", 5, "Firebrand"),
            new TestStoreItem("itemA", 20, "Gear Slot"),
            new TestStoreItem("itemC", 35, "Ice Whip")
        });
        
        _itemStorefront = new ItemStorefront(catalog);
        
        _itemStorefront.TryStockItems(new Dictionary<string, int>
        {
            { "itemA", 1 },
            { "itemB", 2 },
            { "itemC", 1 }
        });
        
        var model = new ItemShopModel(_itemStorefront.GetPurchasableItems(), _purchaser);
        
        _eventChannel.RaiseDataChanged(model);

        _itemStorefront.OnPurchasableItemsUpdated += HandlePurchasableItemsUpdated;
    }

    private void HandlePurchasableItemsUpdated()
    {
        _eventChannel.ThrowIfNull(nameof(_eventChannel));
        _itemStorefront.ThrowIfNull(nameof(_itemStorefront));
        
        _eventChannel.RaiseDataChanged(new ItemShopModel(_itemStorefront.GetPurchasableItems(), _purchaser));
    }

    private void OnDestroy()
    {
        if (_itemStorefront != null)
        {
            _itemStorefront.OnPurchasableItemsUpdated -= HandlePurchasableItemsUpdated;
        }
    }
}
