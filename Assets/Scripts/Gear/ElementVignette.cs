#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[System.Serializable]
public struct ElementBorder
{
    public Element element;
    [Tooltip("Loops while this element is imbued. Child systems play and stop with it.")]
    public ParticleSystem? held;
    [Tooltip("A one-shot burst as a switch to this element lands. Child systems play with it.")]
    public ParticleSystem? flare;
}

/// <summary>
/// The screen's edges carry the imbued element, faintly: a soft tint in its colour, and its own particles along
/// the edges (embers, snowflakes, lightning streaks, wind streaks, dust, smoke wisps, sparkles) — enough that
/// the player always knows their element, never so much that it pulls their eye. As a switch lands
/// (ElementSwitchFX.OnSwitchLanded) the tint deepens for a moment, the element's particles burst once, and a
/// post-FX pulse — a bloom spike, a touch of chromatic aberration, the colour nudged toward the element —
/// washes over the frame.
/// </summary>
/// <remarks>
/// Everything visual is authored: the tint is Swordgear/Element Vignette on a full-screen image in this Screen
/// Space - Camera canvas, the particles are one prefab per element (Assets/Visuals/Prefabs/ElementFX/Border/)
/// placed under it, and the pulse is a second, higher-priority Volume whose weight an AnimationClip swells and
/// drops. This only turns them on and off, tints, and pulls triggers. The tint image is disabled with no
/// element, so the full-screen pass costs nothing then.
/// </remarks>
public class ElementVignette : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_ElementVignetteColor");
    private static readonly int HoldId = Shader.PropertyToID("_ElementVignetteHold");
    private static readonly int FlareTimeId = Shader.PropertyToID("_ElementVignetteFlareTime");
    private static readonly int PulseTrigger = Animator.StringToHash("Pulse");

    [Tooltip("The full-screen image drawing the edge tint (Swordgear/Element Vignette).")]
    [SerializeField] private Graphic? edgeTint;
    [SerializeField] private List<ElementBorder> borders = new();
    [Tooltip("The switch-pulse Volume (global, above the arena's own) whose weight the Animator animates.")]
    [SerializeField] private Volume? pulseVolume;
    [SerializeField] private Animator? pulseAnimator;
    [Tooltip("How far the pulse's colour filter leans toward the element (0 = none, 1 = the element's colour).")]
    [SerializeField, Range(0f, 1f)] private float colourNudge = 0.3f;

    private ColorAdjustments? pulseColour;

    private void Awake()
    {
        if (edgeTint == null)
        {
            Debug.LogError("ElementVignette: edgeTint is null", this);
        }

        if (pulseVolume == null)
        {
            Debug.LogError("ElementVignette: pulseVolume is null", this);
        }

        if (pulseAnimator == null)
        {
            Debug.LogError("ElementVignette: pulseAnimator is null", this);
        }

        // .profile, not .sharedProfile: the colour is set per switch and mustn't be written into the asset.
        if (pulseVolume != null && pulseVolume.profile.TryGet(out ColorAdjustments adjustments))
        {
            pulseColour = adjustments;
        }

        ResetGlobals();
    }

    private void OnEnable()
    {
        ElementSwitchFX.OnSwitchLanded += HandleSwitchLanded;
        ElementManager.OnActiveElementChanged += Show;

        // The element can be set before this arena's border exists; start from it rather than a blank edge.
        Show(ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical);
    }

    private void OnDisable()
    {
        ElementSwitchFX.OnSwitchLanded -= HandleSwitchLanded;
        ElementManager.OnActiveElementChanged -= Show;
    }

    private void OnDestroy()
    {
        // The globals outlive the arena; leave no border behind for the next scene.
        ResetGlobals();

        // Reading .profile made a per-arena copy of the pulse profile (and its effects), which the Volume never
        // frees; scenes load additively, so without this one would pile up per arena.
        if (pulseVolume != null && pulseVolume.HasInstantiatedProfile())
        {
            VolumeProfile copy = pulseVolume.profile;
            foreach (VolumeComponent component in copy.components)
            {
                Destroy(component);
            }

            Destroy(copy);
        }
    }

    private void HandleSwitchLanded(Element element)
    {
        // Show whatever is imbued now: a quicker flick may already have replaced the element that just landed.
        Show(ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : element);
        Shader.SetGlobalFloat(FlareTimeId, Time.time);   // the clock URP feeds the shader's _Time.y

        foreach (ElementBorder border in borders)
        {
            if (border.element == element && border.flare != null)
            {
                // Play() does nothing on a system that's still playing, so a quick re-flick would lose its burst:
                // restart it.
                border.flare.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                border.flare.Play(true);
            }
        }

        if (pulseColour != null)
        {
            pulseColour.colorFilter.value = Color.Lerp(Color.white, ElementVisuals.GetColor(element), colourNudge);
        }

        if (pulseAnimator != null)
        {
            pulseAnimator.SetTrigger(PulseTrigger);
        }
    }

    /// <summary> The edge shows the imbued element — its tint and its particles — and nothing with none. </summary>
    private void Show(Element element)
    {
        bool imbued = element != Element.Physical;
        Shader.SetGlobalColor(ColorId, ElementVisuals.GetColor(element));
        Shader.SetGlobalFloat(HoldId, imbued ? 1f : 0f);

        if (edgeTint != null)
        {
            edgeTint.enabled = imbued;
        }

        foreach (ElementBorder border in borders)
        {
            if (border.held == null)
            {
                continue;
            }

            bool mine = imbued && border.element == element;
            if (mine && !border.held.isEmitting)
            {
                border.held.Play(true);
            }
            else if (!mine && border.held.isEmitting)
            {
                // Let what's already out drift away rather than vanish.
                border.held.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private static void ResetGlobals()
    {
        Shader.SetGlobalFloat(HoldId, 0f);
        Shader.SetGlobalFloat(FlareTimeId, -100f);
    }
}
