#nullable enable annotations

using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer? spriteRenderer;
    [SerializeField] private GameObject? destroyEffect;
    [SerializeField] private Color physicalColor = Color.white;
    [SerializeField] private Color fireColor = Color.red;
    [SerializeField] private Color iceColor = Color.blue;
    [SerializeField] private Color lightningColor = Color.cyan;

    private Element attackerElement = Element.Physical;
    private float damage = 10f;

    public void Initialize(Element enemyElement, float projectileDamage)
    {
        attackerElement = enemyElement;
        damage = projectileDamage;

        spriteRenderer ??= GetComponent<SpriteRenderer>();
        ApplyVisuals(attackerElement);

        GetComponent<EnemyProjectileVisual>()?.Apply(attackerElement);

        var pooled = GetComponent<PooledInstance>();
        if (pooled != null)
            pooled.ReleaseAfter(lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && GameManager.Instance != null)
            {
                Element defenderElement = GameManager.Instance.currentElement;
                float finalDamage = GameManager.Instance.CalculateDamage(defenderElement, attackerElement, damage);
                player.TakeDamage(finalDamage);
            }

            PrefabPool.Instance?.Release(gameObject);
        }
        if (other.CompareTag("ProjectileBlocking"))
        {
            if (destroyEffect != null)
            {
                IAttackAnimator effect = PrefabPool.Instance!.Spawn(destroyEffect, transform.position, Quaternion.identity).GetComponent<IAttackAnimator>();
                effect.PlayAnimation();
            }

            PrefabPool.Instance?.Release(gameObject);
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
