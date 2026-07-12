using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WaveComposerTest
{
    [Test]
    public void Compose_SameSeed_IdenticalEncounter()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        var context = new EncounterContext
        {
            RunSeed = 42,
            GlobalStepIndex = 0,
            BlockIndex = 0,
            CombatIndexInBlock = 0,
        };

        CombatEncounter a = WaveComposer.Compose(context, catalog, settings);
        CombatEncounter b = WaveComposer.Compose(context, catalog, settings);

        AssertEncountersEqual(a, b);
    }

    [Test]
    public void Compose_Block0Combat0_NoElite_LowerBudgetThanCombat2()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();

        var c0 = new EncounterContext { RunSeed = 7, GlobalStepIndex = 0, BlockIndex = 0, CombatIndexInBlock = 0 };
        var c2 = new EncounterContext { RunSeed = 7, GlobalStepIndex = 2, BlockIndex = 0, CombatIndexInBlock = 2 };

        CombatEncounter easy = EncounterBuilder.Build(c0, catalog, settings);
        CombatEncounter hard = EncounterBuilder.Build(c2, catalog, settings);

        Assert.IsFalse(HasElite(easy));
        Assert.IsTrue(HasElite(hard));
        Assert.Greater(hard.ThreatBudget, easy.ThreatBudget);
        Assert.AreEqual(easy.DifficultyModifiers.HpMultiplier, hard.DifficultyModifiers.HpMultiplier, 0.0001f);
        Assert.AreEqual(easy.DifficultyModifiers.DamageMultiplier, hard.DifficultyModifiers.DamageMultiplier, 0.0001f);
    }

    [Test]
    public void Compose_Block1HarderThanBlock0_SameCombatSlot()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();

        var b0 = new EncounterContext { RunSeed = 99, GlobalStepIndex = 0, BlockIndex = 0, CombatIndexInBlock = 0 };
        var b1 = new EncounterContext { RunSeed = 99, GlobalStepIndex = 4, BlockIndex = 1, CombatIndexInBlock = 0 };

        CombatEncounter early = EncounterBuilder.Build(b0, catalog, settings);
        CombatEncounter later = EncounterBuilder.Build(b1, catalog, settings);

        Assert.Greater(later.ThreatBudget, early.ThreatBudget);
        Assert.Greater(later.DifficultyModifiers.HpMultiplier, early.DifficultyModifiers.HpMultiplier);
    }

    [Test]
    public void Compose_UsesCatalogIds()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        var context = new EncounterContext { RunSeed = 3, GlobalStepIndex = 1, BlockIndex = 0, CombatIndexInBlock = 1 };

        CombatEncounter encounter = EncounterBuilder.Build(context, catalog, settings);
        Assert.Greater(encounter.WaveCount, 0);

        foreach (ComposedWave wave in encounter.Waves)
        {
            Assert.Greater(wave.Spawns.Count, 0);
            foreach (ComposedSpawnSpec spawn in wave.Spawns)
            {
                Assert.IsTrue(catalog.TryGetById(spawn.ArchetypeId, out _), $"Unknown id {spawn.ArchetypeId}");
            }
        }
    }

    [Test]
    public void Compose_ThreatBudget_IsPerWave_NotSplitAcrossWaves()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        settings.combatProfiles = new[]
        {
            new WaveComposerSettings.CombatProfile
            {
                threatBudget = 40f,
                minWaves = 3,
                maxWaves = 3,
                guaranteeElite = false,
            },
        };
        settings.maxEnemiesPerWave = 8;
        settings.minEnemiesPerWave = 1;
        // Cheap melee-only so budget converts cleanly into spawn counts.
        settings.roleWeightsCombat0 = new[] { 100f, 0f, 0f, 0f, 0f };
        settings.themeWeightsCombat0 = new[] { 100f, 0f, 0f, 0f, 0f };

        var context = new EncounterContext
        {
            RunSeed = 11,
            GlobalStepIndex = 0,
            BlockIndex = 0,
            CombatIndexInBlock = 0,
        };

        CombatEncounter encounter = EncounterBuilder.Build(context, catalog, settings);
        Assert.AreEqual(3, encounter.WaveCount);
        Assert.AreEqual(40f, encounter.ThreatBudget, 0.001f);

        // Per-wave budget 40 with ~10 cost melee → about 4 spawns/wave, ~12 total.
        // Old split would give wave 0 only ~6.7 budget → often 1 spawn and ~6–7 total.
        int totalSpawns = 0;
        foreach (ComposedWave wave in encounter.Waves)
        {
            Assert.GreaterOrEqual(wave.Spawns.Count, 3, "Each wave should receive a full per-wave budget.");
            totalSpawns += wave.Spawns.Count;
        }

        Assert.GreaterOrEqual(totalSpawns, 9);
        Assert.Greater(encounter.ThreatSpent, 90f);
    }

    [Test]
    public void Compose_Combat2_IncludesNonMeleeRolesOften()
    {
        EnemyCatalog catalog = BuildTestCatalog();
        WaveComposerSettings settings = ScriptableObject.CreateInstance<WaveComposerSettings>();
        // Biased weights make non-melee very likely across a few seeds.
        settings.roleWeightsCombat2 = new[] { 5f, 10f, 30f, 30f, 25f };

        bool sawSpecial = false;
        for (int seed = 0; seed < 12 && !sawSpecial; seed++)
        {
            var context = new EncounterContext
            {
                RunSeed = seed,
                GlobalStepIndex = 2,
                BlockIndex = 0,
                CombatIndexInBlock = 2,
            };
            CombatEncounter encounter = EncounterBuilder.Build(context, catalog, settings);
            foreach (ComposedWave wave in encounter.Waves)
            {
                foreach (ComposedSpawnSpec spawn in wave.Spawns)
                {
                    if (catalog.TryGetById(spawn.ArchetypeId, out EnemyArchetype arch)
                        && arch != null
                        && arch.role != EnemyRole.Melee
                        && arch.role != EnemyRole.Ranged)
                    {
                        sawSpecial = true;
                        break;
                    }
                }
            }
        }

        Assert.IsTrue(sawSpecial, "Expected turret/shotgun/beam sniper in combat-2 compositions.");
    }

    private static void AssertEncountersEqual(CombatEncounter a, CombatEncounter b)
    {
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
