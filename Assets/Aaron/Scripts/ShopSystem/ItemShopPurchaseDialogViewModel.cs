#nullable enable

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Shop
{
    public class ItemShopPurchaseDialogViewModel : MonoBehaviour
    {
        [SerializeField] private ItemShopElementViewModel? itemVisualViewModel;
        [SerializeField] private Button? confirmPurchaseButton;
        
        // aisara => Expose event on data layer in case we want to hook in some animations in the editor
        [SerializeField] private UnityEvent? onSuccessfulPurchase;

        private ItemShopItemModel? _cachedModel;
        
        public void Initialize(ItemShopItemModel model)
        {
            if(itemVisualViewModel == null || confirmPurchaseButton == null)
            {
                Debug.LogError("[ItemShopPurchaseDialogViewModel] Not all serialized fields are assigned in the inspector.");
                return;
            }
            
            itemVisualViewModel.Initialize(model);

            confirmPurchaseButton.interactable = model.Item.IsPurchaserAbleToBuy(model.Purchaser);
            
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
                return;
            }
            
            onSuccessfulPurchase?.Invoke();
            gameObject.SetActive(false);
        }

        // In the future we may want to make this some kind of animation instead of just enabling/disabling
        public void OpenDialog()
        {
            if (_cachedModel == null)
            {
                Debug.LogError("[ItemShopPurchaseDialogViewModel] Attempting to open dialog before it's been initialized!");
                return;
            }
            
            gameObject.SetActive(true);
        }

        public void CloseDialog()
        {
            gameObject.SetActive(false);
        }
    }
}