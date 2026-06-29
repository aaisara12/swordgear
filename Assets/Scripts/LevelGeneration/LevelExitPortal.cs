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
        if (_triggered || !other.CompareTag("Player"))
        {
            return;
        }

        _triggered = true;
        OnPlayerEntered?.Invoke();
    }
}
