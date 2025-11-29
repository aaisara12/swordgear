#nullable enable

using System;
using System.Collections.Generic;

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
            public string Name { get; }
            public int Cost { get; }
            public bool IsPurchasable { get; }

            private Func<bool> _tryPurchaseItemMethod;
            
            public PurchasableItem(string name, int cost, bool isPurchasable, Func<bool> tryPurchaseItemMethod)
            {
                Name = name;
                Cost = cost;
                IsPurchasable = isPurchasable;
                _tryPurchaseItemMethod = tryPurchaseItemMethod;
            }

            public bool TryPurchaseItem() => _tryPurchaseItemMethod();
        }
        
        private PlayerBlob _playerBlob;
        private IItemCatalog _itemCatalog;

        public ItemStorefront(PlayerBlob playerBlob, IItemCatalog itemCatalog)
        {
            _playerBlob = playerBlob;
            _itemCatalog = itemCatalog;
            
            // TODO: Instead, get a list of items and quantity so we can determine whether an item is sold out or not
        }
        
        public List<PurchasableItem> GetPurchasableItems()
        {
            var items = _itemCatalog.GetItems();
            
            var purchasableItems = new List<PurchasableItem>();

            foreach (var item in items)
            {
                var purchasableItem = new PurchasableItem(
                    item.DisplayName,
                    item.Cost,
                    _playerBlob.CurrencyAmount.Value >= item.Cost,
                    () => TryPurchaseItem(item.Id, item.Cost)
                );
                
                purchasableItems.Add(purchasableItem);
            }
            
            return purchasableItems;
        }
        
        private bool TryPurchaseItem(string itemId, int itemCost)
        {
            if (_playerBlob.CurrencyAmount.Value >= itemCost)
            {
                _playerBlob.CurrencyAmount.Value -= itemCost;

                if (!_playerBlob.InventoryItems.TryAdd(itemId, 1))
                {
                    _playerBlob.InventoryItems[itemId] += 1;
                }

                return true;
            }

            return false;
        }
        
    }
}

