#nullable enable

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
public class LightWeapon : MonoBehaviour, IElementalWeapon
{
    [Header("Tunes")]
    [Tooltip("Which tunes the harp knows this run. The roll only ever plays owned tunes.")]
    [SerializeField] private HarpRepertoire? repertoire;
    [SerializeField] private float tapCooldown = 0.45f;

    [Header("Reveal")]
    [Tooltip("Shows the rolled tune's note above the player. Its pop-in, rise and fade are authored in " +
             "the prefab's animation; this only sets the sprite and places it.")]
    [SerializeField] private GameObject? revealPrefab;
    [Tooltip("Above the streak indicator that floats over the player's head, so the two never overlap.")]
    [SerializeField] private Vector2 revealOffset = new(0f, 3.9f);
    [Tooltip("Seconds before the reveal returns to the pool. Match the reveal clip's length.")]
    [SerializeField] private float revealLifetime = 0.9f;

    [Header("Aim")]
    [Tooltip("Between Fire's 10 and Earth's 15: the harp's notes travel and home, so it reaches further " +
             "than the volley elements. Keep in step with each tune's aimRadius and the note's seekRadius.")]
    [SerializeField] private float autoAimRadius = 12f;

    public float AutoAimRadius => autoAimRadius;

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

    public float OnTap(Transform player, HashSet<UpgradeType> upgrades)
    {
        HarpTune? tune = repertoire != null ? HarpTune.PickByFamily(repertoire.Owned, Random.value, Random.value) : null;
        if (tune == null)
        {
            // Declined: nothing playable is configured, so don't spend a cooldown on silence.
            return 0f;
        }

        Reveal(player, tune);
        StartCoroutine(tune.PlayMelody());
        tune.Play(new HarpContext(player, upgrades, this));
        return tapCooldown;
    }

    /// <summary>Pops the tune's note up above the player.</summary>
    /// <remarks>
    /// Left in the world rather than parented: the player transform turns to face every target, and a
    /// parented note would spin with it.
    /// </remarks>
    private void Reveal(Transform player, HarpTune tune)
    {
        if (revealPrefab == null || tune.Glyph == null || PrefabPool.Instance == null)
        {
            return;
        }

        Vector3 position = player.position + (Vector3)revealOffset;
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
