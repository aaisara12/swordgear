using UnityEngine;

public abstract class Embue : MonoBehaviour
{
    // Define the EmbueType enum

    public Element embueType;

    public float effectDuration = 5f;

    public float damageMultiplier;

    /*    [Tooltip("Optional visual effect to play when the sword hits the embue.")]
        public GameObject activationEffectPrefab;*/

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object has a SwordController component
        
        SwordProjectile sword = collision.GetComponent<SwordProjectile>();
        if (sword != null)
        {
            TryEmpowerSword(sword); // Pass the SwordController
        }
    }

    private void TryEmpowerSword(SwordProjectile sword) // Accepts SwordController
    {
        // Call the abstract Empower method, passing the SwordController
        Empower(sword);
/*            if (activationEffectPrefab != null)
            {
                Instantiate(activationEffectPrefab, transform.position, Quaternion.identity);
            }*/
    }

    // Abstract method now accepts SwordController
    protected abstract void Empower(SwordProjectile sword);
}