#nullable enable
using System;
using Shop;
using UnityEngine;

namespace Testing
{
    public class TestStoreItem : IStoreItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Cost { get; }
        public Sprite? Icon { get; }

        public TestStoreItem(string id, int cost, string? displayName = null, string? description = null, Sprite? icon = null)
        {
            Id = id;
            DisplayName = displayName ?? $"Test-{id}";
            Description = description ?? $"This is the description text for {DisplayName}.";
            Cost = cost;
            Icon = icon;
        }
    }
}
