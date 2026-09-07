using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Earth is the ballista turret: immovable firepower traded against mobility. It has <b>no tap attack</b>
/// — the charge is the entire weapon, and it runs in two phases:
/// <list type="number">
/// <item><b>Construction</b> — pressing roots the player and starts building the ballista. Releasing
/// here fires nothing; the turret never finished.</item>
/// <item><b>Charging the shot</b> — once built, damage ramps for as long as the player holds, uncapped.
/// The movement stick aims throughout.</item>
/// </list>
/// <para>
/// A weak tap shot was tried and cut: it competed with the charge for the same job at range, so the
/// strong play was to spam it and never stand still, which is the opposite of the fantasy. The
/// construction phase is what makes the commitment real — you pay half a second before you have a weapon.
/// </para>
/// <para>
/// The shot itself is a fast, heavy rock that bursts on impact: all of its damage is the blast, so it
/// rewards a committed shot into a group rather than precision against one target.
/// <para>
/// Deliberately absent: the ballista actor itself, so the construction phase currently has no visual.
/// </para>
/// </para>
/// <para>
/// Note what this class does <em>not</em> implement: <c>OnMeleeHit</c> is left to the interface default
/// because Earth never spawns a melee hitbox, so it could never fire.
/// </para>
/// </summary>
public class EarthWeapon : MonoBehaviour, IElementalWeapon, IMeleeChargeProvider, IAimLockProvider
{
    [Header("Charge — phase 1: construction")]
    [Tooltip("Seconds the ballista takes to build before the shot starts charging. Releasing during " +
             "this window fires nothing — the turret never finished.")]
    [SerializeField] private float constructionTime = 0.5f;
    [Tooltip("Shown while the ballista is being built, so the dead half-second reads as deliberate.")]
    [SerializeField] private string buildingLabel = "BUILDING TURRET";
    [Tooltip("Shown once the ballista is up and the shot is charging.")]
    [SerializeField] private string chargingLabel = "CHARGING TURRET";

    [Header("Charge — phase 2: the shot")]
    [Tooltip("Seconds of SHOT charge (after construction) at which the charge INDICATOR reads full. " +
             "Damage keeps climbing past this — there is no damage cap.")]
    [SerializeField] private float maxChargeTime = 0.8f;
    [Tooltip("Extra damage per second of shot charge, as a multiple of base damage. Unbounded: a longer " +
             "hold always hits harder, so the only limit is how long you dare stand still.")]
    [SerializeField] private float chargeDamagePerSecond = 1f;

    [Header("Rock")]
    [FormerlySerializedAs("boltPrefab")]
    [SerializeField] private GameObject rockPrefab;
    [Tooltip("Fast on purpose — a boulder loosed from a ballista, not a lobbed stone. The shot should " +
             "land about when you release it, so the aim you committed to is the aim that connects.")]
    [FormerlySerializedAs("boltSpeed")]
    [SerializeField] private float rockSpeed = 26f;
    [FormerlySerializedAs("boltDamageMultiplier")]
    [SerializeField] private float rockDamageMultiplier = 1.3f;
    [Tooltip("How far in front of the player the rock spawns, so it clears their own collider.")]
    [FormerlySerializedAs("boltSpawnOffset")]
    [SerializeField] private float rockSpawnOffset = 0.7f;
    [Tooltip("Diameter of the rock at zero charge. Set here rather than on the prefab because it grows " +
             "with the charge, so the prefab's own scale would only ever be overwritten.")]
    [SerializeField] private float rockScale = 0.55f;
    [Tooltip("Extra diameter per second of charge.")]
    [SerializeField] private float rockScalePerSecond = 0.35f;
    [SerializeField] private float maxRockScale = 1.6f;

    [Header("Impact Blast")]
    [Tooltip("Blast radius at zero charge. All the rock's damage comes from this, not from the direct hit.")]
    [SerializeField] private float blastRadius = 1.5f;
    [Tooltip("Extra blast radius per second of charge.")]
    [SerializeField] private float blastRadiusPerSecond = 0.7f;
    [Tooltip("Ceiling on the blast. Damage keeps climbing past this — only the area stops growing, so a " +
             "very long hold is a harder hit rather than an ever-wider one.")]
    [SerializeField] private float maxBlastRadius = 4f;

    [Header("Combat")]
    [SerializeField] private float meleeCooldown = 0.5f;
    [Tooltip("How far the aim snaps onto an enemy. Far past the melee elements, because the rock " +
             "outranges them and a pointer that stops short would aim Earth at nothing. Manual aim " +
             "with the movement stick overrides this while rooted.")]
    [SerializeField] private float autoAimRadius = 15f;

    [Header("Cleave")]
    [SerializeField] private GameObject cleaveEffectObject;
    [SerializeField] private float cleaveRadius = 2.5f;
    [SerializeField] private float cleaveDuration = 0.4f;

    private bool isCharging;
    private float holdDuration;

    /// <summary>Phase 1: the ballista is still being built, and releasing now fires nothing.</summary>
    public bool IsConstructing => isCharging && holdDuration < constructionTime;

    /// <summary>Phase 2 elapsed. Zero until construction finishes, so build time is never free damage.</summary>
    private float ChargeDuration => Mathf.Max(0f, holdDuration - constructionTime);

    // ---- IMeleeChargeProvider: the charge indicators come for free once these report honestly ----

    // Reports the SHOT charge only, so the indicator stays empty through construction rather than
    // implying the player is already banking damage.
    public bool IsCharging => isCharging;

    public float ChargeProgress =>
        isCharging && maxChargeTime > 0f ? Mathf.Clamp01(ChargeDuration / maxChargeTime) : 0f;

    public bool IsMaxCharge =>
        isCharging && maxChargeTime > 0f && ChargeDuration >= maxChargeTime;

    // No upgrade gate, unlike Fire's Fire_ChargeMelee: rooting to aim IS Earth's identity, not a purchase.
    public bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player) =>
        player.IsMeleeReady;

    // Overrides the shared default — Earth is the first element that outranges a sword swing.
    public float AutoAimRadius => autoAimRadius;

    // The two phases are indistinguishable on screen, and releasing in the wrong one fires nothing. Until
    // the ballista actor exists to show the difference, say it in words.
    public string ChargePhaseLabel =>
        !isCharging ? string.Empty
        : IsConstructing ? buildingLabel
        : chargingLabel;

    // ---- IAimLockProvider ----

    public bool IsAimLocked => isCharging;

    // Earth has no tap attack, so there is nothing to disambiguate a press from — waiting out the
    // tap/hold split would just be latency before the root and the ballista start.
    public bool ChargesOnPress => true;

    /// <summary>
    /// Begins the root. The player stops moving and their movement stick becomes aim until release or cancel.
    /// </summary>
    /// <remarks>
    /// Fires on the press itself, so the root and the ballista's construction start the instant the
    /// player commits rather than a beat later.
    /// </remarks>
    public void OnCharge(Transform player, HashSet<UpgradeType> upgrades, bool cancel = false)
    {
        if (cancel)
        {
            ResetCharge();
            return;
        }

        if (isCharging)
        {
            return;
        }

        isCharging = true;
        holdDuration = 0f;
    }

    /// <summary>Clears the root whenever the imbue ends, in case something skipped the cancel path.</summary>
    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        ResetCharge();
    }

    private void Update()
    {
        // Deliberately unclamped — damage ramps for as long as the player holds. maxChargeTime only
        // saturates the indicator; it is not a cap.
        if (isCharging)
        {
            holdDuration += Time.deltaTime;
        }
    }

    private void ResetCharge()
    {
        isCharging = false;
        holdDuration = 0f;
    }

    /// <summary>
    /// Fires the charged rock along the aim and returns the cooldown. Does nothing if there was no charge.
    /// </summary>
    /// <remarks>
    /// PlayerController.ReleaseChargeAttack dispatches a charge release to OnTap, so this one entry point
    /// serves both — and <c>isCharging</c> is what tells a real release from a bare press.
    /// <para>
    /// It aims by the weapon indicator like every other element, so it inherits the same auto-aim onto a
    /// nearby enemy. What it deliberately does NOT do is <em>step toward</em> that enemy the way the melee
    /// elements do — a turret that walks itself into range would undercut the whole fantasy.
    /// </para>
    /// <para>
    /// Damage ramps with hold time and is <b>not capped</b>: the only limit on a shot is how long the
    /// player dares stand rooted for it. Size and blast radius ramp too, but those <em>are</em> capped, so
    /// a very long hold reads as a harder hit rather than an ever-widening one.
    /// </para>
    /// </remarks>
    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        // Released before the ballista finished: nothing to shoot from, so nothing fires. Costs no
        // cooldown, making a mistimed press a wasted half-second rather than a punishment.
        if (IsConstructing)
        {
            AudioSystem.Play(AudioSystem.Sound.Bounce);
            ResetCharge();
            return 0f;
        }

        // Earth has no tap attack, so a press that never became a charge does nothing at all.
        if (!isCharging)
        {
            return 0f;
        }

        float charge = ChargeDuration;
        float damageMultiplier = rockDamageMultiplier + charge * chargeDamagePerSecond;
        float scale = Mathf.Min(rockScale + charge * rockScalePerSecond, maxRockScale);
        float blast = Mathf.Min(blastRadius + charge * blastRadiusPerSecond, maxBlastRadius);

        // Drop the root BEFORE firing, so nothing below can leave the player stuck.
        ResetCharge();

        // player.up is the facing PlayerController just synced from the weapon indicator — the same
        // source every other element aims by, so the shot goes where the pointer and the aim line say.
        LaunchRock(player, player.up, damageMultiplier, scale, blast);
        return meleeCooldown;
    }

    private void LaunchRock(Transform player, Vector2 direction, float damageMultiplier, float scale, float blast)
    {
        if (rockPrefab == null)
        {
            return;
        }

        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;

        // Clear the player's own collider by the rock's own radius, so a bigger rock doesn't spawn inside them.
        Vector3 spawnPos = player.position + (Vector3)(direction * (rockSpawnOffset + scale * 0.5f));

        GameObject obj = PrefabPool.Instance!.Spawn(rockPrefab, spawnPos, Quaternion.identity);

        // Uniform, and set per shot rather than on the prefab: a rock is round, and its size is charge.
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        PlayerProjectile rock = obj.GetComponent<PlayerProjectile>();
        if (rock == null)
        {
            PrefabPool.Instance!.Release(obj);
            return;
        }

        float damage = GameManager.Instance.GetEffectiveBaseDamage() * damageMultiplier;

        // No EnableHoming: the rock flies dead straight. Homing would make aiming pointless, and aiming is
        // the mechanic the rest of Earth is built on.
        rock.Launch(Element.Earth, damage, direction, rockSpeed);

        // All of the rock's damage lands as the blast, so a near miss still hits — the reward for a long
        // hold is a wider, harder impact rather than a thinner shot that has to be aimed perfectly.
        rock.EnableExplosion(blast);
        AudioSystem.Play(AudioSystem.Sound.Slash_Basic);
    }

    public void Cleave(Transform player, HashSet<UpgradeType> upgrades)
    {
        StartCoroutine(PlayCleave(player));
    }

    private IEnumerator PlayCleave(Transform player)
    {
        MeleeAugmentUtility.DamageEnemiesInRadius(player.position, MeleeAugmentUtility.ScaleSeekRadius(cleaveRadius), enemy =>
        {
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Earth, GameManager.Instance.GetEffectiveBaseDamage()),
                new MoveType(Element.Earth, AttackKind.MeleeStrike));
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

    // Ranged hooks are dormant while the sword throw is the ultimate, but they're implemented rather than
    // defaulted so Earth stays consistent with the other five elements if the throw comes back.
    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = ElementVisuals.GetColor(Element.Earth);
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Earth, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Earth, AttackKind.Ranged));
    }
}
