using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Commit 22 — verifies pre-roll promotion: every queued combat step gets its encounter composed and cached,
/// the cached encounter matches an on-demand composition for the same step (caching never diverges), and the
/// same seed reproduces the same block.
/// </summary>
public class RunQueuePromotionTest
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
    public void Preroll_ComposesAllQueuedCombats_MatchingOnDemand()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        RunManager manager = BuildManager(catalog, settings, seed: 777);

        manager.EnsureRunStarted();
        LinearRunState run = manager.Run;
        Assert.IsNotNull(run, "Run should be generated.");

        // Every queued combat step is pre-rolled; upgrade steps stay null.
        AssertAllCombatsComposedAndMatch(run, catalog, settings);

        // Advance to the trailing upgrade step and queue the next block — its combats must pre-roll too.
        for (int i = 0; i < LinearRunGenerator.CombatsPerBlock; i++)
        {
            run.TryAdvanceToNextStep();
        }

        Assert.AreEqual(RunStepType.Upgrade, run.CurrentStep.Type, "Should be on the trailing upgrade step.");
        int before = run.Steps.Count;
        Assert.IsTrue(manager.EnsureMoreStepsQueued(), "A new block should be appended at the trailing upgrade.");
        Assert.Greater(run.Steps.Count, before, "Queue should grow by a block.");

        AssertAllCombatsComposedAndMatch(run, catalog, settings);
    }

    [Test]
    public void Preroll_SameSeed_ReproducesBlock()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();

        RunManager a = BuildManager(catalog, settings, seed: 4242);
        RunManager b = BuildManager(catalog, settings, seed: 4242);
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

            AssertEncountersEqual(stepsA[i].Encounter, stepsB[i].Encounter);
        }
    }

    private static void AssertAllCombatsComposedAndMatch(
        LinearRunState run, EnemyCatalog catalog, WaveComposerSettings settings)
    {
        foreach (RunStep step in run.Steps)
        {
            if (step.Type != RunStepType.Combat)
            {
                Assert.IsNull(step.Encounter, "Upgrade steps should not be composed.");
                continue;
            }

            Assert.IsNotNull(step.Encounter, $"Combat step {step.StepIndex} should be pre-rolled.");

            // Independent on-demand composition for the same step must match the cached one.
            var ctx = new EncounterContext
            {
                RunSeed = run.Seed,
                GlobalStepIndex = step.StepIndex,
                BlockIndex = step.StepIndex / LinearRunGenerator.StepsPerBlock,
                CombatIndexInBlock = step.StepIndex % LinearRunGenerator.StepsPerBlock,
            };
            CombatEncounter onDemand = EncounterBuilder.Build(ctx, catalog, settings);
            AssertEncountersEqual(onDemand, step.Encounter);
        }
    }

    private RunManager BuildManager(EnemyCatalog catalog, WaveComposerSettings settings, int seed)
    {
        var go = new GameObject("RunManagerTest");
        _spawned.Add(go);
        RunManager manager = go.AddComponent<RunManager>();

        var generation = new MapGenerationSettings();
        generation.combatLayouts = new List<ArenaLayoutTemplate> { ScriptableObject.CreateInstance<ArenaLayoutTemplate>() };

        SetPrivate(manager, "generationSettings", generation);
        SetPrivate(manager, "enemyCatalog", catalog);
        SetPrivate(manager, "waveComposerSettings", settings);
        SetPrivate(manager, "useRandomSeed", false);
        SetPrivate(manager, "fixedSeed", seed);
        return manager;
    }

    private static void SetPrivate(object target, string field, object value)
    {
        FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(info, $"Field '{field}' not found on {target.GetType().Name}.");
        info.SetValue(target, value);
    }

    private static void AssertEncountersEqual(CombatEncounter a, CombatEncounter b)
    {
        Assert.IsNotNull(a);
        Assert.IsNotNull(b);
        Assert.AreEqual(a.Theme, b.Theme);
        Assert.AreEqual(a.ThreatBudget, b.ThreatBudget, 0.001f);
        Assert.AreEqual(a.WaveCount, b.WaveCount);
        for (int w = 0; w < a.WaveCount; w++)
        {
            Assert.AreEqual(a.Waves[w].Spawns.Count, b.Waves[w].Spawns.Count);
            for (int i = 0; i < a.Waves[w].Spawns.Count; i++)
            {
                Assert.AreEqual(a.Waves[w].Spawns[i].ArchetypeId, b.Waves[w].Spawns[i].ArchetypeId);
                Assert.AreEqual(a.Waves[w].Spawns[i].IsElite, b.Waves[w].Spawns[i].IsElite);
            }
        }
    }

    private static EnemyCatalog BuildTestCatalog()
    {
        var entries = new List<EnemyArchetype>();
        EnemyRole[] roles = { EnemyRole.Melee, EnemyRole.Ranged, EnemyRole.BeamSniper, EnemyRole.Shotgun, EnemyRole.Turret };
        Element[] elements = { Element.Physical, Element.Fire, Element.Ice, Element.Lightning };
        float cost = 10f;
        foreach (EnemyRole role in roles)
        {
            foreach (Element element in elements)
            {
                entries.Add(new EnemyArchetype
                {
                    id = $"{role}_{element}".ToLowerInvariant(),
                    role = role,
                    element = element,
                    baseThreatCost = cost,
                    applyElementKnobsAtSpawn = false,
                    prefab = null,
                });
                cost += 0.5f;
            }
        }

        EnemyCatalog catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
        catalog.EditorSetArchetypes(entries);
        return catalog;
    }
}
