#nullable enable

using System.Collections.Generic;
using Shop;

public class PlayerBlob : IItemPurchaser, IReadOnlyPlayerBlob
{
    public IReadOnlyObservable<int> CurrencyAmount => currencyAmount;
    private Observable<int> currencyAmount = new Observable<int>(0);
    public IReadOnlyObservableDictionary<string, int> InventoryItems => inventoryItems;
    private readonly ObservableDictionary<string, int> inventoryItems = new ObservableDictionary<string, int>();
    public int WalletLedger { get => currencyAmount.Value; set => currencyAmount.Value = value; }
    
    public void ReceiveItem(string itemId, int quantity)
    {
        if (!inventoryItems.TryAdd(itemId, 1))
        {
            inventoryItems[itemId] += 1;
        }
    }
}
