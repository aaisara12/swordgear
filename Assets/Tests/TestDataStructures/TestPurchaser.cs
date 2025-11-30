#nullable enable

using System.Collections.Generic;
using Shop;

public class TestPurchaser : IItemPurchaser
{
    public int WalletLedger { get; set; }
    public readonly Dictionary<string, int> Received = new Dictionary<string, int>();

    public void ReceiveItem(string id, int quantity)
    {
        if (Received.ContainsKey(id))
            Received[id] += quantity;
        else
            Received[id] = quantity;
    }
}
