#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Provides a set of purchasable items for the UI to display and interact with.
    /// </summary>
    public class ItemStorefront
    {
        // intent is to create a very restricted interface that the UI can use to display purchasable items
        public class PurchasableItem
        {
            public IStoreItem StoreItemData { get; }

            private Func<bool> _isItemInStockMethod;
            private Func<IItemPurchaser, bool> _isReadyToPurchaseMethod;
            private Func<IItemPurchaser, bool> _tryPurchaseItemMethod;
            
            public PurchasableItem(IStoreItem storeItemData, Func<bool> isItemInStockMethod, Func<IItemPurchaser, bool> isReadyToPurchaseMethod, Func<IItemPurchaser, bool> tryPurchaseItemMethod)
            {
                StoreItemData = storeItemData;
                _isItemInStockMethod = isItemInStockMethod;
                _isReadyToPurchaseMethod = isReadyToPurchaseMethod;
                _tryPurchaseItemMethod = tryPurchaseItemMethod;
            }

            public bool IsItemInStock => _isItemInStockMethod();
            public bool IsReadyToPurchase(IItemPurchaser purchaser) => _isReadyToPurchaseMethod(purchaser);
            public bool TryPurchaseItem(IItemPurchaser purchaser) => _tryPurchaseItemMethod(purchaser);
        }
        
        private Dictionary<string, int> _availableItems = new Dictionary<string, int>();
        private IItemCatalog _itemCatalog;

        public ItemStorefront(IItemCatalog itemCatalog)
        {
            _itemCatalog = itemCatalog;
        }

        public void StockItems(IReadOnlyDictionary<string, int> items)
        {
            foreach (var itemIdAndQuantity in items)
            {
                _availableItems.TryAdd(itemIdAndQuantity.Key, 0);
                _availableItems[itemIdAndQuantity.Key] += itemIdAndQuantity.Value;
            }
        }

        public void ClearItems()
        {
            _availableItems.Clear();
        }
        
        public List<PurchasableItem> GetPurchasableItems()
        {
            var purchasableItems = new List<PurchasableItem>();

            foreach (var itemIdAndQuantity in _availableItems)
            {
                if (_itemCatalog.TryFindItemData(itemIdAndQuantity.Key, out var itemData) == false)
                {
                    Debug.LogError($"[ItemStorefront] Could not find item data for item ID {itemIdAndQuantity.Key}. Will not present as purchasable item.");
                    continue;
                }

                var purchasableItem = new PurchasableItem(
                    itemData,
                    () => IsItemInStock(itemData, _availableItems),
                    purchaser => IsItemReadyToPurchase(itemData, purchaser, _availableItems),
                    purchaser => TryPurchaseItem(itemData, purchaser, _availableItems)
                );
                
                purchasableItems.Add(purchasableItem);
            }
            
            return purchasableItems;
        }
        
        private bool IsItemInStock(IStoreItem storeItem, IReadOnlyDictionary<string, int> availableItems)
        {
            int numberOfItemAvailable = availableItems.GetValueOrDefault(storeItem.Id, 0);
            
            return numberOfItemAvailable > 0;
        }
        
        private bool IsItemReadyToPurchase(IStoreItem storeItem, IItemPurchaser purchaser, IReadOnlyDictionary<string, int> availableItems)
        {
            return purchaser.WalletLedger >= storeItem.Cost && IsItemInStock(storeItem, availableItems);
        }
        
        private bool TryPurchaseItem(IStoreItem storeItem, IItemPurchaser purchaser, Dictionary<string, int> availableItems)
        {
            string itemId = storeItem.Id;
            int itemCost = storeItem.Cost;
            
            if (IsItemReadyToPurchase(storeItem, purchaser, availableItems) == false)
            {
                return false;
            }
            
            purchaser.WalletLedger -= itemCost;
            purchaser.ReceiveItem(itemId, 1);

            availableItems[itemId] -= 1;

            return true;
        }
        
    }

    public interface IItemPurchaser
    {
        public int WalletLedger { get; set; }
        public void ReceiveItem(string itemId, int quantity);
    }
}

