#nullable enable

using UnityEngine;

/// <summary>
/// A timed effect worn by the player while a sustained tune lasts: Lullaby's drifting notes, Fermata's
/// ward. The look is authored in the prefab; this only fits it to the tune's duration.
/// </summary>
/// <remarks>
/// Any clip is stretched to the duration (DarkMinion's drain-sync idiom) and any emitter emits for exactly
/// the duration, so a tune's length stays the single source of truth and retuning it can never leave
/// its visual out of step.
/// </remarks>
public class HarpAura : MonoBehaviour
{
    [Tooltip("Optional. Its clip is stretched to the aura's duration.")]
    [SerializeField] private Animator? animator;
    [Tooltip("Optional. Emit for the aura's duration, then stop.")]
    [SerializeField] private ParticleSystem[] emitters = System.Array.Empty<ParticleSystem>();
    [Tooltip("Seconds the aura lingers after the duration, so particles already emitted can finish.")]
    [SerializeField] private float tailSeconds = 0f;

    /// <summary>Runs the aura for <paramref name="duration"/> seconds, then returns it to the pool.</summary>
    public void Play(float duration)
    {
        duration = Mathf.Max(0.01f, duration);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            float clipLength = clips.Length > 0 ? clips[0].length : duration;
            animator.speed = clipLength / duration;
            animator.Play(0, 0, 0f);
        }

        foreach (ParticleSystem emitter in emitters)
        {
            if (emitter == null)
            {
                continue;
            }

            // Duration can only change on a stopped system; a pooled aura may be reused mid-tail.
            emitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = emitter.main;
            main.duration = duration;
            emitter.Play(true);
        }

        GetComponent<PooledInstance>()?.ReleaseAfter(duration + tailSeconds);
    }
}

/// <summary>Shared by the tunes that put an aura on the player.</summary>
public static class HarpAuraUtility
{
    /// <summary>Spawns <paramref name="prefab"/> on the player and runs it for <paramref name="duration"/>.</summary>
    /// <remarks>
    /// Parented so it follows the player. The player transform turns to face targets, so an aura's
    /// look must not depend on rotation: rings, and particles simulated in world space.
    /// </remarks>
    public static void Wear(GameObject prefab, Transform player, float duration)
    {
        if (PrefabPool.Instance == null)
        {
            return;
        }

        GameObject obj = PrefabPool.Instance.Spawn(prefab, player.position, Quaternion.identity, player);
        HarpAura? aura = obj.GetComponent<HarpAura>();
        if (aura == null)
        {
            Debug.LogError($"HarpAuraUtility: '{prefab.name}' has no HarpAura");
            PrefabPool.Instance.Release(obj);
            return;
        }

        aura.Play(duration);
    }
}
