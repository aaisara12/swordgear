#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spends a per-wave threat budget using catalog costs, theme, and role weights.
/// Pure logic — no Unity scene dependencies. Deterministic for a given seed + inputs.
/// </summary>
public static class WaveComposer
{
    // Driven off the enum sizes so adding the next element (Element + EncounterTheme entry + catalog rows) needs
    // no edits here — the composer will roll and theme it automatically.
    private static readonly int ElementCount = System.Enum.GetValues(typeof(Element)).Length;
    private static readonly int ThemeCount = System.Enum.GetValues(typeof(EncounterTheme)).Length;

    public static CombatEncounter Compose(
        in EncounterContext context,
        EnemyCatalog catalog,
        WaveComposerSettings settings)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var rng = new System.Random(context.CombinedSeed());
        WaveComposerSettings.CombatProfile profile = settings.ResolveProfile(context.BlockIndex, context.CombatIndexInBlock);
        EncounterTheme theme = PickTheme(rng, settings.GetThemeWeights(context.CombatIndexInBlock));
        int waveCount = Mathf.Clamp(
            rng.Next(profile.minWaves, profile.maxWaves + 1),
            1,
            8);

        float[] waveBudgets = AllocatePerWaveBudgets(profile.threatBudget, waveCount);
        float[] roleWeights = settings.GetRoleWeights(context.CombatIndexInBlock);

        var waves = new List<ComposedWave>(waveCount);
        float spent = 0f;
        bool elitePlaced = false;

        for (int w = 0; w < waveCount; w++)
        {
            var spawns = new List<ComposedSpawnSpec>();
            float remaining = waveBudgets[w];
            int safety = 0;

            while (spawns.Count < settings.minEnemiesPerWave
                   || (remaining >= MinAffordableCost(catalog) && spawns.Count < settings.maxEnemiesPerWave))
            {
                if (++safety > 64)
                {
                    break;
                }

                EnemyRole role = PickWeightedRole(rng, roleWeights);
                Element element = PickElement(rng, theme, settings.themedElementBias);
                EnemyArchetype? archetype = PickArchetype(rng, catalog, role, element);
                if (archetype == null)
                {
                    // Soften filters if catalog gaps exist.
                    archetype = PickAnyArchetype(rng, catalog);
                    if (archetype == null)
                    {
                        break;
                    }
                }

                bool makeElite = false;
                if (profile.guaranteeElite && !elitePlaced && w == waveCount - 1 && spawns.Count == 0)
                {
                    makeElite = true;
                    elitePlaced = true;
                }

                if (spawns.Count >= settings.minEnemiesPerWave
                    && remaining < archetype.baseThreatCost
                    && remaining < MinAffordableCost(catalog))
                {
                    break;
                }

                // Allow slightly overspending so waves aren't empty when budget is awkward.
                if (spawns.Count >= settings.minEnemiesPerWave && remaining <= 0f)
                {
                    break;
                }

                spawns.Add(new ComposedSpawnSpec(archetype.id, makeElite));
                remaining -= archetype.baseThreatCost;
                spent += archetype.baseThreatCost;
            }

            if (spawns.Count == 0)
            {
                EnemyArchetype? fallback = PickAnyArchetype(rng, catalog);
                if (fallback != null)
                {
                    bool makeElite = profile.guaranteeElite && !elitePlaced && w == waveCount - 1;
                    if (makeElite)
                    {
                        elitePlaced = true;
                    }

                    spawns.Add(new ComposedSpawnSpec(fallback.id, makeElite));
                    spent += fallback.baseThreatCost;
                }
            }

            waves.Add(new ComposedWave
            {
                Spawns = spawns,
                DelayAfterClear = settings.delayAfterClear,
            });
        }

        if (profile.guaranteeElite && !elitePlaced && waves.Count > 0 && waves[^1].Spawns.Count > 0)
        {
            ComposedSpawnSpec first = waves[^1].Spawns[0];
            waves[^1].Spawns[0] = new ComposedSpawnSpec(first.ArchetypeId, isElite: true);
            elitePlaced = true;
        }

        CombatEncounter encounter = new CombatEncounter
        {
            Theme = theme,
            ThemeDisplayName = FormatThemeName(theme),
            Waves = waves,
            ThreatBudget = profile.threatBudget,
            ThreatSpent = spent,
            DifficultyModifiers = settings.ToDifficultyModifiers(profile),
        };

        Debug.Log(
            $"[WaveComposer] seed={context.CombinedSeed()} block={context.BlockIndex} combat={context.CombatIndexInBlock} " +
            $"theme={encounter.ThemeDisplayName} waves={waveCount} budgetPerWave={profile.threatBudget:0.#} spent={spent:0.#} " +
            $"elite={(elitePlaced ? "yes" : "no")} roles={SummarizeRoles(encounter, catalog)}");

        return encounter;
    }

    public static string FormatThemeName(EncounterTheme theme) =>
        theme switch
        {
            EncounterTheme.Physical => "Physical Assault",
            EncounterTheme.Fire => "Fire Assault",
            EncounterTheme.Ice => "Ice Assault",
            EncounterTheme.Lightning => "Lightning Assault",
            EncounterTheme.Wind => "Wind Assault",
            _ => "Mixed Assault",
        };

    /// <summary>Each wave gets the full per-wave threat budget (not a split of a fight total).</summary>
    private static float[] AllocatePerWaveBudgets(float budgetPerWave, int waveCount)
    {
        var budgets = new float[waveCount];
        for (int i = 0; i < waveCount; i++)
        {
            budgets[i] = budgetPerWave;
        }

        return budgets;
    }

    private static EncounterTheme PickTheme(System.Random rng, float[] weights)
    {
        int index = PickWeightedIndex(rng, weights, ThemeCount);
        return (EncounterTheme)Mathf.Clamp(index, 0, ThemeCount - 1);
    }

    private static EnemyRole PickWeightedRole(System.Random rng, float[] weights)
    {
        int index = PickWeightedIndex(rng, weights, (int)EnemyRole.Turret + 1);
        return (EnemyRole)Mathf.Clamp(index, 0, (int)EnemyRole.Turret);
    }

    private static Element PickElement(System.Random rng, EncounterTheme theme, float themedBias)
    {
        if (theme == EncounterTheme.Mixed)
        {
            return (Element)rng.Next(0, ElementCount);
        }

        Element themed = theme switch
        {
            EncounterTheme.Physical => Element.Physical,
            EncounterTheme.Fire => Element.Fire,
            EncounterTheme.Ice => Element.Ice,
            EncounterTheme.Lightning => Element.Lightning,
            EncounterTheme.Wind => Element.Wind,
            _ => Element.Physical,
        };

        if (rng.NextDouble() < themedBias)
        {
            return themed;
        }

        return (Element)rng.Next(0, ElementCount);
    }

    private static EnemyArchetype? PickArchetype(
        System.Random rng,
        EnemyCatalog catalog,
        EnemyRole role,
        Element element)
    {
        var matches = new List<EnemyArchetype>();
        IReadOnlyList<EnemyArchetype> all = catalog.Archetypes;
        for (int i = 0; i < all.Count; i++)
        {
            EnemyArchetype a = all[i];
            if (a != null && a.role == role && a.element == element && !string.IsNullOrEmpty(a.id))
            {
                matches.Add(a);
            }
        }

        if (matches.Count == 0)
        {
            // Prefer role match over element when exact pair missing.
            for (int i = 0; i < all.Count; i++)
            {
                EnemyArchetype a = all[i];
                if (a != null && a.role == role && !string.IsNullOrEmpty(a.id))
                {
                    matches.Add(a);
                }
            }
        }

        if (matches.Count == 0)
        {
            return null;
        }

        return matches[rng.Next(matches.Count)];
    }

    private static EnemyArchetype? PickAnyArchetype(System.Random rng, EnemyCatalog catalog)
    {
        IReadOnlyList<EnemyArchetype> all = catalog.Archetypes;
        var valid = new List<EnemyArchetype>();
        for (int i = 0; i < all.Count; i++)
        {
            EnemyArchetype a = all[i];
            if (a != null && !string.IsNullOrEmpty(a.id))
            {
                valid.Add(a);
            }
        }

        if (valid.Count == 0)
        {
            return null;
        }

        return valid[rng.Next(valid.Count)];
    }

    private static float MinAffordableCost(EnemyCatalog catalog)
    {
        float min = float.MaxValue;
        IReadOnlyList<EnemyArchetype> all = catalog.Archetypes;
        for (int i = 0; i < all.Count; i++)
        {
            EnemyArchetype a = all[i];
            if (a != null && a.baseThreatCost < min)
            {
                min = a.baseThreatCost;
            }
        }

        return min < float.MaxValue ? min : 10f;
    }

    private static int PickWeightedIndex(System.Random rng, float[]? weights, int fallbackCount)
    {
        if (weights == null || weights.Length == 0)
        {
            return rng.Next(0, Mathf.Max(1, fallbackCount));
        }

        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            total += Mathf.Max(0f, weights[i]);
        }

        if (total <= 0f)
        {
            return rng.Next(0, weights.Length);
        }

        float roll = (float)(rng.NextDouble() * total);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += Mathf.Max(0f, weights[i]);
            if (roll <= cumulative)
            {
                return i;
            }
        }

        return weights.Length - 1;
    }

    private static string SummarizeRoles(CombatEncounter encounter, EnemyCatalog catalog)
    {
        var counts = new Dictionary<EnemyRole, int>();
        foreach (ComposedWave wave in encounter.Waves)
        {
            foreach (ComposedSpawnSpec spawn in wave.Spawns)
            {
                if (!catalog.TryGetById(spawn.ArchetypeId, out EnemyArchetype? arch) || arch == null)
                {
                    continue;
                }

                counts.TryGetValue(arch.role, out int c);
                counts[arch.role] = c + 1;
            }
        }

        var parts = new List<string>();
        foreach (KeyValuePair<EnemyRole, int> kvp in counts)
        {
            parts.Add($"{kvp.Key}×{kvp.Value}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "none";
    }
}
