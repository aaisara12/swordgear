#nullable enable

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ElementArcLook
{
    public Element element;
    [Tooltip("The arc's shader look (a Swordgear/Gear Arc <Element> material): its tile, and its active look.")]
    public Material? material;
    [Tooltip("Optional. Loose pieces that fly off the arc while it's active (an ArcBits prefab): flame bits, " +
             "twinkles, sparks, swirls, boulders, notes…")]
    public ArcBits? bits;
}

/// <summary>
/// Everything the gear's arcs look like, in one place: the cartoon tile every arc shares and how arcs react
/// when aimed at, made active or granted; and per element, which material its arc uses and which loose pieces
/// fly off it while active. Edit this asset (and the materials and prefabs it points to) to change the arcs;
/// nothing about their look lives in code.
/// </summary>
/// <remarks>
/// The shared settings go to the arc shaders as globals (GearArcCommon.hlsl), so they apply to every arc at
/// once and update live while you edit them, in Play too.
/// </remarks>
[CreateAssetMenu(menuName = "Swordgear/Gear Arc Art", fileName = "GearArcArt")]
public class GearArcArt : ScriptableObject
{
    [Header("Shared tile (every arc)")]
    [Tooltip("The ink outline round each tile, world units.")]
    [SerializeField, Range(0f, 0.5f)] private float inkWidth = 0.16f;
    [Tooltip("The ink's colour: the element's colour this dark.")]
    [SerializeField, Range(0f, 1f)] private float inkTone = 0.28f;
    [Tooltip("How round the tile's corners are, world units.")]
    [SerializeField, Range(0f, 1.5f)] private float cornerRadius = 0.45f;
    [Tooltip("The shaded strip along the inner edge: the element's colour this dark…")]
    [SerializeField, Range(0f, 1f)] private float shadeTone = 0.72f;
    [Tooltip("…covering this share of the band, from the inner edge.")]
    [SerializeField, Range(0f, 1f)] private float shadeShare = 0.24f;
    [Tooltip("The lit strip along the outer edge starts this far across the band (0 inner, 1 outer)…")]
    [SerializeField, Range(0f, 1f)] private float litShare = 0.72f;
    [Tooltip("…and is the element's colour this far toward white.")]
    [SerializeField, Range(0f, 1f)] private float litTint = 0.38f;
    [Tooltip("Seconds between glints sweeping across an idle tile.")]
    [SerializeField, Range(0.5f, 30f)] private float shinePeriod = 6f;
    [Tooltip("How white the glint is.")]
    [SerializeField, Range(0f, 1f)] private float shineStrength = 0.55f;
    [Tooltip("Half-width of the glint's fat stripe, world units (its thin stripe is a third of it).")]
    [SerializeField, Range(0.02f, 1f)] private float shineWidth = 0.22f;

    [Header("States (every arc)")]
    [Tooltip("An aimed-at arc pushes out toward the flick this far, world units.")]
    [SerializeField, Range(0f, 1.5f)] private float aimSwell = 0.35f;
    [Tooltip("An aimed-at arc brightens by this much (above 1 is HDR, which blooms).")]
    [SerializeField, Range(0f, 4f)] private float aimBrightness = 0.9f;
    [Tooltip("The active (imbued) arc brightens by this much.")]
    [SerializeField, Range(0f, 4f)] private float activeBrightness = 0.6f;
    [Tooltip("Brightening stops when the element colour's brightest channel reaches this, so pale colours " +
             "keep their hue instead of clipping to white.")]
    [SerializeField, Range(0.5f, 3f)] private float maxBrightness = 1.15f;
    [Tooltip("The glowing outline round an aimed-at arc, world units outside the ink.")]
    [SerializeField, Range(0f, 1f)] private float haloWidth = 0.2f;
    [Tooltip("How bright that outline is (HDR).")]
    [SerializeField, Range(0f, 8f)] private float haloGlow = 3f;
    [Tooltip("When its element is granted, the arc pops outward this far, world units…")]
    [SerializeField, Range(0f, 3f)] private float flarePop = 0.9f;
    [Tooltip("…blazes this far toward white-hot…")]
    [SerializeField, Range(0f, 1f)] private float flareBlaze = 0.8f;
    [Tooltip("…and settles back at this rate, per second.")]
    [SerializeField, Range(1f, 20f)] private float flareDecay = 7f;

    [Header("Elements")]
    [Tooltip("The arc for an element with no entry below (a Swordgear/Gear Arc material).")]
    [SerializeField] private Material? defaultMaterial;
    [SerializeField] private List<ElementArcLook> elements = new();

    public Material? DefaultMaterial => defaultMaterial;

    private void OnEnable() => ApplyShared();

    private void OnValidate() => ApplyShared();

    /// <summary> Pushes the shared tile and state settings to the arc shaders. </summary>
    public void ApplyShared()
    {
        Shader.SetGlobalFloat("_ArcInkWidth", inkWidth);
        Shader.SetGlobalFloat("_ArcInkTone", inkTone);
        Shader.SetGlobalFloat("_ArcCornerRadius", cornerRadius);
        Shader.SetGlobalFloat("_ArcShadeTone", shadeTone);
        Shader.SetGlobalFloat("_ArcShadeShare", shadeShare);
        Shader.SetGlobalFloat("_ArcLitShare", litShare);
        Shader.SetGlobalFloat("_ArcLitTint", litTint);
        Shader.SetGlobalFloat("_ArcShinePeriod", shinePeriod);
        Shader.SetGlobalFloat("_ArcShineStrength", shineStrength);
        Shader.SetGlobalFloat("_ArcShineWidth", shineWidth);
        Shader.SetGlobalFloat("_ArcSwell", aimSwell);
        Shader.SetGlobalFloat("_ArcHighlightBoost", aimBrightness);
        Shader.SetGlobalFloat("_ArcActiveBoost", activeBrightness);
        Shader.SetGlobalFloat("_ArcMaxBrightness", maxBrightness);
        Shader.SetGlobalFloat("_ArcHaloWidth", haloWidth);
        Shader.SetGlobalFloat("_ArcHaloGlow", haloGlow);
        Shader.SetGlobalFloat("_ArcFlarePop", flarePop);
        Shader.SetGlobalFloat("_ArcFlareBlaze", flareBlaze);
        Shader.SetGlobalFloat("_ArcFlareDecay", flareDecay);
    }

    /// <summary> The look for an element; false when it has no entry (use <see cref="DefaultMaterial"/>). </summary>
    public bool TryGetLook(Element element, out ElementArcLook look)
    {
        foreach (ElementArcLook entry in elements)
        {
            if (entry.element == element)
            {
                look = entry;
                return true;
            }
        }

        look = default;
        return false;
    }
}
