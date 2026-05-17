# Observables (Reactive State)

## Purpose
Provide reactive wrappers for values and dictionaries so that UI and other systems can subscribe to changes without polling or tight coupling.

---

## Key Scripts

| Script | Path |
|---|---|
| `Observable<T>` | `Assets/Aaron/Scripts/Observables/Observable.cs` |
| `IReadOnlyObservable<T>` | `Assets/Aaron/Scripts/Observables/IReadOnlyObservable.cs` |
| `ObservableDictionary<TKey, TValue>` | `Assets/Aaron/Scripts/Observables/ObservableDictionary.cs` |
| `IReadOnlyObservableDictionary<TKey, TValue>` | `Assets/Aaron/Scripts/Observables/IReadOnlyObservableDictionary.cs` |
| `ObservableDictionaryChangedEventArgs<TKey, TValue>` | `Assets/Aaron/Scripts/Observables/ObservableDictionaryChangedEventArgs.cs` |

---

## `Observable<T>`

A generic wrapper that fires `OnValueChanged` only when the value actually changes (uses `EqualityComparer<T>.Default`):

```csharp
var currency = new Observable<int>(0);
currency.OnValueChanged += newVal => UpdateCurrencyUI(newVal);
currency.Value = 100; // fires OnValueChanged
currency.Value = 100; // no-op, same value
```

Expose the read-only view via `IReadOnlyObservable<T>` to prevent external mutation:
```csharp
public IReadOnlyObservable<int> CurrencyAmount => _currencyAmount;
private Observable<int> _currencyAmount = new(0);
```

---

## `ObservableDictionary<TKey, TValue>`

Wraps a `Dictionary<TKey, TValue>` and fires `DictionaryChanged` (with `ObservableDictionaryChangedEventArgs`) on add, update, or remove.

Used by `PlayerBlob.InventoryItems` so `PlayerStatModifiers` can react to any inventory change without being given the `PlayerBlob` directly.

---

## When to Use

| Need | Use |
|---|---|
| Reactive single value + current value readable | `Observable<T>` |
| Reactive collection + current entries readable | `ObservableDictionary<K,V>` |
| Fire-and-forget event across scenes | [Event Channel](EventChannels.md) |

---

## Pattern: Read-Only Exposure

Always expose `Observable` and `ObservableDictionary` through their read-only interfaces to enforce encapsulation:

```csharp
// In PlayerBlob:
public IReadOnlyObservableDictionary<string, int> InventoryItems => inventoryItems;
private readonly ObservableDictionary<string, int> inventoryItems = new();
```
