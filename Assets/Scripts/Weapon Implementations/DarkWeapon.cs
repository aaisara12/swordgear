using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dark is the scythe: a heavier, slower arc than Physical's sword. This commit lands only the swing.
/// </summary>
/// <remarks>
/// Modelled on <see cref="PhysicalWeapon"/> — seek the nearest enemy, step in, spawn a static hitbox —
/// because that shape is already proven and Dark's identity in this commit is weight, not a new motion.
/// It borrows Physical's <em>shape</em> but shares none of its state: the two are separate components with
/// their own fields, and the hitbox they both spawn routes damage through
/// <see cref="ElementManager"/> to whichever element is active.
/// <para>
/// Deliberately absent, each landing in its own commit: the charge that blinks and cuts a circle, then
/// execution raising the corpse as a minion — which is where Dark stops being a recoloured sword.
/// </para>
/// <para>
/// ⚠️ <c>OnMeleeHit</c> is overridden rather than defaulted. The interface default is a no-op, so a
/// weapon that spawns a hitbox and forgets it deals <b>zero damage</b>.
/// </para>
/// </remarks>
public class DarkWeapon : MonoBehaviour, IElementalWeapon
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

    [Header("Aim")]
    [Tooltip("A shade past Physical's 5: the scythe's arc reaches further than a sword swing, but Dark " +
             "is still a melee element that has to close the distance.")]
    [SerializeField] private float autoAimRadius = 6f;

    public float AutoAimRadius => autoAimRadius;

    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
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
