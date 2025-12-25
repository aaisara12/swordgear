#nullable enable
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu (fileName = "DummyPlayerBlobLoaderSO", menuName = "Scriptable Objects/Player Blob Loaders/Dummy Player Blob Loader")]
public class DummyPlayerBlobLoaderSO : PlayerBlobLoaderSO
{
    [SerializeField] private int startingCurrency = 1000;
    [SerializeField] private SerializedDictionary<string, int> startingInventory = new ()
    {
        {"item_1", 5},
        {"item_2", 10}
    };
    
    public override bool TryLoad(out PlayerBlob? blob)
    {
        blob = new PlayerBlob();
        
        blob.WalletLedger = startingCurrency;
        foreach (var kvp in startingInventory)
        {
            blob.ReceiveItem(kvp.Key, kvp.Value);
        }
        
        return true;
    }
}