using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Lightning is the katana. Holding attack sheathes the blade; releasing it fires an iaido dash-slash that
/// carries the player forward with i-frames. Dashing through an enemy attack during that window empowers the
/// blade, unlocking a faster slash combo for a short duration.
/// </summary>
public class LightningWeapon : MonoBehaviour, IElementalWeapon, IMeleeChargeProvider
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

    [Header("Katana - Iaido Dash")]
    [Tooltip("How far the dash-slash carries the player on release.")]
    [SerializeField] private float iaidoDashDistance = 4.5f;
    [SerializeField] private float iaidoDashDuration = 0.18f;
    [SerializeField] private float iaidoCooldown = 0.45f;
    [SerializeField] private float iaidoDamageMultiplier = 1.5f;
    [Tooltip("I-frames granted for the dash, so dodging through an attack is actually survivable.")]
    [SerializeField] private float iaidoIFrameDuration = 0.3f;

    [Header("Katana - Dodge Empower")]
    [Tooltip("Radius around the player scanned for enemy attacks to dodge through during the dash.")]
    [SerializeField] private float dodgeDetectRadius = 0.9f;
    [Tooltip("Layers holding enemy attacks. Projectiles at minimum; beams are found by component.")]
    [SerializeField] private LayerMask dodgeDetectLayers = 1 << 9; // Projectiles
    [SerializeField] private float empoweredDuration = 5f;
    [Tooltip("Attack-speed multiplier while empowered. Shortens both the swing and the cooldown.")]
    [SerializeField] private float empoweredAttackSpeedMultiplier = 2f;

    [Header("Aim")]
    [Tooltip("How far the aim snaps onto an enemy. Covers the 3.5 attack radius plus the 4.5 iaido dash used to close.")]
    [SerializeField] private float autoAimRadius = 5f;

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;

    int combo = 0;
    bool lightningActive = false;

    private bool isSheathed;
    private bool iaidoDashActive;
    private float empoweredUntil = -1f;
    private readonly Collider2D[] _dodgeScanBuffer = new Collider2D[12];

    public bool IsEmpowered => Time.time < empoweredUntil;

    // IMeleeChargeProvider — the sheathe is binary rather than a ramp, so it reports a full ring the moment
    // it engages. That gives the existing charge indicators a "blade is sheathed, release to strike" tell.
    public bool IsCharging => isSheathed;
    public float ChargeProgress => isSheathed ? 1f : 0f;
    public bool IsMaxCharge => isSheathed;
    public bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player) => player.IsMeleeReady;

    public void OnCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        SetSheathed(player, !cancel);
    }

    private void SetSheathed(Transform player, bool sheathed)
    {
        if (isSheathed == sheathed)
        {
            return;
        }

        isSheathed = sheathed;
        player.GetComponent<PlayerController>()?.SetWeaponVisible(!sheathed);
    }

    private IEnumerator Swing(Transform player, float speedMultiplier = 1f)
    {
        float reach = MeleeAugmentUtility.ScaleDistance(distanceFromPlayer);
        float duration = MeleeAugmentUtility.ScaleSwingDuration(swingDuration) / Mathf.Max(0.05f, speedMultiplier);
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

    public void Strike(Transform player, HashSet<UpgradeType> upgrades, float speedMultiplier = 1f)
    {
        //transform.position = player.position + player.up * distanceFromPlayer;
        //transform.up = player.up;

        switch (combo)
        {
            case 0:
            case 1:
                StartCoroutine(Swing(player, speedMultiplier));
                break;
            case 2:

                StartCoroutine(Thrust(player));
                break;
        }
        combo = (combo + 1) % (upgrades.Contains(UpgradeType.Lightning_DashStrike) ? 3 : 2);
    }


    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        // Released from a sheathe: the iaido dash takes priority over everything else.
        if (isSheathed)
        {
            SetSheathed(player, false);
            StartCoroutine(IaidoDash(player));
            return iaidoCooldown;
        }

        if (IsEmpowered)
        {
            return EmpoweredSlash(player, upgrades);
        }

        SeekAndStepToward(player);
        Strike(player, upgrades);
        return meleeCooldown;
    }

    /// <summary>
    /// The reward for a successful dodge-dash: a faster slash combo. Deliberately kept as its own entry
    /// point so the empowered form can diverge from the normal swing later (different anim, extra hits,
    /// chain lightning, ...). For now it is the normal slash run at an increased attack speed.
    /// </summary>
    private float EmpoweredSlash(Transform player, HashSet<UpgradeType> upgrades)
    {
        float speed = Mathf.Max(0.05f, empoweredAttackSpeedMultiplier);

        SeekAndStepToward(player);
        Strike(player, upgrades, speed);

        return meleeCooldown / speed;
    }

    // Shared approach step: face the nearest enemy in range and close a fraction of the gap.
    private void SeekAndStepToward(Transform player)
    {
        float seekRadius = MeleeAugmentUtility.ScaleSeekRadius(attackRadius);
        if (!ActiveEnemyRegistry.TryGetNearest(player.position, seekRadius, out EnemyController nearestEnemy, out float shortestDistance))
        {
            return;
        }

        Vector2 direction = ((Vector2)nearestEnemy.transform.position - (Vector2)player.position).normalized;
        player.up = direction;
        player.position = (Vector2)player.position + direction * (shortestDistance * dashFactor);
    }

    /// <summary>
    /// Iaido: carry the player forward behind a persistent hitbox, with i-frames for the whole ride. Each
    /// frame we look for an enemy attack overlapping the player — passing through one empowers the blade.
    /// </summary>
    private IEnumerator IaidoDash(Transform player)
    {
        float duration = MeleeAugmentUtility.ScaleSwingDuration(iaidoDashDuration);
        float distance = MeleeAugmentUtility.ScaleDistance(iaidoDashDistance);

        PlayerController controller = player.GetComponent<PlayerController>();
        controller?.GrantIFrames(Mathf.Max(iaidoIFrameDuration, duration));

        GameObject weaponHitbox = PrefabPool.Instance!.Spawn(strongCollider, player.position, Quaternion.identity);
        weaponHitbox.transform.up = player.up;
        MeleeAugmentUtility.ApplyRangeScale(weaponHitbox.transform);

        GameObject effect = null;
        if (strongEffectObject != null)
        {
            effect = PrefabPool.Instance!.Spawn(strongEffectObject, player.position, Quaternion.identity, player);
            effect.transform.up = player.up;
            MeleeAugmentUtility.ApplyRangeScale(effect.transform);
            effect.GetComponent<IAttackAnimator>()?.PlayAnimation();
        }

        AudioSystem.Play(AudioSystem.Sound.Slash_LightningEmpowered);

        iaidoDashActive = true;
        lightningActive = true;

        Vector2 startPos = player.position;
        Vector2 dest = startPos + (Vector2)player.up * distance;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            player.position = Vector2.Lerp(startPos, dest, t);
            weaponHitbox.transform.position = player.position;

            if (!IsEmpowered && HasDodgedEnemyAttack(player.position))
            {
                GrantEmpowered();
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.position = dest;

        iaidoDashActive = false;
        lightningActive = false;

        PrefabPool.Instance!.Release(weaponHitbox);
        if (effect != null)
        {
            PrefabPool.Instance!.Release(effect);
        }
    }

    /// <summary>
    /// True when an active enemy attack overlaps the given point. Covers enemy projectiles (by layer) and
    /// beam lasers (by component); enemy melee has no discrete hitbox to dodge, so it isn't detected.
    /// </summary>
    private bool HasDodgedEnemyAttack(Vector2 position)
    {
        int count = Physics2D.OverlapCircleNonAlloc(position, dodgeDetectRadius, _dodgeScanBuffer, dodgeDetectLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _dodgeScanBuffer[i];
            if (hit == null) continue;

            if (hit.GetComponentInParent<EnemyProjectile>() != null || hit.GetComponentInParent<EnemyBeamLaser>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void GrantEmpowered()
    {
        empoweredUntil = Time.time + empoweredDuration;
        AudioSystem.Play(AudioSystem.Sound.Slash_LightningEmpowered);
        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake();
    }

    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        // Swapping off Lightning must never leave the blade hidden or the empower running.
        SetSheathed(player, false);
        empoweredUntil = -1f;
    }

    public void OnBuffStart(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        isSheathed = false;
        empoweredUntil = -1f;
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
        float damage = GameManager.Instance.GetEffectiveBaseDamage() * (iaidoDashActive ? iaidoDamageMultiplier : 1f);
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Lightning, damage),
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

    public float AutoAimRadius => autoAimRadius;
}
