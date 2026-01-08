using UnityEngine;

public interface ISwordThrowBehavior
{
    void OnFlight();
    void OnEnemyHit(EnemyController enemy);
}

public class SwordProjectile : MonoBehaviour
{
    public SpriteRenderer sprite;
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
            ElementManager.Instance.OnBuffStart(GameManager.Instance.player.transform, this);
            ElementManager.Instance.OnBuffEnd(GameManager.Instance.player.transform, this);

            _currentBuff = value;
        }
    }
    public float buffPower = 0;
    bool isFlying = false;  // For damage checks
    [SerializeField] ParticleSystem swingTrail;

    // [SerializeField] Vector2 startingVelocity = Vector2.zero;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float decelerationRate = 2f;
    public GameObject spriteObject;

    private Rigidbody2D rb;
    public Vector2 prevVelocity = Vector2.zero;

    [Header("Fire Projectile")]
    [SerializeField] float spinSpeed = 50f;

    public static SwordProjectile Instance;

    [Header("Lightning Projectile")]
    private bool lightningActive = false;
    [SerializeField] GameObject lightningPrefab;

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
        sprite = spriteObject.GetComponent<SpriteRenderer>();
        OnBuffBegin(_currentBuff);
    }

    public void StartFlight(Vector3 position, Vector2 velocity)
    {
        gameObject.SetActive(true);
        transform.position = position;
        transform.up = velocity.normalized;
        rb.linearVelocity = velocity;
        sprite.enabled = true;
        isFlying = true;
    }

    public void StopFlight()
    {
        gameObject.SetActive(false);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        sprite.enabled = false;
        isFlying = false;
    }

    public void ToggleSwingTrail(bool on)
    {
        if (on)
        {
            if (!swingTrail.isPlaying)
                swingTrail.Play();
        }
        else
        {
            swingTrail.Stop();
        }
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
                sprite.color = Color.white;
                break;
            case Element.Fire:
                sprite.color = Color.red;
                sprite.transform.localEulerAngles += spinSpeed * Time.deltaTime * Vector3.forward;
                break;
            case Element.Ice:
                sprite.color = Color.cyan;
                break;
        }
    }

    void Update()
    {
        ElementManager.Instance.OnRangedFlight(GameManager.Instance.player.transform, this);
        //DisplaySword();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (collision.CompareTag("Enemy"))
        //{
        //    EnemyController enemy = collision.GetComponent<EnemyController>();
        //    enemy.TakeDamage(GameManager.Instance.CalculateDamage(enemy.element, CurrentBuff, GameManager.Instance.currentDamage * GameManager.Instance.rangedMultiplier));
        //    if (lightningActive)
        //    {
        //        ChainLightningProjectile lightning = Instantiate(lightningPrefab, collision.transform.position, Quaternion.identity).GetComponent<ChainLightningProjectile>();
        //        lightning.Initialize(transform);
        //    }
        //}
    }

    void OnBuffEnd(Element buff)
    {
        switch (buff)
        {
            case Element.Fire:
                sprite.transform.localEulerAngles = Vector3.zero;
                break;
            case Element.Ice:
                lightningActive = false;
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
                lightningActive = true;
                break;
        }
    }
}
