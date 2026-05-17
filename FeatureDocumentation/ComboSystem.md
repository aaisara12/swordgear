# Combo System & Scoring

## Purpose
Tracks the player's combat rhythm: combo count, combo timer, score multiplier, rapid-streak bonuses, and total level points. The final points tally determines the quality tier of augments offered in the shop.

---

## Key Scripts

| Script | Path |
|---|---|
| `ComboSystem` | `Assets/Scripts/Combat/ComboSystem.cs` |
| `UltimateMeter` | `Assets/Scripts/Combat/UltimateMeter.cs` |
| `AugmentQualityTier` | `Assets/Aaron/Scripts/ShopSystem/AugmentQualityTier.cs` |
| `CombatHUD` | `Assets/Scripts/UI/CombatHUD.cs` |

---

## Entry Points

`ComboSystem` is a singleton (`ComboSystem.Instance`). It wires itself to global enemy events in `OnEnable`/`OnDisable`:

```csharp
EnemyController.OnAnyEnemyHit   → HandleEnemyHit
EnemyController.OnAnyEnemyDeath → HandleEnemyDeath
ElementManager.OnActiveElementChanged → HandleElementChanged
```

No direct calls into `ComboSystem` are needed from combat code — it is purely event-driven.

---

## Scoring Rules

| Event | Points |
|---|---|
| Enemy hit | `hitBasePoints × currentMultiplier` |
| Rapid streak (N hits within window) | `rapidStreakBonusPoints × currentMultiplier` |
| Enemy kill | multiplier bumped first, then `killBonusPoints × newMultiplier` |

Multiplier increments by 1 (capped at `maxMultiplier`) on kills and rapid streaks. The combo timer resets on each hit. When the timer expires, the combo breaks and the multiplier resets to 1.

---

## Events Exposed

| Event | Payload | Use |
|---|---|---|
| `OnComboChanged` | `(int combo, int multiplier)` | HUD display |
| `OnComboTimerChanged` | `(float timer, float duration)` | Timer bar |
| `OnMultiplierChanged` | `int multiplier` | Multiplier pop |
| `OnComboBroken` | — | Visual/audio feedback |
| `OnLevelPointsChanged` | `int points` | Points display |
| `OnPointsAwarded` | `(int points, Element)` | `UltimateMeter` charge |

---

## Augment Quality Tier

After a level ends, call `ComboSystem.Instance.GetAugmentQualityTier()` to get an `AugmentQualityTier` value (`Low`, `Medium`, `High`, `Elite`) based on configurable point thresholds set in the Inspector.

---

## Level vs Round Reset

| Method | Resets points? | Use case |
|---|---|---|
| `ResetForNewLevel()` | No | Between levels in a round |
| `ResetForNewRound()` | Yes | Starting a fresh run |
| `OnLevelFinished()` | No | Freeze scoring at end of level |
