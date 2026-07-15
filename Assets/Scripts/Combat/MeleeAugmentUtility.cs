#nullable enable

using System;
using UnityEngine;

/// <summary>
/// Shared melee augment helpers so elemental weapons apply range/attack-speed the same way.
/// </summary>
public static class MeleeAugmentUtility
{
    public static void DamageEnemiesInRadius(Vector2 origin, float radius, Action<EnemyController> onHit)
    {
        float radiusSqr = radius * radius;
        foreach (EnemyController enemy in ActiveEnemyRegistry.All)
        {
            if (enemy == null) continue;
            if (((Vector2)enemy.transform.position - origin).sqrMagnitude > radiusSqr) continue;

            onHit(enemy);
        }
    }

    public static float RangeMultiplier =>
        PlayerStatModifiers.Instance != null ? PlayerStatModifiers.Instance.MeleeRangeMultiplier : 1f;

    public static float AttackSpeedMultiplier =>
        PlayerStatModifiers.Instance != null
            ? Mathf.Max(0.05f, PlayerStatModifiers.Instance.AttackSpeedMultiplier)
            : 1f;

    public static float ScaleDistance(float baseDistance) => baseDistance * RangeMultiplier;

    public static float ScaleSeekRadius(float baseRadius) => baseRadius * RangeMultiplier;

    public static float ScaleSwingDuration(float baseDuration) => baseDuration / AttackSpeedMultiplier;

    public static float ScaleCooldown(float baseCooldown) => baseCooldown / AttackSpeedMultiplier;

    public static Vector3 ForwardOffset(Transform player, float baseDistance) =>
        player.position + player.up * ScaleDistance(baseDistance);

    public static void ApplyRangeScale(Transform? target)
    {
        if (target == null)
        {
            return;
        }

        float range = RangeMultiplier;
        if (Mathf.Approximately(range, 1f))
        {
            return;
        }

        target.localScale = Vector3.Scale(target.localScale, new Vector3(range, range, 1f));
    }
}
