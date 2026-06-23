#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dev/testing override: a hand-authored run map. When assigned on <see cref="RunManager"/>,
/// it is used verbatim instead of procedural generation, giving a deterministic level progression.
/// </summary>
[CreateAssetMenu(fileName = "FixedMapDefinition", menuName = "Scriptable Objects/Map/FixedMapDefinition")]
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
