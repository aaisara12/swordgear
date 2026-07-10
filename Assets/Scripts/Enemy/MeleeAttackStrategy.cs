using UnityEngine;
using UnityEngine.Serialization;

public class MeleeAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [FormerlySerializedAs("attackDamage")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float attackFrequency = 1f; // Attacks per second

    private float nextAttackTime = 0f;
    private EnemyController enemyController;
    private EnemyAttackDamage? damageScaler;

    private void Start()
    {
        // Get reference to EnemyController on the same GameObject
        enemyController = GetComponent<EnemyController>();
        damageScaler = GetComponent<EnemyAttackDamage>();
        if (enemyController == null)
        {
            Debug.LogError($"MeleeAttackStrategy on {gameObject.name} requires an EnemyController component!");
        }
    }

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
            if (player != null && enemyController != null && GameManager.Instance != null)
            {
                // Get enemy element from EnemyController (attacker)
                Element attackerElement = enemyController.element;
                
                // Get player weapon element from GameManager (defender)
                Element defenderElement = GameManager.Instance.currentElement;
                
                // Calculate damage using GameManager's CalculateDamage method
                // CalculateDamage(defenderElement, attackerElement, baseDamage)
                // Uses interactionMatrix[attackerElement][defenderElement] to get damage multiplier
                float scaledDamage = baseDamage * (damageScaler != null ? damageScaler.DamageMultiplier : 1f);
                float finalDamage = GameManager.Instance.CalculateDamage(
                    defenderElement,
                    attackerElement,
                    scaledDamage);
                
                // Update next attack time based on attack frequency
                nextAttackTime = Time.time + (1f / attackFrequency);
                
                player.TakeDamage(finalDamage);
            }
        }
    }
}