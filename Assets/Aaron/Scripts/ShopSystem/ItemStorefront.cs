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
            public IItem ItemData { get; }
            public bool IsReadyToPurchase => _isReadyToPurchaseMethod.Invoke();

            private Func<bool> _isReadyToPurchaseMethod;
            private Func<bool> _tryPurchaseItemMethod;
            
            public PurchasableItem(IItem itemData, Func<bool> isReadyToPurchaseMethod, Func<bool> tryPurchaseItemMethod)
            {
                ItemData = itemData;
                _isReadyToPurchaseMethod = isReadyToPurchaseMethod;
                _tryPurchaseItemMethod = tryPurchaseItemMethod;
            }

            public bool TryPurchaseItem() => _tryPurchaseItemMethod();
        }
        
        private PlayerBlob _playerBlob;
        private Dictionary<string, int> _availableItems = new Dictionary<string, int>();
        private IItemCatalog _itemCatalog;

        public ItemStorefront(PlayerBlob playerBlob, IItemCatalog itemCatalog)
        {
            // It might make more intuitive sense to have the PlayerBlob passed into the PurchaseItem method - right now
            // it's being passed as an invisible parameter
            _playerBlob = playerBlob;
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
                    () => IsItemReadyToPurchase(itemIdAndQuantity.Key, itemData.Cost),
                    () => TryPurchaseItem(itemData.Id, itemData.Cost)
                );
                
                purchasableItems.Add(purchasableItem);
            }
            
            return purchasableItems;
        }
        
        private bool IsItemReadyToPurchase(string itemId, int itemCost)
        {
            int numberOfItemAvailable = _availableItems.GetValueOrDefault(itemId, 0);
            
            return _playerBlob.CurrencyAmount.Value >= itemCost && numberOfItemAvailable > 0;
        }
        
        private bool TryPurchaseItem(string itemId, int itemCost)
        {
            if (IsItemReadyToPurchase(itemId, itemCost) == false)
            {
                return false;
            }
            
            _playerBlob.CurrencyAmount.Value -= itemCost;

            if (!_playerBlob.InventoryItems.TryAdd(itemId, 1))
            {
                _playerBlob.InventoryItems[itemId] += 1;
            }

            return true;
        }
        
    }
}

