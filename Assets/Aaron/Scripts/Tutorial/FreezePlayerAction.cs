#nullable enable

using UnityEngine;

namespace Tutorial
{
    // PlayerGameplayInputManager typically lives in an additively-loaded systems scene (e.g. TestRig),
    // which can't be a serialized UnityEvent target. Resolve it at call time instead.
    public class FreezePlayerAction : MonoBehaviour
    {
        public void Freeze()
        {
            FindFirstObjectByType<PlayerGameplayInputManager>(FindObjectsInactive.Include)?.DisableGameplayInput();
        }

        public void Unfreeze()
        {
            FindFirstObjectByType<PlayerGameplayInputManager>(FindObjectsInactive.Include)?.EnableGameplayInput();
        }
    }
}
