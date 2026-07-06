#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class AugmentShopElementViewModel : MonoBehaviour, IScrollViewElementInitializable<AugmentShopItemModel>
    {
        [SerializeField] private TMPro.TMP_Text? itemNameText;
        [SerializeField] private TMPro.TMP_Text? itemDescText;
        [SerializeField] private Image? itemIcon;
        [SerializeField] private Image? cardBackground;
        [SerializeField] private Image? cardBorder;
        [SerializeField] private Image? cardAura;
        [SerializeField] private Image? cardInnerFlare;
        [SerializeField] private Material? tierCardMaterialTemplate;
        [SerializeField] private Material? tierAuraMaterialTemplate;
        [SerializeField] private Material? tierFlareMaterialTemplate;

        private AugmentShopItemModel? model;
        private AugmentQualityTier appliedTier = AugmentQualityTier.Low;
        private Material? tierCardMaterialInstance;
        private Material? tierAuraMaterialInstance;
        private Material? tierFlareMaterialInstance;
        private float animationTimeOffset;

        private void Awake()
        {
            animationTimeOffset = Random.Range(0f, 12f);
        }

        private void OnDestroy()
        {
            DestroyMaterial(ref tierCardMaterialInstance);
            DestroyMaterial(ref tierAuraMaterialInstance);
            DestroyMaterial(ref tierFlareMaterialInstance);
        }

        private static void DestroyMaterial(ref Material? material)
        {
            if (material != null)
            {
                Destroy(material);
                material = null;
            }
        }

        public void Initialize(AugmentShopItemModel model)
        {
            if(itemNameText == null || itemDescText == null || itemIcon == null)
            {
                Debug.LogError("[AugmentShopElementViewModel] Not all serialized fields are assigned in the inspector.");
                return;
            }

            itemNameText.text = model.DisplayName;
            itemDescText.text = model.Description;
            itemIcon.sprite = model.Icon;
            this.model = model;

            if (cardBackground != null)
            {
                ApplyTierVisuals(model.QualityTier);
            }
        }

        private void ApplyTierVisuals(AugmentQualityTier tier)
        {
            appliedTier = tier;

            if (cardBackground == null)
            {
                return;
            }

            if (tierCardMaterialTemplate != null)
            {
                tierCardMaterialInstance ??= Instantiate(tierCardMaterialTemplate);
                if (tierAuraMaterialTemplate != null)
                {
                    tierAuraMaterialInstance ??= Instantiate(tierAuraMaterialTemplate);
                }

                if (tierFlareMaterialTemplate != null)
                {
                    tierFlareMaterialInstance ??= Instantiate(tierFlareMaterialTemplate);
                }

                AugmentTierVisuals.ApplyCardStyle(
                    cardBackground,
                    cardBorder,
                    cardAura,
                    cardInnerFlare,
                    tier,
                    tierCardMaterialInstance,
                    tierAuraMaterialInstance,
                    tierFlareMaterialInstance,
                    animationTimeOffset);
                return;
            }

            cardBackground.color = AugmentTierVisuals.GetCardBackgroundColor(tier);
            if (cardBorder != null)
            {
                cardBorder.color = AugmentTierVisuals.GetCardStyle(tier).BorderColor;
            }

            if (cardAura != null)
            {
                cardAura.enabled = false;
            }

            if (cardInnerFlare != null)
            {
                cardInnerFlare.enabled = false;
            }
        }
        
        public void ChooseAugment()
        {
            if (model == null)
            {
                Debug.LogError("[AugmentShopElementViewModel] Augment model is not initialized! Cannot choose augment");
                return;
            }
            
            model.Choose();
        }

        public void RefreshTierVisuals()
        {
            if (cardBackground == null)
            {
                return;
            }

            AugmentQualityTier tier = model?.QualityTier ?? appliedTier;
            ApplyTierVisuals(tier);
        }
    }
}
