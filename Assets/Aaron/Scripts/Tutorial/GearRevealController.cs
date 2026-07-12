#nullable enable

using System.Collections;
using UnityEngine;

namespace Tutorial
{
    public class GearRevealController : MonoBehaviour
    {
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

            foreach (SpriteRenderer renderer in gearManager.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = visible;
            }

            foreach (Collider2D collider in gearManager.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = visible;
            }
        }
    }
}
