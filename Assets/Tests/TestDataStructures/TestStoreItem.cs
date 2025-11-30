#nullable enable
using Shop;

public class TestStoreItem : IStoreItem
{
    public string Id { get; }
    public string DisplayName => string.Empty;
    public int Cost { get; }

    public TestStoreItem(string id, int cost)
    {
        Id = id;
        Cost = cost;
    }
}
