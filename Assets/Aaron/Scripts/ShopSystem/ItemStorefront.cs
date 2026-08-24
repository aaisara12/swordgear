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
        // Avoid allocating additional memory on the heap in the stocking process to reduce fragmentation
        private struct NewItemStock
        {
            public IStoreItem ItemData;
            public int Quantity;
        }
        
        private readonly List<PurchasableItem> _cachedPurchasableItems = new List<PurchasableItem>();
        private readonly Dictionary<string, IStoreItem> _cachedItemData = new Dictionary<string, IStoreItem>();
        private readonly Dictionary<string, int> _availableItemStock = new Dictionary<string, int>();
        
        private readonly IItemCatalog _itemCatalog;

        public event Action? OnPurchasableItemsUpdated;
        
        public ItemStorefront(IItemCatalog itemCatalog)
        {
            _itemCatalog = itemCatalog;
        }

        // aisara => Pass in dictionary of item IDs and quantities to stock the storefront with instead of
        // a list of IStoreItems so that caller doesn't need to go through trouble of looking up item data themselves.
        public bool TryStockItems(IReadOnlyDictionary<string, int> items)
        {
            NewItemStock[] newItems = new NewItemStock[items.Count];

            int i = 0;
            foreach (var itemIdAndQuantity in items)
            {
                if (_itemCatalog.TryFindItemData(itemIdAndQuantity.Key, out var itemData) == false)
                {
                    return false;
                }
                
                newItems[i] = new NewItemStock { ItemData = itemData, Quantity = itemIdAndQuantity.Value };
                i++;
            }

            foreach (var newItem in newItems)
            {
                _cachedItemData.TryAdd(newItem.ItemData.Id, newItem.ItemData);
                _availableItemStock.TryAdd(newItem.ItemData.Id, 0);

                _availableItemStock[newItem.ItemData.Id] += newItem.Quantity;
            }

            RebuildPurchasableItems();

            return true;
        }

        /// <summary>
        /// Stocks concrete item instances rather than catalog IDs, for items that only exist at runtime: an augment
        /// offer stamps a rolled element onto a catalog asset, producing an item the catalog can't resolve by ID.
        /// Repeats of the same ID collapse into a single entry, matching the ID-keyed overload.
        /// </summary>
        public void StockItems(IReadOnlyList<IStoreItem> items, int quantityEach = 1)
        {
            foreach (IStoreItem item in items)
            {
                if (_cachedItemData.TryAdd(item.Id, item) == false)
                {
                    continue;
                }

                _availableItemStock[item.Id] = quantityEach;
            }

            RebuildPurchasableItems();
        }

        private void RebuildPurchasableItems()
        {
            _cachedPurchasableItems.Clear();

            foreach (var itemIdAndData in _cachedItemData)
            {
                var purchasableItem = new PurchasableItem(itemIdAndData.Value, this);
                _cachedPurchasableItems.Add(purchasableItem);
            }

            OnPurchasableItemsUpdated?.Invoke();
        }

        public void ClearItems()
        {
            _cachedItemData.Clear();
            _availableItemStock.Clear();
            _cachedPurchasableItems.Clear();

            OnPurchasableItemsUpdated?.Invoke();
        }
        
        public List<PurchasableItem> GetPurchasableItems()
        {
            return _cachedPurchasableItems;
        }

        public bool IsItemInStock(IStoreItem item)
        {
            return PurchaseUtility.IsItemInStock(item, _availableItemStock);
        }

        public bool IsPurchaserAbleToBuyItem(IItemPurchaser purchaser, IStoreItem item)
        {
            return PurchaseUtility.IsItemReadyToPurchase(item, purchaser, _availableItemStock);
        }

        public bool TryPurchaseItem(IStoreItem item, IItemPurchaser purchaser)
        {
            var wasPurchaseSuccessful = PurchaseUtility.TryPurchaseItem(item, purchaser, _availableItemStock);

            if (wasPurchaseSuccessful)
            {
                OnPurchasableItemsUpdated?.Invoke();
            }

            return wasPurchaseSuccessful;
        }
    }
}

