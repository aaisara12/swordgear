#nullable enable
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Represents any item that can be displayed and sold in the shop
    /// </summary>
    public interface IStoreItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Cost { get; }
        public Sprite? Icon { get; }    // Nullable because we may not have a valid 
    }
}