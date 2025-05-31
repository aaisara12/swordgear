using System.Collections;
using UnityEngine;

public class LightningMelee : MonoBehaviour, IMeleeWeapon
{

    [SerializeField] private Collider2D weaponCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator anim;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private float thrustDistance = 1.5f;
    [SerializeField] private string slashAnimName;
    [SerializeField] private string thrustAnimName;

    int combo = 0;

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

        anim.Play(slashAnimName);
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

    IEnumerator Thrust(Transform parent)
    {
        weaponCollider.enabled = true;
        float elapsedTime = 0f;

        Vector2 startPos = parent.position;
        Vector2 dest = parent.position + parent.up * thrustDistance;

        anim.Play(thrustAnimName);
        while (elapsedTime < swingDuration)
        {
            Vector2 pos = Vector2.Lerp(startPos, dest, elapsedTime * 8 / swingDuration);
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / swingDuration);
            if (spriteRenderer != null)
            {
                Color col = spriteRenderer.color;
                spriteRenderer.color = new Color(col.r, col.g, col.b, alpha);
            }
            parent.position = pos; 
            transform.position = parent.position + parent.up * distanceFromPlayer;
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

        switch (combo)
        {
            case 0:
            case 1:
                StartCoroutine(Swing());
                break;
            case 2:
                StartCoroutine(Thrust(parent));
                break;
        }
        combo = (combo + 1) % 3;
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
