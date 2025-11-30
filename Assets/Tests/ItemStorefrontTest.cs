#nullable enable

using System.Collections.Generic;
using Shop;
using NUnit.Framework;
using Testing;

[TestFixture]
[TestOf(typeof(ItemStorefront))]
public class ItemStorefrontTest
{
    private IItemCatalog _itemCatalog = new DummyItemCatalog(
        new List<DummyStoreItem>()
        {
            new DummyStoreItem("fire-upgrade-1", "Firebrand", 50),
            new DummyStoreItem("fire-upgrade-2", "Molten Whip", 90),
            new DummyStoreItem("gear-bumper-slot", "Add Bumper Slot", 50),
            new DummyStoreItem("gear-upgrade-fire", "Fire Bumper", 30)
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

        OneItemPurchaser itemPurchaser = new OneItemPurchaser();
        itemPurchaser.WalletLedger = purchasableItem.StoreItemData.Cost;
        
        Assert.IsTrue(purchasableItem.IsReadyToPurchase(itemPurchaser));
        
        Assert.AreEqual(string.Empty, itemPurchaser.ItemPurchased);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsTrue(isSuccessfulPurchase);
        
        var properlyRecordedPurchasedItem = 
            itemPurchaser.ItemPurchased == purchasableItem.StoreItemData.Id && 
            itemPurchaser.ItemPurchasedQuantity == 1;
        
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

        OneItemPurchaser itemPurchaser = new OneItemPurchaser();
        
        int walletValueBeforePurchaseAttempt = purchasableItem.StoreItemData.Cost - 1;
        
        itemPurchaser.WalletLedger = walletValueBeforePurchaseAttempt;
        
        Assert.IsFalse(purchasableItem.IsReadyToPurchase(itemPurchaser));
        
        Assert.AreEqual(string.Empty, itemPurchaser.ItemPurchased);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        var purchaserHasNotRecordedItemPurchase = 
            itemPurchaser.ItemPurchased == string.Empty && 
            itemPurchaser.ItemPurchasedQuantity == -1;
        
        Assert.IsTrue(purchaserHasNotRecordedItemPurchase);
        
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

        OneItemPurchaser itemPurchaser = new OneItemPurchaser();
        itemPurchaser.WalletLedger = int.MaxValue;
        
        Assert.IsFalse(purchasableItem.IsReadyToPurchase(itemPurchaser));
        
        Assert.AreEqual(string.Empty, itemPurchaser.ItemPurchased);

        var isSuccessfulPurchase = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsFalse(isSuccessfulPurchase);
        
        var purchaserHasNotRecordedItemPurchase = 
            itemPurchaser.ItemPurchased == string.Empty && 
            itemPurchaser.ItemPurchasedQuantity == -1;
        
        Assert.IsTrue(purchaserHasNotRecordedItemPurchase);
        
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

        OneItemPurchaser itemPurchaser = new OneItemPurchaser();
        itemPurchaser.WalletLedger = int.MaxValue;
        
        Assert.IsTrue(purchasableItem.IsItemInStock);

        var isFirstPurchaseSuccessful = purchasableItem.TryPurchaseItem(itemPurchaser);
        
        Assert.IsTrue(isFirstPurchaseSuccessful);
        
        Assert.IsFalse(purchasableItem.IsItemInStock);
    }
} 
