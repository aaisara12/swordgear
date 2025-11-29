#nullable enable

using Shop;

namespace Testing
{
    public class DummyItem : IItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }
        
        public DummyItem(string id, string displayName, int cost)
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
        }
    }
}