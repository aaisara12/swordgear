#nullable enable

using System.Collections.Generic;
using Shop;

public class PlayerBlob : IItemPurchaser
{
    public Observable<int> WalletLedger { get; } = new Observable<int>(0);
    
    // TODO: Make this IReadOnlyObservableDictionary because we want to make sure users go through our API
    public ObservableDictionary<string, int> InventoryItems { get; } = new ObservableDictionary<string, int>();
    
    public void ReceiveItem(string itemId, int quantity)
    {
        if (!InventoryItems.TryAdd(itemId, 1))
        {
            InventoryItems[itemId] += 1;
        }
    }
}
