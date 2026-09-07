#nullable enable

using System.Collections.Generic;
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
    [Tooltip("Optional streak behind the shot. Tinted to the element on launch, like the sprite.")]
    [SerializeField] private TrailRenderer? trail;
    [Tooltip("Rotate the sprite so its local up faces the direction of travel.")]
    [SerializeField] private bool faceTravelDirection = true;

    [Header("Impact Explosion")]
    [Tooltip("Blast radius on impact. 0 means the projectile only damages what it directly hits, which " +
             "is how Fire's fireballs and Wind's darts behave.")]
    [SerializeField] private float explosionRadius = 0f;
    [Tooltip("The blast radius the hit effect looks right at unscaled. The effect is scaled by the actual " +
             "radius over this, so the explosion you see is the area that actually got damaged.")]
    [SerializeField] private float hitEffectBaseRadius = 1.5f;

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
    private float activeExplosionRadius;
    private readonly HashSet<EnemyController> blastHits = new HashSet<EnemyController>();
    private static int _arenaLayer = -1;

    private bool Explodes => activeExplosionRadius > 0f;

    // Backstop for a hit effect that neither self-destroys nor animates itself, so one can't leak per impact.
    private const float UnmanagedHitEffectLifetime = 2f;

    public void OnSpawned()
    {
        target = null;
        homing = false;
        nextRetargetTime = 0f;
        weaveTime = 0f;
        activeExplosionRadius = explosionRadius;
        blastHits.Clear();
        trail?.Clear();
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

        Color glow = ElementVisuals.GetGlowColor(element);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = glow;
        }

        TintAndResetTrail(glow);
        ApplyFacing();

        GetComponent<PooledInstance>()?.ReleaseAfter(lifetime);
    }

    /// <summary>
    /// Colours the trail to the element and wipes whatever it drew last time.
    /// </summary>
    /// <remarks>
    /// The clear is the important half: these projectiles are pooled, so a reused instance would otherwise
    /// draw a streak from wherever it last died to wherever it just spawned.
    /// </remarks>
    private void TintAndResetTrail(Color glow)
    {
        if (trail == null)
        {
            return;
        }

        Color tail = glow;
        tail.a = 0f;
        trail.startColor = glow;
        trail.endColor = tail;
        trail.Clear();
    }

    /// <summary>
    /// Makes this shot burst on impact, damaging everything within <paramref name="radius"/> instead of
    /// only what it struck. Call after <see cref="Launch"/>, mirroring <see cref="EnableHoming"/>.
    /// </summary>
    /// <remarks>
    /// Lets the caller scale the blast per shot — Earth's rock grows its radius with charge — without
    /// needing a prefab per size.
    /// </remarks>
    public void EnableExplosion(float radius)
    {
        activeExplosionRadius = Mathf.Max(0f, radius);
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

                // An exploding shot deals all its damage through the blast, which covers this enemy too —
                // applying both would double-dip on whatever it happened to strike.
                if (!Explodes)
                {
                    enemy.TakeDamage(
                        GameManager.Instance.CalculateDamage(enemy.element, element, damage),
                        new MoveType(element, AttackKind.Ranged));
                }
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
        if (Explodes)
        {
            ApplyBlastDamage();
        }

        SpawnHitEffect();

        PrefabPool.Instance?.Release(gameObject);
    }

    /// <summary>Plays the impact effect, sized to the blast it represents.</summary>
    /// <remarks>
    /// Instantiated rather than pooled because <see cref="CatchExplosionFX"/> destroys itself when it
    /// finishes, and pooling something that self-destroys hands a destroyed instance back out later. This
    /// mirrors how PlayerController spawns the same effect for the sword catch.
    /// </remarks>
    private void SpawnHitEffect()
    {
        if (hitEffect == null)
        {
            return;
        }

        GameObject fx = Instantiate(hitEffect, transform.position, Quaternion.identity);

        // Match the effect to the area that actually took damage. Without this a small blast and a fully
        // charged one look identical, so the explosion misreports how big the hit was.
        fx.transform.localScale = Explodes && hitEffectBaseRadius > 0.001f
            ? Vector3.one * (activeExplosionRadius / hitEffectBaseRadius)
            : Vector3.one;

        if (fx.TryGetComponent(out CatchExplosionFX explosion))
        {
            explosion.Play(ElementVisuals.GetGlowColor(element));
            return;
        }

        fx.GetComponent<IAttackAnimator>()?.PlayAnimation();
        Destroy(fx, UnmanagedHitEffectLifetime);
    }

    /// <summary>Damages every enemy inside the blast, once each.</summary>
    private void ApplyBlastDamage()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        blastHits.Clear();

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, activeExplosionRadius))
        {
            if (hit == null || !hit.CompareTag("Enemy"))
            {
                continue;
            }

            EnemyController? enemy = hit.GetComponent<EnemyController>();

            // One entry per enemy: a multi-collider enemy must not be hit once per collider.
            if (enemy == null || !blastHits.Add(enemy))
            {
                continue;
            }

            if (GameManager.Instance.IsAttunementBlocked(element, enemy.element))
            {
                continue;
            }

            enemy.TakeDamage(
                GameManager.Instance.CalculateDamage(enemy.element, element, damage),
                new MoveType(element, AttackKind.Ranged));
        }
    }
}
