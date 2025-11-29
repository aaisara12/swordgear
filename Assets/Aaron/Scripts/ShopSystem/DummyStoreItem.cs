#nullable enable

using Shop;

namespace Testing
{
    public class DummyStoreItem : IStoreItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }

        public DummyStoreItem()
        {
            Id = string.Empty;
            DisplayName = string.Empty;
            Cost = -1;
        }
        
        public DummyStoreItem(string id, string displayName, int cost)
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
        }
    }
}