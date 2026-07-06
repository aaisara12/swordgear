#nullable enable
using System.Collections.Generic;
using Testing;
using UnityEngine;

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
                int j = Random.Range(0, indices.Count);
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
            return GetRandomItemsFromPool(catalog, numberOfItems, allowDuplicates: false, item =>
                item is LoadableStoreItem loadable && loadable.QualityTier >= minimumTier);
        }

        /// <summary>
        /// Returns a random selection of items that match the requested tier exactly.
        /// Never mixes in other tiers. Re-rolls with replacement if the tier pool is smaller than the request.
        /// </summary>
        public static List<IStoreItem> GetRandomItemsForExactTier(this LoadableStoreItemCatalog catalog, int numberOfItems, AugmentQualityTier tier)
        {
            if (numberOfItems <= 0)
            {
                return new List<IStoreItem>();
            }

            var tierPool = new List<IStoreItem>();
            foreach (IStoreItem item in catalog.GetItems())
            {
                if (item is LoadableStoreItem loadable && loadable.QualityTier == tier)
                {
                    tierPool.Add(item);
                }
            }

            if (tierPool.Count == 0)
            {
                Debug.LogWarning(
                    $"[ItemCatalog] No augments found for tier {tier}. Offer will be empty.");
                return new List<IStoreItem>();
            }

            var selection = new List<IStoreItem>(numberOfItems);
            var uniqueIndices = new List<int>(tierPool.Count);
            for (int i = 0; i < tierPool.Count; ++i)
            {
                uniqueIndices.Add(i);
            }

            int uniqueCount = Mathf.Min(numberOfItems, tierPool.Count);
            for (int i = 0; i < uniqueCount; ++i)
            {
                int pick = Random.Range(0, uniqueIndices.Count);
                int poolIndex = uniqueIndices[pick];
                uniqueIndices.RemoveAt(pick);
                selection.Add(tierPool[poolIndex]);
            }

            for (int i = uniqueCount; i < numberOfItems; ++i)
            {
                selection.Add(tierPool[Random.Range(0, tierPool.Count)]);
            }

            return selection;
        }

        private static List<IStoreItem> GetRandomItemsFromPool(
            LoadableStoreItemCatalog catalog,
            int numberOfItems,
            bool allowDuplicates,
            System.Func<IStoreItem, bool> predicate)
        {
            var items = catalog.GetItems();

            var filtered = new List<IStoreItem>();
            foreach (var item in items)
            {
                if (predicate(item))
                {
                    filtered.Add(item);
                }
            }

            if (filtered.Count == 0)
            {
                filtered.AddRange(items);
            }

            if (filtered.Count == 0)
            {
                return catalog.GetRandomItems(numberOfItems);
            }

            if (allowDuplicates)
            {
                var withReplacement = new List<IStoreItem>(numberOfItems);
                for (int i = 0; i < numberOfItems; ++i)
                {
                    withReplacement.Add(filtered[Random.Range(0, filtered.Count)]);
                }

                return withReplacement;
            }

            var indices = new List<int>();
            var selection = new List<IStoreItem>();
            for (int i = 0; i < filtered.Count; ++i)
            {
                indices.Add(i);
            }

            for (int i = 0; i < numberOfItems; ++i)
            {
                if (indices.Count == 0)
                {
                    break;
                }

                int j = Random.Range(0, indices.Count);
                int itemIndex = indices[j];
                indices.RemoveAt(j);
                selection.Add(filtered[itemIndex]);
            }

            return selection;
        }
    }
}
