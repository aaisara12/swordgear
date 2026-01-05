#nullable enable

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Exposes MonoBehaviour callback events in the editor. This allows us to move some logic out to the data layer.
/// </summary>
public class MonoBehaviourCallbackNotifier : MonoBehaviour
{
    [SerializeField] private UnityEvent onAwake = new UnityEvent();
    [SerializeField] private UnityEvent onStart = new UnityEvent();
    [SerializeField] private UnityEvent onEnable = new UnityEvent();
    [SerializeField] private UnityEvent onDisable = new UnityEvent();
    [SerializeField] private UnityEvent onDestroy = new UnityEvent();
    
    private void Awake()
    {
        onAwake.Invoke();
    }

    private void Start()
    {
        onStart.Invoke();
    }
    
    private void OnEnable()
    {
        onEnable.Invoke();
    }

    private void OnDisable()
    {
        onDisable.Invoke();
    }

    private void OnDestroy()
    {
        onDestroy.Invoke();
    }
}
