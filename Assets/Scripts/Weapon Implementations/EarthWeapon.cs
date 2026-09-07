using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Earth is the ballista turret: immovable firepower traded against mobility. A tap fires one straight
/// bolt along the player's facing; holding roots the player and turns the movement stick into aim, and
/// releasing fires along that aim.
/// <para>
/// Deliberately absent, and each landing in its own commit: the ballista actor, then pierce and charge
/// scaling. Damage is flat for now, so the charge buys aim rather than power — the tap-versus-charge
/// tradeoff isn't real until the charge tiers land.
/// </para>
/// <para>
/// Note what this class does <em>not</em> implement: <c>OnMeleeHit</c> is left to the interface default
/// because Earth never spawns a melee hitbox, so it could never fire. Same for <c>OnCharge</c> until the
/// charge ramp exists.
/// </para>
/// </summary>
public class EarthWeapon : MonoBehaviour, IElementalWeapon, IMeleeChargeProvider, IAimLockProvider
{
    [Header("Charge")]
    [Tooltip("Seconds of hold to reach full charge. The root and the aim start immediately on hold; " +
             "this only drives the charge indicator until charge tiers land.")]
    [SerializeField] private float maxChargeTime = 0.8f;

    [Header("Bolt")]
    [SerializeField] private GameObject boltPrefab;
    [SerializeField] private float boltSpeed = 11f;
    [SerializeField] private float boltDamageMultiplier = 1.3f;
    [Tooltip("How far in front of the player the bolt spawns, so it clears their own collider.")]
    [SerializeField] private float boltSpawnOffset = 0.5f;

    [Header("Combat")]
    [SerializeField] private float meleeCooldown = 0.5f;

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;

    private bool isCharging;
    private float chargeDuration;
    private Vector2 aimDirection = Vector2.up;

    // ---- IMeleeChargeProvider: the charge indicators come for free once these report honestly ----

    public bool IsCharging => isCharging;

    public float ChargeProgress =>
        isCharging && maxChargeTime > 0f ? Mathf.Clamp01(chargeDuration / maxChargeTime) : 0f;

    public bool IsMaxCharge =>
        isCharging && maxChargeTime > 0f && chargeDuration >= maxChargeTime;

    // No upgrade gate, unlike Fire's Fire_ChargeMelee: rooting to aim IS Earth's identity, not a purchase.
    public bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player) =>
        player.IsMeleeReady;

    // ---- IAimLockProvider ----

    public bool IsAimLocked => isCharging;

    public Vector2 AimDirection => aimDirection;

    public void SetAimDirection(Vector2 direction)
    {
        // Hold the last aim when the stick returns to centre — snapping back to a default mid-charge would
        // throw the shot away every time the player lets go to steady their hand.
        if (direction.sqrMagnitude > 0.001f)
        {
            aimDirection = direction.normalized;
        }
    }

    /// <summary>
    /// Begins the root. The player stops moving and their movement stick becomes aim until release or cancel.
    /// </summary>
    /// <remarks>
    /// Only reached after the hold interaction validates (0.3s), so a plain tap never roots the player.
    /// </remarks>
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

        // Seed from the current facing so the aim indicator has a sane direction on the very first frame,
        // before PlayerController feeds in the stick. Keeps this weapon correct on its own.
        Vector2 facing = ((Vector2)player.up).normalized;
        if (facing.sqrMagnitude > 0.001f)
        {
            aimDirection = facing;
        }
    }

    /// <summary>Clears the root whenever the imbue ends, in case something skipped the cancel path.</summary>
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

    /// <summary>
    /// Fires one bolt and returns the cooldown. A charged release fires along the aim; a bare tap fires
    /// along the player's facing.
    /// </summary>
    /// <remarks>
    /// Both arrive here — PlayerController.ReleaseChargeAttack dispatches a charge release to OnTap, so
    /// <c>isCharging</c> is what tells them apart. Same trick FireWeapon uses with its charge duration.
    /// <para>
    /// Unlike the melee elements this deliberately does NOT seek the nearest enemy and step toward it.
    /// A turret that walks itself into range would undercut the whole fantasy — aim is the player's job.
    /// </para>
    /// <para>
    /// Damage does not scale with charge yet; that lands with the charge tiers. Today the charge buys
    /// aim, not power.
    /// </para>
    /// </remarks>
    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        Vector2 direction = isCharging ? aimDirection : ((Vector2)player.up).normalized;

        // Drop the root BEFORE firing, so nothing below can leave the player stuck.
        ResetCharge();

        LaunchBolt(player, direction);
        return meleeCooldown;
    }

    private void LaunchBolt(Transform player, Vector2 direction)
    {
        if (boltPrefab == null)
        {
            return;
        }

        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        Vector3 spawnPos = player.position + (Vector3)(direction * boltSpawnOffset);

        GameObject obj = PrefabPool.Instance!.Spawn(boltPrefab, spawnPos, Quaternion.identity);
        PlayerProjectile bolt = obj.GetComponent<PlayerProjectile>();
        if (bolt == null)
        {
            PrefabPool.Instance!.Release(obj);
            return;
        }

        float damage = GameManager.Instance.GetEffectiveBaseDamage() * boltDamageMultiplier;

        // No EnableHoming: the bolt flies dead straight. Homing would make aiming pointless, and aiming is
        // the mechanic the rest of Earth is built on.
        bolt.Launch(Element.Earth, damage, direction, boltSpeed);
        AudioSystem.Play(AudioSystem.Sound.Slash_Basic);
    }

    public void Cleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        StartCoroutine(PlayCleave(player));
    }

    private IEnumerator PlayCleave(Transform player)
    {
        MeleeAugmentUtility.DamageEnemiesInRadius(player.position, MeleeAugmentUtility.ScaleSeekRadius(cleaveRadius), enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Earth, GameManager.Instance.GetEffectiveBaseDamage()),
                new MoveType(Element.Earth, AttackKind.MeleeStrike));
        });

        AudioSystem.Play(AudioSystem.Sound.Slash_Basic);

        if (cleaveEffectObject == null)
        {
            yield break;
        }

        GameObject effect = PrefabPool.Instance!.Spawn(cleaveEffectObject, player.position, Quaternion.identity, player);
        effect.transform.up = player.up;
        IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
        attackAnimator.PlayAnimation();

        yield return new WaitForSeconds(MeleeAugmentUtility.ScaleSwingDuration(cleaveDuration));

        PrefabPool.Instance!.Release(effect);
    }

    // Ranged hooks are dormant while the sword throw is the ultimate, but they're implemented rather than
    // defaulted so Earth stays consistent with the other five elements if the throw comes back.
    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = ElementVisuals.GetColor(Element.Earth);
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Earth, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Earth, AttackKind.Ranged));
    }
}
