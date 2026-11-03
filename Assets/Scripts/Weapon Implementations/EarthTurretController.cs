#nullable enable

using UnityEngine;

/// <summary>
/// Drives the Earth ballista's visuals: assembles on enable, sits on the player and tracks their aim while
/// the shot charges, then stays where it fired from while the fire animation plays out.
/// </summary>
/// <remarks>
/// <see cref="EarthWeapon"/> owns all the timing; this only translates it into animator speeds. The clips
/// are authored at fixed lengths, so each speed parameter is set to "clip length / gameplay duration" —
/// that keeps the turret finishing assembly on exactly the frame the shot becomes fireable, and stepping
/// up a charge stage on exactly the frame the damage tier goes up.
/// </remarks>
[RequireComponent(typeof(Animator))]
public class EarthTurretController : MonoBehaviour
{
    private const float AssembleClipLength = 31f / 60f;
    private const float ChargeClipLength = 21f / 60f;

    private static readonly int AssembleState = Animator.StringToHash("TurretAssemble");
    private static readonly int FireState = Animator.StringToHash("TurretFire");
    private static readonly int AssembleSpeedParam = Animator.StringToHash("assembleSpeed");
    private static readonly int ChargeSpeedParam = Animator.StringToHash("chargeSpeed");

    private Animator? animator;
    private PlayerController? player;
    private bool following;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        PlayAssemble();
    }

    private void LateUpdate()
    {
        if (following && player != null)
        {
            FollowPlayer();
        }
    }

    /// <summary>
    /// Starts a fresh build on <paramref name="owner"/>, restarting from assembly even if a previous shot's
    /// fire animation is still playing.
    /// </summary>
    public void BeginCharge(PlayerController owner, float constructionTime, float chargeTimePerTier)
    {
        player = owner;
        following = true;
        FollowPlayer();

        if (animator != null)
        {
            animator.SetFloat(AssembleSpeedParam, SpeedFor(AssembleClipLength, constructionTime));
            animator.SetFloat(ChargeSpeedParam, SpeedFor(ChargeClipLength, chargeTimePerTier));
        }

        // Already active means the last shot's fire animation is still running, so OnEnable won't fire
        // to restart the build — do it here instead.
        if (gameObject.activeSelf)
        {
            PlayAssemble();
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    /// <summary>Plants the turret where it is and plays the shot.</summary>
    public void Fire()
    {
        following = false;
        animator?.Play(FireState, 0, 0f);
    }

    /// <summary>Hides the turret. Called directly on an aborted build, and as an animation event at the end of TurretFire.</summary>
    public void Hide()
    {
        following = false;
        gameObject.SetActive(false);
    }

    private void PlayAssemble()
    {
        animator?.Play(AssembleState, 0, 0f);
    }

    private void FollowPlayer()
    {
        transform.position = player!.transform.position;

        Vector2 aim = player.AimDirection;
        if (aim.sqrMagnitude > 0.001f)
        {
            transform.up = aim;
        }
    }

    private static float SpeedFor(float clipLength, float duration) =>
        duration > 0f ? clipLength / duration : 1f;
}
