using UnityEngine;

// Shared sight tests for enemy AI. Movement and shooting use different masks on purpose: bodies
// collide with LowWall, but the Projectiles layer ignores it, so bullets clear low cover.
public static class EnemyVision
{
    private static int movementMask;
    private static int shotMask;
    private static bool masksReady;

    public static int MovementMask
    {
        get { EnsureMasks(); return movementMask; }
    }

    public static int ShotMask
    {
        get { EnsureMasks(); return shotMask; }
    }

    private static void EnsureMasks()
    {
        if (masksReady)
        {
            return;
        }

        movementMask = LayerMask.GetMask("Arena", "LowWall");
        shotMask = LayerMask.GetMask("Arena");
        masksReady = true;
    }

    public static bool CanShoot(Vector2 from, Vector2 to, float radius = 0f)
    {
        return IsClear(from, to, radius, ShotMask);
    }

    public static bool CanWalk(Vector2 from, Vector2 to, float radius = 0f)
    {
        return IsClear(from, to, radius, MovementMask);
    }

    public static bool IsClear(Vector2 from, Vector2 to, float radius, int mask)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        Vector2 direction = delta / distance;
        return radius > 0f
            ? Physics2D.CircleCast(from, radius, direction, distance, mask).collider == null
            : Physics2D.Raycast(from, direction, distance, mask).collider == null;
    }
}
