#nullable enable

using UnityEngine;

public static class GameUtility
{
    // aisara => static because no internal state needed
    public static void LoadElementUpgradesFromPlayerBlob(IReadOnlyPlayerBlob playerBlob, ElementManager elementManager)
    {
        elementManager.ClearUpgrades();
        
        foreach (var kvp in playerBlob.InventoryItems)
        {
            var itemId = kvp.Key;

            if (UpgradeTypeSerializer.TryDeserialize(itemId, out var upgrade) == false)
            {
                continue;
            }

            if (kvp.Value == 0)
            {
                continue;
            }

            if (kvp.Value < 0)
            {
                Debug.LogError("On load, PlayerBlob has invalid quantity for upgrade item: " + itemId);
                continue;
            }
            
            elementManager.AddUpgrade(upgrade);
        }
    }
}
