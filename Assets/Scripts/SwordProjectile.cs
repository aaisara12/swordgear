using System;
using System.Collections.Generic;
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
    bool isRecalling = false;
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

    [Header("Terrain")]
    [SerializeField] private LayerMask terrainLayers;
    [SerializeField] private string gearPhysicsLayer = "Gear";

    [Header("Lightning Projectile")]
    [SerializeField] GameObject lightningPrefab;

    Transform? recallTarget;
    float recallSpeed;
    float recallCatchRadius;
    float recallElapsed;
    float recallMaxDuration;
    Action<bool>? onRecallArrived;
    bool _recallFinished;
    readonly List<int> _recallIgnoredLayers = new List<int>();

    public bool IsRecalling => isRecalling;

    static bool _swingTrailWarmed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        Instance = this;

        if (!_swingTrailWarmed && swingTrail != null)
        {
            swingTrail.Simulate(1f, true, true);
            swingTrail.Clear(true);
            _swingTrailWarmed = true;
        }
    }

    private void Start()
    {
        sprite = spriteObject.GetComponent<SpriteRenderer>();
        OnBuffBegin(_currentBuff);
    }

    public void StartFlight(Vector3 position, Vector2 velocity)
    {
        ClearRecallState();
        EnsureTerrainCollisionsEnabled();
        gameObject.SetActive(true);
        transform.position = position;
        transform.up = velocity.normalized;
        rb.linearVelocity = velocity;
        sprite.enabled = true;
        isFlying = true;
    }

    public void StartRecallFlight(
        Transform player,
        float speed,
        float catchRadius,
        float maxDuration,
        Action<bool> onArrived)
    {
        if (rb == null || player == null)
        {
            return;
        }

        gameObject.SetActive(true);
        sprite.enabled = true;

        rb.angularVelocity = 0f;

        SetRecallCollisionIgnores(true);

        recallTarget = player;
        recallSpeed = speed;
        recallCatchRadius = catchRadius;
        recallMaxDuration = maxDuration;
        onRecallArrived = onArrived;
        recallElapsed = 0f;
        _recallFinished = false;
        isRecalling = true;
        isFlying = true;

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.up;
        }

        transform.up = direction;
        rb.linearVelocity = direction * recallSpeed;
    }

    public void StopFlight()
    {
        ClearRecallState();

        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer(gearPhysicsLayer), false);
        gameObject.SetActive(false);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        sprite.enabled = false;
        isFlying = false;
    }

    void ClearRecallState()
    {
        isRecalling = false;
        recallTarget = null;
        onRecallArrived = null;
        recallElapsed = 0f;
        RestoreRecallCollisionIgnores();
    }

    void SetRecallCollisionIgnores(bool ignore)
    {
        if (!ignore)
        {
            RestoreRecallCollisionIgnores();
            return;
        }

        RestoreRecallCollisionIgnores();

        int swordLayer = gameObject.layer;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((terrainLayers.value & (1 << layer)) == 0)
            {
                continue;
            }

            Physics2D.IgnoreLayerCollision(swordLayer, layer, true);
            _recallIgnoredLayers.Add(layer);
        }

        int gearLayer = LayerMask.NameToLayer(gearPhysicsLayer);
        if (gearLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(swordLayer, gearLayer, true);
            _recallIgnoredLayers.Add(gearLayer);
        }
    }

    void RestoreRecallCollisionIgnores()
    {
        if (_recallIgnoredLayers.Count == 0)
        {
            return;
        }

        int swordLayer = gameObject.layer;
        foreach (int layer in _recallIgnoredLayers)
        {
            Physics2D.IgnoreLayerCollision(swordLayer, layer, false);
        }

        _recallIgnoredLayers.Clear();
    }

    void EnsureTerrainCollisionsEnabled()
    {
        int swordLayer = gameObject.layer;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((terrainLayers.value & (1 << layer)) == 0)
            {
                continue;
            }

            Physics2D.IgnoreLayerCollision(swordLayer, layer, false);
        }
    }

    void DoRecallSteer()
    {
        if (recallTarget == null || !recallTarget.gameObject.activeInHierarchy)
        {
            StopFlight();
            return;
        }

        Vector2 toTarget = (Vector2)recallTarget.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;
        float arrivalThreshold = Mathf.Max(recallCatchRadius, recallSpeed * Time.fixedDeltaTime);

        if (distance <= arrivalThreshold)
        {
            FinishRecallArrival(countAsCatch: true);
            return;
        }

        recallElapsed += Time.fixedDeltaTime;
        if (recallElapsed >= recallMaxDuration)
        {
            transform.position = recallTarget.position;
            FinishRecallArrival(countAsCatch: false);
            return;
        }

        Vector2 direction = toTarget / distance;
        transform.up = direction;
        rb.linearVelocity = direction * recallSpeed;
        prevVelocity = rb.linearVelocity;
    }

    void FinishRecallArrival(bool countAsCatch)
    {
        if (_recallFinished)
        {
            return;
        }

        _recallFinished = true;

        Action<bool>? callback = onRecallArrived;
        onRecallArrived = null;
        isRecalling = false;

        callback?.Invoke(countAsCatch);
    }

    public void StickToTerrain()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        swingTrail.Stop();
        isFlying = false;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer(gearPhysicsLayer), true);
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
        if (rb == null)
        {
            return;
        }

        if (isRecalling)
        {
            DoRecallSteer();
            return;
        }

        if (!isFlying)
        {
            return;
        }

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
        if (!isFlying) return;
        ElementManager.Instance.OnRangedFlight(GameManager.Instance.player.transform, this);
        //DisplaySword();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isFlying || isRecalling) return;

        if ((terrainLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            StickToTerrain();
            return;
        }

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
        }
    }

    void OnBuffBegin(Element buff)
    {
        switch (buff)
        {
            case Element.Fire:
                break;
        }
    }
}
