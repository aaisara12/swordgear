using UnityEngine;

public class SwordProjectile : MonoBehaviour
{
    SpriteRenderer sprite;
    public enum WeaponBuff
    {
        None,
        Fire,
        Lightning
    }

    public WeaponBuff CurrentBuff = WeaponBuff.None;
    public float buffPower = 0;
    bool isFlying = false;  // For damage checks

    // [SerializeField] Vector2 startingVelocity = Vector2.zero;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float decelerationRate = 2f;
    private Rigidbody2D rb;
    public Vector2 prevVelocity = Vector2.zero;
    public static SwordProjectile Instance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        Instance = this;
    }

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public void StartFlight(Vector3 position, Vector2 velocity)
    {
        transform.position = position;
        transform.up = velocity.normalized;
        rb.linearVelocity = velocity;
        sprite.enabled = true;
        isFlying = true;
    }

    public void StopFlight()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        sprite.enabled = false;
        isFlying = false;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed > maxSpeed)
        {
            Vector2 deceleration = rb.linearVelocity.normalized * (decelerationRate * Time.fixedDeltaTime);
            rb.linearVelocity -= deceleration;

            if (rb.linearVelocity.magnitude < maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        prevVelocity = rb.linearVelocity;
    }

    void DisplaySword()
    {
        switch (CurrentBuff)
        {
            case WeaponBuff.None:
                sprite.color = Color.white;
                break;
            case WeaponBuff.Fire:
                sprite.color = Color.red;
                break;
            case WeaponBuff.Lightning:
                sprite.color = Color.cyan;
                break;
        }
    }

    void Update()
    {
        DisplaySword();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.GetComponent<EnemyController>();
            enemy.TakeDamage(1 + buffPower);
        }
    }
}
