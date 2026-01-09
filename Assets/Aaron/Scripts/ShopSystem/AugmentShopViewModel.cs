#nullable enable

using UnityEngine;

namespace Shop
{
    public class AugmentShopViewModel : ItemShopViewModel
    {
        [SerializeField] private ElementCollectionView? augmentsView;
        
        private void Awake()
        {
            if(augmentsView == null)
            {
                Debug.LogError("[AugmentShopViewModel] Augments view is not assigned in the inspector.");
                return;
            }
        }
        
        public override void Initialize(ItemShopModel model)
        {
            if (augmentsView == null)
            {
                return;
            }
            
            
        }
    }
}