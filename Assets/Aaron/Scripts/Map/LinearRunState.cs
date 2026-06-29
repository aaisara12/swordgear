#nullable enable

using System.Collections.Generic;

/// <summary>Queued steps for the current linear run block and the player's position on the rail.</summary>
public class LinearRunState
{
    private readonly List<RunStep> _steps;

    public int Seed { get; }
    public IReadOnlyList<RunStep> Steps => _steps;
    public int CurrentStepIndex { get; private set; }

    public RunStep? CurrentStep =>
        CurrentStepIndex >= 0 && CurrentStepIndex < _steps.Count
            ? _steps[CurrentStepIndex]
            : null;

    public LinearRunState(int seed, List<RunStep> steps, int currentStepIndex = 0)
    {
        Seed = seed;
        _steps = steps;
        CurrentStepIndex = currentStepIndex;
    }
}
