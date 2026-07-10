#nullable enable

/// <summary>
/// Interim step-based difficulty scaling until WaveComposerSettings owns the curve (Commit 21).
/// Combat 2 in a block is noticeably tankier than combat 1; later blocks ramp further.
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
