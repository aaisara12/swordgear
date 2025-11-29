#nullable enable

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
