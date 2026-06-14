#nullable enable

using System.Collections;
using UnityEngine;

/// <summary>
/// Prewarms combat VFX shaders and seeds PrefabPool during boot, behind the loading screen.
/// </summary>
public class VfxPrewarmer : InitializeableGameComponent
{
    [SerializeField] private VfxWarmupCatalog? catalog;
    [SerializeField] private int entriesPerFrame = 2;
    [SerializeField] private float simulateDuration = 1f;

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

        var svc = catalog.ShaderVariants;
        if (svc != null && svc.shaderCount > 0)
            svc.WarmUp();

        int processedThisFrame = 0;
        foreach (var entry in catalog.Entries)
        {
            if (entry.prefab == null) continue;

            switch (entry.tier)
            {
                case WarmupTier.Inert:
                    PrefabPool.Instance.WarmupSimulate(entry.prefab, simulateDuration);
                    PrefabPool.Instance.Prewarm(entry.prefab, entry.prewarmCount);
                    break;
                case WarmupTier.Behavioral:
                    PrefabPool.Instance.WarmupBehavioral(entry.prefab, simulateDuration);
                    PrefabPool.Instance.Prewarm(entry.prefab, entry.prewarmCount);
                    break;
            }

            processedThisFrame++;
            if (processedThisFrame >= entriesPerFrame)
            {
                processedThisFrame = 0;
                yield return null;
            }
        }

        _isComplete = true;
    }
}
