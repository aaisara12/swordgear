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

    public RunStep(RunStepType type, int stepIndex, ArenaLayoutTemplate? layout = null)
    {
        Type = type;
        StepIndex = stepIndex;
        Layout = layout;
    }
}
