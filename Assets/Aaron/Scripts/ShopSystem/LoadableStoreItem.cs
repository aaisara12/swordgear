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
        [SerializeField] private AugmentQualityTier qualityTier = AugmentQualityTier.Medium;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int Cost => cost;
        public Sprite? Icon => icon;
        public AugmentQualityTier QualityTier => qualityTier;

        /// <summary>
        /// Whether this item may be offered right now. Always true by default; an item that can run out
        /// (Light's "Learn a tune", once every tune is known) overrides it to leave the offer pool.
        /// </summary>
        public virtual bool IsOfferable => true;
    }
}
