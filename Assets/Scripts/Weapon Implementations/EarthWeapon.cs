using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Earth is the ballista turret: immovable firepower traded against mobility. This commit lands only the
/// barrel — a tap fires one straight bolt along the player's facing for fixed damage.
/// <para>
/// Deliberately absent, and each landing in its own commit: the charge ramp and the movement-lock aim
/// mode, the ballista actor, then pierce and charge scaling. Until then Earth reads as a slow
/// single-shot, which is the point — the tradeoff isn't real until charging actually roots you.
/// </para>
/// <para>
/// Note what this class does <em>not</em> implement: <c>OnMeleeHit</c> is left to the interface default
/// because Earth never spawns a melee hitbox, so it could never fire. Same for <c>OnCharge</c> until the
/// charge ramp exists.
/// </para>
/// </summary>
public class EarthWeapon : MonoBehaviour, IElementalWeapon
{
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

    /// <summary>
    /// Fires one bolt along the player's facing and returns the cooldown.
    /// </summary>
    /// <remarks>
    /// Unlike the melee elements this deliberately does NOT seek the nearest enemy and step toward it.
    /// A turret that walks itself into range would undercut the whole fantasy, and the movement lock is
    /// about to take walking away entirely — so aim stays the player's job from the start.
    /// </remarks>
    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        LaunchBolt(player);
        return meleeCooldown;
    }

    private void LaunchBolt(Transform player)
    {
        if (boltPrefab == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)player.up).normalized;
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
