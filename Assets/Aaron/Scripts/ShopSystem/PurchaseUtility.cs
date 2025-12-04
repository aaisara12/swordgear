#nullable enable

using System.Collections.Generic;

namespace Shop
{
    public static class PurchaseUtility
    {
        public static bool IsItemInStock(IStoreItem storeItem, IReadOnlyDictionary<string, int> availableItemStock)
        {
            int numberOfItemAvailable = availableItemStock.GetValueOrDefault(storeItem.Id, 0);
            
            return numberOfItemAvailable > 0;
        }
        
        public static bool IsItemReadyToPurchase(IStoreItem storeItem, IItemPurchaser purchaser, IReadOnlyDictionary<string, int> availableItemStock)
        {
            return purchaser.WalletLedger >= storeItem.Cost && IsItemInStock(storeItem, availableItemStock);
        }
        
        public static bool TryPurchaseItem(IStoreItem storeItem, IItemPurchaser purchaser, Dictionary<string, int> availableItemStock)
        {
            string itemId = storeItem.Id;
            int itemCost = storeItem.Cost;
            
            if (IsItemReadyToPurchase(storeItem, purchaser, availableItemStock) == false)
            {
                return false;
            }
            
            purchaser.WalletLedger -= itemCost;
            purchaser.ReceiveItem(itemId, 1);

            availableItemStock[itemId] -= 1;

            return true;
        }
    }
}