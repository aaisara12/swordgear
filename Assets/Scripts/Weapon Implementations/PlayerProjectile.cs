#nullable enable

using UnityEngine;

/// <summary>
/// Pooled player-owned projectile. Shared by Fire's homing fireball bursts and Wind's reflected darts —
/// the prefab only needs a trigger Collider2D + Rigidbody2D; steering and lifetime are driven here.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerProjectile : MonoBehaviour, IPoolReset
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private SpriteRenderer? spriteRenderer;
    [SerializeField] private GameObject? hitEffect;
    [Tooltip("Rotate the sprite so its local up faces the direction of travel.")]
    [SerializeField] private bool faceTravelDirection = true;

    [Header("Homing")]
    [SerializeField] private float seekRadius = 9f;
    [SerializeField] private float retargetInterval = 0.2f;

    private Element element = Element.Physical;
    private float damage;
    private float speed;
    private Vector2 direction = Vector2.up;

    private bool homing;
    private float turnDegreesPerSecond;
    private EnemyController? target;
    private float nextRetargetTime;

    private Rigidbody2D? body;
    private static int _arenaLayer = -1;

    public void OnSpawned()
    {
        target = null;
        homing = false;
        nextRetargetTime = 0f;
    }

    public void OnReleased()
    {
        target = null;
        homing = false;
    }

    /// <summary>Fires the projectile. Call <see cref="EnableHoming"/> afterwards for a seeking shot.</summary>
    public void Launch(Element attackElement, float projectileDamage, Vector2 launchDirection, float launchSpeed)
    {
        element = attackElement;
        damage = projectileDamage;
        speed = launchSpeed;
        direction = launchDirection.sqrMagnitude > 0.0001f ? launchDirection.normalized : Vector2.up;

        body ??= GetComponent<Rigidbody2D>();
        body.linearVelocity = direction * speed;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = ElementVisuals.GetGlowColor(element);
        }

        ApplyFacing();

        GetComponent<PooledInstance>()?.ReleaseAfter(lifetime);
    }

    public void EnableHoming(float turnRate)
    {
        homing = true;
        turnDegreesPerSecond = turnRate;
        AcquireTarget();
    }

    private void FixedUpdate()
    {
        if (homing)
        {
            SteerTowardTarget();
        }

        body ??= GetComponent<Rigidbody2D>();
        body.linearVelocity = direction * speed;
    }

    private void SteerTowardTarget()
    {
        // Timer-gated only: a null target must not re-scan the registry every physics step.
        if (Time.time >= nextRetargetTime)
        {
            AcquireTarget();
        }

        if (target == null)
        {
            return;
        }

        Vector2 desired = ((Vector2)target.transform.position - (Vector2)transform.position);
        if (desired.sqrMagnitude < 0.0001f)
        {
            return;
        }

        // Cap the turn per step so a fireball arcs toward its mark instead of snapping onto it.
        float maxDegrees = turnDegreesPerSecond * Time.fixedDeltaTime;
        direction = Vector3.RotateTowards(direction, desired.normalized, maxDegrees * Mathf.Deg2Rad, 0f);
        ApplyFacing();
    }

    private void AcquireTarget()
    {
        nextRetargetTime = Time.time + retargetInterval;
        target = ActiveEnemyRegistry.TryGetNearest(transform.position, seekRadius, out EnemyController nearest, out _)
            ? nearest
            : null;
    }

    private void ApplyFacing()
    {
        if (!faceTravelDirection)
        {
            return;
        }

        transform.up = direction;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_arenaLayer < 0)
        {
            _arenaLayer = LayerMask.NameToLayer("Arena");
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyController? enemy = other.GetComponent<EnemyController>();
            if (enemy != null && GameManager.Instance != null)
            {
                // Attunement: a same-element shot phases straight through and isn't consumed, matching how
                // EnemyProjectile treats the mirror case.
                if (GameManager.Instance.IsAttunementBlocked(element, enemy.element))
                {
                    return;
                }

                enemy.TakeDamage(
                    GameManager.Instance.CalculateDamage(enemy.element, element, damage),
                    new MoveType(element, AttackKind.Ranged));
            }

            Detonate();
            return;
        }

        if (other.gameObject.layer == _arenaLayer)
        {
            Detonate();
        }
    }

    private void Detonate()
    {
        if (hitEffect != null && PrefabPool.Instance != null)
        {
            GameObject fx = PrefabPool.Instance.Spawn(hitEffect, transform.position, Quaternion.identity);
            fx.GetComponent<IAttackAnimator>()?.PlayAnimation();
        }

        PrefabPool.Instance?.Release(gameObject);
    }
}
