using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer? spriteRenderer;
    [SerializeField] private Color physicalColor = Color.white;
    [SerializeField] private Color fireColor = Color.red;
    [SerializeField] private Color iceColor = Color.blue;
    [SerializeField] private Color lightningColor = Color.cyan;

    // Element of the enemy that shot this projectile (attacker)
    private Element attackerElement = Element.Physical;
    // Damage value from the enemy that shot this projectile
    private float damage = 10f;

    /// <summary>
    /// Initialize the projectile with the enemy's element and damage
    /// </summary>
    /// <param name="enemyElement">The element of the enemy that shot this projectile</param>
    /// <param name="projectileDamage">The damage value from the enemy's attack strategy</param>
    public void Initialize(Element enemyElement, float projectileDamage)
    {
        attackerElement = enemyElement;
        damage = projectileDamage;
    }

    private void Start()
    {
        // Fallback: auto-assign SpriteRenderer if not set in inspector
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        ApplyVisuals(attackerElement);

        // Destroy the projectile after a set lifetime to prevent memory leaks
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the projectile hits the player
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && GameManager.Instance != null)
            {
                // Get player weapon element from GameManager (defender)
                Element defenderElement = GameManager.Instance.currentElement;
                
                // Calculate damage using GameManager's CalculateDamage method
                // CalculateDamage(defenderElement, attackerElement, baseDamage)
                // Uses interactionMatrix[attackerElement][defenderElement] to get damage multiplier
                float finalDamage = GameManager.Instance.CalculateDamage(defenderElement, attackerElement, damage);
                
                player.TakeDamage(finalDamage);
            }

            // Destroy the projectile after it hits the player
            Destroy(gameObject);
        }
        if (other.CompareTag("ProjectileBlocking"))
        {
            // Destroy the projectile if the player hits it with a melee attack
            Destroy(gameObject);
        }
    }

    private void ApplyVisuals(Element element)
    {
        if (spriteRenderer == null) return;

        switch (element)
        {
            case Element.Fire:
                spriteRenderer.color = fireColor;
                break;
            case Element.Ice:
                spriteRenderer.color = iceColor;
                break;
            case Element.Lightning:
                spriteRenderer.color = lightningColor;
                break;
            default:
                spriteRenderer.color = physicalColor;
                break;
        }
    }
}