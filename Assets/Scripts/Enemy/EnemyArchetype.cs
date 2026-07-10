#nullable enable

using System;
using UnityEngine;

/// <summary>One catalog entry for a spawnable enemy prefab (20 total: 5 roles × 4 elements).</summary>
[Serializable]
public class EnemyArchetype
{
    [Tooltip("Stable id, e.g. melee_physical or turret_fire.")]
    public string id = string.Empty;

    public GameObject? prefab;
    public Element element = Element.Physical;
    public EnemyRole role = EnemyRole.Melee;

    [Min(0.1f)]
    [Tooltip("Threat cost spent by WaveComposer (Commit 21).")]
    public float baseThreatCost = 10f;

    [Tooltip(
        "When true, LevelLoader applies catalog ElementStatKnobs at spawn (Physical-baseline prefabs). " +
        "When false, elemental variance is already baked into the prefab (legacy melee/ranged).")]
    public bool applyElementKnobsAtSpawn = true;
}
