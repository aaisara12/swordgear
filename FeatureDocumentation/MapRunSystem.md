# Map / Run System

## Purpose

Replaces the old fixed **3-level round** system with a **Slay-the-Spire–style branching node map**. A run is a finite sequence of player-chosen nodes that ends in a **Boss**. The map owns run-long state (HP, ultimate meter, combo points-since-last-augment) so it survives the Map ⇄ Arena scene swaps.

This system fixes three pain points from the old flow:
- **Waves** now announce themselves (banner + audio) and keep the `EnemyWaveConfig.DelayAfterClear` breather between them.
- **Levels** end with a gated **Stage Complete** overlay (performance summary + Continue) instead of teleporting the player abruptly; the pawn is fully reset between nodes (no more sword flying into the next level).
- **Rounds** are gone — the player picks their path on a visual map.

---

## Key Scripts

| Script | Path | Role |
|---|---|---|
| `NodeType` | `Assets/Aaron/Scripts/Map/NodeType.cs` | Enum: Combat, Shop, Augment, Rest, Boss |
| `MapNode` | `Assets/Aaron/Scripts/Map/MapNode.cs` | One node: type, column/row, edges, arena/wave payload, completed flag |
| `RunMap` | `Assets/Aaron/Scripts/Map/RunMap.cs` | The graph + current position; exposes selectable (reachable) nodes |
| `MapGenerationSettings` | `Assets/Aaron/Scripts/Map/MapGenerationSettings.cs` | Serialized generator params + content pools |
| `MapGenerator` | `Assets/Aaron/Scripts/Map/MapGenerator.cs` | Pure C# seedable procedural generator (unit-tested) |
| `FixedMapDefinition` | `Assets/Aaron/Scripts/Map/FixedMapDefinition.cs` | Dev/testing override SO (use a hand-built map instead of generating) |
| `RunManager` | `Assets/Aaron/Scripts/Map/RunManager.cs` | Persistent singleton on `CoreSystems.prefab`; owns the run + routes node selection |
| `MapSceneController` | `Assets/Aaron/Scripts/Map/MapSceneController.cs` | Renders the map in `Map.unity`, forwards node clicks |
| `MapNodeButton` | `Assets/Aaron/Scripts/Map/MapNodeButton.cs` | A single interactive node button |
| `NodeStarter` | `Assets/Aaron/Scripts/Map/NodeStarter.cs` | Lives in `Arena.unity`; loads the current node's level (replaces `RoundStarter`) |
| `RestNodeController` | `Assets/Aaron/Scripts/Map/RestNodeController.cs` | Rest overlay (full heal + confirm) |
| `StageCompleteStateController` | `Assets/Scripts/UI/StageCompleteStateController.cs` | Stage Complete overlay (stats + Continue) |
| `WaveAnnouncer` | `Assets/Scripts/UI/WaveAnnouncer.cs` | Banner + audio cue on wave incoming/cleared |
| `ChannelDrivenVisibility` | `Assets/Aaron/Scripts/ChannelDrivenVisibility.cs` | Generic GameObject show/hide from a `BoolEventChannelSO` |

---

## Data Model

```
RunMap
  └─ List<MapNode>
        ├─ NodeType            (Combat / Shop / Augment / Rest / Boss)
        ├─ Column, Row         (layout position)
        ├─ List<int> Next      (forward edges → node ids)
        ├─ ArenaLayoutTemplate Layout   (Combat/Boss/Shop only)
        ├─ List<EnemyWaveConfig> Waves  (Combat/Boss only)
        └─ bool Completed
```

`MapNode` / `RunMap` are plain C# (testable). Arena content (`ArenaLayoutTemplate`, `EnemyWaveConfig`) is still reused verbatim from the old Level Generation pools.

---

## Generation

`MapGenerator.Generate(settings, seed)` builds a column graph:
- **Column 0** is all `Combat` (a guaranteed opener).
- **Interior columns** mix Combat with a few `Shop` and `Augment` nodes (`shopCount`, `augmentEveryNCombats`).
- **Second-to-last column** is a single `Rest` (guaranteed breather before the boss).
- **Final column** is a single `Boss`.
- Consecutive columns are fully connected (every node has ≥1 outgoing and ≥1 incoming edge), so the Boss is always reachable; `extraEdgeChance` adds occasional branches.

Content is then assigned: Combat draws from `combatLayouts`/`combatWaves`, Boss uses `bossLayout`/`bossWaves`, Shop uses `shopLayout`. Augment/Rest are overlays and carry no arena content.

**Determinism:** same seed → identical map (covered by `Assets/Tests/MapGeneratorTest.cs`). Assign a `FixedMapDefinition` on `RunManager` to bypass generation entirely for testing.

---

## Flow

```
BootUp → TitleScene → (Play) → Map.unity
Map → select node:
   Combat / Boss / Shop → Arena.unity   (via SceneTransitioner)
   Augment / Rest        → overlay on Map
Combat cleared → Stage Complete overlay → Continue → back to Map (node marked done)
Boss cleared   → Run Complete → TitleScene
Defeat         → input lock + defeat overlay (~3s, skippable) → TitleScene (run cleared; next Play regenerates a fresh map)
```

- The Title **Play** button raises the scene-change channel with `"Map"`. `MapSceneController.OnEnable` calls `RunManager.EnsureRunStarted()`, which lazily generates a fresh run if none exists (so post-defeat re-entry gets a brand-new map).
- `RunManager.SelectNode(id)` validates reachability, sets the current node, and either requests the Arena scene or opens the Augment/Rest overlay.
- `NodeStarter` (in Arena) reads `RunManager.CurrentNode`, builds a one-off `LevelBlueprint`, and loads it via `LevelLoader`. On clear it shows Stage Complete (Combat) or completes the run (Boss).
- Node completion (`Continue`, shop exit, rest confirm, augment chosen) routes back through `RunManager` → returns to `Map.unity`.

---

## Run-long State & Persistence

`RunManager` lives on `CoreSystems.prefab` (a persistent, never-unloaded host), so the map and run state survive Map ⇄ Arena swaps.

- **HP**: initialized to max at run start (`PlayerGameplayManager.InitializeHealthForNewRun`) and **persists across nodes** — the per-spawn refill was removed. `PlayerGameplayManager.FullHeal()` is called at Rest nodes.
- **Ultimate meter**: `UltimateMeter` (also on `CoreSystems`) persists run-long; `ResetForNewRun()` only at run start.
- **Augment tiering**: `ComboSystem` tracks a dedicated `_pointsSinceLastAugment` accumulator (distinct from the per-node combo reset). `GetAugmentQualityTier()` reads it; `ResetPointsSinceLastAugment()` is called when an Augment node completes.
- **Pawn reset between nodes**: `PlayerGameplayPawn.ResetForNode()` (implemented by `PlayerController`) stops a thrown sword, cancels dash/recall, clears cooldowns, zeroes velocity, resets facing/aim, and returns to `MeleeReady`. Called from `PlayerGameplayManager.SpawnPawnAtLocation`.

---

## Event Channels (`Assets/Aaron/ScriptableObjects/EventChannels/MapRun/`)

| Asset | Type | Use |
|---|---|---|
| `CombatHudVisibilityChannel` | Bool | Show/hide the combat HUD (hidden on Map) |
| `StageCompleteVisibilityChannel` | Bool | Show/hide the Stage Complete overlay |
| `StageCompletePerformanceChannel` | ComboPerformance | Carries the per-node performance summary |
| `StageCompleteContinueChannel` | Trigger | Continue pressed → return to Map |
| `DefeatOverlayVisibilityChannel` | Bool | Show/hide the defeat overlay |
| `DefeatContinueChannel` | Trigger | Defeat overlay finished / skipped → return to Title |
| `RestNodeVisibilityChannel` | Bool | Show/hide the Rest overlay |

Augment nodes reuse the existing `ShowNextAugmentSetChannel` (trigger) and `EnableAugmentShopEventChannel` (bool). `RunManager` completes an Augment node when the augment UI closes (visibility → false).

---

## Adding Content

- **Combat arenas / waves**: add `ArenaLayoutTemplate` / `EnemyWaveConfig` assets to `RunManager.generationSettings.combatLayouts` / `combatWaves`.
- **Boss**: assign `generationSettings.bossLayout` and `bossWaves` (currently a tougher reused combat layout + combined waves as a placeholder).
- **Shop**: `generationSettings.shopLayout` (reuses `Shop_Arena`).
- **Map shape**: tune `columns`, `minNodesPerColumn`/`maxNodesPerColumn`, `extraEdgeChance`, `shopCount`, `augmentEveryNCombats` on `RunManager`.

---

## Status / Remaining Work

Implemented and compiling (29 EditMode tests green): data model, generator + tests, `RunManager` (wired on `CoreSystems.prefab` with content pools + channels), `Map.unity` (canvas, node/edge prefabs, Rest overlay), `NodeStarter` in Arena (old `Basic Game Loop` disabled), Stage Complete + Wave banner in `CombatHUD.unity`, Title `Play` → `Map`, pawn/HP/ultimate persistence.

Still requires an in-editor **play-mode smoke test** and may need follow-up wiring:
- Confirm `AuxiliarySceneAdder` loads the augment UI (`AugmentShop.unity`) while on the Map so Augment nodes display.
- Hide the gameplay HUD elements on the Map (consume `CombatHudVisibilityChannel`).
- Verify the defeat path raises `PlayerDefeatedEventChannel` (→ `RunManager.ClearRun`) and returns to Title.
- The old round scripts (`RoundStarter`, `RoundGenerator`, `StartNextRound`, `TestGameManager`) remain in the project but unwired (incremental cutover) — retire once the map flow is verified end-to-end.
