#nullable enable

using System;

/// <summary>
/// One enemy to spawn in a composed wave (Commit 21). Commit 20 uses this shape for interim elite tagging.
/// </summary>
[Serializable]
public struct ComposedSpawnSpec
{
    public string ArchetypeId;
    public bool IsElite;

    public ComposedSpawnSpec(string archetypeId, bool isElite = false)
    {
        ArchetypeId = archetypeId;
        IsElite = isElite;
    }
}
