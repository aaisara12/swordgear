using UnityEngine;
using UnityEngine.Audio;
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
        public AudioMixerGroup mixerGroup; // optional
    }

    Dictionary<AudioSystem.Sound, Entry> lookup;

    public void Init()
    {
        lookup = new Dictionary<AudioSystem.Sound, Entry>();

        foreach (var e in entries)
        {
            if (!lookup.ContainsKey(e.sound))
                lookup.Add(e.sound, e);
        }
    }

    public bool TryGet(AudioSystem.Sound sound, out Entry entry)
    {
        if (lookup == null)
            Init();

        return lookup.TryGetValue(sound, out entry);
    }
}