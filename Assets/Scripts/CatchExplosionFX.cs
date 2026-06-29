using System.Collections;
using UnityEngine;

public class CatchExplosionFX : MonoBehaviour
{
    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    static readonly int ColorCoreId = Shader.PropertyToID("_ColorCore");
    static readonly int ColorMidId = Shader.PropertyToID("_ColorMid");
    static readonly int ColorOuterId = Shader.PropertyToID("_ColorOuter");

    [SerializeField] private SpriteRenderer ringRenderer;
    [SerializeField] private float lifetime = 0.7f;
    [SerializeField] private float ringDuration = 0.45f;
    [SerializeField] private float ringStartProgress = 0.05f;

    Material _ringMaterial;
    Coroutine _ringCoroutine;

    public void Play(Color color)
    {
        if (ringRenderer != null)
        {
            PlayRing(color);
            return;
        }

        PlayParticles(color);
    }

    void PlayParticles(Color color)
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(includeInactive: true))
        {
            var main = ps.main;
            main.startColor = color;
            ps.Clear();
            ps.Play();
        }

        Destroy(gameObject, lifetime);
    }

    void PlayRing(Color tint)
    {
        if (_ringCoroutine != null)
        {
            StopCoroutine(_ringCoroutine);
        }

        ringRenderer.gameObject.SetActive(true);
        ringRenderer.SetPropertyBlock(null);
        _ringMaterial = ringRenderer.material;
        ApplyElementPalette(_ringMaterial, tint);
        _ringMaterial.SetFloat(ProgressId, ringStartProgress);

        _ringCoroutine = StartCoroutine(AnimateRing());
        Destroy(gameObject, lifetime);
    }

    static void ApplyElementPalette(Material material, Color tint)
    {
        material.SetColor(ColorCoreId, ScaleRgb(tint, 1.25f));
        material.SetColor(ColorMidId, tint);
        material.SetColor(ColorOuterId, ScaleRgb(tint, 0.45f));
        material.SetColor(TintColorId, Color.white);
    }

    static Color ScaleRgb(Color color, float factor)
    {
        return new Color(
            Mathf.Min(color.r * factor, 1f),
            Mathf.Min(color.g * factor, 1f),
            Mathf.Min(color.b * factor, 1f),
            color.a);
    }

    IEnumerator AnimateRing()
    {
        float elapsed = 0f;
        while (elapsed < ringDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ringDuration);
            float eased = 1f - (1f - t) * (1f - t);
            float progress = Mathf.Lerp(ringStartProgress, 1f, eased);

            _ringMaterial.SetFloat(ProgressId, progress);

            yield return null;
        }

        _ringMaterial.SetFloat(ProgressId, 1f);
        _ringCoroutine = null;
    }
}
