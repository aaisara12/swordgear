# Level Generation

## Purpose
Provides the building blocks for a single level — an `ArenaLayoutTemplate`, a list of `EnemyWaveConfig`s, and a `LevelLoader` that instantiates the arena and spawns waves.

> **Progression note:** The old `RoundGenerator` "3 levels per round" system has been **replaced by the [Map / Run System](MapRunSystem.md)** (a Slay-the-Spire branching node map). `RoundGenerator` / `RoundStarter` still exist but are unwired (incremental cutover). Level *content* (`ArenaLayoutTemplate`, `EnemyWaveConfig`, `LevelLoader`) is still used verbatim — a map node now carries one `LevelBlueprint` and `NodeStarter` loads it. New wave-feedback hooks (`LevelLoader.OnWaveIncoming` / `OnWaveCleared`) drive the `WaveAnnouncer`.

---

## Key Scripts

| Script | Path |
|---|---|
| `RoundGenerator` | `Assets/Scripts/LevelGeneration/RoundGenerator.cs` |
| `LevelBlueprint` | `Assets/Scripts/LevelGeneration/LevelBlueprint.cs` |
| `LevelLoader` | `Assets/Scripts/LevelGeneration/LevelLoader.cs` |
| `ArenaLayoutTemplate` | `Assets/Scripts/LevelGeneration/ArenaLayoutTemplate.cs` |
| `EnemyWaveConfig` | `Assets/Scripts/LevelGeneration/EnemyWaveConfig.cs` |
| `EnemySpawnPoint` | `Assets/Scripts/LevelGeneration/EnemySpawnPoint.cs` |
| `LevelTransitionType` | `Assets/Scripts/LevelGeneration/LevelTransitionType.cs` |

---

## Data Model

```
Round
  └─ List<LevelBlueprint>  (3 levels per round)
        ├─ ArenaLayoutTemplate   (ScriptableObject — defines the physical arena)
        ├─ List<EnemyWaveConfig> (ScriptableObjects — which enemies, how many)
        └─ LevelTransitionType   (ScriptableObject — transition style)
```

`LevelBlueprint` is a plain C# class (not a MonoBehaviour or ScriptableObject) — it is a runtime data bag assembled from ScriptableObject assets.

---

## Generation Flow

`RoundGenerator.GenerateNewRound()` (singleton):
1. Loops 3 times.
2. Picks a random `ArenaLayoutTemplate` from `LayoutPool`.
3. Picks a random `LevelTransitionType` from `TransitionPool`.
4. Generates a random number of waves (`MinWavesPerLevel`–`MaxWavesPerLevel`) and picks a random `EnemyWaveConfig` for each.
5. Returns a `List<LevelBlueprint>`.

`LevelLoader` consumes the blueprints to actually load and set up each level scene.

---

## Adding Content

- **New arena**: create an `ArenaLayoutTemplate` ScriptableObject asset and add it to `RoundGenerator.LayoutPool` in the Inspector.
- **New enemy wave**: create an `EnemyWaveConfig` ScriptableObject and add it to `RoundGenerator.WavePool`.
- **New transition**: create a `LevelTransitionType` ScriptableObject and add it to `RoundGenerator.TransitionPool`.

No code changes required to add content — the system is data-driven.
