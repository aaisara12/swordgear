#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prewarms combat VFX shaders and seeds PrefabPool during boot, behind the loading screen.
/// </summary>
public class VfxPrewarmer : InitializeableGameComponent
{
    [SerializeField] private VfxWarmupCatalog? catalog;

    // aisara => Assign the BootUp Main Camera in the Inspector. If left null we fall back to the first
    // camera in the scene, since Camera.main is null during boot (the BootUp camera is untagged).
    [SerializeField] private Camera? warmupCamera;
    [SerializeField, Min(1)] private int drawFramesPerEntry = 2;
    [SerializeField, Min(0f)] private float loadingOverlayTimeout = 5f;

    bool _isComplete;

    public bool IsComplete => _isComplete;

    public override void InitializeOnGameStart(IReadOnlyPlayerBlob playerBlob)
    {
        // Warmup is driven by GameInitializer boot coroutine via RunWarmup().
    }

    public IEnumerator RunWarmup()
    {
        _isComplete = false;

        if (PrefabPool.Instance == null)
        {
            Debug.LogError("VfxPrewarmer: PrefabPool.Instance is null");
            _isComplete = true;
            yield break;
        }

        if (catalog == null)
        {
            Debug.LogError("VfxPrewarmer: catalog is null");
            _isComplete = true;
            yield break;
        }

        Camera? camera = ResolveWarmupCamera();
        if (camera == null)
        {
            Debug.LogError("VfxPrewarmer: no camera available to draw warmup VFX");
            _isComplete = true;
            yield break;
        }

        // Draw warmup effects only once the loading overlay is opaque, so they stay hidden from the player.
        yield return WaitForLoadingOverlay();

        // aisara => Boot-only "Loading..." label; SceneTransitioner fade-ins keep the overlay black.
        FindFirstObjectByType<LoadingScreenAnimator>()?.ShowBootLoadingLabel();

        var svc = catalog.ShaderVariants;
        if (svc != null && svc.shaderCount > 0)
            svc.WarmUp();

        // Dedupe by prefab so repeated catalog entries do not pay the draw cost twice.
        var warmed = new HashSet<GameObject>();
        foreach (var entry in catalog.Entries)
        {
            if (entry.prefab == null) continue;
            if (!warmed.Add(entry.prefab)) continue;

            yield return PrefabPool.Instance.WarmupByDrawing(entry.prefab, camera, drawFramesPerEntry);
            PrefabPool.Instance.Prewarm(entry.prefab, entry.prewarmCount);
        }

        _isComplete = true;
    }

    Camera? ResolveWarmupCamera()
    {
        if (warmupCamera != null)
            return warmupCamera;
        if (Camera.main != null)
            return Camera.main;
        return FindFirstObjectByType<Camera>();
    }

    IEnumerator WaitForLoadingOverlay()
    {
        float elapsed = 0f;
        while (elapsed < loadingOverlayTimeout)
        {
            var animator = FindFirstObjectByType<LoadingScreenAnimator>();
            if (animator != null && animator.IsFullyOpaque)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
