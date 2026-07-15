#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerAimIndicator : MonoBehaviour
{
    public enum AimMode
    {
        SwordThrow,
        Dash
    }

    [Header("Indicators")]
    [SerializeField] private GameObject? primaryIndicator;
    [SerializeField] private Transform? primaryVisual;
    [SerializeField] private GameObject? bounceIndicator;
    [SerializeField] private Transform? bounceVisual;

    [Header("References")]
    [SerializeField] private PlayerWeaponIndicator? weaponIndicator;
    [SerializeField] private PlayerController? playerController;

    [Header("Throw Preview")]
    [SerializeField] private float swordThrowLength = 5f;
    [SerializeField] private float bounceSegmentLength = 3f;
    [SerializeField] private Vector2 boxCastSize = new Vector2(0.5f, 0.5f);
    [SerializeField] private float boxCastDistance = 5f;
    [Tooltip("Solid colliders on these layers also clip the throw line (no bounce). Tilemap / composite colliders always clip.")]
    [SerializeField] private LayerMask wallLayers = 1; // Default layer

    [Header("Length Mapping")]
    [Tooltip("World length that matches the authored Visual localScale.y / localPosition.")]
    [SerializeField] private float referenceLength = 5f;
    [Tooltip("Multiplier on the authored indicator Visual localScale.x (1 = prefab width).")]
    [SerializeField] private float indicatorWidth = 1f;

    private Vector2 aimDirection;
    private AimMode aimMode;
    private bool aimActive;

    private Vector3 primaryBaseScale = Vector3.one;
    private Vector3 primaryBaseLocalPos = Vector3.zero;
    private Vector3 bounceBaseScale = Vector3.one;
    private Vector3 bounceBaseLocalPos = Vector3.zero;

    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(16);
    private ContactFilter2D aimCastFilter;

    private enum AimBlockKind
    {
        None,
        Bumper,
        Wall
    }

    private void Awake()
    {
        // No layer filter on the cast itself — bumpers can live on any layer; walls are filtered after.
        aimCastFilter = ContactFilter2D.noFilter;
        aimCastFilter.useTriggers = true;

        if (primaryIndicator == null)
        {
            Debug.LogError("PlayerAimIndicator: primaryIndicator is null");
            return;
        }

        if (primaryVisual == null)
        {
            primaryVisual = primaryIndicator.transform.childCount > 0
                ? primaryIndicator.transform.GetChild(0)
                : null;
        }

        if (bounceIndicator == null)
        {
            Debug.LogError("PlayerAimIndicator: bounceIndicator is null");
            return;
        }

        if (bounceVisual == null)
        {
            bounceVisual = bounceIndicator.transform.childCount > 0
                ? bounceIndicator.transform.GetChild(0)
                : null;
        }

        if (primaryVisual != null)
        {
            primaryBaseScale = primaryVisual.localScale;
            primaryBaseLocalPos = primaryVisual.localPosition;
        }

        if (bounceVisual != null)
        {
            bounceBaseScale = bounceVisual.localScale;
            bounceBaseLocalPos = bounceVisual.localPosition;
        }

        if (boxCastDistance <= 0f)
        {
            boxCastDistance = swordThrowLength;
        }

        if (referenceLength <= 0.001f)
        {
            referenceLength = 1f;
        }

        Clear();
    }

    public void SetAim(Vector2 direction, AimMode mode)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        aimDirection = direction.normalized;
        aimMode = mode;
        aimActive = true;
    }

    public void Clear()
    {
        aimActive = false;
        aimDirection = Vector2.zero;

        if (primaryIndicator != null)
        {
            primaryIndicator.SetActive(false);
        }

        if (bounceIndicator != null)
        {
            bounceIndicator.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (!aimActive || aimDirection.sqrMagnitude < 0.001f)
        {
            if (primaryIndicator != null && primaryIndicator.activeSelf)
            {
                primaryIndicator.SetActive(false);
            }

            if (bounceIndicator != null && bounceIndicator.activeSelf)
            {
                bounceIndicator.SetActive(false);
            }

            return;
        }

        if (aimMode == AimMode.Dash)
        {
            UpdateDashPreview();
        }
        else
        {
            UpdateSwordThrowPreview();
        }
    }

    private void UpdateDashPreview()
    {
        if (bounceIndicator != null)
        {
            bounceIndicator.SetActive(false);
        }

        float dashTipDistance = playerController != null ? playerController.DashDistance : referenceLength;
        float dashLengthParam = LengthParamForTipDistance(primaryBaseLocalPos, primaryBaseScale, dashTipDistance);
        PlaceIndicator(primaryIndicator, primaryVisual, transform.position, aimDirection, dashLengthParam, primaryBaseScale, primaryBaseLocalPos);
    }

    private void UpdateSwordThrowPreview()
    {
        Vector2 origin = weaponIndicator != null
            ? (Vector2)weaponIndicator.GetThrowOrigin()
            : (Vector2)transform.position;

        // Cast at least as far as the drawn throw line so length tweaks stay in sync.
        float castDistance = Mathf.Max(boxCastDistance, swordThrowLength);
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        castHits.Clear();
        int hitCount = Physics2D.BoxCast(
            origin,
            boxCastSize,
            angle,
            aimDirection,
            aimCastFilter,
            castHits,
            castDistance);

        RaycastHit2D? blockingHit = null;
        AimBlockKind blockKind = AimBlockKind.None;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
            {
                continue;
            }

            AimBlockKind kind = ClassifyBlock(hit.collider);
            if (kind == AimBlockKind.None)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                blockingHit = hit;
                blockKind = kind;
            }
        }

        if (!blockingHit.HasValue)
        {
            float freeLengthParam = LengthParamForTipDistance(primaryBaseLocalPos, primaryBaseScale, swordThrowLength);
            PlaceIndicator(primaryIndicator, primaryVisual, origin, aimDirection, freeLengthParam, primaryBaseScale, primaryBaseLocalPos);

            if (bounceIndicator != null)
            {
                bounceIndicator.SetActive(false);
            }

            return;
        }

        RaycastHit2D blockHit = blockingHit.Value;
        // Seam on the aim centerline — BoxCast hit.point can sit laterally off-axis inside the cast width.
        float contactDist = Mathf.Clamp(blockHit.distance, 0.05f, swordThrowLength);
        Vector2 contact = origin + aimDirection * contactDist;
        float primaryLengthParam = LengthParamForTipDistance(primaryBaseLocalPos, primaryBaseScale, contactDist);
        PlaceIndicator(primaryIndicator, primaryVisual, origin, aimDirection, primaryLengthParam, primaryBaseScale, primaryBaseLocalPos);

        if (blockKind != AimBlockKind.Bumper)
        {
            if (bounceIndicator != null)
            {
                bounceIndicator.SetActive(false);
            }

            return;
        }

        Vector2 reflected = Vector2.Reflect(aimDirection, blockHit.normal);
        if (reflected.sqrMagnitude < 0.001f)
        {
            if (bounceIndicator != null)
            {
                bounceIndicator.SetActive(false);
            }

            return;
        }

        Vector2 reflectDir = reflected.normalized;
        float bounceLengthParam = LengthParamForTipDistance(bounceBaseLocalPos, bounceBaseScale, bounceSegmentLength);
        PlaceIndicator(
            bounceIndicator,
            bounceVisual,
            contact,
            reflectDir,
            bounceLengthParam,
            bounceBaseScale,
            bounceBaseLocalPos);
    }

    private float LengthParamForTipDistance(Vector3 baseLocalPos, Vector3 baseScale, float tipDistance)
    {
        // Near tip is anchored at the root; far tip lands at factor * baseScale.y.
        float tipSpanAtReference = baseScale.y;
        if (Mathf.Abs(tipSpanAtReference) < 0.001f)
        {
            return tipDistance;
        }

        return tipDistance * referenceLength / tipSpanAtReference;
    }

    private AimBlockKind ClassifyBlock(Collider2D collider)
    {
        if (collider.GetComponentInParent<Bumper>() != null)
        {
            return AimBlockKind.Bumper;
        }

        if (IsWallCollider(collider))
        {
            return AimBlockKind.Wall;
        }

        return AimBlockKind.None;
    }

    private bool IsWallCollider(Collider2D collider)
    {
        // Arena wall tilemaps use TilemapCollider2D (often on Default); always clip those.
        if (collider is TilemapCollider2D || collider is CompositeCollider2D)
        {
            return true;
        }

        if (collider.isTrigger)
        {
            return false;
        }

        return (wallLayers.value & (1 << collider.gameObject.layer)) != 0;
    }

    private void PlaceIndicator(
        GameObject? root,
        Transform? visual,
        Vector2 worldOrigin,
        Vector2 direction,
        float length,
        Vector3 baseScale,
        Vector3 baseLocalPos)
    {
        if (root == null)
        {
            return;
        }

        root.SetActive(true);
        float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        root.transform.SetPositionAndRotation(
            worldOrigin,
            Quaternion.Euler(0f, 0f, rotationAngle - 90f));

        if (visual == null)
        {
            return;
        }

        float factor = length / Mathf.Max(referenceLength, 0.001f);
        float width = Mathf.Max(0.001f, indicatorWidth);
        float scaleY = baseScale.y * factor;
        // Anchor the near tip at the root so length changes only grow the far tip.
        float halfY = Mathf.Abs(scaleY) * 0.5f;
        visual.localScale = new Vector3(baseScale.x * width, scaleY, baseScale.z);
        visual.localPosition = new Vector3(baseLocalPos.x, halfY, baseLocalPos.z);
    }
}
