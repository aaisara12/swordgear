#nullable enable
using System.Collections.Generic;

namespace Shop
{
    /// <summary>
    /// Basically just a wrapper around an IStoreItem that provides necessary available item data to perform purchases.
    /// This allows the user of the class to not need to know about the available items.
    /// </summary>
    public class PurchasableItem
    {
        public IStoreItem StoreItemData { get; }
        
        private Dictionary<string, int> _availableItems;
        
        public PurchasableItem(IStoreItem storeItemData, Dictionary<string, int> availableItems)
        {
            StoreItemData = storeItemData;
            _availableItems = availableItems;
        }

        public bool IsItemInStock => PurchaseUtility.IsItemInStock(StoreItemData, _availableItems);
        public bool IsReadyToPurchase(IItemPurchaser purchaser) => PurchaseUtility.IsItemReadyToPurchase(StoreItemData, purchaser, _availableItems);
        public bool TryPurchaseItem(IItemPurchaser purchaser) => PurchaseUtility.TryPurchaseItem(StoreItemData, purchaser, _availableItems);
    }
}