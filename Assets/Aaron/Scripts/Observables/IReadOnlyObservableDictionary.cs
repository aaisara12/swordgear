#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

public interface IReadOnlyObservableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, ICollection
{
    public event Action<ObservableDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;
}
