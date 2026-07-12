using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Commit 24 — seeded arena selection: each combat step gets a layout picked from the pool, the choice is
/// deterministic per seed, <see cref="RunManager.ResolveCombatLayout"/> returns the current step's pick, and
/// the choice actually depends on the seed (guards against always loading one arena).
/// </summary>
public class ArenaSelectionTest
{
    private readonly List<Object> _spawned = new List<Object>();

    [TearDown]
    public void Cleanup()
    {
        foreach (Object o in _spawned)
        {
            if (o != null)
            {
                Object.DestroyImmediate(o);
            }
        }

        _spawned.Clear();
    }

    [Test]
    public void Selection_IsDeterministic_AndFromPool()
    {
        List<ArenaLayoutTemplate> pool = BuildPool(3);
        RunManager a = BuildManager(pool, seed: 4242);
        RunManager b = BuildManager(pool, seed: 4242);
        a.EnsureRunStarted();
        b.EnsureRunStarted();

        IReadOnlyList<RunStep> stepsA = a.Run.Steps;
        IReadOnlyList<RunStep> stepsB = b.Run.Steps;
        Assert.AreEqual(stepsA.Count, stepsB.Count);
        for (int i = 0; i < stepsA.Count; i++)
        {
            if (stepsA[i].Type != RunStepType.Combat)
            {
                continue;
            }

            Assert.IsNotNull(stepsA[i].Layout, $"Combat step {i} should have a layout.");
            Assert.Contains(stepsA[i].Layout, pool, "Layout must come from the pool.");
            Assert.AreSame(stepsA[i].Layout, stepsB[i].Layout, "Same seed must reproduce the same arena per step.");
        }
    }

    [Test]
    public void ResolveCombatLayout_ReturnsCurrentStepLayout()
    {
        List<ArenaLayoutTemplate> pool = BuildPool(3);
        RunManager manager = BuildManager(pool, seed: 7);
        manager.EnsureRunStarted();

        Assert.AreEqual(RunStepType.Combat, manager.Run.CurrentStep.Type);
        Assert.AreSame(manager.Run.CurrentStep.Layout, manager.ResolveCombatLayout());
    }

    [Test]
    public void Selection_DependsOnSeed()
    {
        List<ArenaLayoutTemplate> pool = BuildPool(3);
        var chosenForFirstCombat = new HashSet<ArenaLayoutTemplate>();
        for (int seed = 0; seed < 10; seed++)
        {
            RunManager manager = BuildManager(pool, seed);
            manager.EnsureRunStarted();
            chosenForFirstCombat.Add(manager.Run.Steps[0].Layout);
        }

        Assert.Greater(chosenForFirstCombat.Count, 1,
            "First combat's arena should vary across seeds, not be hard-wired to one.");
    }

    private RunManager BuildManager(List<ArenaLayoutTemplate> pool, int seed)
    {
        var go = new GameObject("RunManagerArenaTest");
        _spawned.Add(go);
        RunManager manager = go.AddComponent<RunManager>();

        var generation = new MapGenerationSettings { combatLayouts = new List<ArenaLayoutTemplate>(pool) };
        SetPrivate(manager, "generationSettings", generation);
        SetPrivate(manager, "useRandomSeed", false);
        SetPrivate(manager, "fixedSeed", seed);
        return manager;
    }

    private List<ArenaLayoutTemplate> BuildPool(int count)
    {
        var pool = new List<ArenaLayoutTemplate>(count);
        for (int i = 0; i < count; i++)
        {
            var t = ScriptableObject.CreateInstance<ArenaLayoutTemplate>();
            t.name = $"PoolArena_{i}";
            _spawned.Add(t);
            pool.Add(t);
        }

        return pool;
    }

    private static void SetPrivate(object target, string field, object value)
    {
        FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(info, $"Field '{field}' not found on {target.GetType().Name}.");
        info.SetValue(target, value);
    }
}
