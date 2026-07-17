#nullable enable

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives a Light2D's colour from the player's currently-active element, so an imbued sword casts light of
/// its element (fire = orange, ice = cyan, lightning = yellow, ...). Reactive polish; no gameplay effect.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class ElementLightTint : MonoBehaviour
{
    private Light2D _light = null!;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
    }

    private void OnEnable()
    {
        ElementManager.OnActiveElementChanged += Apply;
        if (ElementManager.Instance != null)
        {
            Apply(ElementManager.Instance.ActiveElement);
        }
    }

    private void OnDisable()
    {
        ElementManager.OnActiveElementChanged -= Apply;
    }

    private void Apply(Element element)
    {
        _light.color = ElementVisuals.GetColor(element);
    }
}
