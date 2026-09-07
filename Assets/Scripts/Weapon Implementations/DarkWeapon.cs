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
/// Deliberately absent, landing in its own commit: execution raising the corpse as a minion — which is
/// where Dark stops being a recoloured sword.
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

    [Header("Aim")]
    [Tooltip("A shade past Physical's 5: the scythe's arc reaches further than a sword swing, but Dark " +
             "is still a melee element that has to close the distance.")]
    [SerializeField] private float autoAimRadius = 6f;

    public float AutoAimRadius => autoAimRadius;

    private bool isCharging;
    private float chargeDuration;

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

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(
            GameManager.Instance.CalculateDamage(enemy.element, Element.Dark, GameManager.Instance.GetEffectiveBaseDamage() * meleeDamageMultiplier),
            new MoveType(Element.Dark, AttackKind.MeleeStrike));
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
