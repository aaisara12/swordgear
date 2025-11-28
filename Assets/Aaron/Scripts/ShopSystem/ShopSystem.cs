#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AaronPrototype.ShopSystem
{
    public class ShopSystem
    {
        // intent is to create a very restricted interface that the UI can use to display purchasable items
        public class PurchasableItem
        {
            public string Name { get; }
            public int Cost { get; }
            public bool IsPurchasable { get; }

            private Func<bool> _tryPurchaseItemMethod;
            
            public PurchasableItem(string name, int cost, bool isPurchasable, Func<bool> tryPurchaseItemMethod)
            {
                Name = name;
                Cost = cost;
                IsPurchasable = isPurchasable;
                _tryPurchaseItemMethod = tryPurchaseItemMethod;
            }

            public bool TryPurchaseItem() => _tryPurchaseItemMethod();
        }
        
        private PlayerBlob _playerBlob;
        private IItemCatalog _itemCatalog;

        public ShopSystem(PlayerBlob playerBlob, IItemCatalog itemCatalog)
        {
            _playerBlob = playerBlob;
            _itemCatalog = itemCatalog;
        }
        
        public List<PurchasableItem> GetPurchasableItems()
        {
            var items = _itemCatalog.GetItems();
            
            var purchasableItems = new List<PurchasableItem>();

            foreach (var item in items)
            {
                var purchasableItem = new PurchasableItem(
                    item.DisplayName,
                    item.Cost,
                    _playerBlob.CurrencyAmount.Value >= item.Cost,
                    () => TryPurchaseItem(item.Id, item.Cost)
                );
                
                purchasableItems.Add(purchasableItem);
            }
            
            return purchasableItems;
        }
        
        private bool TryPurchaseItem(string itemId, int itemCost)
        {
            if (_playerBlob.CurrencyAmount.Value >= itemCost)
            {
                _playerBlob.CurrencyAmount.Value -= itemCost;

                if (!_playerBlob.PurchasedItems.TryAdd(itemId, 1))
                {
                    _playerBlob.PurchasedItems[itemId] += 1;
                }

                return true;
            }

            return false;
        }
        
    }

    public interface IItemCatalog
    {
        public List<IItem> GetItems();
    }

    // interface because we may want to instantiate dummy items in unit tests using a dummy class
    public interface IItem
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }
    }
    
    public class Item : ScriptableObject
    {
        [SerializeField] private int cost;
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "";
        
        public int Cost => cost;
        public string Id => id;
        public string DisplayName => DisplayName;
    }

    public class PlayerBlob
    {
        public Observable<int> CurrencyAmount { get; } = new Observable<int>(0);
        public ObservableDictionary<string, int> PurchasedItems { get; } = new ObservableDictionary<string, int>();
    }
    
    public interface IPlayerBlobLoader
    {
        public bool TryLoadPlayerBlob(out PlayerBlob blob);
    }



    public class Observable<T>
    {
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(_value);
                }
            }
        }

        public event System.Action<T>? OnValueChanged;

        public Observable(T initialValue)
        {
            _value = initialValue;
        }
    }
    
    public sealed class ObservableDictionaryChangedEventArgs<TKey, TValue>
    {
        public enum ChangeType
        {
            Add,
            Remove,
            Replace,
            Clear
        }

        public ChangeType Action { get; }
        public TKey? Key { get; }
        public TValue? OldValue { get; }
        public TValue? NewValue { get; }

        public ObservableDictionaryChangedEventArgs(ChangeType action, TKey? key = default, TValue? oldValue = default, TValue? newValue = default)
        {
            Action = action;
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dict;

        public event Action<ObservableDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

        public ObservableDictionary() : this(null) { }
        public ObservableDictionary(IEqualityComparer<TKey>? comparer) => _dict = new Dictionary<TKey, TValue>(comparer);

        public TValue this[TKey key]
        {
            get => _dict[key];
            set
            {
                if (_dict.TryGetValue(key, out var old))
                {
                    _dict[key] = value;
                    DictionaryChanged?.Invoke(new ObservableDictionaryChangedEventArgs<TKey, TValue>(ObservableDictionaryChangedEventArgs<TKey, TValue>.ChangeType.Replace, key, old, value));
                }
                else
                {
                    _dict[key] = value;
                    DictionaryChanged?.Invoke(new ObservableDictionaryChangedEventArgs<TKey, TValue>(ObservableDictionaryChangedEventArgs<TKey, TValue>.ChangeType.Add, key, default, value));
                }
            }
        }

        public ICollection<TKey> Keys => _dict.Keys;
        public ICollection<TValue> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            _dict.Add(key, value);
            DictionaryChanged?.Invoke(new ObservableDictionaryChangedEventArgs<TKey, TValue>(ObservableDictionaryChangedEventArgs<TKey, TValue>.ChangeType.Add, key, default, value));
        }

        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

        public bool Remove(TKey key)
        {
            if (_dict.TryGetValue(key, out var old))
            {
                var removed = _dict.Remove(key);
                if (removed)
                    DictionaryChanged?.Invoke(new ObservableDictionaryChangedEventArgs<TKey, TValue>(ObservableDictionaryChangedEventArgs<TKey, TValue>.ChangeType.Remove, key, old, default));
                return removed;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value!);

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            if (_dict.Count == 0) return;
            _dict.Clear();
            DictionaryChanged?.Invoke(new ObservableDictionaryChangedEventArgs<TKey, TValue>(ObservableDictionaryChangedEventArgs<TKey, TValue>.ChangeType.Clear));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, item.Value);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
                return Remove(item.Key);
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

