#nullable enable

using System.Collections.Generic;
using Shop;
using NUnit.Framework;
using Testing;

public class TestShop
{
    private IItemCatalog _itemCatalog;
    private PlayerBlob _playerBlob;

    public TestShop()
    {
        _playerBlob = new PlayerBlob();
        _itemCatalog = new DummyItemCatalog(
            new List<DummyItem>()
            {
                new DummyItem("fire-upgrade-1", "Firebrand", 50),
                new DummyItem("fire-upgrade-2", "Molten Whip", 90),
                new DummyItem("gear-bumper-slot", "Add Bumper Slot", 50),
                new DummyItem("gear-upgrade-fire", "Fire Bumper", 30)
            });
    }

    [SetUp]
    public void SetUp()
    {
        _playerBlob.CurrencyAmount.Value = 0;
        _playerBlob.InventoryItems.Clear();
    }
    
    
    [Test]
    public void TestPurchaseViaStorefrontRecorded()
    {
        ItemStorefront itemStorefront = new ItemStorefront(_itemCatalog);

        var items = _itemCatalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        itemStorefront.StockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = itemStorefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];
        
        _playerBlob.CurrencyAmount.Value = purchasableItem.ItemData.Cost;
        
        Assert.IsTrue(purchasableItem.IsReadyToPurchase(_playerBlob));
        
        Assert.IsEmpty(_playerBlob.InventoryItems);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(_playerBlob);
        
        Assert.IsTrue(isSuccessfulPurchase);
        
        var properlyRecordedPurchasedItem = _playerBlob.InventoryItems.Contains(new KeyValuePair<string, int>(purchasableItem.ItemData.Id, 1));
        
        Assert.IsTrue(properlyRecordedPurchasedItem);
        
        Assert.AreEqual(0, _playerBlob.CurrencyAmount.Value);
    }

    [Test]
    public void TestStorefrontBlocksPurchaseIfInsufficientFunds()
    {
        ItemStorefront itemStorefront = new ItemStorefront(_itemCatalog);

        var items = _itemCatalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        itemStorefront.StockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = itemStorefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        int amountInPlayerWalletBeforePurchase = purchasableItem.ItemData.Cost - 1;
        
        _playerBlob.CurrencyAmount.Value = amountInPlayerWalletBeforePurchase;
        
        Assert.IsFalse(purchasableItem.IsReadyToPurchase(_playerBlob));
        
        Assert.IsEmpty(_playerBlob.InventoryItems);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(_playerBlob);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        var properlyRecordedPurchasedItem = _playerBlob.InventoryItems.Contains(new KeyValuePair<string, int>(purchasableItem.ItemData.Id, 1));
        
        Assert.IsFalse(properlyRecordedPurchasedItem);
        
        Assert.AreEqual(amountInPlayerWalletBeforePurchase, _playerBlob.CurrencyAmount.Value);
    }
}
