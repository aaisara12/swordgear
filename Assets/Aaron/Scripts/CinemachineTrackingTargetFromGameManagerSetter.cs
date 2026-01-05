#nullable enable

using Unity.Cinemachine;
using UnityEngine;

namespace Testing
{
    /// <summary>
    /// Temporary solution for setting tracking target when GameManager's player is available
    /// </summary>
    public class CinemachineTrackingTargetFromGameManagerSetter : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera? camera;
        [SerializeField] private GameManager? gameManager;
        
        void Update()
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
    }
}