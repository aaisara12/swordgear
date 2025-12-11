#nullable enable
using System.Collections.Generic;
using Shop;

namespace Testing
{
    public class TestItemCatalog : IItemCatalog
    {
        private List<IStoreItem> _items = new List<IStoreItem>();
        private Dictionary<string, IStoreItem> _itemDataById = new Dictionary<string, IStoreItem>();
        
        public TestItemCatalog(List<IStoreItem> dummyItems)
        {
            foreach (var dummyItem in dummyItems)
            {
                _items.Add(dummyItem);
                _itemDataById.Add(dummyItem.Id, dummyItem);
            }
        }
        
        public IReadOnlyList<IStoreItem> GetItems() => _items;
        
        public bool TryFindItemData(string itemId, out IStoreItem storeItemData)
        {
            if (_itemDataById.TryGetValue(itemId, out var data) == false)
            {
                storeItemData = new TestStoreItem(string.Empty, -1);
                return false;
            }
            
            storeItemData = data;

            return true;
        }
    }
}