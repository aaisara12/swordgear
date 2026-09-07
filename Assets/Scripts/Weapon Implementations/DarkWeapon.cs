using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dark is the scythe: a heavier, slower arc than Physical's sword, with a charge that blinks the player
/// a short way forward and cuts a circle around where they land.
/// </summary>
/// <remarks>
/// Modelled on <see cref="PhysicalWeapon"/> — seek the nearest enemy, step in, spawn a static hitbox —
/// because that shape is already proven and Dark's identity in this commit is weight, not a new motion.
/// It borrows Physical's <em>shape</em> but shares none of its state: the two are separate components with
/// their own fields, and the hitbox they both spawn routes damage through
/// <see cref="ElementManager"/> to whichever element is active.
/// <para>
/// A Dark melee kill raises a shade that fights for you — see <see cref="DarkMinion"/> for why that is a
/// new actor rather than the enemy switching sides.
/// </para>
/// <para>
/// ⚠️ <c>OnMeleeHit</c> is overridden rather than defaulted. The interface default is a no-op, so a
/// weapon that spawns a hitbox and forgets it deals <b>zero damage</b>.
/// </para>
/// </remarks>
public class DarkWeapon : MonoBehaviour, IElementalWeapon, IMeleeChargeProvider
{
    [Header("Hitbox Spawning")]
    [SerializeField] private GameObject weaponCollider;
    [Tooltip("Longer than Physical's swing — the scythe is heavier and commits harder.")]
    [SerializeField] private float swingDuration = 0.6f;
    [SerializeField] private float distanceFromPlayer = 0.7f;
    [SerializeField] private GameObject effectObject;

    [Header("Combat")]
    [SerializeField] private float attackRadius = 3.5f;
    [Tooltip("Fraction of the distance to the target the player steps in on a swing.")]
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float meleeCooldown = 0.45f;
    [Tooltip("Hits harder than a plain sword, and swings slower to pay for it.")]
    [SerializeField] private float meleeDamageMultiplier = 1.25f;

    [Header("Charge — blink and cut")]
    [Tooltip("Seconds of hold at which the charge indicator reads full. The blink fires on release " +
             "whatever the charge, so this is feedback rather than a gate.")]
    [SerializeField] private float maxChargeTime = 0.6f;
    [Tooltip("How far the blink carries. Short enough to be a repositioning tool rather than an escape.")]
    [SerializeField] private float blinkDistance = 4.5f;
    [SerializeField] private float circleRadius = 3f;
    [Tooltip("Damage of the circle cut, as a multiple of base. Higher than the swing because it costs a " +
             "charge and lands you in the middle of whatever you just blinked into.")]
    [SerializeField] private float circleDamageMultiplier = 1.6f;
    [SerializeField] private GameObject circleEffectObject;
    [Tooltip("Layers the blink refuses to cross, so it can't drop the player inside a wall.")]
    [SerializeField] private LayerMask blinkBlockers = 1;
    [Tooltip("How far short of a blocking wall the blink lands.")]
    [SerializeField] private float blinkWallMargin = 0.5f;

    [Header("Execution")]
    [Tooltip("Raised when a Dark melee blow kills. Leave empty to disable executions entirely.")]
    [SerializeField] private GameObject minionPrefab;
    [Tooltip("Shade damage per hit, as a multiple of base.")]
    [SerializeField] private float minionDamageMultiplier = 0.6f;
    [Tooltip("Ceiling on live shades. Without one, a good Dark run buries the arena and the player stops " +
             "having to fight at all.")]
    [SerializeField] private int maxLiveMinions = 4;

    [Header("Aim")]
    [Tooltip("A shade past Physical's 5: the scythe's arc reaches further than a sword swing, but Dark " +
             "is still a melee element that has to close the distance.")]
    [SerializeField] private float autoAimRadius = 6f;

    public float AutoAimRadius => autoAimRadius;

    private bool isCharging;
    private float chargeDuration;
    private readonly List<DarkMinion> liveMinions = new List<DarkMinion>();

    // The enemy currently being struck, so the death handler knows the kill was ours.
    private EnemyController executionTarget;
    private Vector3 executionPosition;
    private Vector3 executionScale;
    private Sprite executionSprite;

    private void OnEnable()
    {
        EnemyController.OnAnyEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EnemyController.OnAnyEnemyDeath -= HandleEnemyDeath;
    }

    /// <summary>Raises a shade when the enemy that just died is the one this weapon was hitting.</summary>
    /// <remarks>
    /// This has to run off the death EVENT rather than a null check after the hit. <c>Destroy</c> is
    /// deferred to the end of the frame, so the enemy reference is still alive immediately afterwards and
    /// the obvious "did it die?" test silently never fires. <c>OnAnyEnemyDeath</c> is raised synchronously
    /// inside <c>Die()</c>, so it lands while the strike is still on the stack.
    /// </remarks>
    private void HandleEnemyDeath(EnemyController enemy)
    {
        if (enemy == null || enemy != executionTarget)
        {
            return;
        }

        RaiseMinion(executionPosition, executionScale, executionSprite);
    }

    // ---- IMeleeChargeProvider: lights up the existing charge indicators ----

    public bool IsCharging => isCharging;

    public float ChargeProgress =>
        isCharging && maxChargeTime > 0f ? Mathf.Clamp01(chargeDuration / maxChargeTime) : 0f;

    public bool IsMaxCharge =>
        isCharging && maxChargeTime > 0f && chargeDuration >= maxChargeTime;

    // No upgrade gate: the blink is Dark's baseline identity, not a purchase.
    public bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player) =>
        player.IsMeleeReady;

    /// <summary>
    /// Starts the charge. Unlike Earth, Dark does <b>not</b> root the player — it stays mobile, because
    /// its charge is a repositioning tool and rooting would fight the thing the charge is for.
    /// </summary>
    public void OnCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        if (cancel)
        {
            ResetCharge();
            return;
        }

        if (isCharging)
        {
            return;
        }

        isCharging = true;
        chargeDuration = 0f;
    }

    /// <summary>Clears the charge whenever the imbue ends, in case something skipped the cancel path.</summary>
    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        ResetCharge();
    }

    private void Update()
    {
        if (isCharging && chargeDuration < maxChargeTime)
        {
            chargeDuration = Mathf.Min(chargeDuration + Time.deltaTime, maxChargeTime);
        }
    }

    private void ResetCharge()
    {
        isCharging = false;
        chargeDuration = 0f;
    }

    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        // A charge release arrives here too — ReleaseChargeAttack dispatches to OnTap — so isCharging is
        // what tells a blink from a plain swing.
        if (isCharging)
        {
            ResetCharge();
            BlinkAndCut(player);
            return meleeCooldown;
        }

        float seekRadius = MeleeAugmentUtility.ScaleSeekRadius(attackRadius);
        if (ActiveEnemyRegistry.TryGetNearest(player.position, seekRadius, out EnemyController nearestEnemy, out float shortestDistance))
        {
            // Step in toward the target, the same closing move Physical makes.
            Vector2 direction = ((Vector2)nearestEnemy.transform.position - (Vector2)player.position).normalized;
            player.up = direction;
            player.position = (Vector2)player.position + direction * (shortestDistance * dashFactor);
        }

        StartCoroutine(Swing(player));
        return meleeCooldown;
    }

    private IEnumerator Swing(Transform player)
    {
        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        Vector3 spawnPos = player.position + player.up * reach;

        GameObject weaponHitbox = PrefabPool.Instance!.Spawn(weaponCollider, spawnPos, Quaternion.identity);
        weaponHitbox.transform.up = player.up;
        MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);

        GameObject effect = null;
        if (effectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(effectObject, spawnPos, Quaternion.identity, player);
            effect.transform.up = player.up;
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            effect.GetComponent<IAttackAnimator>()?.PlayAnimation();
            AudioSystem.Play(AudioSystem.Sound.Slash_Basic);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        PrefabPool.Instance!.Release(weaponHitbox);
        if (effect != null)
        {
            PrefabPool.Instance!.Release(effect);
        }
    }

    /// <summary>
    /// Teleports a short way along the player's facing, then cuts a circle around where they land.
    /// </summary>
    /// <remarks>
    /// Reuses <c>PlayerController.BlinkTo</c> (Lightning's Thunderstep primitive) and
    /// <c>MeleeAugmentUtility.DamageEnemiesInRadius</c> (cleave's) rather than inventing either. BlinkTo
    /// carries the dash cooldown, i-frames and afterimage with it, so blinking into a pack is survivable
    /// on arrival — which is the whole point of landing in the middle of one.
    /// </remarks>
    private void BlinkAndCut(Transform player)
    {
        PlayerController controller = player.GetComponent<PlayerController>();
        Vector2 direction = ((Vector2)player.up).normalized;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector2.up;
        }

        Vector2 destination = ResolveBlinkDestination(player.position, direction);

        if (controller != null)
        {
            controller.BlinkTo(destination);
        }
        else
        {
            player.position = destination;
        }

        MeleeAugmentUtility.DamageEnemiesInRadius(destination, MeleeAugmentUtility.ScaleSeekRadius(circleRadius), enemy =>
        {
            enemy.TakeDamage(
                GameManager.Instance.CalculateDamage(enemy.element, Element.Dark, GameManager.Instance.GetEffectiveBaseDamage() * circleDamageMultiplier),
                new MoveType(Element.Dark, AttackKind.MeleeCharge));
        });

        AudioSystem.Play(AudioSystem.Sound.Slash_Basic);

        if (circleEffectObject != null)
        {
            GameObject effect = PrefabPool.Instance!.Spawn(circleEffectObject, destination, Quaternion.identity);
            effect.transform.up = direction;
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            effect.GetComponent<IAttackAnimator>()?.PlayAnimation();
            effect.GetComponent<PooledInstance>()?.ReleaseWhenParticlesDone();
        }
    }

    /// <summary>
    /// Where the blink actually lands: full distance, or just short of the first wall in the way.
    /// </summary>
    /// <remarks>
    /// BlinkTo sets the position outright with no collision check, so without this the charge could drop
    /// the player inside level geometry.
    /// </remarks>
    private Vector2 ResolveBlinkDestination(Vector2 origin, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, blinkDistance, blinkBlockers);
        if (hit.collider == null)
        {
            return origin + direction * blinkDistance;
        }

        float safeDistance = Mathf.Max(0f, hit.distance - blinkWallMargin);
        return origin + direction * safeDistance;
    }

    /// <summary>
    /// Damages the enemy, and raises a shade in its place if the blow killed it.
    /// </summary>
    /// <remarks>
    /// The kill is detected through <c>EnemyController.OnAnyEnemyDeath</c>, which fires synchronously
    /// inside <c>Die()</c>. Checking the reference for null after the hit does <b>not</b> work: <c>Destroy</c>
    /// is deferred to the end of the frame, so the enemy is still non-null on the next line.
    /// <para>
    /// The enemy dies a completely normal death, so wave-clear accounting, the combo and ultimate credit
    /// all stay correct without this having to special-case any of them.
    /// </para>
    /// </remarks>
    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        // Captured BEFORE the hit, because the death handler fires from inside TakeDamage and the enemy
        // is mid-teardown by then.
        executionTarget = enemy;
        executionPosition = enemy.transform.position;

        // Take the renderer's WORLD scale, not the sprite alone: enemies draw through a scaled child, so a
        // shade built from the bare sprite comes out a fraction of the size the enemy appeared.
        SpriteRenderer corpseRenderer = enemy.GetComponentInChildren<SpriteRenderer>();
        executionSprite = corpseRenderer != null ? corpseRenderer.sprite : null;
        executionScale = corpseRenderer != null ? corpseRenderer.transform.lossyScale : Vector3.one;

        enemy.TakeDamage(
            GameManager.Instance.CalculateDamage(enemy.element, Element.Dark, GameManager.Instance.GetEffectiveBaseDamage() * meleeDamageMultiplier),
            new MoveType(Element.Dark, AttackKind.MeleeStrike));

        executionTarget = null;
    }

    /// <summary>Raises a shade where an enemy died, unless the arena already has its fill.</summary>
    private void RaiseMinion(Vector3 position, Vector3 corpseScale, Sprite corpseSprite)
    {
        if (minionPrefab == null)
        {
            return;
        }

        liveMinions.RemoveAll(m => m == null);
        if (liveMinions.Count >= maxLiveMinions)
        {
            return;
        }

        GameObject obj = Instantiate(minionPrefab, position, Quaternion.identity);
        DarkMinion minion = obj.GetComponent<DarkMinion>();
        if (minion == null)
        {
            Destroy(obj);
            return;
        }

        minion.Raise(corpseSprite, corpseScale, GameManager.Instance.GetEffectiveBaseDamage() * minionDamageMultiplier);
        liveMinions.Add(minion);
    }

    // Ranged hooks are dormant while the sword throw is the ultimate, but implemented rather than
    // defaulted so Dark stays consistent with the other elements if the throw comes back.
    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = ElementVisuals.GetColor(Element.Dark);
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(
            GameManager.Instance.CalculateDamage(enemy.element, Element.Dark, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Dark, AttackKind.Ranged));
    }
}
