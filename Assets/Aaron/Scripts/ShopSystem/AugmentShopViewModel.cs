#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Shop
{
    /// <summary>
    /// Really, represents a shop that gives away items for free - It's kind of like we're a shop where we cover the cost of the item ourselves.
    /// </summary>
    // TODO: aisara => Need to rethink this class - augments are not purchased, they are chosen, should they even be purchasable items?
    public class AugmentShopViewModel : ItemShopViewModel
    {
        [SerializeField] private AugmentShopElementViewModel? augmentViewModelPrefab;
        [SerializeField] private ElementCollectionView? augmentsView;
        [SerializeField] private UnityEvent<int> onAugmentChosen = new UnityEvent<int>();

        private IElementCollectionViewController<AugmentShopItemModel>? augmentsCollectionController;

        private IItemPurchaser? purchaser;
        private List<PurchasableItem> augments = new List<PurchasableItem>();
        
        private void Awake()
        {
            if(augmentsView == null)
            {
                Debug.LogError("[AugmentShopViewModel] Augments view is not assigned in the inspector.");
                return;
            }

            if (augmentViewModelPrefab == null)
            {
                Debug.LogError("[AugmentShopViewModel] Augment view model prefab is not assigned in the inspector.");
                return;
            }
            
            if (augmentsView.TryInitialize(augmentViewModelPrefab, out augmentsCollectionController) == false)
            {
                Debug.LogError("[AugmentShopViewModel] Augments view failed to initialize.");
                return;
            }
        }
        
        public override void Initialize(ItemShopModel model)
        {
            if (augmentsCollectionController == null)
            {
                return;
            }
            
            purchaser = model.Purchaser;
            
            augments.Clear();
            augmentsCollectionController.Clear();

            for (var i = 0; i < model.Items.Count; i++)
            {
                // TODO: aisara => gotta rethink the names here - it's an augment
                var item = model.Items[i];
                
                augments.Add(item);
                
                var storeItemData = item.StoreItemData;
                
                var augmentItemModel = new AugmentShopItemModel(i, storeItemData.DisplayName, storeItemData.Description, storeItemData.Icon, this);
                augmentsCollectionController.AddElement(augmentItemModel);
            }
        }

        public override void CleanUp()
        {
            // TODO: aisara => Anything to do here?
        }

        public void ChooseAugment(int augmentIndex)
        {
            if (augmentIndex < 0 || augmentIndex >= augments.Count)
            {
                Debug.LogError("[AugmentShopViewModel] Augment index is out of range.");
                return;
            }

            if (purchaser == null)
            {
                Debug.LogError("[AugmentShopViewModel] Purchaser is not assigned. Cannot choose augment.");
                return;
            }
            
            var chosenAugment = augments[augmentIndex];

            // aisara => We're covering the cost of the augment ourselves (we're giving it out for free)
            purchaser.WalletLedger += chosenAugment.StoreItemData.Cost;

            if(chosenAugment.TryPurchaseItem(purchaser) == false)
            {
                Debug.LogError("[AugmentShopViewModel] Failed to choose augment.");
                return;
            }
            
            onAugmentChosen.Invoke(augmentIndex);
        }
    }
}