#nullable enable

using UnityEngine;
using Shop;

[CreateAssetMenu(fileName = "InstantHealStoreItem", menuName = "Scriptable Objects/Instant Heal Augment", order = 201)]
public class InstantHealLoadableStoreItem : LoadableStoreItem
{
    [SerializeField] private float healPercentOfMaxHp = 10f;

    public float HealPercentOfMaxHp => healPercentOfMaxHp;
}
