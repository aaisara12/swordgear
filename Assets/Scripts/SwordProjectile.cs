using UnityEngine;

public interface ISwordThrowBehavior
{
    void OnFlight();
    void OnEnemyHit(EnemyController enemy);
}

public class SwordProjectile : MonoBehaviour
{
    SpriteRenderer spriteObject;
    public enum WeaponBuff
    {
        None,
        Fire,
        Lightning
    }

    [SerializeField] Element _currentBuff = Element.Physical;
    public Element CurrentBuff
    {
        get { return _currentBuff; }
        set
        {
            OnBuffEnd(_currentBuff);
            OnBuffBegin(value);
            _currentBuff = value;
        }
    }
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
        spriteObject = GetComponentInChildren<SpriteRenderer>();
        OnBuffBegin(_currentBuff);
    }

    public void StartFlight(Vector3 position, Vector2 velocity)
    {
        gameObject.SetActive(true);
        transform.position = position;
        transform.up = velocity.normalized;
        rb.linearVelocity = velocity;
        spriteObject.enabled = true;
        isFlying = true;
    }

    public void StopFlight()
    {
        gameObject.SetActive(false);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        spriteObject.enabled = false;
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

    // Called every frame, think of this as animation loop
    void DisplaySword()
    {
        switch (CurrentBuff)
        {
            case Element.Physical:
                spriteObject.color = Color.white;
                break;
            case Element.Fire:
                spriteObject.color = Color.red;
                spriteObject.transform.Rotate(25f * Time.deltaTime * Vector3.forward);
                break;
            case Element.Ice:
                spriteObject.color = Color.cyan;
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
            enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, CurrentBuff, GameManager.Instance.currentDamage * GameManager.Instance.rangedMultiplier));
        }
    }

    void OnBuffEnd(Element buff)
    {
        switch (buff)
        {
            case Element.Fire:
                spriteObject.transform.up = transform.up;
                break;
            case Element.Ice:
                break;
        }
    }

    void OnBuffBegin(Element buff)
    {
        switch (buff)
        {
            case Element.Fire:
                break;
            case Element.Ice:
                break;
        }
    }
}
