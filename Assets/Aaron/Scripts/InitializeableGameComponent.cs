#nullable enable

using UnityEngine;

/// <summary>
/// Represents components that need to be initialized at the start of the game with the player's game state.
/// </summary>
// aisara => Use an abstract MonoBehaviour instead of an interface so we can inject them as dependencies in the editor.
// inspired by legends of runeterra
public abstract class InitializeableGameComponent : MonoBehaviour
{
    public abstract void InitializeOnGameStart(IReadOnlyPlayerBlob playerBlob);
}
