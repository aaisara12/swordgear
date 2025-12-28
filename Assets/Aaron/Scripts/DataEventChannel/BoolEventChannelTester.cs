#nullable enable
using UnityEngine;

public class BoolEventChannelTester : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO? boolEventChannel;
    [SerializeField] private bool valueToSend;

    private void Awake()
    {
        boolEventChannel?.RaiseDataChanged(valueToSend);
    }
    
    public void Trigger()
    {
        boolEventChannel?.RaiseDataChanged(valueToSend);
    }
}