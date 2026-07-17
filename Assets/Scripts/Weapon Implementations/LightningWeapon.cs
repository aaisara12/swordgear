using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class LightningWeapon : MonoBehaviour, IElementalWeapon
{
    [SerializeField] private GameObject weaponCollider;  // TODO: Add separate, larger collider for weapon thrust 
    [SerializeField] private GameObject strongCollider;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private string slashAnimName;
    [SerializeField] private string thrustAnimName;
    [SerializeField] private GameObject weakEffectObject;
    [SerializeField] private GameObject strongEffectObject;

    [SerializeField] GameObject lightningPrefab;

    [Header("Combat")]
    [SerializeField] private float attackRadius = ActiveEnemyRegistry.AutoTargetRadius;
    [SerializeField] private float dashFactor = 0.2f;
    [SerializeField] private float thrustDistance = 1.5f;
    [SerializeField] private float meleeCooldown = 0.3f;

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;

    int combo = 0;
    bool lightningActive = false;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {}

    private IEnumerator Swing(Transform player)
    {
        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        Vector3 spawnPos = player.position + player.up * reach;

        GameObject weaponHitbox = PrefabPool.Instance!.Spawn(weaponCollider, spawnPos, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();
        weaponHitbox.transform.up = player.up;

        float elapsedTime = 0f;

        GameObject effect = null;
        if (weakEffectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(weakEffectObject, spawnPos, Quaternion.identity, player);
            effect.transform.up = player.up;
            if (combo > 0)
            {
                effect.transform.localScale += Vector3.left * 2; // x = -1 scale
            }
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);
            anim.Play(slashAnimName);  // Old implementation
        }

        AudioSystem.Play(AudioSystem.Sound.Slash_LightningBasic);

        while (elapsedTime < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        PrefabPool.Instance!.Release(weaponHitbox);
        if (effect != null)
        {
            PrefabPool.Instance!.Release(effect);
        }
    }

    IEnumerator Thrust(Transform player)
    {
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        float thrustReach = MeleeAugmentUtility.ScaleDistance(thrustDistance);

        GameObject weaponHitbox = PrefabPool.Instance!.Spawn(strongCollider, player.position, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();
        weaponHitbox.transform.up = player.up;
        MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);

        float elapsedTime = 0f;
        lightningActive = true;

        Vector2 startPos = player.position;
        Vector2 dest = player.position + player.up * thrustReach;

        GameObject effect = null;
        if (strongEffectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(strongEffectObject, player.position, Quaternion.identity, player);
            effect.transform.up = player.up;
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            anim.Play(thrustAnimName);
        }

        AudioSystem.Play(AudioSystem.Sound.Slash_LightningEmpowered);

        while (elapsedTime < duration)
        {
            Vector2 pos = Vector2.Lerp(startPos, dest, elapsedTime * 8 / duration);
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            player.position = pos;
            weaponHitbox.transform.position = player.position;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        PrefabPool.Instance!.Release(weaponHitbox);
        if (effect != null)
        {
            PrefabPool.Instance!.Release(effect);
        }
        lightningActive = false;
    }

    public void Strike(Transform player, HashSet<UpgradeType> upgrades)
    {
        //transform.position = player.position + player.up * distanceFromPlayer;
        //transform.up = player.up;

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


    public float MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {
        float seekRadius = MeleeAugmentUtility.ScaleSeekRadius(attackRadius);
        if (!ActiveEnemyRegistry.TryGetNearest(player.position, seekRadius, out EnemyController nearestEnemy, out float shortestDistance))
        {
            Strike(player, upgrades);
            return meleeCooldown;
        }

        Vector2 direction = ((Vector2)nearestEnemy.transform.position - (Vector2)player.position).normalized;
        player.up = direction;

        Vector2 dashPosition = (Vector2)player.position + direction * (shortestDistance * dashFactor);
        player.position = dashPosition;
        Strike(player, upgrades);
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

    // Thunderstep: while Lightning is imbued (this weapon only runs when it's active), dashing blinks the
    // player straight to the thrown sword and catches it — firing the cleave + returning the sword.
    public bool TryOverrideDash(PlayerController player, HashSet<UpgradeType> upgrades)
    {
        if (player == null || !upgrades.Contains(UpgradeType.Lightning_Thunderstep))
        {
            return false;
        }

        SwordProjectile sword = SwordProjectile.Instance;
        if (sword == null || !sword.gameObject.activeSelf || sword.IsRecalling)
        {
            return false;
        }

        player.BlinkTo(sword.transform.position);   // teleport + dash cooldown + i-frames + blink ghost
        player.CatchThrownSword();                  // -> cleave + pickup
        return true;
    }

    private IEnumerator PlayCleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        bool applyCleaveStatic = upgrades.Contains(UpgradeType.Lightning_ApplyStatic);
        MeleeAugmentUtility.DamageEnemiesInRadius(player.position, MeleeAugmentUtility.ScaleSeekRadius(cleaveRadius), enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Lightning, GameManager.Instance.GetEffectiveBaseDamage()),
                new MoveType(Element.Lightning, AttackKind.MeleeStrike));

            if (applyCleaveStatic)
            {
                ChainLightningProjectile lightning = PrefabPool.Instance!.Spawn(lightningPrefab, enemy.transform.position, Quaternion.identity).GetComponent<ChainLightningProjectile>();
                lightning.Initialize(transform);
            }
        });

        AudioSystem.Play(AudioSystem.Sound.Slash_LightningBasic);

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
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Lightning, GameManager.Instance.GetEffectiveBaseDamage()),
            new MoveType(Element.Lightning, AttackKind.MeleeStrike));
        if (lightningActive && upgrades.Contains(UpgradeType.Lightning_ApplyStatic))
        {
            ChainLightningProjectile lightning = PrefabPool.Instance!.Spawn(lightningPrefab, enemy.transform.position, Quaternion.identity).GetComponent<ChainLightningProjectile>();
            lightning.Initialize(transform);
        }
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = ElementVisuals.GetColor(Element.Lightning); // was Color.cyan (Ice's colour) — a thrown Lightning sword looked like Ice
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Lightning, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Lightning, AttackKind.Ranged));
        if (upgrades.Contains(UpgradeType.Lightning_ApplyStatic))
        {
            ChainLightningProjectile lightning = PrefabPool.Instance!.Spawn(lightningPrefab, enemy.transform.position, Quaternion.identity).GetComponent<ChainLightningProjectile>();
            lightning.Initialize(transform);
        }
    }
}
