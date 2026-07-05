using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LinearRunPreviewTest
{
    [Test]
    public void GenerateInitialBlock_ContainsThreeCombatsThenUpgrade()
    {
        var layouts = new List<ArenaLayoutTemplate>
        {
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        LinearRunState run = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 42);

        Assert.AreEqual(4, run.Steps.Count);
        Assert.AreEqual(RunStepType.Combat, run.Steps[0].Type);
        Assert.AreEqual(RunStepType.Combat, run.Steps[1].Type);
        Assert.AreEqual(RunStepType.Combat, run.Steps[2].Type);
        Assert.AreEqual(RunStepType.Upgrade, run.Steps[3].Type);
        Assert.AreEqual(0, run.CurrentStepIndex);
        Assert.IsNotNull(run.Steps[0].Layout);
    }

    [Test]
    public void GenerateInitialBlock_SameSeed_PicksSameLayouts()
    {
        var layouts = new List<ArenaLayoutTemplate>
        {
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        LinearRunState first = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 99);
        LinearRunState second = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 99);

        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(first.Steps[i].Layout, second.Steps[i].Layout);
        }
    }

    [Test]
    public void GenerateNextBlock_StartsAtRequestedStepIndex()
    {
        var layouts = new List<ArenaLayoutTemplate>
        {
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        List<RunStep> block = LinearRunGenerator.GenerateNextBlock(layouts, seed: 7, blockIndex: 1, startStepIndex: 4);

        Assert.AreEqual(4, block.Count);
        Assert.AreEqual(4, block[0].StepIndex);
        Assert.AreEqual(RunStepType.Combat, block[0].Type);
        Assert.AreEqual(6, block[2].StepIndex);
        Assert.AreEqual(RunStepType.Upgrade, block[3].Type);
        Assert.AreEqual(7, block[3].StepIndex);
    }

    [Test]
    public void AppendSteps_ExtendsQueueForSecondBlock()
    {
        var layouts = new List<ArenaLayoutTemplate>
        {
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        LinearRunState run = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 42);
        List<RunStep> nextBlock = LinearRunGenerator.GenerateNextBlock(layouts, seed: 42, blockIndex: 1, startStepIndex: 4);
        run.AppendSteps(nextBlock);

        Assert.AreEqual(8, run.Steps.Count);
        Assert.AreEqual(2, run.QueuedBlockCount);
        Assert.AreEqual(RunStepType.Upgrade, run.Steps[3].Type);
        Assert.AreEqual(RunStepType.Combat, run.Steps[4].Type);
        Assert.AreEqual(RunStepType.Upgrade, run.Steps[7].Type);
    }

    [Test]
    public void TryAdvanceToNextStep_ReachesUpgradeAfterThreeCombats()
    {
        var layouts = new List<ArenaLayoutTemplate>
        {
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        LinearRunState run = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 1);

        Assert.IsTrue(run.TryAdvanceToNextStep());
        Assert.AreEqual(1, run.CurrentStepIndex);
        Assert.IsTrue(run.TryAdvanceToNextStep());
        Assert.AreEqual(2, run.CurrentStepIndex);
        Assert.IsTrue(run.TryAdvanceToNextStep());
        Assert.AreEqual(3, run.CurrentStepIndex);
        Assert.AreEqual(RunStepType.Upgrade, run.CurrentStep!.Type);
        Assert.IsFalse(run.TryAdvanceToNextStep());
    }

    [Test]
    public void TryAdvancePastUpgrade_ReachesNextCombatBlock()
    {
        var layouts = new List<ArenaLayoutTemplate>
        {
            ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        LinearRunState run = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 1);
        List<RunStep> nextBlock = LinearRunGenerator.GenerateNextBlock(layouts, seed: 1, blockIndex: 1, startStepIndex: 4);
        run.AppendSteps(nextBlock);

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(run.TryAdvanceToNextStep());
        }

        Assert.AreEqual(RunStepType.Upgrade, run.CurrentStep!.Type);
        Assert.IsTrue(run.TryAdvanceToNextStep());
        Assert.AreEqual(4, run.CurrentStepIndex);
        Assert.AreEqual(RunStepType.Combat, run.CurrentStep!.Type);
    }
}
