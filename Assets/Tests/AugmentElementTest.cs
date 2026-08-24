using System.Collections.Generic;
using NUnit.Framework;
using Shop;

[TestFixture]
public class AugmentElementSerializerTests
{
    [Test]
    public void Serialize_Then_TryDeserialize_Roundtrip()
    {
        foreach (Element element in AugmentElementSerializer.RollableElements)
        {
            string tagged = AugmentElementSerializer.Serialize(element, "stat-MoveSpeed-5");

            Assert.IsTrue(AugmentElementSerializer.TryDeserialize(tagged, out Element parsed));
            Assert.AreEqual(element, parsed);
            Assert.AreEqual("stat-MoveSpeed-5", AugmentElementSerializer.StripTag(tagged));
        }
    }

    [Test]
    public void RollableElements_ExcludePhysical()
    {
        CollectionAssert.DoesNotContain(AugmentElementSerializer.RollableElements, Element.Physical);
    }

    [Test]
    public void Serialize_IsIdempotent_WhenAlreadyTagged()
    {
        string once = AugmentElementSerializer.Serialize(Element.Fire, "stat-MoveSpeed-5");
        string twice = AugmentElementSerializer.Serialize(Element.Ice, once);

        Assert.IsTrue(AugmentElementSerializer.TryDeserialize(twice, out Element parsed));
        Assert.AreEqual(Element.Ice, parsed);
        Assert.AreEqual("stat-MoveSpeed-5", AugmentElementSerializer.StripTag(twice));
    }

    // The tag is a prefix precisely so the other ID serializers keep working on tagged IDs.
    [Test]
    public void TaggedIds_StillParse_WithTheirOwnSerializers()
    {
        string statId = AugmentElementSerializer.Serialize(Element.Wind, StatBoostSerializer.Serialize(StatBoostKind.MaxHp, 10f));
        Assert.IsTrue(StatBoostSerializer.TryDeserializeEntries(statId, out List<StatBoostEntry> entries));
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(StatBoostKind.MaxHp, entries[0].kind);
        Assert.AreEqual(10f, entries[0].value);

        string upgradeId = AugmentElementSerializer.Serialize(Element.Fire, UpgradeTypeSerializer.Serialize(UpgradeType.Fire_ChargeMelee));
        Assert.IsTrue(UpgradeTypeSerializer.TryDeserialize(upgradeId, out UpgradeType upgrade));
        Assert.AreEqual(UpgradeType.Fire_ChargeMelee, upgrade);

        string healId = AugmentElementSerializer.Serialize(Element.Ice, InstantHealSerializer.Serialize(25f));
        Assert.IsTrue(InstantHealSerializer.TryDeserialize(healId, out float percent));
        Assert.AreEqual(25f, percent);
    }

    [Test]
    public void TryDeserialize_UntaggedInputs_ReturnFalse()
    {
        Assert.IsFalse(AugmentElementSerializer.TryDeserialize(null, out _));
        Assert.IsFalse(AugmentElementSerializer.TryDeserialize(string.Empty, out _));
        Assert.IsFalse(AugmentElementSerializer.TryDeserialize("stat-MoveSpeed-5", out _));
        Assert.IsFalse(AugmentElementSerializer.TryDeserialize("@|stat-MoveSpeed-5", out _));
        Assert.IsFalse(AugmentElementSerializer.TryDeserialize("@Nonsense|stat-MoveSpeed-5", out _));
    }

    [Test]
    public void StripTag_LeavesUntaggedIdsAlone()
    {
        Assert.AreEqual("stat-MoveSpeed-5", AugmentElementSerializer.StripTag("stat-MoveSpeed-5"));
        Assert.AreEqual(string.Empty, AugmentElementSerializer.StripTag(string.Empty));
    }
}

[TestFixture]
public class UpgradeTypeElementTests
{
    [Test]
    public void TryGetElement_ReadsElementFromUpgradeName()
    {
        Assert.IsTrue(UpgradeTypeSerializer.TryGetElement(UpgradeType.Fire_ChargeMelee, out Element fire));
        Assert.AreEqual(Element.Fire, fire);

        Assert.IsTrue(UpgradeTypeSerializer.TryGetElement(UpgradeType.Wind_Windstorm, out Element wind));
        Assert.AreEqual(Element.Wind, wind);

        Assert.IsTrue(UpgradeTypeSerializer.TryGetElement(UpgradeType.Lightning_Thunderstep, out Element lightning));
        Assert.AreEqual(Element.Lightning, lightning);
    }

    [Test]
    public void TryGetElement_ReturnsFalse_ForNonelementalUpgrades()
    {
        Assert.IsFalse(UpgradeTypeSerializer.TryGetElement(UpgradeType.Nonelemental_Attunement, out _));
        Assert.IsFalse(UpgradeTypeSerializer.TryGetElement(UpgradeType.Nonelemental_DemoUpgrade, out _));
    }
}

[TestFixture]
public class AugmentElementLedgerTests
{
    private PlayerBlob _blob;

    [SetUp]
    public void SetUp()
    {
        _blob = new PlayerBlob();
        AugmentElementLedger.Bind(_blob);
    }

    private static string StatAugment(Element element, StatBoostKind kind, float value) =>
        AugmentElementSerializer.Serialize(element, StatBoostSerializer.Serialize(kind, value));

    [Test]
    public void Counts_AreEmpty_ForAFreshBlob()
    {
        Assert.AreEqual(0, AugmentElementLedger.GetCount(Element.Fire));
        Assert.AreEqual(0, AugmentElementLedger.Counts.Count);
    }

    [Test]
    public void Counts_TrackTaggedStatAugments_PerElement()
    {
        _blob.ReceiveItem(StatAugment(Element.Fire, StatBoostKind.MaxHp, 10f), 1);
        _blob.ReceiveItem(StatAugment(Element.Fire, StatBoostKind.MoveSpeed, 5f), 1);
        _blob.ReceiveItem(StatAugment(Element.Ice, StatBoostKind.MaxHp, 10f), 1);

        Assert.AreEqual(2, AugmentElementLedger.GetCount(Element.Fire));
        Assert.AreEqual(1, AugmentElementLedger.GetCount(Element.Ice));
        Assert.AreEqual(0, AugmentElementLedger.GetCount(Element.Wind));
    }

    // The same stat buff rolled as two elements must stay two entries, one per element.
    [Test]
    public void SameStatBuff_RolledAsTwoElements_CountsForBoth()
    {
        _blob.ReceiveItem(StatAugment(Element.Fire, StatBoostKind.MaxHp, 10f), 1);
        _blob.ReceiveItem(StatAugment(Element.Wind, StatBoostKind.MaxHp, 10f), 1);

        Assert.AreEqual(1, AugmentElementLedger.GetCount(Element.Fire));
        Assert.AreEqual(1, AugmentElementLedger.GetCount(Element.Wind));
    }

    [Test]
    public void Quantity_CountsEveryStack()
    {
        _blob.ReceiveItem(StatAugment(Element.Lightning, StatBoostKind.MaxHp, 10f), 3);

        Assert.AreEqual(3, AugmentElementLedger.GetCount(Element.Lightning));
    }

    [Test]
    public void UntaggedElementUpgrades_FallBackToTheirOwnElement()
    {
        _blob.ReceiveItem(UpgradeTypeSerializer.Serialize(UpgradeType.Ice_EmpowerMelee), 1);

        Assert.AreEqual(1, AugmentElementLedger.GetCount(Element.Ice));
    }

    [Test]
    public void Consumables_AreNotCounted()
    {
        _blob.ReceiveItem(AugmentElementSerializer.Serialize(Element.Fire, InstantHealSerializer.Serialize(25f)), 1);
        _blob.ReceiveItem("some-unrelated-item", 1);

        Assert.AreEqual(0, AugmentElementLedger.GetCount(Element.Fire));
        Assert.AreEqual(0, AugmentElementLedger.Counts.Count);
    }

    [Test]
    public void ClearingInventory_EmptiesTheLedger()
    {
        _blob.ReceiveItem(StatAugment(Element.Fire, StatBoostKind.MaxHp, 10f), 2);
        Assert.AreEqual(2, AugmentElementLedger.GetCount(Element.Fire));

        _blob.ClearInventory();

        Assert.AreEqual(0, AugmentElementLedger.GetCount(Element.Fire));
    }
}
