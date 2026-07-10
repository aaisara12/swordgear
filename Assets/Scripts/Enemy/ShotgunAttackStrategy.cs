using UnityEngine;

public class ShotgunAttackStrategy : MonoBehaviour, IChargingAttackStrategy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackFrequency = 0.35f;
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private float damagePerPellet = 4f;
    [SerializeField] private int pelletCount = 5;
    [SerializeField] private float spreadDegrees = 40f;
    [SerializeField] private float chargeUpTime = 0.65f;

    private float nextAttackTime;
    private bool isCharging;
    private Transform playerTransform;
    private EnemyController enemyController;
    private EnemyAttackDamage? attackDamage;

    public bool IsCharging => isCharging;

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
            Debug.LogError($"ShotgunAttackStrategy on {gameObject.name} requires EnemyController.");
        }
    }

    private void Update()
    {
        if (playerTransform == null || enemyController == null)
        {
            return;
        }

        if (isCharging)
        {
            if (Time.time >= nextAttackTime)
            {
                Attack(transform, playerTransform);
                isCharging = false;
            }

            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= attackRange)
        {
            isCharging = true;
            nextAttackTime = Time.time + chargeUpTime;
        }
    }

    public void Attack(Transform selfTransform, Transform targetTransform)
    {
        if (enemyController == null)
        {
            return;
        }

        Vector2 baseDirection = (targetTransform.position - selfTransform.position).normalized;
        float pelletDamage = damagePerPellet * (attackDamage != null ? attackDamage.DamageMultiplier : 1f);
        int count = Mathf.Max(1, pelletCount);
        float startAngle = -spreadDegrees * 0.5f;
        float step = count > 1 ? spreadDegrees / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angleOffset = startAngle + step * i;
            Vector2 direction = Rotate(baseDirection, angleOffset * Mathf.Deg2Rad);
            SpawnProjectile(selfTransform.position, direction, pelletDamage);
        }

        nextAttackTime = Time.time + (1f / Mathf.Max(0.05f, attackFrequency));
    }

    private void SpawnProjectile(Vector3 position, Vector2 direction, float pelletDamage)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        GameObject projectile = PrefabPool.Instance!.Spawn(
            projectilePrefab,
            position,
            Quaternion.Euler(0f, 0f, angle));

        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null && enemyController != null)
        {
            enemyProjectile.Initialize(enemyController.element, pelletDamage);
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
    }

    private static Vector2 Rotate(Vector2 vector, float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }
}
