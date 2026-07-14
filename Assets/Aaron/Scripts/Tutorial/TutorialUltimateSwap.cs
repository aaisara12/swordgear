#nullable enable

using UnityEngine;

namespace Tutorial
{
    // UltimateChargeTracker typically lives in an additively-loaded systems scene (e.g. TestRig),
    // which can't be a serialized UnityEvent target. Resolve it at call time instead.
    public class TutorialUltimateSwap : MonoBehaviour
    {
        [SerializeField] private UltimateAbilitySO? tutorialAbility;

        public void Activate()
        {
            tutorialAbility.ThrowIfNull(nameof(tutorialAbility));
            FindFirstObjectByType<UltimateChargeTracker>(FindObjectsInactive.Include)?.SetActiveUltimate(tutorialAbility);
        }
    }
}
