#nullable enable
using System.Collections.Generic;

namespace Shop
{
    public interface IItemCatalog
    {
        public IReadOnlyList<IStoreItem> GetItems();

        public bool TryFindItemData(string itemId, out IStoreItem storeItemData);
    }
}