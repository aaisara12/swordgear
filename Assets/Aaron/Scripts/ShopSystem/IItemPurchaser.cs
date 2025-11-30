#nullable enable

namespace Shop
{
    public interface IItemPurchaser
    {
        public int WalletLedger { get; set; }
        public void ReceiveItem(string itemId, int quantity);
    }
}