#nullable enable
using System;

namespace Shop
{
    /// <summary>
    /// Basically just a wrapper around an IStoreItem that provides necessary available item data to perform purchases.
    /// This allows the user of the class to not need to know about the available items.
    /// </summary>
    public class PurchasableItem : IDisposable
    {
        public IStoreItem StoreItemData { get; }
        
        private ObservableDictionary<string, int> availableItemStock;

        public event Action? OnItemStockUpdated;
        
        public PurchasableItem(IStoreItem storeItemData, ObservableDictionary<string, int> availableItemStock)
        {
            StoreItemData = storeItemData;
            this.availableItemStock = availableItemStock;
            this.availableItemStock.DictionaryChanged += HandleStockChanged;
        }

        public bool IsItemInStock => PurchaseUtility.IsItemInStock(StoreItemData, availableItemStock);
        public bool IsReadyToPurchase(IItemPurchaser purchaser) => PurchaseUtility.IsItemReadyToPurchase(StoreItemData, purchaser, availableItemStock);
        public bool TryPurchaseItem(IItemPurchaser purchaser) => PurchaseUtility.TryPurchaseItem(StoreItemData, purchaser, availableItemStock);

        public void Dispose()
        {
            availableItemStock.DictionaryChanged -= HandleStockChanged;
        }
        
        private void HandleStockChanged(ObservableDictionaryChangedEventArgs<string, int> obj)
        {
            if (obj.Key != StoreItemData.Id)
            {
                return;
            }
            
            OnItemStockUpdated?.Invoke();
        }
    }
}