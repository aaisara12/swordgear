#nullable enable

using UnityEngine;

namespace Shop
{
    public class ItemShopViewModel : MonoBehaviour
    {
        [SerializeField] private ItemShopElementViewModel? purchasableItemElementPrefab;
        [SerializeField] private Transform? purchasableItemElementContainer;
        
        [SerializeField] private ItemShopPurchaseDialogViewModel? confirmPurchaseViewModel;

        private ItemShopModel? _cachedModel;
        
        public void Initialize(ItemShopModel model)
        {
            if(purchasableItemElementPrefab == null || purchasableItemElementContainer == null)
            {
                Debug.LogError("[ItemShopViewModel] Not all serialized fields are assigned in the inspector. Cannot initialize.");
                return;
            }
            
            var purchasableItems = model.Items;

            foreach (var item in purchasableItems)
            {
                var itemElement = Instantiate(purchasableItemElementPrefab, purchasableItemElementContainer);
                itemElement.Initialize(new ItemShopItemModel(item, model.Purchaser, this));
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