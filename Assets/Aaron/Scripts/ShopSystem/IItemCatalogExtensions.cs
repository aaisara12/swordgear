#nullable enable
using System.Collections.Generic;
using Testing;

namespace Shop
{
    public static class IItemCatalogExtensions
    {
        // This is intended more for debugging purposes
        public static List<IStoreItem> GetRandomItems(this IItemCatalog catalog, int numberOfItems)
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

            for (int i = 0; i < numberOfItems; ++i)
            {
                if (indices.Count == 0) break;
                int j = UnityEngine.Random.Range(0, indices.Count);
                int itemIndex = indices[j];
                indices.RemoveAt(j);
                selection.Add(items[itemIndex]);
            }

            return selection;
        }

        /// <summary>
        /// Returns a random selection of items whose quality tier is at least the requested tier.
        /// Falls back to the full catalog if no such items exist.
        /// </summary>
        public static List<IStoreItem> GetRandomItemsForTier(this LoadableStoreItemCatalog catalog, int numberOfItems, AugmentQualityTier minimumTier)
        {
            var items = catalog.GetItems();

            // Filter by quality tier (only LoadableStoreItem instances have that data).
            var filtered = new List<IStoreItem>();
            foreach (var item in items)
            {
                if (item is LoadableStoreItem loadable && loadable.QualityTier >= minimumTier)
                {
                    filtered.Add(loadable);
                }
            }

            if (filtered.Count == 0)
            {
                // Fallback to the full list if no items meet the minimum tier.
                filtered.AddRange(items);
            }

            if (filtered.Count == 0)
            {
                // Let the original helper handle the empty case / debug item.
                return catalog.GetRandomItems(numberOfItems);
            }

            var indices = new List<int>();
            var selection = new List<IStoreItem>();
            for (int i = 0; i < filtered.Count; ++i)
            {
                indices.Add(i);
            }

            for (int i = 0; i < numberOfItems; ++i)
            {
                if (indices.Count == 0) break;
                int j = UnityEngine.Random.Range(0, indices.Count);
                int itemIndex = indices[j];
                indices.RemoveAt(j);
                selection.Add(filtered[itemIndex]);
            }

            return selection;
        }
    }
}