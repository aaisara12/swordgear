using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class FireWeapon : MonoBehaviour, IElementalWeapon, IMeleeChargeProvider
{
    [SerializeField] private GameObject weaponCollider;
    [SerializeField] private GameObject strongCollider;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] float spinSpeed = 50f;
    [SerializeField] private float maxChargeTime = 1f;
    [SerializeField] private float meleeCooldown = 0.3f;
    [SerializeField] private string[] chargeAnimNames;
    [SerializeField] private GameObject weakEffectObject;
    [SerializeField] private GameObject strongEffectObject;

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;
    [SerializeField] private int cleaveBurnDuration = 3;

    bool applyBurn = false;
    bool isCharging = false;
    float chargeDuration = 0f;
    int chargeTier = 0;

    public bool IsCharging => isCharging;

    public float ChargeProgress =>
        isCharging && maxChargeTime > 0f ? Mathf.Clamp01(chargeDuration / maxChargeTime) : 0f;

    public bool IsMaxCharge =>
        isCharging && maxChargeTime > 0f && chargeDuration >= maxChargeTime;

    public bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player) =>
        player.IsMeleeReady && upgrades.Contains(UpgradeType.Fire_ChargeMelee);

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        if (cancel)
        {
            ResetCharge();
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

        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        Vector3 spawnPos = player.position + player.up * reach;

        GameObject weaponHitbox;
        if (chargeTier == chargeAnimNames.Length - 1)
        {
            weaponHitbox = PrefabPool.Instance!.Spawn(strongCollider, spawnPos, Quaternion.identity);
            SpawnBombCascade(player);
        }
        else
            weaponHitbox = PrefabPool.Instance!.Spawn(weaponCollider, spawnPos, Quaternion.identity);
        weaponHitbox.transform.up = player.up;

        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();

        GameObject effect = null;
        if (weakEffectObject != null && strongEffectObject != null)
        {
            if (chargeTier == chargeAnimNames.Length - 1)
            {
                effect = PrefabPool.Instance!.Spawn(strongEffectObject, spawnPos, Quaternion.identity);
                AudioSystem.Play(AudioSystem.Sound.Slash_FireCharged);
            } 
            else
            {
                effect = PrefabPool.Instance!.Spawn(weakEffectObject, spawnPos, Quaternion.identity);
                AudioSystem.Play(AudioSystem.Sound.Slash_FireBasic);
            }
            
            effect.transform.up = player.up;
            effect.transform.localScale = Vector3.one * (1 + 0.2f * chargeTier);
            weaponHitbox.transform.localScale = Vector3.one * (1 + 0.2f * chargeTier);
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
        }
        else
        {
            MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);
            anim.Play(chargeAnimNames[chargeTier]);
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        PrefabPool.Instance!.Release(weaponHitbox);
        if (effect != null)
        {
            PrefabPool.Instance!.Release(effect);
        }
        applyBurn = false;
    }

    [Header("Bomb Cascade")]
    [SerializeField] private GameObject bombObject;
    [SerializeField] private int waves = 3;
    [SerializeField] private int bombsPerWave = 5;
    [SerializeField] private float fanAngle = 60f;          // total arc angle
    [SerializeField] private float startDistance = 1.5f;    // first wave distance
    [SerializeField] private float waveDistanceStep = 1.2f; // how much farther each wave is
    [SerializeField] private float waveDelay = 0.15f;

    void SpawnBombCascade(Transform player)
    {
        StartCoroutine(SpawnBombCascadeRoutine(player));
    }

    private IEnumerator SpawnBombCascadeRoutine(Transform player)
    {
        Vector2 forward = player.up;

        for (int wave = 0; wave < waves; wave++)
        {
            float distance = startDistance + wave * waveDistanceStep;

            float angleStep = bombsPerWave > 1
                ? fanAngle / (bombsPerWave - 1)
                : 0f;

            float startAngle = -fanAngle * 0.5f;

            for (int i = 0; i < bombsPerWave; i++)
            {
                float angle = startAngle + angleStep * i;

                // Rotate forward vector around Z
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * forward;
                Vector2 spawnPos = (Vector2)player.position + dir.normalized * distance;

                var bomb = PrefabPool.Instance!.Spawn(
                    bombObject,
                    spawnPos,
                    Quaternion.identity
                );
                foreach (var ps in bomb.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Play(true);
                var pooled = bomb.GetComponent<PooledInstance>();
                if (pooled != null)
                    pooled.ReleaseWhenParticlesDone();
            }
            AudioSystem.Play(AudioSystem.Sound.Slash_FireEruption);
            yield return new WaitForSeconds(waveDelay);
        }
    }

    public float MeleeStrike(Transform player, HashSet<UpgradeType> upgrades)
    {
        transform.position = MeleeAugmentUtility.ForwardOffset(player, distanceFromPlayer);
        transform.up = player.up;
        StartCoroutine(Swing(player));
        ResetCharge();
        return meleeCooldown;
    }

    private void ResetCharge()
    {
        isCharging = false;
        chargeDuration = 0;
    }

    private void Update()
    {
        if (isCharging && chargeDuration < maxChargeTime)
        {
            chargeDuration += Time.deltaTime;
            if (chargeDuration >= maxChargeTime)
            {
                chargeDuration = maxChargeTime;
            }
        }
    }

    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        ResetCharge();
        sword.sprite.transform.localEulerAngles = Vector3.zero;
        sword.ToggleSwingTrail(false);
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.ToggleSwingTrail(true);
    }

    public void Cleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        StartCoroutine(PlayCleave(player, upgrades));
    }

    private IEnumerator PlayCleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        bool applyCleaveBurn = upgrades.Contains(UpgradeType.Fire_RangedBurn);
        MeleeAugmentUtility.DamageEnemiesInRadius(player.position, MeleeAugmentUtility.ScaleSeekRadius(cleaveRadius), enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Fire, GameManager.Instance.GetEffectiveBaseDamage()),
                new MoveType(Element.Fire, AttackKind.MeleeStrike));
            if (applyCleaveBurn)
            {
                GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Burn, cleaveBurnDuration);
            }
        });

        AudioSystem.Play(AudioSystem.Sound.Slash_FireBasic);

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
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        AttackKind kind = chargeTier > 0 ? AttackKind.MeleeCharge : AttackKind.MeleeStrike;
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Fire, GameManager.Instance.GetEffectiveBaseDamage() * (1f + 0.2f * chargeTier)),
            new MoveType(Element.Fire, kind));
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
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Fire, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Fire, AttackKind.Ranged));
        if (upgrades.Contains(UpgradeType.Fire_RangedBurn))
        {
            GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Burn, 3);
        }
    }
}
