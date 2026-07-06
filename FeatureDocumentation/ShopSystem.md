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
| `AugmentTierRollSettings` | `Assets/Aaron/ScriptableObjects/AugmentTierRollSettings.asset` |
| `AugmentTierRollWeights` | `Assets/Aaron/Scripts/ShopSystem/AugmentTierRollWeights.cs` |
| `AugmentTierVisuals` | `Assets/Aaron/Scripts/ShopSystem/AugmentTierVisuals.cs` |
| `AugmentShopElementViewModel` | `Assets/Aaron/Scripts/ShopSystem/AugmentShopElementViewModel.cs` |
| `UpgradeFlowController` | `Assets/Aaron/Scripts/Map/UpgradeFlowController.cs` |
| `InGameAugmentsManager` | `Assets/Aaron/Scripts/InGameAugmentsManager.cs` |

All shop classes live in the **`Shop` namespace**.

---

## Data Flow

```
LoadableStoreItemCatalog (ScriptableObject)
  └─ list of LoadableStoreItem (ScriptableObject per item)

InGameAugmentsManager.HandleTriggerAugmentGeneration()
  ├─ Rolls one offer tier (AugmentTierRollSettings + combo floor)
  ├─ Calls catalog.GetRandomItemsForExactTier(3, tier) — all three picks same tier
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

---

## Augment quality tiers

Each `LoadableStoreItem` has a **`qualityTier`** field (`AugmentQualityTier`: Low / Medium / High / Elite). In UI these map to **Bronze / Silver / Gold / Diamond**.

| Enum | Display | Typical roll weight (future) |
|---|---|---|
| `Low` | Bronze | Common |
| `Medium` | Silver | Uncommon |
| `High` | Gold | Rare |
| `Elite` | Diamond | Very rare |

`InGameAugmentsManager` reads **`AugmentTierRollSettings`** (default 50 / 20 / 20 / 10), rolls an offer tier, applies the combo floor from `ComboSystem.GetAugmentQualityTier()`, then offers three exact-tier augments.

Tune weights on **`AugmentTierRollSettings`** (`Assets/Aaron/ScriptableObjects/AugmentTierRollSettings.asset`).

**Stat boost content:** run **Henry → Generate Stat Boost Augments** to regenerate the default tiered set (Bronze ~10%, Silver ~30%, Gold ~50%, Diamond multi-stat). Element gear upgrades are Diamond tier.

Card shader effects use **`Time.unscaledTime`** so rim glow, flare, and sweep keep animating while `timeScale = 0` during the augment pick.

---

## Augment Tuner (editor)

| Section | Purpose |
|---|---|
| **Offer Tier Roll Weights** | Edit Bronze/Silver/Gold/Diamond weights on `AugmentTierRollSettings` |
| **Augment list** | Per-item name, cost, quality tier, stat boosts |
| **Wire CoreSystems Prefab** | Assign roll settings to production `InGameAugmentsManager` |

---

## Test scene debug panel

`AugmentPickerTest.unity` uses the shared roll settings asset. The in-scene panel only sets a **combo floor override** and refreshes offers — adjust tier percentages in **Augment Tuner**.

---

## Tier card visuals

Shader-driven card styling lives in **`AugmentTierVisuals`** and is applied by **`AugmentShopElementViewModel`** at runtime.

| Asset | Role |
|---|---|
| `Assets/Visuals/Shaders/AugmentTierCard.shader` | Gradient fill, rim glow, metallic noise, diamond sweep + sparkles |
| `Assets/Visuals/Shaders/AugmentTierCardFlare.shader` | Additive inner flare (bottom band, drifts horizontally) |
| `Assets/Visuals/Materials/AugmentTierCard.mat` | Template material for card background |
| `Assets/Visuals/Materials/AugmentTierCardFlare.mat` | Template material for inner flare layer |
| `Assets/Aaron/Prefabs/Shop/Augment Card.prefab` | `Main` (shader card), `Border`, `TierInnerFlare` child |

Per-tier colors and effect strengths are defined in `AugmentTierVisuals.GetCardStyle()`. Diamond adds light sweep and sparkles; all tiers share rim glow and inner flare with tier-tuned colors (silver uses muted flare colors to avoid white blowout).

### Editor tooling (Henry menu)

| Menu item | Purpose |
|---|---|
| **Henry → Augment Tuner** | Edit augment SOs: name, description, cost, **quality tier**, stat boosts; live tier color preview |
| **Henry → Setup Augment Card Visuals** | Wire card/flare materials on the Augment Card prefab |
| **Henry → Setup Augment Picker Test Scene** | Standalone scene with tier buttons + refresh for visual iteration |
| **Henry → Open Augment Picker Test Scene** | Opens the test scene |

After changing shaders or prefab layout, run **Setup Augment Card Visuals** before playtesting.

---

## Upgrade hub augment offer

The linear run loop (Combat×3 → Upgrade → Map) is handled by `RunManager` / `NodeStarter`. When the player enters an **Upgrade** step, **`UpgradeFlowController`** (on `Node Loop` in `Arena.unity`):

1. Waits for the additive `AugmentShop` scene to load.
2. Raises `ShowNextAugmentSetChannel` to roll three offers.
3. Pauses gameplay (`timeScale = 0`) while the picker is visible.
4. Resumes after the player chooses an augment.

Related fix: `ShopAnimation` uses `Time.unscaledDeltaTime` so the shop intro animates while paused.

---

## Test scene debug tier

See **Test scene debug panel** above for tier weight sliders and combo floor buttons.
