#nullable enable
using System.Collections.Generic;

namespace Shop
{
    public interface IItemCatalog
    {
        public List<IItem> GetItems();

        public bool TryFindItemData(string itemId, out IItem itemData);
    }
}