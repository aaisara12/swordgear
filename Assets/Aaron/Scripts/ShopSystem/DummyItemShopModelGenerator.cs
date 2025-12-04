#nullable enable

using System.Collections.Generic;
using Shop;
using Testing;
using UnityEngine;

public class DummyItemShopModelGenerator : MonoBehaviour
{
    [SerializeField] private ItemShopModelEventChannelSO? _eventChannel;
    
    private void Awake()
    {
        _eventChannel.ThrowIfNull(nameof(_eventChannel));
        
        var catalog = new TestItemCatalog(new List<IStoreItem>()
        {
            new TestStoreItem("itemB", 5, "Firebrand"),
            new TestStoreItem("itemA", 20, "Gear Slot"),
            new TestStoreItem("itemC", 35, "Ice Whip")
        });
        var storefront = new ItemStorefront(catalog);
        
        storefront.TryStockItems(new Dictionary<string, int>
        {
            { "itemA", 1 },
            { "itemB", 2 },
            { "itemC", 1 }
        });

        var purchaser = new TestPurchaser();
        purchaser.WalletLedger.Value = 40;
        
        var model = new ItemShopModel(storefront.GetPurchasableItems(), purchaser);
        
        _eventChannel.RaiseDataChanged(model);
    }
}
