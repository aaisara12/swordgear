using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PhysicalWeapon : MonoBehaviour, IElementalWeapon
{
    [Header("Hitbox Spawning")]
    [SerializeField] private GameObject weaponCollider;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private string animName;
    [SerializeField] private GameObject effectObject;
    [Header("Combat")]
    [SerializeField] private float attackRadius = ActiveEnemyRegistry.AutoTargetRadius;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float meleeCooldown = 0.3f;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        return; // Physical does not have charge attacks 
    }

    private IEnumerator Swing(Transform player)
    {
        GameObject weaponHitbox = Instantiate(weaponCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();
        weaponHitbox.transform.up = player.up;

        float elapsedTime = 0f;

        GameObject effect = null;
        if (effectObject != null)
        {
            effect = Instantiate(effectObject, player.position + player.up * distanceFromPlayer, Quaternion.identity);
            effect.transform.up = player.up;
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
            AudioSystem.Play(AudioSystem.Sound.Slash_Basic);
        } 
        else
        {
            anim.Play(animName);
        }
        
        while (elapsedTime < swingDuration)
        {
            // float alpha = Mathf.Lerp(1f, 0f, elapsedTime / swingDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(weaponHitbox);
        if (effect != null)
        {
            Destroy(effect);
        }
    }

    public void Strike(Transform player)
    {
        Debug.Log("Attack physical");
        StartCoroutine(Swing(player));
    }

    public float MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {
        if (!ActiveEnemyRegistry.TryGetNearest(player.position, attackRadius, out EnemyController nearestEnemy, out float shortestDistance))
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
        return;
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        return;
    }

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Physical, GameManager.Instance.GetEffectiveBaseDamage()));
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = Color.white;
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Physical, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()));
    }
}
