#nullable enable

using System;
using UnityEngine;

namespace Shop
{
    public class ItemShopViewModel : MonoBehaviour
    {
        [SerializeField] private ItemShopModelEventChannelSO? itemShopModelProvider;

        [SerializeField] private ScrollView? scrollView;
        
        [SerializeField] private ItemShopElementViewModel? purchasableItemElementPrefab;
        
        [SerializeField] private ItemShopPurchaseDialogViewModel? confirmPurchaseViewModel;

        private ItemShopModel? _cachedModel;

        private IScrollViewController<ItemShopItemModel>? _scrollViewController;
        
        private void Awake()
        {
            if (itemShopModelProvider == null)
            {
                Debug.LogError("[ItemShopViewModel] ItemShopModelProvider is not assigned in the inspector. Cannot subscribe to model updates.");
                return;
            }

            if(purchasableItemElementPrefab == null || this.scrollView == null)
            {
                Debug.LogError("[ItemShopViewModel] Not all serialized fields are assigned in the inspector. Cannot initialize scrollview.");
                return;
            }
            
            if (scrollView.TryInitialize<ItemShopItemModel, ItemShopElementViewModel>(
                    purchasableItemElementPrefab, out var scrollViewController) == false)
            {
                Debug.LogError("[ItemShopViewModel] ScrollView has already been initialized!");
                return;
            }
            
            _scrollViewController = scrollViewController;

            itemShopModelProvider.OnDataChanged += Initialize;

            if (itemShopModelProvider.GetMostRecentData != null)
            {
                Initialize(itemShopModelProvider.GetMostRecentData);
            }
        }

        private void OnDestroy()
        {
            if (itemShopModelProvider != null)
            {
                itemShopModelProvider.OnDataChanged -= Initialize;
            }
        }

        public void Initialize(ItemShopModel model)
        {
            var purchasableItems = model.Items;

            if (_scrollViewController == null)
            {
                Debug.LogError("[ItemShopViewModel] ScrollView controller is not initialized! Cannot initialize.");
                return;
            }
            
            _scrollViewController.Clear();
            
            foreach (var item in purchasableItems)
            {
                _scrollViewController.AddElement(new ItemShopItemModel(item, model.Purchaser, this));
            }
            
            _cachedModel = model;
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
            
            var model = new ItemShopItemModel(item, _cachedModel.Purchaser, this);
            confirmPurchaseViewModel.gameObject.SetActive(true);
            confirmPurchaseViewModel.Initialize(model);
        }
    }
}