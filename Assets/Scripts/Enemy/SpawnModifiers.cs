#nullable enable

using System;
using UnityEngine;

/// <summary>Combined HP / damage / speed / cadence / scale multipliers applied when an enemy spawns.</summary>
[Serializable]
public struct SpawnModifiers
{
    public float HpMultiplier;
    public float DamageMultiplier;
    public float SpeedMultiplier;
    public float AttackRateMultiplier;
    public float ChargeTimeMultiplier;
    public float ProjectileSpeedMultiplier;
    public float ScaleMultiplier;

    public static SpawnModifiers Identity => new SpawnModifiers
    {
        HpMultiplier = 1f,
        DamageMultiplier = 1f,
        SpeedMultiplier = 1f,
        AttackRateMultiplier = 1f,
        ChargeTimeMultiplier = 1f,
        ProjectileSpeedMultiplier = 1f,
        ScaleMultiplier = 1f,
    };

    public static SpawnModifiers FromElement(in ElementStatKnobs knobs)
    {
        return new SpawnModifiers
        {
            HpMultiplier = knobs.hpMultiplier,
            DamageMultiplier = knobs.damageMultiplier,
            SpeedMultiplier = knobs.speedMultiplier,
            AttackRateMultiplier = knobs.attackRateMultiplier,
            ChargeTimeMultiplier = knobs.chargeTimeMultiplier,
            ProjectileSpeedMultiplier = knobs.projectileSpeedMultiplier,
            ScaleMultiplier = 1f,
        };
    }

    public static SpawnModifiers Combine(in SpawnModifiers a, in SpawnModifiers b)
    {
        return new SpawnModifiers
        {
            HpMultiplier = a.HpMultiplier * b.HpMultiplier,
            DamageMultiplier = a.DamageMultiplier * b.DamageMultiplier,
            SpeedMultiplier = a.SpeedMultiplier * b.SpeedMultiplier,
            AttackRateMultiplier = a.AttackRateMultiplier * b.AttackRateMultiplier,
            ChargeTimeMultiplier = a.ChargeTimeMultiplier * b.ChargeTimeMultiplier,
            ProjectileSpeedMultiplier = a.ProjectileSpeedMultiplier * b.ProjectileSpeedMultiplier,
            ScaleMultiplier = a.ScaleMultiplier * b.ScaleMultiplier,
        };
    }
}
