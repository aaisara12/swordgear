using UnityEngine;

public static class ElementVisuals
{
    public static Color GetColor(Element element)
    {
        return element switch
        {
            Element.Fire => Color.red,
            Element.Lightning => Color.yellow,
            Element.Ice => Color.cyan,
            _ => new Color(0.85f, 1f, 1f, 1f), // bright white-cyan for Physical
        };
    }

    public static Color GetGlowColor(Element element)
    {
        Color baseColor = GetColor(element);
        return new Color(
            Mathf.Min(baseColor.r * 1.35f, 1f),
            Mathf.Min(baseColor.g * 1.35f, 1f),
            Mathf.Min(baseColor.b * 1.35f, 1f),
            baseColor.a);
    }

    public static Element GetCurrentElement()
    {
        return GameManager.Instance != null
            ? GameManager.Instance.currentElement
            : Element.Physical;
    }
}
