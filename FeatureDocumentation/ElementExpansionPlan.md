# Element Expansion Plan — Earth, Dark, Light

> **Goal:** Land the three new elements as small commits where **every commit changes something you can play**.
> **Scope:** The "Core Gameplay" block of the improvements doc, up to (not including) Elemental Affinities.
> **Order:** Cheapest weapon first, so the shared plumbing is proven before the expensive one.

---

## Rules

1. **Playtest gate** — each commit must answer: *"What do I do in Play Mode that I couldn't do before?"*
2. **Reachability is part of the gate** — an element is unreachable until it's on the gear ring. Every
   commit that adds behaviour must also say **how to reach it** (see [Reaching a new element](#reaching-a-new-element)).
3. **Regression check every commit** — the existing four elements must still play correctly. New content
   is worthless if it quietly breaks Fire.
4. **One weapon capability per commit** — split if the playtest description needs "and also…".
5. **Don't touch `IElementalWeapon`** — the sword throw returns as the ultimate, so its ranged hooks are
   dormant, not dead. Refactor pass later if it's still awkward.
6. **EditMode tests are bonus** — allowed alongside a playtest, never instead of one.

---

## Milestone overview (play order)

```text
M0  Foundation — elements exist in the enum, gear ring becomes loadout-driven   [landed, editor-verified]
M1  Earth / Ballista Turret — charge-only, movement-locks to aim, piercing bolt
M2  Light / Harp — marking projectile + angel that detonates the marks
M3  Dark / Scythe — arc swing, blink-circle charge, then minion conversion
```

Earth is first because it's the smallest and proves the movement-lock aim mode. Dark is last because
minion conversion is the only genuinely new *system* in the batch.

---

## Status

| Commit | Title | Committed | Editor pass | Verified in play |
|---|---|---|---|---|
| 01 | Foundation — elements + loadout-driven gear | ✅ `a3e6770` | ✅ 2026-08-23 | ⏳ flick check pending |
| 02 | Earth selectable, fires a basic bolt | ✅ landed | ✅ 2026-08-23 | ⏳ not yet |
| 03 | Earth charge locks movement and aims | ☐ | ☐ | ☐ |
| 04 | Earth bolt pierces + scales with charge | ☐ | ☐ | ☐ |
| 05 | Light selectable, tap marks enemies | ☐ | ☐ | ☐ |
| 06 | Light charge summons the angel | ☐ | ☐ | ☐ |
| 07 | Dark selectable, tap arc swing | ☐ | ☐ | ☐ |
| 08 | Dark charge blinks + circle swing | ☐ | ☐ | ☐ |
| 09 | Dark execution raises a minion | ☐ | ☐ | ☐ |

### Commit 01 editor pass — 2026-08-23

Done with Unity MCP attached. `a3e6770` and the `6ec660a` follow-up both compile:

- **Compiles clean.** No errors; the only warnings are pre-existing CS8632 nullable-annotation
  warnings in the `Enemy` attack strategies, unrelated to this work.
- **The enum grew as intended** — `Element` is `Physical, Fire, Ice, Lightning, Wind, Earth, Dark, Light`
  in the loaded assembly. `GearManager.arcCount` is gone and `SetEquippedLoadout` is present.
- **The raw-YAML risk did not fire.** `Gear.prefab.startingLoadout` deserializes to exactly
  `Wind, Fire, Ice, Lightning` (size 4), confirmed both through `SerializedObject` and in the Inspector.
  `ArcCount` therefore still derives 4 arcs.
- **EditMode tests: 73/73 pass** — the existing suite is unaffected.

Still open: the **play-mode flick check** — the ring drawing 4 arcs and each flick direction granting the
same element as before. That's a feel check, not something the editor pass can settle.

---

## Reaching a new element

Unlike the level-gen work, these features are gated behind the gear ring. The ring deliberately holds
only *equipped* elements — that's the whole point of the loadout-driven arc count, since flick target
size is 360/count degrees.

So to reach a new element, edit **`startingLoadout` on `Gear.prefab`** (Inspector,
`Assets/Prototype3/Prefabs/`).

**Each new element is appended, not swapped in** — the loadout grows as the elements land:

```text
Commit 01 (ship default) →  Wind, Fire, Ice, Lightning          4 arcs, 90°
Commit 02 (+ Earth)      →  Wind, Fire, Ice, Lightning, Earth   5 arcs, 72°
Commit 05 (+ Light)      →  … , Light                           6 arcs, 60°
Commit 07 (+ Dark)       →  … , Dark                            7 arcs, ~51°
```

> **Superseded 2026-08-23.** This section previously said to *swap* rather than append, on the theory
> that 7 arcs would recreate the "hitting the desired colour feels random" complaint. Reverted by
> decision: keeping every element equipped is what the ring is for, and swapping meant no playtest ever
> exercised more than four. **Arc size is now the thing to watch** — if flicking starts feeling
> imprecise as the count climbs, that's real feedback about the ring, not a reason to shrink the loadout.

Arc order follows list order starting at +X counter-clockwise, so an appended element takes the next
arc round and every previously-learned flick direction shifts. Expect the existing four to feel
different the first time the count changes.

---

## M0 — Foundation

### Commit 01 — Elements exist; gear ring becomes loadout-driven ✅ `a3e6770`

| | |
|---|---|
| **Adds** | `Earth`, `Dark`, `Light` on `Element` and `GearTile`; `ElementalInteractions.GetMultiplier`; `GearManager.SetEquippedLoadout`; this plan doc |
| **Changes** | `ElementVisuals` + `ElementVisualUtility` — 3 cases across 5 switches; `GameManager`/`SwordController` — matrix reads go through `GetMultiplier`; `Gear.prefab` — `arcCount: 4` → `startingLoadout: [Wind, Fire, Ice, Lightning]`; `ElementSystem.md` |
| **Removes** | `GearManager.arcCount`, `DefaultLoadout`, `ApplyDefaultLoadout` |
| **Mechanism** | `ArcCount` derives from `slotTiles.Count`. New elements are absent from the damage matrix and resolve to a neutral 1× via the `GetMultiplier` fallback, so they're neutral without 39 hand-written entries |
| **Playtest** | ⚠️ **None — this commit deliberately fails the playtest gate.** No weapons are registered and the loadout is unchanged, so the new elements are unreachable by design |
| **Regression check** | Ring still draws **4 arcs**; flicks grant the same elements in the same directions; `startingLoadout` reads Wind/Fire/Ice/Lightning in the Inspector ✅; damage numbers unchanged for the existing elements |
| **Risk** | ~~`Gear.prefab` was edited as raw YAML. Prefab deserialization fails *quietly*~~ — **cleared 2026-08-23**: the loadout deserializes correctly, confirmed in the Inspector |
| **Not in commit** | Any weapon; any loadout change |

> A pure-foundation commit is the one case where the playtest gate can't be met — there's nothing to
> play until a weapon exists. It's called out rather than papered over. Everything after this is playable.

---

## M1 — Earth / Ballista Turret

> *Fantasy: immovable firepower. Charge: disable movement (movement aims instead), charge a single
> piercing projectile, launch on release. Tap: N/A, weak charge attack.*

### Scope boundary

| In M1 | Out of scope |
|---|---|
| `EarthWeapon`, charge ramp, movement-lock aim mode | Light/Dark weapons |
| Pierce support on `PlayerProjectile` | Earth enemies (player-only, by decision) |
| Earth entries in the damage matrix | Ultimate / sword throw |

### Commit 02 — Earth is selectable and shoots ✅

| | |
|---|---|
| **Adds** | `Assets/Scripts/Weapon Implementations/EarthWeapon.cs` — implements `IElementalWeapon`; tap fires one bolt via `PlayerProjectile`. `EarthBolt.prefab` (copied from `WindDart`; `Launch` tints it amber automatically, so no hand-authored colour) |
| **Changes** | `CoreSystems.prefab` — `Element Manager/Earth` node with `EarthWeapon` + an appended `elementalWeapons` entry; `Gear.prefab` + `GearManager`'s C# default — `startingLoadout` **appends** Earth, 4 arcs → 5 |
| **Mechanism** | `MeleeStrike` fires the bolt and returns the cooldown. No charge ramp yet — press and release, fixed damage. The bolt flies dead straight: no homing, since aiming is what commits 03–04 are built on |
| **How to reach it** | Flick the right stick toward the Earth arc — the new 5th arc, last in ring order |
| **Playtest** | Flick to Earth → the joystick ring and charge VFX turn **amber**. Tap → a bolt fires forward and damages the first enemy it hits |
| **Regression check** | Wind, Fire, Ice and Lightning all still selectable and behave as before. ⚠️ **Every flick direction has moved** — arcs are 72° not 90°, so all five sit in new places. Judge the elements by which arc lights up, not by muscle memory |
| **Not in commit** | Movement lock, pierce, charge scaling |

### Commit 03 — Charging plants you and aims

| | |
|---|---|
| **Adds** | `IMeleeChargeProvider` on `EarthWeapon` (charge indicators light up for free); a movement-lock aim mode on `PlayerController` |
| **Changes** | `PlayerController.MoveInDirection` — while Earth is charging, the left stick aims instead of moving |
| **Mechanism** | Charge start sets the lock; release/cancel clears it. Bolt launches along the aim direction, not the facing direction |
| **Playtest** | Hold to charge → **you stop moving**; the left stick now swings the aim. Release → the bolt fires where you aimed. This is the whole "immovable firepower" fantasy — it should feel like a tradeoff |
| **Regression check** | Movement is normal for every other element, and normal for Earth when *not* charging. Getting hit or dying mid-charge must not leave you stuck |
| **Risk** | Movement lock is the one change reaching outside the weapon into `PlayerController`. Every exit path (cancel, damage, death, node change) must clear it |
| **Not in commit** | Pierce, charge tiers |

### Commit 04 — The bolt pierces and rewards a full charge

| | |
|---|---|
| **Adds** | Pierce support on `PlayerProjectile` (survives a hit, damages each enemy once) |
| **Changes** | `EarthWeapon` — charge tiers scale damage, size and pierce count |
| **Playtest** | Line up 3 enemies → full charge → **one bolt kills the whole line**. A tap-level charge only chips the first |
| **Regression check** | Fire's fireballs and Wind's darts still consume on hit — pierce must be opt-in, since they share `PlayerProjectile` |
| **EditMode (optional)** | Pierce decrement / once-per-enemy |

### Acceptance criteria (M1 done)

- Earth is selectable from the ring and visually reads as amber everywhere (joystick, charge VFX, damage numbers).
- Holding charge visibly roots the player and repurposes the left stick as aim.
- A full-charge bolt pierces a line of enemies; a short charge doesn't.
- No other element's movement, damage or projectiles changed.

---

## M2 — Light / Harp

> *Fantasy: mark and detonate, ranged burst. Tap: launch a single marking projectile. Charge: summon an
> angel at your position that launches piercing beams at each mark.*

### Design principles

1. **Marks are a status effect, not a bespoke system.** `EnemyEffect` already carries Burn/Chill/Static/
   Buffetted on a shared 1s tick — `Marked` is a fifth entry and inherits the expiry plumbing.
2. **The angel is an actor with a lifetime**, not a weapon mode. It fires and expires on its own.
3. **Beams mirror the enemy Beam Sniper.** `EnemyBeamLaser` is the reference; don't invent a second beam.

### Commit 05 — Tap marks enemies

| | |
|---|---|
| **Adds** | `LightWeapon.cs`; `EnemyEffect.Marked` + its visual; an enumerate-marked query on `ActiveEnemyRegistry` (it only does `TryGetNearest` today) |
| **Changes** | `CoreSystems.prefab` — register `LightWeapon`; `Gear.prefab` — loadout swaps in Light |
| **How to reach it** | Flick toward the Light arc |
| **Playtest** | Flick to Light → tap → a projectile flies out and the enemy it hits **visibly carries a mark**. Mark expires on its own after a few seconds |
| **Regression check** | Existing status effects still apply and expire — Fire's burn, Ice's chill, Lightning's static |
| **Not in commit** | The angel; anything that consumes marks |

### Commit 06 — Charge summons the angel

| | |
|---|---|
| **Adds** | Angel actor prefab + controller (lifetime, beam cadence); player-side piercing beam |
| **Changes** | `LightWeapon` — charge release summons the angel at the player's position |
| **Mechanism** | On summon the angel queries all marked enemies and fires one piercing beam per mark, consuming it |
| **Playtest** | Mark 3 enemies → hold charge → release → an **angel appears and beams every marked enemy**. Marking nobody and charging should whiff harmlessly, not error |
| **Regression check** | Marks still expire naturally when no angel is summoned |
| **Risk** | Enemies can die between marking and detonation — the angel must tolerate dead/despawned targets |

### Acceptance criteria (M2 done)

- Tap marks; marks are readable at a glance and expire.
- Charge summons an angel that beams every live mark and consumes them.
- Zero marks, or marks on enemies that died mid-charge, are handled without errors.
- The play pattern reads as *mark → detonate*, not *shoot → shoot*.

---

## M3 — Dark / Scythe

> *Fantasy: summoner. Tap: swing scythe in an arc. Charge: blink and swing in a circle. Execution with
> dark melee resummons the enemy as a minion.*

Split because the combat and the necromancy are very different sizes — 07/08 are assembly from existing
parts, 09 is a new system. Dark should be playable long before minions land.

### Commit 07 — Dark is selectable, tap swings

| | |
|---|---|
| **Adds** | `DarkWeapon.cs` — arc swing modelled on `PhysicalWeapon` (seek nearest, step in, static hitbox) |
| **Changes** | `CoreSystems.prefab` — register; `Gear.prefab` — loadout swaps in Dark |
| **Playtest** | Flick to Dark → tap → a **purple arc swing** that damages enemies in front of you |
| **Regression check** | Physical's swing is unchanged (Dark borrows its shape, it must not share its state) |

### Commit 08 — Charge blinks and cuts a circle

| | |
|---|---|
| **Changes** | `DarkWeapon` — charge release calls `PlayerController.BlinkTo` then a radial hit via `MeleeAugmentUtility.DamageEnemiesInRadius` |
| **Mechanism** | Both primitives already exist — `BlinkTo` from Lightning's Thunderstep, radial damage from cleave |
| **Playtest** | Hold → release → you **teleport a short distance and everything around the landing point takes a hit**. Blinking into a pack should feel like the reward |
| **Regression check** | Lightning's Thunderstep still blinks correctly — `BlinkTo` is now shared by two elements |
| **Not in commit** | Minions |

### Commit 09 — Execution raises a minion

| | |
|---|---|
| **Adds** | Minion conversion — friendly AI, target acquisition, ownership, lifetime, death handling |
| **Changes** | `DarkWeapon.OnMeleeHit` — a killing blow converts instead of killing; `EnemyController` — a converted state that retargets to enemies |
| **Mechanism** | Reuses the enemy's own prefab and movement/attack strategies with a flipped target set, so minions inherit archetype behaviour for free |
| **Playtest** | Kill an enemy with Dark melee → it **gets back up on your side** and attacks other enemies. It expires (or dies) on its own rather than accumulating forever |
| **Regression check** | Non-Dark kills still just die. Minions must not count as live enemies for **wave-clear**, or the arena will never complete |
| **Risk** | The largest item in the plan. Wave-clear accounting, combo/ult credit, and player-collision are all places a converted enemy can leak into systems that assume "enemy = hostile" |
| **EditMode (optional)** | Wave-clear ignores converted enemies |

### Acceptance criteria (M3 done)

- Dark plays as a full melee element without minions (commits 07–08 alone are shippable).
- Dark executions raise minions that fight for you and expire cleanly.
- Waves still clear with minions alive; combo and ultimate charge behave sanely.

---

## Quick reference — what to play after each commit

| After commit | Play this |
|---|---|
| **01** | Nothing new — verify the ring still draws 4 arcs and flicks are unchanged |
| **02** | Flick to Earth → tap → amber bolt |
| **03** | Hold Earth charge → you're rooted, left stick aims |
| **04** | Full-charge Earth bolt pierces a line of 3 |
| **05** | Flick to Light → tap → enemies visibly marked |
| **06** | Mark 3 → charge → angel beams them all |
| **07** | Flick to Dark → tap → purple arc swing |
| **08** | Dark charge → blink + circle cut |
| **09** | Dark execution → the corpse fights for you |

---

## Backlog (in this doc's scope, not started)

- ☐ **Element-switch flourish / "gear arts"** — the doc asks for a visual pop on switch; nothing exists.
  `ElementManager.OnActiveElementChanged` is the hook.
- ☐ **Sword sprite changes per element.** The doc says *"sword changes to match that element"* with a
  shared gear crossguard motif. Today only VFX differ — `PlayerWeaponIndicator` just rotates the pivot
  and toggles visibility; there is no element→sprite path. Art-blocked (7 sprites), then a small change.
- ☐ **Restore the shipping loadout** before the last commit if playtest swaps left it modified.

## Known adjacent issues (out of scope, recorded so they aren't lost)

- **The dash is currently unreachable.** `9d96937` removed the only assignment of
  `PlayerState.SwordThrown`, and both dash triggers are gated behind it — so `DashCoroutine` is dead code
  and the player has no dodge. The doc's unresolved "dash dilemma" is about a build that no longer
  exists. Confirm this is a deliberate step toward throw-as-ultimate and not a regression.
- **Element grants are timed, not persistent.** A flick calls `ApplyEmpowerment` with `imbueDuration`
  and expires back to `Physical`. The doc's model reads as a persistent mode, and playtesters asked for
  longer buffs. Resolve with Elemental Affinities, since it decides whether `Physical` stays a real state.

---

## Decisions already taken

1. **New elements are matrix-neutral** — 1× both directions, pending Elemental Affinities. Avoids
   building a 7-element rock-paper-scissors that rework may delete.
2. **Player-only elements** — no Earth/Dark/Light enemies; enemy prefabs, catalog knobs and wave
   composition stay at five elements.
3. **Dynamic gear arcs** — the ring shows equipped elements only.
4. **`IElementalWeapon` untouched** — see Rule 5.

### Already done before this plan (do not redo)

| Doc bullet | Commit |
|---|---|
| Gear becomes a background object | `9d96937` |
| Right joystick = element selector | `9d96937` |
| Sword no longer thrown (returns as the ultimate) | `9d96937` |
| Per-element weapons with distinct playstyles | `bcf247f` |

**Fire is already a spellblade**, matching the doc — `meleeDamageMultiplier = 0.35f` with
`FireballBurst()` on swing and a bomb cascade at max charge. The `bcf247f` commit message calling it a
"gun" is wrong; the code is right.

---

*Last updated: 2026-08-23 — commit 01 landed and passed its editor pass (compile + 73/73 EditMode);
play-mode flick check still outstanding. Say **"start commit 02"** (or a later number) and we apply only
that slice and give you the exact play steps.*
