#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using Shop;
using UnityEditor;
using UnityEngine;

[TestFixture]
public class ItemCatalogExtensionsTest
{
    [Test]
    public void GetRandomItemsForExactTier_OnlyReturnsMatchingTier()
    {
        var catalog = ScriptableObject.CreateInstance<LoadableStoreItemCatalog>();
        var bronze = CreateStatBoost("bronze", AugmentQualityTier.Low);
        var silver = CreateStatBoost("silver", AugmentQualityTier.Medium);
        var gold = CreateStatBoost("gold", AugmentQualityTier.High);

        SetCatalogItems(catalog, bronze, silver, gold);

        List<IStoreItem> offer = catalog.GetRandomItemsForExactTier(3, AugmentQualityTier.Low);

        Assert.AreEqual(3, offer.Count);
        foreach (IStoreItem item in offer)
        {
            Assert.AreEqual(AugmentQualityTier.Low, ((LoadableStoreItem)item).QualityTier);
        }

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(bronze);
        Object.DestroyImmediate(silver);
        Object.DestroyImmediate(gold);
    }

    [Test]
    public void GetRandomItemsForExactTier_AllowsDuplicatesWhenPoolIsSmall()
    {
        var catalog = ScriptableObject.CreateInstance<LoadableStoreItemCatalog>();
        var onlyBronze = CreateStatBoost("only", AugmentQualityTier.Low);
        SetCatalogItems(catalog, onlyBronze);

        List<IStoreItem> offer = catalog.GetRandomItemsForExactTier(3, AugmentQualityTier.Low);

        Assert.AreEqual(3, offer.Count);
        Assert.AreEqual(onlyBronze, offer[0]);
        Assert.AreEqual(onlyBronze, offer[1]);
        Assert.AreEqual(onlyBronze, offer[2]);

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(onlyBronze);
    }

    private static StatBoostLoadableStoreItem CreateStatBoost(string name, AugmentQualityTier tier)
    {
        var asset = ScriptableObject.CreateInstance<StatBoostLoadableStoreItem>();
        asset.name = name;
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("id").stringValue = $"stat-MoveSpeed-1-{name}";
        serialized.FindProperty("displayName").stringValue = name;
        serialized.FindProperty("qualityTier").enumValueIndex = (int)tier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    private static void SetCatalogItems(LoadableStoreItemCatalog catalog, params LoadableStoreItem[] items)
    {
        var serialized = new SerializedObject(catalog);
        SerializedProperty list = serialized.FindProperty("_loadedItems");
        list.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
