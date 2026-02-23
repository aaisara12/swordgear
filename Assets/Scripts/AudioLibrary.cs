using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Audio/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    public List<Entry> entries = new();

    [Serializable]
    public struct Entry
    {
        public AudioSystem.Sound sound;
        public AudioClip clip;
    }

    Dictionary<AudioSystem.Sound, AudioClip> lookup;

    public void Init()
    {
        lookup = new Dictionary<AudioSystem.Sound, AudioClip>();

        foreach (var e in entries)
        {
            if (!lookup.ContainsKey(e.sound))
                lookup.Add(e.sound, e.clip);
        }
    }

    public AudioClip Get(AudioSystem.Sound sound)
    {
        if (lookup == null)
            Init();

        return lookup.TryGetValue(sound, out var clip) ? clip : null;
    }
}