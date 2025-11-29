#nullable enable

using UnityEngine;

namespace Shop
{
    public class Item : ScriptableObject
    {
        [SerializeField] private int cost;
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        
        public int Cost => cost;
        public string Id => id;
        public string DisplayName => DisplayName;
    }
}