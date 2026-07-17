using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class WindWeapon : MonoBehaviour, IElementalWeapon
{
    /*
     * Melee hits accumulate wind charges (like ult charge generation, 1 per enemy hit).
     * Ranged throws consume all charges to empower the sword: partial charges summon wind wisps
     * that periodically damage an AoE around the sword's trajectory (no direct contact damage),
     * while max charges summon a tornado that also pulls enemies toward its center and destroys
     * enemy projectiles that enter it. Slash/cleave visuals reuse the Physical weapon's for now.
     */

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

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;

    [Header("Wind Charges")]
    [SerializeField] private int maxWindCharges = 5;

    [Header("Ranged - Wisp Empower (partial charges)")]
    [SerializeField] private GameObject wispEffectPrefab;
    [SerializeField] private float wispBaseRadius = 1.5f;
    [SerializeField] private float wispRadiusPerCharge = 0.4f;
    [SerializeField] private float wispBaseDamageMultiplier = 0.3f;
    [SerializeField] private float wispDamageMultiplierPerCharge = 0.15f;
    [SerializeField] private float wispTickInterval = 0.4f;
    [SerializeField] private float wispBaseDuration = 1.5f;
    [SerializeField] private float wispDurationPerCharge = 0.5f;

    [Header("Ranged - Tornado Empower (max charges)")]
    [SerializeField] private GameObject tornadoEffectPrefab;
    [SerializeField] private float tornadoRadius = 4f;
    [SerializeField] private float tornadoDamageMultiplier = 1.2f;
    [SerializeField] private float tornadoTickInterval = 0.35f;
    [SerializeField] private float tornadoDuration = 4f;
    [SerializeField] private float tornadoPullForce = 4.5f;

    [Header("Rending Gale (Buffetted debuff)")]
    [SerializeField] private int buffettedDuration = 3;

    private int windCharges = 0;
    private int _lastFlightFrame = -1;

    public void MeleeCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        return; // Wind does not have hold-to-charge melee
    }

    private IEnumerator Swing(Transform player)
    {
        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration);
        Vector3 spawnPos = player.position + player.up * reach;

        GameObject weaponHitbox = PrefabPool.Instance!.Spawn(weaponCollider, spawnPos, Quaternion.identity);
        Animator anim = weaponHitbox.GetComponentInChildren<Animator>();
        weaponHitbox.transform.up = player.up;
        MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);

        float elapsedTime = 0f;

        GameObject effect = null;
        if (effectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(effectObject, spawnPos, Quaternion.identity, player);
            effect.transform.up = player.up;
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            IAttackAnimator attackAnimator = effect.GetComponent<IAttackAnimator>();
            attackAnimator.PlayAnimation();
            AudioSystem.Play(AudioSystem.Sound.Slash_Basic);
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

    public void Strike(Transform player)
    {
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
        windCharges = 0;
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        windCharges = 0;
    }

    public void Cleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        StartCoroutine(PlayCleave(player));
    }

    private IEnumerator PlayCleave(Transform player)
    {
        MeleeAugmentUtility.DamageEnemiesInRadius(player.position, MeleeAugmentUtility.ScaleSeekRadius(cleaveRadius), enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Wind, GameManager.Instance.GetEffectiveBaseDamage()),
                new MoveType(Element.Wind, AttackKind.MeleeStrike));
        });

        AudioSystem.Play(AudioSystem.Sound.Slash_Basic);

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
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Wind, GameManager.Instance.GetEffectiveBaseDamage()),
            new MoveType(Element.Wind, AttackKind.MeleeStrike));

        if (upgrades.Contains(UpgradeType.Wind_Windstorm))
        {
            windCharges = Mathf.Min(windCharges + 1, maxWindCharges);
        }
    }

    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        // OnRangedFlight only fires on frames the sword is actually flying, so a gap in
        // Time.frameCount since the last call means a brand new throw just started.
        bool isNewThrow = Time.frameCount != _lastFlightFrame + 1;
        _lastFlightFrame = Time.frameCount;

        if (isNewThrow && upgrades.Contains(UpgradeType.Wind_Windstorm))
        {
            BeginEmpoweredThrow(sword);
        }

        sword.sprite.color = ElementVisuals.GetColor(Element.Wind);
    }

    private void BeginEmpoweredThrow(SwordProjectile sword)
    {
        int chargesUsed = windCharges;
        windCharges = 0;

        if (chargesUsed <= 0)
        {
            return;
        }

        if (chargesUsed >= maxWindCharges)
        {
            SpawnTornado(sword);
        }
        else
        {
            SpawnWisps(sword, chargesUsed);
        }
    }

    private void SpawnWisps(SwordProjectile sword, int chargesUsed)
    {
        if (wispEffectPrefab == null)
        {
            return;
        }

        // Spawned unparented (not under sword.transform): a Collider2D nested under the sword's
        // Rigidbody2D becomes part of its compound physics body, which was sticking the thrown
        // sword to terrain the wisp/tornado radius merely swept over. WindWispEffect instead
        // tracks the sword's position manually each frame.
        GameObject obj = PrefabPool.Instance!.Spawn(wispEffectPrefab, sword.transform.position, Quaternion.identity);
        WindWispEffect wisp = obj.GetComponent<WindWispEffect>();
        if (wisp == null)
        {
            return;
        }

        float radius = wispBaseRadius + wispRadiusPerCharge * chargesUsed;
        float damagePerTick = GameManager.Instance.GetEffectiveBaseDamage() * (wispBaseDamageMultiplier + wispDamageMultiplierPerCharge * chargesUsed);
        float duration = wispBaseDuration + wispDurationPerCharge * chargesUsed;

        wisp.Begin(sword.transform, radius, damagePerTick, wispTickInterval, duration);
    }

    private void SpawnTornado(SwordProjectile sword)
    {
        if (tornadoEffectPrefab == null)
        {
            return;
        }

        // Spawned unparented — see SpawnWisps for why this can't hang off sword.transform.
        GameObject obj = PrefabPool.Instance!.Spawn(tornadoEffectPrefab, sword.transform.position, Quaternion.identity);
        WindTornadoEffect tornado = obj.GetComponent<WindTornadoEffect>();
        if (tornado == null)
        {
            return;
        }

        float damagePerTick = GameManager.Instance.GetEffectiveBaseDamage() * tornadoDamageMultiplier;

        tornado.Begin(sword.transform, tornadoRadius, damagePerTick, tornadoTickInterval, tornadoDuration, tornadoPullForce);
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Wind, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Wind, AttackKind.Ranged));

        if (upgrades.Contains(UpgradeType.Wind_RendingGale))
        {
            GameManager.Instance.AddEffect(enemy, GameManager.EnemyEffect.Buffetted, buffettedDuration);
        }
    }
}
