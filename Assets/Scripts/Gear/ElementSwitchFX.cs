#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ElementBurstPrefab
{
    public Element element;
    public GameObject? prefab;
}

/// <summary>
/// The element-switch moment. When a flick grants an element the arc flares (GearManager does that), energy
/// streaks from the arc into the player, and as it arrives the element's own burst blooms round them, a
/// tinted shockwave rolls out across the arena, the camera kicks along the streak and the player's light
/// flashes, all to the element's own switch sound. Big but fast — under half a second — and it never pauses
/// play.
/// </summary>
/// <remarks>
/// Every visual is an authored prefab under Assets/Visuals/Prefabs/ElementFX; this only listens for
/// <see cref="GearManager.OnElementGranted"/>, places the prefabs, and sets the streak's and shockwave's tint
/// and the streak's speed. The bursts carry their element's colours themselves.
/// </remarks>
public class ElementSwitchFX : MonoBehaviour
{
    /// <summary>
    /// The streak reached the player and the burst went off: the switch's moment of impact, for effects that
    /// should land with it rather than with the flick (the screen's flare and post-FX pulse).
    /// </summary>
    public static event System.Action<Element>? OnSwitchLanded;

    [SerializeField] private GameObject? streakPrefab;
    [SerializeField] private GameObject? shockwavePrefab;
    [Tooltip("One burst per element. An element without one still gets the streak and the shockwave.")]
    [SerializeField] private List<ElementBurstPrefab> bursts = new();
    [Tooltip("Seconds the streak takes from the arc to the player; the burst lands as it arrives. Must match " +
             "the streak prefab's particle lifetime.")]
    [SerializeField] private float streakSeconds = 0.14f;
    [SerializeField] private float cameraKick = 0.8f;
    [Tooltip("Extra intensity flashed on the player's light as the burst lands.")]
    [SerializeField] private float lightFlash = 1.5f;

    private void Awake()
    {
        if (streakPrefab == null)
        {
            Debug.LogError("ElementSwitchFX: streakPrefab is null", this);
        }

        if (shockwavePrefab == null)
        {
            Debug.LogError("ElementSwitchFX: shockwavePrefab is null", this);
        }
    }

    private void OnEnable()
    {
        GearManager.OnElementGranted += HandleElementGranted;
    }

    private void OnDisable()
    {
        GearManager.OnElementGranted -= HandleElementGranted;
    }

    private void HandleElementGranted(Element element, int arcIndex)
    {
        // The sound's impact sits ~0.14s in, so it plays from the flick and lands with the burst.
        AudioSystem.Sound? sound = SwitchSound(element);
        if (sound.HasValue)
        {
            AudioSystem.Play(sound.Value);
        }

        GearManager? gear = GearManager.Instance;
        GameObject? player = GameManager.Instance != null ? GameManager.Instance.player : null;

        if (gear == null || player == null || PrefabPool.Instance == null || !gear.TryGetArcCentre(arcIndex, out Vector3 from))
        {
            return;
        }

        StartCoroutine(PlaySwitch(element, from, player.transform));
    }

    private IEnumerator PlaySwitch(Element element, Vector3 from, Transform player)
    {
        Color color = ElementVisuals.GetColor(element);
        Vector3 toPlayer = player.position - from;

        // The streak's prefab fires its particles along its local up; aim that at the player and set the speed
        // so they arrive as the burst lands.
        if (streakPrefab != null)
        {
            GameObject streak = Spawn(streakPrefab, from, Quaternion.LookRotation(Vector3.forward, toPlayer));
            Tint(streak, color);
            foreach (ParticleSystem system in streak.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = system.main;
                main.startSpeed = toPlayer.magnitude / Mathf.Max(streakSeconds, 0.01f);
            }
        }

        yield return new WaitForSeconds(streakSeconds);

        if (player == null)
        {
            yield break;
        }

        Vector3 at = player.position;
        GameObject? burst = FindBurst(element);

        if (burst != null)
        {
            Spawn(burst, at, Quaternion.identity);
        }

        if (shockwavePrefab != null)
        {
            // A touch paler than the element, so the wave reads as a flash of light rolling out.
            Tint(Spawn(shockwavePrefab, at, Quaternion.identity), Color.Lerp(color, Color.white, 0.3f));
        }

        Testing.CinemachineTrackingTargetFromGameManagerSetter.Shake(cameraKick, toPlayer);
        OnSwitchLanded?.Invoke(element);

        LightFlash? flash = player.GetComponentInChildren<LightFlash>();
        if (flash != null)
        {
            flash.Flash(lightFlash);
        }
    }

    private static AudioSystem.Sound? SwitchSound(Element element) => element switch
    {
        Element.Fire => AudioSystem.Sound.Switch_Fire,
        Element.Ice => AudioSystem.Sound.Switch_Ice,
        Element.Lightning => AudioSystem.Sound.Switch_Lightning,
        Element.Wind => AudioSystem.Sound.Switch_Wind,
        Element.Earth => AudioSystem.Sound.Switch_Earth,
        Element.Dark => AudioSystem.Sound.Switch_Dark,
        Element.Light => AudioSystem.Sound.Switch_Light,
        _ => null,
    };

    private GameObject? FindBurst(Element element)
    {
        foreach (ElementBurstPrefab entry in bursts)
        {
            if (entry.element == element)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    /// <summary> Spawns a one-shot effect from the pool and returns it once its particles have played out. </summary>
    private static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject obj = PrefabPool.Instance!.Spawn(prefab, position, rotation);
        foreach (ParticleSystem system in obj.GetComponentsInChildren<ParticleSystem>())
        {
            system.Play(false);
        }

        if (obj.TryGetComponent(out PooledInstance pooled))
        {
            pooled.ReleaseWhenParticlesDone();
        }

        return obj;
    }

    /// <summary> Recolours every particle system in an effect, keeping each one's authored alpha. </summary>
    private static void Tint(GameObject obj, Color color)
    {
        foreach (ParticleSystem system in obj.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = system.main;
            Color tinted = color;
            tinted.a = main.startColor.color.a;
            main.startColor = tinted;
        }
    }
}
