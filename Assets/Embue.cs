using System.Collections;
using UnityEngine;

public abstract class Embue : MonoBehaviour
{
    public Element embueType;

    public float effectDuration = 5f;
    public float damageMultiplier;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object has a SwordController component
        SwordProjectile sword = collision.GetComponent<SwordProjectile>();
        if (sword != null)
        {
            GameManager.Instance.ApplyEmpowerment(embueType, damageMultiplier, effectDuration);
        }
    }
}