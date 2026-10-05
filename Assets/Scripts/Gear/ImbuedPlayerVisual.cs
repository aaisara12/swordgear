#nullable enable

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ElementAura
{
    public Element element;
    public ParticleSystem? particles;
}

/// <summary>
/// The player carries their imbue. While an element is imbued the held sword takes its colour and a glowing
/// silhouette in it, and a light aura of the element's particles hangs round the player (embers, snowflakes,
/// sparks, wind streaks, dust motes, smoke wisps, opal sparkles).
/// </summary>
/// <remarks>
/// The glow and auras are authored in Player.prefab; this only tints, starts and stops them.
/// </remarks>
public class ImbuedPlayerVisual : MonoBehaviour
{
    [Tooltip("The held sword (PlayerWeaponIndicator's renderer); tinted while imbued.")]
    [SerializeField] private SpriteRenderer? swordRenderer;
    [Tooltip("An unlit copy of the sword, scaled up behind it: its glowing silhouette.")]
    [SerializeField] private SpriteRenderer? glowRenderer;
    [SerializeField] private List<ElementAura> auras = new();
    [Tooltip("How far the sword's colour leans toward the element.")]
    [SerializeField, Range(0f, 1f)] private float tintStrength = 0.75f;
    [Tooltip("HDR brightness of the glow, so it blooms.")]
    [SerializeField] private float glowIntensity = 2.5f;

    private bool glowLit;

    private void Awake()
    {
        if (swordRenderer == null)
        {
            Debug.LogError("ImbuedPlayerVisual: swordRenderer is null", this);
        }

        if (glowRenderer == null)
        {
            Debug.LogError("ImbuedPlayerVisual: glowRenderer is null", this);
        }
    }

    private void OnEnable()
    {
        ElementManager.OnActiveElementChanged += Show;
        Show(ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical);
    }

    private void OnDisable()
    {
        ElementManager.OnActiveElementChanged -= Show;
    }

    private void LateUpdate()
    {
        // The glow belongs to the held sword: PlayerWeaponIndicator hides the sword while it's thrown.
        if (glowRenderer != null)
        {
            glowRenderer.enabled = glowLit && swordRenderer != null && swordRenderer.enabled;
        }
    }

    private void Show(Element element)
    {
        bool imbued = element != Element.Physical;
        Color colour = ElementVisuals.GetColor(element);

        if (swordRenderer != null)
        {
            swordRenderer.color = imbued ? Color.Lerp(Color.white, colour, tintStrength) : Color.white;
        }

        if (glowRenderer != null)
        {
            glowRenderer.color = new Color(colour.r * glowIntensity, colour.g * glowIntensity, colour.b * glowIntensity, 1f);
        }

        glowLit = imbued;

        foreach (ElementAura aura in auras)
        {
            if (aura.particles == null)
            {
                continue;
            }

            if (imbued && aura.element == element)
            {
                aura.particles.Play(true);
            }
            else
            {
                aura.particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
