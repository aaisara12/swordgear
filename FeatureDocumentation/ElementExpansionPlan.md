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
5. ~~**Don't touch `IElementalWeapon`**~~ — **lifted 2026-08-23, see commit 02.** The sword throw still
   returns as the ultimate and its ranged hooks stay dormant-not-dead; the rename and default members
   deliberately leave them in place. What the rule was guarding is still guarded.
6. **EditMode tests are bonus** — allowed alongside a playtest, never instead of one.

---

## Milestone overview (play order)

```text
M0  Foundation — elements exist in the enum, gear ring becomes loadout-driven   [landed, editor-verified]
R   Interface tidy — tap/charge naming + defaults, so weapons implement only what they use
M1  Earth / Ballista Turret — grounds you, builds a ballista, charged piercing beam
M1b Auto-aim range — the pointer's reach becomes per-element
M2  Light / Harp — marking projectile + angel that detonates the marks
M3  Dark / Scythe — arc swing, blink-circle charge, then minion conversion
```

Earth is first because it's the smallest and proves the movement-lock aim mode. Dark is last because
minion conversion is the only genuinely new *system* in the batch. **R sits before all element work** so
every weapon — Earth, Light and Dark — is written against the tidied interface rather than retrofitted
afterwards.

---

## Status

| Commit | Title | Committed | Editor pass | Verified in play |
|---|---|---|---|---|
| 01 | Foundation — elements + loadout-driven gear | ✅ `a3e6770` | ✅ 2026-08-23 | ⏳ flick check pending |
| 02 | Interface tidy — tap/charge naming + defaults | ✅ landed | ✅ 2026-08-23 | n/a (refactor) |
| 03 | Earth selectable, fires a bolt | ✅ landed | ✅ 2026-08-23 | ⏳ superseded by 04 |
| 04 | Earth grounds you, builds, aims, charges | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 05 | The ballista appears and builds itself | ☐ | ☐ | ☐ |
| 06 | The shot becomes a beam and pierces | ☐ | ☐ | ☐ |
| 07 | Auto-aim range becomes per-element | ☐ | ☐ | ☐ |
| 08 | Light selectable, tap marks enemies | ☐ | ☐ | ☐ |
| 09 | Light charge summons the angel | ☐ | ☐ | ☐ |
| 10 | Dark selectable, tap arc swing | ☐ | ☐ | ☐ |
| 11 | Dark charge blinks + circle swing | ☐ | ☐ | ☐ |
| 12 | Dark execution raises a minion | ☐ | ☐ | ☐ |

> **Renumbered 2026-08-23.** M1 was three commits, now four: the old 03 (charge locks movement) split
> into *grounds you* (04) and *the ballista appears* (05). M2/M3 shifted by two; their content is
> unchanged.
>
> **Reordered 2026-08-23.** The interface tidy moved ahead of the Earth work — it's now commit 02 and
> Earth is 03. History was rewritten (nothing was pushed) so the tidy genuinely lands first. This wasn't
> cosmetic: `EarthWeapon` was rewritten against the clean interface and dropped from eight implemented
> methods to four, which is the shape every later element inherits.

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
Commit 03 (+ Earth)      →  Wind, Fire, Ice, Lightning, Earth   5 arcs, 72°
Commit 08 (+ Light)      →  … , Light                           6 arcs, 60°
Commit 10 (+ Dark)       →  … , Dark                            7 arcs, ~51°
```

> **Superseded 2026-08-23.** This section previously said to *swap* rather than append, on the theory
> that 7 arcs would recreate the "hitting the desired colour feels random" complaint. Reverted by
> decision: keeping every element equipped is what the ring is for, and swapping meant no playtest ever
> exercised more than four. **Arc size is explicitly not a concern** — shrinking to ~51° at 7 arcs is
> accepted, and is not a reason to revisit the loadout.

Arc order follows list order starting at +X counter-clockwise, so an appended element takes the next
arc round and every previously-learned flick direction shifts. Expect the existing four to feel
different each time the count changes.

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

## R — Interface tidy

> *Not a feature. `IElementalWeapon` is named as if every element is a melee weapon, and forces each
> weapon to implement hooks it has no use for. Both are cheap to fix, and get more expensive with each
> new element that inherits the problem.*

*R is its own section because the refactor is cross-cutting, not part of Earth. It lands **before** any
element work so every weapon after it is written against the clean interface.*

### Commit 02 — Tap/charge naming and default members ✅

| | |
|---|---|
| **Changes** | `IElementalWeapon` — `MeleeStrike` → `OnTap`, `MeleeCharge` → `OnCharge`; call sites in `ElementManager` and `PlayerController` follow |
| **Adds** | Default no-op implementations for everything except `OnTap`: `OnCharge`, `OnMeleeHit`, `Cleave`, `OnRangedFlight`, `OnRangedHit`, `OnBuffStart`, `OnBuffEnd` |
| **Removes** | Seven now-redundant overrides — Physical and Ice each had an empty `OnCharge` plus empty `OnBuffStart`/`OnBuffEnd`; Wind had an empty `OnCharge`. Also a stray `Debug.Log` that fired on every tap |
| **Mechanism** | C# default interface members — **already used in this codebase** by `TryOverrideDash`, so this is an established pattern, not a new one. `ElementManager` dispatches through the interface reference, which is what makes the defaults reachable |
| **Why not split into melee/ranged interfaces** | Considered and rejected. Every element — melee or ranged — is driven by the same input shape: **tap, hold, release**. A split would put a type-test at every call site and an "implements neither" fallback in the dispatcher, while both branches still called the same two methods. The names were wrong, not the structure |
| **Careful** | `AttackKind.MeleeStrike`/`MeleeCharge` is a **different enum**, used for damage attribution. It is deliberately untouched — a blind find-and-replace breaks it |
| **Risk** | `OnMeleeHit` now defaults to a no-op, i.e. **no damage**. Any weapon that spawns a melee hitbox must override it. A weapon that forgets deals zero damage, which is loud in playtest rather than silent in production |
| **Playtest** | ⚠️ **None — pure refactor.** The second commit in this plan that can't meet the playtest gate, and for the same reason as commit 01: it's plumbing |
| **Regression check** | Every one of the five existing elements still taps and charges **identically**. This is the whole test — a rename that changes behaviour has failed |
| **Not in commit** | Any behaviour change; any new element |

**Editor pass 2026-08-23:** compiles clean, EditMode 73/73. Verified by reflection that `OnTap` is the
only required member and all eight others carry defaults, and that `AttackKind` still reads
`MeleeStrike, MeleeCharge, Ranged`.

> Rule 5 said don't touch `IElementalWeapon`. **Lifted 2026-08-23** — the rule existed to stop a
> speculative refactor around the dormant sword-throw hooks, which this doesn't touch. Renaming and
> adding defaults leaves the ranged hooks exactly where they are.

---

## M1 — Earth / Ballista Turret

> *Fantasy: immovable firepower. Charging plants you, a ballista rises at your feet, and the left stick
> aims it. Tap is a weak uncharged shot; the charge is the real attack.*

### Scope boundary

| In M1 | Out of scope |
|---|---|
| `EarthWeapon`, charge ramp, movement-lock aim mode | Light/Dark weapons |
| The ballista actor (transient, lasts the charge) | A **placed** turret that persists and fires on its own |
| Pierce support on `PlayerProjectile` | Earth enemies (player-only, by decision) |
| | Earth entries in the damage matrix — Earth is 1× by decision |
| | Ultimate / sword throw |

> **The ballista is transient**, not placed: it appears for the duration of the charge and goes away on
> release. A turret you plant and walk away from is a summon, not a weapon — it would need its own
> lifetime, targeting and threat handling, and it would collide with Dark's minions. Recorded here
> because "spawn the turret" reads both ways.

### Commit 03 — Earth is selectable and shoots ✅

| | |
|---|---|
| **Adds** | `Assets/Scripts/Weapon Implementations/EarthWeapon.cs` — implements `IElementalWeapon`; tap fires one bolt via `PlayerProjectile`. `EarthBolt.prefab` (copied from `WindDart`; `Launch` tints it amber automatically, so no hand-authored colour) |
| **Changes** | `CoreSystems.prefab` — `Element Manager/Earth` node with `EarthWeapon` + an appended `elementalWeapons` entry; `Gear.prefab` + `GearManager`'s C# default — `startingLoadout` **appends** Earth, 4 arcs → 5 |
| **Mechanism** | `MeleeStrike` fires the bolt and returns the cooldown. No charge ramp yet — press and release, fixed damage. The bolt flies dead straight: no homing, since aiming is what commits 03–04 are built on |
| **How to reach it** | Flick the right stick toward the Earth arc — the new 5th arc, last in ring order |
| **Playtest** | Flick to Earth → the joystick ring and charge VFX turn **amber**. Tap → a bolt fires forward and damages the first enemy it hits |
| **Regression check** | Wind, Fire, Ice and Lightning all still selectable and behave as before. ⚠️ **Every flick direction has moved** — arcs are 72° not 90°, so all five sit in new places. Judge the elements by which arc lights up, not by muscle memory |
| **Not in commit** | Movement lock, pierce, charge scaling |

### Commit 04 — Earth grounds you and aims ✅

| | |
|---|---|
| **Adds** | `IAimLockProvider`; `IMeleeChargeProvider` + `IAimLockProvider` on `EarthWeapon` (charge indicators light up for free); a movement-lock aim mode on `PlayerController`; `AimMode.Ranged` on `PlayerAimIndicator` so the direction is readable before you commit; a **two-phase charge** — 0.5s ballista construction, then the shot charges |
| **Removes** | **Earth's tap attack.** The weak bolt competed with the charge for the same job at range, so the strong play was to spam it and never stand still — the opposite of the fantasy |
| **Changes** | `PlayerController.MoveInDirection` — while Earth is charging, the left stick aims instead of moving; `ReleaseChargeAttack` now cancels the charge when the attack is swallowed, and discards charges shorter than the tap window; charge damage ramps **uncapped** with hold time; the hold interaction now validates on the **press** rather than 0.3s later |
| **Mechanism** | **The lock is pulled, not pushed.** `PlayerController` polls `ElementManager.IsAimLocked` every frame instead of being told when to lock and unlock. However the charge ends, the weapon stops reporting the lock and movement returns — no exit path has to know the root exists. The bolt launches along the aim direction, not the facing direction |
| **Playtest** | Hold to charge → **you stop moving**; the left stick now swings the aim. Release → the bolt fires where you aimed. This is the whole "immovable firepower" fantasy, and the first commit where Earth stops being "Fire but straight" |
| **Regression check** | Movement is normal for every other element, and normal for Earth when *not* charging. Getting hit or dying mid-charge must not leave you stuck |
| **Risk** | Movement lock is the one change reaching outside the weapon into `PlayerController`. Every exit path — cancel, damage, death, node change, **and a future dash** — must clear it |
| **Decision** | **A dash cancels the root.** The dash is currently dead code (see Known adjacent issues), so this can't be tested yet — but the pull model means a revived dash that cancels the charge clears the root for free, without touching this code |
| **Not in commit** | The ballista visual (so construction is currently a blank half-second), the shot becoming a laser beam, pierce |

> **Re-sliced 2026-09-06 after playtest discussion.** Four changes, all driven by the same problem — the
> weak tap made standing still pointless:
>
> 1. **Earth has no tap attack.** The charge is the whole weapon.
> 2. **The charge runs in two phases** — 0.5s building the ballista, then charging the shot. Releasing
>    during construction fires nothing, which is what makes the commitment real.
> 3. **Damage is uncapped**, ramping for as long as you hold. The only limit is how long you dare stand
>    rooted. This replaces the charge *tiers* that were scheduled for 06.
> 4. **The root starts on the press**, not 0.3s later. `SecondsBeforeHoldValidated` went to zero, so a
>    quick press now starts a charge too — `ReleaseChargeAttack` discards charges shorter than the tap
>    window so the shared button can't fire twice. ⚠️ **This affects every element**: Fire and Lightning
>    charges also begin on the press now. More responsive, but it is a feel change to them.

**Editor pass 2026-09-06:** compiles clean, EditMode 73/73. Verified by reflection that `EarthWeapon`
implements all three interfaces, that no other weapon picked up `IAimLockProvider`, and that `AttackKind`
still reads `MeleeStrike, MeleeCharge, Ranged`. New serialized fields took their C# defaults on the
prefabs (`maxChargeTime` 0.8, `rangedAimLength` 6).

> **Two things the input layer decided for us.** `ChargeAttack` fires `started` only after the hold
> interaction validates at 0.3s, so **a plain tap never roots the player** — no flicker on tap. And
> `MoveInDirection` is event-driven (`HandleMove` fires only when the stick *changes*), so both lock
> edges act immediately in `Update`: without that the player would slide through the root on entry and
> stay frozen after it ended until they moved the stick again.
>
> **A stranding bug found and fixed here.** `ReleaseChargeAttack` only reached the weapon when
> `playerState == MeleeReady && !IsOnAttackCooldown`. Releasing a charge while on cooldown left the
> weapon still reporting its charge — with a root attached, that stranded the player permanently. It now
> cancels the charge on that branch.

### Commit 05 — The ballista appears at your feet

| | |
|---|---|
| **Adds** | Ballista actor prefab + a small controller — spawns on press, plays a **0.5s construction animation** matching the charge's phase 1, then rotates to the aim direction; despawns on release or cancel |
| **Changes** | `EarthWeapon` — the shot launches **from the ballista's muzzle** rather than from the player |
| **Mechanism** | Transient and purely presentational: it owns no targeting, no lifetime beyond the charge, and no collision. Its whole job is to make the root legible from across the screen. `EarthWeapon.IsConstructing` already exposes the phase for the animation to read |
| **Playtest** | Hold → a **ballista builds itself at your feet** over half a second, then swings as you aim. Release → the shot launches from it and it drops away. Construction stops being a blank pause and becomes the reason you're standing still |
| **Regression check** | Cancelling a charge (or dying mid-charge) despawns the ballista — it must not be possible to strand one in the arena |
| **Not in commit** | Pierce, any autonomous firing |

### Commit 06 — The shot becomes a beam and pierces

| | |
|---|---|
| **Adds** | A player-side **laser beam** fired from the ballista instead of a travelling bolt, mirroring `EnemyBeamLaser` rather than inventing a second beam; pierce falls out of the beam naturally — it hits everything on the line |
| **Changes** | `EarthWeapon` — beam length and width scale with charge; the bolt prefab path retires |
| **Playtest** | Line up 3 enemies → long charge → **one beam cuts the whole line**. A short charge is a thin, weak beam |
| **Regression check** | Fire's fireballs and Wind's darts are untouched — the beam is Earth's own path, not a change to `PlayerProjectile` |

> **Re-sliced 2026-09-06.** Charge *tiers* are gone — 04's uncapped ramp replaced them. What's left here
> is the shot's form changing from a projectile to a beam, which is also what makes pierce free.

### Acceptance criteria (M1 done)

- Earth is selectable from the ring and visually reads as amber everywhere (joystick, charge VFX, damage numbers).
- Holding charge visibly roots the player, raises a ballista, and repurposes the left stick as aim.
- A full-charge bolt pierces a line of enemies; a short charge doesn't.
- No other element's movement, damage or projectiles changed.
- Every exit path from the root clears both the lock and the ballista.

---

## Cross-cutting — Auto-aim range

### Commit 07 — Auto-aim range becomes per-element

| | |
|---|---|
| **Problem** | `PlayerWeaponIndicator.targetRadius` is **one number for every element**. It decides how far away an enemy can be and still pull the pointer toward them, so a short-range melee element and a long-range turret currently snap to enemies at exactly the same distance |
| **Adds** | A per-element auto-aim radius, read from the active weapon rather than a single serialized field |
| **Changes** | `PlayerWeaponIndicator.GetFacingDirection` and `RefreshTrackedEnemy` — both take the radius from the active element; each weapon declares its own |
| **Mechanism** | Same capability-interface shape as `IMeleeChargeProvider` and `IAimLockProvider`: an optional member on the weapon, with the current global value as the fallback for weapons that don't care. Nothing has to be touched to keep behaving as it does today |
| **Playtest** | Stand the same distance from an enemy and switch elements. A long-range element should pull the pointer onto them; a short-range one shouldn't. Today both do |
| **Regression check** | Every existing element still auto-aims at its current distance — this commit should be invisible until an element opts into a different radius |
| **Why here** | Earth is the first element whose range differs sharply from the melee four, so it's the first time one shared radius is visibly wrong. Landing it before Light and Dark means both are written against per-element aim rather than retrofitted |
| **Not in commit** | Tuning each element's radius beyond an obvious first pass; anything about the aim *indicator* visuals |

---

## M2 — Light / Harp

> *Fantasy: mark and detonate, ranged burst. Tap: launch a single marking projectile. Charge: summon an
> angel at your position that launches piercing beams at each mark.*

### Design principles

1. **Marks are a status effect, not a bespoke system.** `EnemyEffect` already carries Burn/Chill/Static/
   Buffetted on a shared 1s tick — `Marked` is a fifth entry and inherits the expiry plumbing.
2. **The angel is an actor with a lifetime**, not a weapon mode. It fires and expires on its own.
3. **Beams mirror the enemy Beam Sniper.** `EnemyBeamLaser` is the reference; don't invent a second beam.

### Commit 08 — Tap marks enemies

| | |
|---|---|
| **Adds** | `LightWeapon.cs`; `EnemyEffect.Marked` + its visual; an enumerate-marked query on `ActiveEnemyRegistry` (it only does `TryGetNearest` today) |
| **Changes** | `CoreSystems.prefab` — register `LightWeapon`; `Gear.prefab` — loadout appends Light (6 arcs) |
| **How to reach it** | Flick toward the Light arc |
| **Playtest** | Flick to Light → tap → a projectile flies out and the enemy it hits **visibly carries a mark**. Mark expires on its own after a few seconds |
| **Regression check** | Existing status effects still apply and expire — Fire's burn, Ice's chill, Lightning's static |
| **Not in commit** | The angel; anything that consumes marks |

### Commit 09 — Charge summons the angel

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

Split because the combat and the necromancy are very different sizes — 09/10 are assembly from existing
parts, 11 is a new system. Dark should be playable long before minions land.

### Commit 10 — Dark is selectable, tap swings

| | |
|---|---|
| **Adds** | `DarkWeapon.cs` — arc swing modelled on `PhysicalWeapon` (seek nearest, step in, static hitbox) |
| **Changes** | `CoreSystems.prefab` — register; `Gear.prefab` — loadout appends Dark (7 arcs) |
| **Playtest** | Flick to Dark → tap → a **purple arc swing** that damages enemies in front of you |
| **Regression check** | Physical's swing is unchanged (Dark borrows its shape, it must not share its state) |

### Commit 11 — Charge blinks and cuts a circle

| | |
|---|---|
| **Changes** | `DarkWeapon` — charge release calls `PlayerController.BlinkTo` then a radial hit via `MeleeAugmentUtility.DamageEnemiesInRadius` |
| **Mechanism** | Both primitives already exist — `BlinkTo` from Lightning's Thunderstep, radial damage from cleave |
| **Playtest** | Hold → release → you **teleport a short distance and everything around the landing point takes a hit**. Blinking into a pack should feel like the reward |
| **Regression check** | Lightning's Thunderstep still blinks correctly — `BlinkTo` is now shared by two elements |
| **Not in commit** | Minions |

### Commit 12 — Execution raises a minion

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

- Dark plays as a full melee element without minions (commits 09–10 alone are shippable).
- Dark executions raise minions that fight for you and expire cleanly.
- Waves still clear with minions alive; combo and ultimate charge behave sanely.

---

## Quick reference — what to play after each commit

| After commit | Play this |
|---|---|
| **01** | Nothing new — verify the ring still draws its arcs and flicks are unchanged |
| **02** | Nothing new — every element must tap and charge exactly as before |
| **03** | Flick to Earth → fires an amber bolt |
| **04** | Hold Earth → rooted, ballista builds 0.5s, left stick aims, damage ramps uncapped. No tap attack |
| **05** | Hold Earth → a ballista builds itself at your feet and swings with your aim |
| **06** | Long Earth charge → a beam cuts a line of 3 |
| **07** | Switch elements at a fixed distance → auto-aim reaches further on long-range elements |
| **08** | Flick to Light → tap → enemies visibly marked |
| **09** | Mark 3 → charge → angel beams them all |
| **10** | Flick to Dark → tap → purple arc swing |
| **11** | Dark charge → blink + circle cut |
| **12** | Dark execution → the corpse fights for you |

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
  **Decided 2026-08-23:** whenever it returns, **a dash cancels the Earth root** — see commit 04.
- ~~**Element grants are timed, not persistent.**~~ **Resolved 2026-08-23** — timed is the intended
  model and this is no longer an open question. See Decision 5.

---

## Decisions already taken

1. **New elements are matrix-neutral** — 1× both directions. **Confirmed 2026-08-23**: this is the
   settled answer, not a placeholder. Earth/Dark/Light differ by *weapon behaviour*, not by damage
   multipliers. Supersedes the old M1 scope line that listed "Earth entries in the damage matrix" as
   in-scope — the two contradicted each other, and this is the survivor.
2. **Player-only elements** — no Earth/Dark/Light enemies; enemy prefabs, catalog knobs and wave
   composition stay at five elements.
3. **Dynamic gear arcs** — the ring shows equipped elements only, appended as they land. Arc size
   shrinking toward ~51° is accepted and not a concern.
4. **`IElementalWeapon` gets tidied, not split** — rename to tap/charge + default members (commit 02).
   A melee/ranged interface split was considered and rejected: every element shares one input shape.
5. **Element grants stay timed** — `ApplyEmpowerment` with `imbueDuration`, expiring back to `Physical`,
   is the intended model. **Confirmed 2026-08-23**; no longer an open question.
6. **A dash cancels the Earth root** — fixed now even though the dash is unreachable, so reviving it
   can't silently strand a rooted player.
7. **The ballista is transient** — it exists for the duration of the charge. A placed, persistent turret
   is explicitly out of scope for M1.

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

*Last updated: 2026-09-06 — commits 01, 02 and 03 landed and editor-verified; 01 and 03 await the
play-mode feel check. Plan revised in discussion: matrix-neutral confirmed, timed grants confirmed, arc
size accepted, interface tidied rather than split and reordered ahead of all element work, M1 re-sliced
into four beats around a transient ballista. Say **"start commit 04"** (or a later number) and we apply
only that slice and give you the exact play steps.*
