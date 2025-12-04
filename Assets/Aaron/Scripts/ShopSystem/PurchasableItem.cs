#nullable enable
using System;

namespace Shop
{
    /// <summary>
    /// Basically just a wrapper around an IStoreItem that provides necessary available item data to perform purchases.
    /// This allows the user of the class to not need to know about the available items.
    /// </summary>
    public class PurchasableItem
    {
        public IStoreItem StoreItemData { get; }
        
        private ItemStorefront _itemStorefront;
        
        public PurchasableItem(IStoreItem storeItemData, ItemStorefront itemStorefront)
        {
            StoreItemData = storeItemData;
            _itemStorefront = itemStorefront;
        }

        public bool IsItemInStock => _itemStorefront.IsItemInStock(StoreItemData);

        public bool IsPurchaserAbleToBuy(IItemPurchaser purchaser) =>
            _itemStorefront.IsPurchaserAbleToBuyItem(purchaser, StoreItemData);
        public bool TryPurchaseItem(IItemPurchaser purchaser) => _itemStorefront.TryPurchaseItem(StoreItemData, purchaser);
    }
}