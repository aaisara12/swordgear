#nullable enable

using UnityEngine;

namespace Shop
{
    [CreateAssetMenu(fileName = "LoadableStoreItem", menuName = "Scriptable Objects/LoadableStoreItem")]
    public class LoadableStoreItem : ScriptableObject, IStoreItem
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private int cost;
        [SerializeField] private Sprite? icon;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int Cost => cost;
        public Sprite? Icon => icon;
    }
}
