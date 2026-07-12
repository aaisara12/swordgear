#nullable enable

/// <summary>
/// Fallback step-based difficulty when no <see cref="CombatEncounter"/> / <see cref="WaveComposerSettings"/> is available.
/// Prefer modifiers baked on the composed encounter (Commit 21).
/// </summary>
public static class DifficultyCurve
{
    public static SpawnModifiers Evaluate(in EncounterContext context)
    {
        // Block 0 combat 0 = 1.0 HP; each later combat in the block +15% HP; each block +25% HP.
        float hpMult = 1f
            + (context.BlockIndex * 0.25f)
            + (context.CombatIndexInBlock * 0.15f);

        float damageMult = 1f
            + (context.BlockIndex * 0.1f)
            + (context.CombatIndexInBlock * 0.05f);

        return new SpawnModifiers
        {
            HpMultiplier = hpMult,
            DamageMultiplier = damageMult,
            SpeedMultiplier = 1f,
            AttackRateMultiplier = 1f,
            ChargeTimeMultiplier = 1f,
            ProjectileSpeedMultiplier = 1f,
            ScaleMultiplier = 1f,
        };
    }
}
