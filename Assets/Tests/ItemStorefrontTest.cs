#nullable enable

using System.Collections.Generic;
using Shop;
using NUnit.Framework;
using Testing;

[TestFixture]
[TestOf(typeof(ItemStorefront))]
public class ItemStorefrontTest
{
    private IItemCatalog _itemCatalog = new TestItemCatalog(
        new List<IStoreItem>()
        {
            new TestStoreItem("fire-upgrade-1", 50),
            new TestStoreItem("fire-upgrade-2", 90),
            new TestStoreItem("gear-bumper-slot", 50),
            new TestStoreItem("gear-upgrade-fire", 30)
        });

    [Test]
    public void TryPurchaseItem_RecordsPurchase_WhenSuccessful()
    {
        ItemStorefront itemStorefront = new ItemStorefront(_itemCatalog);

        var items = _itemCatalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        itemStorefront.StockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = itemStorefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        TestPurchaser itemPurchaser = new TestPurchaser
        {
            WalletLedger = purchasableItem.StoreItemData.Cost
        };

        Assert.IsTrue(purchasableItem.IsReadyToPurchase(itemPurchaser));
        
        Assert.AreEqual(string.Empty, itemPurchaser.Received);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsTrue(isSuccessfulPurchase);
        
        var properlyRecordedPurchasedItem = 
            itemPurchaser.Received.ContainsKey(purchasableItem.StoreItemData.Id) && 
            itemPurchaser.Received[purchasableItem.StoreItemData.Id] == 1;
        
        Assert.IsTrue(properlyRecordedPurchasedItem);
        
        Assert.AreEqual(0, itemPurchaser.WalletLedger);
    }

    [Test]
    public void TryPurchaseItem_Fails_WhenInsufficientFunds_NoStateChange()
    {
        ItemStorefront itemStorefront = new ItemStorefront(_itemCatalog);

        var items = _itemCatalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        itemStorefront.StockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = itemStorefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        TestPurchaser itemPurchaser = new TestPurchaser();
        
        int walletValueBeforePurchaseAttempt = purchasableItem.StoreItemData.Cost - 1;
        
        itemPurchaser.WalletLedger = walletValueBeforePurchaseAttempt;
        
        Assert.IsFalse(purchasableItem.IsReadyToPurchase(itemPurchaser));
        
        Assert.AreEqual(string.Empty, itemPurchaser.Received);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        Assert.IsFalse(itemPurchaser.Received.ContainsKey(purchasableItem.StoreItemData.Id));
        
        Assert.AreEqual(walletValueBeforePurchaseAttempt, itemPurchaser.WalletLedger);
    }
    
    [Test]
    public void TryPurchaseItem_Fails_WhenOutOfStock_NoStateChange()
    {
        ItemStorefront itemStorefront = new ItemStorefront(_itemCatalog);

        var items = _itemCatalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        itemStorefront.StockItems(new Dictionary<string, int>{{testItem.Id, 0}});
        
        var purchasableItems = itemStorefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];
        
        Assert.IsFalse(purchasableItem.IsItemInStock);

        TestPurchaser itemPurchaser = new TestPurchaser();
        itemPurchaser.WalletLedger = int.MaxValue;
        
        Assert.IsFalse(purchasableItem.IsReadyToPurchase(itemPurchaser));
        
        Assert.AreEqual(string.Empty, itemPurchaser.Received);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        Assert.IsFalse(itemPurchaser.Received.ContainsKey(purchasableItem.StoreItemData.Id));
        
        Assert.AreEqual(int.MaxValue, itemPurchaser.WalletLedger);
    }
    
    [Test]
    public void TryPurchaseItem_DecrementsStock_WhenSuccessful()
    {
        ItemStorefront itemStorefront = new ItemStorefront(_itemCatalog);

        var items = _itemCatalog.GetItems();
        
        Assert.IsNotEmpty(items);

        var testItem = items[0];
        
        itemStorefront.StockItems(new Dictionary<string, int>{{testItem.Id, 1}});
        
        var purchasableItems = itemStorefront.GetPurchasableItems();
        
        Assert.IsNotEmpty(purchasableItems);

        var purchasableItem = purchasableItems[0];

        TestPurchaser itemPurchaser = new TestPurchaser();
        itemPurchaser.WalletLedger = int.MaxValue;
        
        Assert.IsTrue(purchasableItem.IsItemInStock);

        var isFirstPurchaseSuccessful = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsTrue(isFirstPurchaseSuccessful);
        
        Assert.IsFalse(purchasableItem.IsItemInStock);
    }
} 
