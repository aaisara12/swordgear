#nullable enable

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The whole screen takes part in an element switch. As the switch lands (ElementSwitchFX.OnSwitchLanded) the
/// screen's edges flare in the element (a cartoon border in its shape: flames, icicles, crackle…) and settle
/// to a thin edge held while the imbue lasts, thinning away over its last quarter; and a post-FX pulse — a
/// bloom spike, a touch of chromatic aberration, the colour nudged toward the element — washes over the frame.
/// </summary>
/// <remarks>
/// The border is Swordgear/Element Vignette on a full-screen image in this Screen Space - Camera canvas, so it
/// sits under the HUD and blooms with the scene. It's driven through shader globals and animates itself from
/// the flare's time. The pulse is a second, higher-priority Volume whose weight an authored AnimationClip
/// swells and drops; this only tints it and pulls the trigger.
/// </remarks>
public class ElementVignette : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_ElementVignetteColor");
    private static readonly int StyleId = Shader.PropertyToID("_ElementVignetteStyle");
    private static readonly int HoldId = Shader.PropertyToID("_ElementVignetteHold");
    private static readonly int FlareTimeId = Shader.PropertyToID("_ElementVignetteFlareTime");
    private static readonly int PulseTrigger = Animator.StringToHash("Pulse");

    [Tooltip("The switch-pulse Volume (global, above the arena's own) whose weight the Animator animates.")]
    [SerializeField] private Volume? pulseVolume;
    [SerializeField] private Animator? pulseAnimator;
    [Tooltip("How far the pulse's colour filter leans toward the element (0 = none, 1 = the element's colour).")]
    [SerializeField, Range(0f, 1f)] private float colourNudge = 0.3f;
    [Tooltip("The share of the imbue, at its end, over which the held edge thins away.")]
    [SerializeField, Range(0.05f, 1f)] private float holdFadeShare = 0.25f;

    private GameManager? timerSource;
    private ColorAdjustments? pulseColour;

    private void Awake()
    {
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
        ElementManager.OnActiveElementChanged += HandleActiveElementChanged;
    }

    private void OnDisable()
    {
        ElementSwitchFX.OnSwitchLanded -= HandleSwitchLanded;
        ElementManager.OnActiveElementChanged -= HandleActiveElementChanged;
        TrackImbueTimer(null);
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

    private void Update()
    {
        TrackImbueTimer(GameManager.Instance);
    }

    private void HandleSwitchLanded(Element element)
    {
        ShowElement(element);
        Shader.SetGlobalFloat(FlareTimeId, Time.time);   // the clock URP feeds the shader's _Time.y

        if (pulseColour != null)
        {
            pulseColour.colorFilter.value = Color.Lerp(Color.white, ElementVisuals.GetColor(element), colourNudge);
        }

        if (pulseAnimator != null)
        {
            pulseAnimator.SetTrigger(PulseTrigger);
        }
    }

    private void HandleActiveElementChanged(Element element)
    {
        ShowElement(element);

        if (element == Element.Physical)
        {
            Shader.SetGlobalFloat(HoldId, 0f);
        }
    }

    /// <summary> The held edge: full while imbued, thinning away over the imbue's last stretch. </summary>
    private void HandleImbueTimerChanged(float remaining, float duration)
    {
        float hold = duration > 0f ? Mathf.Clamp01(remaining / (duration * holdFadeShare)) : 0f;
        Shader.SetGlobalFloat(HoldId, hold);
    }

    private static void ShowElement(Element element)
    {
        Shader.SetGlobalColor(ColorId, ElementVisuals.GetColor(element));
        Shader.SetGlobalFloat(StyleId, (int)element);
    }

    private static void ResetGlobals()
    {
        Shader.SetGlobalFloat(HoldId, 0f);
        Shader.SetGlobalFloat(FlareTimeId, -100f);
        Shader.SetGlobalFloat(StyleId, 0f);
    }

    /// <summary> Follows whichever GameManager is current; its imbue timer drives the held edge. </summary>
    private void TrackImbueTimer(GameManager? source)
    {
        if (timerSource == source)
        {
            return;
        }

        if (timerSource is not null)
        {
            timerSource.OnEmpowermentTimerChanged -= HandleImbueTimerChanged;
        }

        timerSource = source;

        if (timerSource is not null)
        {
            timerSource.OnEmpowermentTimerChanged += HandleImbueTimerChanged;
        }
    }
}
