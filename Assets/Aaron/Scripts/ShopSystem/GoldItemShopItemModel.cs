namespace Shop
{
    public class GoldItemShopItemModel
    {
        public PurchasableItem Item { get; }
        public IItemPurchaser Purchaser { get; }
        public GoldItemShopViewModel ParentViewModel { get; }
        
        public GoldItemShopItemModel(PurchasableItem item, IItemPurchaser purchaser, GoldItemShopViewModel parentViewModel)
        {
            Item = item;
            Purchaser = purchaser;
            ParentViewModel = parentViewModel;
        }
    }
}