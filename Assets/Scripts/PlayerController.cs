#nullable enable

using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float attackRadius = 5f;
    [SerializeField] private float dashFactor = 0.2f;

    [SerializeField] private float speed = 3f;
    [SerializeField] private SwordController? sword;
    [SerializeField] private GearController? gear;

    [SerializeField] private GameObject? weaponObject;

    IMeleeWeapon? weapon;

    private Rigidbody2D? rb;

    private void Awake()
    {
        weaponObject.ThrowIfNull(nameof(weaponObject));

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        weapon = weaponObject.GetComponent<IMeleeWeapon>();
    }

    void MeleeAttack()
    {
        rb.ThrowIfNull(nameof(rb));
        weapon.ThrowIfNull(nameof(weapon));

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject? nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < shortestDistance && distance <= attackRadius)
            {
                shortestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy == null)
        {
            weapon.Strike(transform);
            return;
        }

        Vector2 direction = (nearestEnemy.transform.position - transform.position).normalized;
        transform.up = direction;

        Vector2 dashPosition = (Vector2)transform.position + direction * (shortestDistance * dashFactor);
        transform.position = dashPosition;
        weapon.Strike(transform);
    }

    private void OnMove(InputValue value)
    {
        rb.ThrowIfNull(nameof(rb));

        Vector2 v = value.Get<Vector2>();
        rb.linearVelocity = v * speed;
    }

    private void OnAction()
    {
        MeleeAttack();
    }

}
