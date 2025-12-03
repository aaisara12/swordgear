#nullable enable
using System;
using Shop;

namespace Testing
{
    public class TestStoreItem : IStoreItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }

        public TestStoreItem(string id, int cost, string displayName = "")
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
        }
    }
}
