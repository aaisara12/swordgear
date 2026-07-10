using UnityEngine;

/// <summary>Stationary burst fire — rapid shots, then a reload pause before the next magazine.</summary>
public class TurretAttackStrategy : MonoBehaviour, IAttackStrategy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackFrequency = 3f;
    [SerializeField] private float attackRange = 11f;
    [SerializeField] private float projectileSpeed = 11f;
    [SerializeField] private float damage = 4f;
    [SerializeField] private int burstSize = 6;
    [SerializeField] private float reloadDuration = 2f;

    private float nextAttackTime;
    private int shotsFiredInBurst;
    private bool isReloading;
    private Transform playerTransform;
    private EnemyController enemyController;
    private EnemyAttackDamage? attackDamage;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        playerTransform = GameManager.Instance.player.transform;
        enemyController = GetComponent<EnemyController>();
        attackDamage = GetComponent<EnemyAttackDamage>();
        if (enemyController == null)
        {
            Debug.LogError($"TurretAttackStrategy on {gameObject.name} requires EnemyController.");
        }
    }

    private void Update()
    {
        if (playerTransform == null || enemyController == null || projectilePrefab == null)
        {
            return;
        }

        if (isReloading)
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            isReloading = false;
            shotsFiredInBurst = 0;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance > attackRange || Time.time < nextAttackTime)
        {
            return;
        }

        Attack(transform, playerTransform);
        shotsFiredInBurst++;

        if (shotsFiredInBurst >= burstSize)
        {
            isReloading = true;
            shotsFiredInBurst = 0;
            nextAttackTime = Time.time + reloadDuration;
        }
        else
        {
            float rate = attackFrequency * (attackDamage != null ? attackDamage.AttackRateMultiplier : 1f);
            nextAttackTime = Time.time + (1f / Mathf.Max(0.05f, rate));
        }
    }

    public void Attack(Transform selfTransform, Transform targetTransform)
    {
        if (enemyController == null)
        {
            return;
        }

        Vector2 direction = (targetTransform.position - selfTransform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        GameObject projectile = PrefabPool.Instance!.Spawn(
            projectilePrefab,
            selfTransform.position,
            Quaternion.Euler(0f, 0f, angle));

        float finalDamage = damage * (attackDamage != null ? attackDamage.DamageMultiplier : 1f);
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Initialize(enemyController.element, finalDamage);
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float speed = projectileSpeed * (attackDamage != null ? attackDamage.ProjectileSpeedMultiplier : 1f);
            rb.linearVelocity = direction * speed;
        }
    }
}
