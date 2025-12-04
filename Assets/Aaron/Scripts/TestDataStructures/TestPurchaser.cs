#nullable enable

using System.Collections.Generic;
using Shop;

namespace Testing
{
    public class TestPurchaser : IItemPurchaser
    {
        public Observable<int> WalletLedger { get; } = new Observable<int>(0);
        public readonly Dictionary<string, int> Received = new Dictionary<string, int>();

        public void ReceiveItem(string id, int quantity)
        {
            if (Received.ContainsKey(id))
                Received[id] += quantity;
            else
                Received[id] = quantity;
        }
    }
}

