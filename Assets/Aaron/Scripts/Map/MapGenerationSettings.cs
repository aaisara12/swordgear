#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-tunable parameters and content pools for procedural run-map generation.
/// Plain serializable class so it can be edited on <see cref="RunManager"/> and constructed in tests.
/// </summary>
[Serializable]
public class MapGenerationSettings
{
    [Header("Shape")]
    [Tooltip("Total columns including the pre-boss Rest column and the final Boss column. ~5-7 for a short run.")]
    [Min(3)] public int columns = 6;
    [Min(1)] public int minNodesPerColumn = 1;
    [Min(1)] public int maxNodesPerColumn = 3;
    [Range(0f, 1f)] public float extraEdgeChance = 0.35f;

    [Header("Distribution")]
    [Tooltip("Convert roughly one interior combat node to an Augment node every N combats.")]
    [Min(1)] public int augmentEveryNCombats = 2;
    [Tooltip("How many Shop nodes to place in the interior (mid-run).")]
    [Min(0)] public int shopCount = 1;

    [Header("Combat content")]
    [Min(1)] public int minWavesPerCombat = 2;
    [Min(1)] public int maxWavesPerCombat = 4;
    public List<ArenaLayoutTemplate> combatLayouts = new List<ArenaLayoutTemplate>();
    public List<EnemyWaveConfig> combatWaves = new List<EnemyWaveConfig>();

    [Header("Shop / Boss content")]
    public ArenaLayoutTemplate? shopLayout;
    public ArenaLayoutTemplate? bossLayout;
    [Tooltip("Boss wave(s) - e.g. a single high-HP placeholder enemy.")]
    public List<EnemyWaveConfig> bossWaves = new List<EnemyWaveConfig>();
}
