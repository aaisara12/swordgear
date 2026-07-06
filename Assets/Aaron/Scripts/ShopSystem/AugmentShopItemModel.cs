#nullable enable

using UnityEngine;

namespace Shop
{
    public class AugmentShopItemModel
    {
        public string DisplayName { get; }
        public string Description { get; }
        
        public Sprite? Icon { get; }
        public AugmentQualityTier QualityTier { get; }
        
        private int augmentIndex;
        private AugmentShopViewModel parent;

        public AugmentShopItemModel(
            int augmentIndex,
            string displayName,
            string description,
            Sprite? icon,
            AugmentQualityTier qualityTier,
            AugmentShopViewModel parent)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            QualityTier = qualityTier;
            
            this.augmentIndex = augmentIndex;
            this.parent = parent;
        }

        public void Choose()
        {
            parent.ChooseAugment(augmentIndex);
        }
    }
}