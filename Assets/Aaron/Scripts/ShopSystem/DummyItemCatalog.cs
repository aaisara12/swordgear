#nullable enable
using System.Collections.Generic;
using Shop;

namespace Testing
{
    public class DummyItemCatalog : IItemCatalog
    {
        private List<IItem> _items = new List<IItem>();
        public DummyItemCatalog(List<DummyItem> dummyItems)
        {
            foreach (var dummyItem in dummyItems)
            {
                _items.Add(dummyItem);
            }
        }
        
        public List<IItem> GetItems() => _items;
    }
}