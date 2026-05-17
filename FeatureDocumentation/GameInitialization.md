# Game Initialization & Player State

## Purpose
Bootstraps the game: loads the player's persistent state (`PlayerBlob`) from a ScriptableObject loader, then distributes it to all systems that need it at game start.

---

## Key Scripts

| Script | Path |
|---|---|
| `GameInitializer` | `Assets/Aaron/Scripts/GameInitializer.cs` |
| `PlayerBlob` | `Assets/Aaron/Scripts/PlayerBlob.cs` |
| `IReadOnlyPlayerBlob` | `Assets/Aaron/Scripts/IReadOnlyPlayerBlob.cs` |
| `PlayerBlobLoaderSO` | `Assets/Aaron/Scripts/PlayerBlobLoaderSO.cs` |
| `DummyPlayerBlobLoaderSO` | `Assets/Aaron/Scripts/DummyPlayerBlobLoaderSO.cs` |
| `InitializeableGameComponent` | `Assets/Aaron/Scripts/InitializeableGameComponent.cs` |
| `InitializeableUnrestrictedGameComponent` | `Assets/Aaron/Scripts/InitializeableUnrestrictedGameComponent.cs` |
| `InitializeableObject` | `Assets/Aaron/Scripts/InitializeableObject.cs` |

---

## Entry Point

`GameInitializer.Awake()` is the single entry point for game startup. It:
1. Calls `PlayerBlobLoaderSO.TryLoad()` to obtain (or create a fresh) `PlayerBlob`.
2. Iterates three lists of initializable recipients and calls their init method:
   - `List<InitializeableGameComponent>` → `InitializeOnGameStart(IReadOnlyPlayerBlob)`
   - `List<InitializeableUnrestrictedGameComponent>` → `InitializeOnGameStart_Dangerous(PlayerBlob)`
   - `List<InitializeableObject>` → `InitializeOnGameStart_Dangerous(PlayerBlob)`
3. Transitions to the start scene via `SceneTransitioner`.

---

## Player Blob

`PlayerBlob` holds the player's runtime game state:
- **`CurrencyAmount`** — `Observable<int>` exposed as `IReadOnlyObservable<int>`
- **`InventoryItems`** — `ObservableDictionary<string, int>` (item ID → quantity)

It implements:
- `IItemPurchaser` — so it can be passed directly to shop purchase flows
- `IReadOnlyPlayerBlob` — the read-only view passed to `InitializeableGameComponent`

---

## Initializable Hierarchy

| Base class | Receives | Use when |
|---|---|---|
| `InitializeableGameComponent` | `IReadOnlyPlayerBlob` | Component only needs to read player state |
| `InitializeableUnrestrictedGameComponent` | `PlayerBlob` (full) | Component needs to mutate player state (e.g., purchasing) |
| `InitializeableObject` | `PlayerBlob` (full) | Non-MonoBehaviour managers |

Register components in the `GameInitializer` Inspector lists so they are initialized at game start.
