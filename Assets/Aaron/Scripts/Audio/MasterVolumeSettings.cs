#nullable enable

using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persists and applies master volume to an exposed AudioMixer parameter.
/// </summary>
public static class MasterVolumeSettings
{
    public const string PrefKey = "swordgear.masterVolume";
    public const string MixerParameter = "MasterVolume";
    public const float DefaultLinear = 1f;

    public static float GetLinear()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKey, DefaultLinear));
    }

    public static void SetLinear(float linear, AudioMixer? mixer)
    {
        float clamped = Mathf.Clamp01(linear);
        PlayerPrefs.SetFloat(PrefKey, clamped);
        PlayerPrefs.Save();
        ApplyToMixer(mixer, clamped);
    }

    public static void ApplySaved(AudioMixer? mixer)
    {
        ApplyToMixer(mixer, GetLinear());
    }

    public static void ApplyToMixer(AudioMixer? mixer, float linear)
    {
        if (mixer == null)
        {
            return;
        }

        float db = LinearToDecibels(linear);
        if (!mixer.SetFloat(MixerParameter, db))
        {
            Debug.LogWarning(
                $"MasterVolumeSettings: failed to set '{MixerParameter}' on mixer '{mixer.name}'. " +
                "Expose the Master group Volume parameter with that name.");
        }
    }

    public static float LinearToDecibels(float linear)
    {
        return Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
    }
}
