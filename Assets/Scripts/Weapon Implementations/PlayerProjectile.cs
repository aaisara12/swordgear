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

    [Header("Seek Weave")]
    [Tooltip("How far a homing projectile snakes off its launch heading while it has no target, in degrees. 0 disables the weave.")]
    [SerializeField] private float weaveAmplitudeDegrees = 16f;
    [Tooltip("Weave oscillations per second.")]
    [SerializeField] private float weaveFrequency = 2.5f;

    private Element element = Element.Physical;
    private float damage;
    private float speed;
    private Vector2 direction = Vector2.up;

    private bool homing;
    private float turnDegreesPerSecond;
    private EnemyController? target;
    private float nextRetargetTime;

    private Vector2 seekHeading = Vector2.up;
    private float weaveTime;
    private float weavePhase;

    private Rigidbody2D? body;
    private static int _arenaLayer = -1;

    public void OnSpawned()
    {
        target = null;
        homing = false;
        nextRetargetTime = 0f;
        weaveTime = 0f;
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

        // The weave oscillates around the launch heading. Phase starts at a zero crossing so the shot leaves
        // dead on its heading (a random phase would launch it up to the full amplitude off-axis, fighting the
        // burst's own spread); randomising which crossing just picks whether it curves left or right first,
        // which is enough to keep a burst from snaking in lockstep.
        seekHeading = direction;
        weaveTime = 0f;
        weavePhase = Random.value < 0.5f ? 0f : Mathf.PI;

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
            WeaveWhileSearching();
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

    /// <summary>
    /// Snakes an untargeted homing projectile around its launch heading so it reads as hunting rather than
    /// gliding. The offset is rebuilt from the fixed heading each step instead of being integrated into the
    /// current direction, so it always returns to centre and can never accumulate drift. Once a target is
    /// found this stops and normal steering takes over from wherever the weave left the nose pointing.
    /// </summary>
    private void WeaveWhileSearching()
    {
        if (weaveAmplitudeDegrees <= 0f)
        {
            return;
        }

        weaveTime += Time.fixedDeltaTime;
        float offsetDegrees = Mathf.Sin(weavePhase + weaveTime * weaveFrequency * 2f * Mathf.PI) * weaveAmplitudeDegrees;
        direction = ((Vector2)(Quaternion.Euler(0f, 0f, offsetDegrees) * seekHeading)).normalized;
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
