#nullable enable

using System.Collections.Generic;
using System.Linq;

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
            return purchaser.WalletLedger.Value >= storeItem.Cost && IsItemInStock(storeItem, availableItemStock);
        }
        
        public static bool TryPurchaseItem(IStoreItem storeItem, IItemPurchaser purchaser, IDictionary<string, int> availableItemStock)
        {
            string itemId = storeItem.Id;
            int itemCost = storeItem.Cost;
            
            // IDictionary interestingly does not implement IReadOnlyDictionary (see https://softwareengineering.stackexchange.com/questions/446473/why-does-idictionary-not-implement-ireadonlydictionary)
            // Therefore, we're forced to convert the provided IDictionary to a regular Dictionary which DOES implement IReadOnlyDictionary
            var newDictionary = availableItemStock.ToDictionary(x => x.Key, x => x.Value);
            if (IsItemReadyToPurchase(storeItem, purchaser, newDictionary) == false)
            {
                return false;
            }
            
            purchaser.WalletLedger.Value -= itemCost;
            purchaser.ReceiveItem(itemId, 1);

            availableItemStock[itemId] -= 1;

            return true;
        }
    }
}