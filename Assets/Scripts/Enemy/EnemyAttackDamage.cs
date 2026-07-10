using UnityEngine;

/// <summary>
/// Combat multipliers applied at spawn (difficulty curve, elemental knobs, later elites).
/// Attack strategies multiply their authored base values by these fields.
/// </summary>
public class EnemyAttackDamage : MonoBehaviour
{
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float attackRateMultiplier = 1f;
    [SerializeField] private float chargeTimeMultiplier = 1f;
    [SerializeField] private float projectileSpeedMultiplier = 1f;

    public float DamageMultiplier => damageMultiplier;
    public float AttackRateMultiplier => attackRateMultiplier;
    public float ChargeTimeMultiplier => chargeTimeMultiplier;
    public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ApplyCombatMultipliers(in SpawnModifiers modifiers)
    {
        damageMultiplier = Mathf.Max(0f, modifiers.DamageMultiplier);
        attackRateMultiplier = Mathf.Max(0.05f, modifiers.AttackRateMultiplier);
        chargeTimeMultiplier = Mathf.Max(0.05f, modifiers.ChargeTimeMultiplier);
        projectileSpeedMultiplier = Mathf.Max(0.05f, modifiers.ProjectileSpeedMultiplier);
    }
}
