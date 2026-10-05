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
/// Everything the gear's arcs look like, in one place: which material each element's arc uses and which loose
/// pieces fly off it while active. Edit this asset (and the materials and prefabs it points to) to change the
/// arcs; nothing about their look lives in code.
/// </summary>
[CreateAssetMenu(menuName = "Swordgear/Gear Arc Art", fileName = "GearArcArt")]
public class GearArcArt : ScriptableObject
{
    [Tooltip("The arc for an element with no entry below (a Swordgear/Gear Arc material).")]
    [SerializeField] private Material? defaultMaterial;
    [SerializeField] private List<ElementArcLook> elements = new();

    public Material? DefaultMaterial => defaultMaterial;

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
