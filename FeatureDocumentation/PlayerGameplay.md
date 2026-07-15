# Player Gameplay

## Purpose
Manages the player's in-game pawn: spawning, despawning, health (including regen and lifesteal), and linking/unlinking input.

---

## Key Scripts

| Script | Path |
|---|---|
| `PlayerGameplayManager` | `Assets/Aaron/Scripts/PlayerGameplayManager.cs` |
| `PlayerGameplayPawn` | `Assets/Aaron/Scripts/PlayerGameplayPawn.cs` |
| `PlayerGameplayInputManager` | `Assets/Aaron/Scripts/PlayerGameplayInputManager.cs` |
| `OnAwakePawnSpawner` | `Assets/Aaron/Scripts/OnAwakePawnSpawner.cs` |
| `OnAwakePawnInputLinker` | `Assets/Aaron/Scripts/OnAwakePawnInputLinker.cs` |
| `Mover` | `Assets/Aaron/Scripts/PlayerBehaviours/Mover.cs` |
| `Attacker` | `Assets/Aaron/Scripts/PlayerBehaviours/Attacker.cs` |
| `Shooter` | `Assets/Aaron/Scripts/PlayerBehaviours/Shooter.cs` |
| `PlayerVisual` | `Assets/Aaron/Scripts/PlayerBehaviours/PlayerVisual.cs` |
| `ShootDirectionVisualizer` | `Assets/Aaron/Scripts/PlayerBehaviours/ShootDirectionVisualizer.cs` (TestPlayer only) |
| `PlayerAimIndicator` | `Assets/Aaron/Scripts/PlayerBehaviours/PlayerAimIndicator.cs` (production Player) |
| `PlayerController` | `Assets/Scripts/PlayerController.cs` |
| `PlayerWeaponIndicator` | `Assets/Scripts/PlayerWeaponIndicator.cs` |

---

## Pawn Lifecycle

1. `PlayerGameplayManager.Awake()` — instantiates the pawn prefab and sets it inactive.
2. `SpawnPawnAtLocation(Transform)` — activates the pawn, plays a spawn animation, links it to `PlayerGameplayInputManager`, and subscribes to `OnRegisterDamage`.
3. `DespawnPawn()` — deactivates the pawn, unlinks input, and cleans up event subscriptions.

Spawn is triggered by a `TransformEventChannelSO` (event channel) so the caller doesn't need a direct reference to `PlayerGameplayManager`.

---

## Health System

Health lives entirely in `PlayerGameplayManager` (not on the pawn):
- `baseMaxHp` is a serialized base value; `maxHp` is computed as `baseMaxHp × PlayerStatModifiers.MaxHpMultiplier`.
- When max HP increases from an augment, current HP rises by the same amount (e.g. +50 max also heals +50). Decreases only clamp.
- Damage is received via `HandlePawnRegisterDamage` → `TakeDamage`.
- `Heal(float)` is called by the lifesteal handler and the regen coroutine.
- Regen runs via `RegenTick()` coroutine, ticking every second. It starts/stops in response to stat changes.
- When HP reaches 0, `Defeat()` sets `IsDefeated`, stops regen, hides simulated joysticks via `EnableSimulatedJoysticksEventChannel`, disables input (zeroing movement), plays `DoDefeatAnimation()` (disables colliders), raises `PlayerDefeatedEventChannel` (clears run), and shows the defeat overlay via `DefeatOverlayVisibilityChannel`.
- Enemies stop chasing while `IsDefeated` is true.
- `IsDefeated` resets in `InitializeHealthForNewRun()` when a new run starts.
- `DefeatStateController` (CombatHUD) hides gameplay HUD, ducks BGM, shows **DEFEATED** + subtitle, then auto-returns to Title via `DefeatContinueChannel` (~3s, tap to skip).

---

## Player Behaviours

The pawn composes discrete behaviour components rather than implementing everything in one class:

| Component | Responsibility |
|---|---|
| `Mover` | Reads directional input and moves the pawn |
| `Attacker` | Handles melee attack with cooldown |
| `Shooter` | Handles ranged attack |
| `PlayerVisual` | Manages sprite/animation based on movement direction |
| `ShootDirectionVisualizer` | TestPlayer-only aim arrow (direction only) |
| `PlayerAimIndicator` | Production aim preview on `Player.prefab` during AimedAttack hold |

### Production aim preview (`PlayerAimIndicator`)

While the player holds AimedAttack:

- **Sword in hand (`MeleeReady`)** — fixed-length throw line from the throw origin. A `Physics2D.BoxCast` finds the nearest blocker: a `Bumper` (bounce segment along `Vector2.Reflect`) or a wall (`TilemapCollider2D` / `CompositeCollider2D`, plus any solid collider on `wallLayers`). Walls clip the line with no bounce. Bounce length is separately configurable.
- **Sword out (`SwordThrown`)** — dash preview only: primary line scales to `dashSpeed × dashDuration`. No bounce segment. Attack-button dash (move-stick) does not show this indicator.

Cleared on `StopAiming` (aim stick release / aim session end). TestPlayer still uses `ShootDirectionVisualizer` and is unchanged.

---

## Notes

- `GameManager.Instance.player` is set/cleared on spawn/despawn as a temporary bridge. The comment in the code marks this for future refactoring.
- `PlayerStatModifiers.OnStatsChanged` is subscribed during spawn to recompute max HP and restart regen when augments are purchased.
