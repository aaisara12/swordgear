#nullable enable

using UnityEngine;

namespace Shop
{
    /// <summary> A store item that has been stamped with the element it is being offered as. </summary>
    public interface IAugmentStoreItem : IStoreItem
    {
        public Element Element { get; }

        /// <summary> The catalog asset this was rolled from. Consumers needing asset-typed data unwrap through this. </summary>
        public IStoreItem Source { get; }
    }

    /// <summary>
    /// Runtime wrapper that stamps an element onto a catalog augment. Only the ID changes — it gains the element
    /// tag, so the acquired copy is a distinct inventory entry from the same augment rolled as another element.
    /// </summary>
    public class ElementTaggedStoreItem : IAugmentStoreItem
    {
        public ElementTaggedStoreItem(IStoreItem source, Element element)
        {
            Source = source;
            Element = element;
            Id = AugmentElementSerializer.Serialize(element, source.Id);
        }

        public IStoreItem Source { get; }
        public Element Element { get; }
        public string Id { get; }
        public string DisplayName => Source.DisplayName;
        public string Description => Source.Description;
        public int Cost => Source.Cost;
        public Sprite? Icon => Source.Icon;
    }

    public static class AugmentElementRoller
    {
        /// <summary>
        /// Stamps an element on a catalog augment for an offer. Element-specific upgrades always come as their own
        /// element; everything else — stat buffs and non-elemental upgrades — rolls one, so the same buff can show
        /// up under any element across runs.
        /// </summary>
        public static IAugmentStoreItem Tag(IStoreItem item)
        {
            if (item is ElementUpgradeLoadableStoreItem upgradeItem
                && UpgradeTypeSerializer.TryGetElement(upgradeItem.ElementUpgrade, out Element upgradeElement))
            {
                return new ElementTaggedStoreItem(item, upgradeElement);
            }

            return new ElementTaggedStoreItem(item, AugmentElementSerializer.RollElement());
        }
    }
}
