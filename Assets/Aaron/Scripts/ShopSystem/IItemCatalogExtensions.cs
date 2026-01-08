#nullable enable
using System.Collections.Generic;
using Testing;

namespace Shop
{
    public static class IItemCatalogExtensions
    {
        // This is intended more for debugging purposes
        public static List<IStoreItem> GetAnItem(this IItemCatalog catalog, int amt = 1)
        {
            var items = catalog.GetItems();

            if (items.Count == 0)
            {
                return new List<IStoreItem>{ new TestStoreItem("debug-item", 99, "Din Don Dan",
                    "A mysterious item that appears out of nowhere. (couldn't find any real items in the catalog)") };
            }

            var indices = new List<int>();
            var selection = new List<IStoreItem>();
            for (int i = 0; i < items.Count; ++i)
            {
                indices.Add(i);
            }

            for (int i = 0; i < amt; ++i)
            {
                if (indices.Count == 0) break;
                int j = UnityEngine.Random.Range(0, indices.Count);
                int itemIndex = indices[j];
                indices.RemoveAt(j);
                selection.Add(items[itemIndex]);
            }

            return selection;
        }
    }
}