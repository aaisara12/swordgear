#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The harp's roll and repertoire as logic: tunes are built here with chosen families and weights, so no
/// test depends on the shipped tune assets or their numbers.
/// </summary>
[TestFixture]
public class HarpRollTest
{
    private readonly List<Object> _created = new();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }

        _created.Clear();
    }

    private HarpTune Tune(string name, HarpTuneFamily family, float weight = 1f)
    {
        var tune = ScriptableObject.CreateInstance<StreakTune>();
        tune.name = name;
        var serialized = new SerializedObject(tune);
        serialized.FindProperty("family").enumValueIndex = (int)family;
        serialized.FindProperty("weight").floatValue = weight;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        _created.Add(tune);
        return tune;
    }

    /// <summary> Rolls the whole [0, 1) range of the family roll in even steps and counts the families picked. </summary>
    private static Dictionary<HarpTuneFamily, int> CountFamilies(IReadOnlyList<HarpTune?> tunes, int steps)
    {
        var counts = new Dictionary<HarpTuneFamily, int>();
        for (int i = 0; i < steps; i++)
        {
            HarpTune? picked = HarpTune.PickByFamily(tunes, (i + 0.5f) / steps, 0.5f);
            Assert.IsNotNull(picked);
            counts[picked!.Family] = counts.TryGetValue(picked.Family, out int c) ? c + 1 : 1;
        }

        return counts;
    }

    [Test]
    public void Families_StayEven_HoweverManyTunesEachHolds()
    {
        // Four offense tunes against one of each other family: a flat pick would be 4/6 offense.
        var tunes = new List<HarpTune?>
        {
            Tune("O1", HarpTuneFamily.Offense), Tune("O2", HarpTuneFamily.Offense),
            Tune("O3", HarpTuneFamily.Offense), Tune("O4", HarpTuneFamily.Offense),
            Tune("S1", HarpTuneFamily.Sustain), Tune("K1", HarpTuneFamily.Streak),
        };

        var counts = CountFamilies(tunes, 300);

        Assert.AreEqual(100, counts[HarpTuneFamily.Offense]);
        Assert.AreEqual(100, counts[HarpTuneFamily.Sustain]);
        Assert.AreEqual(100, counts[HarpTuneFamily.Streak]);
    }

    [Test]
    public void AFamilyWithNoTunes_IsLeftOut_ButTheRestStayEven()
    {
        var tunes = new List<HarpTune?> { Tune("O1", HarpTuneFamily.Offense), Tune("S1", HarpTuneFamily.Sustain) };

        var counts = CountFamilies(tunes, 200);

        Assert.AreEqual(100, counts[HarpTuneFamily.Offense]);
        Assert.AreEqual(100, counts[HarpTuneFamily.Sustain]);
        Assert.IsFalse(counts.ContainsKey(HarpTuneFamily.Streak));
    }

    [Test]
    public void JackpotTunes_AreNeverPartOfTheFamilyRoll()
    {
        var tunes = new List<HarpTune?> { Tune("O1", HarpTuneFamily.Offense), Tune("J", HarpTuneFamily.Jackpot) };

        var counts = CountFamilies(tunes, 50);

        Assert.AreEqual(50, counts[HarpTuneFamily.Offense]);
        Assert.IsFalse(counts.ContainsKey(HarpTuneFamily.Jackpot));
    }

    [Test]
    public void WithinAFamily_WeightsDecide()
    {
        HarpTune common = Tune("Common", HarpTuneFamily.Offense, 3f);
        HarpTune rare = Tune("Rare", HarpTuneFamily.Offense, 1f);
        var tunes = new List<HarpTune?> { common, rare };

        int commonCount = 0;
        const int steps = 400;
        for (int i = 0; i < steps; i++)
        {
            if (HarpTune.PickByFamily(tunes, 0.5f, (i + 0.5f) / steps) == common)
            {
                commonCount++;
            }
        }

        Assert.AreEqual(300, commonCount);
    }

    [Test]
    public void NothingPlayable_PicksNothing()
    {
        Assert.IsNull(HarpTune.PickByFamily(new List<HarpTune?>(), 0.5f, 0.5f));
        Assert.IsNull(HarpTune.PickByFamily(new List<HarpTune?> { Tune("Zero", HarpTuneFamily.Offense, 0f) }, 0.5f, 0.5f));
    }

    [Test]
    public void Repertoire_KnowsStarters_AndLearnedItems_Only()
    {
        HarpTune starter = Tune("Starter", HarpTuneFamily.Offense);
        HarpTune learned = Tune("Learned", HarpTuneFamily.Sustain);
        HarpTune unknown = Tune("Unknown", HarpTuneFamily.Streak);

        var go = new GameObject("Repertoire");
        _created.Add(go);
        var repertoire = go.AddComponent<HarpRepertoire>();
        var serialized = new SerializedObject(repertoire);
        var starters = serialized.FindProperty("starters");
        starters.arraySize = 1;
        starters.GetArrayElementAtIndex(0).objectReferenceValue = starter;
        var learnable = serialized.FindProperty("learnable");
        learnable.arraySize = 2;
        learnable.GetArrayElementAtIndex(0).objectReferenceValue = learned;
        learnable.GetArrayElementAtIndex(1).objectReferenceValue = unknown;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var blob = new PlayerBlob();
        repertoire.InitializeOnGameStart_Dangerous(blob);
        blob.ReceiveItem(HarpRepertoire.LearnedItemId(learned), 1);

        CollectionAssert.AreEquivalent(new[] { starter, learned }, repertoire.Owned);
        Assert.IsTrue(repertoire.HasUnlearned);

        blob.ReceiveItem(HarpRepertoire.LearnedItemId(unknown), 1);
        Assert.IsFalse(repertoire.HasUnlearned);
    }
}
