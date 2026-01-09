using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class FireWeapon : MonoBehaviour, IElementalWeapon
{
    [SerializeField] private GameObject weaponCollider;
    [SerializeField] private GameObject strongCollider;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] float spinSpeed = 50f;
    [SerializeField] private float maxChargeTime = 1f;
    [SerializeField] private string[] chargeAnimNames;
    [SerializeField] private GameObject weakEffectObject;
    [SerializeField] private GameObject strongEffectObject;

    bool applyBurn = false;
    bool isCharging = false;
    float chargeDuration = 0f;
    int chargeTier = 0;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        if (cancel)
        {
            isCharging = false;
            chargeDuration = 0;
            return;
        }
        if (upgrades.Contains(UpgradeType.Fire_ChargeMelee))
        {
            isCharging = true;
            chargeDuration = 0;
        }
    }

    private IEnumerator Swing(Transform player)
    {
        chargeTier = (int)(chargeDuration / maxChargeTime * (chargeAnimNames.Length - 1));
        if (chargeTier == chargeAnimNames.Length - 1)  // Max tier charge
        {
            applyBurn = true;
        }

        GameObject weaponHitbox;
        if (chargeTier == chargeAnimNames.Length - 1)
        {
            weaponHitbox = Instantiate(strongCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        }
        else
            weaponHitbox = Instantiate(weaponCollider, player.position + player.up * distanceFromPlayer, Quaternion.identity);
        weaponHitbox.transform.up = player.up;

        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();

        GameObject effect = null;
        if (weakEffectObject != null && strongEffectObject != null)
        {
            if (chargeTier == chargeAnimNames.Length - 1)
            {
                effect = Instantiate(strongEffectObject, player.position + player.up * distanceFromPlayer, Quaternion.identity);
            } 
            else
            {
                effect = Instantiate(weakEffectObject, player.position + player.up * distanceFromPlayer, Quaternion.identity);
            }
            
            effect.transform.up = player.up;
            effect.transform.localScale = Vector3.one * (1 + 0.2f * chargeTier);
            weaponHitbox.transform.localScale = Vector3.one * (1 + 0.2f * chargeTier);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            anim.Play(chargeAnimNames[chargeTier]);
        }

        float elapsedTime = 0f;
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
        applyBurn = false;
    }

    public void MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {
        transform.position = player.position + player.up * distanceFromPlayer;
        transform.up = player.up;
        isCharging = false;
        StartCoroutine(Swing(player));
        chargeDuration = 0;
    }
    private void Update()
    {
        if (isCharging && chargeDuration < maxChargeTime)
        {
            chargeDuration += Time.deltaTime;
            if (chargeDuration >= maxChargeTime)
            {
                // Play effect 
            }
        }
    }

    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.transform.localEulerAngles = Vector3.zero;
        sword.ToggleSwingTrail(false);
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.ToggleSwingTrail(true);
    }

    public void OnMeleeHit(Transform player, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Fire, GameManager.Instance.currentDamage * (1f + 0.2f * chargeTier)));
        if (applyBurn)
        {
            GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Burn, 3);
        }
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        SpriteRenderer sprite = sword.sprite;

        sword.ToggleSwingTrail(true);
        sprite.color = Color.red;
        sprite.transform.localEulerAngles += spinSpeed * Time.deltaTime * Vector3.forward;
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Fire, GameManager.Instance.currentDamage * GameManager.Instance.rangedMultiplier));
        if (upgrades.Contains(UpgradeType.Fire_RangedBurn))
        {
            GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Burn, 3);
        }
    }
}
