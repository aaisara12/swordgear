#nullable enable

using System.Collections;
using UnityEngine;

/// <summary>
/// Brief time-freeze on impactful hits ("hit-stop") for weight and juice. Global, self-contained singleton
/// created on first use. Uses a near-zero timeScale for a real-time duration then restores to 1 — but only if
/// nothing else (e.g. the pause menu at timeScale 0, or a scene transition) changed the timescale meanwhile,
/// so it never fights the pause. Rapid calls extend the freeze rather than stacking coroutines.
/// </summary>
public class HitStop : MonoBehaviour
{
    // Near-frozen, but deliberately non-zero so it is distinguishable from a real pause (timeScale 0).
    private const float FreezeScale = 0.05f;

    private static HitStop? _instance;
    private static float _resumeAtRealtime;
    private static bool _running;

    /// <summary>Freeze the game for <paramref name="seconds"/> of real time. No-op while already paused/frozen.</summary>
    public static void Do(float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        // Never start a hit-stop while the game is paused or already frozen by something else.
        if (Time.timeScale == 0f)
        {
            return;
        }

        EnsureInstance();
        float end = Time.realtimeSinceStartup + seconds;
        if (end > _resumeAtRealtime)
        {
            _resumeAtRealtime = end;
        }

        if (!_running)
        {
            _instance!.StartCoroutine(_instance.Run());
        }
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        var go = new GameObject("HitStop");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<HitStop>();
        _running = false; // clear any state left over from a previous play session (fast enter-play-mode)
    }

    private IEnumerator Run()
    {
        _running = true;
        Time.timeScale = FreezeScale;

        while (Time.realtimeSinceStartup < _resumeAtRealtime)
        {
            // If something else took over the timescale (pause -> 0, transition -> 1), bail without clobbering it.
            if (!Mathf.Approximately(Time.timeScale, FreezeScale))
            {
                _running = false;
                yield break;
            }

            yield return null;
        }

        if (Mathf.Approximately(Time.timeScale, FreezeScale))
        {
            Time.timeScale = 1f;
        }

        _running = false;
    }
}
