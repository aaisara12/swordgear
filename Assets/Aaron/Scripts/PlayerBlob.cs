#nullable enable

using System.Collections.Generic;
using Shop;

public class PlayerBlob : IItemPurchaser
{
    public Observable<int> CurrencyAmount { get; } = new Observable<int>(0);
    public ObservableDictionary<string, int> InventoryItems { get; } = new ObservableDictionary<string, int>();
    
    public int WalletLedger { get => CurrencyAmount.Value; set => CurrencyAmount.Value = value; }
    
    public void ReceiveItem(string itemId, int quantity)
    {
        if (!InventoryItems.TryAdd(itemId, 1))
        {
            InventoryItems[itemId] += 1;
        }
    }
}
