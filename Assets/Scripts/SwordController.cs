#nullable enable

using UnityEngine;

public class SwordController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float angleModifier;
    [SerializeField] private Transform? gear;
    [SerializeField] private Transform? player;
    
    private bool canMove;
    private Rigidbody2D? rb;
    private float damage = 1f;
    private Vector3 lastPlayerPosition;
    
    public void MoveSword(Vector2 playerPosition)
    {
        rb.ThrowIfNull(nameof(rb));

        if(canMove == false)
        {
            return;
        }

        DetachFromGear();

        Vector2 swordPosition = transform.position;
        Vector2 pullDirection = (playerPosition - swordPosition).normalized;
        float angle = Mathf.Atan2(pullDirection.y, pullDirection.x) * Mathf.Rad2Deg + angleModifier;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        rb.linearVelocity = pullDirection * speed;
    }

    public float GetDamage()
    {
        // add multipliers in the future
        return damage;
    }
    
    private void Start()
    {
        player.ThrowIfNull(nameof(player));
        
        lastPlayerPosition = player.position;
        canMove = true;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (transform.parent != null)
        {
            PointAtPlayer();
            MoveWithPlayer();
        }
    }

    private void MoveWithPlayer()
    {
        player.ThrowIfNull(nameof(player));
        
        transform.position += player.position - lastPlayerPosition;
        lastPlayerPosition = player.position;
    }

    private void PointAtPlayer()
    {
        player.ThrowIfNull(nameof(player));
        
        Vector2 swordPosition = transform.position;
        Vector2 playerPosition = player.position;
        Vector2 pullDirection = (playerPosition - swordPosition).normalized;
        float angle = Mathf.Atan2(pullDirection.y, pullDirection.x) * Mathf.Rad2Deg + angleModifier;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void AttachToGear()
    {
        player.ThrowIfNull(nameof(player));
        
        if (gear != null)
        {
            transform.parent = gear;
            lastPlayerPosition = player.position;
            canMove = true;
        }
    }

    private void DetachFromGear()
    {
        if (gear != null)
        {
            transform.parent = null;

            canMove = false;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb != null)
        {
            if (collision.transform.gameObject.layer == 11)
            { // gear layer
                rb.linearVelocity = Vector2.zero;

                // attach it to the gear
                AttachToGear();
            }
        }
    }
}
