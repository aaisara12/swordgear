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

            if (_cachedModel != null)
            {
                _cachedModel.Item.OnItemStockUpdated -= HandleItemStockUpdated;
                _cachedModel.Purchaser.WalletLedger.OnValueChanged -= HandleWalletUpdated;
            }

            model.Item.OnItemStockUpdated += HandleItemStockUpdated;
            model.Purchaser.WalletLedger.OnValueChanged += HandleWalletUpdated;
            
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
        
        private void HandleWalletUpdated(int newWalletVal)
        {
            if (_cachedModel == null)
            {
                return;
            }
            
            if (confirmPurchaseButton == null)
            {
                return;
            }
            
            confirmPurchaseButton.interactable = _cachedModel.Item.IsReadyToPurchase(_cachedModel.Purchaser);
        }

        private void HandleItemStockUpdated()
        {
            if (_cachedModel == null)
            {
                return;
            }

            if (confirmPurchaseButton == null)
            {
                return;
            }
            
            confirmPurchaseButton.interactable = _cachedModel.Item.IsReadyToPurchase(_cachedModel.Purchaser);
        }
    }
}