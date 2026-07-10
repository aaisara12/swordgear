#nullable enable

using System;

/// <summary>
/// Deterministic combat identity derived from the linear run rail.
/// Same runSeed + globalStepIndex → same encounter composition (Commit 21) and difficulty (Commit 19).
/// </summary>
[Serializable]
public struct EncounterContext
{
    public int RunSeed;
    public int GlobalStepIndex;
    public int BlockIndex;
    public int CombatIndexInBlock;

    /// <summary>
    /// Builds context for a combat step. Returns false for upgrade steps or out-of-range combat slots.
    /// </summary>
    public static bool TryFrom(LinearRunState run, RunStep step, out EncounterContext context)
    {
        context = default;
        if (step.Type != RunStepType.Combat)
        {
            return false;
        }

        int combatIndexInBlock = step.StepIndex % LinearRunGenerator.StepsPerBlock;
        if (combatIndexInBlock < 0 || combatIndexInBlock >= LinearRunGenerator.CombatsPerBlock)
        {
            return false;
        }

        context = new EncounterContext
        {
            RunSeed = run.Seed,
            GlobalStepIndex = step.StepIndex,
            BlockIndex = step.StepIndex / LinearRunGenerator.StepsPerBlock,
            CombatIndexInBlock = combatIndexInBlock,
        };
        return true;
    }

    /// <summary>Convenience for the run's current step when it is a combat.</summary>
    public static bool TryFromCurrent(LinearRunState? run, out EncounterContext context)
    {
        context = default;
        if (run?.CurrentStep == null)
        {
            return false;
        }

        return TryFrom(run, run.CurrentStep, out context);
    }

    /// <summary>Combines run seed with step index the same way <see cref="LinearRunGenerator"/> mixes block seeds.</summary>
    public int CombinedSeed()
    {
        unchecked
        {
            return (RunSeed * 397) ^ GlobalStepIndex;
        }
    }
}
