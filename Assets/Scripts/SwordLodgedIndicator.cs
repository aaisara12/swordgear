#nullable enable

using System;
using UnityEngine;

/// <summary>
/// World-space VFX when the thrown sword lodges in terrain: impact burst, idle hilt glow,
/// catch ring, proximity tether, and recall-channel escalation.
/// </summary>
public class SwordLodgedIndicator : MonoBehaviour
{
    public static event Action? OnSwordLodged;

    [Header("Anchors")]
    [Tooltip("Local offset toward the grip (along -transform.up). Particles and tether end use this; catch ring uses pivot.")]
    [SerializeField] private Vector2 hiltLocalOffset = new Vector2(0f, -0.35f);
    [Tooltip("Local offset to the blade tip (along +transform.up) where the wall-impact VFX spawns.")]
    [SerializeField] private Vector2 tipLocalOffset = new Vector2(0f, 0.5f);
    [Tooltip("Raises the lodged hilt FX in WORLD/screen up (independent of the sword's angle).")]
    [SerializeField] private float hiltFxWorldRaise = 0.25f;

    [Header("Proximity")]
    [SerializeField] private float maxTetherRange = 4f;
    [SerializeField] private float recallEscalationMultiplier = 1.5f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseScaleAmplitude = 0.06f;
    [SerializeField] private float basePulseAlpha = 0.85f;
    [SerializeField] private float maxTetherWidth = 0.12f;
    [Tooltip("Minimum tether visibility when the sword is far, so the guide line is always on. 0 = fade out past maxTetherRange like before.")]
    [SerializeField] private float minTetherIntensity = 0.35f;

    [Header("References")]
    [SerializeField] private SpriteRenderer? swordSprite;
    [SerializeField] private GameObject? hiltFxPrefab;
    [SerializeField] private GameObject? catchRingPrefab;
    [SerializeField] private GameObject? stickImpactPrefab;
    [SerializeField] private LineRenderer? tetherLine;

    Transform? _hiltFxRoot;
    SpriteRenderer? _catchRingRenderer;
    Vector3 _defaultSpriteScale = Vector3.one;
    Color _defaultSpriteColor = Color.white;
    bool _isActive;
    bool _hiltFxWarmed;
    float _pulsePhase;
    Element _lastTintElement = Element.Physical;

    public bool IsActive => _isActive;

    public Vector2 HiltWorldPosition => transform.TransformPoint(hiltLocalOffset);

    public Vector2 TipWorldPosition => transform.TransformPoint(tipLocalOffset);

    public Vector2 PivotWorldPosition => transform.position;

    void Awake()
    {
        if (swordSprite == null)
        {
            SwordProjectile? projectile = GetComponent<SwordProjectile>();
            if (projectile != null && projectile.spriteObject != null)
            {
                swordSprite = projectile.spriteObject.GetComponent<SpriteRenderer>();
            }
        }

        if (swordSprite != null)
        {
            _defaultSpriteScale = swordSprite.transform.localScale;
            _defaultSpriteColor = swordSprite.color;
        }

        EnsureTetherLine();
        EnsureCachedChildFx();
    }

    void EnsureTetherLine()
    {
        if (tetherLine != null)
        {
            return;
        }

        tetherLine = gameObject.GetComponent<LineRenderer>();
        if (tetherLine == null)
        {
            tetherLine = gameObject.AddComponent<LineRenderer>();
        }

        tetherLine.positionCount = 2;
        tetherLine.useWorldSpace = true;
        tetherLine.loop = false;
        tetherLine.numCapVertices = 4;
        tetherLine.sortingOrder = 50;
        tetherLine.startWidth = 0f;
        tetherLine.endWidth = 0f;
        tetherLine.enabled = false;

        Material? lineMat = new Material(Shader.Find("Sprites/Default"));
        if (lineMat != null)
        {
            tetherLine.material = lineMat;
        }
    }

    void EnsureCachedChildFx()
    {
        if (_hiltFxRoot == null && hiltFxPrefab != null)
        {
            GameObject instance = Instantiate(hiltFxPrefab, transform);
            instance.name = "LodgedHiltFX";
            _hiltFxRoot = instance.transform;
            PlaceHiltFx();
            _hiltFxRoot.gameObject.SetActive(false);
        }

        if (_hiltFxRoot == null)
        {
            _hiltFxRoot = CreateRuntimeHiltFx();
        }

        if (_catchRingRenderer == null && catchRingPrefab != null)
        {
            GameObject instance = Instantiate(catchRingPrefab, transform);
            instance.name = "CatchRing";
            _catchRingRenderer = instance.GetComponent<SpriteRenderer>();
            if (_catchRingRenderer == null)
            {
                _catchRingRenderer = instance.GetComponentInChildren<SpriteRenderer>();
            }

            instance.SetActive(false);
        }

        if (_catchRingRenderer == null)
        {
            _catchRingRenderer = CreateProceduralCatchRing();
        }
    }

    SpriteRenderer CreateProceduralCatchRing()
    {
        GameObject ringGo = new GameObject("CatchRing");
        ringGo.transform.SetParent(transform, false);
        SpriteRenderer renderer = ringGo.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite(64);
        renderer.sortingOrder = 45;
        renderer.color = new Color(1f, 1f, 1f, 0.35f);
        ringGo.SetActive(false);
        return renderer;
    }

    static Texture2D? _hiltDotTexture;

    // Soft round glow dot (radial alpha falloff) so particles read as embers, not squares.
    static Texture2D SoftDotTexture()
    {
        if (_hiltDotTexture != null)
        {
            return _hiltDotTexture;
        }

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float r = size * 0.5f;
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;
                float a = Mathf.Clamp01(1f - d);
                a *= a; // soft falloff toward the edge
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        _hiltDotTexture = tex;
        return tex;
    }

    Transform CreateRuntimeHiltFx()
    {
        GameObject root = new GameObject("LodgedHiltFX");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = hiltLocalOffset;

        ParticleSystem ps = root.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.75f;
        main.startSpeed = 0.7f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.45f); // big, clearly visible embers
        main.maxParticles = 90;
        main.gravityModifier = -0.3f;     // rise well up off the hilt
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 18f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.16f;

        // Fade in fast then out over life for a soft glow (particle startColor is tinted per element elsewhere).
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Shrink as they rise so they twinkle out.
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Runtime ParticleSystems have NO material (Unity draws magenta). Use the URP-safe sprite shader with
        // a soft dot so the hilt reads as glowing embers; bright element colours bloom via the arena post-FX.
        Shader particleShader = Shader.Find("Sprites/Default");
        if (particleShader != null)
        {
            Material mat = new Material(particleShader) { mainTexture = SoftDotTexture() };
            renderer.material = mat;
        }
        renderer.sortingOrder = 48;

        root.SetActive(false);
        return root.transform;
    }

    static Sprite? _circleSprite;

    static Sprite CreateCircleSprite(int size)
    {
        if (_circleSprite != null)
        {
            return _circleSprite;
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.48f;
        float innerRadius = size * 0.38f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = dist <= outerRadius && dist >= innerRadius ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        _circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _circleSprite;
    }

    public void OnLodged(Vector2? contactPoint)
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _pulsePhase = 0f;

        // Spawn the impact at the blade TIP (following the sword's rotation + embedded position), not the
        // collider contact point — the contact point lands wherever the collider edges touched, which reads
        // as off from where the blade actually meets the wall.
        SpawnStickImpact(TipWorldPosition);
        PlayStickSound();
        OnSwordLodged?.Invoke();

        EnsureCachedChildFx();
        WarmAndPlayHiltFx();
        SetCatchRingVisible(false);
        SetTetherVisible(false);
    }

    public void OnRecallStarted()
    {
        OnCleared();
    }

    public void OnCleared()
    {
        if (!_isActive && _hiltFxRoot == null && (_catchRingRenderer == null || !_catchRingRenderer.gameObject.activeSelf))
        {
            RestoreSpriteDefaults();
            SetTetherVisible(false);
            return;
        }

        _isActive = false;
        StopHiltFx();
        SetCatchRingVisible(false);
        SetTetherVisible(false);
        RestoreSpriteDefaults();
    }

    void LateUpdate()
    {
        if (!_isActive)
        {
            return;
        }

        SwordProjectile? projectile = SwordProjectile.Instance;
        if (projectile != null && projectile.IsRecalling)
        {
            SetTetherVisible(false);
            SetCatchRingVisible(false);
            return;
        }

        GameObject? playerGo = GameManager.Instance?.player;
        if (playerGo == null || !playerGo.TryGetComponent(out PlayerController playerController))
        {
            ApplyBaseIdlePulse(0f, 1f);
            SetTetherVisible(false);
            SetCatchRingVisible(false);
            return;
        }

        Vector2 playerPos = playerGo.transform.position;
        float catchRadius = playerController.SwordCatchRadius;
        float distanceToPivot = Vector2.Distance(playerPos, PivotWorldPosition);
        float proximity = 1f - Mathf.Clamp01(distanceToPivot / maxTetherRange);
        float recallBoost = playerController.IsRecallChannelActive ? recallEscalationMultiplier : 1f;
        float intensity = proximity * recallBoost;

        ApplyElementTint();
        ApplyBaseIdlePulse(proximity, recallBoost);
        UpdateHiltFxIntensity(intensity);
        UpdateCatchRing(distanceToPivot, catchRadius, intensity);
        UpdateTether(playerPos, intensity);
    }

    void ApplyElementTint()
    {
        Element element = ElementVisuals.GetCurrentElement();
        if (element == _lastTintElement)
        {
            return;
        }

        _lastTintElement = element;
        Color tint = ElementVisuals.GetGlowColor(element);

        if (_catchRingRenderer != null)
        {
            Color ringColor = _catchRingRenderer.color;
            ringColor.r = tint.r;
            ringColor.g = tint.g;
            ringColor.b = tint.b;
            _catchRingRenderer.color = ringColor;
        }

        TintParticleSystems(_hiltFxRoot, tint);
    }

    static void TintParticleSystems(Transform? root, Color tint)
    {
        if (root == null)
        {
            return;
        }

        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            Color startColor = main.startColor.color;
            startColor.r = tint.r;
            startColor.g = tint.g;
            startColor.b = tint.b;
            main.startColor = startColor;
        }
    }

    void ApplyBaseIdlePulse(float proximity, float recallBoost)
    {
        if (swordSprite == null)
        {
            return;
        }

        _pulsePhase += Time.deltaTime * pulseSpeed * recallBoost;
        float wave = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
        float amplitude = pulseScaleAmplitude * (0.35f + proximity * 0.65f) * recallBoost;
        float scale = 1f + amplitude * wave;
        swordSprite.transform.localScale = _defaultSpriteScale * scale;

        Color color = _defaultSpriteColor;
        color.a = Mathf.Clamp01(basePulseAlpha + proximity * 0.15f * recallBoost);
        swordSprite.color = color;
    }

    // Places the hilt FX at the grip, then lifts it in WORLD up (screen up) regardless of the sword's angle.
    void PlaceHiltFx()
    {
        if (_hiltFxRoot == null)
        {
            return;
        }

        _hiltFxRoot.localPosition = hiltLocalOffset;
        if (!Mathf.Approximately(hiltFxWorldRaise, 0f))
        {
            Vector3 p = _hiltFxRoot.position;
            p.y += hiltFxWorldRaise;
            _hiltFxRoot.position = p;
        }
    }

    void UpdateHiltFxIntensity(float intensity)
    {
        if (_hiltFxRoot == null)
        {
            return;
        }

        PlaceHiltFx();
        foreach (ParticleSystem ps in _hiltFxRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = 4f + intensity * 12f;
        }
    }

    void UpdateCatchRing(float distanceToPivot, float catchRadius, float intensity)
    {
        if (_catchRingRenderer == null)
        {
            return;
        }

        bool inRange = distanceToPivot <= catchRadius;
        _catchRingRenderer.gameObject.SetActive(inRange);
        if (!inRange)
        {
            return;
        }

        _catchRingRenderer.transform.position = PivotWorldPosition;
        float diameter = catchRadius * 2f;
        _catchRingRenderer.transform.localScale = Vector3.one * diameter;

        Color color = _catchRingRenderer.color;
        float pulse = (Mathf.Sin(_pulsePhase * 1.5f) + 1f) * 0.5f;
        color.a = Mathf.Clamp01((0.25f + pulse * 0.35f) * intensity);
        _catchRingRenderer.color = color;
    }

    void UpdateTether(Vector2 playerPos, float intensity)
    {
        if (tetherLine == null)
        {
            return;
        }

        // Always show the tether as a guide back to the lodged sword; brighten/thicken as the player nears
        // it, but never fully fade so the sword is always findable.
        float display = Mathf.Max(intensity, minTetherIntensity);

        Color tint = ElementVisuals.GetGlowColor(_lastTintElement);
        float alpha = Mathf.Clamp01(display * tint.a);
        Color startColor = new Color(tint.r, tint.g, tint.b, alpha * 0.25f);
        Color endColor = new Color(tint.r, tint.g, tint.b, alpha);

        tetherLine.enabled = true;
        tetherLine.startColor = startColor;
        tetherLine.endColor = endColor;
        tetherLine.startWidth = maxTetherWidth * display * 0.5f;
        tetherLine.endWidth = maxTetherWidth * display;
        tetherLine.SetPosition(0, playerPos);
        tetherLine.SetPosition(1, HiltWorldPosition);
    }

    void SetTetherVisible(bool visible)
    {
        if (tetherLine != null)
        {
            tetherLine.enabled = visible;
        }
    }

    void SetCatchRingVisible(bool visible)
    {
        if (_catchRingRenderer != null)
        {
            _catchRingRenderer.gameObject.SetActive(visible);
        }
    }

    void RestoreSpriteDefaults()
    {
        if (swordSprite == null)
        {
            return;
        }

        swordSprite.transform.localScale = _defaultSpriteScale;
        swordSprite.color = _defaultSpriteColor;
    }

    void WarmAndPlayHiltFx()
    {
        if (_hiltFxRoot == null)
        {
            return;
        }

        PlaceHiltFx();
        _hiltFxRoot.gameObject.SetActive(true);

        foreach (ParticleSystem ps in _hiltFxRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!_hiltFxWarmed)
            {
                ps.Simulate(1f, true, true);
                ps.Clear(true);
            }

            ps.Play();
        }

        _hiltFxWarmed = true;
    }

    void StopHiltFx()
    {
        if (_hiltFxRoot == null)
        {
            return;
        }

        foreach (ParticleSystem ps in _hiltFxRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _hiltFxRoot.gameObject.SetActive(false);
    }

    void SpawnStickImpact(Vector3 position)
    {
        if (stickImpactPrefab == null)
        {
            return;
        }

        GameObject fx = Instantiate(stickImpactPrefab, position, Quaternion.identity);
        CatchExplosionFX? explosion = fx.GetComponent<CatchExplosionFX>();
        if (explosion != null)
        {
            explosion.Play(ElementVisuals.GetGlowColor(ElementVisuals.GetCurrentElement()));
        }
    }

    void PlayStickSound()
    {
        if (AudioSystem.TryPlay(AudioSystem.Sound.Sword_Stick))
        {
            return;
        }

        AudioSystem.Play(AudioSystem.Sound.Bounce);
    }
}
