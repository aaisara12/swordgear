#nullable enable

using UnityEngine;

/// <summary>
/// Class that can be used to make a canvas UI element follow the position of the player. 
/// </summary>
public class PlayerPositionFollower : MonoBehaviour
{
    private Camera? _playerCamera;
    
    [SerializeField] private RectTransform? _followerRectTransform;
    
    private void Update()
    {
        if (_followerRectTransform == null)
        {
            return;
        }
        
        if (GameManager.Instance == null)
        {
            return;
        }

        // TODO: aisara => With the game manager refactor, we should provide both a screen point and a world point for the player position, meaning we won't need to do the screen point calc ourselves
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;

            if (_playerCamera == null)
            {
                return;
            }
        }

        // TODO: aisara => Fix this when we refactor game manager not to use a hard reference to the transform of the player
        var player = GameManager.Instance.player;

        if (player == null)
        {
            return;
        }
        
        var playerScreenPosition = _playerCamera.WorldToScreenPoint(player.transform.position);
        _followerRectTransform.position = playerScreenPosition;
    }
}
