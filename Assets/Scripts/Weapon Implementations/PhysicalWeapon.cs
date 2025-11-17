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
    [Header("Combat")]
    [SerializeField] private float attackRadius = 3f;
    [SerializeField] private float dashFactor = 0.2f;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        return; // Physical does not have charge attacks 
    }

    private IEnumerator Swing(Transform player)
    {
        GameObject weaponHitbox = Instantiate(weaponCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();

        float elapsedTime = 0f;

        anim.Play(animName);
        while (elapsedTime < swingDuration)
        {
            // float alpha = Mathf.Lerp(1f, 0f, elapsedTime / swingDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(weaponHitbox); 
    }

    public void Strike(Transform player)
    {
        Debug.Log("Attack physical");
        StartCoroutine(Swing(player));
    }

    public void MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject? nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(player.position, enemy.transform.position);
            if (distance < shortestDistance && distance <= attackRadius)
            {
                shortestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy == null)
        {
            Strike(player);
            return;
        }

        Vector2 direction = (nearestEnemy.transform.position - player.position).normalized;
        player.up = direction;

        Vector2 dashPosition = (Vector2)player.position + direction * (shortestDistance * dashFactor);
        player.position = dashPosition;
        Strike(player);

        
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
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Physical, GameManager.Instance.currentDamage));
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = Color.white;
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Physical, GameManager.Instance.currentDamage * GameManager.Instance.rangedMultiplier));
    }
}
