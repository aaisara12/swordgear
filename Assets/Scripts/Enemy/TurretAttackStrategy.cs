using UnityEngine;

/// <summary>Stationary burst fire — a brief wind-up telegraph, then rapid shots, then a reload pause.</summary>
public class TurretAttackStrategy : MonoBehaviour, IChargingAttackStrategy
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackFrequency = 3f;
    [SerializeField] private float attackRange = 11f;
    [SerializeField] private float projectileSpeed = 11f;
    [SerializeField] private float damage = 4f;
    [SerializeField] private int burstSize = 6;
    [SerializeField] private float reloadDuration = 2f;
    [SerializeField] private float chargeUpTime = 0.5f; // wind-up telegraph before each burst

    private float nextAttackTime;
    private int shotsFiredInBurst;
    private bool isReloading;
    private bool isCharging;
    private float chargeEndTime;
    private Transform playerTransform;
    private EnemyController enemyController;
    private EnemyAttackDamage? attackDamage;

    // Charging only ever precedes the FIRST shot of a burst, so the enemy telegraphs (tint/glow) before the
    // magazine opens up, then fires the rest rapidly.
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
            Debug.LogError($"TurretAttackStrategy on {gameObject.name} requires EnemyController.");
        }
    }

    private void Update()
    {
        if (playerTransform == null || enemyController == null || projectilePrefab == null)
        {
            return;
        }

        if (PlayerGameplayManager.Instance?.IsDefeated == true)
        {
            return; // stop firing once the run is over — the scene/pool may be tearing down
        }

        // Wind-up: telegraph the burst, then fire its opening shot the instant the charge completes.
        if (isCharging)
        {
            if (Time.time >= chargeEndTime)
            {
                isCharging = false;
                FireShot();
            }

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

        // Stationary turret: no range gate — it can't chase, so it snipes the player at any distance.
        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (!EnemyVision.CanShoot(transform.position, playerTransform.position))
        {
            return;
        }

        // Start of a fresh burst → play a wind-up telegraph before the first shot (the rest fire rapidly).
        if (shotsFiredInBurst == 0)
        {
            isCharging = true;
            chargeEndTime = Time.time + chargeUpTime * (attackDamage != null ? attackDamage.ChargeTimeMultiplier : 1f);
            return;
        }

        FireShot();
    }

    private void FireShot()
    {
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
        if (enemyController == null || PrefabPool.Instance == null)
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
