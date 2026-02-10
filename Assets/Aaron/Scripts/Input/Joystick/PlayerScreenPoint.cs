#nullable enable

using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerScreenPoint : MonoBehaviour
{
    [SerializeField] private Transform? _followTarget;
    [SerializeField] private Camera? _camera;
    [SerializeField] private RectTransform? _dot;

    private Vector2 _lastClickPosition;
    
    private void Update()
    {
        if (_followTarget == null || _camera == null || _dot == null)
        {
            return;
        }
        
        Vector3 screenPoint = _camera.WorldToScreenPoint(_followTarget.position);

        _dot.position = screenPoint;
    }
}
