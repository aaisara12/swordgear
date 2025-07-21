using UnityEngine;

public class FireEmbue : Embue
{
    [Header("Fire Embue Settings")]

    [Tooltip("Duration of the burn effect *applied by the sword* in seconds.")]
    public float burnDuration = 3f; // How long enemies burn for

    [Tooltip("Damage per second of the burn effect.")]
    public float burnDPS = 5f;

    private void Start()
    {
        damageMultiplier = 1.5f;
        effectDuration = 30f;
    }
}