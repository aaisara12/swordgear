#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Provides a set of purchasable items for the UI to display and interact with.
    /// </summary>
    public class ItemStorefront
    {
        private Dictionary<string, int> _availableItems = new Dictionary<string, int>();
        private IItemCatalog _itemCatalog;

        public ItemStorefront(IItemCatalog itemCatalog)
        {
            _itemCatalog = itemCatalog;
        }

        public bool TryStockItems(IReadOnlyDictionary<string, int> items)
        {
            foreach (var itemIdAndQuantity in items)
            {
                if (_itemCatalog.TryFindItemData(itemIdAndQuantity.Key, out _) == false)
                {
                    return false;
                }
            }
            
            foreach (var itemIdAndQuantity in items)
            {
                _availableItems.TryAdd(itemIdAndQuantity.Key, 0);
                _availableItems[itemIdAndQuantity.Key] += itemIdAndQuantity.Value;
            }

            return true;
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

                var purchasableItem = new PurchasableItem(itemData, _availableItems);
                
                purchasableItems.Add(purchasableItem);
            }
            
            return purchasableItems;
        }
    }
}

