#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection, IReadOnlyDictionary<TKey, TValue>
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

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dict.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dict.Values;

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

    public bool TryGetValue(TKey key, out TValue? value) => _dict.TryGetValue(key, out value!);

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
    bool ICollection.IsSynchronized => (_dict as ICollection).IsSynchronized;
    object ICollection.SyncRoot => (_dict as ICollection).SyncRoot;
    void ICollection.CopyTo(Array array, int index) => (_dict as ICollection).CopyTo(array, index);
}
