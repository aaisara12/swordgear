#nullable enable

using System.Collections.Generic;

/// <summary>
/// Runtime data for a single node on the run map. Plain C# (like LevelBlueprint) - assembled from
/// ScriptableObject content and held by <see cref="RunManager"/> across Map/Arena scene swaps.
/// </summary>
public class MapNode
{
    public int Id { get; }
    public NodeType Type { get; }

    // Layout/coordinates used for map UI rendering.
    public int Column { get; }
    public int Row { get; }

    // Ids of nodes in the next column reachable from this one.
    public List<int> Next { get; } = new List<int>();

    public bool Completed { get; set; }

    // Content payload (only relevant for Arena-loaded nodes: Combat / Boss / Shop).
    public ArenaLayoutTemplate? Layout { get; set; }
    public List<EnemyWaveConfig> Waves { get; set; } = new List<EnemyWaveConfig>();

    public bool IsBoss => Type == NodeType.Boss;

    // Combat, Boss, and Shop load the Arena scene; Augment and Rest are handled as overlays on the map.
    public bool LoadsArena => Type == NodeType.Combat || Type == NodeType.Boss || Type == NodeType.Shop;

    public MapNode(int id, NodeType type, int column, int row)
    {
        Id = id;
        Type = type;
        Column = column;
        Row = row;
    }
}
