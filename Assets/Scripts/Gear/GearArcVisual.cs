#nullable enable

using UnityEngine;

/// <summary>
/// Procedural annulus-sector ("wedge") renderer for a single gear arc. Colour is baked into vertex
/// colours so every arc on a gear can share one material per element.
/// </summary>
/// <remarks>
/// The arc's state — aimed at, active, how much imbue is left — is eased here and handed to the shader
/// through a MaterialPropertyBlock (<c>_Highlight</c>, <c>_Active</c>, <c>_Fill</c>; see
/// GearArcCommon.hlsl), so arcs that share a material still each show their own state. Easing lives in
/// code rather than an AnimationClip because the targets change with every frame of aiming and the arc is
/// a procedural mesh with no authored rig to animate.
/// <para>
/// The mesh carries two UV channels so shaders can flow along it and glow at its edges: TEXCOORD0 is
/// u along the arc (0..1) and v from inner edge (0) to outer edge (1); TEXCOORD1 is the same in world
/// units, for patterns that must not stretch with arc length.
/// </para>
/// </remarks>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GearArcVisual : MonoBehaviour
{
    private const int MinSegments = 2;

    private static readonly int HighlightId = Shader.PropertyToID("_Highlight");
    private static readonly int ActiveId = Shader.PropertyToID("_Active");
    private static readonly int FillId = Shader.PropertyToID("_Fill");

    /// <summary> How quickly the arc eases toward its targets, per second (exponential). </summary>
    public float EaseRate { get; set; } = 14f;

    private MeshFilter? meshFilter;
    private MeshRenderer? meshRenderer;
    private Mesh? mesh;
    private MaterialPropertyBlock? block;

    private Vector3[] vertices = System.Array.Empty<Vector3>();
    private Color[] colors = System.Array.Empty<Color>();
    private Vector2[] uvs = System.Array.Empty<Vector2>();
    private Vector2[] worldUvs = System.Array.Empty<Vector2>();
    private int[] triangles = System.Array.Empty<int>();

    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    private float highlight;
    private float targetHighlight;
    private float active;
    private float targetActive;
    private float fill = 1f;
    private float targetFill = 1f;
    private bool stateDirty = true;

    private void Awake() => EnsureBuilt();

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
            mesh = null;
        }
    }

    private void EnsureBuilt()
    {
        if (mesh != null)
        {
            return;
        }

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = new Mesh { name = "GearArc" };
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;
        block = new MaterialPropertyBlock();
    }

    public void SetMaterial(Material material)
    {
        EnsureBuilt();
        meshRenderer!.sharedMaterial = material;
        stateDirty = true;
    }

    public void SetSorting(int sortingLayerId, int sortingOrder)
    {
        EnsureBuilt();
        meshRenderer!.sortingLayerID = sortingLayerId;
        meshRenderer.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Sets what the arc should look like; it eases there over the next few frames.
    /// </summary>
    /// <param name="color">Element colour and alpha.</param>
    /// <param name="highlight01">1 while the player is aiming at this arc.</param>
    /// <param name="active01">1 while this arc's element is the active imbue.</param>
    public void SetTarget(Color color, float highlight01, float active01)
    {
        targetColor = color;
        targetHighlight = highlight01;
        targetActive = active01;
    }

    /// <summary> How much of the arc is lit, 0..1: the imbue timer draining along the active arc. </summary>
    public void SetFill(float fill01) => targetFill = Mathf.Clamp01(fill01);

    /// <summary> Jumps straight to the targets, for a freshly built arc that shouldn't fade in from white. </summary>
    public void SnapToTarget()
    {
        currentColor = targetColor;
        highlight = targetHighlight;
        active = targetActive;
        fill = targetFill;
        ApplyColor();
        stateDirty = true;
        ApplyState();
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-EaseRate * Time.unscaledDeltaTime);

        if (!Approximately(currentColor, targetColor))
        {
            currentColor = Color.Lerp(currentColor, targetColor, t);
            ApplyColor();
        }

        stateDirty |= Ease(ref highlight, targetHighlight, t)
                      | Ease(ref active, targetActive, t)
                      | Ease(ref fill, targetFill, t);
        ApplyState();
    }

    private static bool Ease(ref float value, float target, float t)
    {
        if (Mathf.Abs(value - target) < 0.001f)
        {
            if (value == target)
            {
                return false;
            }

            value = target;
            return true;
        }

        value = Mathf.Lerp(value, target, t);
        return true;
    }

    private static bool Approximately(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f &&
        Mathf.Abs(a.b - b.b) < 0.002f && Mathf.Abs(a.a - b.a) < 0.002f;

    private void ApplyColor()
    {
        if (colors.Length == 0)
        {
            return;
        }

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = currentColor;
        }

        EnsureBuilt();
        mesh!.colors = colors;
    }

    private void ApplyState()
    {
        if (!stateDirty || meshRenderer == null || block == null)
        {
            return;
        }

        meshRenderer.GetPropertyBlock(block);
        block.SetFloat(HighlightId, highlight);
        block.SetFloat(ActiveId, active);
        block.SetFloat(FillId, fill);
        meshRenderer.SetPropertyBlock(block);
        stateDirty = false;
    }

    /// <summary>
    /// Rebuilds the wedge in local space, centred on <paramref name="centerAngleDegrees"/> (0 = +X axis,
    /// increasing counter-clockwise) and spanning <paramref name="sweepDegrees"/>.
    /// </summary>
    public void Rebuild(
        float innerRadius,
        float outerRadius,
        float centerAngleDegrees,
        float sweepDegrees,
        int segments)
    {
        EnsureBuilt();

        segments = Mathf.Max(MinSegments, segments);
        int vertexCount = (segments + 1) * 2;

        if (vertices.Length != vertexCount)
        {
            vertices = new Vector3[vertexCount];
            colors = new Color[vertexCount];
            uvs = new Vector2[vertexCount];
            worldUvs = new Vector2[vertexCount];
            triangles = new int[segments * 6];
        }

        // World-unit length along the arc's middle, so world-space patterns keep their size on any ring.
        float midRadius = (innerRadius + outerRadius) * 0.5f;
        float arcLength = midRadius * sweepDegrees * Mathf.Deg2Rad;
        float thickness = outerRadius - innerRadius;

        float startDegrees = centerAngleDegrees - sweepDegrees * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float along = (float)i / segments;
            float radians = (startDegrees + sweepDegrees * along) * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

            int v = i * 2;
            vertices[v] = direction * innerRadius;
            vertices[v + 1] = direction * outerRadius;
            colors[v] = currentColor;
            colors[v + 1] = currentColor;
            uvs[v] = new Vector2(along, 0f);
            uvs[v + 1] = new Vector2(along, 1f);
            worldUvs[v] = new Vector2(along * arcLength, 0f);
            worldUvs[v + 1] = new Vector2(along * arcLength, thickness);
        }

        for (int i = 0; i < segments; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 3;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }

        mesh!.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, worldUvs);
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        // The swell pushes vertices outward in the shader; widen the bounds so the arc isn't culled mid-swell.
        Bounds bounds = mesh.bounds;
        bounds.Expand(2f);
        mesh.bounds = bounds;
    }
}
