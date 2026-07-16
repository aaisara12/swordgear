#nullable enable

using UnityEngine;

/// <summary>Optional glow + trail for pooled enemy projectiles. Authored on Arrow.prefab.</summary>
public class EnemyProjectileVisual : MonoBehaviour, IPoolReset
{
    [SerializeField] private SpriteRenderer? glowRenderer;
    [SerializeField] private ParticleSystem? trailParticles;
    [SerializeField] private float glowScale = 1.55f;

    private static readonly Color PhysicalColor = new Color(0.95f, 0.98f, 1f, 1f);
    private static readonly Color FireColor = new Color(1f, 0.45f, 0.1f, 1f);
    private static readonly Color IceColor = new Color(0.45f, 0.9f, 1f, 1f);
    private static readonly Color LightningColor = new Color(1f, 0.98f, 0.35f, 1f);
    private static readonly Color WindColor = new Color(0.5f, 1f, 0.55f, 1f);

    public void OnSpawned()
    {
    }

    public void OnReleased()
    {
        if (trailParticles != null)
        {
            trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void Apply(Element element)
    {
        Color color = GetElementColor(element);

        if (glowRenderer != null)
        {
            glowRenderer.color = new Color(color.r, color.g, color.b, 0.42f);
        }

        if (trailParticles != null)
        {
            ParticleSystem.MainModule main = trailParticles.main;
            main.startColor = color;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = trailParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;

            trailParticles.Clear(true);
            trailParticles.Play(true);
        }
    }

    private static Color GetElementColor(Element element)
    {
        return element switch
        {
            Element.Fire => FireColor,
            Element.Ice => IceColor,
            Element.Lightning => LightningColor,
            Element.Wind => WindColor,
            _ => PhysicalColor,
        };
    }
}
