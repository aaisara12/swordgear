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
        
        private Dictionary<string, int> availableItemStock;
        
        public PurchasableItem(IStoreItem storeItemData, Dictionary<string, int> availableItemStock)
        {
            StoreItemData = storeItemData;
            this.availableItemStock = availableItemStock;
        }

        public bool IsItemInStock => PurchaseUtility.IsItemInStock(StoreItemData, availableItemStock);
        public bool IsReadyToPurchase(IItemPurchaser purchaser) => PurchaseUtility.IsItemReadyToPurchase(StoreItemData, purchaser, availableItemStock);
        public bool TryPurchaseItem(IItemPurchaser purchaser) => PurchaseUtility.TryPurchaseItem(StoreItemData, purchaser, availableItemStock);
    }
}