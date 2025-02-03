using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordController : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float angleModifier;
    [SerializeField] Transform gear;
    [SerializeField] Transform player;
    bool canMove;


    Rigidbody2D rb;

    private float damage = 1f;
    private Vector3 lastPlayerPosition;
    void Start()
    {
        lastPlayerPosition = player.position;
        canMove = true;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.parent != null)
        {
            pointAtPlayer();
            moveWithPlayer();
        }
    }

    void moveWithPlayer()
    {
        transform.position += player.position - lastPlayerPosition;
        lastPlayerPosition = player.position;
    }

    void pointAtPlayer()
    {
        Vector2 swordPosition = transform.position;
        Vector2 playerPosition = player.position;
        Vector2 pullDirection = (playerPosition - swordPosition).normalized;
        float angle = Mathf.Atan2(pullDirection.y, pullDirection.x) * Mathf.Rad2Deg + angleModifier;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void AttachToGear()
    {
        if (gear != null)
        {
            transform.parent = gear;
            lastPlayerPosition = player.position;
            canMove = true;
        }
    }

    void DetachFromGear()
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

/*    private void OnCollisionExit2D(Collision2D collision)
    {
        if (rb != null)
        {
            if (collision.transform.gameObject.layer == 11)
            { // gear layer
                DetachFromGear();
            }
        }
    }
*/

    public void MoveSword(Vector2 playerPosition)
    {

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

    public float getDamage()
    {
        // add multipliers in the future
        return damage;
    }
}
