#nullable enable

using System.Collections.Generic;
using AaronPrototype.ShopSystem;
using NUnit.Framework;

public class TestShop
{
    // Test that player
    [Test]
    public void TestPlayerPurchaseRecorded()
    {
        const int kItemCost = 40;
        const int kPlayerCurrency = 100;
        const string kItemId = "fire-upgrade-1";
        const string kItemDisplayName = "Firebrand";
        
        PlayerBlob blob = new PlayerBlob();
        
        blob.CurrencyAmount.Value = kPlayerCurrency;
        
        DummyItemCatalog itemCatalog = new DummyItemCatalog(new List<DummyItem>()
        {
            new DummyItem(kItemId, kItemDisplayName, kItemCost)
        });
        
        ShopSystem shopSystem = new ShopSystem(blob, itemCatalog);
        
        var purchasableItems = shopSystem.GetPurchasableItems();
        
        Assert.IsEmpty(blob.PurchasedItems);
        
        var isSuccessfulPurchase = purchasableItems[0].TryPurchaseItem();
        
        Assert.IsTrue(isSuccessfulPurchase);
        
        Assert.AreEqual(1, blob.PurchasedItems.Count);
        
        var properlyRecordedPurchasedItem = blob.PurchasedItems.Contains(new KeyValuePair<string, int>(kItemId, 1));
        
        Assert.IsTrue(properlyRecordedPurchasedItem);
        
        Assert.AreEqual(kPlayerCurrency - kItemCost, blob.CurrencyAmount.Value);
    }

    [Test]
    public void TestPlayerPurchaseInsufficientFunds()
    {
        const int kItemCost = 40;
        const int kPlayerCurrency = 30;
        const string kItemId = "fire-upgrade-1";
        const string kItemDisplayName = "Firebrand";

        PlayerBlob blob = new PlayerBlob();

        blob.CurrencyAmount.Value = kPlayerCurrency;

        DummyItemCatalog itemCatalog = new DummyItemCatalog(new List<DummyItem>()
        {
            new DummyItem(kItemId, kItemDisplayName, kItemCost)
        });

        ShopSystem shopSystem = new ShopSystem(blob, itemCatalog);

        var purchasableItems = shopSystem.GetPurchasableItems();

        Assert.IsEmpty(blob.PurchasedItems);

        var isSuccessfulPurchase = purchasableItems[0].TryPurchaseItem();

        Assert.IsFalse(isSuccessfulPurchase);

        Assert.IsEmpty(blob.PurchasedItems);

        Assert.AreEqual(kPlayerCurrency, blob.CurrencyAmount.Value);
    }
}
