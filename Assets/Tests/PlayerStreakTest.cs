#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using Shop;
using UnityEngine;

[TestFixture]
public class PlayerStreakTest
{
    private GameObject _go = null!;
    private PlayerStatModifiers _modifiers = null!;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PlayerStatModifiers");
        _modifiers = _go.AddComponent<PlayerStatModifiers>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    private static List<StreakBonus> Bonuses(params (StreakStat stat, float percent)[] bonuses)
    {
        var list = new List<StreakBonus>();
        foreach (var (stat, percent) in bonuses)
        {
            list.Add(new StreakBonus { stat = stat, percentPerStack = percent });
        }

        return list;
    }

    [Test]
    public void Stacks_RaiseTheirStatAdditively()
    {
        var damage = Bonuses((StreakStat.Damage, 20f));
        _modifiers.AddStreakStack("Fortissimo", damage, 3, 20f);
        _modifiers.AddStreakStack("Fortissimo", damage, 3, 20f);

        Assert.AreEqual(2, _modifiers.GetStreak("Fortissimo")!.Stacks);
        Assert.AreEqual(1.4f, _modifiers.DamageMultiplier, 0.001f);
    }

    [Test]
    public void Stacks_StopAtMax()
    {
        var damage = Bonuses((StreakStat.Damage, 20f));
        for (int i = 0; i < 5; i++)
        {
            _modifiers.AddStreakStack("Fortissimo", damage, 3, 20f);
        }

        Assert.AreEqual(3, _modifiers.GetStreak("Fortissimo")!.Stacks);
        Assert.AreEqual(1.6f, _modifiers.DamageMultiplier, 0.001f);
    }

    [Test]
    public void OneStreak_CanRaiseSeveralStats_AndLeavesOthersAlone()
    {
        var twoStats = Bonuses((StreakStat.AttackSpeed, 20f), (StreakStat.MoveSpeed, 15f));
        _modifiers.AddStreakStack("TwoStats", twoStats, 3, 20f);
        _modifiers.AddStreakStack("TwoStats", twoStats, 3, 20f);

        Assert.AreEqual(1.4f, _modifiers.AttackSpeedMultiplier, 0.001f);
        Assert.AreEqual(1.3f, _modifiers.MoveSpeedMultiplier, 0.001f);
        Assert.AreEqual(1f, _modifiers.DamageMultiplier, 0.001f);
    }

    [Test]
    public void Streaks_SurviveAnAugmentPickup()
    {
        _modifiers.AddStreakStack("Fortissimo", Bonuses((StreakStat.Damage, 20f)), 3, 20f);

        // Initialising from a blob and then picking up an augment both rebuild the augment values from
        // scratch; neither may touch the streak layered on top.
        var blob = new PlayerBlob();
        _modifiers.InitializeOnGameStart(blob);
        blob.ReceiveItem(StatBoostSerializer.Serialize(StatBoostKind.DamageMultiplier, 10f), 1);

        Assert.IsNotNull(_modifiers.GetStreak("Fortissimo"));
        Assert.AreEqual(1.3f, _modifiers.DamageMultiplier, 0.001f);
    }

    [Test]
    public void ClearStreaks_ReportsTheReason_AndRemovesTheBonus()
    {
        _modifiers.AddStreakStack("Fortissimo", Bonuses((StreakStat.Damage, 20f)), 3, 20f);
        _modifiers.AddStreakStack("Allegro", Bonuses((StreakStat.AttackSpeed, 20f)), 3, 20f);

        var reported = new List<(StreakChange change, string id)>();
        void Record(StreakChange change, string id) => reported.Add((change, id));
        PlayerStatModifiers.OnStreakChanged += Record;
        try
        {
            _modifiers.ClearStreaks(StreakChange.Busted);
        }
        finally
        {
            PlayerStatModifiers.OnStreakChanged -= Record;
        }

        CollectionAssert.AreEquivalent(
            new[] { (StreakChange.Busted, "Fortissimo"), (StreakChange.Busted, "Allegro") },
            reported);
        Assert.AreEqual(0, _modifiers.Streaks.Count);
        Assert.AreEqual(1f, _modifiers.DamageMultiplier, 0.001f);
        Assert.AreEqual(1f, _modifiers.AttackSpeedMultiplier, 0.001f);
    }

    [Test]
    public void Describe_NamesEveryBonusAtTheGivenStacks()
    {
        var twoStats = Bonuses((StreakStat.AttackSpeed, 20f), (StreakStat.MoveSpeed, 15f));

        Assert.AreEqual("+40% ATK SPD  +30% MOVE", StreakText.Describe(twoStats, 2));
        Assert.AreEqual("+20% DMG", StreakText.Describe(Bonuses((StreakStat.Damage, 20f)), 1));
    }
}
