#nullable enable

using UnityEngine;

/// <summary>
/// Represents components that need to be initialized at the start of the game with unrestricted access to the player's game state.
/// In general, this should only be used as a last resort.
/// </summary>
public abstract class InitializeableUnrestrictedGameComponent : MonoBehaviour
{
    // aisara => This method has an extra scary name to really drive home the point that this should be used with caution.
    public abstract void InitializeOnGameStart_Dangerous(PlayerBlob playerBlob);
}
