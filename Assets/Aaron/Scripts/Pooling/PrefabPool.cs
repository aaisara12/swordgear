#nullable enable

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

    /// <summary>
    /// Instantiate once off-screen and simulate particle systems to compile shaders without pooling.
    /// </summary>
    public void WarmupSimulate(GameObject prefab, float simulateDuration = 1f)
    {
        if (prefab == null) return;

        var go = Instantiate(prefab, new Vector3(0f, -1000f, 0f), Quaternion.identity);
        go.SetActive(true);

        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            ps.Simulate(simulateDuration, true, true);
            ps.Clear(true);
        }

        Destroy(go);
    }

    /// <summary>
    /// Warm renderer/particle materials for behavioral prefabs without running gameplay logic.
    /// </summary>
    public void WarmupBehavioral(GameObject prefab, float simulateDuration = 1f)
    {
        if (prefab == null) return;

        var go = Instantiate(prefab, new Vector3(0f, -1000f, 0f), Quaternion.identity);

        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is PlayerHitbox)
                continue;
            mb.enabled = false;
        }

        go.SetActive(true);

        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            ps.Simulate(simulateDuration, true, true);
            ps.Clear(true);
        }

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.enabled = true;

        Destroy(go);
    }
}
