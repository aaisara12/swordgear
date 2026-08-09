using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Ice is the spear: a single committed thrust with long reach and heavy damage, paid for with a slow
/// attack rate. The hitbox is stretched along its local forward axis only — the spear pokes, it doesn't sweep.
/// </summary>
public class IceWeapon : MonoBehaviour, IElementalWeapon
{
    [Header("Hitbox Spawning")]
    [SerializeField] private GameObject weakCollider;  // the spear thrust hitbox
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private string animName;
    [SerializeField] private GameObject effectObject;

    [Header("Spear")]
    [Tooltip("Stretches the thrust along its forward axis only, so reach grows without the hitbox widening.")]
    [SerializeField] private float reachScale = 1.9f;
    [Tooltip("The spear hits hard to pay for its slow cadence.")]
    [SerializeField] private float meleeDamageMultiplier = 1.8f;
    [Header("Ranged")]
    [SerializeField] private GameObject chillFieldObject;
    [SerializeField] private float fieldSpawnInterval = 0.2f;
    [SerializeField] private float fieldDuration = 3f;
    [Header("Combat")]
    [SerializeField] private float attackRadius = ActiveEnemyRegistry.AutoTargetRadius;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float meleeCooldown = 0.3f;
    [SerializeField] private int chillDuration = 5;

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {

    }

    private IEnumerator Swing(Transform player)
    {
        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        Vector3 spawnPos = player.position + player.up * reach;

        GameObject weaponHitbox = PrefabPool.Instance!.Spawn(weakCollider, spawnPos, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();
        weaponHitbox.transform.up = player.up;

        // Stretch forward only: scaling local Y lengthens the thrust, X is left alone so it never widens.
        StretchForward(weaponHitbox.transform, player);
        MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);

        float elapsedTime = 0f;

        GameObject effect = null;
        if (effectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(effectObject, spawnPos, Quaternion.identity, player);
            effect.transform.up = player.up;
            effect.transform.localScale = Vector3.Scale(effect.transform.localScale, new Vector3(1f, reachScale, 1f));
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            AudioSystem.Play(AudioSystem.Sound.Slash_IceEmpowered);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            anim.Play(animName);
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        PrefabPool.Instance!.Release(weaponHitbox);
        if (effect != null)
        {
            PrefabPool.Instance!.Release(effect);
        }
    }

    /// <summary>
    /// Lengthens a hitbox along its local forward (+Y) axis, then slides it forward so the near edge stays
    /// where it was. Without the slide, scaling a box whose offset is forward of its origin also grows it
    /// backwards through the player.
    /// </summary>
    private void StretchForward(Transform hitbox, Transform player)
    {
        hitbox.localScale = Vector3.Scale(hitbox.localScale, new Vector3(1f, reachScale, 1f));

        BoxCollider2D box = hitbox.GetComponent<BoxCollider2D>();
        if (box == null)
        {
            return;
        }

        float nearEdge = box.offset.y - box.size.y * 0.5f;
        hitbox.position += player.up * (-nearEdge * (reachScale - 1f));
    }

    public void Strike(Transform player)
    {
        Debug.Log("Attack physical");
        StartCoroutine(Swing(player));
    }

    public float MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {
        float seekRadius = MeleeAugmentUtility.ScaleSeekRadius(attackRadius);
        if (!ActiveEnemyRegistry.TryGetNearest(player.position, seekRadius, out EnemyController nearestEnemy, out float shortestDistance))
        {
            Strike(player);
            return meleeCooldown;
        }

        Vector2 direction = ((Vector2)nearestEnemy.transform.position - (Vector2)player.position).normalized;
        player.up = direction;

        Vector2 dashPosition = (Vector2)player.position + direction * (shortestDistance * dashFactor);
        player.position = dashPosition;
        Strike(player);
        return meleeCooldown;
    }

    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {

    }

    public void Cleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        StartCoroutine(PlayCleave(player, upgrades));
    }

    private IEnumerator PlayCleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        bool applyCleaveChill = upgrades.Contains(UpgradeType.Ice_EmpowerMelee);
        MeleeAugmentUtility.DamageEnemiesInRadius(player.position, MeleeAugmentUtility.ScaleSeekRadius(cleaveRadius), enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Ice, GameManager.Instance.GetEffectiveBaseDamage()),
                new MoveType(Element.Ice, AttackKind.MeleeStrike));
            if (applyCleaveChill)
            {
                GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Chill, chillDuration);
            }
        });

        AudioSystem.Play(AudioSystem.Sound.Slash_IceBasic);

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

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        float damage = GameManager.Instance.GetEffectiveBaseDamage() * meleeDamageMultiplier;
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Ice, damage),
            new MoveType(Element.Ice, AttackKind.MeleeStrike));

        GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Chill, chillDuration);
    }

    float flightTime = 0f;

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        flightTime = (flightTime += Time.deltaTime) % fieldSpawnInterval;
        if (flightTime < Time.deltaTime && upgrades.Contains(UpgradeType.Ice_RangedChill))
        {
            IceChillField field = PrefabPool.Instance!.Spawn(chillFieldObject, sword.transform.position, Quaternion.identity).GetComponent<IceChillField>();
            field.transform.up = sword.transform.up; // align so the frost drifts backward along the blade's path, reading as a trail rather than a static pool
            field.lingerDuration = fieldDuration;
            field.BeginEffect();
        }
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Ice, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Ice, AttackKind.Ranged));
    }

}
