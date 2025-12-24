#nullable enable

using System;

/// <summary>
/// Tracks player's game state and synchronizes core game systems.
/// </summary>
public class GameStateSynchronizer : IDisposable
{
    private PlayerBlob playerBlob;
    private ElementManager elementManager;
    
    private bool hasStarted = false;
    
    public GameStateSynchronizer(PlayerBlob playerBlob, ElementManager elementManager)
    {
        this.playerBlob = playerBlob;
        this.elementManager = elementManager;

        playerBlob.InventoryItems.DictionaryChanged += HandlePlayerInventoryChanged;
    }
    
    public void Start()
    {
        if (hasStarted)
        {
            return;
        }
        
        // On start, load existing upgrades from player blob
        GameUtility.LoadElementUpgradesFromPlayerBlob(playerBlob, elementManager);
        hasStarted = true;
    }

    public void Dispose()
    {
        playerBlob.InventoryItems.DictionaryChanged -= HandlePlayerInventoryChanged;
    }
    
    private void HandlePlayerInventoryChanged(ObservableDictionaryChangedEventArgs<string, int> obj)
    {
        if (hasStarted == false)
        {
            return;
        }
        
        switch (obj.Action)
        {
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Add:
            {
                string? itemId = obj.Key;
                itemId.ThrowIfNull(nameof(itemId));
                
                HandleInventoryCountChanged(itemId, obj.NewValue);

                break;
            }
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Remove:
            {
                string? itemId = obj.Key;
                itemId.ThrowIfNull(nameof(itemId));
                
                HandleInventoryCountChanged(itemId, 0);

                break;
            }
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Replace:
            {
                string? itemId = obj.Key;
                itemId.ThrowIfNull(nameof(itemId));
                
                HandleInventoryCountChanged(itemId, obj.NewValue);

                break;
            }
            case ObservableDictionaryChangedEventArgs<string, int>.ChangeType.Clear:
            {
                HandleInventoryCleared();
                break;
            }
        }
    }

    private void HandleInventoryCountChanged(string itemId, int newCount)
    {
        if (UpgradeTypeSerializer.TryDeserialize(itemId, out UpgradeType upgrade))
        {
            if (newCount == 0)
            {
                elementManager.RemoveUpgrade(upgrade);
            }
            else if (newCount > 1)
            {
                if (elementManager.HasUpgrade(upgrade))
                {
                    return;
                }
                
                elementManager.AddUpgrade(upgrade);
            }
        }
    }
    
    private void HandleInventoryCleared()
    {
        elementManager.ClearUpgrades();
    }
}
