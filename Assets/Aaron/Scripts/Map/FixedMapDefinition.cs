#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DEPRECATED — hand-authored branching map override; linear runs use <see cref="LinearRunGenerator"/> instead.
/// </summary>
[CreateAssetMenu(fileName = "FixedMapDefinition", menuName = "Scriptable Objects/Map/FixedMapDefinition (DEPRECATED)")]
[Obsolete("DEPRECATED: Branching map override replaced by LinearRunGenerator. Retained for reference.")]
public class FixedMapDefinition : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int id;
        public NodeType type;
        public int column;
        public int row;
        public List<int> next = new List<int>();
        public ArenaLayoutTemplate? layout;
        public List<EnemyWaveConfig> waves = new List<EnemyWaveConfig>();
    }

    [SerializeField] private List<Entry> nodes = new List<Entry>();

    public RunMap BuildRunMap()
    {
        var mapNodes = new List<MapNode>();
        foreach (Entry entry in nodes)
        {
            var node = new MapNode(entry.id, entry.type, entry.column, entry.row)
            {
                Layout = entry.layout,
                Waves = new List<EnemyWaveConfig>(entry.waves)
            };
            node.Next.AddRange(entry.next);
            mapNodes.Add(node);
        }
        return new RunMap(mapNodes);
    }
}
