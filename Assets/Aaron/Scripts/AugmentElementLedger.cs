#nullable enable

using System;
using System.Collections.Generic;
using Shop;

/// <summary>
/// Counts the augments the player owns, per element. Ultimates read this to decide whether they are unlocked and
/// at what level. Derived entirely from the player's inventory, so it can never drift from what was actually
/// picked up — including a run reset, which clears the inventory and empties the ledger with it.
/// </summary>
public static class AugmentElementLedger
{
    private static readonly Dictionary<Element, int> _counts = new();
    private static IReadOnlyPlayerBlob? _blob;

    public static IReadOnlyDictionary<Element, int> Counts => _counts;

    public static event Action? OnCountsChanged;

    /// <summary> Called once at boot with the run's player blob. Re-binding swaps which blob is tracked. </summary>
    public static void Bind(IReadOnlyPlayerBlob blob)
    {
        if (_blob != null)
        {
            _blob.InventoryItems.DictionaryChanged -= HandleInventoryChanged;
        }

        _blob = blob;
        blob.InventoryItems.DictionaryChanged += HandleInventoryChanged;
        Rebuild();
    }

    public static int GetCount(Element element) => _counts.TryGetValue(element, out int count) ? count : 0;

    private static void HandleInventoryChanged(ObservableDictionaryChangedEventArgs<string, int> _) => Rebuild();

    private static void Rebuild()
    {
        _counts.Clear();

        if (_blob != null)
        {
            foreach (KeyValuePair<string, int> kvp in _blob.InventoryItems)
            {
                if (kvp.Value <= 0)
                    continue;

                if (!TryResolveElement(kvp.Key, out Element element))
                    continue;

                _counts[element] = GetCount(element) + kvp.Value;
            }
        }

        OnCountsChanged?.Invoke();
    }

    /// <summary>
    /// Only augments count — consumables such as instant heals are ignored. Tagged IDs carry the element they were
    /// offered as; an untagged element upgrade falls back to the element in its UpgradeType name, so upgrades
    /// granted outside the augment offer flow (debug menu, save data from before the tag existed) still count.
    /// </summary>
    private static bool TryResolveElement(string itemId, out Element element)
    {
        element = default;

        bool isElementUpgrade = UpgradeTypeSerializer.TryDeserialize(itemId, out UpgradeType upgrade);
        if (!isElementUpgrade && !StatBoostSerializer.TryDeserializeEntries(itemId, out _))
            return false;

        if (AugmentElementSerializer.TryDeserialize(itemId, out element))
            return true;

        return isElementUpgrade && UpgradeTypeSerializer.TryGetElement(upgrade, out element);
    }
}
