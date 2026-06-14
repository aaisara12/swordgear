#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks live enemies without scene-wide tag searches.
/// </summary>
public static class ActiveEnemyRegistry
{
    public const float AutoTargetRadius = 5f;

    private static readonly List<EnemyController> ActiveEnemies = new();

    public static IReadOnlyList<EnemyController> All => ActiveEnemies;

    public static void Register(EnemyController enemy)
    {
        if (!ActiveEnemies.Contains(enemy))
        {
            ActiveEnemies.Add(enemy);
        }
    }

    public static void Unregister(EnemyController enemy)
    {
        ActiveEnemies.Remove(enemy);
    }

    public static bool TryGetNearest(Vector2 origin, float maxRadius, out EnemyController nearest, out float distance)
    {
        nearest = null!;
        distance = 0f;

        float maxRadiusSqr = maxRadius * maxRadius;
        float shortestDistanceSqr = float.MaxValue;
        EnemyController? candidate = null;

        for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyController enemy = ActiveEnemies[i];
            if (enemy == null)
            {
                ActiveEnemies.RemoveAt(i);
                continue;
            }

            float distanceSqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
            if (distanceSqr > maxRadiusSqr || distanceSqr >= shortestDistanceSqr)
            {
                continue;
            }

            shortestDistanceSqr = distanceSqr;
            candidate = enemy;
        }

        if (candidate == null)
        {
            return false;
        }

        nearest = candidate;
        distance = Mathf.Sqrt(shortestDistanceSqr);
        return true;
    }

    public static bool TryGetNearestDirection(Vector2 origin, float maxRadius, out Vector2 direction)
    {
        direction = Vector2.zero;

        if (!TryGetNearest(origin, maxRadius, out EnemyController nearest, out _))
        {
            return false;
        }

        direction = ((Vector2)nearest.transform.position - origin).normalized;
        return true;
    }
}
