#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Light is the harp, and its fantasy is gambling: every tap plays a random tune. You don't choose what
/// you get, only when to pull the lever.
/// </summary>
/// <remarks>
/// The weapon only rolls and reveals; what each tune does lives on its <see cref="HarpTune"/> asset. Light
/// never spawns a melee hitbox, so <c>OnMeleeHit</c> is correctly left at the interface's no-op default.
/// </remarks>
public class LightWeapon : MonoBehaviour, IElementalWeapon, IMeleeChargeProvider
{
    [Header("Tunes")]
    [Tooltip("Which tunes the harp knows this run. The roll only ever plays owned tunes.")]
    [SerializeField] private HarpRepertoire? repertoire;
    [Tooltip("The Grand Chord: plays every owned tune at once. Rolled before, and outside, the family roll.")]
    [SerializeField] private HarpTune? jackpot;
    [SerializeField, Range(0f, 1f)] private float jackpotChance = 0.03f;
    [SerializeField] private float tapCooldown = 0.45f;

    [Header("Reveal")]
    [Tooltip("Shows the rolled tune's note above the player. Its pop-in, rise and fade are authored in " +
             "the prefab's animation; this only sets the sprite and places it.")]
    [SerializeField] private GameObject? revealPrefab;
    [Tooltip("Above the streak indicator that floats over the player's head, so the two never overlap.")]
    [SerializeField] private Vector2 revealOffset = new(0f, 3.9f);
    [Tooltip("Seconds before the reveal returns to the pool. Match the reveal clip's length.")]
    [SerializeField] private float revealLifetime = 0.9f;
    [Tooltip("Colour of the \"NEW TUNE\" callout when the harp learns one.")]
    [SerializeField] private Color learnedColor = new(1f, 0.93f, 0.55f, 1f);

    [Header("Charge — Flourish")]
    [Tooltip("Seconds of hold to reach a full Flourish.")]
    [SerializeField] private float maxChargeTime = 0.9f;
    [Tooltip("Tunes strummed by a release with almost no charge.")]
    [SerializeField, Min(1)] private int minFlourishTunes = 2;
    [Tooltip("Tunes strummed by a fully charged release.")]
    [SerializeField, Min(1)] private int maxFlourishTunes = 5;
    [Tooltip("Seconds between strummed tunes.")]
    [SerializeField] private float strumInterval = 0.1f;
    [SerializeField] private float flourishCooldown = 0.9f;
    [Tooltip("Sideways spacing of a Flourish's reveals, so every note it played can be read.")]
    [SerializeField] private float flourishRevealSpacing = 1.4f;

    [Header("Aim")]
    [Tooltip("Between Fire's 10 and Earth's 15: the harp's notes travel and home, so it reaches further " +
             "than the volley elements. Keep in step with each tune's aimRadius and the note's seekRadius.")]
    [SerializeField] private float autoAimRadius = 12f;

    public float AutoAimRadius => autoAimRadius;

    private bool isCharging;
    private float chargeDuration;

    private void Awake()
    {
        if (repertoire == null)
        {
            Debug.LogError("LightWeapon: repertoire is null");
            return;
        }

        if (revealPrefab == null)
        {
            Debug.LogError("LightWeapon: revealPrefab is null");
            return;
        }
    }

    private void OnEnable()
    {
        HarpRepertoire.OnTuneLearned += AnnounceLearned;
    }

    private void OnDisable()
    {
        HarpRepertoire.OnTuneLearned -= AnnounceLearned;
    }

    /// <summary>
    /// Shows a newly learned tune's note over the player and names it, so the shop's random pick isn't
    /// a silent change: the player sees what they won.
    /// </summary>
    /// <remarks>
    /// Usually fires while the augment pick has the game paused. The reveal and callout run on scaled time,
    /// so they play as the game resumes rather than vanishing behind the shop.
    /// </remarks>
    private void AnnounceLearned(HarpTune tune)
    {
        GameManager? gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.player == null)
        {
            return;
        }

        Reveal(gameManager.player.transform, tune);
        StreakWorldIndicator.Instance?.Announce($"NEW TUNE: {tune.name.ToUpperInvariant()}", learnedColor);
    }

    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        if (repertoire == null)
        {
            return 0f;
        }

        // A charge release arrives here too (ReleaseChargeAttack dispatches to OnTap), so isCharging is what
        // tells a Flourish from a plain tap.
        if (isCharging)
        {
            int tunes = Mathf.RoundToInt(Mathf.Lerp(minFlourishTunes, maxFlourishTunes, ChargeProgress));
            ResetCharge();
            StartCoroutine(Flourish(player, upgrades, tunes));
            return flourishCooldown;
        }

        if (!PlayRolledTune(player, upgrades, 0f))
        {
            // Declined: nothing playable is configured, so don't spend a cooldown on silence.
            return 0f;
        }

        return tapCooldown;
    }

    /// <summary> Rolls one tune and plays it: reveal, melody, effect. False when nothing was playable. </summary>
    private bool PlayRolledTune(Transform player, HashSet<UpgradeType> upgrades, float revealXOffset)
    {
        if (repertoire == null)
        {
            return false;
        }

        IReadOnlyList<HarpTune?> owned = repertoire.Owned;
        HarpTune? tune = HarpTune.Roll(owned, jackpot, jackpotChance, Random.value, Random.value, Random.value);
        if (tune == null)
        {
            return false;
        }

        Reveal(player, tune, revealXOffset);
        StartCoroutine(tune.PlayMelody());
        tune.Play(new HarpContext(player, upgrades, this, owned));
        return true;
    }

    /// <summary>
    /// The charge release: strums several independent rolls in quick succession, like running a hand
    /// across the strings.
    /// </summary>
    /// <remarks>
    /// Each strum is its own full roll, jackpot included, so a Flourish is several pulls of the lever
    /// for the price of holding still on the attack button, not a better version of one pull. The
    /// reveals fan out side by side so every note it played can be read.
    /// </remarks>
    private IEnumerator Flourish(Transform player, HashSet<UpgradeType> upgrades, int tunes)
    {
        for (int i = 0; i < tunes; i++)
        {
            if (player == null)
            {
                yield break;
            }

            float x = (i - (tunes - 1) * 0.5f) * flourishRevealSpacing;
            PlayRolledTune(player, upgrades, x);

            if (i < tunes - 1)
            {
                yield return new WaitForSeconds(strumInterval);
            }
        }
    }

    // ---- IMeleeChargeProvider: lights up the existing charge indicators ----

    public bool IsCharging => isCharging;

    public float ChargeProgress =>
        isCharging && maxChargeTime > 0f ? Mathf.Clamp01(chargeDuration / maxChargeTime) : 0f;

    public bool IsMaxCharge =>
        isCharging && maxChargeTime > 0f && chargeDuration >= maxChargeTime;

    // No upgrade gate: the Flourish is Light's baseline charge, not a purchase.
    public bool CanShowChargeIndicator(HashSet<UpgradeType> upgrades, PlayerController player) =>
        player.IsMeleeReady;

    /// <summary>
    /// Starts the charge. Like Dark, Light does <b>not</b> root the player: the harp keeps moving while
    /// it builds a Flourish.
    /// </summary>
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
        chargeDuration = 0f;
    }

    /// <summary>Clears the charge whenever the imbue ends, in case something skipped the cancel path.</summary>
    public void OnBuffEnd(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        ResetCharge();
    }

    private void Update()
    {
        if (isCharging && chargeDuration < maxChargeTime)
        {
            chargeDuration = Mathf.Min(chargeDuration + Time.deltaTime, maxChargeTime);
        }
    }

    private void ResetCharge()
    {
        isCharging = false;
        chargeDuration = 0f;
    }

    /// <summary>Pops the tune's note up above the player, shifted sideways by <paramref name="xOffset"/>.</summary>
    /// <remarks>
    /// Left in the world rather than parented: the player transform turns to face every target, and a
    /// parented note would spin with it.
    /// </remarks>
    private void Reveal(Transform player, HarpTune tune, float xOffset = 0f)
    {
        if (revealPrefab == null || tune.Glyph == null || PrefabPool.Instance == null)
        {
            return;
        }

        Vector3 position = player.position + (Vector3)revealOffset + Vector3.right * xOffset;
        GameObject reveal = PrefabPool.Instance.Spawn(revealPrefab, position, Quaternion.identity);

        SpriteRenderer? glyphRenderer = reveal.GetComponentInChildren<SpriteRenderer>();
        if (glyphRenderer != null)
        {
            glyphRenderer.sprite = tune.Glyph;
        }

        reveal.GetComponent<PooledInstance>()?.ReleaseAfter(revealLifetime);
    }

    // Ranged hooks are dormant while the sword throw is the ultimate, but implemented rather than
    // defaulted so Light stays consistent with the other elements if the throw comes back.
    public void OnRangedFlight(Transform player, SwordProjectile sword, HashSet<UpgradeType> upgrades)
    {
        sword.sprite.color = ElementVisuals.GetColor(Element.Light);
    }

    public void OnRangedHit(Transform player, SwordProjectile sword, Transform hitSource, EnemyController enemy, HashSet<UpgradeType> upgrades)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        enemy.TakeDamage(
            GameManager.Instance.CalculateDamage(enemy.element, Element.Light, GameManager.Instance.GetEffectiveBaseDamage() * GameManager.Instance.GetEffectiveRangedMultiplier()),
            new MoveType(Element.Light, AttackKind.Ranged));
    }
}
