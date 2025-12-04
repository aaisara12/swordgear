#nullable enable

namespace Shop
{
    public interface IItemPurchaser
    {
        public Observable<int> WalletLedger { get; }
        public void ReceiveItem(string itemId, int quantity);
    }
}