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
    [SerializeField] private string animName;
    [Header("Ranged")]
    [SerializeField] private GameObject chillFieldObject;
    [SerializeField] private float fieldSpawnInterval = 0.2f;
    [SerializeField] private float fieldDuration = 3f;
    [Header("Combat")]
    [SerializeField] private float attackRadius = 3f;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float strongHitBonusDmg = 10f;
    [SerializeField] private int chillDuration = 5;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        
    }

    int combo = 1;

    private IEnumerator Swing(Transform player)
    {
        GameObject weaponHitbox;
        if (combo == 0)
            weaponHitbox = Instantiate(weakCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        else
            weaponHitbox = Instantiate(strongCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);

        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();

        float elapsedTime = 0f;

        anim.Play(animName);
        while (elapsedTime < swingDuration)
        {

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
        combo = (combo + 1) % 2;
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
        
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        combo = 1;
    }

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Ice, GameManager.Instance.currentDamage + strongHitBonusDmg * combo));

        if (combo == 1)
        {
            GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Chill, chillDuration);
        }
    }

    float flightTime = 0f;

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        flightTime = (flightTime += Time.deltaTime) % fieldSpawnInterval;
        if (flightTime < Time.deltaTime)
        {
            IceChillField field = Instantiate(chillFieldObject, sword.transform.position, Quaternion.identity).GetComponent<IceChillField>();
            field.lingerDuration = fieldDuration;
            field.BeginEffect();
        }
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Ice, GameManager.Instance.currentDamage * GameManager.Instance.rangedMultiplier));
    }

}
