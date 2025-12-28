#nullable enable

using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Sets the ItemShop UI state based on inputs received from other game systems
    /// </summary>
    public class ItemShopStateController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private BoolEventChannelSO? uiVisibilityEventChannel;
        [SerializeField] private ItemShopModelEventChannelSO? uiDataEventChannel;

        [Header("Scene References")]
        [SerializeField] private ItemShopViewModel? viewModel;
        [SerializeField] private Transform? view;

        public void CloseShopUi()
        {
            // aisara => Either clean up viewmodel and view or neither to keep them in sync
            if (viewModel != null && view != null)
            {
                viewModel.CloseChildDialogs();
                view.gameObject.SetActive(false);
            }
        }
        
        private void Awake()
        {
            // TODO: aisara => Perhaps we can centralize this null logging logic somewhere
            if (uiVisibilityEventChannel == null)
            {
                Debug.LogError($"[{nameof(ItemShopViewModel)}] {nameof(uiVisibilityEventChannel)} is not assigned in the inspector.");
                return;
            }
            
            if (uiDataEventChannel == null)
            {
                Debug.LogError("[ItemShopViewModel] ItemShopModelProvider is not assigned in the inspector. Cannot subscribe to model updates.");
                return;
            }

            if (viewModel == null)
            {
                Debug.LogError("[ItemShopViewModel] ViewModel is not assigned in the inspector.");
                return;
            }

            if (view == null)
            {
                Debug.LogError("[ItemShopViewModel] View is not assigned in the inspector.");
                return;
            }
            
            view.gameObject.SetActive(false);
            
            uiVisibilityEventChannel.OnDataChanged += HandleUIVisibilityChanged;
            uiDataEventChannel.OnDataChanged += HandleItemShopModelChanged;
        }

        private void HandleItemShopModelChanged(ItemShopModel newModel)
        {
            if (viewModel == null)
            {
                return;
            }
            
            viewModel.Initialize(newModel);
        }

        private void HandleUIVisibilityChanged(bool isShopUiVisible)
        {
            view?.gameObject.SetActive(isShopUiVisible);
        }
        
        private void OnDestroy()
        {
            if (uiVisibilityEventChannel != null)
            {
                uiVisibilityEventChannel.OnDataChanged -= HandleUIVisibilityChanged;
            }
            
            if (uiDataEventChannel != null)
            {
                uiDataEventChannel.OnDataChanged -= HandleItemShopModelChanged;
            }
        }
    }
}