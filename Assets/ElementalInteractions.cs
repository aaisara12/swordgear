using System.Collections.Generic;

public enum Element
{
    Physical,
    Fire,
    Ice,
    Lightning
}
public class ElementalInteractions
{


    public static Dictionary<Element, Dictionary<Element, float>> interactionMatrix =
        new Dictionary<Element, Dictionary<Element, float>>()
        {
            {
                Element.Physical, new Dictionary<Element, float>()
                {
                    { Element.Physical, 1f },
                    { Element.Fire, 0.5f },
                    { Element.Ice, 0.5f },
                    { Element.Lightning, 0.5f }
                }
            },
            {
                Element.Fire, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, .5f },
                    { Element.Ice, 2f },
                    { Element.Lightning, 1f }
                }
            },
            {
                Element.Ice, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, 1f },
                    { Element.Ice, .5f },
                    { Element.Lightning, 2f }
                }
            },
            {
                Element.Lightning, new Dictionary<Element, float>()
                {
                    { Element.Physical, 2f },
                    { Element.Fire, 2f },
                    { Element.Ice, 1f },
                    { Element.Lightning, 0.5f }
                }
            }
        };
}