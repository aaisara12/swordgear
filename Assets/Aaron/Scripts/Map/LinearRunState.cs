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

    public bool TryAdvanceToNextStep()
    {
        RunStep? current = CurrentStep;
        if (current != null)
        {
            current.Completed = true;
        }

        if (CurrentStepIndex + 1 >= _steps.Count)
        {
            return false;
        }

        CurrentStepIndex += 1;
        return true;
    }

    /// <summary>Appends another Combat×3 + Upgrade block to the tail of the queue.</summary>
    public void AppendSteps(IReadOnlyList<RunStep> steps)
    {
        if (steps == null || steps.Count == 0)
        {
            return;
        }

        _steps.AddRange(steps);
    }

    /// <summary>How many full blocks are represented in the queue (each block is 3 combats + 1 upgrade).</summary>
    public int QueuedBlockCount => _steps.Count / LinearRunGenerator.StepsPerBlock;
}
