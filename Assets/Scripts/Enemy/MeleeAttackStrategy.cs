using UnityEngine;

public class MeleeAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackFrequency = 1f; // Attacks per second

    private float nextAttackTime = 0f;

    // This method is called by Unity when a collider stays in contact with another.
    private void OnCollisionStay2D(Collision2D other)
    {
        Attack(transform, other.transform);
    }

    public void Attack(Transform selfTransform, Transform targetTransform)
    {
        if (Time.time >= nextAttackTime)
        {
            PlayerController player = targetTransform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
                nextAttackTime = Time.time + (1f / attackFrequency);
            }
        }
    }
}