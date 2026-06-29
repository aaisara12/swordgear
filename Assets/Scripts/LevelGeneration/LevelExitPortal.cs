using System;
using UnityEngine;

/// <summary>
/// Combat exit trigger spawned after the final wave is cleared. Commit 09 wires this to return to the map.
/// </summary>
public class LevelExitPortal : MonoBehaviour
{
    public event Action OnPlayerEntered;

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered)
        {
            return;
        }

        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<PlayerGameplayPawn>() != null;
        if (!isPlayer)
        {
            return;
        }

        _triggered = true;
        Debug.Log("LevelExitPortal: player entered.");
        OnPlayerEntered?.Invoke();
    }
}
