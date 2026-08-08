#nullable enable

using UnityEngine;

/// <summary>
/// Procedural annulus-sector ("wedge") renderer for a single gear arc. Colour is baked into vertex
/// colours so every arc on a gear can share one unlit material.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GearArcVisual : MonoBehaviour
{
    private const int MinSegments = 2;

    private MeshFilter? meshFilter;
    private MeshRenderer? meshRenderer;
    private Mesh? mesh;

    private Vector3[] vertices = System.Array.Empty<Vector3>();
    private Color[] colors = System.Array.Empty<Color>();
    private int[] triangles = System.Array.Empty<int>();
    private Color currentColor = Color.white;

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
    }

    public void SetMaterial(Material material)
    {
        EnsureBuilt();
        meshRenderer!.sharedMaterial = material;
    }

    public void SetSorting(int sortingLayerId, int sortingOrder)
    {
        EnsureBuilt();
        meshRenderer!.sortingLayerID = sortingLayerId;
        meshRenderer.sortingOrder = sortingOrder;
    }

    public void SetColor(Color color)
    {
        if (currentColor == color)
        {
            return;
        }

        currentColor = color;

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
            triangles = new int[segments * 6];
        }

        float startDegrees = centerAngleDegrees - sweepDegrees * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float radians = (startDegrees + sweepDegrees * i / segments) * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);

            int v = i * 2;
            vertices[v] = direction * innerRadius;
            vertices[v + 1] = direction * outerRadius;
            colors[v] = currentColor;
            colors[v + 1] = currentColor;
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
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
