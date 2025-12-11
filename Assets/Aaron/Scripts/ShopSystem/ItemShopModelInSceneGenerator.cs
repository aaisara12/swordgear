#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    public abstract class ItemShopModelInSceneGenerator : MonoBehaviour
    {
        [SerializeField] private ItemShopModelEventChannelSO? _eventChannel;

        // Replaced Dictionary with a serializable list so Unity can persist the entries
        [System.Serializable]
        private class InitialStockEntry
        {
            public string ItemId = string.Empty; // stores the IStoreItem.Id
            public int Quantity = 1;
        }

        [SerializeField] private List<InitialStockEntry> _initialStockEntries = new List<InitialStockEntry>();
        
        private ItemStorefront? _itemStorefront;
        
        protected abstract IItemPurchaser GetPurchaser();
        protected abstract IItemCatalog GetCatalog();

        private void Awake()
        {
            _eventChannel.ThrowIfNull(nameof(_eventChannel));

            var catalog = GetCatalog();

            // Convert the serialized list into a dictionary for the storefront
            var initialStock = new Dictionary<string, int>();
            foreach (var entry in _initialStockEntries)
            {
                if (string.IsNullOrEmpty(entry.ItemId))
                    continue;

                if (initialStock.ContainsKey(entry.ItemId))
                    initialStock[entry.ItemId] += Mathf.Max(0, entry.Quantity);
                else
                    initialStock[entry.ItemId] = Mathf.Max(0, entry.Quantity);
            }

            _itemStorefront = new ItemStorefront(catalog);

            if (_itemStorefront.TryStockItems(initialStock) == false)
            {
                Debug.LogError("Failed to stock items in ItemStorefront. This shouldn't happen because the initial stock is validated by the editor.");
                return;
            }
        
            var model = new ItemShopModel(_itemStorefront.GetPurchasableItems(), GetPurchaser());
        
            _eventChannel.RaiseDataChanged(model);

            _itemStorefront.OnPurchasableItemsUpdated += HandlePurchasableItemsUpdated;
        }

        private void HandlePurchasableItemsUpdated()
        {
            _eventChannel.ThrowIfNull(nameof(_eventChannel));
            _itemStorefront.ThrowIfNull(nameof(_itemStorefront));
        
            _eventChannel.RaiseDataChanged(new ItemShopModel(_itemStorefront.GetPurchasableItems(), GetPurchaser()));
        }

        private void OnDestroy()
        {
            if (_itemStorefront != null)
            {
                _itemStorefront.OnPurchasableItemsUpdated -= HandlePurchasableItemsUpdated;
            }
        }
    }
}