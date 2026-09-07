#nullable enable

using UnityEngine;

public class PlayerWeaponIndicator : MonoBehaviour
{
    [SerializeField] private Transform? pivot;
    [SerializeField] private Transform? swordVisual;
    [SerializeField] private SpriteRenderer? swordRenderer;
    [SerializeField] private Transform? playerRoot;
    [Tooltip("Fallback auto-aim reach, used only before an element is active. The live value comes from " +
             "the equipped element — see IElementalWeapon.AutoAimRadius.")]
    [SerializeField] private float targetRadius = ActiveEnemyRegistry.AutoTargetRadius;
    [SerializeField] private float enemyScanInterval = 0.15f;
    [SerializeField] private float idleRotationDegreesPerSecond = 720f;

    private Vector2 aimDirection;
    private Vector2 moveFallbackDirection = Vector2.up;
    private Vector2 manualAimDirection;
    private bool manualAimActive;
    private EnemyController? trackedEnemy;
    private EnemyController? renderedEnemy;
    private bool throwAimActive;
    private bool isVisible = true;
    private float nextEnemyScanTime;

    // Reach belongs to the equipped element, not to this component: a turret should lock on from across
    // the arena where a sword shouldn't. The serialized value only covers the window before an element
    // is active.
    private float TargetRadius =>
        ElementManager.Instance != null ? ElementManager.Instance.AutoAimRadius : targetRadius;

    private void Awake()
    {
        if (pivot == null)
        {
            Debug.LogError("PlayerWeaponIndicator: pivot is null");
            return;
        }

        if (swordVisual == null)
        {
            Debug.LogError("PlayerWeaponIndicator: swordVisual is null");
            return;
        }

        if (playerRoot == null)
        {
            playerRoot = pivot.root;
        }

        moveFallbackDirection = pivot.up;
        RefreshTrackedEnemy(force: true);
        ApplyIdleFacing(forceSnap: true);
    }

    private void LateUpdate()
    {
        if (pivot == null || !isVisible)
        {
            return;
        }

        if (throwAimActive)
        {
            SetPivotRotation(aimDirection, immediate: true);
            return;
        }

        if (Time.time >= nextEnemyScanTime)
        {
            RefreshTrackedEnemy(force: true);
        }

        ApplyIdleFacing();
    }

    public void UpdateThrowAim(Vector2 direction)
    {
        if (!throwAimActive)
        {
            throwAimActive = true;
            if (aimDirection.sqrMagnitude < 0.001f && pivot != null)
            {
                aimDirection = pivot.up;
            }
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            aimDirection = direction.normalized;
            SetPivotRotation(aimDirection, immediate: true);
        }
    }

    public void EndThrowAim()
    {
        throwAimActive = false;
        aimDirection = Vector2.zero;
        RefreshTrackedEnemy(force: true);
        ApplyIdleFacing(forceSnap: true);
    }

    /// <summary>
    /// Takes manual control of the facing, above auto-aim, until <see cref="ClearManualAim"/>.
    /// </summary>
    /// <remarks>
    /// Used while an element roots the player and the movement stick becomes aim. Auto-aim outranks the
    /// stick normally, which is right when the stick means "move" — but during a rooted charge it would
    /// mean a nearby enemy silently steals the shot the player is lining up.
    /// <para>
    /// Cleared as soon as the stick recentres, so auto-aim takes over again the moment the player stops
    /// steering. With no enemy in range there is nothing to take over, and facing falls through to
    /// <see cref="SetMoveFallbackDirection"/> — which the root feeds the same direction — so the aim
    /// simply stays where it was pointed.
    /// </para>
    /// </remarks>
    public void SetManualAim(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        manualAimActive = true;
        manualAimDirection = direction.normalized;
    }

    /// <summary>Hands facing back to auto-aim. Called when the root ends.</summary>
    public void ClearManualAim()
    {
        manualAimActive = false;
        manualAimDirection = Vector2.zero;
    }

    public void SetMoveFallbackDirection(Vector2 direction)
    {
        if (throwAimActive || direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        moveFallbackDirection = direction.normalized;
    }

    public void SetEquippedVisible(bool visible)
    {
        isVisible = visible;

        if (swordRenderer != null)
        {
            swordRenderer.enabled = visible;
        }
    }

    public Vector3 GetThrowOrigin()
    {
        return swordVisual != null ? swordVisual.position : transform.position;
    }

    public Vector2 GetFacingDirection()
    {
        if (throwAimActive)
        {
            return aimDirection.sqrMagnitude > 0.001f ? aimDirection : (Vector2)pivot!.up;
        }

        if (manualAimActive)
        {
            return manualAimDirection;
        }

        if (playerRoot != null
            && ActiveEnemyRegistry.TryGetNearestDirection(playerRoot.position, TargetRadius, out Vector2 enemyDirection))
        {
            return enemyDirection;
        }

        if (moveFallbackDirection.sqrMagnitude > 0.001f)
        {
            return moveFallbackDirection;
        }

        return pivot != null ? (Vector2)pivot.up : Vector2.up;
    }

    private void RefreshTrackedEnemy(bool force)
    {
        if (!force && Time.time < nextEnemyScanTime)
        {
            return;
        }

        nextEnemyScanTime = Time.time + enemyScanInterval;

        if (playerRoot != null
            && ActiveEnemyRegistry.TryGetNearest(playerRoot.position, TargetRadius, out EnemyController nearest, out _))
        {
            trackedEnemy = nearest;
            return;
        }

        trackedEnemy = null;
    }

    private void ApplyIdleFacing(bool forceSnap = false)
    {
        if (!TryGetIdleDirection(out Vector2 direction))
        {
            return;
        }

        SetPivotRotation(direction, forceSnap || trackedEnemy != renderedEnemy);
        renderedEnemy = trackedEnemy;
    }

    private bool TryGetIdleDirection(out Vector2 direction)
    {
        if (manualAimActive)
        {
            direction = manualAimDirection;
            return true;
        }

        if (trackedEnemy != null && playerRoot != null)
        {
            Vector2 offset = (Vector2)trackedEnemy.transform.position - (Vector2)playerRoot.position;
            float maxRadiusSqr = TargetRadius * TargetRadius;
            if (offset.sqrMagnitude <= maxRadiusSqr && offset.sqrMagnitude > 0.001f)
            {
                direction = offset.normalized;
                return true;
            }

            trackedEnemy = null;
        }

        direction = moveFallbackDirection;
        return direction.sqrMagnitude > 0.001f;
    }

    private void SetPivotRotation(Vector2 direction, bool immediate)
    {
        if (pivot == null || direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
        if (immediate)
        {
            pivot.rotation = targetRotation;
            return;
        }

        pivot.rotation = Quaternion.RotateTowards(
            pivot.rotation,
            targetRotation,
            idleRotationDegreesPerSecond * Time.deltaTime);
    }
}
