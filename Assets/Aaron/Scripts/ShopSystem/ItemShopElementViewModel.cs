#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ItemShopElementViewModel : MonoBehaviour, IScrollViewElementInitializable<ItemShopItemModel>
    {
        [SerializeField] private TMPro.TMP_Text? itemNameText;
        [SerializeField] private TMPro.TMP_Text? itemDescText;
        [SerializeField] private TMPro.TMP_Text? priceText;
        [SerializeField] private Image? itemIcon;
        
        private ItemShopItemModel? _cachedModel;

        public void InitializeElement(ItemShopItemModel model) => Initialize(model);
        
        public void Initialize(ItemShopItemModel model)
        {
            if(itemNameText == null || itemDescText == null || priceText == null || itemIcon == null)
            {
                Debug.LogError("[ItemShopElementViewModel] Not all serialized fields are assigned in the inspector.");
                return;
            }
            
            itemNameText.text = model.Item.StoreItemData.DisplayName;
            // TODO: itemDescText.text = model.Item.StoreItemData.Description;
            // TODO: itemIcon = model.Item.StoreItemData.Icon;
            priceText.text = model.Item.StoreItemData.Cost.ToString();
            
            _cachedModel = model;
        }
        
        public void OnItemElementClicked()
        {
            // NOTE: This can actually reasonably happen if the user clicks very fast before the UI is fully initialized.
            // Thus, we don't have to bother with an error log.
            if (_cachedModel == null) return;
            
            _cachedModel.ParentViewModel.ShowPurchaseDialogForItem(_cachedModel.Item);
        }
    }
}