#nullable enable

using System.Collections.Generic;
using Shop;

/// <summary>
/// Represents the player's game state
/// </summary>
public class PlayerBlob : IItemPurchaser, IReadOnlyPlayerBlob
{
    public IReadOnlyObservable<int> CurrencyAmount => currencyAmount;
    private Observable<int> currencyAmount = new Observable<int>(0);
    public IReadOnlyObservableDictionary<string, int> InventoryItems => inventoryItems;
    private readonly ObservableDictionary<string, int> inventoryItems = new ObservableDictionary<string, int>();
    public int WalletLedger { get => currencyAmount.Value; set => currencyAmount.Value = value; }
    
    public void ReceiveItem(string itemId, int quantity)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;
        if (!inventoryItems.TryAdd(itemId, quantity))
            inventoryItems[itemId] += quantity;
    }

    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        return inventoryItems.TryGetValue(itemId, out int count) ? count : 0;
    }

    public bool TryRemoveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        return inventoryItems.Remove(itemId);
    }

    public void ClearInventory()
    {
        inventoryItems.Clear();
    }
}
