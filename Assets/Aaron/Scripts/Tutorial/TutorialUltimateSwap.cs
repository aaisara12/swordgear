#nullable enable

using UnityEngine;

namespace Tutorial
{
    // UltimateChargeTracker typically lives in an additively-loaded systems scene (e.g. TestRig),
    // which can't be a serialized UnityEvent target. Resolve it at call time instead.
    public class TutorialUltimateSwap : MonoBehaviour
    {
        [SerializeField] private UltimateAbilitySO? tutorialAbility;

        private UltimateAbilitySO? previousAbility;
        private bool hasSwapped;

        public void Activate()
        {
            tutorialAbility.ThrowIfNull(nameof(tutorialAbility));

            UltimateChargeTracker? tracker = FindFirstObjectByType<UltimateChargeTracker>(FindObjectsInactive.Include);
            if (tracker == null)
            {
                return;
            }

            previousAbility = tracker.ActiveUltimate;
            hasSwapped = true;
            tracker.SetActiveUltimate(tutorialAbility);
        }

        /// <summary>Restores the ultimate the player had equipped before the tutorial swap. Wired to run when the tutorial scene unloads.</summary>
        public void Restore()
        {
            if (!hasSwapped)
            {
                return;
            }

            FindFirstObjectByType<UltimateChargeTracker>(FindObjectsInactive.Include)?.SetActiveUltimate(previousAbility);
            hasSwapped = false;
        }
    }
}
