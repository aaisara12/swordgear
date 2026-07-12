#nullable enable

using System;
using System.Collections.Generic;

/// <summary>One wave inside a <see cref="CombatEncounter"/>.</summary>
[Serializable]
public class ComposedWave
{
    public List<ComposedSpawnSpec> Spawns = new();
    public float DelayAfterClear = 1.5f;
}
