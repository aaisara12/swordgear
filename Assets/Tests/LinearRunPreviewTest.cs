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
}
