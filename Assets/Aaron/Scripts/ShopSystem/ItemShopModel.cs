using System.Collections.Generic;

namespace Shop
{
    public class ItemShopModel
    {
        public IReadOnlyList<PurchasableItem> Items { get; }
        public IItemPurchaser Purchaser { get; }
        
        public ItemShopModel(IReadOnlyList<PurchasableItem> items, IItemPurchaser purchaser)
        {
            Items = items;
            Purchaser = purchaser;
        }
    }
}