using UnityEngine;

/// <summary>
/// Optional damage multiplier applied at spawn (difficulty / elite scaling in later commits).
/// </summary>
public class EnemyAttackDamage : MonoBehaviour
{
    [SerializeField] private float damageMultiplier = 1f;

    public float DamageMultiplier => damageMultiplier;

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }
}
