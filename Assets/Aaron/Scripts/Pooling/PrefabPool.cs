#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic prefab pool for combat VFX and projectiles. Mirrors the AudioSystem pooling pattern.
/// </summary>
public class PrefabPool : MonoBehaviour
{
    public static PrefabPool? Instance { get; private set; }

    readonly Dictionary<GameObject, Stack<PooledInstance>> _pools = new();
    Transform? _poolRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        var rootGo = new GameObject("PrefabPool_Root");
        _poolRoot = rootGo.transform;
        DontDestroyOnLoad(rootGo);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform? parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError("PrefabPool.Spawn: prefab is null");
            return null!;
        }

        var pooled = GetFromPool(prefab);
        var instance = pooled.gameObject;

        if (parent != null)
            instance.transform.SetParent(parent, false);

        instance.SetActive(true);
        pooled.OnSpawnedFromPool();
        // aisara => Apply caller transform after pool reset; OnSpawnedFromPool restores cached local pose under pool root.
        instance.transform.SetPositionAndRotation(position, rotation);

        return instance;
    }

    PooledInstance GetFromPool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<PooledInstance>();
            _pools[prefab] = stack;
        }

        while (stack.Count > 0)
        {
            var candidate = stack.Pop();
            if (candidate == null)
                continue;

            return candidate;
        }

        return CreateInstance(prefab);
    }

    PooledInstance CreateInstance(GameObject prefab)
    {
        var go = Instantiate(prefab);
        var pooled = go.GetComponent<PooledInstance>();
        if (pooled == null)
            pooled = go.AddComponent<PooledInstance>();
        pooled.Initialize(prefab);
        return pooled;
    }

    public void Release(GameObject instance)
    {
        if (instance == null) return;

        var pooled = instance.GetComponent<PooledInstance>();
        if (pooled == null || pooled.SourcePrefab == null)
        {
            Destroy(instance);
            return;
        }

        if (!pooled.IsActive) return;

        pooled.OnReleasedToPool();
        instance.SetActive(false);
        instance.transform.SetParent(_poolRoot, false);

        if (!_pools.TryGetValue(pooled.SourcePrefab, out var stack))
        {
            stack = new Stack<PooledInstance>();
            _pools[pooled.SourcePrefab] = stack;
        }

        stack.Push(pooled);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            var pooled = CreateInstance(prefab);
            pooled.gameObject.SetActive(false);
            pooled.gameObject.transform.SetParent(_poolRoot, false);

            if (!_pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<PooledInstance>();
                _pools[prefab] = stack;
            }

            stack.Push(pooled);
        }
    }

    /// <summary>
    /// Returns all active pooled instances to the idle stack. Call on scene transitions.
    /// </summary>
    public void ReleaseAll()
    {
        var activeInstances = FindObjectsByType<PooledInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pooled in activeInstances)
        {
            if (pooled != null && pooled.IsActive)
                Release(pooled.gameObject);
        }
    }

    // aisara => Off-screen ParticleSystem.Simulate never triggers a real draw, so on Metal the shader/PSO
    // is still compiled on first in-game use (per-element + catch hitching). Draw it in-frustum instead.
    const float WarmupDrawDistance = 10f;
    const float WarmupSimulateTime = 0.1f;

    /// <summary>
    /// Instantiate inside the camera's frustum and render for a few frames so the GPU actually creates the
    /// shader/pipeline state during boot (fixes first-use hitching on Metal). Gameplay behaviours are
    /// disabled to avoid side effects, and every child GameObject is force-activated so sprite-only effects
    /// (e.g. the catch ring, which starts inactive in its prefab) still draw.
    /// </summary>
    public IEnumerator WarmupByDrawing(GameObject prefab, Camera camera, int frames)
    {
        if (prefab == null || camera == null)
            yield break;

        // Centre of the view, between the near and far planes, so the object is guaranteed on-screen.
        Vector3 position = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, WarmupDrawDistance));
        var go = Instantiate(prefab, position, Quaternion.identity);

        // Disable behaviours before activating so their OnEnable side effects never fire during warmup.
        // PlayerHitbox is kept to mirror the prior behavioural-warmup intent.
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb is PlayerHitbox)
                continue;
            mb.enabled = false;
        }

        // Force-activate every child so inactive renderers still draw a frame.
        foreach (var childTransform in go.GetComponentsInChildren<Transform>(true))
            childTransform.gameObject.SetActive(true);
        go.SetActive(true);

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        // Populate particles so the particle renderer has something to draw this frame.
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            ps.Simulate(WarmupSimulateTime, true, true);
            ps.Play(true);
        }

        // Hold across rendered frames so a real draw is submitted BEFORE we clear/destroy.
        int holdFrames = Mathf.Max(1, frames);
        for (int i = 0; i < holdFrames; i++)
            yield return new WaitForEndOfFrame();

        foreach (var ps in systems)
        {
            if (ps != null)
                ps.Clear(true);
        }

        Destroy(go);
    }
}
