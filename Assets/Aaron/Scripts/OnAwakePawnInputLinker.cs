#nullable enable

using UnityEngine;

namespace Testing
{
    public class OnAwakePawnInputLinker : MonoBehaviour
    {
        [SerializeField] private PlayerGameplayPawn? pawn;
        [SerializeField] private PlayerGameplayInputManager? gameplayInputManager;
        
        private void Awake()
        {
            pawn.ThrowIfNull(nameof(pawn));
            gameplayInputManager.ThrowIfNull(nameof(gameplayInputManager));
            
            gameplayInputManager.AssignPawn(pawn);
        }
    }
}