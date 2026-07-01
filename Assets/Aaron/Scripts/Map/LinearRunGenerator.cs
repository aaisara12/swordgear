#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds the linear Combat×3 → Upgrade step queue.</summary>
public static class LinearRunGenerator
{
    public const int CombatsPerBlock = 3;
    public const int StepsPerBlock = CombatsPerBlock + 1;

    public static LinearRunState GenerateInitialBlock(
        IReadOnlyList<ArenaLayoutTemplate> combatLayouts,
        int seed)
    {
        if (combatLayouts == null || combatLayouts.Count == 0)
        {
            Debug.LogError("LinearRunGenerator: combatLayouts is empty; cannot generate run.");
            return new LinearRunState(seed, new List<RunStep>());
        }

        List<RunStep> steps = GenerateNextBlock(combatLayouts, seed, blockIndex: 0, startStepIndex: 0);
        return new LinearRunState(seed, steps);
    }

    /// <summary>Builds one Combat×3 + Upgrade block with deterministic layout picks per block index.</summary>
    public static List<RunStep> GenerateNextBlock(
        IReadOnlyList<ArenaLayoutTemplate> combatLayouts,
        int seed,
        int blockIndex,
        int startStepIndex)
    {
        var steps = new List<RunStep>(StepsPerBlock);
        if (combatLayouts == null || combatLayouts.Count == 0)
        {
            return steps;
        }

        var rng = new System.Random(CombineSeed(seed, blockIndex));

        for (int i = 0; i < CombatsPerBlock; i++)
        {
            ArenaLayoutTemplate layout = combatLayouts[rng.Next(combatLayouts.Count)];
            steps.Add(new RunStep(RunStepType.Combat, startStepIndex + i, layout));
        }

        steps.Add(new RunStep(RunStepType.Upgrade, startStepIndex + CombatsPerBlock));
        return steps;
    }

    private static int CombineSeed(int seed, int blockIndex)
    {
        unchecked
        {
            return (seed * 397) ^ blockIndex;
        }
    }
}
