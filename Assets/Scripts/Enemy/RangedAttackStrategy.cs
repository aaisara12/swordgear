using UnityEngine;

public class RangedAttackStrategy : MonoBehaviour, IChargingAttackStrategy
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
    private EnemyAttackDamage? attackDamage;

    public bool IsCharging => isAttacking;

    private void Start()
    {
        playerTransform = GameManager.Instance.player.transform;
        // Get reference to EnemyController on the same GameObject
        enemyController = GetComponent<EnemyController>();
        attackDamage = GetComponent<EnemyAttackDamage>();
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
                float charge = chargeUpTime * (attackDamage != null ? attackDamage.ChargeTimeMultiplier : 1f);
                nextAttackTime = Time.time + charge;
            }
        }
    }

    public void Attack(Transform selfTransform, Transform targetTransform)
    {
        if (enemyController == null) return;
        
        // Core logic for the ranged attack
        Vector2 direction = (targetTransform.position - selfTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        GameObject projectile = PrefabPool.Instance!.Spawn(projectilePrefab, selfTransform.position, Quaternion.Euler(0, 0, angle));

        // Initialize the projectile with the enemy's element and damage
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            float finalDamage = damage * (attackDamage != null ? attackDamage.DamageMultiplier : 1f);
            enemyProjectile.Initialize(enemyController.element, finalDamage);
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float speed = projectileSpeed * (attackDamage != null ? attackDamage.ProjectileSpeedMultiplier : 1f);
            rb.linearVelocity = direction * speed;
        }
        // Set the cooldown for the next full attack cycle (movement and attack)
        float rate = attackFrequency * (attackDamage != null ? attackDamage.AttackRateMultiplier : 1f);
        nextAttackTime = Time.time + (1f / Mathf.Max(0.05f, rate));
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }
}