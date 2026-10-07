#nullable enable

using UnityEngine;

/// <summary>
/// Procedural annulus-sector ("wedge") renderer for a single gear arc. Colour is baked into vertex
/// colours so every arc on a gear can share one material per element.
/// </summary>
/// <remarks>
/// The arc's state — aimed at, active — is eased here and handed to the shader
/// through a MaterialPropertyBlock (<c>_Highlight</c>, <c>_Active</c>; see
/// GearArcCommon.hlsl), so arcs that share a material still each show their own state. Easing lives in
/// code rather than an AnimationClip because the targets change with every frame of aiming and the arc is
/// a procedural mesh with no authored rig to animate.
/// <para>
/// The mesh carries two UV channels so shaders can flow along it and glow at its edges: TEXCOORD0 is
/// u along the arc (0..1) and v from inner edge (0) to outer edge (1); TEXCOORD1 is the same in world
/// units, for patterns that must not stretch with arc length.
/// </para>
/// <para>
/// Around the band sit overflow rows (inside and outside) and overflow at both ends, built with zero area:
/// TEXCOORD2 tells the shader how far each vertex may open. The shader opens them only while the arc is
/// aimed at or active, so the active element can spill past the gear while idle arcs cost no extra fill.
/// </para>
/// </remarks>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GearArcVisual : MonoBehaviour
{
    private const int MinSegments = 2;

    // Vertex rows per column, inner to outer: inner overflow, inner edge, outer edge, outer overflow.
    private const int Rows = 4;

    private static readonly int HighlightId = Shader.PropertyToID("_Highlight");
    private static readonly int ActiveId = Shader.PropertyToID("_Active");
    private static readonly int ArcShapeId = Shader.PropertyToID("_ArcShape");
    private static readonly int FlareTimeId = Shader.PropertyToID("_FlareTime");

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
    private Vector2[] overflows = System.Array.Empty<Vector2>();
    private int[] triangles = System.Array.Empty<int>();
    private Vector4 arcShape;
    private ArcBits? bits;

    private Color currentColor = Color.white;
    private Color targetColor = Color.white;
    private float highlight;
    private float targetHighlight;
    private float active;
    private float targetActive;
    private float flareTime = -100f;
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

    /// <summary>
    /// The arc's loose pieces (an ArcBits instance parented under it): fitted to the arc whenever it's built,
    /// and flying while the arc is active.
    /// </summary>
    public void SetBits(ArcBits? arcBits)
    {
        bits = arcBits;
        if (bits != null)
        {
            bits.Fit(arcShape.y, arcShape.z, arcShape.w * Mathf.Rad2Deg, arcShape.x * Mathf.Rad2Deg);
        }
    }

    /// <summary> Lets the pieces sit back with the rest of the gear (the arc itself reads the global fade). </summary>
    public void SetPieceRecede(float fadeOut, float desaturate)
    {
        if (bits != null)
        {
            bits.SetRecede(fadeOut, desaturate);
        }
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

        if (bits != null)
        {
            bits.SetPlaying(active01 >= 0.5f);
        }
    }

    /// <summary>
    /// Flares the arc: a blaze and an outward pop that die away in a fraction of a second, for the moment its
    /// element is granted. Plays out in the shader from the flare's time, so nothing here animates.
    /// </summary>
    public void Flare()
    {
        flareTime = Time.time;   // the clock URP feeds the shader's _Time.y
        stateDirty = true;
    }

    /// <summary> Jumps straight to the targets, for a freshly built arc that shouldn't fade in from white. </summary>
    public void SnapToTarget()
    {
        currentColor = targetColor;
        highlight = targetHighlight;
        active = targetActive;
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
                      | Ease(ref active, targetActive, t);
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
        block.SetVector(ArcShapeId, arcShape);
        block.SetFloat(FlareTimeId, flareTime);
        meshRenderer.SetPropertyBlock(block);
        stateDirty = false;
    }

    /// <summary>
    /// Rebuilds the wedge in local space, centred on <paramref name="centerAngleDegrees"/> (0 = +X axis,
    /// increasing counter-clockwise) and spanning <paramref name="sweepDegrees"/>.
    /// </summary>
    /// <param name="overflowInner">How far, in world units, the arc may spill inward past its band when in play.</param>
    /// <param name="overflowOuter">How far it may spill outward past its band.</param>
    /// <param name="overflowAlong">How far it may spill past each end, in world units at the mid radius.</param>
    public void Rebuild(
        float innerRadius,
        float outerRadius,
        float centerAngleDegrees,
        float sweepDegrees,
        int segments,
        float overflowInner = 0f,
        float overflowOuter = 0f,
        float overflowAlong = 0f)
    {
        EnsureBuilt();

        segments = Mathf.Max(MinSegments, segments);
        int vertexCount = (segments + 1) * Rows;

        if (vertices.Length != vertexCount)
        {
            vertices = new Vector3[vertexCount];
            colors = new Color[vertexCount];
            uvs = new Vector2[vertexCount];
            worldUvs = new Vector2[vertexCount];
            overflows = new Vector2[vertexCount];
            triangles = new int[segments * (Rows - 1) * 6];
        }

        // World-unit length along the arc's middle, so world-space patterns keep their size on any ring.
        float midRadius = (innerRadius + outerRadius) * 0.5f;
        float arcLength = midRadius * sweepDegrees * Mathf.Deg2Rad;
        float thickness = outerRadius - innerRadius;
        float endTurn = midRadius > 0f ? overflowAlong / midRadius : 0f;

        float startDegrees = centerAngleDegrees - sweepDegrees * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float along = (float)i / segments;
            float radians = (startDegrees + sweepDegrees * along) * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            float turn = i == 0 ? -endTurn : i == segments ? endTurn : 0f;

            // Overflow rows start on the band's edges; the shader moves them out by their overflow.
            int v = i * Rows;
            for (int row = 0; row < Rows; row++)
            {
                bool outer = row >= 2;
                float spill = row == 0 ? -overflowInner : row == Rows - 1 ? overflowOuter : 0f;
                vertices[v + row] = direction * (outer ? outerRadius : innerRadius);
                colors[v + row] = currentColor;
                uvs[v + row] = new Vector2(along, outer ? 1f : 0f);
                worldUvs[v + row] = new Vector2(along * arcLength, outer ? thickness : 0f);
                overflows[v + row] = new Vector2(spill, turn);
            }
        }

        for (int i = 0; i < segments; i++)
        {
            for (int row = 0; row < Rows - 1; row++)
            {
                int v = i * Rows + row;
                int next = v + Rows;
                int t = (i * (Rows - 1) + row) * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = next + 1;
                triangles[t + 3] = v;
                triangles[t + 4] = next + 1;
                triangles[t + 5] = next;
            }
        }

        mesh!.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, worldUvs);
        mesh.SetUVs(2, overflows);
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        // The shader pushes vertices outward (the swell, the overflow); widen the bounds so the arc isn't
        // culled while they're out.
        Bounds bounds = mesh.bounds;
        bounds.Expand(2f * (Mathf.Max(overflowOuter, overflowAlong) + 1f));
        mesh.bounds = bounds;

        arcShape = new Vector4(sweepDegrees * Mathf.Deg2Rad, innerRadius, outerRadius, centerAngleDegrees * Mathf.Deg2Rad);
        stateDirty = true;
        ApplyState();

        if (bits != null)
        {
            bits.Fit(innerRadius, outerRadius, centerAngleDegrees, sweepDegrees);
        }
    }
}
