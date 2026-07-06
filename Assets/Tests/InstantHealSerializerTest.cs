#nullable enable

using NUnit.Framework;

[TestFixture]
public class InstantHealSerializerTest
{
    [Test]
    public void Serialize_RoundTripsPercent()
    {
        string id = InstantHealSerializer.Serialize(30f);
        Assert.IsTrue(InstantHealSerializer.TryDeserialize(id, out float percent));
        Assert.AreEqual(30f, percent, 0.001f);
    }

    [Test]
    public void TryDeserialize_RejectsStatBoostIds()
    {
        Assert.IsFalse(InstantHealSerializer.TryDeserialize("stat-MaxHp-10", out _));
    }
}
