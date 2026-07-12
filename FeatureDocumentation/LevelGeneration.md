# Level Generation

## Purpose
Provides the building blocks for a single level — an `ArenaLayoutTemplate`, a runtime `CombatEncounter` (or legacy `EnemyWaveConfig` list), and a `LevelLoader` that instantiates the arena and spawns waves.

> **Progression note:** The old `RoundGenerator` "3 levels per round" system has been **replaced by the [Map / Run System](MapRunSystem.md)** (linear rail). Level *content* (`ArenaLayoutTemplate`, `LevelLoader`) is still used; combat waves are composed at runtime by `WaveComposer` (see [EnemySystem.md](EnemySystem.md)).

---

## Key Scripts

| Script | Path |
|---|---|
| `LevelBlueprint` | `Assets/Scripts/LevelGeneration/LevelBlueprint.cs` |
| `LevelLoader` | `Assets/Scripts/LevelGeneration/LevelLoader.cs` |
| `ArenaLayoutTemplate` | `Assets/Scripts/LevelGeneration/ArenaLayoutTemplate.cs` |
| `EnemyWaveConfig` | `Assets/Scripts/LevelGeneration/EnemyWaveConfig.cs` *(legacy)* |
| `EnemySpawnPoint` | `Assets/Scripts/LevelGeneration/EnemySpawnPoint.cs` |
| `WaveComposer` / `EncounterBuilder` | `Assets/Scripts/Enemy/` |

---

## Data Model

```
LevelBlueprint
  ├─ ArenaLayoutTemplate
  ├─ CombatEncounter          (primary — composed waves + theme + difficulty)
  │     └─ List<ComposedWave>
  │           └─ List<ComposedSpawnSpec> (archetypeId, isElite)
  └─ List<EnemyWaveConfig>    (legacy fallback when Encounter is null)
```

`RunManager.BuildBlueprintForCurrentStep` builds combat blueprints via `EncounterBuilder.Build(context, catalog, settings)`.

---

## Adding Content

- **New arena**: create an `ArenaLayoutTemplate` and assign as `RunManager` fallback / pool layout.
- **New enemy**: add prefab + `EnemyCatalog` entry (threat cost + role). Composer picks it automatically.
- **Tune fights**: edit `WaveComposerSettings` (budgets, theme/role weights, elite rules).

Legacy `MapGenerationSettings.combatWaves` is deprecated for linear runs.
