#nullable enable

using System.Collections.Generic;
using Testing;
using UnityEngine;

namespace Shop
{
    public class LoadableItemShopModelInSceneGenerator : ItemShopModelInSceneGenerator
    {
        [SerializeField] private LoadableStoreItemCatalog? catalog;

        private TestPurchaser defaultPurchaser = new TestPurchaser(150);
        
        protected override IItemPurchaser GetPurchaser()
        {
            // TODO: Be able to slot in different purchasers via the inspector
            return defaultPurchaser;
        }

        protected override IItemCatalog GetCatalog()
        {
            if (catalog == null)
            {
                return new TestItemCatalog(new List<IStoreItem>());
            }
            
            return catalog;
        }
    }
}