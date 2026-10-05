#nullable enable

using UnityEngine;

/// <summary>
/// Makes a particle system in a screen-space canvas emit from a thin frame just inside the screen's edges,
/// whatever the screen's shape: the element border's embers, snowflakes, sparks and so on.
/// </summary>
/// <remarks>
/// Sits on a ParticleSystem whose RectTransform stretches over the whole canvas. It builds the frame as the
/// system's Shape mesh (Mesh, emitting from its triangles) and rebuilds it when the canvas changes size, so
/// the emitter needs no per-aspect hand-fitting. Everything else about the particles — rate, size, colour,
/// motion (radial velocity = inward, orbital = circling the screen) — is the ParticleSystem's own, edited as
/// usual. Units are the canvas's reference pixels, so 10 = 10 px at 1080p on any device. The frame only
/// exists in Play (the border's particles only play then), so editing scenes leaves no overrides behind.
/// </remarks>
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(RectTransform))]
public class ScreenFrameEmitter : MonoBehaviour
{
    [Tooltip("How far in from the screen's edge the emitting band starts, in reference pixels. Negative starts " +
             "it off screen, so particles drift in from outside.")]
    [SerializeField] private float inset;
    [Tooltip("How deep the emitting band is, in reference pixels.")]
    [SerializeField, Min(0.1f)] private float bandWidth = 14f;

    private Mesh? frame;
    private Vector4 builtFor;   // size, inset and band width the frame was last built for

    private void OnEnable() => Fit();

    private void OnRectTransformDimensionsChange() => Fit();

    private void OnValidate()
    {
        if (Application.isPlaying && isActiveAndEnabled)
        {
            Fit();
        }
    }

    private void OnDestroy()
    {
        if (frame != null)
        {
            Destroy(frame);
        }
    }

    private void Fit()
    {
        Vector2 size = ((RectTransform)transform).rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        var wanted = new Vector4(size.x, size.y, inset, bandWidth);
        if (frame != null && wanted == builtFor)
        {
            return;
        }

        builtFor = wanted;

        // A fresh mesh each time, swapped in below: clearing the one the Shape module is using makes it complain.
        Mesh? previous = frame;
        frame = new Mesh { name = "ScreenFrame", hideFlags = HideFlags.DontSave };

        // Two rectangles, outer and inner, joined by four quads: a frame of even width round the screen.
        float outerX = size.x * 0.5f - inset;
        float outerY = size.y * 0.5f - inset;
        float innerX = Mathf.Max(outerX - bandWidth, 0f);
        float innerY = Mathf.Max(outerY - bandWidth, 0f);
        frame.vertices = new[]
        {
            new Vector3(-outerX, -outerY), new Vector3(outerX, -outerY), new Vector3(outerX, outerY), new Vector3(-outerX, outerY),
            new Vector3(-innerX, -innerY), new Vector3(innerX, -innerY), new Vector3(innerX, innerY), new Vector3(-innerX, innerY),
        };
        frame.triangles = new[]
        {
            0, 1, 5, 0, 5, 4,   // bottom
            1, 2, 6, 1, 6, 5,   // right
            2, 3, 7, 2, 7, 6,   // top
            3, 0, 4, 3, 4, 7,   // left
        };
        frame.RecalculateNormals();
        frame.RecalculateBounds();

        ParticleSystem.ShapeModule shape = GetComponent<ParticleSystem>().shape;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
        shape.mesh = frame;

        if (previous != null)
        {
            Destroy(previous);
        }
    }
}
