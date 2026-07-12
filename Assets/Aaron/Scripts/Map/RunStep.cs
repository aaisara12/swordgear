#nullable enable

using System;
using UnityEngine;

/// <summary>One step on the linear run rail (combat arena or upgrade hub).</summary>
[Serializable]
public class RunStep
{
    public RunStepType Type;
    public int StepIndex;
    public ArenaLayoutTemplate? Layout;
    public bool Completed;

    /// <summary>
    /// Pre-rolled encounter for a Combat step (Commit 22). Composed when the block is queued so the
    /// upgrade-hub preview can read the next combats before they load, and so the fight reuses the exact
    /// composition without re-rolling. Null for Upgrade steps and until pre-rolled.
    /// </summary>
    public CombatEncounter? Encounter;

    public RunStep(RunStepType type, int stepIndex, ArenaLayoutTemplate? layout = null)
    {
        Type = type;
        StepIndex = stepIndex;
        Layout = layout;
    }
}
