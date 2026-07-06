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
M6  Composed waves (replace static layout waves) — still authored arena
M7  Procedural wall layout (one commit: graph, one: tilemap, one: wire combat)
M8  Procedural crates/props in arena
M9  Level preview panel after upgrade
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

## M6 — Composed waves (authored arena, dynamic enemies)

> **Why now:** You already have the full loop; this only changes **what spawns** in combat.

### Commit 18 — `EnemyCatalog` + wired on RunManager

| | |
|---|---|
| **Adds** | `EnemyCatalog.cs`, `EnemyCatalog.asset`, `EnemyCatalogCreator.cs` (menu) |
| **Changes** | `CoreSystems` — `RunManager.enemyCatalog` |
| **Playtest** | Enter combat → enemies still spawn (catalog used by next commit). Verify catalog asset in inspector. |

### Commit 19 — `WaveComposer` + encounter types

| | |
|---|---|
| **Adds** | `WaveSpec`, `WaveComposerSettings`, `WaveComposer`, `EnemyArchetype`, `EnemySpawnSpec`, `CombatEncounter`, `EncounterContext`, `EncounterTheme*`, `PlayerLoadoutSnapshot`, `ElementExtensions` |
| **Changes** | `EnemySpawnPoint` lane field if needed |
| **Playtest** | **No visible change yet** — skip unless bundled with 20. |

**Note:** Commits 18–19 can merge into **Commit 20** if 19 alone fails the playtest rule.

### Commit 20 — Combat uses `EncounterBuilder` + composed waves (still authored room)

| | |
|---|---|
| **Adds** | `EncounterBuilder.cs` |
| **Changes** | `RunManager.BuildBlueprintForCurrentStep` → `EncounterBuilder.Build` with **`GeneratedRoom = null`** (layout prefab only) |
| **Changes** | `LevelBlueprint` extensions as needed |
| **Playtest** | Combats feel **different by cycle/theme** — enemy mix changes, arena is still **Square_Arena**. Check console for wave compose logs. |
| **EditMode** | `WaveComposerTest`, `EncounterThemeTest` |

---

## M7 — Procedural walls (three commits, three playtests)

### Commit 21 — Procedural room replaces prefab (walls only, no props)

| | |
|---|---|
| **Adds** | `ArenaGraphSettings`, `ArenaGraphGenerator`, `ArenaTilemapBuilder`, `ArenaLayoutGenerator`, `ArenaLayoutResult` |
| **Changes** | `EncounterBuilder` — `ArenaLayoutGenerator.Generate` → `GeneratedRoom`; tilemap uses **authored wall scale**, **no grey floor** |
| **Playtest** | Enter combat → **procedural room** with correct-sized walls (match Level2 scale), player/enemies **inside** arena. **No crates yet.** |
| **EditMode** | `ArenaGraphGeneratorTest`, `ArenaLayoutGeneratorTest` |

### Commit 22 — `ExitSpawnPoint` + spawn lanes on generated room

| | |
|---|---|
| **Changes** | Generated room places `PlayerSpawnMarker`, `ExitSpawnPoint`, `EnemySpawnPoint` lanes; portal uses generated exit |
| **Playtest** | Procedural combat → clear waves → portal at **sensible spot in room** → exit to Map. |

### Commit 23 — Lane-based enemy spawn on procedural arenas

| | |
|---|---|
| **Changes** | `LevelLoader` spawns wave enemies on lane spawn points |
| **Playtest** | Enemies enter from **edges** of procedural room, not random off-map coordinates. |

---

## M8 — Crates & props (only when spawn works)

### Commit 24 — Crate/pillar prefabs + `ObstacleCatalog`

| | |
|---|---|
| **Adds** | `ObstacleCatalog`, `DestructibleCrate`, `ArenaCrate.prefab`, `ArenaPillar.prefab` |
| **Changes** | `Square_Arena.asset` / template — catalog reference |
| **Playtest** | Manually place crate in **ShopLevel or Arena test scene** → visible, collidable, destructible. |

### Commit 25 — Spawn crates in procedural arenas

| | |
|---|---|
| **Adds** | `ArenaChunkPopulator`, `ArenaComposer`, `PropSpawnZone` (optional on authored arenas) |
| **Changes** | `EncounterBuilder` / `ApplyProps` after layout gen |
| **Playtest** | Procedural combat → **crates/pillars visible inside room**, collide with player. |

---

## M9 — Level preview

### Commit 26 — Preview block generation on upgrade enter

| | |
|---|---|
| **Changes** | `RunManager.GeneratePreviewBlockIfNeeded`, `LinearRunGenerator.GeneratePreviewBlock` |
| **Playtest** | Console/logs show preview encounters generated when entering upgrade (UI next commit). |

### Commit 27 — `LevelPreviewPanel` in upgrade hub

| | |
|---|---|
| **Adds** | `LevelPreviewPanel.cs` |
| **Changes** | `ShopLevel` — panel UI wired to `RunManager.PreviewBlock` |
| **Playtest** | Upgrade hub → panel shows **next 3 combat** previews (icons/elements). |

### Commit 28 — Promote preview into real queue

| | |
|---|---|
| **Changes** | `EnsureMoreStepsQueued` → `BlockFromEncounters` |
| **Playtest** | After upgrade, next 3 combats **match** what preview showed (element/theme). |
| **EditMode** | `RunQueuePromotionTest` |

---

## M10 — Tooling & docs

### Commit 29 — `SwordGearLevelGenSetup` menus

| | |
|---|---|
| **Adds** | `SwordGearLevelGenSetup.cs` |
| **Playtest** | Run **SwordGear → Setup →** items; re-run M4 playtest to confirm nothing broke. |

### Commit 30 — Documentation for shipped milestones only

| | |
|---|---|
| **Changes** | `MapRunSystem.md`, `LevelGeneration.md`, `ProjectIndex.md`, `AGENTS.md` |
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
| **20** | Combats differ by composition |
| **21** | Procedural walls, correct scale |
| **25** | Procedural + crates |
| **28** | Preview matches next combats |

---

## Local WIP today

All of the above is **stashed/mixed** in one tree. Reset and land **Commit 01** first — do not cherry-pick Phase A markers in isolation.

When ready: say **“start commit 01”** and we apply only that slice + give you the exact play steps.

---

*Last updated: 2026-06-28 — playtest-first reorder; linear map is commits 01–03, not Phase F.*
