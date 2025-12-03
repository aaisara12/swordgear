#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ItemShopPurchaseDialogViewModel : MonoBehaviour
    {
        [SerializeField] private ItemShopElementViewModel? itemVisualViewModel;
        [SerializeField] private Button? confirmPurchaseButton;

        private ItemShopItemModel? _cachedModel;
        
        public void Initialize(ItemShopItemModel model)
        {
            if(itemVisualViewModel == null || confirmPurchaseButton == null)
            {
                Debug.LogError("[ItemShopPurchaseDialogViewModel] Not all serialized fields are assigned in the inspector.");
                return;
            }
            
            itemVisualViewModel.Initialize(model);

            confirmPurchaseButton.interactable = model.Item.IsReadyToPurchase(model.Purchaser);
            
            _cachedModel = model;
        }
        
        public void OnConfirmPurchaseButtonClicked()
        {
            // NOTE: This can actually reasonably happen if the user clicks very fast before the UI is fully initialized.
            // Thus, we don't have to bother with an error log.
            if (_cachedModel == null) return;

            var isPurchaseSuccessful = _cachedModel.Item.TryPurchaseItem(_cachedModel.Purchaser);

            if (isPurchaseSuccessful == false)
            {
                // TODO: We can show some UI feedback to the user here.
                Debug.Log("[ItemShopPurchaseDialogViewModel] Purchase failed. Perhaps the user is clicking too fast?");
            }
        }
    }
}