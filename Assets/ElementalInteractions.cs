using System.Collections.Generic;

public enum Element
{
    // Serialized as ints in prefabs and .assets — only ever APPEND. Inserting a value
    // silently remaps every existing reference to the elements after it.
    Physical,
    Fire,
    Ice,
    Lightning,
    Wind,
    Earth,
    Dark,
    Light
}
public class ElementalInteractions
{
    /// <summary> Multiplier for any pair with no explicit entry below. </summary>
    public const float NeutralMultiplier = 1f;

    /// <summary>
    /// Look up [attacker][defender], falling back to <see cref="NeutralMultiplier"/> for any pair the
    /// table doesn't cover. Callers must use this rather than indexing the matrix directly — a missing
    /// pair would otherwise throw KeyNotFoundException mid-combat.
    /// </summary>
    public static float GetMultiplier(Element attacker, Element defender)
    {
        return interactionMatrix.TryGetValue(attacker, out var row) && row.TryGetValue(defender, out float multiplier)
            ? multiplier
            : NeutralMultiplier;
    }

    // Earth, Dark and Light are deliberately absent: they are neutral (1x) in both directions until the
    // Elemental Affinities rework decides whether the counter-cycle survives at all. Adding a row here
    // is how you opt an element into the matrix.
    public static Dictionary<Element, Dictionary<Element, float>> interactionMatrix =
        new Dictionary<Element, Dictionary<Element, float>>()
        {
            {
                Element.Physical, new Dictionary<Element, float>()
                {
                    { Element.Physical, 1f },
                    { Element.Fire, 0.5f },
                    { Element.Ice, 0.5f },
                    { Element.Lightning, 0.5f },
                    { Element.Wind, 0.5f }
                }
            },
            {
                // Counter cycle: Fire > Ice > Lightning > Wind > Fire (">" = counters, i.e. 2x damage;
                // the reverse pairing is the weak side at 0.5x; non-adjacent pairs are neutral at 1x).
                Element.Fire, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, .5f },
                    { Element.Ice, 2f },
                    { Element.Lightning, 1f },
                    { Element.Wind, 0.5f }
                }
            },
            {
                Element.Ice, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, 0.5f },
                    { Element.Ice, .5f },
                    { Element.Lightning, 2f },
                    { Element.Wind, 1f }
                }
            },
            {
                Element.Lightning, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, 1f },
                    { Element.Ice, 0.5f },
                    { Element.Lightning, 0.5f },
                    { Element.Wind, 2f }
                }
            },
            {
                Element.Wind, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, 2f },
                    { Element.Ice, 1f },
                    { Element.Lightning, 0.5f },
                    { Element.Wind, 0.5f }
                }
            }
        };
}