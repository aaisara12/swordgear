using UnityEngine;

public static class ElementVisualUtility
{
    public static Color GetAccentColor(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                return new Color(1f, 0.28f, 0.12f, 1f);
            case Element.Ice:
                return new Color(0.35f, 0.88f, 1f, 1f);
            case Element.Lightning:
                return new Color(1f, 0.92f, 0.2f, 1f);
            case Element.Wind:
                return new Color(0.56f, 0.93f, 0.56f, 1f);
            default:
                return new Color(0.92f, 0.92f, 0.92f, 1f);
        }
    }

    public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    public static Color GetChargeOverlayColor(Element element, float progress)
    {
        Color peakColor = element switch
        {
            Element.Fire => new Color(1f, 1f, 1f, 1f),
            Element.Ice => new Color(0.92f, 1f, 1f, 1f),
            Element.Lightning => new Color(1f, 1f, 0.95f, 1f),
            _ => new Color(1f, 1f, 1f, 1f),
        };

        Color endColor = element switch
        {
            Element.Fire => new Color(0.95f, 0.9f, 0.85f, 1f),
            Element.Ice => new Color(0.72f, 0.94f, 1f, 1f),
            Element.Lightning => new Color(0.95f, 0.96f, 0.88f, 1f),
            _ => new Color(0.95f, 0.95f, 0.95f, 1f),
        };

        return Color.Lerp(peakColor, endColor, progress);
    }

    public static Color GetChargeGlowColor(Element element, float progress)
    {
        Color peakColor = element switch
        {
            Element.Fire => new Color(1f, 0.42f, 0.32f, 1f),
            Element.Ice => new Color(0.45f, 0.78f, 1f, 1f),
            Element.Lightning => new Color(1f, 0.92f, 0.35f, 1f),
            _ => new Color(1f, 1f, 1f, 1f),
        };

        return Color.Lerp(peakColor, GetAccentColor(element), progress);
    }

    public static Color GetChargeParticleColor(Element element, float progress)
    {
        Color endColor = element switch
        {
            Element.Fire => new Color(1f, 0.5f, 0.15f, 1f),
            Element.Ice => new Color(0.5f, 0.88f, 1f, 1f),
            Element.Lightning => new Color(1f, 0.98f, 0.4f, 1f),
            _ => new Color(1f, 1f, 1f, 1f),
        };

        Color peakColor = element switch
        {
            Element.Fire => new Color(1f, 0.88f, 0.45f, 1f),
            Element.Ice => new Color(0.82f, 1f, 1f, 1f),
            Element.Lightning => new Color(1f, 1f, 0.75f, 1f),
            _ => new Color(1f, 1f, 1f, 1f),
        };

        return Color.Lerp(peakColor, endColor, progress);
    }

    /// <summary>Single soft bump triggered once when max charge is first reached. Ramps up then decays.</summary>
    public static float StepMaxChargePulse(
        bool isMaxCharge,
        ref MaxChargePulseTracker tracker,
        float pulseDuration,
        float attackDuration = 0.12f)
    {
        if (isMaxCharge && !tracker.WasMaxCharge)
        {
            tracker.PulseTimer = pulseDuration;
        }

        tracker.WasMaxCharge = isMaxCharge;

        if (tracker.PulseTimer <= 0f || pulseDuration <= 0f)
        {
            return 0f;
        }

        float elapsed = pulseDuration - tracker.PulseTimer;
        float envelope;
        if (elapsed < attackDuration)
        {
            envelope = Mathf.SmoothStep(0f, 1f, elapsed / attackDuration);
        }
        else
        {
            float decayDuration = Mathf.Max(0.01f, pulseDuration - attackDuration);
            envelope = Mathf.SmoothStep(1f, 0f, (elapsed - attackDuration) / decayDuration);
        }

        tracker.PulseTimer = Mathf.Max(0f, tracker.PulseTimer - Time.unscaledDeltaTime);
        return envelope;
    }
}

public struct MaxChargePulseTracker
{
    public bool WasMaxCharge;
    public float PulseTimer;

    public void Reset()
    {
        WasMaxCharge = false;
        PulseTimer = 0f;
    }
}
