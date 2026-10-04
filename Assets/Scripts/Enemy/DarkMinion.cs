#nullable enable

using System.Collections;
using UnityEngine;

/// <summary>
/// A shade raised by a Dark execution. It hunts enemies, damages what it touches, and drains away.
/// </summary>
/// <remarks>
/// <b>It is not a converted enemy.</b> The executed enemy dies for real — a normal death, with its normal
/// FX, event and despawn — and this is a separate actor raised in its place wearing the dead enemy's
/// sprite. That choice removes the whole risk class the plan flagged around minions:
/// <list type="bullet">
/// <item>Wave clear counts <c>LevelLoader.activeEnemies</c> until the objects are destroyed, so a minion
/// that was still the enemy would hold the wave open forever. This one was never in that list.</item>
/// <item>The player's auto-aim reads <c>ActiveEnemyRegistry</c>, which a minion never joins — so the
/// pointer can't snap onto your own shade.</item>
/// <item>Nothing has to reliably neuter the enemy's own AI. There are 26 enemy prefabs with differing
/// attack and movement strategies, and disabling the right components on every one of them is exactly the
/// kind of thing that half-works.</item>
/// </list>
/// <para>
/// Its collider is a trigger and it is deliberately not tagged <c>Enemy</c>: triggers don't shove the
/// player around, and <c>PlayerHitbox</c> only reacts to that tag, so the player's own swings can't hit
/// their shade.
/// </para>
/// <para>
/// <b>Enemies ignore shades entirely.</b> A shade's only threat is its own constant health drain — the
/// necromancer's minions are borrowed time, not bodies to be fought over. That keeps the pressure on the
/// player to keep executing rather than on the enemies to clean up after them.
/// </para>
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class DarkMinion : MonoBehaviour
{
    [SerializeField] private SpriteRenderer? spriteRenderer;
    [Tooltip("Plays the drain fade, authored in DarkShade_Drain.anim. Its speed is set from drainSeconds " +
             "so the fade and the health always run out together.")]
    [SerializeField] private Animator? animator;
    [SerializeField] private float moveSpeed = 4.5f;
    [Tooltip("Seconds between hits on the same target, so contact isn't a damage-per-frame blender.")]
    [SerializeField] private float attackInterval = 0.6f;
    [Tooltip("Scales the size inherited from the corpse. 1 matches the enemy it was raised from.")]
    [SerializeField] private float sizeMultiplier = 1f;
    [Tooltip("Seconds of constant health drain from fully raised to collapsed.")]
    [SerializeField] private float drainSeconds = 8f;

    private float damage;
    // Normalised: 1 when raised, 0 when it collapses. Nothing but the drain ever lowers it.
    private float health;
    private float nextAttackTime;
    private EnemyController? target;
    private Rigidbody2D? body;

    private void Awake()
    {
        if (animator == null)
        {
            Debug.LogError("DarkMinion: animator is null");
            return;
        }
    }

    /// <summary>Raises the shade at full health, wearing the dead enemy's look and size.</summary>
    public void Raise(Sprite? corpseSprite, Vector3 corpseScale, float attackDamage)
    {
        damage = attackDamage;
        health = 1f;
        nextAttackTime = 0f;
        target = null;

        body ??= GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;

        // Inherit the corpse's on-screen size. The sprite alone renders at its native scale, which for most
        // enemies is far smaller than they actually appear.
        if (corpseScale.sqrMagnitude > 0.0001f)
        {
            transform.localScale = corpseScale * sizeMultiplier;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            // Wearing the dead enemy's sprite is what sells "that one is fighting for you now" — the
            // shade is a different object, but it should not look like one.
            if (corpseSprite != null)
            {
                spriteRenderer.sprite = corpseSprite;
            }

            // Tint only: the drain clip owns alpha and leaves the colour channels alone.
            spriteRenderer.color = ElementVisualUtility.GetAccentColor(Element.Dark);
        }

        SyncDrainAnimation();
        StartCoroutine(Live());
    }

    private IEnumerator Live()
    {
        while (health > 0f)
        {
            health -= Time.deltaTime / Mathf.Max(0.01f, drainSeconds);
            Hunt();
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Restarts the drain fade and stretches it to last exactly <see cref="drainSeconds"/>.
    /// </summary>
    /// <remarks>
    /// The fade itself is authored in the editor (DarkShade_Drain.anim — eased to stay solid for most of
    /// the shade's life and thin over the last stretch). drainSeconds stays the gameplay source of truth;
    /// this only scales playback so retuning the drain can never leave the fade out of step with it.
    /// </remarks>
    private void SyncDrainAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        float clipLength = clips.Length > 0 ? clips[0].length : drainSeconds;
        animator.speed = clipLength / Mathf.Max(0.01f, drainSeconds);
        animator.Play(0, 0, 0f);
    }

    /// <summary>
    /// Walks toward the nearest living enemy, re-picking every frame.
    /// </summary>
    /// <remarks>
    /// There is deliberately <b>no range limit</b> and no target loyalty: a shade always goes for whatever
    /// is closest to it right now, so it never strands itself chasing something across the arena while an
    /// enemy stands next to it, and never idles because everything drifted out of some radius.
    /// </remarks>
    private void Hunt()
    {
        if (!ActiveEnemyRegistry.TryGetNearest(transform.position, Mathf.Infinity, out target, out _))
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            return;
        }

        if (target == null || body == null)
        {
            return;
        }

        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
        body.linearVelocity = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized * moveSpeed : Vector2.zero;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextAttackTime || !other.CompareTag("Enemy"))
        {
            return;
        }

        EnemyController? enemy = other.GetComponent<EnemyController>();
        if (enemy == null || GameManager.Instance == null)
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;

        // feedsCombo:false, matching how DoT ticks are treated: the shade acts on its own, so its kills
        // must not keep the player's combo alive or charge their ultimate while they do nothing.
        enemy.TakeDamage(
            GameManager.Instance.CalculateDamage(enemy.element, Element.Dark, damage),
            new MoveType(Element.Dark, AttackKind.MeleeStrike),
            applyImpactFeel: true,
            feedsCombo: false);
    }
}
