#nullable enable
namespace Shop
{
    public interface IItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }
    }
}