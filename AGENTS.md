# AGENTS.md — Swordgear Codebase Guide

This file is an orientation guide for AI coding agents and new contributors. It describes the project's structure, core systems, and code conventions to help you navigate and extend the codebase confidently.

> **For AI agents:** Before searching through source files, always check the [Table of Contents — Feature Documentation](#table-of-contents--feature-documentation) below. The feature docs describe the purpose, key scripts, and design of each system and will often answer your question faster than a code search.

---

## Project Overview

**Swordgear** is a 2D top-down roguelite arena brawler built in Unity (C#). Players fight waves of enemies using elemental weapons, build combo streaks to earn points, then spend those points in an augment shop between levels to improve their stats.

---

## Repository Layout

```
swordgear/
├── Assets/
│   ├── Aaron/              # Primary in-development code (see below)
│   │   ├── Scripts/        # Game logic: player, shop, input, observables, event channels
│   │   └── Editor/         # Custom Unity editor scripts
│   ├── Scripts/            # Core gameplay: combat, enemies, audio, UI, level generation, weapons
│   ├── Plugins/            # Third-party: DOTween (tweening), SerializedCollections
│   ├── Scenes/             # Unity scene files (Main/, Prototype4, Test Scenes/, etc.)
│   ├── Tests/              # NUnit unit tests for shop and serializer utilities
│   ├── Prototype2/         # ⚠️ Legacy prototype — do not add new code here
│   ├── Prototype3/         # ⚠️ Legacy prototype — do not add new code here
│   ├── HenryTestScripts/   # ⚠️ Legacy contributor scripts — do not add new code here
│   ├── NamuFX/             # Third-party VFX asset pack
│   └── Travis Game Assets/ # Third-party visual asset pack
├── FeatureDocumentation/   # Detailed docs per feature/system (see Table of Contents below)
└── AGENTS.md               # This file
```

New game code belongs in **`Assets/Aaron/Scripts/`** or **`Assets/Scripts/`** depending on the subsystem.

---

## Table of Contents — Feature Documentation

| Feature | Doc |
|---|---|
| Game Initialization & Player State | [FeatureDocumentation/GameInitialization.md](FeatureDocumentation/GameInitialization.md) |
| Player Gameplay (pawn, health, behaviours) | [FeatureDocumentation/PlayerGameplay.md](FeatureDocumentation/PlayerGameplay.md) |
| Stat Modifiers & Augment Stacking | [FeatureDocumentation/StatModifiers.md](FeatureDocumentation/StatModifiers.md) |
| Element System | [FeatureDocumentation/ElementSystem.md](FeatureDocumentation/ElementSystem.md) |
| Combo System & Scoring | [FeatureDocumentation/ComboSystem.md](FeatureDocumentation/ComboSystem.md) |
| Enemy System | [FeatureDocumentation/EnemySystem.md](FeatureDocumentation/EnemySystem.md) |
| Shop System | [FeatureDocumentation/ShopSystem.md](FeatureDocumentation/ShopSystem.md) |
| Event Channels (ScriptableObject messaging) | [FeatureDocumentation/EventChannels.md](FeatureDocumentation/EventChannels.md) |
| Observables (reactive state) | [FeatureDocumentation/Observables.md](FeatureDocumentation/Observables.md) |
| Level Generation | [FeatureDocumentation/LevelGeneration.md](FeatureDocumentation/LevelGeneration.md) |
| Input System | [FeatureDocumentation/InputSystem.md](FeatureDocumentation/InputSystem.md) |
| Audio System | [FeatureDocumentation/AudioSystem.md](FeatureDocumentation/AudioSystem.md) |

---

## Unity MCP Workflow

This project uses [CoplayDev Unity MCP](https://github.com/CoplayDev/unity-mcp) so agents can drive the Unity Editor directly. Cursor connects via `.cursor/mcp.json` (`http://127.0.0.1:8080/mcp`).

**Prerequisites:** Unity Editor must be open with the SwordGear project loaded and the MCP bridge running.

**Agent loop (follow every time you change C# or scenes):**
1. Check `FeatureDocumentation/` before broad code searches.
2. After script edits → `read_console` for compile errors; wait until compilation finishes before using new types.
3. After shop/serializer changes → run EditMode tests via `run_tests` (21 tests in `Assets/Tests/`, ~1s).
4. Scene work → use `manage_scene`, `manage_gameobject`, and `manage_components`; main flow scenes live in `Assets/Scenes/Main/` (BootUp → Title → Arena).
5. Multi-step editor setup → prefer `batch_execute` (max 25 commands per batch).

**PlayMode tests:** One PlayMode test exists; use a long `init_timeout` (~120000 ms) if running it.

---

## Code Conventions

### Nullable Reference Types
All files in `Assets/Aaron/` start with `#nullable enable`. Treat compiler warnings about nullability as errors — use `?` annotations and guard clauses rather than suppressing them.

### Guard Clauses in `Awake`
Serialized references are validated in `Awake()` with `Debug.LogError` and an early return. This is the standard pattern — do not silently swallow null references.

```csharp
private void Awake()
{
    if (myReference == null)
    {
        Debug.LogError("MyComponent: myReference is null");
        return;
    }
    // ...
}
```

### Singleton Pattern
Singletons use a `public static T? Instance` property set in `Awake`, with a duplicate-detection guard:

```csharp
public static MySystem? Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(this); return; }
    Instance = this;
}
```

### Event Wiring
- Subscribe to events in `OnEnable`, unsubscribe in `OnDisable` (for component lifetime events).
- Subscribe in `Awake` and unsubscribe in `OnDestroy` when the subscription should last the full object lifetime.

### Abstract MonoBehaviours Over Interfaces (for injectable deps)
When a dependency needs to be assigned in the Unity Inspector, prefer an `abstract MonoBehaviour` over an `interface`. This allows Unity's serialization to reference it directly.

### Namespaces
- `Shop` — all pure C# shop/purchasing classes (`ItemShopModel`, `IStoreItem`, etc.)
- `Testing` — experimental or prototype behaviours not yet promoted to production

### Design Rationale Comments
Comments tagged `// aisara =>` explain *why* a design decision was made. Read them before changing surrounding code.

### ScriptableObjects
ScriptableObjects serve two roles:
1. **Data containers** — e.g., `LoadableStoreItemCatalog`, `ArenaLayoutTemplate`
2. **Event channels** — e.g., `DataEventChannelSO<T>` and its concrete subtypes

---

## What to Avoid

- **`Prototype2/`, `Prototype3/`, `HenryTestScripts/`** — legacy exploration code. Do not add new logic here and do not assume patterns there reflect current conventions.
- **`PlayerControllerOld.cs`** — superseded by the new player system in `Aaron/Scripts/PlayerBehaviours/`. Do not modify.
- **`GameManager`** — being incrementally replaced. Prefer `PlayerGameplayManager` / `PlayerStatModifiers` / `ElementManager` for new features. See TODOs in the file.
