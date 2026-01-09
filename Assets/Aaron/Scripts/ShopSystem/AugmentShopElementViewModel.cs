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

        private AugmentShopItemModel? model;
        
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
    }
}