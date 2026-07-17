#nullable enable

using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Plays a zoom-in transition on level start: the camera snaps out to a wider orthographic
/// size, then eases back down to its authored size. Lives on the same GameObject as the
/// CinemachineCamera, which is re-created fresh each time a level (Arena scene) loads, so
/// Start() here already fires exactly once per level.
/// </summary>
public class CameraLevelStartZoom : MonoBehaviour
{
    [SerializeField] private CinemachineCamera? cinemachineCamera;
    [SerializeField] private float zoomedOutOrthographicSize = 22f;
    [SerializeField] private float zoomInDuration = 1.2f;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }
    }

    private void Start()
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError("CameraLevelStartZoom: no CinemachineCamera found.");
            return;
        }

        StartCoroutine(ZoomInRoutine(cinemachineCamera.Lens.OrthographicSize));
    }

    private IEnumerator ZoomInRoutine(float targetOrthographicSize)
    {
        CinemachineCamera cam = cinemachineCamera!;
        cam.Lens.OrthographicSize = zoomedOutOrthographicSize;

        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = zoomCurve.Evaluate(Mathf.Clamp01(elapsed / zoomInDuration));
            cam.Lens.OrthographicSize = Mathf.Lerp(zoomedOutOrthographicSize, targetOrthographicSize, t);
            yield return null;
        }

        cam.Lens.OrthographicSize = targetOrthographicSize;
    }
}
