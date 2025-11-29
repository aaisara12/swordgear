#nullable enable
namespace Shop
{
    public interface IStoreItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }
    }
}