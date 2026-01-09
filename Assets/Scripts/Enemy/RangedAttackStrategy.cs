using UnityEngine;

public class RangedAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackFrequency = 1f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float damage = 10f;

    // Time to stand still and "charge up" the shot
    [SerializeField] private float chargeUpTime = 1f;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private Transform playerTransform;
    private EnemyController enemyController;

    private void Start()
    {
        playerTransform = GameManager.Instance.player.transform;
        // Get reference to EnemyController on the same GameObject
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError($"RangedAttackStrategy on {gameObject.name} requires an EnemyController component!");
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // If currently in the "attack" state, we're just waiting for the charge-up time
        if (isAttacking)
        {
            if (Time.time >= nextAttackTime)
            {
                // Charge-up is complete, now fire the projectile
                Attack(transform, playerTransform);
                isAttacking = false;
            }
            return;
        }

        // Check if it's time to start preparing the next attack
        if (Time.time >= nextAttackTime)
        {
            float distanceToTarget = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToTarget <= attackRange)
            {
                // Set the state to "isAttacking" and set the attack time for the charge-up
                isAttacking = true;
                nextAttackTime = Time.time + chargeUpTime;
            }
        }
    }

    public void Attack(Transform selfTransform, Transform targetTransform)
    {
        if (enemyController == null) return;
        
        // Core logic for the ranged attack
        Vector2 direction = (targetTransform.position - selfTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        GameObject projectile = Instantiate(projectilePrefab, selfTransform.position, Quaternion.Euler(0, 0, angle));

        // Initialize the projectile with the enemy's element and damage
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Initialize(enemyController.element, damage);
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
        // Set the cooldown for the next full attack cycle (movement and attack)
        nextAttackTime = Time.time + (1f / attackFrequency);
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }
}