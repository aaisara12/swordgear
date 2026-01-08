using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class LightningWeapon : MonoBehaviour, IElementalWeapon
{
    [SerializeField] private GameObject weaponCollider;  // TODO: Add separate, larger collider for weapon thrust 
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private string slashAnimName;
    [SerializeField] private string thrustAnimName;
    [SerializeField] private GameObject weakEffectObject;
    [SerializeField] private GameObject strongEffectObject;

    [SerializeField] GameObject lightningPrefab;

    [Header("Combat")]
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float thrustDistance = 1.5f;

    int combo = 0;
    bool lightningActive = false;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {}

    private IEnumerator Swing(Transform player)
    {
        GameObject weaponHitbox = Instantiate(weaponCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();

        float elapsedTime = 0f;

        GameObject effect = null;
        if (weakEffectObject != null)
        {
            effect = Instantiate(weakEffectObject, player.position + player.up * distanceFromPlayer, Quaternion.identity);
            effect.transform.up = player.up;
            if (combo > 0)
            {
                effect.transform.localScale += Vector3.left * 2; // x = -1 scale
            }
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            anim.Play(slashAnimName);
        }
        while (elapsedTime < swingDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / swingDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(weaponHitbox);
        if (effect != null)
        {
            Destroy(effect);
        }
    }

    IEnumerator Thrust(Transform player)
    {
        GameObject weaponHitbox = Instantiate(weaponCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();

        float elapsedTime = 0f;
        lightningActive = true;

        Vector2 startPos = player.position;
        Vector2 dest = player.position + player.up * thrustDistance;

        GameObject effect = null;
        if (strongEffectObject != null)
        {
            effect = Instantiate(strongEffectObject, player.position + player.up * distanceFromPlayer, Quaternion.identity);
            effect.transform.up = player.up;
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            anim.Play(thrustAnimName);
        }

        while (elapsedTime < swingDuration)
        {
            Vector2 pos = Vector2.Lerp(startPos, dest, elapsedTime * 8 / swingDuration);
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / swingDuration);
            player.position = pos;
            transform.position = player.position + player.up * distanceFromPlayer;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(weaponHitbox);
        if (effect != null)
        {
            Destroy(effect);
        }
        lightningActive = false;
    }

    public void Strike(Transform player, HashSet<UpgradeType> upgrades)
    {
        transform.position = player.position + player.up * distanceFromPlayer;
        transform.up = player.up;

        switch (combo)
        {
            case 0:
            case 1:
                StartCoroutine(Swing(player));
                break;
            case 2:

                StartCoroutine(Thrust(player));
                break;
        }
        combo = (combo + 1) % (upgrades.Contains(UpgradeType.Lightning_DashStrike) ? 3 : 2);
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
            Strike(player, upgrades);
            return;
        }

        Vector2 direction = (nearestEnemy.transform.position - player.position).normalized;
        player.up = direction;

        Vector2 dashPosition = (Vector2)player.position + direction * (shortestDistance * dashFactor);
        player.position = dashPosition;
        Strike(player, upgrades);
    }

    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        
    }

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Lightning, GameManager.Instance.currentDamage));
        if (lightningActive && upgrades.Contains(UpgradeType.Lightning_ApplyStatic))
        {
            ChainLightningProjectile lightning = Instantiate(lightningPrefab, enemy.transform.position, Quaternion.identity).GetComponent<ChainLightningProjectile>();
            lightning.Initialize(transform);
        }
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = Color.cyan;
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Lightning, GameManager.Instance.currentDamage * GameManager.Instance.rangedMultiplier));
        if (upgrades.Contains(UpgradeType.Lightning_ApplyStatic))
        {
            ChainLightningProjectile lightning = Instantiate(lightningPrefab, enemy.transform.position, Quaternion.identity).GetComponent<ChainLightningProjectile>();
            lightning.Initialize(transform);
        }
    }
}
