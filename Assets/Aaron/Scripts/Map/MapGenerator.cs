#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DEPRECATED — branching map generator replaced by <see cref="LinearRunGenerator"/>.
/// Pure (non-MonoBehaviour) procedural generator for a Slay-the-Spire style branching run map.
/// Retained for reference and unit tests; safe to delete once no longer needed.
/// </summary>
[Obsolete("DEPRECATED: Branching map replaced by LinearRunGenerator. Retained for reference.")]
public static class MapGenerator
{
    public static RunMap Generate(MapGenerationSettings settings, int seed)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        int columnCount = Math.Max(3, settings.columns);
        int minNodes = Math.Max(1, settings.minNodesPerColumn);
        int maxNodes = Math.Max(minNodes, settings.maxNodesPerColumn);

        Random rng = new Random(seed);

        var columns = new List<List<MapNode>>();
        int nextId = 0;

        for (int col = 0; col < columnCount; col++)
        {
            var columnNodes = new List<MapNode>();

            if (col == columnCount - 1)
            {
                // Final column: single Boss.
                columnNodes.Add(new MapNode(nextId++, NodeType.Boss, col, 0));
            }
            else if (col == columnCount - 2)
            {
                // Pre-boss column: single Rest (guaranteed rest before the boss).
                columnNodes.Add(new MapNode(nextId++, NodeType.Rest, col, 0));
            }
            else
            {
                int count = rng.Next(minNodes, maxNodes + 1);
                for (int row = 0; row < count; row++)
                {
                    // Default everything to Combat; specials are assigned afterwards.
                    columnNodes.Add(new MapNode(nextId++, NodeType.Combat, col, row));
                }
            }

            columns.Add(columnNodes);
        }

        AssignSpecialNodeTypes(columns, settings, rng, columnCount);
        ConnectColumns(columns, settings, rng);
        AssignContent(columns, settings, rng);

        return new RunMap(columns.SelectMany(c => c));
    }

    // Interior columns are indices 1 .. columnCount-3 (column 0 stays all-Combat as a guaranteed opener).
    private static void AssignSpecialNodeTypes(
        List<List<MapNode>> columns, MapGenerationSettings settings, Random rng, int columnCount)
    {
        var interiorCombat = new List<MapNode>();
        for (int col = 1; col <= columnCount - 3; col++)
        {
            interiorCombat.AddRange(columns[col].Where(n => n.Type == NodeType.Combat));
        }

        if (interiorCombat.Count == 0)
        {
            return;
        }

        // Shops: convert a few mid-run combat nodes.
        for (int s = 0; s < settings.shopCount && interiorCombat.Count > 0; s++)
        {
            MapNode shop = interiorCombat[rng.Next(interiorCombat.Count)];
            ReplaceNodeType(columns, shop, NodeType.Shop);
            interiorCombat.Remove(shop);
        }

        // Augments: roughly one per N combats.
        int augmentCount = Math.Max(1, interiorCombat.Count / Math.Max(1, settings.augmentEveryNCombats));
        for (int a = 0; a < augmentCount && interiorCombat.Count > 0; a++)
        {
            MapNode aug = interiorCombat[rng.Next(interiorCombat.Count)];
            ReplaceNodeType(columns, aug, NodeType.Augment);
            interiorCombat.Remove(aug);
        }
    }

    private static void ReplaceNodeType(List<List<MapNode>> columns, MapNode node, NodeType type)
    {
        var replacement = new MapNode(node.Id, type, node.Column, node.Row);
        var column = columns[node.Column];
        int index = column.IndexOf(node);
        column[index] = replacement;
    }

    private static void ConnectColumns(List<List<MapNode>> columns, MapGenerationSettings settings, Random rng)
    {
        for (int col = 0; col < columns.Count - 1; col++)
        {
            var current = columns[col];
            var next = columns[col + 1];

            // Every node connects forward to at least one node in the next column.
            foreach (MapNode node in current)
            {
                MapNode target = next[rng.Next(next.Count)];
                AddEdge(node, target);

                // Occasionally branch to a second (adjacent) node for variety.
                if (next.Count > 1 && rng.NextDouble() < settings.extraEdgeChance)
                {
                    MapNode second = next[rng.Next(next.Count)];
                    AddEdge(node, second);
                }
            }

            // Ensure every node in the next column has at least one incoming edge.
            foreach (MapNode target in next)
            {
                bool hasIncoming = current.Any(n => n.Next.Contains(target.Id));
                if (!hasIncoming)
                {
                    MapNode source = current[rng.Next(current.Count)];
                    AddEdge(source, target);
                }
            }
        }
    }

    private static void AddEdge(MapNode from, MapNode to)
    {
        if (!from.Next.Contains(to.Id))
        {
            from.Next.Add(to.Id);
        }
    }

    private static void AssignContent(List<List<MapNode>> columns, MapGenerationSettings settings, Random rng)
    {
        int minWaves = Math.Max(1, settings.minWavesPerCombat);
        int maxWaves = Math.Max(minWaves, settings.maxWavesPerCombat);

        foreach (MapNode node in columns.SelectMany(c => c))
        {
            switch (node.Type)
            {
                case NodeType.Combat:
                    node.Layout = PickRandom(settings.combatLayouts, rng);
                    node.Waves = BuildWaves(settings.combatWaves, rng.Next(minWaves, maxWaves + 1), rng);
                    break;
                case NodeType.Boss:
                    node.Layout = settings.bossLayout;
                    node.Waves = new List<EnemyWaveConfig>(settings.bossWaves);
                    break;
                case NodeType.Shop:
                    node.Layout = settings.shopLayout;
                    break;
                // Augment / Rest are overlays; no arena content.
            }
        }
    }

    private static List<EnemyWaveConfig> BuildWaves(List<EnemyWaveConfig> pool, int count, Random rng)
    {
        var waves = new List<EnemyWaveConfig>();
        if (pool == null || pool.Count == 0)
        {
            return waves;
        }

        for (int i = 0; i < count; i++)
        {
            waves.Add(pool[rng.Next(pool.Count)]);
        }
        return waves;
    }

    private static T? PickRandom<T>(List<T> pool, Random rng) where T : class
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }
        return pool[rng.Next(pool.Count)];
    }
}
