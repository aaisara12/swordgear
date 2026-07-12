#nullable enable

using UnityEngine;

namespace Tutorial
{
    public class SwordProximityTimeDilation : MonoBehaviour
    {
        [SerializeField] private float catchRadius = 3f;
        [SerializeField] private float slowScale = 0.35f;
        [SerializeField] private float lerpSpeed = 8f;

        private bool playerInRoom;

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRoom = true;
            }
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRoom = false;
            }
        }

        private void Update()
        {
            float target = 1f;

            if (playerInRoom &&
                SwordProjectile.Instance != null &&
                SwordProjectile.Instance.gameObject.activeSelf &&
                !SwordProjectile.Instance.IsLodged &&
                GameManager.Instance != null &&
                GameManager.Instance.player != null)
            {
                float distance = Vector2.Distance(
                    GameManager.Instance.player.transform.position,
                    SwordProjectile.Instance.transform.position);

                if (distance < catchRadius)
                {
                    target = slowScale;
                }
            }

            Time.timeScale = Mathf.MoveTowards(Time.timeScale, target, lerpSpeed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
