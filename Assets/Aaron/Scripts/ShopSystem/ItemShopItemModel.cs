namespace Shop
{
    public class ItemShopItemModel
    {
        public PurchasableItem Item { get; }
        public IItemPurchaser Purchaser { get; }
        public ItemShopViewModel ParentViewModel { get; }
        
        public ItemShopItemModel(PurchasableItem item, IItemPurchaser purchaser, ItemShopViewModel parentViewModel)
        {
            Item = item;
            Purchaser = purchaser;
            ParentViewModel = parentViewModel;
        }
    }
}