#nullable enable

using System;

namespace Shop
{
    /// <summary>
    /// Every augment the player acquires carries an element, and it is baked into the inventory item ID so two
    /// copies of the same augment rolled as different elements stay distinct entries the ledger can count.
    /// Format: "@Fire|stat-MoveSpeed-5".
    /// </summary>
    public static class AugmentElementSerializer
    {
        // The tag is a PREFIX so the other ID serializers — which all scan for their own prefix and take the
        // remainder — keep parsing tagged IDs unchanged.
        private const char kTagOpen = '@';
        private const char kTagClose = '|';

        /// <summary> Elements an augment can roll. Physical is excluded: it advances no ultimate's requirements. </summary>
        public static readonly Element[] RollableElements =
        {
            Element.Fire, Element.Ice, Element.Lightning, Element.Wind
        };

        public static Element RollElement() =>
            RollableElements[UnityEngine.Random.Range(0, RollableElements.Length)];

        public static string Serialize(Element element, string baseId) =>
            $"{kTagOpen}{element}{kTagClose}{StripTag(baseId)}";

        public static bool TryDeserialize(string? id, out Element element)
        {
            element = default;

            if (string.IsNullOrEmpty(id) || id![0] != kTagOpen)
            {
                return false;
            }

            int close = id.IndexOf(kTagClose);
            if (close <= 1)
            {
                return false;
            }

            return Enum.TryParse(id.Substring(1, close - 1), ignoreCase: true, result: out element);
        }

        /// <summary> Returns the untagged catalog ID. Safe to call on IDs that were never tagged. </summary>
        public static string StripTag(string id)
        {
            if (string.IsNullOrEmpty(id) || id[0] != kTagOpen)
            {
                return id;
            }

            int close = id.IndexOf(kTagClose);
            return close < 0 ? id : id.Substring(close + 1);
        }
    }
}
