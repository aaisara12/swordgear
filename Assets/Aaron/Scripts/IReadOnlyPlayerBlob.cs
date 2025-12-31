#nullable enable

/// <summary>
/// Interface for read-only access to player data. Intended to prevent unauthorized modifications.
/// </summary>
public interface IReadOnlyPlayerBlob
{
    public IReadOnlyObservable<int> CurrencyAmount { get; }
    public IReadOnlyObservableDictionary<string, int> InventoryItems { get; }
}
