#nullable enable

using UnityEngine;
using UnityEngine.Serialization;

namespace Shop
{
    /// <summary>
    /// The view model supporting the UI for the overall shop window that a purchaser interacts with.
    /// </summary>
    public class GoldItemShopViewModel : ItemShopViewModel
    {
        [SerializeField] private ElementCollectionView? goldItemsView;
        
        [SerializeField] private GoldItemShopElementViewModel? purchasableItemElementPrefab;
        
        [SerializeField] private GoldItemShopPurchaseDialogViewModel? confirmPurchaseViewModel;
        [SerializeField] private TMPro.TMP_Text? purchaserWalletAmountText;

        private IElementCollectionViewController<GoldItemShopItemModel>? _scrollViewController;
        
        private ItemShopModel? _cachedModel;
        
        private void Awake()
        {
            if(purchasableItemElementPrefab == null || this.goldItemsView == null)
            {
                Debug.LogError("[ItemShopViewModel] Not all serialized fields are assigned in the inspector. Cannot initialize scrollview.");
                return;
            }
            
            if (goldItemsView.TryInitialize<GoldItemShopItemModel, GoldItemShopElementViewModel>(
                    purchasableItemElementPrefab, out var scrollViewController) == false)
            {
                Debug.LogError("[ItemShopViewModel] ScrollView has already been initialized!");
                return;
            }
            
            _scrollViewController = scrollViewController;
        }

        public override void Initialize(ItemShopModel model)
        {
            var purchasableItems = model.Items;

            if (_scrollViewController == null)
            {
                Debug.LogError("[ItemShopViewModel] ScrollView controller is not initialized! Cannot initialize.");
                return;
            }
            
            if (confirmPurchaseViewModel == null)
            {
                Debug.LogError("[ItemShopViewModel] Confirm purchase view model is not assigned in the inspector. We cannot complete the initialization sequence.");
                return;
            }

            if (purchaserWalletAmountText == null)
            {
                Debug.LogError("[ItemShopViewModel] Purchaser wallet text is not assigned in the inspector. We cannot complete the initialization sequence.");
                return;
            }
            
            _scrollViewController.Clear();
            
            foreach (var item in purchasableItems)
            {
                // For this particular UI implementation, we don't show the item at all if it's out of stock... just bc it's simpler
                if (item.IsItemInStock == false)
                {
                    continue;
                }
                
                _scrollViewController.AddElement(new GoldItemShopItemModel(item, model.Purchaser, this));
            }
            
            _cachedModel = model;
            
            confirmPurchaseViewModel.CloseDialog();

            purchaserWalletAmountText.text = model.Purchaser.WalletLedger.ToString();
        }
        
        public void ShowPurchaseDialogForItem(PurchasableItem item)
        {
            if (_cachedModel == null)
            {
                Debug.LogError("[ItemShopViewModel] Trying to show purchase dialog when view model is not initialized.");
                return;
            }
            
            if (confirmPurchaseViewModel == null)
            {
                Debug.LogError("[ItemShopViewModel] Confirm purchase view model is not assigned in the inspector. We cannot show the purchase dialog.");
                return;
            }
            
            var model = new GoldItemShopItemModel(item, _cachedModel.Purchaser, this);
            confirmPurchaseViewModel.Initialize(model);
            confirmPurchaseViewModel.OpenDialog();
        }

        /// <summary>
        /// Clean up any opened dialogs when the shop UI is closed.
        /// </summary>
        public void CloseChildDialogs()
        {
            if (confirmPurchaseViewModel != null)
            {
                confirmPurchaseViewModel.CloseDialog();
            }
        }
    }
}