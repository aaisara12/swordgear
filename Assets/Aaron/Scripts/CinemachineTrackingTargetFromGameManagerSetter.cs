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
        [SerializeField] private CinemachineCamera? camera;

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

        public static void Shake()
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulse();
        }
    }
}