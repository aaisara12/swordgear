#nullable enable

public class PlayerBlob
{
    public Observable<int> CurrencyAmount { get; } = new Observable<int>(0);
    public ObservableDictionary<string, int> InventoryItems { get; } = new ObservableDictionary<string, int>();
}
