using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        // Destroy the projectile after a set lifetime to prevent memory leaks
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the projectile hits the player
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            // Destroy the projectile after it hits the player
            Destroy(gameObject);
        }
    }
}