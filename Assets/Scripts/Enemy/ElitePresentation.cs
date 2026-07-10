#nullable enable

using UnityEngine;

/// <summary>
/// Elite presentation + combat multipliers. Elites are runtime flags on any archetype — not duplicate prefabs.
/// </summary>
[CreateAssetMenu(fileName = "ElitePresentation", menuName = "Scriptable Objects/ElitePresentation")]
public class ElitePresentation : ScriptableObject
{
    [Tooltip("Optional reference for editor tooling; runtime uses the EliteAura child already on the enemy prefab.")]
    [SerializeField] private GameObject? auraPrefab;

    [Header("Stat multipliers (combined on top of difficulty + element)")]
    [SerializeField] private float hpMultiplier = 2f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float speedMultiplier = 1.1f;
    [SerializeField] private float scaleMultiplier = 1.4f;

    public GameObject? AuraPrefab => auraPrefab;

    public SpawnModifiers ToSpawnModifiers()
    {
        return new SpawnModifiers
        {
            HpMultiplier = hpMultiplier,
            DamageMultiplier = damageMultiplier,
            SpeedMultiplier = speedMultiplier,
            AttackRateMultiplier = 1f,
            ChargeTimeMultiplier = 1f,
            ProjectileSpeedMultiplier = 1f,
            ScaleMultiplier = scaleMultiplier,
        };
    }
}
