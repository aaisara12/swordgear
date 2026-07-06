#nullable enable

using NUnit.Framework;

[TestFixture]
public class PlayerStatModifiersStackingTest
{
    [Test]
    public void CombineMultiplicativePercentBonuses_TwoFiftyPercentBonuses_YieldsSeventyFivePercentTotal()
    {
        float inverse = PlayerStatModifiers.ApplyMultiplicativePercentBonus(1f, 50f);
        inverse = PlayerStatModifiers.ApplyMultiplicativePercentBonus(inverse, 50f);

        float multiplier = PlayerStatModifiers.CombineMultiplicativePercentBonuses(inverse);

        Assert.AreEqual(1.75f, multiplier, 0.001f);
    }

    [Test]
    public void CombineMultiplicativePercentBonuses_SingleBonus_MatchesAdditive()
    {
        float inverse = PlayerStatModifiers.ApplyMultiplicativePercentBonus(1f, 30f);
        float multiplier = PlayerStatModifiers.CombineMultiplicativePercentBonuses(inverse);

        Assert.AreEqual(1.3f, multiplier, 0.001f);
    }

    [Test]
    public void CombineMultiplicativePercentBonuses_PositiveAndNegative_AppliesBoth()
    {
        float inverse = PlayerStatModifiers.ApplyMultiplicativePercentBonus(1f, 50f);
        inverse = PlayerStatModifiers.ApplyMultiplicativePercentBonus(inverse, -10f);
        float multiplier = PlayerStatModifiers.CombineMultiplicativePercentBonuses(inverse);

        Assert.AreEqual(1.45f, multiplier, 0.001f);
    }
}
