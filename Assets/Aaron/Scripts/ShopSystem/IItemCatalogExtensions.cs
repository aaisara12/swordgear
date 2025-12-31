#nullable enable
using Testing;

namespace Shop
{
    public static class IItemCatalogExtensions
    {
        // This is intended more for debugging purposes
        public static IStoreItem GetAnItem(this IItemCatalog catalog)
        {
            var items = catalog.GetItems();

            if (items.Count == 0)
            {
                return new TestStoreItem("debug-item", 99, "Din Don Dan",
                    "A mysterious item that appears out of nowhere. (couldn't find any real items in the catalog)");
            }
            var randomIndex = UnityEngine.Random.Range(0, items.Count);
            return items[randomIndex];
        }
    }
}