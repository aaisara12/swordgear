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
M3  Dark / Scythe — arc swing, blink-circle charge, then minion conversion
M2  Light / Harp — gambling: random tunes, a hot streak, a strummed charge
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
| 05a | Charge phases are named on screen | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 05 | The ballista appears and builds itself | ☐ | ☐ | ☐ |
| 06 | The shot is a rock that bursts on impact | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 07 | Auto-aim range becomes per-element | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 08 | Dark selectable, tap arc swing | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 09 | Dark charge blinks + circle swing | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 10 | Dark execution raises a minion | ✅ landed | ✅ 2026-09-06 | ⏳ not yet |
| 11 | Light on the ring, in opalite (Flurry) | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 12 | The harp sounds | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 13 | Strike & Resonance | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 14 | Lullaby & Fermata | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 15 | Hot streak — Fortissimo & Allegro | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 16 | Presto — move-speed streak | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 17 | The arsenal — owned tunes, starters, family-first roll | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 18 | "Learn a tune" — the repeatable augment | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 19 | Staccato & Rest — two more tunes | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 20 | Grand Chord jackpot — every owned tune at once | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |
| 21 | Flourish — the charge | ✅ landed | ✅ 2026-10-03 | ⏳ not yet |

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
Commit 08 (+ Dark)       →  … , Dark                            6 arcs, 60°
Commit 11 (+ Light)      →  … , Light                           7 arcs, ~51°
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

### Commit 05a — Charge phases are named on screen ✅

| | |
|---|---|
| **Adds** | `IElementalWeapon.ChargePhaseLabel` (empty by default); Earth returns `BUILDING TURRET` then `CHARGING TURRET`; `ChargePhaseLabel` on the player renders it above their head |
| **Mechanism** | The label is built in code, so it needs no prefab or art. Mirrors `MeleeChargeWorldIndicator`: poll the active weapon in LateUpdate, create the visual on first use, hide when there's nothing to say. Positioned in world space each frame because the player rotates to face attacks |
| **Why it exists** | Commit 04 made releasing during construction fire nothing, but the two phases look identical, so that rule was invisible and read as a bug. This is the cheapest honest fix — **placeholder art would have been worse than words** |
| **Playtest** | Hold Earth → **BUILDING TURRET** for half a second, then **CHARGING TURRET**. Release during the first and nothing fires; release during the second and it does |
| **Regression check** | Every other element shows nothing — verified all five return an empty label |
| **Superseded by** | Commit 05. Once the ballista actor shows the phases visually, this label is redundant and should be reconsidered |

---

### Commit 05 — The ballista appears at your feet

| | |
|---|---|
| **Adds** | Ballista actor prefab + a small controller — spawns on press, plays a **0.5s construction animation** matching the charge's phase 1, then rotates to the aim direction; despawns on release or cancel |
| **Changes** | `EarthWeapon` — the shot launches **from the ballista's muzzle** rather than from the player |
| **Mechanism** | Transient and purely presentational: it owns no targeting, no lifetime beyond the charge, and no collision. Its whole job is to make the root legible from across the screen. `EarthWeapon.IsConstructing` already exposes the phase for the animation to read |
| **Playtest** | Hold → a **ballista builds itself at your feet** over half a second, then swings as you aim. Release → the shot launches from it and it drops away. Construction stops being a blank pause and becomes the reason you're standing still |
| **Regression check** | Cancelling a charge (or dying mid-charge) despawns the ballista — it must not be possible to strand one in the arena |
| **Not in commit** | Pierce, any autonomous firing |

### Commit 06 — The shot is a rock that bursts on impact ✅

| | |
|---|---|
| **Adds** | Opt-in `explosionRadius` + `EnableExplosion` on `PlayerProjectile`; `EarthRock.prefab` (renamed from `EarthBolt`, so the GUID and every reference survive) using `explosion_normal` for its impact |
| **Changes** | `EarthWeapon` — bolt fields become rock fields via `FormerlySerializedAs`; speed 11 → **26**; size and blast radius scale with charge and are **capped**, while damage stays uncapped |
| **Mechanism** | An exploding shot deals *all* its damage through the blast — the direct hit is skipped so it can't double-dip on whatever it struck. `PlayerProjectile` already had a `Detonate()` hook, so this is opt-in and 0 for everything else |
| **Playtest** | Long charge → a **fast, heavy rock** that bursts and clears a cluster. A short charge is a small rock with a small blast |
| **Regression check** | Fire's fireballs and Wind's darts have `explosionRadius` 0 and behave exactly as before — they still consume on hit and damage only what they strike |
| **Known gap** | **No rock sprite exists**, so it's still the placeholder circle, just bigger and round instead of a stretched capsule |

> **Re-sliced twice, 2026-09-06.** Charge *tiers* went first — 04's uncapped ramp replaced them. Then the
> beam went: it was tried and cut in favour of a rock that bursts on impact, which gets the "clears a
> group" payoff from area rather than from a line, and needs no new beam actor.

### Acceptance criteria (M1 done)

- Earth is selectable from the ring and visually reads as amber everywhere (joystick, charge VFX, damage numbers).
- Holding charge visibly roots the player, raises a ballista, and repurposes the left stick as aim.
- A full-charge bolt pierces a line of enemies; a short charge doesn't.
- No other element's movement, damage or projectiles changed.
- Every exit path from the root clears both the lock and the ballista.

---

## Cross-cutting — Auto-aim range

### Commit 07 — Auto-aim range becomes per-element ✅

| | |
|---|---|
| **Problem** | `PlayerWeaponIndicator.targetRadius` is **one number for every element**. It decides how far away an enemy can be and still pull the pointer toward them, so a short-range melee element and a long-range turret currently snap to enemies at exactly the same distance |
| **Adds** | `IElementalWeapon.AutoAimRadius`, defaulting to the shared 5; `EarthWeapon.autoAimRadius`, serialized at **10** |
| **Changes** | All three of `PlayerWeaponIndicator`'s radius reads go through the active element; its serialized field becomes the fallback for before an element is active |
| **Mechanism** | A default interface member rather than a separate capability interface — reach is something every weapon has, not an optional extra, so there's nothing to type-test. The five melee elements inherit the old value and are provably unchanged |
| **Playtest** | Stand the same distance from an enemy and switch elements. A long-range element should pull the pointer onto them; a short-range one shouldn't. Today both do |
| **Regression check** | Every existing element still auto-aims at its current distance — this commit should be invisible until an element opts into a different radius |
| **Why here** | Earth is the first element whose range differs sharply from the melee four, so it's the first time one shared radius is visibly wrong. Landing it before Light and Dark means both are written against per-element aim rather than retrofitted |
| **Not in commit** | Tuning the other five elements away from 5; anything about the aim *indicator* visuals |

**Editor pass 2026-09-06:** compiles clean, EditMode 73/73. Verified by reflection that Physical, Fire,
Ice, Lightning and Wind all still report 5 while Earth reports 10, and that the prefab picked up the new
field. Note `PlayerAimIndicator.rangedAimLength` is still 6, so Earth's pointer can snap to an enemy
slightly past the end of the drawn line — the line shows direction, not range.

---

## M3 — Dark / Scythe  *(brought forward — see note)*

> *Fantasy: summoner, necromancer theme. Weapon: scythe. Tap: swing scythe in an arc. Charge: blink and
> swing in a circle. Execution with dark melee resummons the enemy as a minion. Minions have constant
> health drain.*

Split because the combat and the necromancy are very different sizes — 09/10 are assembly from existing
parts, 11 is a new system. Dark should be playable long before minions land.

### Commit 08 — Dark is selectable, tap swings ✅

| | |
|---|---|
| **Adds** | `DarkWeapon.cs` — arc swing modelled on `PhysicalWeapon` (seek nearest, step in, static hitbox); `DarkSlashEffect.prefab`, a pre-tinted purple copy of `BasicSlashEffect` |
| **Changes** | `CoreSystems.prefab` — `Element Manager/Dark` + register; `Gear.prefab` — loadout appends Dark, 5 arcs → **6** (60°) |
| **Mechanism** | Reuses Physical's `PhysicalSlash` hitbox, which is safe because its sprites are empty and `PlayerHitbox` routes damage through `ElementManager` to whichever element is active. The slash *effect* is a separate prefab rather than a runtime tint — pools are keyed by prefab, so tinting the shared instance would eventually hand Physical a purple slash |
| **Careful** | `OnMeleeHit` is overridden, not defaulted. The interface default is a no-op, so a weapon that spawns a hitbox and forgets it deals zero damage |
| **Playtest** | Flick to Dark → tap → a **purple arc swing** that damages enemies in front of you |
| **Regression check** | Physical's swing is unchanged (Dark borrows its shape, it must not share its state) |

### Commit 09 — Charge blinks and cuts a circle ✅

| | |
|---|---|
| **Adds** | `IMeleeChargeProvider` on `DarkWeapon` (charge indicators for free); `darkcleave.prefab`, a pre-tinted copy of `basiccleave` |
| **Changes** | `DarkWeapon` — charge release calls `PlayerController.BlinkTo` then a radial hit via `MeleeAugmentUtility.DamageEnemiesInRadius` |
| **Mechanism** | Both primitives already exist — `BlinkTo` from Lightning's Thunderstep, radial damage from cleave. `BlinkTo` brings i-frames and the dash cooldown with it, which is what makes landing inside a pack survivable |
| **Careful** | `BlinkTo` sets position outright with **no collision check**, so the destination is raycast against the Arena layer first and lands short of a wall. LowWall is excluded — shots already pass through it, so the blink should too |
| **Contrast with Earth** | Dark deliberately does **not** implement `IAimLockProvider`. Its charge is a repositioning tool, so rooting would fight the thing the charge is for |
| **Playtest** | Hold → release → you **teleport a short distance and everything around the landing point takes a hit**. Blinking into a pack should feel like the reward |
| **Regression check** | Lightning's Thunderstep still blinks correctly — `BlinkTo` is now shared by two elements |
| **Not in commit** | Minions |

### Commit 10 — Execution raises a minion ✅

| | |
|---|---|
| **Adds** | `DarkMinion` (a "shade": hunts the nearest enemy, damages on contact, drains away); `DarkShade.prefab` |
| **Changes** | `DarkWeapon.OnMeleeHit` — a killing blow raises a shade wearing the dead enemy's sprite |
| **Mechanism** | ⚠️ **Not conversion.** The enemy dies a completely normal death and a *separate* actor is raised in its place. `EnemyController` is untouched |
| **Playtest** | Kill an enemy with Dark melee → it **gets back up on your side**, hunts the nearest enemy, and visibly thins as its health drains, collapsing after **8s**. Enemies ignore it entirely |
| **Health drain** | The shade's only threat is its own constant drain — enemies never target or damage it. Its health is drained from full to nothing over `drainSeconds` (8), and the sprite thins with it so the time left is readable. At most 4 live at once |
| **Regression check** | Non-Dark kills still just die. Minions must not count as live enemies for **wave-clear**, or the arena will never complete |
| **Why not conversion** | Every risk this commit was flagged for came from keeping the enemy alive on the player's side. Killing it for real removes them by construction: wave clear counts `LevelLoader.activeEnemies` until objects are destroyed, so a converted enemy would hold the wave open **forever**; the player's auto-aim reads `ActiveEnemyRegistry`, which a shade never joins; and nothing has to reliably neuter enemy AI across **26 prefabs** with differing strategies. Combo and ult credit stay correct because the kill really is a kill |
| **Careful** | The shade is deliberately **not tagged `Enemy`** — `PlayerHitbox` reacts to that tag, so a tagged shade would be hit by the player's own swings. Its collider is a trigger so it can't shove anyone. Its damage passes `feedsCombo:false`, matching DoT ticks: a shade acting on its own must not keep the player's combo alive |
| **EditMode (optional)** | Wave-clear ignores converted enemies |

### Acceptance criteria (M3 done)

- Dark plays as a full melee element without minions (commits 08–09 alone are shippable).
- Dark executions raise minions that fight for you and expire cleanly.
- Waves still clear with minions alive; combo and ultimate charge behave sanely.

> **Redesigned 2026-10-03.** Mark-then-detonate was shelved on 2026-09-06 because it read as *delayed AoE*,
> which Fire's bomb cascade already covers ("a ranged attack like Fire, with more steps"). That version
> is parked on the tag **`light-marks-parked`**. Light is now the gambling harp below.

## M2 — Light / Harp  *(Opalite · Gambling)*

> *Fantasy: gambling. Weapon: harp. Tap: play a random tune — an offensive spell, a weak heal, or a buff.
> Charge: a burst of random tunes.* Light becomes the roster's only **support / sustain** element, and its
> identity is variance: you don't choose what you get, you choose when to pull the lever, and you nurse
> a lucky streak.

### The tunes

Every tune **shows its note above the player** as it plays (the "reel reveal"), sounds its own short
melody, and the note value *means* something: quick notes are many quick hits, long notes are sustained
effects, dynamic markings are intensity.

| # | Tune | Note | Family | Effect | Starter? |
|---|---|---|---|---|---|
| 1 | **Flurry** | four beamed 16ths | Offense | 4 small homing notes at the nearest enemies | ✅ |
| 2 | **Strike** | quarter note | Offense | one heavy straight note that bursts on impact | |
| 3 | **Resonance** | two beamed 8ths | Offense | two sound waves roll out across the room, hitting each enemy as they pass | |
| 4 | **Staccato** | two quarter notes with staccato dots | Offense | a ring of short, detached notes bursts outward all around you | |
| 5 | **Lullaby** | whole note | Sustain | heal ~12% max HP over 3s — the sustained note | ✅ |
| 6 | **Fermata** | half note + fermata | Sustain | ~1.5s invulnerability — the note "holds" you | |
| 7 | **Rest** | quarter rest | Sustain | everything around you is stilled — nearby enemies slowed for ~2s | |
| 8 | **Fortissimo** | ff | Streak | +10% damage per stack · overhead icon: sword | ✅ |
| 9 | **Allegro** | eighth note | Streak | +10% attack speed per stack · overhead icon: metronome | |
| 10 | **Presto** | thirty-second note | Streak | +10% move speed per stack · overhead icon: wing | |
| ★ | **Grand Chord** | stacked chord | Jackpot | ~3%: plays **every tune you own** at once | always |

- **One stat per streak tune**, at +10% per stack (halved from the first pass, which was too strong).
  Each has its own overhead icon with a chevron per stack, so the icon *is* the stat.
- **The arsenal.** A run starts with one tune per family (Flurry, Lullaby, Fortissimo). Every other tune
  is learned through **one repeatable augment, "Learn a tune"**: each purchase teaches a random tune you
  don't know yet, and it stops being offered once you know them all. The gamble starts in the shop.
- **Odds:** the roll picks a **family** evenly among the families you own a tune in, then a tune within
  it. So it's even thirds from the first tap and stays even as the arsenal grows, with no weights to
  rebalance per tune. The jackpot is its own ~3% roll ahead of that, and gets better as you learn more.
- **Hot streak:** Streak tunes stack (max 3) and last **until you take damage** (20s safety cap,
  refreshed per stack). Getting hit busts the streak; Fermata exists partly to protect it.
- **Tap** ~0.45s cooldown. **Charge — "Flourish":** stays mobile (Dark's shape, no aim lock); release
  strums 2 tunes at a short hold up to 5 at full (~0.9s), 0.1s apart.
- All numbers are first-pass and serialized on the tune assets.

### Design principles

1. **Tunes are data.** Abstract `HarpTune : ScriptableObject` (family, weight, glyph); one subclass per
   *kind* of effect (`ProjectileTune`, `WaveTune`, `HealTune`, `WardTune`, `StreakTune`, `JackpotTune`), one asset per tune under `Assets/Visuals/Light/Tunes/`. Flurry, Strike and Staccato are
   three assets of one class; Resonance and Rest are both `WaveTune`.
2. **Streaks are player-level.** Light is a 5s imbue and switching element ends the old weapon's buffs,
   so a weapon-local buff would die with the imbue. The streak layer lives in `PlayerStatModifiers` and
   sits *on top of* augments, so an augment pickup (`ReapplyFromBlob`) can't wipe it.
3. **Opal is a shader, not a colour.** `Swordgear/Opalite` — milky base, pastel sheen and twinkling
   play-of-colour flecks, all driven by world position + time (the gear arc mesh has no UVs). The flat
   palette entry (lilac pearl `0.95, 0.86, 1.0`) is the stand-in for everything that can only take a colour.
4. **Notes are real notation; stat icons are icons.** The tune reveal is hand-authored notation
   (`Assets/Visuals/Light/Notes/`); a live streak is shown by a plain stat icon (`Assets/Visuals/Light/Icons/`
   — sword, metronome, wing, chevron) with its outline baked in. All imported as Textured Sprites at one
   shared scale (1 SVG unit = 0.012 world units), drawn white so the opal shader colours them.
5. **Light's feedback lives over the player, not on the HUD.** A HUD panel was tried and dropped as too
   loud for something one element uses. Over-head icons carry the state; floating callouts carry the
   exact numbers at the moment you earn them.
6. **Ownership is inventory.** Learned tunes are inventory items on the player blob, so they persist
   across nodes and are wiped with everything else on a new run, with no new save path.

### Commit 11 — Light is on the ring, in opalite ✅

| | |
|---|---|
| **Adds** | `Opalite.shader`; `Opalite` / `OpaliteAdditive` / `OpaliteArc` materials; `HarpTune` + `ProjectileTune` + `LightWeapon`; `Flurry.asset`; `FlurryNote` and `HarpReveal` prefabs + `HarpReveal_Pop` clip; 16th and beamed-16ths SVGs |
| **Changes** | `GearManager` — per-element arc material override (Light → `OpaliteArc`); `ElementVisuals` / `ElementVisualUtility` — Light becomes lilac pearl; `CoreSystems.prefab` — registers `LightWeapon`, loadout appends Light (**7 arcs, ~51°**) |
| **Playtest** | Flick to Light: the arc shimmers opal. Tap → four beamed sixteenths pop above you and four small sixteenth notes home into the nearest enemies |
| **Regression check** | Every other arc still draws with its flat colour; every flick direction has shifted (7 arcs) |

### Commit 12 — The harp sounds ✅

| | |
|---|---|
| **Adds** | `Assets/Audio/Light/harp_pluck.wav` — one C5 pluck, Karplus-Strong synthesized offline with the Python stdlib (soft fingered excitation, a pick-position comb, a faintly detuned sympathetic string); `Sound.Harp_Pluck` (appended) + its `MainAudioLibrary` entry on the SFX group; `HarpNote` melodies on `HarpTune` |
| **Changes** | `LightWeapon` — plays the rolled tune's melody alongside its effect; Flurry's melody is C–E–G–C rising, 0.06s apart to match its volley |
| **Mechanism** | Every pitch is the one sample re-pitched by `2^(semitones/12)`. That also shortens high notes, the way short harp strings die faster |
| **Playtest** | Tap → a quick rising four-note run, one pluck per note leaving |

### Commit 13 — Strike & Resonance join the roll ✅

| | |
|---|---|
| **Adds** | `WaveTune` + `HarpWave`; `Strike.asset` (a `ProjectileTune`) + `StrikeNote.prefab`; `Resonance.asset` + `ResonanceWave.prefab` and its `ResonanceWave_Travel` clip; quarter-note, beamed-eighths and wave-ring SVGs |
| **Strike** | One straight, non-homing quarter note (1.8× base) that bursts for 2.2 on impact through `PlayerProjectile.EnableExplosion`, Earth's proven burst. It hits harder than Flurry's 4 × 0.35 because it can miss. Sounds a low open fifth, C4 + G4 together |
| **Resonance** | Two opal sound waves roll out from where you played them, 0.15s apart, each reaching 8 (just inside the gear ring) in 0.5s and dealing 0.5× base to every enemy **as its front passes them**. Needs no target and reaches most of the room, so it pays out whether you're swarmed or not. Sounds C4 then G4, one pluck per wave |
| **Why a wave** | It began as a point-blank 3.5 burst, but that's melee reach: it only paid when enemies were already on you, and a random tune that rolls a dud in most situations feels like losing the gamble. The burst ring also faded from 55% of its growth, so it read smaller than its damage |
| **Mechanism** | The expansion is an authored, normalised AnimationClip. `HarpWave.Play` stretches it to `travelSeconds` and sizes it to `radius` (DarkMinion's drain-sync idiom), and `WaveTune` reads the animated front back each frame to decide who's been hit. The ring on screen *is* the hit test, so retiming or re-easing the clip moves the damage with it |
| **Careful** | `CatchExplosionFX`'s ring tops out at radius ~1.2 at scale 1 (`_MaxRadius` 0.95 of a 2.5-unit quad), so Strike's burst scales from **1.2**, not the 1.5 Earth's rock uses. The ring you see is the area that took damage |
| **Odds** | Three Offense tunes at weight 1 each: a measured 33.8 / 32.9 / 33.3% over 30k rolls |
| **Playtest** | Taps vary: a heavy quarter note that bursts; two opal waves rolling out across the room, hitting enemies as they pass |

### Commit 14 — Lullaby & Fermata ✅

| | |
|---|---|
| **Adds** | `HealTune`, `WardTune`, `HarpAura` (+ `HarpAuraUtility.Wear`); `Lullaby.asset`, `Fermata.asset`; `LullabyAura.prefab` (particles, `OpaliteEighthNote.mat`), `FermataWard.prefab` + `FermataWard_Hold` clip; whole-note, half-note-with-fermata and single-eighth SVGs |
| **Lullaby** | Whole note. Heals 12% of max HP over 3s in 12 even ticks through `PlayerGameplayManager.Heal`, so the bar visibly climbs. Opal eighth notes drift up around you while it plays. Sounds a slow falling C6–G5–E5–C5 |
| **Fermata** | Half note under a fermata. 1.5s invulnerable through `PlayerController.GrantIFrames`, which never shortens a window already running. An opal ward rings you, popping in, breathing and swelling open as it lets go. Sounds an open chord, C4 + G4 + C5, left to ring |
| **Careful** | `GrantIFrames` also runs the player's hurt-blink, so Fermata blinks the sprite; the ward is what tells it apart from being hit. `HarpAura` stretches its clip and sets its emitters' duration to the tune's, so tune data stays the single source of truth for timing. Emitters must keep stop action `None`: the pool turns `Destroy` into a release callback |
| **Odds** | Sustain's two tunes at weight 1.5 match Offense's three at 1, so it's 50/50 by family until Streak lands in 15: 16.7% per Offense tune and 25% per Sustain tune over 60k rolls |
| **Verified** | Lullaby healed exactly 24 of 200 in 2-HP steps every 0.25s. Under Fermata, hits at 0.3s and 1.3s did nothing and one at 1.7s landed |
| **Heal clarity (follow-up)** | Drifting opal notes alone didn't read as healing. Each tick that actually heals now pops a green "+2" off the player (`HealNumber.prefab`, skipped at full HP) and a quiet high harp sparkle; a synthesized `Sound.Heal_Shimmer` (rising C-major bell chime, appended) marks the start; and `PlayerHealthBarUI` flashes the fill green and swells the bar on **any** heal (lifesteal and rest nodes too) |
| **Playtest** | Some taps heal you (HP bar climbs over 3s, notes drift up around you), some make you briefly untouchable (an opal ring holds you) |

### Commit 15 — Hot streak: Fortissimo & Allegro ✅

| | |
|---|---|
| **Adds** | Streak layer in `PlayerStatModifiers` (`StreakStat`, `StreakBonus`, `StreakState`, `AddStreakStack` / `ClearStreaks` / static `OnStreakChanged(StreakChange, id)`); `StreakTune`; `Fortissimo.asset`, `Allegro.asset`; ff SVG; sword, metronome (+ swinging arm) and chevron icon SVGs; `StreakWorldIndicator` + `StreakIndicator.prefab` (on `CoreSystems`) + `StreakCallout.prefab`; `StreakText`; outlined HUD-font material; `PlayerStreakTest` |
| **Fortissimo** | ff, "very loud". +10% damage per stack. Sounds two hammered octaves |
| **Allegro** | Eighth note. +10% attack speed per stack, shortening every element's cooldown through `ElementManager.OnTap` — Light's own taps too, so you pull the lever faster |
| **Streak rules** | Max 3 stacks per streak; each stack refreshes a 20s safety cap; **any damage busts every streak** (`OnHealthChanged` with `Delta < 0`). Cleared on node reset (`PlayerController.ResetForNode`) and new run (`ClearForNewRun`) |
| **Visibility** | Over the player's head, never the HUD. Each live streak is a **stat icon with a chevron per stack**: a sword for damage, a metronome for attack speed whose arm swings faster with every stack. Chevrons climb in a loop, a new stack pops the icon, a bust swells and bursts it. **Callouts** carry the numbers: "+10% DMG" per stack (stacking in lanes at tap speed) and a red "STREAK BROKEN" with a sour low harp cluster on a bust |
| **History** | First pass gave Allegro attack + move speed and used notation glyphs and a HUD panel; reworked on feedback to one stat per tune at half strength, icons instead of text, no HUD. Crescendo's hairpin read as "a sideways pixelated shape" and became Fortissimo's ff |
| **Careful** | Augment values live in backing fields and `DamageMultiplier` / `AttackSpeedMultiplier` / `MoveSpeedMultiplier` are augment + streak, because `ReapplyFromBlob` rebuilds augments from scratch on every pickup (covered by `PlayerStreakTest`). Move speed is applied on stick input, so `PlayerController` replays the stick on `OnStreakChanged`. The icon row is scaled on its `Bob` node, not the icons, whose own scale belongs to their pop/burst clips. Light's reveal moved up to 3.9 to clear the icons |
| **Playtest** | A sword (or metronome) with chevrons appears over your head and grows a chevron per stack; you hit harder or attack faster until you get hit, then it bursts with "STREAK BROKEN" |

### Commit 16 — Presto ✅

| | |
|---|---|
| **Adds** | `Presto.asset` (`StreakTune`, Streak); thirty-second-note SVG (three flags: the fastest note on the harp); wing icon SVG + its indicator group, whose `Wing_Flap` clip beats faster per stack like the metronome |
| **Effect** | +10% move speed per stack, max 3. Sounds a blur of a run, G5 up to C7 |
| **Odds** | Streak tunes now weigh 1 each (three of them = 3, level with Offense and Sustain) until commit 17's family-first roll retires per-family weight balancing. Measured 33.4 / 33.2 / 33.4% |
| **Art note** | The wing took three drafts: a single swept blade read as a leaf, fanned ellipses as pebbles; the pointed-primaries silhouette reads as a wing at in-game size |
| **Playtest** | Roll Presto → a wing with a chevron appears over your head and you move faster; it grows per stack and bursts on a hit |

### Commit 17 — The arsenal ✅

| | |
|---|---|
| **Adds** | `HarpRepertoire` (an `InitializeableUnrestrictedGameComponent` on the Light node, registered with `GameInitializer` in `BootUp`) — which tunes the player owns: the starters plus every learnable tune with a `light-tune-<name>` item in the player blob's inventory. It keeps no state of its own. `HarpTune.PickByFamily` (family evenly, then weight within family); `HarpRollTest` (6 EditMode tests over the logic, tunes built in-test) |
| **Changes** | `LightWeapon` rolls `PickByFamily` over `repertoire.Owned`; its own tune list is gone. Every tune's `weight` reset to 1: weights now only rank tunes *within* a family |
| **Verified** | Fresh run owns exactly Flurry / Lullaby / Fortissimo (33.2 / 33.4 / 33.4%). Learning Strike and Allegro through the live blob adds them, and families stay 32.9 / 33.9 / 33.2% |
| **Starters** | Flurry, Lullaby, Fortissimo — one per family, so the first tap of a run is already even thirds |
| **Playtest** | A fresh run rolls only Flurry, Lullaby and Fortissimo |

### Commit 18 — "Learn a tune" ✅

| | |
|---|---|
| **Adds** | `UpgradeType.Light_LearnTune` (appended); `LearnTuneStoreItem` (an `ElementUpgradeLoadableStoreItem`) + `up_light_learntune.asset` in `AugmentCatalog` — **Silver** tier, cost 100, harp icon SVG; `LoadableStoreItem.IsOfferable` (virtual, default true) honoured by the tier pickers; `HarpRepertoire.OnTuneLearned`; `StreakWorldIndicator.Announce`; 3 more `HarpRollTest` cases |
| **How it works** | The purchase is an ordinary element upgrade: each copy lands in the inventory (element-tagged `@Light|elem-upgrade-Light_LearnTune`) and **stays there**, so the element ledger counts it as a Light augment like any other. `HarpRepertoire` counts copies against learned tunes and, on every inventory change, learns random unknown tunes until they match (idempotent; over-buying stops at everything). Each learn pops the tune's note over the player with a gold "NEW TUNE: STRIKE" |
| **Rules** | `IsOfferable` is false once nothing is left to learn, so it drops out of every offer |
| **Tier** | Silver rather than the Diamond other element upgrades use, so it shows up in regular post-fight offers: it's meant to be taken many times. ~1 in 11 of the Silver pool. A tuning knob |
| **Verified** | Bought through `PurchaseUtility` with a real tagged offer: learned Presto, Light ledger 0 → 1; buying out learned all five in random order, then 0 appearances in 300 Silver offer rolls. HP unchanged across a purchase |
| **Playtest** | Buy it in the shop → next fight, a tune you didn't have starts showing up; keep buying until it disappears from the shop |

### Commit 19 — Staccato & Rest ✅

| | |
|---|---|
| **Adds** | `Staccato.asset` (`ProjectileTune`) + `StaccatoNote.prefab` (an eighth note, 0.55s life); `Rest.asset` (`WaveTune`); staccato (two dotted quarters) and quarter-rest SVGs; `ProjectileTune.ring`; `WaveTune.chillSeconds` |
| **Staccato** | Eight short-lived notes burst out evenly all around you, 0.4× base each. Fired as a `ring` starting straight at the aim: a centred fan of an even count never sends a note at the target or along the axes (the first pass hit only 2 of 4 enemies placed around the player) |
| **Rest** | One slow wave (0.8s to radius 8) that deals nothing and chills every enemy it passes for 2s: 50% slower, through Ice's existing Chill effect (so it also wears Ice's blue tint). No new class: Rest is a `WaveTune` with damage 0. Sounds a soft low C |
| **Unlock** | Both are learned through "Learn a tune", never starters |
| **Playtest** | Staccato sprays notes in every direction; Rest stills the enemies around you |

### Commit 20 — Grand Chord jackpot ✅

| | |
|---|---|
| **Adds** | `JackpotTune` + `GrandChord.asset`; stacked-chord SVG (four heads in thirds on one stem, with an arpeggio line); `HarpTune.Roll` (jackpot first, then the family roll) + a `HarpRollTest` case; `HarpContext.Owned`; `LightWeapon.jackpot` / `jackpotChance` (0.03) |
| **Effect** | ~3% per tap, rolled before and outside the family roll: plays the **effect** of every tune you own at once (each streak gains a stack), with one full strummed chord instead of ten melodies, and a gold "JACKPOT!". Grows with the arsenal: three tunes on a fresh run, ten once everything is learned |
| **Verified** | Forced with all ten owned: three streaks stacked, invulnerable, 10 projectiles, 3 waves, both auras, "JACKPOT!". Natural rate 2.81% over 20k rolls |
| **Playtest** | Rarely, everything you own fires at once |

### Commit 21 — Flourish (the charge) ✅

| | |
|---|---|
| **Adds** | `LightWeapon` implements `IMeleeChargeProvider` (Dark's shape: mobile, no aim lock); `PlayRolledTune` shared by the tap and the strum |
| **Effect** | Hold up to 0.9s, release → strums `round(lerp(2, 5, charge))` tunes 0.1s apart, each a full independent roll (jackpot included), with their reveals fanned side by side (1.4 apart) so every note played can be read. 0.9s cooldown |
| **Verified** | Through the real input path (`BeginChargeAttack` / `ReleaseChargeAttack`): indicators read 0.5 at half hold and max at full; walking at full speed while charging; a full hold put 5 reveals on screen, a 0.12s hold put 2 |

### Acceptance criteria (M2 done)

- Every tap plays something, and you can tell which tune it was from the note alone.
- Families come up in roughly even thirds over a fight, however many tunes are owned.
- A fresh run plays only the three starters; "Learn a tune" grows the arsenal one random tune at a time.
- A streak feels worth protecting, and getting hit visibly ends it.
- Light reads as support / sustain, not as a fourth ranged damage element.

---

---

## Quick reference — what to play after each commit

| After commit | Play this |
|---|---|
| **01** | Nothing new — verify the ring still draws its arcs and flicks are unchanged |
| **02** | Nothing new — every element must tap and charge exactly as before |
| **03** | Flick to Earth → fires an amber bolt |
| **04** | Hold Earth → rooted, ballista builds 0.5s, left stick aims, damage ramps uncapped. No tap attack |
| **05** | Hold Earth → a ballista builds itself at your feet and swings with your aim |
| **06** | Long Earth charge → a fast rock bursts and takes out a cluster |
| **07** | Switch elements at a fixed distance → auto-aim reaches further on long-range elements |
| **08** | Flick to Dark → tap → purple arc swing |
| **09** | Dark charge → blink + circle cut |
| **10** | Dark execution → the corpse fights for you |
| **11** | Flick to Light → opal arc; tap → beamed 16ths pop overhead, four notes home in |
| **12** | Tap → a rising four-note harp run |
| **13** | Taps vary: a bursting quarter note, two waves rolling across the room |
| **14** | Some taps heal (notes drift up, HP climbs), some make you untouchable (an opal ring holds you) |
| **15** | A sword or metronome with chevrons over your head; hit harder / attack faster until hit, then "STREAK BROKEN" |
| **16** | Presto: a wing over your head and you move faster |
| **17** | A fresh run only rolls Flurry, Lullaby, Fortissimo |
| **18** | Buy "Learn a tune" → a new random tune joins the roll; it leaves the shop once you know them all |
| **19** | Staccato sprays notes all around; Rest stills nearby enemies |
| **20** | Rarely, every tune you own fires at once |
| **21** | Hold → release strums 2–5 tunes |

---

## Backlog (in this doc's scope, not started)

- ☐ **Element-switch flourish / "gear arts"** — the doc asks for a visual pop on switch; nothing exists.
  `ElementManager.OnActiveElementChanged` is the hook. Absorbed by milestone P (polish pass) at the end of this doc.
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

---

## P — Polish pass: a living gear and an element switch with real impact

> Planned 2026-10-03, replacing the reminder that stood here. The user's choices: an **animated shader per
> element** for the gear sections, a **big but fast** switch that never pauses play, the **active section drains**
> to show imbue time, **synthesized switch sounds** per element, and: *the whole screen can be involved; make it
> glowy, with VFX and shaders that have real impact.* Absorbs the backlog's element-switch flourish.

### What exists (before P)

- Switching is a flick of the aim stick at an arc: `PlayerController.GrabElementFromGear` →
  `GearManager.TryGrantElementFromDirection` → `GameManager.ApplyEmpowerment` → `ElementManager.SetActiveElement`
  → static `OnActiveElementChanged`. The only feedback is `Sound.Bounce`. The event also fires on boot and when an
  imbue **expires back to Physical**.
- **An imbue lasts 20s**, not 5: `Gear.prefab` overrides `imbueDuration`. `GameManager.OnEmpowermentTimerChanged`
  ticks every frame, but nothing in the real game shows it.
- Arcs are `GearArcVisual` wedges: vertex colour only, **no UVs**, colour snaps instantly. Only Light has its own
  material. The decorative `gear_thin.png` ring has 8 baked notches against 7 arcs.
- Arena has a global Volume (`Assets/Misc/ArenaPostFX.asset`, bloom threshold 0.9) and HDR on, but every element
  colour is capped at 1.0, so almost nothing blooms. **HDR emission is the lever for glow.**
- The held sword is never element-tinted.

### Design

- **Glow language.** Every element gets an HDR emission colour (`ElementVisuals.GetEmissionColor`), and bloom is
  retuned so only deliberate emission blooms. Everything P adds glows through that one path.
- **The gear.** Arcs gain UVs (u along, v inner→outer). One animated shader per element on a shared
  `ElementFX.hlsl`: Fire flame tongues and embers; Ice frosted facets and glints; Lightning striking bolts;
  Wind streaming currents; Earth glowing strata cracks; Dark drifting void smoke; Light's Opalite upgraded.
  Arc states ease (idle → aimed-at → **active**) through a MaterialPropertyBlock. The active arc **drains** over
  the imbue. A shader **hub** replaces `gear_thin.png`: teeth match the arc count, it takes the element's colour,
  and it clicks round a notch on every switch. Arcs never move, so flick directions stay learnable.
- **The switch** (≤0.4s, never pauses), fired from a new `GearManager.OnElementGranted` (a real flick, not boot or
  expiry): the arc flares and streaks energy into the player; an element burst blooms (one authored prefab per
  element); a tinted shockwave rolls across the arena; the **screen edges** flare in the element, then settle to a
  faint tint while imbued; a post-FX pulse (bloom spike, chromatic aberration, colour nudge); camera kick, light
  flash, and the element's own sound.
- **Imbued player.** The held sword takes the element (tint and HDR glow); a low-rate ambient aura; a flicker in
  the last seconds; an audible, visible fizzle on expiry.
- **Authoring.** Bursts, auras, vignette and hub are prefabs, materials and AnimationClips made in the editor;
  scripts only listen and set parameters. Shaders are hand-written URP 2D HLSL. Mobile-conscious throughout.

### Status

| Commit | Title | Committed | Editor pass | Verified in play |
|---|---|---|---|---|
| P1 | Glow foundation — HDR emission, bloom retune, `ElementFX.hlsl`, arc UVs, eased arc states | ☐ | ☐ | ☐ |
| P2 | Fire & Ice sections | ☐ | ☐ | ☐ |
| P3 | Lightning & Wind sections | ☐ | ☐ | ☐ |
| P4 | Earth & Dark sections, Light upgraded | ☐ | ☐ | ☐ |
| P5 | Imbue timer on the gear | ☐ | ☐ | ☐ |
| P6 | The hub | ☐ | ☐ | ☐ |
| P7 | Switch burst — director, flare + streak, shockwave, camera, light; Fire/Ice/Lightning bursts | ☐ | ☐ | ☐ |
| P8 | Bursts for Wind/Earth/Dark/Light | ☐ | ☐ | ☐ |
| P9 | Whole screen — element vignette and post-FX pulse | ☐ | ☐ | ☐ |
| P10 | Switch sounds | ☐ | ☐ | ☐ |
| P11 | Imbued player — sword glow, aura, flicker, expiry fizzle | ☐ | ☐ | ☐ |

### Quick reference — what to play after each commit

| After commit | Play this |
|---|---|
| **P1** | Aim across the ring: sections ease brighter and swell, nothing snaps; glows bloom |
| **P2** | Fire's arc licks with flame and embers; Ice's is frosted crystal with glints |
| **P3** | Bolts strike across Lightning's arc; currents stream along Wind's |
| **P4** | Glowing strata cracks; drifting void smoke; opal rims and blooms |
| **P5** | The active section glows and drains over 20s, pulsing near the end |
| **P6** | The centre gear matches the arc count and clicks round as you switch |
| **P7** | Switching fires a streak from the arc into you and a burst |
| **P8** | Every element has its own burst |
| **P9** | The screen edges flare in the element and stay faintly tinted while imbued |
| **P10** | Each element switch has its own sound |
| **P11** | Your sword and aura carry the element; expiry is visible and audible |
