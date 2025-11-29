#nullable enable

using Shop;

namespace Testing
{
    /// <summary>
    /// Very simple model for testing a single purchase
    /// </summary>
    public class OneItemPurchaser : IItemPurchaser
    {
        public int WalletLedger { get; set; } = int.MaxValue;
        public string ItemPurchased { get; private set; } = string.Empty;
        public int ItemPurchasedQuantity { get; private set; } = -1;

        public void ReceiveItem(string itemId, int quantity)
        {
            ItemPurchased = itemId;
            ItemPurchasedQuantity = quantity;
        }
    }
}