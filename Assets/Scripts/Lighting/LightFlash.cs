#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A momentary intensity burst on a Light2D that eases back to its authored value. Call Flash() on a
/// reactive beat (dash, impact, cast) for a punchy pop of light. Layered on top of the base intensity, so
/// it composes with a static light without clobbering its authored value.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlash : MonoBehaviour
{
    [Tooltip("Intensity added on top of the base at the peak of a flash.")]
    [SerializeField] private float flashIntensity = 1.5f;
    [Tooltip("How fast the flash falls back to base (higher = snappier).")]
    [SerializeField] private float decay = 9f;

    private Light2D _light = null!;
    private float _baseIntensity;
    private float _extra;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        _baseIntensity = _light.intensity;
    }

    /// <summary>Pop the light using the serialized flash intensity.</summary>
    public void Flash() => Flash(flashIntensity);

    /// <summary>Pop the light by a specific amount (takes the max with any in-progress flash).</summary>
    public void Flash(float amount)
    {
        _extra = Mathf.Max(_extra, amount);
    }

    private void Update()
    {
        if (_extra > 0f)
        {
            _extra = Mathf.Lerp(_extra, 0f, 1f - Mathf.Exp(-decay * Time.deltaTime));
            if (_extra < 0.01f)
            {
                _extra = 0f;
            }
            _light.intensity = _baseIntensity + _extra;
        }
    }
}
