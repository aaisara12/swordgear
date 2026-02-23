#nullable enable

using System.Collections.Generic;
using UnityEngine;
using Shop;

[CreateAssetMenu(fileName = "StatBoostStoreItem", menuName = "Scriptable Objects/Stat Boost", order = 200)]
public class StatBoostLoadableStoreItem : LoadableStoreItem
{
    [SerializeField] private List<StatBoostEntry> statBoosts = new List<StatBoostEntry>();

    public IReadOnlyList<StatBoostEntry> StatBoosts => statBoosts;
}
