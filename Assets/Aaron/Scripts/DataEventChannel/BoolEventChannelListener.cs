#nullable enable

using UnityEngine;

public class BoolEventChannelListener : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO? eventChannel;
    
    [SerializeField] private UnityEngine.Events.UnityEvent<bool> onTrue = new UnityEngine.Events.UnityEvent<bool>();
    [SerializeField] private UnityEngine.Events.UnityEvent<bool> onFalse = new UnityEngine.Events.UnityEvent<bool>();
    
    private void OnEnable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnDataChanged += HandleDataChanged;
        }
    }

    private void HandleDataChanged(bool dataValue)
    {
        if (dataValue)
        {
            onTrue.Invoke(dataValue);
        }
        else
        {
            onFalse.Invoke(dataValue);
        }
    }
    
    private void OnDisable()
    {
        if (eventChannel != null)
        {
            eventChannel.OnDataChanged -= HandleDataChanged;
        }
    }
}
