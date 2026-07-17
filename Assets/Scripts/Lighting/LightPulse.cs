#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gently oscillates a Light2D's intensity around its authored value so the light "breathes" instead of
/// sitting dead-flat. Good for portals, braziers, hazards. Purely cosmetic; no gameplay coupling.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightPulse : MonoBehaviour
{
    [Tooltip("Peak intensity added/subtracted from the authored base intensity.")]
    [SerializeField] private float amplitude = 0.2f;
    [Tooltip("Oscillation speed (radians/sec).")]
    [SerializeField] private float speed = 2.2f;

    private Light2D _light = null!;
    private float _baseIntensity;
    private float _phase;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        _baseIntensity = _light.intensity;
    }

    private void Update()
    {
        _phase += speed * Time.deltaTime;
        _light.intensity = _baseIntensity + Mathf.Sin(_phase) * amplitude;
    }
}
