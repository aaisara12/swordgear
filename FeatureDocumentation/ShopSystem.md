# Shop System

## Purpose
Presents the player with a selection of purchasable items (augments or stat boosts) between levels and processes purchases against the player's `PlayerBlob`.

---

## Key Scripts

| Script | Path |
|---|---|
| `IStoreItem` | `Assets/Aaron/Scripts/ShopSystem/IStoreItem.cs` |
| `IItemCatalog` | `Assets/Aaron/Scripts/ShopSystem/IItemCatalog.cs` |
| `IItemCatalogExtensions` | `Assets/Aaron/Scripts/ShopSystem/IItemCatalogExtensions.cs` |
| `LoadableStoreItem` | `Assets/Aaron/Scripts/ShopSystem/LoadableStoreItem.cs` |
| `LoadableStoreItemCatalog` | `Assets/Aaron/Scripts/ShopSystem/LoadableStoreItemCatalog.cs` |
| `StatBoostLoadableStoreItem` | `Assets/Aaron/Scripts/ShopSystem/StatBoostLoadableStoreItem.cs` |
| `ElementUpgradeLoadableStoreItem` | `Assets/Aaron/Scripts/ShopSystem/ElementUpgradeLoadableStoreItem.cs` |
| `ItemStorefront` | `Assets/Aaron/Scripts/ShopSystem/ItemStorefront.cs` |
| `PurchasableItem` | `Assets/Aaron/Scripts/ShopSystem/PurchasableItem.cs` |
| `PurchaseUtility` | `Assets/Aaron/Scripts/ShopSystem/PurchaseUtility.cs` |
| `IItemPurchaser` | `Assets/Aaron/Scripts/ShopSystem/IItemPurchaser.cs` |
| `ItemShopModel` | `Assets/Aaron/Scripts/ShopSystem/ItemShopModel.cs` |
| `ItemShopViewModel` | `Assets/Aaron/Scripts/ShopSystem/ItemShopViewModel.cs` |
| `AugmentShopViewModel` | `Assets/Aaron/Scripts/ShopSystem/AugmentShopViewModel.cs` |
| `ItemShopStateController` | `Assets/Aaron/Scripts/ShopSystem/ItemShopStateController.cs` |
| `InGameAugmentsManager` | `Assets/Aaron/Scripts/InGameAugmentsManager.cs` |

All shop classes live in the **`Shop` namespace**.

---

## Data Flow

```
LoadableStoreItemCatalog (ScriptableObject)
  └─ list of LoadableStoreItem (ScriptableObject per item)

InGameAugmentsManager.HandleTriggerAugmentGeneration()
  ├─ Gets AugmentQualityTier from ComboSystem
  ├─ Calls catalog.GetRandomItemsForTier(3, tier)
  ├─ Stocks ItemStorefront with selected items
  ├─ Calls itemStorefront.GetPurchasableItems()
  ├─ Wraps in ItemShopModel(items, playerBlob)
  └─ Raises ItemShopModelEventChannelSO → UI reads model
```

---

## Key Interfaces

| Interface | Role |
|---|---|
| `IStoreItem` | Read-only item data: `Id`, `DisplayName`, `Description`, `Cost`, `Icon` |
| `IItemCatalog` | Provides a list of `IStoreItem`s |
| `IItemPurchaser` | `WalletLedger` + `ReceiveItem(id, qty)` — implemented by `PlayerBlob` |

---

## Purchase Flow

`PurchaseUtility.TryPurchase(item, purchaser)`:
1. Checks `purchaser.WalletLedger >= item.Cost`.
2. Deducts the cost.
3. Calls `purchaser.ReceiveItem(item.Id, 1)`.
4. Returns success/failure.

Since `PlayerBlob` implements `IItemPurchaser`, purchases directly mutate the player's inventory, which in turn triggers `PlayerStatModifiers` to recalculate stats.

---

## Adding a New Item Type
1. Create a new `ScriptableObject` class extending `LoadableStoreItem`.
2. Override `ApplyEffect(PlayerBlob)` (or equivalent) to define what happens on purchase.
3. Create instances in the Unity editor and add them to a `LoadableStoreItemCatalog`.

---

## View-Model Layer

`ItemShopViewModel` and `AugmentShopViewModel` wrap `ItemShopModel` for UI binding. They expose `Observable` properties so the UI can react reactively without knowing about shop internals. See [Observables.md](Observables.md) for the reactive pattern.
