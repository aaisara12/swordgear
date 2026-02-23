using UnityEngine;
using System.Collections.Generic;

public class AudioSystem : MonoBehaviour
{
    public enum Sound
    {
        // Player
        Player_Walking,
        Player_Recall,
        Player_Hurt,

        // Slashes
        Slash_Basic,
        Slash_FireBasic,
        Slash_FireCharged,
        Slash_FireEruption,
        Slash_IceBasic,
        Slash_IceEmpowered,
        Slash_LightningBasic,
        Slash_LightningEmpowered,

        // Throwing
        Throw,
        Bounce,
        Basic_Flight,
        Fire_Flight,
        Ice_Flight,
        Lightning_Flight
    }

    public AudioLibrary library;
    public int initialPoolSize = 5;
    public float unusedLifetime = 10f;

    static AudioSystem instance;

    class PooledSource
    {
        public AudioSource source;
        public float lastUsedTime;
    }

    List<PooledSource> pool = new();
    Dictionary<string, PooledSource> activeLoops = new();

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        library.Init();

        for (int i = 0; i < initialPoolSize; i++)
            CreateSource();
    }

    void Update()
    {
        CleanupUnused();
    }

    PooledSource CreateSource()
    {
        GameObject go = new GameObject("AudioSource");
        go.transform.parent = transform;

        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        var ps = new PooledSource
        {
            source = src,
            lastUsedTime = Time.time
        };

        pool.Add(ps);
        return ps;
    }

    PooledSource GetAvailable()
    {
        foreach (var p in pool)
        {
            if (!p.source.isPlaying && !activeLoops.ContainsValue(p))
                return p;
        }

        return CreateSource();
    }

    void CleanupUnused()
    {
        float now = Time.time;

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            var p = pool[i];

            bool isLoopReserved = activeLoops.ContainsValue(p);

            if (!p.source.isPlaying &&
                !isLoopReserved &&
                now - p.lastUsedTime > unusedLifetime)
            {
                Destroy(p.source.gameObject);
                pool.RemoveAt(i);
            }
        }
    }

    // ---------------- ONE SHOT ----------------

    public static void Play(Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (instance == null) return;

        if (!instance.library.TryGet(sound, out var entry))
        {
            Debug.LogWarning($"No clip for {sound}");
            return;
        }

        var pooled = instance.GetAvailable();
        var src = pooled.source;

        src.clip = entry.clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = false;

        if (entry.mixerGroup != null)
            src.outputAudioMixerGroup = entry.mixerGroup;

        src.Play();
        pooled.lastUsedTime = Time.time;
    }

    // ---------------- LOOP PLAY ----------------

    public static void PlayLoop(string id, Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (instance == null) return;

        if (instance.activeLoops.ContainsKey(id))
            return; // already playing

        if (!instance.library.TryGet(sound, out var entry))
        {
            Debug.LogWarning($"No clip for {sound}");
            return;
        }

        var pooled = instance.GetAvailable();
        var src = pooled.source;

        src.clip = entry.clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = true;

        if (entry.mixerGroup != null)
            src.outputAudioMixerGroup = entry.mixerGroup;

        src.Play();

        pooled.lastUsedTime = Time.time;
        instance.activeLoops.Add(id, pooled);
    }

    // ---------------- LOOP STOP ----------------

    public static void StopLoop(string id)
    {
        if (instance == null) return;

        if (!instance.activeLoops.TryGetValue(id, out var pooled))
            return;

        pooled.source.Stop();
        pooled.source.loop = false;
        pooled.lastUsedTime = Time.time;

        instance.activeLoops.Remove(id);
    }
}