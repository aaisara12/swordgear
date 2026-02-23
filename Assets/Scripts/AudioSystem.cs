using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioSystem : MonoBehaviour
{
    public enum Sound
    {
        Shoot,
        Explosion,
        Jump,
        Dash,
        Hit,
        UI_Click
    }

    public AudioLibrary library;
    public int initialPoolSize = 5;
    public float unusedLifetime = 10f;

    static AudioSystem instance;

    List<PooledSource> pool = new();

    class PooledSource
    {
        public AudioSource source;
        public float lastUsedTime;
        public bool InUse => source.isPlaying;
    }

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
        src.spatialBlend = 0f; // 2D

        PooledSource ps = new PooledSource
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
            if (!p.InUse)
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

            if (!p.InUse && now - p.lastUsedTime > unusedLifetime)
            {
                Destroy(p.source.gameObject);
                pool.RemoveAt(i);
            }
        }
    }

    // -------- STATIC PLAY METHOD --------

    public static void Play(Sound sound, float volume = 1f, float pitch = 1f)
    {
        if (instance == null)
        {
            Debug.LogWarning("AudioSystem missing in scene.");
            return;
        }

        var clip = instance.library.Get(sound);

        if (clip == null)
        {
            Debug.LogWarning($"No clip mapped for {sound}");
            return;
        }

        var pooled = instance.GetAvailable();

        pooled.source.clip = clip;
        pooled.source.volume = volume;
        pooled.source.pitch = pitch;
        pooled.source.Play();

        pooled.lastUsedTime = Time.time;
    }
}