#nullable enable

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
        
        private readonly Dictionary<string, IStoreItem> _cachedItemData = new Dictionary<string, IStoreItem>();
        private readonly Dictionary<string, int> _availableItemStock = new Dictionary<string, int>();
        
        private readonly IItemCatalog _itemCatalog;

        public ItemStorefront(IItemCatalog itemCatalog)
        {
            _itemCatalog = itemCatalog;
        }

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

            return true;
        }

        public void ClearItems()
        {
            _cachedItemData.Clear();
            _availableItemStock.Clear();
        }
        
        public List<PurchasableItem> GetPurchasableItems()
        {
            var purchasableItems = new List<PurchasableItem>();

            foreach (var itemIdAndItem in _cachedItemData)
            {
                var purchasableItem = new PurchasableItem(itemIdAndItem.Value, _availableItemStock);
                
                purchasableItems.Add(purchasableItem);
            }
            
            return purchasableItems;
        }
    }
}

