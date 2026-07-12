#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Tutorial
{
    public class TutorialTriggerEvent : MonoBehaviour
    {
        public UnityEvent onTriggered = new ();
        public bool triggerOnce = true;
        public float delay = 0f;

        private bool triggered;

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (triggerOnce && triggered)
            {
                return;
            }

            triggered = true;

            if (delay <= 0f)
            {
                onTriggered.Invoke();
                return;
            }

            StartCoroutine(InvokeAfterDelay());
        }

        private IEnumerator InvokeAfterDelay()
        {
            yield return new WaitForSeconds(delay);
            onTriggered.Invoke();
        }
    }
}
