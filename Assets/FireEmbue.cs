using UnityEngine;

public class FireEmbue : Embue
{
    [Header("Fire Embue Settings")]

    [Tooltip("Duration of the burn effect *applied by the sword* in seconds.")]
    public float burnDuration = 3f; // How long enemies burn for

    [Tooltip("Damage per second of the burn effect.")]
    public float burnDPS = 5f;

    [Tooltip("Prefab for the fire effect *on the sword* while imbued.")]
    public GameObject fireEffectPrefab; // Visual effect on the sword itself


    private void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }
    // Override the Empower method to apply fire effects using SwordController
    protected override void Empower(SwordController sword) // Accepts SwordController
    {
        Debug.Log($"Attempting to apply Fire Embue via {sword.gameObject.name}");
        SetDamageMultiplier(2f);
        sword.ApplyEmbue(this);
    }

    // RemoveEffect is not needed here for clearing the sword's state.
}