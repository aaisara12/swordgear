#nullable enable

using NUnit.Framework;

[TestFixture]
public class PlayerStatModifiersStackingTest
{
    [Test]
    public void AddPercentBonus_TwoFiftyPercentBonuses_YieldsOneHundredPercentTotal()
    {
        float multiplier = PlayerStatModifiers.AddPercentBonus(1f, 50f);
        multiplier = PlayerStatModifiers.AddPercentBonus(multiplier, 50f);

        Assert.AreEqual(2.0f, multiplier, 0.001f);
    }

    [Test]
    public void AddPercentBonus_SingleBonus_MatchesAdditive()
    {
        float multiplier = PlayerStatModifiers.AddPercentBonus(1f, 30f);
        Assert.AreEqual(1.3f, multiplier, 0.001f);
    }

    [Test]
    public void AddPercentBonus_Stacks_MultiplyBonusByCount()
    {
        float multiplier = PlayerStatModifiers.AddPercentBonus(1f, 50f, 2);
        Assert.AreEqual(2.0f, multiplier, 0.001f);
    }

    [Test]
    public void AddPercentBonus_PositiveAndNegative_AppliesBoth()
    {
        float multiplier = PlayerStatModifiers.AddPercentBonus(1f, 50f);
        multiplier = PlayerStatModifiers.AddPercentBonus(multiplier, -10f);
        Assert.AreEqual(1.4f, multiplier, 0.001f);
    }
}
