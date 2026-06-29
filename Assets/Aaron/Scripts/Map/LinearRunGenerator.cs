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

        var rng = new System.Random(seed);
        var steps = new List<RunStep>(StepsPerBlock);

        for (int i = 0; i < CombatsPerBlock; i++)
        {
            ArenaLayoutTemplate layout = combatLayouts[rng.Next(combatLayouts.Count)];
            steps.Add(new RunStep(RunStepType.Combat, i, layout));
        }

        steps.Add(new RunStep(RunStepType.Upgrade, CombatsPerBlock));
        return new LinearRunState(seed, steps);
    }
}
