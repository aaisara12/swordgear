#nullable enable

using System;
using UnityEngine;

/// <summary>
/// Per-element combat multipliers applied at spawn on top of Physical-baseline prefab stats.
/// Tuned to mirror the legacy Melee/Ranged elemental feel (Ice tanky/slow/hard, Lightning fast/weak, Fire aggressive).
/// </summary>
[Serializable]
public struct ElementStatKnobs
{
    public Element element;
    [Min(0.05f)] public float hpMultiplier;
    [Min(0.05f)] public float damageMultiplier;
    [Min(0.05f)] public float speedMultiplier;
    [Min(0.05f)] public float attackRateMultiplier;
    [Min(0.05f)] public float chargeTimeMultiplier;
    [Min(0.05f)] public float projectileSpeedMultiplier;

    public static ElementStatKnobs Identity(Element element)
    {
        return new ElementStatKnobs
        {
            element = element,
            hpMultiplier = 1f,
            damageMultiplier = 1f,
            speedMultiplier = 1f,
            attackRateMultiplier = 1f,
            chargeTimeMultiplier = 1f,
            projectileSpeedMultiplier = 1f,
        };
    }

    public static ElementStatKnobs DefaultFor(Element element)
    {
        return element switch
        {
            Element.Fire => new ElementStatKnobs
            {
                element = Element.Fire,
                hpMultiplier = 1f,
                damageMultiplier = 1.4f,
                speedMultiplier = 1.5f,
                attackRateMultiplier = 1.15f,
                chargeTimeMultiplier = 1f,
                projectileSpeedMultiplier = 1.5f,
            },
            Element.Ice => new ElementStatKnobs
            {
                element = Element.Ice,
                hpMultiplier = 2.25f,
                damageMultiplier = 2f,
                speedMultiplier = 0.5f,
                attackRateMultiplier = 0.85f,
                chargeTimeMultiplier = 1.1f,
                projectileSpeedMultiplier = 1f,
            },
            Element.Lightning => new ElementStatKnobs
            {
                element = Element.Lightning,
                hpMultiplier = 1f,
                damageMultiplier = 0.5f,
                speedMultiplier = 2.75f,
                attackRateMultiplier = 2f,
                chargeTimeMultiplier = 0.25f,
                projectileSpeedMultiplier = 2f,
            },
            _ => Identity(Element.Physical),
        };
    }
}
