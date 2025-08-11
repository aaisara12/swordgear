using UnityEngine;

public class LightningEmbue : Embue
{
    [Header("Lightning Embue Settings")]

    [Tooltip("Duration of the burn effect *applied by the sword* in seconds.")]
    public float burnDuration = 3f; // How long enemies burn for

    private void Start()
    {
        damageMultiplier = 1.2f;
        effectDuration = 30f;
    }
}
