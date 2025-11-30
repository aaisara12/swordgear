#nullable enable

using System.Collections.Generic;
using Shop;
using NUnit.Framework;
using Testing;

[TestFixture]
[TestOf(typeof(ItemStorefront))]
public class ItemStorefrontTest
{
    [Test]
    public void TryStockItems_AddsPurchasableItems_WhenItemInCatalog()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem>() { new TestStoreItem("itemA", 5) });
        var storefront = new ItemStorefront(catalog);

        storefront.TryStockItems(new Dictionary<string, int> { { "itemA", 2 } });

        var purchasableItems = storefront.GetPurchasableItems();

        Assert.IsNotEmpty(purchasableItems);
        Assert.AreEqual(1, purchasableItems.Count);
        Assert.AreEqual("itemA", purchasableItems[0].StoreItemData.Id);
        Assert.IsTrue(purchasableItems[0].IsItemInStock);
    }

    [Test]
    public void TryStockItems_IncrementsStock_WhenCalledMultipleTimes()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem>() { new TestStoreItem("itemB", 1) });
        var storefront = new ItemStorefront(catalog);

        storefront.TryStockItems(new Dictionary<string, int> { { "itemB", 1 } });
        storefront.TryStockItems(new Dictionary<string, int> { { "itemB", 2 } }); // total 3

        var purchasableItem = storefront.GetPurchasableItems()[0];

        var purchaser = new TestPurchaser { WalletLedger = int.MaxValue };

        int successfulPurchases = 0;
        while (purchasableItem.TryPurchaseItem(purchaser))
        {
            successfulPurchases++;
        }

        Assert.AreEqual(3, successfulPurchases);
        Assert.IsFalse(purchasableItem.IsItemInStock);
        Assert.IsTrue(purchaser.Received.ContainsKey("itemB"));
        Assert.AreEqual(3, purchaser.Received["itemB"]);
    }

    [Test]
    public void TryStockItems_IgnoresUnknownItemIds()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem>() { new TestStoreItem("known", 1) });
        var storefront = new ItemStorefront(catalog);
        
        bool isStockSuccessful = storefront.TryStockItems(new Dictionary<string, int> { { "unknown", 1 } });
        
        Assert.IsFalse(isStockSuccessful);

        var purchasableItems = storefront.GetPurchasableItems();

        Assert.IsEmpty(purchasableItems);
    }

    [Test]
    public void ClearItems_RemovesAllStock_WhenCalled()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem> { new TestStoreItem("itemC", 2) });
        var storefront = new ItemStorefront(catalog);

        storefront.TryStockItems(new Dictionary<string, int> { { "itemC", 1 } });
        Assert.IsNotEmpty(storefront.GetPurchasableItems());

        storefront.ClearItems();

        var afterClear = storefront.GetPurchasableItems();
        Assert.IsEmpty(afterClear);
    }
        
    [Test]
    public void TryPurchaseItem_RecordsPurchase_WhenSuccessful()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem> { new TestStoreItem("itemA", 5) });
        var storefront = new ItemStorefront(catalog);

        var items = catalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        storefront.TryStockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = storefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        var purchaser = new TestPurchaser
        {
            WalletLedger = purchasableItem.StoreItemData.Cost
        };

        Assert.IsTrue(purchasableItem.IsReadyToPurchase(purchaser));
        
        Assert.AreEqual(string.Empty, purchaser.Received);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(purchaser);
        
        Assert.IsTrue(isSuccessfulPurchase);
        
        var properlyRecordedPurchasedItem = 
            purchaser.Received.ContainsKey(purchasableItem.StoreItemData.Id) && 
            purchaser.Received[purchasableItem.StoreItemData.Id] == 1;
        
        Assert.IsTrue(properlyRecordedPurchasedItem);
        
        Assert.AreEqual(0, purchaser.WalletLedger);
    }

    [Test]
    public void TryPurchaseItem_Fails_WhenInsufficientFunds_NoStateChange()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem> { new TestStoreItem("itemA", 5) });
        var storefront = new ItemStorefront(catalog);

        var items = catalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        storefront.TryStockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = storefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        int walletValueBeforePurchaseAttempt = purchasableItem.StoreItemData.Cost - 1;
        
        var purchaser = new TestPurchaser
        {
            WalletLedger = walletValueBeforePurchaseAttempt
        };

        Assert.IsFalse(purchasableItem.IsReadyToPurchase(purchaser));
        
        Assert.AreEqual(string.Empty, purchaser.Received);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(purchaser);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        Assert.IsFalse(purchaser.Received.ContainsKey(purchasableItem.StoreItemData.Id));
        
        Assert.AreEqual(walletValueBeforePurchaseAttempt, purchaser.WalletLedger);
    }
    
    [Test]
    public void TryPurchaseItem_Fails_WhenOutOfStock_NoStateChange()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem> { new TestStoreItem("itemA", 5) });
        var storefront = new ItemStorefront(catalog);

        var items = catalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        storefront.TryStockItems(new Dictionary<string, int>{{testItem.Id, 0}});
        
        var purchasableItems = storefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];
        
        Assert.IsFalse(purchasableItem.IsItemInStock);

        var purchaser = new TestPurchaser
        {
            WalletLedger = int.MaxValue
        };

        Assert.IsFalse(purchasableItem.IsReadyToPurchase(purchaser));
        
        Assert.AreEqual(string.Empty, purchaser.Received);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(purchaser);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        Assert.IsFalse(purchaser.Received.ContainsKey(purchasableItem.StoreItemData.Id));
        
        Assert.AreEqual(int.MaxValue, purchaser.WalletLedger);
    }
    
    [Test]
    public void TryPurchaseItem_DecrementsStock_WhenSuccessful()
    {
        var catalog = new TestItemCatalog(new List<IStoreItem> { new TestStoreItem("itemA", 5) });
        var storefront = new ItemStorefront(catalog);

        var items = catalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        storefront.TryStockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = storefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        var purchaser = new TestPurchaser
        {
            WalletLedger = int.MaxValue
        };

        Assert.IsTrue(purchasableItem.IsItemInStock);

        var isFirstPurchaseSuccessful = purchasableItem.TryPurchaseItem(purchaser);
        
        Assert.IsTrue(isFirstPurchaseSuccessful);
        
        Assert.IsFalse(purchasableItem.IsItemInStock);
    }
} 
