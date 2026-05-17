# Event Channels (ScriptableObject Messaging)

## Purpose
Decouple systems and cross-scene objects using ScriptableObject-based event channels. Senders raise events on a shared ScriptableObject asset; listeners subscribe to the same asset. Neither side holds a direct reference to the other.

---

## Key Scripts

| Script | Path |
|---|---|
| `DataEventChannelSO<T>` | `Assets/Aaron/Scripts/DataEventChannel/DataEventChannelSO.cs` |
| `TriggerEventChannelSO` | `Assets/Aaron/Scripts/DataEventChannel/TriggerEventChannelSO.cs` |
| `BoolEventChannelSO` | `Assets/Aaron/Scripts/DataEventChannel/BoolEventChannelSO.cs` |
| `TransformEventChannelSO` | `Assets/Aaron/Scripts/DataEventChannel/TransformEventChannelSO.cs` |
| `StringEventChannelSO` | `Assets/Aaron/Scripts/DataEventChannel/StringEventChannelSO.cs` |
| `ItemShopModelEventChannelSO` | `Assets/Aaron/Scripts/DataEventChannel/ItemShopModelEventChannelSO.cs` |
| `BoolEventChannelListener` | `Assets/Aaron/Scripts/DataEventChannel/BoolEventChannelListener.cs` |
| `BoolEventChannelTester` | `Assets/Aaron/Scripts/DataEventChannel/BoolEventChannelTester.cs` |

---

## Base Class

```csharp
public class DataEventChannelSO<T> : ScriptableObject
{
    public event Action<T>? OnDataChanged;
    public void RaiseDataChanged(T newData) => OnDataChanged?.Invoke(newData);
}
```

Concrete types just inherit with a fixed generic parameter:
```csharp
[CreateAssetMenu] public class BoolEventChannelSO : DataEventChannelSO<bool> { }
```

`TriggerEventChannelSO` is a parameterless variant (no data payload).

---

## Usage Pattern

**Sender** (raises the event):
```csharp
[SerializeField] private BoolEventChannelSO? visibilityChannel;
// ...
visibilityChannel.RaiseDataChanged(true);
```

**Receiver** (subscribes in Awake / OnDestroy):
```csharp
[SerializeField] private BoolEventChannelSO? visibilityChannel;

private void Awake() => visibilityChannel.OnDataChanged += HandleVisibilityChanged;
private void OnDestroy() => visibilityChannel.OnDataChanged -= HandleVisibilityChanged;

private void HandleVisibilityChanged(bool visible) { ... }
```

Both reference the **same ScriptableObject asset** assigned in the Inspector.

---

## When to Use

- Cross-scene communication (objects in different scenes/prefabs that can't hold direct references)
- Decoupling UI from game logic
- Triggering one-time actions (use `TriggerEventChannelSO`)

For in-scene reactive state that needs a current value (not just fire-and-forget), prefer [Observables](Observables.md).

---

## Editor Tools

`BoolEventChannelTesterEditor` adds an Inspector button to manually raise the event in Play Mode for testing purposes.
