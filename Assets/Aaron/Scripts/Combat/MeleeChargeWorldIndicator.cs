#nullable enable

using UnityEngine;

/// <summary>
/// Circular elemental particle burst centered on the player while melee charging.
/// </summary>
public class MeleeChargeWorldIndicator : MonoBehaviour
{
    [SerializeField] private GameObject? chargeVfxPrefab;
    [SerializeField] private float minVfxScale = 1.35f;
    [SerializeField] private float maxChargeVfxScale = 1.95f;
    [SerializeField] private float minEmissionRate = 40f;
    [SerializeField] private float maxChargeEmissionRate = 130f;
    [SerializeField] private float circleRadius = 0.55f;
    [SerializeField] private float circleThickness = 0.35f;
    [SerializeField] private float minParticleSize = 0.32f;
    [SerializeField] private float maxParticleSize = 0.58f;
    [SerializeField] private int particleSortingOrder = 20;
    [SerializeField] private float maxChargePulseDuration = 0.45f;
    [SerializeField] private float maxChargePulseScale = 0.08f;
    [SerializeField] private float maxChargePulseEmissionBoost = 25f;

    private GameObject? _vfxInstance;
    private ParticleSystem? _particleSystem;
    private MaxChargePulseTracker _maxChargePulse;

    private void Awake()
    {
        if (chargeVfxPrefab == null)
        {
            Debug.LogError($"{nameof(MeleeChargeWorldIndicator)}: chargeVfxPrefab is not assigned.");
        }
    }

    private void LateUpdate()
    {
        ElementManager? elementManager = ElementManager.Instance;
        if (elementManager == null
            || !elementManager.TryGetMeleeChargeDisplayState(out MeleeChargeDisplayState state))
        {
            Hide();
            return;
        }

        Show(state);
    }

    private void Show(MeleeChargeDisplayState state)
    {
        if (chargeVfxPrefab == null)
        {
            return;
        }

        if (_vfxInstance == null)
        {
            _vfxInstance = Instantiate(chargeVfxPrefab, transform);
            _vfxInstance.transform.localPosition = Vector3.zero;
            _vfxInstance.transform.localRotation = Quaternion.identity;

            _particleSystem = _vfxInstance.GetComponentInChildren<ParticleSystem>();
            if (_particleSystem == null)
            {
                Debug.LogError($"{nameof(MeleeChargeWorldIndicator)}: charge VFX prefab has no ParticleSystem.");
                Destroy(_vfxInstance);
                _vfxInstance = null;
                return;
            }

            ConfigureParticleSystem(_particleSystem);
        }

        if (_vfxInstance == null || _particleSystem == null)
        {
            return;
        }

        _vfxInstance.transform.localPosition = Vector3.zero;
        _vfxInstance.transform.localRotation = Quaternion.identity;
        ApplyChargeVisuals(state);

        if (!_vfxInstance.activeSelf)
        {
            _vfxInstance.SetActive(true);
        }

        if (!_particleSystem.isPlaying)
        {
            _particleSystem.Clear(true);
            _particleSystem.Play(true);
        }
    }

    private void ConfigureParticleSystem(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startLifetime = 0.55f;
        main.maxParticles = 400;
        main.playOnAwake = false;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = circleRadius;
        shape.radiusThickness = circleThickness;
        shape.arc = 360f;
        shape.rotation = Vector3.zero;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = minEmissionRate;

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.15f, 1f),
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.55f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = particleSortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.maxParticleSize = 2f;
    }

    private void ApplyChargeVisuals(MeleeChargeDisplayState state)
    {
        if (_particleSystem == null || _vfxInstance == null)
        {
            return;
        }

        float easedProgress = ElementVisualUtility.EaseOutQuad(state.Progress);
        float pulseEnvelope = ElementVisualUtility.StepMaxChargePulse(
            state.IsMaxCharge,
            ref _maxChargePulse,
            maxChargePulseDuration);

        ParticleSystem.MainModule main = _particleSystem.main;
        main.startColor = ElementVisualUtility.GetChargeParticleColor(state.Element, easedProgress);
        main.startSize = Mathf.Lerp(minParticleSize, maxParticleSize, easedProgress);

        ParticleSystem.EmissionModule emission = _particleSystem.emission;
        emission.rateOverTime = Mathf.Lerp(minEmissionRate, maxChargeEmissionRate, easedProgress)
            + pulseEnvelope * maxChargePulseEmissionBoost;

        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        shape.radius = Mathf.Lerp(circleRadius * 0.9f, circleRadius * 1.1f, easedProgress);

        float scale = Mathf.Lerp(minVfxScale, maxChargeVfxScale, easedProgress)
            * (1f + pulseEnvelope * maxChargePulseScale);
        _vfxInstance.transform.localScale = Vector3.one * scale;
    }

    private void Hide()
    {
        _maxChargePulse.Reset();

        if (_particleSystem == null)
        {
            return;
        }

        if (_particleSystem.isPlaying)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDestroy()
    {
        if (_vfxInstance != null)
        {
            Destroy(_vfxInstance);
        }
    }
}
