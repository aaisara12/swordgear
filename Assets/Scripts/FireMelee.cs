using UnityEngine;
using System.Collections;

public class FireMelee : MonoBehaviour, IMeleeWeapon
{
    [SerializeField] private Collider2D weaponCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator anim;
    [SerializeField] private float swingDuration = 0.5f;
    [SerializeField] private float distanceFromPlayer = 0.5f;
    [SerializeField] private float maxChargeTime = 1f;
    [SerializeField] private string[] chargeAnimNames;

    bool isCharging = false;
    float chargeDuration = 0f;
    Transform parentTransform;

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
        int chargeTier = (int)(chargeDuration / maxChargeTime * (chargeAnimNames.Length - 1));
        anim.Play(chargeAnimNames[chargeTier]);
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

    private void Update()
    {
        if (isCharging && chargeDuration < maxChargeTime)
        {
            chargeDuration += Time.deltaTime;
            if (chargeDuration >= maxChargeTime)
            {
                // Play effect 
            }
        }
    }

    public void Strike(Transform parent)
    {
        transform.position = parent.position + parent.up * distanceFromPlayer;
        transform.up = parent.up;
        isCharging = false;
        StartCoroutine(Swing());
        chargeDuration = 0;
    }

    public void Charge(Transform parent, bool cancel = false) 
    {
        if (cancel)
        {
            isCharging = false;
            parentTransform = parent;
            chargeDuration = 0;
            return;
        }
        isCharging = true;
        parentTransform = parent;
        chargeDuration = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, Element.Fire, GameManager.Instance.currentDamage));
        }
    }
}
