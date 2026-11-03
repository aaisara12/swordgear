#nullable enable

using AYellowpaper.SerializedCollections;
using UnityEngine;

/// <summary>
/// Trailing mount for the player's visible weapon: sits a step behind them, swaps in the blade belonging
/// to the active element, and hides the whole display while they can't attack.
/// </summary>
/// <remarks>
/// The hierarchy this drives, and why it has three levels rather than one:
/// <code>
/// WeaponAnchor      this component — follows the player in WORLD space
///   Display         displayRoot — switched off while the player can't attack
///     Fire          one child per element; exactly one is ever active
///     Ice
///     ...
/// </code>
/// The anchor is not a child of the player because the player's root rotates to face its attacks, and a
/// weapon parented to that would swing around with it instead of trailing. Hiding goes through the middle
/// object rather than the element children so the two concerns stay independent: switching element while
/// the display is down must not bring it back, and coming back up must not have to remember which element
/// was showing.
/// <para>
/// Element and attack state are polled in LateUpdate rather than driven by
/// <see cref="ElementManager.OnActiveElementChanged"/>. Same reason as <see cref="ChargePhaseLabel"/>:
/// the managers come up in their own order, and a subscriber that misses the first switch shows the wrong
/// weapon for the rest of the run.
/// </para>
/// </remarks>
public class WeaponAnchor : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("The player. Left empty, GameManager's player is used.")]
    [SerializeField] private Transform? followTarget;
    [Tooltip("World units to the side of the player, opposite the way they last moved horizontally.")]
    [SerializeField] private float trailDistance = 0.45f;
    [Tooltip("Vertical nudge applied on top of the trailing offset, for sitting the weapon at shoulder " +
             "height rather than at the player's centre.")]
    [SerializeField] private float heightOffset = 0f;
    [Tooltip("How hard the anchor is pulled toward its destination each second. Higher is tighter to the " +
             "player; lower drags further behind on direction changes.")]
    [SerializeField] private float followLerpSpeed = 12f;

    [Header("Display")]
    [Tooltip("Child holding the per-element weapons. Toggled off while the player can't attack.")]
    [SerializeField] private GameObject? displayRoot;
    [SerializedDictionary("Element", "Weapon Object")]
    [SerializeField] private SerializedDictionary<Element, GameObject> elementWeapons = new();

    [SerializeField] private Transform? target;
    private PlayerController? player;

    // Nullable so the first frame always applies. ActiveElement starts at Physical, which is also
    // default(Element) — a plain field would match on frame one and leave every weapon switched off.
    private Element? shownElement;

    // Only ever +1 or -1. Stopping or moving straight up/down keeps the last side, so the weapon stays put
    // rather than snapping back to some default.
    private float facingSign = 1f;

    private void Start()
    {
        ApplyActiveElement();

        // The one hard snap. Authoring puts the anchor wherever it was dropped in the scene, and lerping
        // from there means the weapon flies in across the level on the first frame of play.
        Transform? followed = ResolveTarget();
        if (followed != null)
        {
            transform.position = DestinationFor(followed);
        }
    }

    private void LateUpdate()
    {
        Transform? followed = ResolveTarget();
        if (followed != null)
        {
            UpdateFacing();

            // Exponential smoothing, so the pull is the same at any framerate. Scaled delta on purpose:
            // the weapon should freeze along with everything else during HitStop.
            transform.position = Vector3.Lerp(
                transform.position,
                DestinationFor(followed),
                1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime));
        }

        ApplyActiveElement();
        ApplyDisplayVisibility();
    }

    private Vector3 DestinationFor(Transform followed)
    {
        return followed.position + new Vector3(-facingSign * trailDistance, heightOffset, 0f);
    }

    private void UpdateFacing()
    {
        if (player == null)
        {
            return;
        }

        float moveX = player.MoveDirection.x;
        if (Mathf.Abs(moveX) > 0.03f)
        {
            facingSign = Mathf.Sign(moveX);
        }
    }

    private void ApplyActiveElement()
    {
        ElementManager? elements = ElementManager.Instance;
        if (elements == null)
        {
            return;
        }

        Element element = elements.ActiveElement;
        if (shownElement == element)
        {
            return;
        }

        shownElement = element;

        foreach (var pair in elementWeapons)
        {
            if (pair.Value == null)
            {
                continue;
            }

            bool shouldBeActive = pair.Key == element;
            if (pair.Value.activeSelf != shouldBeActive)
            {
                pair.Value.SetActive(shouldBeActive);
            }
        }
    }

    private void ApplyDisplayVisibility()
    {
        if (displayRoot == null)
        {
            return;
        }

        bool show = ShouldShowWeapon();
        if (displayRoot.activeSelf != show)
        {
            displayRoot.SetActive(show);
        }
    }

    /// <remarks>
    /// Charging keeps the weapon in hand everywhere except Earth, which attacks from a ballista it builds
    /// instead of with the blade — showing the blade through that charge would promise a swing that never
    /// comes. It returns on the same "ready to attack again" edge as every other element, so handing back
    /// from the ballista needs no separate rule.
    /// </remarks>
    private bool ShouldShowWeapon()
    {
        ElementManager? elements = ElementManager.Instance;
        if (elements != null && elements.ActiveElement == Element.Earth && elements.IsWeaponCharging)
        {
            return false;
        }

        // Before the player resolves there is no attack to hide for, so rest at visible.
        return player == null || player.IsAttackReady;
    }

    private Transform? ResolveTarget()
    {
        // if (target != null)
        // {
        //     return target;
        // }

        if (followTarget != null)
        {
            target = followTarget;
        }
        else if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            target = GameManager.Instance.player.transform;
        }

        if (target != null && player == null)
        {
            player = GameManager.Instance != null && GameManager.Instance.playerController != null
                ? GameManager.Instance.playerController
                : target.GetComponentInParent<PlayerController>();
        }

        return target;
    }
}
