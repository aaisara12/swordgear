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
            Element.Wind => new Color(0.56f, 0.93f, 0.56f, 1f), // light green
            Element.Earth => new Color(0.76f, 0.52f, 0.24f, 1f), // amber / brown
            Element.Dark => new Color(0.34f, 0.11f, 0.52f, 1f), // deep violet
            // Opal's milky lilac-pearl base. The real opal shimmer is the Opalite shader; this is the flat
            // stand-in for everything that can only take a colour. Must stay distinct from Physical's
            // white-cyan below.
            Element.Light => new Color(0.95f, 0.86f, 1f, 1f),
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
