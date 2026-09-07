#nullable enable

using TMPro;
using UnityEngine;

/// <summary>
/// Floating text above the player naming the stage of a multi-stage charge.
/// </summary>
/// <remarks>
/// Earth's charge builds a ballista for half a second before it starts charging a shot, and releasing
/// during the build fires nothing. Without the ballista actor to show it, the two stages look identical
/// and that rule is invisible — this says which one you're in until there's art that can.
/// <para>
/// The label is built in code rather than authored, so it needs no prefab or art. It mirrors
/// <see cref="MeleeChargeWorldIndicator"/>: poll the active weapon in LateUpdate, create the visual on
/// first use, hide it whenever there's nothing to say.
/// </para>
/// </remarks>
public class ChargePhaseLabel : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_FontAsset? font;
    [Tooltip("About 0.076 world units of cap height per point with this font. The player sprite is " +
             "2.63 units tall, so 5 gives a label roughly a seventh of their height.")]
    [SerializeField] private float fontSize = 5f;

    [Header("Placement")]
    [Tooltip("Height above the player's centre, in world units. The sprite is 2.63 tall, so this needs " +
             "to clear ~1.3 to sit above their head.")]
    [SerializeField] private float verticalOffset = 1.8f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 25;

    private TextMeshPro? label;

    private void LateUpdate()
    {
        ElementManager? elementManager = ElementManager.Instance;
        if (elementManager == null)
        {
            Hide();
            return;
        }

        string phase = elementManager.ChargePhaseLabel;
        if (string.IsNullOrEmpty(phase))
        {
            Hide();
            return;
        }

        TextMeshPro text = EnsureLabel();
        if (!text.gameObject.activeSelf)
        {
            text.gameObject.SetActive(true);
        }

        // TMP rebuilds its mesh on assignment, so only assign when the phase actually changed.
        if (text.text != phase)
        {
            text.text = phase;
        }

        text.color = ElementVisuals.GetColor(elementManager.ActiveElement);

        // Positioned in WORLD space every frame: the player rotates to face its attacks, and text that
        // rotated with them would end up upside down.
        Transform labelTransform = text.transform;
        labelTransform.position = transform.position + Vector3.up * verticalOffset;
        labelTransform.rotation = Quaternion.identity;
    }

    private void Hide()
    {
        if (label != null && label.gameObject.activeSelf)
        {
            label.gameObject.SetActive(false);
        }
    }

    private TextMeshPro EnsureLabel()
    {
        if (label != null)
        {
            return label;
        }

        var go = new GameObject("Charge Phase Label");
        go.transform.SetParent(transform, worldPositionStays: false);

        // Left at 1: font size alone controls how big the text is. Scaling the object as well made the
        // two fight, and the label ended up a tenth of the intended height and invisible in play.
        go.transform.localScale = Vector3.one;

        label = go.AddComponent<TextMeshPro>();
        if (font != null)
        {
            label.font = font;
        }

        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        // Auto-sized rects clip a longer phase name; this is wide enough for both and never wraps.
        label.rectTransform.sizeDelta = new Vector2(10f, 2f);

        var meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayer;
            meshRenderer.sortingOrder = sortingOrder;
        }

        return label;
    }
}
