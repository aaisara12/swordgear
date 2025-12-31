#nullable enable
using System.Collections.Generic;

public class Observable<T> : IReadOnlyObservable<T>
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