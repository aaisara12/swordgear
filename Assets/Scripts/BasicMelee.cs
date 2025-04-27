using System.Collections;
using UnityEngine;

public interface IMeleeWeapon
{
    public void Strike(Transform parent);
}

public class BasicMelee : MonoBehaviour, IMeleeWeapon
{
    [SerializeField] private Collider2D weaponCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator anim;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private string animName;

    private void Awake()
    {
        if (weaponCollider == null)
            weaponCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private IEnumerator Swing()
    {
        weaponCollider.enabled = true;
        float elapsedTime = 0f;

        anim.Play(animName);
        while (elapsedTime < swingDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / swingDuration);
            if (spriteRenderer != null)
            {
                Color col = spriteRenderer.color;
                spriteRenderer.color = new Color(col.r, col.g, col.b, alpha);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        weaponCollider.enabled = false;
        if (spriteRenderer != null)
        {
            Color col = spriteRenderer.color;
            spriteRenderer.color = new Color(col.r, col.g, col.b, 0f);
        }
    }

    public void Strike(Transform parent)
    {
        transform.position = parent.position + parent.up * distanceFromPlayer;
        transform.up = parent.up;
        StartCoroutine(Swing());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            enemy.TakeDamage(1);
        }
    }
}
