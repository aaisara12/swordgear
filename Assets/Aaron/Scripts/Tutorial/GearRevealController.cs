#nullable enable

using System.Collections;
using UnityEngine;

namespace Tutorial
{
    public class GearRevealController : MonoBehaviour
    {
        [Tooltip("The gear normally sits back until the player picks from it; at its reveal it comes forward for " +
                 "this many seconds so the player sees it.")]
        [SerializeField, Min(0f)] private float revealLift = 3f;

        private void Awake()
        {
            // GearManager may live in an additively-loaded scene (not a valid serialized reference target),
            // and spawns its ring/tile visuals in its own Start(), which isn't guaranteed to run before this
            // Awake(). Wait a frame so the lookup + Hide() happen after every Start() this frame has finished.
            StartCoroutine(HideNextFrame());
        }

        private IEnumerator HideNextFrame()
        {
            yield return null;
            Hide();
        }

        public void Hide() => SetVisible(false);

        public void Reveal() => SetVisible(true);

        private void SetVisible(bool visible)
        {
            GearManager? gearManager = FindFirstObjectByType<GearManager>(FindObjectsInactive.Include);
            if (gearManager == null)
            {
                return;
            }

            // Every kind of renderer under the gear: sprites, the arc wedges and hub (meshes), and the particles
            // that fly off an active arc.
            foreach (Renderer renderer in gearManager.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }

            foreach (Collider2D collider in gearManager.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = visible;
            }

            if (visible)
            {
                gearManager.Lift(revealLift);
            }
        }
    }
}
