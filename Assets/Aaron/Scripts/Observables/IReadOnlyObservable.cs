#nullable enable

public interface IReadOnlyObservable<T>
{
    public T Value { get; }
    public event System.Action<T>? OnValueChanged;
}
