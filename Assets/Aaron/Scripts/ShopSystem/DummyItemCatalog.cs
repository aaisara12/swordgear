#nullable enable
using System.Collections.Generic;
using Shop;

namespace Testing
{
    public class DummyItemCatalog : IItemCatalog
    {
        private List<IItem> _items = new List<IItem>();
        private Dictionary<string, DummyItem> _itemDataById = new Dictionary<string, DummyItem>();
        
        public DummyItemCatalog(List<DummyItem> dummyItems)
        {
            foreach (var dummyItem in dummyItems)
            {
                _items.Add(dummyItem);
                _itemDataById.Add(dummyItem.Id, dummyItem);
            }
        }
        
        public List<IItem> GetItems() => _items;
        
        public bool TryFindItemData(string itemId, out IItem itemData)
        {
            if (_itemDataById.TryGetValue(itemId, out var data) == false)
            {
                itemData = new DummyItem();
                return false;
            }
            
            itemData = data;

            return true;
        }
    }
}