#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-tunable parameters and content pools for run generation.
/// Linear runs use the Combat / Shop content fields; branching-map shape fields are DEPRECATED.
/// </summary>
[Serializable]
public class MapGenerationSettings
{
    [Header("DEPRECATED — branching map shape")]
    [Obsolete("DEPRECATED: Branching map shape; linear runs ignore these fields.")]
    [Tooltip("Total columns including the pre-boss Rest column and the final Boss column. ~5-7 for a short run.")]
    [Min(3)] public int columns = 6;
    [Obsolete("DEPRECATED: Branching map shape; linear runs ignore these fields.")]
    [Min(1)] public int minNodesPerColumn = 1;
    [Obsolete("DEPRECATED: Branching map shape; linear runs ignore these fields.")]
    [Min(1)] public int maxNodesPerColumn = 3;
    [Obsolete("DEPRECATED: Branching map shape; linear runs ignore these fields.")]
    [Range(0f, 1f)] public float extraEdgeChance = 0.35f;

    [Header("DEPRECATED — branching map distribution")]
    [Obsolete("DEPRECATED: Branching map distribution; linear runs ignore these fields.")]
    [Tooltip("Convert roughly one interior combat node to an Augment node every N combats.")]
    [Min(1)] public int augmentEveryNCombats = 2;
    [Obsolete("DEPRECATED: Branching map distribution; linear runs ignore these fields.")]
    [Tooltip("How many Shop nodes to place in the interior (mid-run).")]
    [Min(0)] public int shopCount = 1;

    [Header("Combat content")]
    [Min(1)] public int minWavesPerCombat = 2;
    [Min(1)] public int maxWavesPerCombat = 4;
    public List<ArenaLayoutTemplate> combatLayouts = new List<ArenaLayoutTemplate>();
    [Obsolete("DEPRECATED: Prefer WaveComposer + EnemyCatalog (Commit 21). Kept for branching-map / legacy fallback.")]
    public List<EnemyWaveConfig> combatWaves = new List<EnemyWaveConfig>();

    [Header("Shop content")]
    public ArenaLayoutTemplate? shopLayout;

    [Header("DEPRECATED — branching map Boss content")]
    [Obsolete("DEPRECATED: Branching map boss node; linear runs have no boss node.")]
    public ArenaLayoutTemplate? bossLayout;
    [Obsolete("DEPRECATED: Branching map boss node; linear runs have no boss node.")]
    [Tooltip("Boss wave(s) - e.g. a single high-HP placeholder enemy.")]
    public List<EnemyWaveConfig> bossWaves = new List<EnemyWaveConfig>();
}
