using NUnit.Framework;
using System;

[TestFixture]
public class UpgradeTypeSerializerTests
{
    private static UpgradeType AnyUpgrade()
    {
        var values = Enum.GetValues(typeof(UpgradeType));
        Assert.IsTrue(values.Length > 0, "Project must define at least one UpgradeType value for tests.");
        return (UpgradeType)values.GetValue(0)!;
    }

    [Test]
    public void Serialize_Then_TryDeserialize_Roundtrip()
    {
        var upgrade = AnyUpgrade();
        var serialized = UpgradeTypeSerializer.Serialize(upgrade);

        Assert.IsTrue(UpgradeTypeSerializer.TryDeserialize(serialized, out var parsed));
        Assert.AreEqual(upgrade, parsed);
    }

    [Test]
    public void TryDeserialize_IsCaseInsensitive()
    {
        var upgrade = AnyUpgrade();
        var serialized = UpgradeTypeSerializer.Serialize(upgrade);
        var mixedCase = serialized.Replace(upgrade.ToString(), upgrade.ToString().ToLowerInvariant());

        Assert.IsTrue(UpgradeTypeSerializer.TryDeserialize(mixedCase, out var parsed));
        Assert.AreEqual(upgrade, parsed);
    }

    [Test]
    public void TryDeserialize_InvalidInputs_ReturnsFalse()
    {
        Assert.IsFalse(UpgradeTypeSerializer.TryDeserialize(null, out _));
        Assert.IsFalse(UpgradeTypeSerializer.TryDeserialize(string.Empty, out _));
        Assert.IsFalse(UpgradeTypeSerializer.TryDeserialize("   ", out _));
        Assert.IsFalse(UpgradeTypeSerializer.TryDeserialize("elem-upgrade-NON_EXISTENT_VALUE", out _));
        Assert.IsFalse(UpgradeTypeSerializer.TryDeserialize("no-prefix-here", out _));
    }

    [Test]
    public void Deserialize_Valid_ReturnsUpgrade()
    {
        var upgrade = AnyUpgrade();
        var serialized = UpgradeTypeSerializer.Serialize(upgrade);

        var result = UpgradeTypeSerializer.Deserialize(serialized);
        Assert.AreEqual(upgrade, result);
    }

    [Test]
    public void Deserialize_Invalid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UpgradeTypeSerializer.Deserialize(null));
        Assert.Throws<ArgumentException>(() => UpgradeTypeSerializer.Deserialize("invalid-string"));
    }
}
