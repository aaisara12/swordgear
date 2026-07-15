#nullable enable

using Unity.Cinemachine;
using UnityEngine;

namespace Testing
{
    /// <summary>
    /// TODO: Temporary solution for setting tracking target when GameManager's player is available
    /// </summary>
    public class CinemachineTrackingTargetFromGameManagerSetter : MonoBehaviour
    {
        [SerializeField] private new CinemachineCamera? camera;

        private GameManager? gameManager;
        
        private void Start()
        {
            gameManager = GameManager.Instance;
            impulseSource = impulseSourceObject;
        }

        private void Update()
        {
            if (camera == null || gameManager == null)
            {
                return;
            }
            

            var player = gameManager.player;
            if (player != null)
            {
                camera.Target = new CameraTarget{ TrackingTarget = player.transform };
            }
        }

        [SerializeField] private CinemachineImpulseSource? impulseSourceObject;
        private static CinemachineImpulseSource? impulseSource = null;

        // Per-frame shake coalescing: an AoE / big pack hits many enemies in one frame, each requesting a
        // shake. We don't want N impulses stacking into a violent lurch — we want ONE shake at the strongest
        // request (i.e. the highest-damage hit). Accumulate the max this frame; emit it once in LateUpdate.
        private static bool _hasPendingShake = false;
        private static float _pendingShakeForce = 0f;
        private static Vector3 _pendingShakeDir = Vector3.down;

        public static void Shake()
        {
            Shake(1f);
        }

        public static void Shake(float force)
        {
            Shake(force, Vector3.down);
        }

        public static void Shake(float force, Vector3 direction)
        {
            if (impulseSource == null)
            {
                return;
            }

            // Keep only the strongest shake requested this frame; LateUpdate fires it.
            if (!_hasPendingShake || force > _pendingShakeForce)
            {
                _pendingShakeForce = force;
                _pendingShakeDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.down;
            }
            _hasPendingShake = true;
        }

        private void LateUpdate()
        {
            if (_hasPendingShake && impulseSource != null)
            {
                impulseSource.GenerateImpulse(_pendingShakeDir * _pendingShakeForce);
            }
            _hasPendingShake = false;
            _pendingShakeForce = 0f;
        }
    }
}