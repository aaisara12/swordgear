using NUnit.Framework;

public class EncounterContextTest
{
    [Test]
    public void TryFrom_Combat0_Block0_CombatIndex0()
    {
        LinearRunState run = CreateRun(seed: 42);
        Assert.IsTrue(EncounterContext.TryFrom(run, run.Steps[0], out EncounterContext context));
        Assert.AreEqual(42, context.RunSeed);
        Assert.AreEqual(0, context.GlobalStepIndex);
        Assert.AreEqual(0, context.BlockIndex);
        Assert.AreEqual(0, context.CombatIndexInBlock);
    }

    [Test]
    public void TryFrom_Combat2_Block0_CombatIndex2()
    {
        LinearRunState run = CreateRun(seed: 7);
        Assert.IsTrue(EncounterContext.TryFrom(run, run.Steps[2], out EncounterContext context));
        Assert.AreEqual(0, context.BlockIndex);
        Assert.AreEqual(2, context.CombatIndexInBlock);
        Assert.AreEqual(2, context.GlobalStepIndex);
    }

    [Test]
    public void TryFrom_UpgradeStep_ReturnsFalse()
    {
        LinearRunState run = CreateRun(seed: 1);
        Assert.IsFalse(EncounterContext.TryFrom(run, run.Steps[3], out _));
    }

    [Test]
    public void TryFrom_SecondBlockCombat0()
    {
        var layouts = new System.Collections.Generic.List<ArenaLayoutTemplate>
        {
            UnityEngine.ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };

        LinearRunState run = LinearRunGenerator.GenerateInitialBlock(layouts, seed: 9);
        run.AppendSteps(LinearRunGenerator.GenerateNextBlock(layouts, seed: 9, blockIndex: 1, startStepIndex: 4));

        Assert.IsTrue(EncounterContext.TryFrom(run, run.Steps[4], out EncounterContext context));
        Assert.AreEqual(1, context.BlockIndex);
        Assert.AreEqual(0, context.CombatIndexInBlock);
        Assert.AreEqual(4, context.GlobalStepIndex);
    }

    [Test]
    public void CombinedSeed_DiffersByStepIndex()
    {
        LinearRunState run = CreateRun(seed: 100);
        Assert.IsTrue(EncounterContext.TryFrom(run, run.Steps[0], out EncounterContext a));
        Assert.IsTrue(EncounterContext.TryFrom(run, run.Steps[1], out EncounterContext b));
        Assert.AreNotEqual(a.CombinedSeed(), b.CombinedSeed());
    }

    [Test]
    public void DifficultyCurve_Combat2HarderThanCombat1_SameBlock()
    {
        var combat0 = new EncounterContext { RunSeed = 1, GlobalStepIndex = 0, BlockIndex = 0, CombatIndexInBlock = 0 };
        var combat1 = new EncounterContext { RunSeed = 1, GlobalStepIndex = 1, BlockIndex = 0, CombatIndexInBlock = 1 };

        SpawnModifiers easy = DifficultyCurve.Evaluate(combat0);
        SpawnModifiers harder = DifficultyCurve.Evaluate(combat1);

        Assert.Greater(harder.HpMultiplier, easy.HpMultiplier);
        Assert.Greater(harder.DamageMultiplier, easy.DamageMultiplier);
    }

    [Test]
    public void DifficultyCurve_Block1HarderThanBlock0()
    {
        var block0 = new EncounterContext { RunSeed = 1, GlobalStepIndex = 0, BlockIndex = 0, CombatIndexInBlock = 0 };
        var block1 = new EncounterContext { RunSeed = 1, GlobalStepIndex = 4, BlockIndex = 1, CombatIndexInBlock = 0 };

        Assert.Greater(DifficultyCurve.Evaluate(block1).HpMultiplier, DifficultyCurve.Evaluate(block0).HpMultiplier);
    }

    [Test]
    public void ElementStatKnobs_IceTankierThanPhysical()
    {
        ElementStatKnobs physical = ElementStatKnobs.DefaultFor(Element.Physical);
        ElementStatKnobs ice = ElementStatKnobs.DefaultFor(Element.Ice);

        Assert.Greater(ice.hpMultiplier, physical.hpMultiplier);
        Assert.Greater(ice.damageMultiplier, physical.damageMultiplier);
        Assert.Less(ice.speedMultiplier, physical.speedMultiplier);
    }

    [Test]
    public void ElementStatKnobs_LightningFasterWeaker()
    {
        ElementStatKnobs physical = ElementStatKnobs.DefaultFor(Element.Physical);
        ElementStatKnobs lightning = ElementStatKnobs.DefaultFor(Element.Lightning);

        Assert.Greater(lightning.speedMultiplier, physical.speedMultiplier);
        Assert.Greater(lightning.attackRateMultiplier, physical.attackRateMultiplier);
        Assert.Less(lightning.damageMultiplier, physical.damageMultiplier);
        Assert.Less(lightning.chargeTimeMultiplier, physical.chargeTimeMultiplier);
    }

    [Test]
    public void SpawnModifiers_CombineMultiplies()
    {
        var difficulty = new SpawnModifiers
        {
            HpMultiplier = 1.15f,
            DamageMultiplier = 1.05f,
            SpeedMultiplier = 1f,
            AttackRateMultiplier = 1f,
            ChargeTimeMultiplier = 1f,
            ProjectileSpeedMultiplier = 1f,
            ScaleMultiplier = 1f,
        };
        SpawnModifiers element = SpawnModifiers.FromElement(ElementStatKnobs.DefaultFor(Element.Ice));
        SpawnModifiers combined = SpawnModifiers.Combine(difficulty, element);

        Assert.AreEqual(difficulty.HpMultiplier * element.HpMultiplier, combined.HpMultiplier, 0.0001f);
        Assert.AreEqual(difficulty.DamageMultiplier * element.DamageMultiplier, combined.DamageMultiplier, 0.0001f);
    }

    private static LinearRunState CreateRun(int seed)
    {
        var layouts = new System.Collections.Generic.List<ArenaLayoutTemplate>
        {
            UnityEngine.ScriptableObject.CreateInstance<ArenaLayoutTemplate>(),
        };
        return LinearRunGenerator.GenerateInitialBlock(layouts, seed);
    }
}
