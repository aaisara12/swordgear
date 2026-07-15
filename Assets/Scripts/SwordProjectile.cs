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
    [SerializeField] GameObject? recallTrailPrefab;
    GameObject? recallTrailInstance;
    [SerializeField] TrailRenderer? swingRibbon; // polished element-tinted ribbon streak (editor component on child "SwingRibbon")

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
    public bool IsLodged => gameObject.activeSelf && !isFlying && !isRecalling;

    [SerializeField] private SwordLodgedIndicator? lodgedIndicator;
    public bool IsLodgedIndicatorActive => lodgedIndicator != null && lodgedIndicator.IsActive;
    public Vector2 LodgedHiltWorldPosition =>
        lodgedIndicator != null ? lodgedIndicator.HiltWorldPosition : (Vector2)transform.position;

    static bool _swingTrailWarmed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D component is missing!");
        }
        Instance = this;

        if (lodgedIndicator == null)
        {
            lodgedIndicator = GetComponent<SwordLodgedIndicator>();
        }

        if (lodgedIndicator == null)
        {
            Debug.LogError("SwordProjectile: lodgedIndicator is null");
        }

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
        lodgedIndicator?.OnCleared();
        ClearRecallState();
        EnsureTerrainCollisionsEnabled();
        gameObject.SetActive(true);
        transform.position = position;
        transform.up = velocity.normalized;
        rb.linearVelocity = velocity;
        sprite.enabled = true;
        isFlying = true;
        StartSwingTrail(); // element-coloured streak for every thrown sword (was Fire-only)
    }

    public void StartRecallFlight(
        Transform player,
        float speed,
        float catchRadius,
        float maxDuration,
        Action<bool> onArrived)
    {
        lodgedIndicator?.OnRecallStarted();

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

        PlayRecallTrail();
    }

    public void StopFlight()
    {
        lodgedIndicator?.OnCleared();
        ClearRecallState();

        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer(gearPhysicsLayer), false);
        if (swingTrail != null)
        {
            swingTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // don't let a caught/recalled sword linger a trail
        }
        StopSwingRibbon();
        gameObject.SetActive(false);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        sprite.enabled = false;
        isFlying = false;
    }

    void ClearRecallState()
    {
        StopRecallTrail();
        isRecalling = false;
        recallTarget = null;
        onRecallArrived = null;
        recallElapsed = 0f;
        RestoreRecallCollisionIgnores();
    }

    void EnsureRecallTrail()
    {
        if (recallTrailInstance != null || recallTrailPrefab == null)
        {
            return;
        }

        recallTrailInstance = Instantiate(recallTrailPrefab, transform);
        recallTrailInstance.transform.localPosition = Vector3.zero;
        recallTrailInstance.transform.localRotation = Quaternion.identity;
    }

    void PlayRecallTrail()
    {
        EnsureRecallTrail();
        if (recallTrailInstance == null)
        {
            return;
        }

        Color tint = ElementVisuals.GetGlowColor(ElementVisuals.GetCurrentElement());
        foreach (ParticleSystem ps in recallTrailInstance.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startColor = tint;
            ps.Clear();
            ps.Play();
        }
    }

    void StopRecallTrail()
    {
        if (recallTrailInstance == null)
        {
            return;
        }

        foreach (ParticleSystem ps in recallTrailInstance.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
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

    public void StickToTerrain(Vector2? contactPoint = null)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        swingTrail.Stop();
        StopSwingRibbon();
        isFlying = false;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer(gearPhysicsLayer), true);
        lodgedIndicator?.OnLodged(contactPoint);
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

    /// <summary>Starts the swing trail for every thrown sword (was Fire-only): an element-tinted sparkle
    /// particle stream plus a polished ribbon streak with a hot, blooming head that tapers off behind.</summary>
    private void StartSwingTrail()
    {
        Element element = ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical;

        if (swingRibbon != null)
        {
            swingRibbon.Clear();
            swingRibbon.emitting = true;
        }
        // The old thin particle trail is replaced by the ribbon — keep it OFF (it was a Fire-only red streak
        // baked red via colour-over-lifetime, so it reads wrong on every other element).
        if (swingTrail != null)
        {
            swingTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ApplyRibbonColor(ElementVisuals.GetColor(element));
    }

    /// <summary>Tints the ribbon streak + sparkle particles to <paramref name="bladeColor"/> (hot blooming
    /// head that tapers/fades behind). Fed the blade's live colour every frame so the trail always matches it.</summary>
    private void ApplyRibbonColor(Color bladeColor)
    {
        if (swingRibbon != null)
        {
            Color hot = Color.Lerp(bladeColor, Color.white, 0.5f); // bright core that pops under bloom
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(hot, 0f),
                    new GradientColorKey(bladeColor, 0.4f),
                    new GradientColorKey(bladeColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f), // head (at the blade): bright
                    new GradientAlphaKey(0.55f, 0.4f),
                    new GradientAlphaKey(0f, 1f)     // tail: fade out
                });
            swingRibbon.colorGradient = g;
        }
    }

    private void StopSwingRibbon()
    {
        if (swingRibbon != null)
        {
            swingRibbon.emitting = false;
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
        if (!isRecalling)
        {
            ElementManager.Instance.OnRangedFlight(GameManager.Instance.player.transform, this);
            UpdateFlightVisuals();
        }
    }

    /// <summary>
    /// Drives the thrown blade's visuals from the LIVE imbue (ElementManager.ActiveElement) every frame, so
    /// colour / spin / ribbon update the instant the player switches element mid-flight — not after a catch.
    /// </summary>
    private void UpdateFlightVisuals()
    {
        if (sprite == null)
        {
            return;
        }

        // Colour the blade to the live imbue, then match the ribbon + sparkles to that EXACT blade colour
        // every frame — so the trail always agrees with the sword and recolours the instant the imbue changes.
        Element el = ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical;
        sprite.color = ElementVisuals.GetColor(el);
        ApplyRibbonColor(sprite.color);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isFlying || isRecalling) return;

        if ((terrainLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            Vector2 contactPoint = collision.ClosestPoint(transform.position);
            StickToTerrain(contactPoint);
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
