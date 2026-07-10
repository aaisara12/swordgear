# Enemy System

## Purpose
Drives enemy movement, attacking, status effects, and death — using the Strategy pattern so behaviours are composable without subclassing.

---

## Key Scripts

| Script | Path |
|---|---|
| `EnemyController` | `Assets/Scripts/Enemy/EnemyController.cs` |
| `IMovementStrategy` | `Assets/Scripts/Enemy/IMovementStrategy.cs` |
| `IAttackStrategy` | `Assets/Scripts/Enemy/IAttackStrategy.cs` |
| `FollowPlayerStrategy` | `Assets/Scripts/Enemy/FollowPlayerStrategy.cs` |
| `StrafeMovementStrategy` | `Assets/Scripts/Enemy/StrafeMovementStrategy.cs` |
| `MeleeAttackStrategy` | `Assets/Scripts/Enemy/MeleeAttackStrategy.cs` |
| `RangedAttackStrategy` | `Assets/Scripts/Enemy/RangedAttackStrategy.cs` |
| `ShotgunAttackStrategy` | `Assets/Scripts/Enemy/ShotgunAttackStrategy.cs` |
| `BeamSniperAttackStrategy` | `Assets/Scripts/Enemy/BeamSniperAttackStrategy.cs` |
| `TurretAttackStrategy` | `Assets/Scripts/Enemy/TurretAttackStrategy.cs` |
| `StationaryMovementStrategy` | `Assets/Scripts/Enemy/StationaryMovementStrategy.cs` |
| `IChargingAttackStrategy` | `Assets/Scripts/Enemy/IChargingAttackStrategy.cs` |
| `EnemyAttackDamage` | `Assets/Scripts/Enemy/EnemyAttackDamage.cs` |
| `EnemyBeamLaser` | `Assets/Scripts/Enemy/EnemyBeamLaser.cs` |
| `EnemyCatalog` | `Assets/Scripts/Enemy/EnemyCatalog.cs` |
| `EnemySpawner` | `Assets/Scripts/Enemy/EnemySpawner.cs` |
| `EnemyProjectile` | `Assets/Scripts/Enemy/EnemyProjectile.cs` |
| `HitEffectAnimator` | `Assets/Scripts/Enemy/HitEffectAnimator.cs` |
| `EnemyBurn` | `Assets/Scripts/Enemy/Enemy Effects/EnemyBurn.cs` |
| `EnemyChill` | `Assets/Scripts/Enemy/Enemy Effects/EnemyChill.cs` |
| `EnemyStatic` | `Assets/Scripts/Enemy/Enemy Effects/EnemyStatic.cs` |

---

## Strategy Pattern

Each enemy prefab attaches concrete strategy components. `EnemyController` discovers them at runtime via `GetComponent<IMovementStrategy>()` / `GetComponent<IAttackStrategy>()` in `Start()`.

To add a new movement or attack behaviour, implement the appropriate interface and attach it to the prefab — no changes to `EnemyController` are required.

### Archetype roles (Commit 18+)

| Role | Movement | Attack | Prefab prefix |
|------|----------|--------|----------------|
| Legacy melee | `FollowPlayerStrategy` | `MeleeAttackStrategy` | `MeleeEnemy_*` |
| Legacy ranged | `StrafeMovementStrategy` | `RangedAttackStrategy` | `RangedEnemy_*` |
| Turret | `StationaryMovementStrategy` | `TurretAttackStrategy` | `Turret_*` |
| Shotgun | `StrafeMovementStrategy` | `ShotgunAttackStrategy` | `Shotgun_*` |
| Beam sniper | `StrafeMovementStrategy` (slow) | `BeamSniperAttackStrategy` | `BeamSniper_*` |

Beam sniper fires a **telegraphed laser**: a translucent rectangle shows the hit zone during charge-up, then a bright beam appears along the same path and damages the player if they overlap the `BoxCollider2D`. Prefab: `EnemyBeamLaser.prefab` (generate via **Henry → Generate Enemy Beam Laser Prefab**).

Generate prefabs: **Henry → Generate New Enemy Archetype Prefabs**. Play Mode showcase: **Henry → Playtest → Spawn New Enemy Archetypes**.

`IChargingAttackStrategy` enemies pause strafe movement while winding up a shot (shotgun, beam sniper, legacy ranged).

---

## Enemy Catalog & Spawn Scaling (Commit 19+)

| Script / Asset | Path |
|---|---|
| `EnemyCatalog` | `Assets/Scripts/Enemy/EnemyCatalog.cs` + `Assets/Aaron/ScriptableObjects/EnemyCatalog.asset` |
| `EnemyArchetype` | `Assets/Scripts/Enemy/EnemyArchetype.cs` |
| `ElementStatKnobs` | `Assets/Scripts/Enemy/ElementStatKnobs.cs` |
| `SpawnModifiers` / `DifficultyCurve` | `Assets/Scripts/Enemy/` |
| `EncounterContext` | `Assets/Aaron/Scripts/Map/EncounterContext.cs` |

`LevelLoader` applies spawn modifiers after each Instantiate:

1. **Difficulty** from `(blockIndex, combatIndexInBlock)` — later combats / blocks get more HP & damage.
2. **Elemental knobs** for new archetypes (`applyElementKnobsAtSpawn`) — Fire aggressive, Ice tanky/slow/hard, Lightning fast/weak. Legacy melee/ranged keep baked prefab stats.

Generate / refresh catalog: **Henry → Generate Enemy Catalog** (also wires `RunManager.enemyCatalog` on CoreSystems).

### Spawn presentation & elites (Commit 20+)

| Script / Asset | Path |
|---|---|
| `EnemySpawnPresentation` | `Assets/Scripts/Enemy/EnemySpawnPresentation.cs` |
| `ElitePresentation` | `Assets/Aaron/ScriptableObjects/ElitePresentation.asset` |
| `EliteAura` | `Assets/Visuals/Prefabs/Enemies/EliteAura.prefab` |
| Shared spawn anim | `Assets/Visuals/Animations/EnemySpawn/` |

On spawn, combat is disabled until the shared **Spawn** clip finishes (~0.65s pop-in on `VisualRoot`). Interim elite rule: **first enemy of the last wave** gets elite HP/damage/scale + `EliteAura` child enabled. Wire/refresh: **Henry → Wire Enemy Spawn Presentation**.

---

## Global Events

`EnemyController` fires two static events used by `ComboSystem` and `UltimateMeter`:

```csharp
public static event Action<EnemyController, float, Element>? OnAnyEnemyHit;
public static event Action<EnemyController>? OnAnyEnemyDeath;
```

It also fires a per-instance `OnDeath` event for systems that track a specific enemy (e.g., `EnemyDeathTracker` in the tutorial).

---

## Status Effects

Status effects are managed by `GameManager` (see `GameManager.AddEffect` / `EffectTickLoop`). Effects implement `GameManager.IEnemyEffect` with `EffectBegin`, `EffectTick`, and `EffectEnd` callbacks. Effects are applied by weapon scripts and tick every second.

| Effect | Applied by |
|---|---|
| `Burn` | Fire weapons |
| `Chill` | Ice weapons — slows enemy via `speedMultiplier` |
| `Static` | Lightning weapons |

---

## Damage Flow

```
Weapon hits enemy
  → EnemyController.TakeDamage(damage)
    → GameManager.DisplayDamageUI(...)
    → OnAnyEnemyHit fired
    → GameManager.NotifyPlayerDealtDamage(damage)  ← lifesteal
    → hp -= damage
    → if hp ≤ 0 → Die()
      → OnAnyEnemyDeath fired
      → OnDeath fired
      → death VFX instantiated
      → gameObject destroyed
```

---

## Spawning

`LevelLoader` instantiates wave prefabs at `EnemySpawnPoint` markers, then calls `EnemyController.ApplySpawnModifiers` using `EncounterContext` + `EnemyCatalog`. Wave lists still come from `RunManager.BuildCombatWaves` (legacy pool) until Commit 21 replaces them with `EncounterBuilder`. See [LevelGeneration.md](LevelGeneration.md) and [MapRunSystem.md](MapRunSystem.md).
