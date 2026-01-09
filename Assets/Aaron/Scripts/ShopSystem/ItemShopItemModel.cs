namespace Shop
{
    public class ItemShopItemModel
    {
        public PurchasableItem Item { get; }
        public IItemPurchaser Purchaser { get; }
        public GoldItemShopViewModel ParentViewModel { get; }
        
        public ItemShopItemModel(PurchasableItem item, IItemPurchaser purchaser, GoldItemShopViewModel parentViewModel)
        {
            Item = item;
            Purchaser = purchaser;
            ParentViewModel = parentViewModel;
        }
    }
}