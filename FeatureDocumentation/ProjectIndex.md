# SwordGear Project Index

> **For AI agents:** Read this file at the start of a session for project-wide orientation. For system design details, use the per-feature docs linked from [AGENTS.md](../AGENTS.md). Last indexed: 2026-05-30.

---

## Stack

| Item | Value |
|---|---|
| Unity | 6000.3.13f1 |
| Render pipeline | URP 17.3.0 (`Assets/Settings/UniversalRP.asset`) |
| Input | New Input System (`PlayerControls.inputactions`, `MainInputActions.inputactions`) |
| Cinemachine | 3.1.5 |
| Agent bridge | CoplayDev Unity MCP (see AGENTS.md) |
| Assembly | Single runtime asmdef: `Assets/Main.asmdef` (Aaron + Scripts together) |

---

## Runtime Scene Flow

```
BootUp → Tutorial → TitleScene → Arena  (loop)
                ↓ defeat
           TitleScene
```

### Primary scenes (`Assets/Scenes/Main/`)

| Scene | Role |
|---|---|
| **BootUp** | Build index 0. Spawns `CoreSystems.prefab`, runs `GameInitializer`, transitions to start scene |
| **Tutorial** | Onboarding arena; end trigger loads TitleScene |
| **TitleScene** | Menu; starts Arena |
| **Arena** | Main combat: `RoundStarter` → `RoundGenerator` → `LevelLoader` |
| **CombatHUD** | Additive combo/timer UI |
| **TestRig** | Dev/minimal test scene |

**BootUp config:** `startScene` is currently **Tutorial** (Inspector on BootUp scene). `DefeatedOutroPlayer` returns to **TitleScene** on player defeat.

### Additive auxiliary scenes (loaded by `CoreSystems.prefab` via `AuxiliarySceneAdder`)

| Scene | Path |
|---|---|
| Loading overlay | `Assets/Aaron/Scenes/Loading.unity` |
| Joystick UI | `Assets/Aaron/Scenes/JoystickControls.unity` |
| Augment shop UI | `Assets/Aaron/Scenes/AugmentShop.unity` |
| Combat HUD | `Assets/Scenes/Main/CombatHUD.unity` |

Scene swapping is handled by `SceneTransitioner` (`Assets/Aaron/Scripts/SceneTransitioner.cs`) using `StringEventChannelSO` / `BoolEventChannelSO` event channels.

### Dev / legacy scenes (in build settings, not production flow)

- `Assets/Scenes/Prototype*.unity`, `Assets/Scenes/Test Scenes/*`
- `Assets/Aaron/Scenes/GoldShop*.unity`, `TitleSceneTest`, `PawnTest`, `PCControlsTest`

---

## Core Prefab: `CoreSystems.prefab`

Path: `Assets/Aaron/Prefabs/CoreSystems.prefab`

Hosts the live game systems:

| Component | Script |
|---|---|
| GameInitializer | Boot + PlayerBlob init |
| SceneTransitioner | Scene swap + loading screen |
| PlayerGameplayManager | Pawn spawn/HP/regen |
| PlayerStatModifiers | Augment stat stacking |
| GameManager ⚠️ | Legacy player ref, damage, elements |
| ElementManager | Active element + weapon routing |
| ComboSystem | Combo streak scoring |
| RoundGenerator | Procedural 3-level rounds |
| InGameAugmentsManager | Between-level augment offerings |
| AudioSystem | Pooled audio |
| GearManager | Gear slot ring |
| EventSystem | UI input |

Also: 4× `AuxiliarySceneAdder`, enemy effect handlers, placeholder player object.

---

## Code Layout

| Path | ~Files | Contents |
|---|---|---|
| `Assets/Aaron/Scripts/` | 99 `.cs` | Boot, scenes, player pawn, shop, observables, event channels, input, tutorial |
| `Assets/Aaron/Editor/` | 11 `.cs` | Custom inspectors for shop SOs, event channels |
| `Assets/Scripts/` | ~59 `.cs` | Combat, enemies, weapons, level gen, HUD, legacy GameManager/PlayerController |
| `Assets/Tests/` | 3 test files | Shop + serializer EditMode tests only |

### Aaron subfolders

- `ShopSystem/` — pure C# shop model (`ItemStorefront`, `PurchaseUtility`, ViewModels)
- `PlayerBehaviours/` — `Mover`, `Attacker`, `Shooter` (namespace `Testing`)
- `DataEventChannel/` — ScriptableObject event bus
- `Observables/` — `Observable<T>`, `ObservableDictionary`
- `Input/` — joystick regions + custom Input System interactions
- `Tutorial/` — tutorial sections + scene triggers

### Scripts subfolders

- `Weapon Implementations/` — Fire, Ice, Lightning, Physical weapons
- `Enemy/` — controller, AI strategies, status effects
- `LevelGeneration/` — `RoundGenerator`, `LevelLoader`, layout/wave SOs
- `Combat/` — combo + ultimate meter
- `UI/` — CombatHUD, GearEditor

**Rule:** New init/shop/input/player code → Aaron. Combat/enemies/level gen → Scripts (unless migrating).

---

## Key Singletons & Init Pattern

Boot sequence: `GameInitializer.Awake()` → `PlayerBlobLoaderSO.TryLoad()` → init registered lists → `SceneTransitioner.TryChangeScene(startScene)`.

Init interfaces:
- `InitializeableGameComponent` — read-only blob access
- `InitializeableUnrestrictedGameComponent` — mutable blob access
- `InitializeableObject` — ScriptableObject init recipients

| Manager | Path | Notes |
|---|---|---|
| PlayerGameplayManager | `Aaron/Scripts/PlayerGameplayManager.cs` | **Preferred** player source of truth (still syncs to GameManager) |
| PlayerStatModifiers | `Aaron/Scripts/PlayerStatModifiers.cs` | Augment stacking |
| ElementManager | `Scripts/ElementManager.cs` | Element + weapon switching |
| GameManager | `Scripts/GameManager.cs` | ⚠️ Legacy — being replaced |
| RoundStarter | `Aaron/Scripts/RoundStarter.cs` | Arena round kickoff |
| LevelLoader | `Scripts/LevelGeneration/LevelLoader.cs` | Instantiates arenas, spawns waves |

---

## Important Prefabs

| Category | Path |
|---|---|
| Player (legacy) | `Assets/Visuals/Prefabs/Player.prefab` |
| Player (test pawn) | `Assets/Aaron/Prefabs/TestPlayer.prefab` |
| Enemies | `Assets/Visuals/Prefabs/Enemies/` (melee + ranged × 4 elements) |
| Arena layouts | `Assets/Visuals/Prefabs/Level1/2/3.prefab`, `ShopLevel.prefab` |
| Shop UI | `Assets/Aaron/Prefabs/Shop/` (Gold Shop, Augment Shop cards) |
| Joystick regions | `Assets/Aaron/Prefabs/Movement Control Region.prefab`, `Attack Control Region*.prefab` |
| Weapon FX (legacy) | `Assets/Prototype3/Prefabs/` — still referenced by weapon scripts |

---

## ScriptableObject Assets

### Event channels (`Assets/Aaron/ScriptableObjects/EventChannels/`)

`BoolEventChannelSO`, `StringEventChannelSO`, `TriggerEventChannelSO`, `TransformEventChannelSO`, `ItemShopModelEventChannelSO`

Key instances: `SceneTransitionEventChannel`, `EnableLoadingScreen`, `SpawnPlayerEventChannel`, `PlayerDefeatedEventChannel`, Augment Shop channels.

### Shop data (`Assets/Aaron/ScriptableObjects/`)

- `LoadableStoreItemCatalog` → `TestCatalog.asset`
- Stat/element upgrade items → `Items/up_*.asset`
- `DummyPlayerBlobLoaderSO` → `AaronTestPlayerLoader.asset`

### Level generation (`Assets/Scripts/LevelGeneration/`)

- `ArenaLayoutTemplate` → `ArenaLayouts/` (Circle, Donut, Square, Shop_Arena)
- `EnemyWaveConfig` → `EnemyWaveConfigs/` (16 wave assets)
- `LevelTransitionType` → `LevelTransitions/Teleport_Transition.asset`

### Audio

- `AudioLibrary` → `Assets/Audio/MainAudioLibrary.asset`

---

## Test Coverage

| Area | Status |
|---|---|
| Shop (`ItemStorefront`, `PurchaseUtility`) | ✅ 16 EditMode tests |
| `UpgradeTypeSerializer` | ✅ 5 EditMode tests |
| Combat, enemies, weapons, combo | ❌ Not tested |
| Level generation, scene flow | ❌ Not tested |
| Player pawn, input, ViewModels | ❌ Not tested |
| PlayMode | ❌ No PlayMode test scripts in `Assets/Tests/` |

**Quick validation:** Run EditMode tests via Unity MCP `run_tests` (~1s, 21 tests).

Test doubles live in `Assets/Aaron/Scripts/TestDataStructures/`.

---

## Legacy — Do Not Extend

| Location | Why |
|---|---|
| `Assets/Prototype2/`, `Assets/Prototype3/` | Old prototypes; Prototype3 FX still referenced |
| `Assets/HenryTestScripts/` | Contributor experiments |
| `Assets/Scripts/PlayerControllerOld.cs` | Superseded |
| `Assets/Scripts/GameManager.cs` | Incrementally replaced — don't add new deps on it |
| `Assets/Scripts/PlayerController.cs` | Coexists with new pawn; still wired through GameManager |
| Namespace `Testing` in Aaron | Prototype behaviours not yet promoted |

---

## Active TODO Themes (crunch-relevant)

1. **GameManager refactor** — `PlayerGameplayManager` should own player pawn; many scripts still read `GameManager.Instance.player` transform
2. **Gear ↔ PlayerBlob** — `GearManager` not yet wired to inventory blob
3. **Level transitions** — `LevelLoader` transitions not implemented; shop levels skip waves but no transition mechanic
4. **Augment shop model** — augments are "chosen" not purchased; `AugmentShopViewModel` needs rethink
5. **Polish gaps** — spawn/defeat animations, difficulty scaling, joystick fade visuals

Full TODO grep targets: `Assets/Aaron/Scripts/`, `Assets/Scripts/`.

---

## Quick Path Reference

| Need | Go to |
|---|---|
| Boot / init | `Assets/Aaron/Scripts/GameInitializer.cs` |
| Scene changes | `Assets/Aaron/Scripts/SceneTransitioner.cs` |
| Player combat pawn | `Assets/Aaron/Scripts/PlayerGameplayPawn.cs`, `PlayerBehaviours/` |
| Shop logic | `Assets/Aaron/Scripts/ShopSystem/` |
| Round/level pipeline | `Assets/Scripts/LevelGeneration/` |
| Feature deep-dives | `FeatureDocumentation/*.md` |
| Agent conventions | `AGENTS.md` |
