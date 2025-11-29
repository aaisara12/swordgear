#nullable enable
using System.Collections.Generic;
using Shop;

namespace Testing
{
    public class DummyItemCatalog : IItemCatalog
    {
        private List<IStoreItem> _items = new List<IStoreItem>();
        private Dictionary<string, DummyStoreItem> _itemDataById = new Dictionary<string, DummyStoreItem>();
        
        public DummyItemCatalog(List<DummyStoreItem> dummyItems)
        {
            foreach (var dummyItem in dummyItems)
            {
                _items.Add(dummyItem);
                _itemDataById.Add(dummyItem.Id, dummyItem);
            }
        }
        
        public List<IStoreItem> GetItems() => _items;
        
        public bool TryFindItemData(string itemId, out IStoreItem storeItemData)
        {
            if (_itemDataById.TryGetValue(itemId, out var data) == false)
            {
                storeItemData = new DummyStoreItem();
                return false;
            }
            
            storeItemData = data;

            return true;
        }
    }
}