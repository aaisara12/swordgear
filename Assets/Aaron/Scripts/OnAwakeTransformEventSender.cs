#nullable enable

using UnityEngine;

public class OnAwakeTransformEventSender : MonoBehaviour
{
    [SerializeField] private TransformEventChannelSO? eventChannel;
    
    private void Awake()
    {
        if (eventChannel == null)
        {
            Debug.LogError("Event channel is null! Can't send transform event.");
            return;
        }
        
        eventChannel.RaiseDataChanged(transform);
    }
}
