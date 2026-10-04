#nullable enable

using UnityEngine;

/// <summary>
/// A travelling ring — Resonance's sound wave. Its expansion is an authored AnimationClip; gameplay reads
/// the animated front back from here, so the ring you see is exactly where damage is.
/// </summary>
/// <remarks>
/// The clip is normalised: one second long, scaling <see cref="front"/> from near 0 to 1. <see cref="Play"/>
/// stretches it to the tune's travel time and sizes the root to the tune's radius, the same way
/// <c>DarkMinion</c> fits its drain clip to <c>drainSeconds</c>. Retiming or re-easing the wave is an edit
/// to the clip, and the damage follows it.
/// </remarks>
public class HarpWave : MonoBehaviour
{
    [SerializeField] private Animator? animator;
    [Tooltip("The child the clip scales. Its local scale x is the front's fraction of full radius.")]
    [SerializeField] private Transform? front;
    [Tooltip("Centreline radius of the ring sprite at scale 1, in world units.")]
    [SerializeField] private float spriteRadius = 0.92f;

    private float radius;

    /// <summary>World-space radius of the wavefront right now.</summary>
    public float FrontRadius => front != null ? front.localScale.x * radius : 0f;

    private void Awake()
    {
        if (animator == null)
        {
            Debug.LogError("HarpWave: animator is null");
            return;
        }

        if (front == null)
        {
            Debug.LogError("HarpWave: front is null");
            return;
        }
    }

    /// <summary>Starts the wave so its front reaches <paramref name="fullRadius"/> after <paramref name="travelSeconds"/>.</summary>
    public void Play(float fullRadius, float travelSeconds)
    {
        radius = fullRadius;
        transform.localScale = Vector3.one * (fullRadius / Mathf.Max(0.001f, spriteRadius));

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        float clipLength = clips.Length > 0 ? clips[0].length : 1f;
        animator.speed = clipLength / Mathf.Max(0.01f, travelSeconds);
        animator.Play(0, 0, 0f);
        // Apply frame 0 now, so the front reads near zero this frame rather than wherever the pooled
        // instance last stopped.
        animator.Update(0f);
    }
}
