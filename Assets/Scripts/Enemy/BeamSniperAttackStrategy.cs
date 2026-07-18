using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Matches <see cref="RangedAttackStrategy"/> charge cadence; locks aim while telegraphing/firing.</summary>
public class BeamSniperAttackStrategy : MonoBehaviour, IChargingAttackStrategy
{
    [FormerlySerializedAs("projectilePrefab")]
    [SerializeField] private GameObject beamLaserPrefab;
    [SerializeField] private float attackFrequency = 0.3f;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float damage = 14f;
    [SerializeField] private float chargeUpTime = 1.35f;

    private float nextAttackTime;
    private bool isCharging;
    private Transform playerTransform;
    private EnemyController enemyController;
    private EnemyAttackDamage? attackDamage;
    private EnemyBeamLaser? activeBeam;

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
            Debug.LogError($"BeamSniperAttackStrategy on {gameObject.name} requires EnemyController.");
        }
        else
        {
            enemyController.OnDeath += HandleOwnerDeath;
        }

        if (beamLaserPrefab == null)
        {
            Debug.LogError($"BeamSniperAttackStrategy on {gameObject.name} requires beamLaserPrefab.");
        }
    }

    private void Update()
    {
        if (playerTransform == null || enemyController == null || beamLaserPrefab == null)
        {
            return;
        }

        if (isCharging)
        {
            if (Time.time >= nextAttackTime)
            {
                isCharging = false;
                float rate = attackFrequency * (attackDamage != null ? attackDamage.AttackRateMultiplier : 1f);
                nextAttackTime = Time.time + (1f / Mathf.Max(0.05f, rate));
            }

            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        // Beam sits on Default, which unlike Projectiles is stopped by LowWall too.
        bool beamPathClear = EnemyVision.IsClear(transform.position, playerTransform.position, 0f, EnemyVision.MovementMask);
        if (distance <= attackRange && beamPathClear)
        {
            BeginChargedBeam();
        }
    }

    private void BeginChargedBeam()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }

        float beamDuration = 0.55f;
        EnemyBeamLaser? laserTemplate = beamLaserPrefab.GetComponent<EnemyBeamLaser>();
        if (laserTemplate != null)
        {
            beamDuration = laserTemplate.BeamActiveDuration;
        }

        // Same pattern as RangedAttackStrategy: enter charging before the wind-up completes.
        float charge = chargeUpTime * (attackDamage != null ? attackDamage.ChargeTimeMultiplier : 1f);
        isCharging = true;
        nextAttackTime = Time.time + charge + beamDuration;

        float finalDamage = damage * (attackDamage != null ? attackDamage.DamageMultiplier : 1f);
        GameObject laserInstance = PrefabPool.Instance!.Spawn(
            beamLaserPrefab,
            transform.position,
            Quaternion.identity);

        EnemyBeamLaser? beamLaser = laserInstance.GetComponent<EnemyBeamLaser>();
        if (beamLaser != null)
        {
            beamLaser.Begin(
                transform,
                direction,
                enemyController.element,
                finalDamage,
                charge);
            activeBeam = beamLaser;
        }
    }

    private void HandleOwnerDeath()
    {
        // The firing enemy just died — kill any beam it still owns so the laser doesn't linger in the
        // air (or keep dealing damage) after the enemy is gone.
        if (activeBeam != null)
        {
            activeBeam.TerminateIfOwnedBy(transform);
            activeBeam = null;
        }
    }

    private void OnDestroy()
    {
        if (enemyController != null)
        {
            enemyController.OnDeath -= HandleOwnerDeath;
        }
    }

    public void Attack(Transform selfTransform, Transform targetTransform)
    {
    }
}
