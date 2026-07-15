#nullable enable
using UnityEngine;
using System;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    // Fired when this specific enemy dies.
    public event Action? OnDeath;

    // Global events so systems like ComboSystem can listen to all enemy hits/deaths.
    public static event Action<EnemyController, float, MoveType>? OnAnyEnemyHit;
    public static event Action<EnemyController>? OnAnyEnemyDeath;

    [SerializeField] private GameObject? player;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float hp = 100f;
    [SerializeField] private GameObject? deathFX;

    public float speedMultiplier = 1f;

    private Rigidbody2D? rb;
    private IMovementStrategy? movementStrategy;

    public Element element = Element.Physical;

    public GameObject? floatingPoints;

    // --- Hit feedback / juice (Tier 0) ---
    private const float FlashDuration = 0.06f;       // real-time white flash on hit
    private const float HitStopHitSeconds = 0.03f;   // freeze on a non-lethal hit
    private const float HitStopKillSeconds = 0.06f;  // longer freeze on a kill
    private const float KnockbackBase = 2.8f;
    private const float KnockbackPerDamage = 0.105f;
    private const float KnockbackMax = 6.3f;
    private const float KnockbackDuration = 0.12f;   // game-time window where movement is suppressed

    // Screen shake (centralised here so every damage source — melee, ranged, all elements — shakes
    // consistently and scales by damage, instead of a flat identical shake at each weapon call site).
    private const float ShakeDamageMin = 5f;         // damage that maps to the smallest hit shake
    private const float ShakeDamageMax = 30f;        // damage at/above which the hit shake maxes out
    private const float ShakeHitForceMin = 0.15f;
    private const float ShakeHitForceMax = 0.45f;
    private const float ShakeKillForce = 0.7f;       // base kill kick
    private const float ShakeKillHpDivisor = 60f;    // + up to ShakeKillBonusMax more, scaled by spawn HP
    private const float ShakeKillBonusMax = 0.8f;    // so tanky/elite kills kick harder (no elite flag needed)

    private SpriteRenderer[]? _sprites;
    private Material?[]? _origMats;
    private Coroutine? _flashRoutine;
    private float _knockbackTimer;
    private Vector2 _lastHitDir = Vector2.down;      // direction the last hit knocked this enemy (shake bias)
    private float _spawnHp = 1f;                      // HP at spawn (post-modifiers) — scales the kill shake
    private static Material? _flashMat;

    /// <summary>
    /// Applies difficulty / elemental / elite spawn multipliers.
    /// Call immediately after Instantiate, before the enemy acts.
    /// </summary>
    public void ApplySpawnModifiers(in SpawnModifiers modifiers)
    {
        hp *= Mathf.Max(0.05f, modifiers.HpMultiplier);
        speed *= Mathf.Max(0.05f, modifiers.SpeedMultiplier);

        if (!Mathf.Approximately(modifiers.ScaleMultiplier, 1f))
        {
            transform.localScale *= Mathf.Max(0.05f, modifiers.ScaleMultiplier);
        }

        EnemyAttackDamage? combat = GetComponent<EnemyAttackDamage>();
        if (combat == null)
        {
            combat = gameObject.AddComponent<EnemyAttackDamage>();
        }

        combat.ApplyCombatMultipliers(modifiers);
    }

    private void OnEnable()
    {
        ActiveEnemyRegistry.Register(this);
    }

    private void OnDisable()
    {
        ActiveEnemyRegistry.Unregister(this);
    }

    private void Start()
    {
        player = GameManager.Instance?.player;
        rb = GetComponent<Rigidbody2D>();
        movementStrategy = GetComponent<IMovementStrategy>();

        // Cache sprites + their original materials for the hit flash (material swap; the lit sprite shader
        // multiplies colour and can't brighten to white on its own).
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _origMats = new Material?[_sprites.Length];
        for (int i = 0; i < _sprites.Length; i++)
        {
            _origMats[i] = _sprites[i] != null ? _sprites[i].sharedMaterial : null;
        }

        if (_flashMat == null)
        {
            _flashMat = Resources.Load<Material>("EnemyFlash");
        }

        // Spawn modifiers (incl. elite HP boost) have already been applied, so this captures the effective
        // max HP — used to make tankier/elite kills kick the camera harder.
        _spawnHp = Mathf.Max(1f, hp);
    }

    private void FixedUpdate()
    {
        if (PlayerGameplayManager.Instance?.IsDefeated == true)
        {
            return;
        }

        if (player == null || rb == null || movementStrategy == null)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        float directionX = player.transform.position.x - rb.position.x;
        if (directionX > 0)
        {
            scale.x = Mathf.Abs(scale.x);
        }
        else
        {
            scale.x = -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;

        // Knockback window: let the impulse coast/decay and skip normal movement so the hit reads.
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity *= 0.85f;
            return;
        }

        // move
        movementStrategy.Move(rb, player.transform, speed * speedMultiplier);
    }

    /// <summary>
    /// Deal damage to this enemy. <paramref name="applyImpactFeel"/> gates knockback + hit-stop + shake so
    /// passive damage-over-time ticks and mass-cleanup kills don't jerk enemies or freeze the game.
    /// <paramref name="feedsCombo"/> gates the OnAnyEnemyHit event so DoT ticks don't inflate the combo/ult.
    /// </summary>
    public void TakeDamage(float damage, MoveType moveType = default, bool applyImpactFeel = true, bool feedsCombo = true)
    {
        if (GameManager.Instance)
            GameManager.Instance.DisplayDamageUI(transform.position, damage, moveType.Element);

        // DoT ticks pass feedsCombo:false so a burning enemy can't passively keep a combo alive, charge the
        // ult, or pollute move-staleness while the player does nothing.
        if (feedsCombo)
            OnAnyEnemyHit?.Invoke(this, damage, moveType);

        hp -= damage;

        // Notify systems that player dealt damage (e.g. lifesteal)
        GameManager.NotifyPlayerDealtDamage(damage);

        Flash();

        if (hp <= 0f)
        {
            Die(applyImpactFeel);
            return;
        }

        if (applyImpactFeel)
        {
            ApplyKnockback(damage);
            HitStop.Do(HitStopHitSeconds);
            ShakeForHit(damage);
        }
    }

    private void Die(bool impactFeel)
    {
        if (impactFeel)
        {
            HitStop.Do(HitStopKillSeconds);
            // Tankier / elite enemies (higher spawn HP) kick the camera harder on death.
            float killForce = ShakeKillForce + Mathf.Min(_spawnHp / ShakeKillHpDivisor, ShakeKillBonusMax);
            Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake(killForce);
        }

        // Global death event for systems that care about any enemy death.
        OnAnyEnemyDeath?.Invoke(this);

        OnDeath?.Invoke();
        GameObject? effectObject = PrefabPool.Instance != null
            ? PrefabPool.Instance.Spawn(deathFX, transform.position, Quaternion.identity)
            : null;
        IAttackAnimator? effect = null;
        if (effectObject != null)
            effect = effectObject.GetComponent<IAttackAnimator>();

        if (effect != null)
            effect.PlayAnimation();
        Destroy(gameObject);
    }

    private void Flash()
    {
        if (_flashMat == null || _sprites == null || _sprites.Length == 0)
        {
            return;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < _sprites!.Length; i++)
        {
            if (_sprites[i] != null)
            {
                _sprites[i].sharedMaterial = _flashMat;
            }
        }

        yield return new WaitForSecondsRealtime(FlashDuration);

        // Always restore to the cached originals (never to the flash material, even if interrupted mid-flash).
        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] != null)
            {
                _sprites[i].sharedMaterial = _origMats![i];
            }
        }

        _flashRoutine = null;
    }

    private void ApplyKnockback(float damage)
    {
        if (rb == null)
        {
            return;
        }

        Vector2 dir = Vector2.zero;
        if (player != null)
        {
            dir = (Vector2)transform.position - (Vector2)player.transform.position;
        }

        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = UnityEngine.Random.insideUnitCircle;
        }

        dir = dir.sqrMagnitude < 0.0001f ? Vector2.up : dir.normalized;
        _lastHitDir = dir;

        float force = Mathf.Clamp(KnockbackBase + damage * KnockbackPerDamage, KnockbackBase, KnockbackMax);
        rb.linearVelocity = dir * force;
        _knockbackTimer = KnockbackDuration;
    }

    /// <summary>
    /// Damage-scaled camera kick, biased toward the direction the enemy was knocked so hits don't all read
    /// the same. Runs on every impactful hit — melee, ranged, all elements — now that shake is centralised.
    /// </summary>
    private void ShakeForHit(float damage)
    {
        float t = Mathf.InverseLerp(ShakeDamageMin, ShakeDamageMax, damage);
        float force = Mathf.Lerp(ShakeHitForceMin, ShakeHitForceMax, t);

        // Mostly downward (reads as a shake, not a directional push) with only a faint nudge toward the hit.
        Vector3 dir = Vector3.down + (Vector3)(_lastHitDir * 0.15f);
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake(force, dir);
    }
}
