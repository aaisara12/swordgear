using System.Collections.Generic;
using Testing;

namespace Shop
{
    public class TestItemShopModelInSceneGenerator : ItemShopModelInSceneGenerator
    {
        private TestPurchaser purchaser = new TestPurchaser(walletLedger: 40);
        private TestItemCatalog catalog = new TestItemCatalog(new List<IStoreItem>()
        {
            new TestStoreItem("itemB", 5, "Firebrand"),
            new TestStoreItem("itemA", 20, "Gear Slot"),
            new TestStoreItem("itemC", 35, "Ice Whip")
        });

        protected override IItemPurchaser GetPurchaser() => purchaser;
        protected override IItemCatalog GetCatalog() => catalog;
    }
}