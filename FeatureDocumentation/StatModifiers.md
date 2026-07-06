# Stat Modifiers & Augment Stacking

## Purpose
`PlayerStatModifiers` is the single runtime source of truth for all stat bonuses derived from augment items the player has purchased. It reads from `PlayerBlob.InventoryItems`, deserializes each item's stat contributions, and exposes the aggregated values as properties to all other systems.

---

## Key Scripts

| Script | Path |
|---|---|
| `PlayerStatModifiers` | `Assets/Aaron/Scripts/PlayerStatModifiers.cs` |
| `StatBoostSerializer` | `Assets/Aaron/Scripts/StatBoostSerializer.cs` |
| `StatBoostType` | `Assets/Aaron/Scripts/ShopSystem/StatBoostType.cs` |
| `StatBoostLoadableStoreItem` | `Assets/Aaron/Scripts/ShopSystem/StatBoostLoadableStoreItem.cs` |
| `AugmentQualityTier` | `Assets/Aaron/Scripts/ShopSystem/AugmentQualityTier.cs` |

---

## How It Works

1. `PlayerStatModifiers` is an `InitializeableGameComponent` — registered with `GameInitializer` so it is initialized at game start.
2. On `InitializeOnGameStart`, it subscribes to `PlayerBlob.InventoryItems.DictionaryChanged`.
3. Whenever the inventory changes (e.g., after an augment purchase), `ReapplyFromBlob()` resets all stats to defaults and then iterates every item in inventory, calling `StatBoostSerializer.TryDeserializeEntries` on each item ID.
4. Each `StatBoostEntry` maps a `StatBoostKind` enum to a float value. That value is multiplied by the item's stack count and added to the appropriate property.
5. After recalculation, `OnStatsChanged` is fired so consumers (e.g., `PlayerGameplayManager`) can react.

---

## Available Stats

| Property | Default | Notes |
|---|---|---|
| `MoveSpeedMultiplier` | `1.0` | Multiplier; boost expressed as % added |
| `DamageMultiplier` | `1.0` | Multiplier on `GameManager.baseDamage`. Multiple +X% sources stack **multiplicatively** (50% + 50% => 1.75x, not 2.0x). |
| `MaxHpMultiplier` | `1.0` | Multiplier; boost expressed as % added |
| `RangedDamageMultiplierBonus` | `0` | Additive bonus to `GameManager.rangedMultiplier` |
| `ProjectileSpeedMultiplier` | `1.0` | Multiplier |
| `UltimateChargeMultiplier` | `1.0` | Multiplier |
| `LifestealPercent` | `0` | % of damage dealt converted to healing |
| `RegenPercentPerSecond` | `0` | % of max HP regenerated per second |

---

## Adding a New Stat
1. Add the new `StatBoostKind` value to the enum in `StatBoostType.cs`.
2. Add a corresponding property to `PlayerStatModifiers`.
3. Add a `Reset()` line and a `case` in `ApplyStatBoost()`.
4. Create a `StatBoostLoadableStoreItem` ScriptableObject in the Unity editor targeting the new kind.
