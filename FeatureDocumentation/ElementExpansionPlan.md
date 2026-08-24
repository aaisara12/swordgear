# Element Expansion Plan — Earth, Dark, Light

Plan for the "Core Gameplay" block of the improvements doc, up to (but excluding) Elemental Affinities.
Worked through iteratively; each phase is independently shippable.

Status legend: ☐ not started · ◐ in progress · ☑ done

---

## Already done (do not redo)

The last four commits landed most of this section:

| Doc bullet | Commit | Notes |
|---|---|---|
| Gear becomes a background object | `9d96937` | Bumpers removed; gear no longer collides |
| Right joystick = element selector | `9d96937` | Flick-and-release picks the arc's element |
| Sword no longer thrown | `9d96937` | Throw returns later as the **ultimate** |
| Per-element weapons w/ distinct playstyles | `bcf247f` | Fire, Ice, Lightning, Wind |

**Fire is already a spellblade**, matching the doc — `meleeDamageMultiplier = 0.35f` with `FireballBurst()`
on swing and a 15-bomb cascade at max charge. The `bcf247f` commit message calling it a "gun" is wrong;
the code is right. No change needed.

Existing weapons are structurally distinct, not reskins: Ice stretches its hitbox on local Y for spear
reach, Lightning runs a sheathe/iaido state machine with i-frames and dodge detection, Wind runs a
reflect window that outlives its damage window.

---

## Decisions taken

1. **New elements are matrix-neutral** — 1.0× in both directions against everything, pending the
   Elemental Affinities rework. Avoids building a 7-element rock-paper-scissors we're about to delete.
2. **Player-only elements** — no Earth/Dark/Light *enemies*. Enemy content (prefabs, catalog knobs,
   wave composition) stays at 5 elements.
3. **Dynamic gear arc count** — the ring shows *equipped* elements, not all elements.
4. **Interface untouched** — `IElementalWeapon` keeps its ranged hooks. The sword throw is coming back
   as the ultimate, so `OnRangedFlight` / `OnRangedHit` / `Cleave` / `TryOverrideDash` are dormant,
   not dead. Refactor pass later if it's still awkward.

### Why dynamic arcs

`Gear.prefab` has `arcCount: 4` with a hardcoded loadout. Selection is a directional flick quantized
into arcs, so 4 arcs = 90° targets. Seven would be 51° targets on a phone — and *"hitting the desired
colour feels random"* was already the #2 playtest complaint. Adding elements to a fixed ring makes a
known problem worse.

The improvements doc resolves this in the Ultimate rework (start with 1 element, buy slots), so the ring
should only ever hold equipped elements. We build the dynamic mechanism now with a stubbed loadout, and
the slot-purchase system fills it in later.

Invariant to preserve (from `ApplyDefaultLoadout`'s comment): **every arc must be a live target.** An
empty arc is a flick that silently does nothing, which reads as a bug rather than a miss.

---

## Phase 0 — Foundation ☑

Blocks all three weapons. Pure table extension; the risk is missing a site, not difficulty.

| Site | File | Change |
|---|---|---|
| `Element` enum | `Assets/ElementalInteractions.cs:3` | **Append** Earth, Dark, Light |
| Damage matrix | `Assets/ElementalInteractions.cs:15` | 3 new rows + 3 new columns on the 5 existing rows |
| Damage lookup | `Assets/Scripts/GameManager.cs:118` | Fail-soft fallback to 1.0 |
| `GearTile` enum | `Assets/Scripts/GearManager.cs:6` | 3 tiles + `TryGetElement` cases |
| Base colours | `Assets/Scripts/ElementVisuals.cs:5` | `GetColor` |
| UI/charge colours | `Assets/Scripts/ElementVisualUtility.cs` | `GetAccentColor` + 3 charge ramps (2 cases each) |
| Dynamic arcs | `Assets/Scripts/GearManager.cs` | Drive `arcCount` from an equipped-element list |

Each weapon registers itself on `CoreSystems.prefab` (`elementalWeapons` list) as part of its own phase —
there is nothing to register until the weapon exists.

### The new elements are inert until their weapons land

They exist in the enum and as gear tiles, but the starting loadout is still Wind/Fire/Ice/Lightning, so
they are unreachable in play. If one is ever equipped early, `ElementManager.SetActiveElement` fails
safe — it logs an error and keeps the current weapon rather than leaving the player unable to attack.

### Append, never insert

Unity serializes enums as **ints** into prefabs and `.asset` files. Inserting Earth mid-enum would
silently remap every existing Wind reference across every prefab. New values go on the end (5/6/7).

### Fail-soft matrix

`interactionMatrix` is a hardcoded nested dictionary, and `CalculateDamage` indexes it directly — so one
missing pair in a 64-entry table is a `KeyNotFoundException` *mid-combat*. Switch to `TryGetValue` with a
1.0 fallback. This also makes "neutral by default" the actual default rather than something we
hand-maintain across three new rows and columns.

### Colours

| Element | Value | Note |
|---|---|---|
| Earth | `(0.76, 0.52, 0.24)` | amber / brown |
| Dark | `(0.45, 0.22, 0.62)` | dark purple |
| Light | `(1.00, 0.99, 0.88)` | white, faint yellow tint |

Light must stay distinguishable from Physical's cool white-cyan `(0.85, 1, 1)` — hence the warm tint.
Both colour files fall through to Physical's white on `default:`, so **omitting a case fails silently
rather than failing to compile**. All 8 switch sites need explicit cases.

---

## Phase 1 — Earth / Ballista Turret ☐

> *Fantasy: immovable firepower. Charge: disable movement (movement aims instead), charge a single
> piercing projectile, launch on release. Tap: N/A, weak charge attack.*

Smallest of the three, and it proves an input pattern the others don't need.

- Implement `IElementalWeapon` + `IMeleeChargeProvider` (charge UI is then free — the joystick and
  world charge indicators pick it up automatically).
- **New:** movement-lock aim mode — route `PlayerController.MoveInDirection` to aiming while charging.
- **New:** pierce flag on `PlayerProjectile` (it currently consumes on hit).

Open question: whether movement-lock needs its own visual state so the player understands *why* they
can't move.

## Phase 2 — Light / Harp ☐

> *Fantasy: mark and detonate, ranged burst. Tap: launch a single marking projectile. Charge: summon an
> angel at your position that launches piercing beams at each mark.*

- Tap reuses `PlayerProjectile` (Fire's fireballs already do).
- Mark = a new `EnemyEffect.Marked`, alongside the existing Burn / Chill / Static / Buffetted on the
  shared 1s tick loop.
- **New:** enumerate-marked query on `ActiveEnemyRegistry` (it only does `TryGetNearest` today).
- **New:** angel actor with a lifetime + player-side piercing beam. The enemy Beam Sniper
  (`EnemyBeamLaser`) is the reference implementation to mirror.

## Phase 3 — Dark / Scythe ☐

> *Fantasy: summoner. Tap: swing scythe in an arc. Charge: blink and swing in a circle. Execution with
> dark melee resummons the enemy as a minion.*

Split, because the combat and the necromancy are very different sizes.

**3a — the weapon.** Mostly assembly: tap borrows Physical's arc shape, and charge combines the existing
`PlayerController.BlinkTo(Vector2)` (already used by Lightning's Thunderstep) with
`MeleeAugmentUtility.DamageEnemiesInRadius`. Ships as a playable element on its own.

**3b — minions.** The one genuinely new system in this batch: friendly AI, retargeting, ownership,
lifetime, death handling. Nothing comparable exists. Scoped separately so Dark is playable well before
this lands.

---

## Backlog (in this doc's scope, not started yet)

- ☐ **Element-switch flourish / "gear arts"** — the doc asks for a visual pop on element switch.
  Nothing exists. `ElementManager.OnActiveElementChanged` is the hook.
- ☐ **Sword sprite changes per element.** The doc says *"sword changes to match that element"* and
  *"all weapons have gear crossguard motif"*. Today only VFX differ — `PlayerWeaponIndicator` just
  rotates the pivot and toggles visibility; there is no element→sprite path at all. Needs 7 weapon
  sprites, so it's art-blocked, then a small code change.

## Known adjacent issues (out of scope, recorded so they aren't lost)

- **The dash is currently unreachable.** `9d96937` removed the only assignment of
  `PlayerState.SwordThrown`, and both dash triggers are gated behind it — so `DashCoroutine` is dead
  code and the player currently has no dodge. The doc's unresolved "dash dilemma" is about a build that
  no longer exists. Worth confirming this is intentional and not a regression.
- **Element grants are timed, not persistent.** A flick calls `ApplyEmpowerment` with
  `imbueDuration` and expires back to `Physical`. The doc's model ("sword changes to match that
  element") reads as a persistent mode, and playtesters asked for longer buffs. Resolve alongside
  Elemental Affinities, since it determines whether `Physical` remains a real state.
