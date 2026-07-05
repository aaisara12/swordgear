#nullable enable

#pragma warning disable CS0618 // Intentionally exercises deprecated branching map types.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
[TestOf(typeof(MapGenerator))]
[Obsolete("DEPRECATED: Tests for the retired branching map generator.")]
public class MapGeneratorTest
{
    private static MapGenerationSettings MakeSettings()
    {
        // Content pools left empty; structure generation is independent of content (assignment is null-safe).
        return new MapGenerationSettings
        {
            columns = 6,
            minNodesPerColumn = 1,
            maxNodesPerColumn = 3,
            extraEdgeChance = 0.35f,
            augmentEveryNCombats = 2,
            shopCount = 1,
            minWavesPerCombat = 2,
            maxWavesPerCombat = 4
        };
    }

    private static bool BossReachableFromStart(RunMap map)
    {
        int firstColumn = map.Nodes.Min(n => n.Column);
        var start = map.Nodes.Where(n => n.Column == firstColumn).Select(n => n.Id);

        var visited = new HashSet<int>();
        var queue = new Queue<int>(start);

        while (queue.Count > 0)
        {
            int id = queue.Dequeue();
            if (!visited.Add(id))
            {
                continue;
            }

            MapNode? node = map.GetNode(id);
            if (node == null)
            {
                continue;
            }

            if (node.IsBoss)
            {
                return true;
            }

            foreach (int next in node.Next)
            {
                queue.Enqueue(next);
            }
        }

        return false;
    }

    [Test]
    public void Generate_Boss_IsReachableFromStart()
    {
        for (int seed = 0; seed < 25; seed++)
        {
            RunMap map = MapGenerator.Generate(MakeSettings(), seed);
            Assert.IsTrue(BossReachableFromStart(map), $"Boss should be reachable for seed {seed}.");
        }
    }

    [Test]
    public void Generate_HasExactlyOneBoss_AndPrecededByRest()
    {
        RunMap map = MapGenerator.Generate(MakeSettings(), seed: 7);

        var bosses = map.Nodes.Where(n => n.Type == NodeType.Boss).ToList();
        Assert.AreEqual(1, bosses.Count, "There must be exactly one boss node.");

        int bossColumn = bosses[0].Column;
        bool restBeforeBoss = map.Nodes.Any(n => n.Type == NodeType.Rest && n.Column == bossColumn - 1);
        Assert.IsTrue(restBeforeBoss, "There must be a Rest node in the column before the boss.");
    }

    [Test]
    public void Generate_DistributesShopAndAugmentNodes()
    {
        RunMap map = MapGenerator.Generate(MakeSettings(), seed: 3);

        int shops = map.Nodes.Count(n => n.Type == NodeType.Shop);
        int augments = map.Nodes.Count(n => n.Type == NodeType.Augment);

        Assert.GreaterOrEqual(shops, 1, "Expected at least one Shop node.");
        Assert.GreaterOrEqual(augments, 1, "Expected at least one Augment node.");
    }

    [Test]
    public void Generate_FirstColumn_IsAllCombat()
    {
        RunMap map = MapGenerator.Generate(MakeSettings(), seed: 11);
        int firstColumn = map.Nodes.Min(n => n.Column);

        Assert.IsTrue(
            map.Nodes.Where(n => n.Column == firstColumn).All(n => n.Type == NodeType.Combat),
            "The first column should always be Combat (a guaranteed opener).");
    }

    [Test]
    public void Generate_EveryNonFirstNode_HasIncomingEdge()
    {
        RunMap map = MapGenerator.Generate(MakeSettings(), seed: 5);
        int firstColumn = map.Nodes.Min(n => n.Column);

        var withIncoming = new HashSet<int>(map.Nodes.SelectMany(n => n.Next));

        foreach (MapNode node in map.Nodes.Where(n => n.Column != firstColumn))
        {
            Assert.IsTrue(withIncoming.Contains(node.Id), $"Node {node.Id} (col {node.Column}) has no incoming edge.");
        }
    }

    [Test]
    public void Generate_IsDeterministic_ForSameSeed()
    {
        RunMap a = MapGenerator.Generate(MakeSettings(), seed: 42);
        RunMap b = MapGenerator.Generate(MakeSettings(), seed: 42);

        Assert.AreEqual(Signature(a), Signature(b), "Same seed should produce identical maps.");
    }

    [Test]
    public void Generate_DiffersForDifferentSeeds()
    {
        RunMap a = MapGenerator.Generate(MakeSettings(), seed: 1);
        RunMap b = MapGenerator.Generate(MakeSettings(), seed: 999);

        Assert.AreNotEqual(Signature(a), Signature(b), "Different seeds should (almost always) produce different maps.");
    }

    private static string Signature(RunMap map)
    {
        return string.Join("|", map.Nodes
            .OrderBy(n => n.Id)
            .Select(n => $"{n.Id}:{n.Type}:{n.Column}:{n.Row}:[{string.Join(",", n.Next.OrderBy(x => x))}]"));
    }

    [Test]
    public void FixedMapDefinition_BuildRunMap_PassesThroughVerbatim()
    {
        var definition = ScriptableObject.CreateInstance<FixedMapDefinition>();

        var entries = new List<FixedMapDefinition.Entry>
        {
            new FixedMapDefinition.Entry { id = 0, type = NodeType.Combat, column = 0, row = 0, next = new List<int> { 1 } },
            new FixedMapDefinition.Entry { id = 1, type = NodeType.Rest, column = 1, row = 0, next = new List<int> { 2 } },
            new FixedMapDefinition.Entry { id = 2, type = NodeType.Boss, column = 2, row = 0, next = new List<int>() }
        };

        FieldInfo field = typeof(FixedMapDefinition).GetField("nodes", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(definition, entries);

        RunMap map = definition.BuildRunMap();

        Assert.AreEqual(3, map.Nodes.Count);
        Assert.AreEqual(NodeType.Combat, map.GetNode(0)!.Type);
        Assert.AreEqual(NodeType.Rest, map.GetNode(1)!.Type);
        Assert.AreEqual(NodeType.Boss, map.GetNode(2)!.Type);
        CollectionAssert.AreEqual(new[] { 1 }, map.GetNode(0)!.Next);
        CollectionAssert.AreEqual(new[] { 2 }, map.GetNode(1)!.Next);

        UnityEngine.Object.DestroyImmediate(definition);
    }
}
