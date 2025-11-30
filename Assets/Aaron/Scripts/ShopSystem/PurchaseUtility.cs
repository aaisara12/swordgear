#nullable enable

using System.Collections.Generic;

namespace Shop
{
    public static class PurchaseUtility
    {
        public static bool IsItemInStock(IStoreItem storeItem, IReadOnlyDictionary<string, int> availableItems)
        {
            int numberOfItemAvailable = availableItems.GetValueOrDefault(storeItem.Id, 0);
            
            return numberOfItemAvailable > 0;
        }
        
        public static bool IsItemReadyToPurchase(IStoreItem storeItem, IItemPurchaser purchaser, IReadOnlyDictionary<string, int> availableItems)
        {
            return purchaser.WalletLedger >= storeItem.Cost && IsItemInStock(storeItem, availableItems);
        }
        
        public static bool TryPurchaseItem(IStoreItem storeItem, IItemPurchaser purchaser, Dictionary<string, int> availableItems)
        {
            string itemId = storeItem.Id;
            int itemCost = storeItem.Cost;
            
            if (IsItemReadyToPurchase(storeItem, purchaser, availableItems) == false)
            {
                return false;
            }
            
            purchaser.WalletLedger -= itemCost;
            purchaser.ReceiveItem(itemId, 1);

            availableItems[itemId] -= 1;

            return true;
        }
    }
}