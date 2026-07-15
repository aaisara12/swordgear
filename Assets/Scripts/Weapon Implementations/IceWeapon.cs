using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class IceWeapon : MonoBehaviour, IElementalWeapon
{
    /*
     * This weapon applies AoE slows as its main effect. The melee attack is a two hit sweeping combo where the second hit slows briefly, and the ranged attack spawns a lingering trail that slows enemies 
     */

    [Header("Hitbox Spawning")]
    [SerializeField] private GameObject weakCollider;  // First hit of combo
    [SerializeField] private GameObject strongCollider;  // Second hit of combo
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private float strongHitScaling = 1.2f;
    [SerializeField] private string animName;
    [SerializeField] private GameObject effectObject;
    [Header("Ranged")]
    [SerializeField] private GameObject chillFieldObject;
    [SerializeField] private float fieldSpawnInterval = 0.2f;
    [SerializeField] private float fieldDuration = 3f;
    [Header("Combat")]
    [SerializeField] private float attackRadius = ActiveEnemyRegistry.AutoTargetRadius;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float meleeCooldown = 0.3f;
    [SerializeField] private float strongHitBonusDmg = 10f;
    [SerializeField] private int chillDuration = 5;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        
    }

    int combo = 1;

    private IEnumerator Swing(Transform player)
    {
        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        Vector3 spawnPos = player.position + player.up * reach;

        GameObject weaponHitbox;
        if (combo == 0)
            weaponHitbox = PrefabPool.Instance!.Spawn(weakCollider, spawnPos, Quaternion.identity);
        else
            weaponHitbox = PrefabPool.Instance!.Spawn(strongCollider, spawnPos, Quaternion.identity);

        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();
        weaponHitbox.transform.up = player.up;

        float elapsedTime = 0f;

        GameObject effect = null;
        if (effectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(effectObject, spawnPos, Quaternion.identity);
            effect.transform.up = player.up;
            if (combo > 0)
            {
                effect.transform.localScale += Vector3.left * 2; // x = -1 scale
                effect.transform.localScale *= strongHitScaling;
                weaponHitbox.transform.localScale *= strongHitScaling;
                AudioSystem.Play(AudioSystem.Sound.Slash_IceEmpowered);
            }
            else
            {
                AudioSystem.Play(AudioSystem.Sound.Slash_IceBasic);
            }
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);
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

    public void Strike(Transform player)
    {
        Debug.Log("Attack physical");
        StartCoroutine(Swing(player));
    }

    public float MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {
        if (upgrades.Contains(UpgradeType.Ice_EmpowerMelee))
            combo = (combo + 1) % 2;
        else
            combo = 0;

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
        combo = 1;
    }

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Ice, GameManager.Instance.GetEffectiveBaseDamage() + strongHitBonusDmg * combo),
            new MoveType(Element.Ice, AttackKind.MeleeStrike));

        if (combo == 1)
        {
            GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Chill, chillDuration);
        }
    }

    float flightTime = 0f;

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        flightTime = (flightTime += Time.deltaTime) % fieldSpawnInterval;
        if (flightTime < Time.deltaTime && upgrades.Contains(UpgradeType.Ice_RangedChill))
        {
            IceChillField field = PrefabPool.Instance!.Spawn(chillFieldObject, sword.transform.position, Quaternion.identity).GetComponent<IceChillField>();
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
