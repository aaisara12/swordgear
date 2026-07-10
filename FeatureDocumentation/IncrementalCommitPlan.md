# Incremental Commit Plan — Playtest-First Order

> **Goal:** Land the overhaul as small commits where **every commit changes something you can play**.  
> **Not acceptable:** “it compiles”, inspector-only checks, or unit tests with no in-game effect.  
> **Order:** Player-facing flow first (linear map → authored combat loop → upgrade → wave composer → procedural layers).

---

## Rules

1. **Playtest gate** — each commit must answer: *“What do I do in Play Mode that I couldn’t do before?”*
2. **One feature per commit** — split if the playtest description needs “and also…”.
3. **Dependencies follow play order** — don’t add `PlayerSpawnMarker` until `LevelLoader` uses it in the same or next commit.
4. **EditMode tests are bonus** — allowed alongside a playtest, never instead of one.
5. **Legacy code stays until replaced** — delete branching map only after linear map loop is proven in play.
6. **Authored arenas before procedural** — `Square_Arena` / `Level2` until procedural walls are individually proven.

---

## Milestone overview (play order)

```text
M1  Linear map screen exists and is reachable from Title
M2  Map → authored combat → back to Map (one combat)
M3  Exit portal + step advance on rail
M4  Full Combat×3 → Upgrade → Map loop (authored arenas only)
M5  Upgrade hub — augment on enter (walkable zones deferred)
M6  Complete encounter system (catalog, composer, difficulty, elites, spawn FX) — orthogonal to arena geometry
M7  Procedural wall layout only (geometry + markers; reuses M6 encounters unchanged)
M8  Procedural crates/props in arena
M9  Level preview UI in upgrade hub (encounter data already from M6)
M10 Legacy cleanup + editor setup menus
```

---

## How to apply

```text
git stash push -u -m "overhaul-monolith"
git checkout main
# For each commit: apply only listed files → playtest → git commit → next
```

---

## M1 — Linear map (first playable change)

### Commit 01 — Map rail UI (mock run, Map scene only)

| | |
|---|---|
| **Adds** | `LinearMapController.cs`, `LinearCombatNode.prefab`, `LinearUpgradeNode.prefab` |
| **Changes** | `Map.unity` — rail container, player token, node prefab refs; **hide** old branching map canvas |
| **Mechanism** | Controller seeds a **hardcoded** 4-step list (C→C→C→U) until `RunManager` exists — no arena load yet |
| **Playtest** | Open `Map.unity` → Play → scrolling rail, 4 nodes, player token visible on top. No Title needed. |
| **Not in commit** | `RunManager`, `NodeStarter`, deleting old map scripts |

### Commit 02 — Title Play → linear Map

| | |
|---|---|
| **Adds** | `TitlePlayButtonHandler.cs` |
| **Changes** | `TitleScene.unity` — Play button loads `Map` (not old node-picker flow) |
| **Playtest** | Boot → Title → **Play** → linear map rail (still mock steps). Old branching UI not shown. |
| **Not in commit** | Arena transition |

### Commit 03 — Real run queue drives the rail

| | |
|---|---|
| **Adds** | `RunStepType.cs`, `RunStep.cs`, `LinearRunState.cs`, `LinearRunGenerator.cs` |
| **Changes** | `RunManager.cs` — generate queue on `StartNewRun` / `EnsureRunStarted`; `LinearMapController` reads `RunManager.Run` (remove mock) |
| **Changes** | `CoreSystems.prefab` — `RunManager` scene refs (Map/Arena names only; layouts can wait) |
| **Playtest** | Title → Play → rail shows **real** Combat×3 + Upgrade nodes from generator. Token at step 0. Still no arena. |
| **EditMode (optional)** | `LinearRunPreviewTest` |

---

## M2 — Map → authored combat → Map (one step)

### Commit 04 — Map interstitial loads Arena

| | |
|---|---|
| **Changes** | `RunManager.cs` — `BeginCurrentStep`, `OnMapInterstitialComplete`; `LinearMapController` calls complete after token anim |
| **Changes** | `SceneTransitioner.cs` — only if needed for Map↔Arena queue |
| **Playtest** | Title → Play → map animates → **Arena scene loads**. Pawn may not spawn yet — that’s OK for this commit. |

### Commit 05 — Combat loads authored `Square_Arena` + waves from layout

| | |
|---|---|
| **Changes** | `NodeStarter.cs` — read `RunManager` current **combat** step; build `LevelBlueprint` from `fallbackCombatLayout` (`Square_Arena`) + **existing waves on the template** (no `EncounterBuilder`) |
| **Changes** | `CoreSystems.prefab` — assign `fallbackCombatLayout` |
| **Playtest** | Title → Play → Map → Arena → **recognizable Square_Arena**, enemies spawn, waves fight. Same content every combat for now. |
| **Not in commit** | Portal, return to map, procedural gen |

### Commit 06 — Player spawns at `PlayerSpawnMarker`

| | |
|---|---|
| **Adds** | `PlayerSpawnMarker.cs` |
| **Changes** | `LevelLoader.cs` — `SpawnPawnAtLocation` at marker; ensure `Square_Arena` / `Level2` prefab has marker object |
| **Playtest** | Enter combat → player spawns at designed spawn point (not corner of arena). **Compare before/after.** |

---

## M3 — Portal exit + rail advance

### Commit 07 — Stage complete overlay auto-fades

| | |
|---|---|
| **Changes** | `StageCompleteStateController.cs`, `CombatHUD.unity` — smaller panel, auto-fade, no Continue button |
| **Playtest** | Clear all waves → performance overlay appears → **fades on its own** (~3s). |

### Commit 08 — Exit portal after wave clear

| | |
|---|---|
| **Adds** | `LevelExitPortal.cs`, `ExitSpawnPoint.cs`, `ExitPortal.prefab` |
| **Changes** | `LevelLoader.cs` — spawn portal at exit marker after waves; `CoreSystems` → `exitPortalPrefab` |
| **Changes** | Place `ExitSpawnPoint` on `Square_Arena` (or spawn at fixed offset if marker missing) |
| **Playtest** | Clear waves → **visible portal** → walk into it (log or empty transition OK). |

### Commit 09 — Portal → Map + token advances one step

| | |
|---|---|
| **Changes** | `NodeStarter.cs` — portal callback → `RunManager.HandleCombatPortalExited`; `RunManager` advances step on interstitial complete |
| **Playtest** | Clear combat → portal → **Map** → token at **next** node. Repeat: second combat loads on next cycle. |

---

## M4 — Full linear loop (authored content only)

### Commit 10 — Three combats then Upgrade on rail

| | |
|---|---|
| **Changes** | `LinearRunGenerator` — full block (C×3 + U); `RunManager.EnsureMoreStepsQueued` appends next block after upgrade |
| **Playtest** | Play through **3 combats** (same arena OK) → 4th step is **Upgrade** on rail. |

### Commit 11 — Upgrade step loads `ShopLevel` + spawn

| | |
|---|---|
| **Changes** | `NodeStarter` upgrade branch; `RunManager.BuildBlueprintForCurrentStep` shop path; `ShopLevel.prefab` + `PlayerSpawnMarker` |
| **Changes** | `CoreSystems` — `shopLayout` → `Shop_Arena` |
| **Playtest** | After 3rd combat → Map → **ShopLevel** loads → **player pawn visible** in shop arena. |

### Commit 12 — Leave upgrade → Map (minimal exit)

| | |
|---|---|
| **Adds** | `UpgradeContinuePad.cs` |
| **Changes** | `ShopLevel.prefab` — exit pad or wire existing shop **Continue** to `RunManager.HandleUpgradeComplete` |
| **Playtest** | Upgrade step → touch Continue → **Map** → token past upgrade → **combat 4** queues on rail. |
| **Milestone M4 done** | Full **Combat×3 → Upgrade → repeat** on authored arenas. |

### Commit 13 — Deprecate branching map (playtest unchanged)

| | |
|---|---|
| **Deprecates** | `MapGenerator`, `RunMap`, `MapNode`, `MapSceneController`, `MapNodeButton`, `FixedMapDefinition`, branching fields on `MapGenerationSettings`, `RestNodeController`, `NodeType`, `MapGeneratorTest`, `MapNodeButton.prefab` — marked `[Obsolete]` / `DEPRECATED` comments; **not deleted** |
| **Keeps active** | `MapGenerationSettings` combat/shop content fields (`combatLayouts`, `combatWaves`, `shopLayout`, wave counts) used by `RunManager` / `LinearRunGenerator` |
| **Playtest** | Same as M4 — linear loop still works. |

### Commit 14 — Delete legacy round system

| | |
|---|---|
| **Deletes** | `RoundGenerator`, `RoundStarter`, `StartNextRound`, `LevelTransitionType` |
| **Changes** | `TestGameManager.cs`, `CoreSystems.prefab` |
| **Playtest** | Same as M4. |

---

## M5 — Upgrade hub (augment on enter)

> **Scope note:** Walkable shop zones (old commit 16) and map token polish (old commit 17) are **deferred**.

### Commit 15 — `UpgradeFlowController` + augment on enter ✅

| | |
|---|---|
| **Adds** | `UpgradeFlowController.cs` |
| **Changes** | `Arena.unity` — `UpgradeFlowController` on `Node Loop`; `LoadingSceneAnimator` unscaled fade during pause |
| **Playtest** | Enter upgrade → **augment choice appears** before roaming. |

### ~~Commit 16~~ — Walk zones open shop panels *(deferred)*

### ~~Commit 17~~ — Map token polish *(deferred — largely done in earlier map commits)*

---

## M6 — Complete encounter system (waves, difficulty, elites)

> **Why now:** The full loop works; combat content is the main source of repetition.  
> **Milestone goal:** After M6, **all enemy-related behaviour is done** — no later milestone may change how waves are composed, scaled, or spawned. Arena work (M7+) only swaps **room geometry**; it plugs into the same `CombatEncounter` + `LevelLoader` spawn contract.

### Scope boundary

| In M6 (enemy / encounter) | Out of scope (arena / geometry) |
|---|---|
| `EnemyCatalog`, threat budget, elemental themes | Procedural walls, tilemaps, room graphs |
| `WaveComposer`, `EncounterBuilder`, difficulty curve | Crates, pillars, prop placement |
| Runtime `CombatEncounter` / `ComposedWave` (not per-fight SOs) | Lane topology, edge-entry choreography |
| HP/damage scaling, elite variants + aura (any of 20 archetypes) | `ArenaLayoutGenerator` |
| **12 new enemy prefabs** (beam sniper, shotgun, turret × 4 elements) + attack/movement strategies | Lane topology, edge-entry choreography |
| Spawn presentation (spawn animation; all enemies spawn together) | Level preview **UI** (data layer ships in M6; panel in M9) |
| Per-combat deterministic seed (`hash(runSeed, globalStepIndex)`) | |
| Pre-roll next block’s encounters for upgrade preview / queue promotion | |
| `LevelLoader` spawns from composed data via `EnemySpawnPoint` markers in **whatever room is loaded** | |

### Design principles

1. **Arena-agnostic spawning** — `LevelLoader` discovers `EnemySpawnPoint` components in the instantiated room and picks positions at random (current behaviour). Any prefab or procedural room that places those markers works without spawn-code changes.
2. **Runtime encounters only** — `EncounterBuilder` produces a `CombatEncounter` (list of `ComposedWave` → `ComposedSpawnSpec`). Do not create `EnemyWaveConfig` assets per fight.
3. **Difficulty curve is data** — `WaveComposerSettings` holds a tunable table: `(blockIndex, combatIndexInBlock)` → threat budget, wave count range, elemental theme weights, elite rules, HP/damage multipliers.
4. **Threat budget, not raw counts** — Composer spends budget using each archetype’s `baseThreatCost` from `EnemyCatalog`.
5. **Enemy roster (20 archetypes)** — 8 existing (melee + strafe-ranged × 4 elements) + **12 new** (beam sniper, shotgun, turret × 4 elements). Catalog lists **20 entries**; composer mixes **roles** and **elements**, not just palette swaps.
6. **Elites are runtime, not duplicate prefabs** — Any archetype can spawn as elite via `isElite` on `ComposedSpawnSpec` (scale, HP/damage mult, aura). **Do not** author 20 elite prefabs — elites are “elite beam sniper”, “elite turret”, etc. from the same 20 base archetypes.
7. **Spawn animation** — Prefab `Animator` + spawn clip (or short authored presentation component). Enemy movement/attacks disabled until spawn clip completes; whole wave spawns in one frame then plays in together.
8. **Determinism** — Same `runSeed` + `globalStepIndex` → identical `CombatEncounter`. Pre-rolled block encounters promoted into the run queue must match preview data.
9. **Legacy wave pool retired** — `MapGenerationSettings.combatWaves` deprecated after M6; keep old `EnemyWaveConfig` assets for reference only.

### Enemy roles (catalog)

| Role | Behaviour | Notes |
|------|-----------|--------|
| `Melee` | `FollowPlayerStrategy` + melee attack | Existing × 4 elements |
| `Ranged` | `StrafeMovementStrategy` + `RangedAttackStrategy` | Existing × 4 elements |
| `BeamSniper` | Slow strafe/follow + telegraphed high-speed beam shot | New × 4 elements |
| `Shotgun` | Strafe + `ShotgunAttackStrategy` (pellet spread) | New × 4 elements |
| `Turret` | `StationaryMovementStrategy` + rapid `RangedAttackStrategy` (no/little charge) | New × 4 elements |

**Total: 20 catalog archetypes.** Elite variants = same archetype + `isElite` at spawn (×2 threat feel, not ×2 prefabs).

### Acceptance criteria (M6 done)

Play through two full blocks (6 combats) on `Square_Arena` and verify:

- Combat **1 vs 3** in the same block: visibly different **role** mix (e.g. melee + turret → shotgun + beam sniper).
- Fights include **new archetypes** (turret, shotgun, beam sniper), not only legacy melee/strafe-ranged.
- **Block 2** combats are harder than block 1 (more HP, higher budget, or more waves).
- **Combat 3** in each block includes at least one **elite** (any role — larger, aura, tankier).
- **Elemental themes** are obvious in at least some combats (e.g. mostly fire enemies across mixed roles).
- **Same seed + step** → identical encounter (EditMode golden test).
- After upgrade, **next 3 combats** match the pre-rolled encounters (log or debug HUD; UI optional until M9).
- Entering combat: enemies **play spawn animation** before chasing the player.

---

### Commit 18 — New enemy archetypes (12 prefabs + attack/movement strategies)

| | |
|---|---|
| **Adds** | `ShotgunAttackStrategy` — pellet count, spread angle, shared charge cadence |
| **Adds** | `BeamSniperAttackStrategy` (or tuned `RangedAttackStrategy`) — long charge, fast beam projectile |
| **Adds** | `StationaryMovementStrategy` — zero velocity; turret does not reposition |
| **Adds** | **12 prefabs** — `BeamSniper_*`, `Shotgun_*`, `Turret_*` for Physical / Fire / Ice / Lightning (duplicate existing enemy prefab structure; element-colored projectiles) |
| **Adds** | Projectile prefabs as needed — beam (elongated/trail), shotgun reuses `EnemyProjectile` pellets |
| **Changes** | `EnemySystem.md` — document roles and strategy pairing |
| **Playtest** | Arena test scene or wave override: spawn **turret**, **shotgun**, and **beam sniper** — distinct movement/attack readable in play. |
| **Not in commit** | Catalog SO, composer, difficulty curve |

---

### Commit 19 — `EnemyCatalog` + spawn modifiers + difficulty hook

| | |
|---|---|
| **Adds** | `EnemyCatalog.cs`, `EnemyArchetype` (prefab, element, **role**, `baseThreatCost`), `EnemyCatalog.asset`, `EnemyCatalogCreator.cs` (menu) |
| **Adds** | `EncounterContext.cs` — `runSeed`, `globalStepIndex`, `blockIndex`, `combatIndexInBlock` derived from `LinearRunState` |
| **Adds** | `DifficultyModifiers` / `SpawnModifiers` (HP mult, damage mult, scale mult) |
| **Changes** | `EnemyController` — `ApplySpawnModifiers(...)`; attack strategies respect damage multiplier |
| **Changes** | `LevelLoader` — after instantiate, apply modifiers from a **temporary** step-based curve (hardcoded or minimal SO) so scaling is playable before composer lands |
| **Changes** | `RunManager` — fix `BuildCombatWaves` to use `hash(seed, globalStepIndex)` *(interim until Commit 21 removes this path)* |
| **Changes** | `CoreSystems` — `RunManager.enemyCatalog` |
| **Playtest** | Combat 2 in a block → enemies **survive longer** than combat 1 (HP scaling visible). Catalog lists **all 20 archetypes** with role + threat cost. |
| **EditMode** | `EncounterContextTest` (block/combat index math) |

---

### Commit 20 — Elite enemies + spawn presentation

| | |
|---|---|
| **Adds** | `ComposedSpawnSpec` (`archetypeId`, `isElite`), `ElitePresentation` (aura prefab ref, scale mult, stat mults) |
| **Adds** | `EnemySpawnPresentation.cs` — wires Animator spawn trigger; disables `EnemyController` / strategies until clip done |
| **Changes** | Enemy prefabs (all 20) — `Animator` + `Spawn` clip (editor-authored); optional `EliteAura` child prefab (particles) |
| **Changes** | `LevelLoader` — run spawn presentation on all wave spawns; apply elite scale/aura when `isElite` |
| **Changes** | Interim elite rule: e.g. last wave spawns one **elite** (any archetype — turret elite, shotgun elite, etc.) until composer owns placement |
| **Playtest** | Enter combat → enemies **pop in with spawn animation** before moving. At least one fight shows a **large aura elite** (preferably a new-role enemy). |
| **Not in commit** | Threat budget / theme composition |

---

### Commit 21 — `WaveComposer` + `EncounterBuilder` + runtime encounters

| | |
|---|---|
| **Adds** | `WaveComposerSettings` (difficulty curve table, theme weights, **role mix weights**, wave count range, elite rules) |
| **Adds** | `WaveComposer`, `ComposedWave`, `CombatEncounter`, `EncounterTheme`, `EncounterBuilder` |
| **Adds** | `PlayerLoadoutSnapshot` *(optional light bias: active element — defer heavy logic if not readable in play)* |
| **Changes** | `LevelBlueprint` — `CombatEncounter` replaces `List<EnemyWaveConfig>` as primary wave source |
| **Changes** | `LevelLoader` — spawn from `ComposedWave` / `ComposedSpawnSpec` via catalog prefab lookup |
| **Changes** | `RunManager.BuildBlueprintForCurrentStep` → `EncounterBuilder.Build(context, catalog, settings)` only |
| **Deprecates** | `RunManager.BuildCombatWaves`, `MapGenerationSettings.combatWaves` pool |
| **Changes** | `WaveAnnouncer` — optional theme subtitle (e.g. “Fire Assault”) from encounter metadata |
| **Playtest** | Full **acceptance criteria** above on `Square_Arena`. Console shows composed budget/theme/roles per combat. |
| **EditMode** | `WaveComposerTest`, `EncounterBuilderTest` (golden fixtures: block0-combat0, block0-combat2, block1-combat0) |

**Note:** Commits 18–20 can be merged for a single large M6 landing; keep the acceptance criteria as the gate.

---

### Commit 22 — Pre-roll encounters + queue promotion (preview data layer)

| | |
|---|---|
| **Adds** | `RunManager.GenerateUpcomingBlockEncounters`, storage on `RunStep` or parallel `CombatEncounter` list per step |
| **Changes** | `EnsureMoreStepsQueued` — appends steps **with encounters already composed** (no re-roll on enter) |
| **Changes** | `UpgradeFlowController` or upgrade enter — trigger pre-roll for next block |
| **Changes** | `LinearRunGenerator` — stop assigning per-step layout for combat if unused; layout resolves via `RunManager.ResolveCombatLayout` only |
| **Playtest** | Finish upgrade → enter combats 4–6 → **same** enemy themes/budgets/roles as logged at upgrade enter. Re-entering same seed reproduces entire block. |
| **EditMode** | `RunQueuePromotionTest` |

**Milestone M6 done** after Commit 22. Enemy system requires **no** M7 changes.

---

## M7 — Procedural arena geometry only

> **Boundary:** M7 **only** replaces or generates **room prefabs** (`ArenaLayoutTemplate.LevelPrefab` or generated room root). It does **not** modify `WaveComposer`, `EncounterBuilder`, `CombatEncounter`, difficulty curve, elite rules, or `LevelLoader` spawn logic beyond loading a different room instance.

### Commit 23 — Procedural room replaces prefab (walls only, no props)

| | |
|---|---|
| **Adds** | `ArenaGraphSettings`, `ArenaGraphGenerator`, `ArenaTilemapBuilder`, `ArenaLayoutGenerator`, `ArenaLayoutResult` |
| **Changes** | `RunManager.ResolveCombatLayout` or layout resolver — optional generated room **instead of** `Square_Arena` prefab |
| **Changes** | Generated room **must include** `PlayerSpawnMarker`, `ExitSpawnPoint`, `EnemySpawnPoint` markers (same contract as authored arenas) |
| **Playtest** | Enter combat → **procedural room** with correct wall scale; **same composed enemies** as before M7; spawn animation + elites still work. |
| **EditMode** | `ArenaGraphGeneratorTest`, `ArenaLayoutGeneratorTest` |

### Commit 24 — Generated room markers + exit portal placement

| | |
|---|---|
| **Changes** | Generated room reliably places spawn/exit markers; portal spawns at `ExitSpawnPoint` |
| **Playtest** | Procedural combat → clear waves → portal at sensible spot → exit to Map. **No change** to wave composition or enemy behaviour. |

### ~~Commit 23 (old)~~ — Lane-based enemy spawn

*Removed — enemy spawning is complete in M6. Procedural rooms place standard `EnemySpawnPoint` markers; no special lane spawn code.*

---

## M8 — Crates & props (arena decoration only)

### Commit 25 — Crate/pillar prefabs + `ObstacleCatalog`

| | |
|---|---|
| **Adds** | `ObstacleCatalog`, `DestructibleCrate`, `ArenaCrate.prefab`, `ArenaPillar.prefab` |
| **Changes** | `Square_Arena` / template — catalog reference for manual placement tests |
| **Playtest** | Manually place crate in **ShopLevel or Arena test scene** → visible, collidable, destructible. **Encounters unchanged.** |

### Commit 26 — Spawn crates in procedural arenas

| | |
|---|---|
| **Adds** | `ArenaChunkPopulator`, `ArenaComposer`, `PropSpawnZone` (optional on authored arenas) |
| **Changes** | Layout resolver / room generator — `ApplyProps` after geometry only; **does not touch** `CombatEncounter` |
| **Playtest** | Procedural combat → **crates/pillars visible inside room**, collide with player. Same waves/elites as without props. |

---

## M9 — Level preview UI (encounter data from M6)

> **Note:** Encounter pre-roll and queue promotion ship in **M6 Commit 22**. M9 is UI only.

### Commit 27 — `LevelPreviewPanel` in upgrade hub

| | |
|---|---|
| **Adds** | `LevelPreviewPanel.cs` |
| **Changes** | `ShopLevel` — panel wired to pre-rolled `CombatEncounter` data on `RunManager` / upcoming steps |
| **Playtest** | Upgrade hub → panel shows **next 3 combat** previews (theme icons, elite indicator, difficulty hint). |

### ~~Commit 26–28 (old)~~ — Preview generation + promotion

*Merged into M6 Commit 22. M9 no longer owns encounter composition.*

### Commit 28 — Preview polish + docs

| | |
|---|---|
| **Changes** | Preview copy/styling; `MapRunSystem.md`, `EnemySystem.md`, `LevelGeneration.md` updated for M6 encounter model |
| **Playtest** | Full loop: preview → fight → composition matches panel. |
| **EditMode** | `RunQueuePromotionTest` already in M6; re-run after UI wiring |

---

## M10 — Tooling & docs

### Commit 29 — `SwordGearLevelGenSetup` menus

| | |
|---|---|
| **Adds** | `SwordGearLevelGenSetup.cs` — includes **Enemy Catalog** + **Wave Composer Settings** setup items |
| **Playtest** | Run **SwordGear → Setup →** items; re-run M6 acceptance playtest to confirm nothing broke. |

### Commit 30 — Documentation for shipped milestones only

| | |
|---|---|
| **Changes** | `MapRunSystem.md`, `LevelGeneration.md`, `EnemySystem.md`, `ProjectIndex.md`, `AGENTS.md` |
| **Playtest** | N/A — review docs match commits 01–29. |

---

## Follow-ups (after core plan)

| # | Playtest |
|---|----------|
| F1 | Move upgrade UI from `UpgradeHubBootstrap` runtime build → prefab-authored objects |
| F2 | `ElementalInteractions` balance + `ElementalInteractionsTest` |
| F3 | Rapid Map↔Arena `SceneTransitioner` stress test |
| F4 | Defeat overlay + linear run clear (already on `main` — verify after Commit 02) |

---

## Quick reference — what to play after each milestone

| After commit | Play this |
|--------------|-----------|
| **01** | Map scene only → see rail |
| **02** | Title → Play → rail |
| **03** | Title → rail reflects real queue |
| **05–06** | Title → Map → Square_Arena combat |
| **09** | One combat → portal → Map → token moved |
| **12** | Full C×3 → Upgrade → Map → next cycle |
| **17** | Upgrade hub roam + smooth map |
| **18** | New enemies: turret, shotgun, beam sniper (per element) |
| **22** | **M6 done** — 20 archetypes, difficulty curve, elites, spawn anim, composed waves, pre-roll |
| **23–24** | Procedural **room only**; encounters identical to M6 |
| **26** | Procedural + crates |
| **27** | Preview **UI** matches pre-rolled encounters |

---

## Local WIP today

All of the above is **stashed/mixed** in one tree. Reset and land **Commit 01** first — do not cherry-pick Phase A markers in isolation.

When ready: say **“start commit 01”** and we apply only that slice + give you the exact play steps.

---

*Last updated: 2026-07-05 — M6: 20 archetypes (12 new roles + elites at spawn), complete encounter system; arena gen orthogonal in M7+.*
