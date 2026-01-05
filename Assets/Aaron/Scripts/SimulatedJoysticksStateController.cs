#nullable enable

using UnityEngine;

public class SimulatedJoysticksStateController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private BoolEventChannelSO? uiVisibilityEventChannel;

    [Header("Scene References")] [SerializeField]
    private GameObject? view;
    
    private void Awake()
    {
        if (uiVisibilityEventChannel == null)
        {
            Debug.LogError("UI Visibility Event Channel is null!");
            return;
        }

        if (view == null)
        {
            Debug.LogError("View is null!");
            return;
        }
        
        view.SetActive(false);
        uiVisibilityEventChannel.OnDataChanged += HandleUiVisibilityChanged;
    }

    private void HandleUiVisibilityChanged(bool isVisible)
    {
        if (view == null)
        {
            return;
        }
        
        view.gameObject.SetActive(isVisible);
    }

    private void OnDestroy()
    {
        if (uiVisibilityEventChannel != null)
        {
            uiVisibilityEventChannel.OnDataChanged -= HandleUiVisibilityChanged;
        }
    }
}
