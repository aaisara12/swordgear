#nullable enable

using UnityEngine;

/// <summary>
/// A tune that makes the player briefly untouchable: Fermata, the note that holds.
/// </summary>
/// <remarks>
/// Uses <c>PlayerController.GrantIFrames</c>, the same window a dash or a hit grants, which never
/// shortens one already running. That primitive also blinks the player sprite; the ward worn on top is
/// what tells this apart from having just been hit.
/// <para>
/// Fermata exists partly to protect a hot streak (commit 15), which busts the moment you take damage.
/// </para>
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Harp Tunes/Ward Tune", fileName = "WardTune")]
public class WardTune : HarpTune
{
    [SerializeField] private float invulnerableSeconds = 1.5f;
    [Tooltip("Worn by the player while the ward holds. Must carry HarpAura.")]
    [SerializeField] private GameObject? wardPrefab;

    public override void Play(HarpContext context)
    {
        if (wardPrefab == null)
        {
            Debug.LogError($"WardTune '{name}': wardPrefab is null");
            return;
        }

        PlayerController? controller = context.Player.GetComponent<PlayerController>();
        if (controller == null)
        {
            Debug.LogError($"WardTune '{name}': player has no PlayerController");
            return;
        }

        controller.GrantIFrames(invulnerableSeconds);
        HarpAuraUtility.Wear(wardPrefab, context.Player, invulnerableSeconds);
    }
}
