#nullable enable

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ElementAura
{
    public Element element;
    public ParticleSystem? particles;
}

/// <summary>
/// The player carries their imbue. While an element is imbued the held sword takes its colour and a glowing
/// silhouette in it, and a light aura of the element's particles hangs round the player (embers, snowflakes,
/// sparks, wind streaks, dust motes, smoke wisps, opal sparkles). In the imbue's last seconds the glow
/// flickers; when it runs out the sword fizzles — a puff of smoke, dying sparks and a hiss — so expiry is seen
/// and heard rather than silent.
/// </summary>
/// <remarks>
/// The glow, auras and fizzle are authored in Player.prefab and ElementFX; this only tints, starts and stops
/// them and spawns the fizzle.
/// </remarks>
public class ImbuedPlayerVisual : MonoBehaviour
{
    [Tooltip("The held sword (PlayerWeaponIndicator's renderer); tinted while imbued.")]
    [SerializeField] private SpriteRenderer? swordRenderer;
    [Tooltip("An unlit copy of the sword, scaled up behind it: its glowing silhouette.")]
    [SerializeField] private SpriteRenderer? glowRenderer;
    [SerializeField] private List<ElementAura> auras = new();
    [SerializeField] private GameObject? fizzlePrefab;
    [Tooltip("How far the sword's colour leans toward the element.")]
    [SerializeField, Range(0f, 1f)] private float tintStrength = 0.75f;
    [Tooltip("HDR brightness of the glow, so it blooms.")]
    [SerializeField] private float glowIntensity = 2.5f;
    [Tooltip("Seconds before the imbue ends that the glow starts to flicker.")]
    [SerializeField] private float flickerSeconds = 3f;

    private Element element = Element.Physical;
    private bool glowLit;
    private float lastRemaining = float.MaxValue;
    private float flickerPhase;
    private GameManager? timerSource;

    private void Awake()
    {
        if (swordRenderer == null)
        {
            Debug.LogError("ImbuedPlayerVisual: swordRenderer is null", this);
        }

        if (glowRenderer == null)
        {
            Debug.LogError("ImbuedPlayerVisual: glowRenderer is null", this);
        }

        if (fizzlePrefab == null)
        {
            Debug.LogError("ImbuedPlayerVisual: fizzlePrefab is null", this);
        }
    }

    private void OnEnable()
    {
        ElementManager.OnActiveElementChanged += HandleActiveElementChanged;
        Show(ElementManager.Instance != null ? ElementManager.Instance.ActiveElement : Element.Physical);
    }

    private void OnDisable()
    {
        ElementManager.OnActiveElementChanged -= HandleActiveElementChanged;
        TrackImbueTimer(null);
    }

    private void Update()
    {
        TrackImbueTimer(GameManager.Instance);
    }

    private void LateUpdate()
    {
        // The glow belongs to the held sword: PlayerWeaponIndicator hides the sword while it's thrown.
        if (glowRenderer != null)
        {
            glowRenderer.enabled = glowLit && swordRenderer != null && swordRenderer.enabled;
        }
    }

    private void HandleActiveElementChanged(Element next)
    {
        // Switching element goes straight from one to the other, so a drop to Physical is an imbue ending. Only
        // fizzle if it ran out: a new arena also resets to Physical, and that's no moment to hiss at the player.
        if (next == Element.Physical && element != Element.Physical && lastRemaining < 0.1f)
        {
            Fizzle(element);
        }

        Show(next);
    }

    private void Show(Element next)
    {
        element = next;
        bool imbued = next != Element.Physical;
        Color colour = ElementVisuals.GetColor(next);

        if (swordRenderer != null)
        {
            swordRenderer.color = imbued ? Color.Lerp(Color.white, colour, tintStrength) : Color.white;
        }

        if (glowRenderer != null)
        {
            glowRenderer.color = new Color(colour.r * glowIntensity, colour.g * glowIntensity, colour.b * glowIntensity, 1f);
        }

        glowLit = imbued;
        lastRemaining = float.MaxValue;

        foreach (ElementAura aura in auras)
        {
            if (aura.particles == null)
            {
                continue;
            }

            if (imbued && aura.element == next)
            {
                aura.particles.Play(true);
            }
            else
            {
                aura.particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    /// <summary> In the imbue's last seconds the glow blinks, faster as it runs out. </summary>
    private void HandleImbueTimerChanged(float remaining, float duration)
    {
        if (element == Element.Physical)
        {
            return;
        }

        lastRemaining = remaining;

        if (remaining > flickerSeconds)
        {
            glowLit = true;
            flickerPhase = 0f;
            return;
        }

        // Integrate the blink's phase: the clock times a changing rate would sweep the frequency far past the
        // rate, into random flicker.
        float urgency = 1f - Mathf.Clamp01(remaining / Mathf.Max(flickerSeconds, 0.01f));
        flickerPhase += Mathf.Lerp(3f, 10f, urgency) * Time.deltaTime;
        glowLit = Mathf.Repeat(flickerPhase, 1f) < 0.6f;
    }

    private void Fizzle(Element from)
    {
        AudioSystem.Play(AudioSystem.Sound.Imbue_Fizzle);

        if (fizzlePrefab == null || PrefabPool.Instance == null)
        {
            return;
        }

        Vector3 at = swordRenderer != null && swordRenderer.enabled ? swordRenderer.transform.position : transform.position;
        GameObject fizzle = PrefabPool.Instance.Spawn(fizzlePrefab, at, Quaternion.identity);

        // The dying sparks keep a little of the element they were.
        Color faded = Color.Lerp(ElementVisuals.GetColor(from), Color.grey, 0.4f);
        foreach (ParticleSystem system in fizzle.GetComponentsInChildren<ParticleSystem>())
        {
            if (system.name == "Sparks")
            {
                ParticleSystem.MainModule main = system.main;
                main.startColor = faded;
            }

            system.Play(false);
        }

        if (fizzle.TryGetComponent(out PooledInstance pooled))
        {
            pooled.ReleaseWhenParticlesDone();
        }
    }

    private void TrackImbueTimer(GameManager? source)
    {
        if (timerSource == source)
        {
            return;
        }

        if (timerSource is not null)
        {
            timerSource.OnEmpowermentTimerChanged -= HandleImbueTimerChanged;
        }

        timerSource = source;

        if (timerSource is not null)
        {
            timerSource.OnEmpowermentTimerChanged += HandleImbueTimerChanged;
        }
    }
}
