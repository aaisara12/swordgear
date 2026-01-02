#nullable enable

using UnityEngine;

namespace Testing
{
    public class OnAwakePawnSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerGameplayPawn? pawnPrefab;
        [SerializeField] private Transform? spawnLocation;
        [SerializeField] private PlayerGameplayInputManager? gameplayInputManager;
        
        private void Awake()
        {
            pawnPrefab.ThrowIfNull(nameof(pawnPrefab));
            spawnLocation.ThrowIfNull(nameof(spawnLocation));
            gameplayInputManager.ThrowIfNull(nameof(gameplayInputManager));
            
            var pawn = Instantiate(pawnPrefab, spawnLocation.position, spawnLocation.rotation);
            
            pawn.ThrowIfNull(nameof(pawn));
            
            gameplayInputManager.AssignPawn(pawn);
        }
    }
}