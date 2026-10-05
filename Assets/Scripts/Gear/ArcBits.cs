#nullable enable

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ArcBitsEmitter
{
    public ParticleSystem? system;
    [Tooltip("Where across the arc it emits from: world units out from the arc's outer edge (negative = inside " +
             "the band). Particles start anywhere between From and To.")]
    public float from;
    public float to;
}

/// <summary>
/// The loose pieces that fly off an element's arc while it's active — flame bits, twinkles, sparks, swirls,
/// boulders, notes — as ordinary particle systems, edited with Unity's particle tools.
/// </summary>
/// <remarks>
/// GearArcVisual parents an instance under the arc and calls <see cref="Fit"/> whenever the arc is built, which
/// shapes each emitter to the arc: a Circle sweeping the arc's angle, at the band set on its entry here.
/// Particles leave the arc outward (Start Speed); Velocity over Lifetime's Radial pulls them back toward the
/// gear (negative) or pushes them away. Simulate in Local space so they ride along as the gear follows the
/// player. Everything else — rates, sizes, colours, materials — is the particle systems' own.
/// </remarks>
public class ArcBits : MonoBehaviour
{
    [SerializeField] private List<ArcBitsEmitter> emitters = new();

    private bool playing;

    /// <summary> Shapes every emitter to an arc, in the arc's own space (centred on the gear). </summary>
    public void Fit(float innerRadius, float outerRadius, float centerAngleDegrees, float sweepDegrees)
    {
        foreach (ArcBitsEmitter emitter in emitters)
        {
            if (emitter.system == null)
            {
                continue;
            }

            float near = outerRadius + Mathf.Min(emitter.from, emitter.to);
            float far = outerRadius + Mathf.Max(emitter.from, emitter.to);
            far = Mathf.Max(far, 0.01f);

            ParticleSystem.ShapeModule shape = emitter.system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(0f, 0f, centerAngleDegrees - sweepDegrees * 0.5f);
            shape.scale = Vector3.one;
            shape.radius = far;
            shape.radiusThickness = Mathf.Clamp01((far - Mathf.Max(near, 0f)) / far);
            shape.arc = sweepDegrees;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        }
    }

    /// <summary> Starts or stops the pieces; ones already out finish their flight. </summary>
    public void SetPlaying(bool play)
    {
        if (play == playing)
        {
            return;
        }

        playing = play;
        foreach (ArcBitsEmitter emitter in emitters)
        {
            if (emitter.system == null)
            {
                continue;
            }

            if (play)
            {
                emitter.system.Play(false);
            }
            else
            {
                emitter.system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
