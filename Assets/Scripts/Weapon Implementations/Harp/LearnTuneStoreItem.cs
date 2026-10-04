#nullable enable

using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Light's "Learn a tune" augment: a repeatable element upgrade that teaches the harp one random tune it
    /// doesn't know yet, and leaves the offer pool once there is nothing left to learn.
    /// </summary>
    /// <remarks>
    /// The purchase itself is an ordinary element upgrade (<see cref="UpgradeType.Light_LearnTune"/>): each
    /// copy lands in the inventory as usual, and <see cref="HarpRepertoire"/> turns every copy into one
    /// learned tune. Keeping the purchased item, rather than swapping it for the tune, leaves the element
    /// ledger's count of Light augments exactly as it would be for any other Light upgrade.
    /// </remarks>
    [CreateAssetMenu(fileName = "LearnTuneStoreItem", menuName = "Scriptable Objects/Learn Tune Store Item")]
    public class LearnTuneStoreItem : ElementUpgradeLoadableStoreItem
    {
        public override bool IsOfferable => HarpRepertoire.Instance == null || HarpRepertoire.Instance.HasUnlearned;
    }
}
