using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Golden-fixture coverage for the Commit 21 encounter pipeline.
/// EncounterBuilder is a thin delegator to WaveComposer, so these tests pin the
/// deterministic, RNG-independent outputs (threat budget, wave count, elite rule,
/// HP/damage multipliers) for three reference slots: block0-combat0, block0-combat2,
/// block1-combat0 — plus argument guards and delegation equivalence.
/// </summary>
public class EncounterBuilderTest
{
    // --- Argument guards ---

    [Test]
    public void Build_NullCatalog_Throws()
    {
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        var context = new EncounterContext { RunSeed = 1, GlobalStepIndex = 0, BlockIndex = 0, CombatIndexInBlock = 0 };
        Assert.Throws<ArgumentNullException>(() => EncounterBuilder.Build(context, null, settings));
    }

    [Test]
    public void Build_NullSettings_Throws()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        var context = new EncounterContext { RunSeed = 1, GlobalStepIndex = 0, BlockIndex = 0, CombatIndexInBlock = 0 };
        Assert.Throws<ArgumentNullException>(() => EncounterBuilder.Build(context, catalog, null));
    }

    // --- Delegation equivalence: Build == Compose for the same inputs ---

    [Test]
    public void Build_MatchesWaveComposerCompose()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        var context = new EncounterContext { RunSeed = 123, GlobalStepIndex = 2, BlockIndex = 0, CombatIndexInBlock = 2 };

        CombatEncounter viaBuilder = EncounterBuilder.Build(context, catalog, settings);
        CombatEncounter viaComposer = WaveComposer.Compose(context, catalog, settings);

        Assert.AreEqual(viaComposer.Theme, viaBuilder.Theme);
        Assert.AreEqual(viaComposer.ThreatBudget, viaBuilder.ThreatBudget, 0.001f);
        Assert.AreEqual(viaComposer.WaveCount, viaBuilder.WaveCount);
        for (int w = 0; w < viaComposer.WaveCount; w++)
        {
            Assert.AreEqual(viaComposer.Waves[w].Spawns.Count, viaBuilder.Waves[w].Spawns.Count);
            for (int i = 0; i < viaComposer.Waves[w].Spawns.Count; i++)
            {
                Assert.AreEqual(viaComposer.Waves[w].Spawns[i].ArchetypeId, viaBuilder.Waves[w].Spawns[i].ArchetypeId);
                Assert.AreEqual(viaComposer.Waves[w].Spawns[i].IsElite, viaBuilder.Waves[w].Spawns[i].IsElite);
            }
        }
    }

    // --- Golden fixtures (deterministic across seeds) ---

    [Test]
    public void Build_Block0Combat0_Golden()
    {
        // Profile 0: budget 80, exactly 2 waves, no elite, block-0 → HP/damage x1.
        CombatEncounter e = Build(blockIndex: 0, combatIndexInBlock: 0, globalStepIndex: 0);

        Assert.AreEqual(80f, e.ThreatBudget, 0.001f, "block0-combat0 per-wave budget");
        Assert.AreEqual(2, e.WaveCount, "block0-combat0 wave count (minWaves==maxWaves==2)");
        Assert.IsFalse(HasElite(e), "block0-combat0 must not guarantee an elite");
        Assert.AreEqual(1f, e.DifficultyModifiers.HpMultiplier, 0.0001f);
        Assert.AreEqual(1f, e.DifficultyModifiers.DamageMultiplier, 0.0001f);
        AssertEveryWaveNonEmptyAndKnown(e);
    }

    [Test]
    public void Build_Block0Combat2_Golden()
    {
        // Profile 2: budget 140, exactly 3 waves, guaranteed elite, block-0 → HP/damage x1.
        CombatEncounter e = Build(blockIndex: 0, combatIndexInBlock: 2, globalStepIndex: 2);

        Assert.AreEqual(140f, e.ThreatBudget, 0.001f, "block0-combat2 per-wave budget");
        Assert.AreEqual(3, e.WaveCount, "block0-combat2 wave count (minWaves==maxWaves==3)");
        Assert.IsTrue(HasElite(e), "block0-combat2 must guarantee an elite");
        Assert.AreEqual(1f, e.DifficultyModifiers.HpMultiplier, 0.0001f);
        Assert.AreEqual(1f, e.DifficultyModifiers.DamageMultiplier, 0.0001f);
        AssertEveryWaveNonEmptyAndKnown(e);
    }

    [Test]
    public void Build_Block1Combat0_Golden()
    {
        // Profile 0 scaled by block 1: budget 80*(1+0.35)=108, 2 waves, no elite,
        // block-1 → HP x(1+0.25)=1.25, damage x(1+0.1)=1.1.
        CombatEncounter e = Build(blockIndex: 1, combatIndexInBlock: 0, globalStepIndex: 4);

        Assert.AreEqual(108f, e.ThreatBudget, 0.001f, "block1-combat0 per-wave budget (80 * 1.35)");
        Assert.AreEqual(2, e.WaveCount, "block1-combat0 wave count");
        Assert.IsFalse(HasElite(e), "block1-combat0 must not guarantee an elite");
        Assert.AreEqual(1.25f, e.DifficultyModifiers.HpMultiplier, 0.0001f, "block-1 HP scale");
        Assert.AreEqual(1.1f, e.DifficultyModifiers.DamageMultiplier, 0.0001f, "block-1 damage scale");
        AssertEveryWaveNonEmptyAndKnown(e);
    }

    [Test]
    public void Build_LaterBlockAndCombat_AreStrictlyHarder()
    {
        // Cross-check the curve monotonicity through the builder entry point.
        CombatEncounter b0c0 = Build(0, 0, 0);
        CombatEncounter b0c2 = Build(0, 2, 2);
        CombatEncounter b1c0 = Build(1, 0, 4);

        Assert.Greater(b0c2.ThreatBudget, b0c0.ThreatBudget, "combat 2 outspends combat 0 within a block");
        Assert.Greater(b1c0.ThreatBudget, b0c0.ThreatBudget, "later block outspends block 0");
        Assert.Greater(b1c0.DifficultyModifiers.HpMultiplier, b0c0.DifficultyModifiers.HpMultiplier, "later block is tankier");
    }

    // --- Helpers ---

    private static CombatEncounter Build(int blockIndex, int combatIndexInBlock, int globalStepIndex)
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        var context = new EncounterContext
        {
            RunSeed = 4242,
            GlobalStepIndex = globalStepIndex,
            BlockIndex = blockIndex,
            CombatIndexInBlock = combatIndexInBlock,
        };
        return EncounterBuilder.Build(context, catalog, settings);
    }

    private static void AssertEveryWaveNonEmptyAndKnown(CombatEncounter encounter)
    {
        EnemyCatalog catalog = BuildTestCatalog();
        foreach (ComposedWave wave in encounter.Waves)
        {
            Assert.Greater(wave.Spawns.Count, 0, "no wave should be empty");
            foreach (ComposedSpawnSpec spawn in wave.Spawns)
            {
                Assert.IsTrue(catalog.TryGetById(spawn.ArchetypeId, out _), $"unknown archetype id {spawn.ArchetypeId}");
            }
        }
    }

    private static bool HasElite(CombatEncounter encounter)
    {
        foreach (ComposedWave wave in encounter.Waves)
        {
            foreach (ComposedSpawnSpec spawn in wave.Spawns)
            {
                if (spawn.IsElite)
                {
                    return true;
                }
            }
        }

        return false;
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
