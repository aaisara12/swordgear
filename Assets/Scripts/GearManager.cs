#nullable enable

using UnityEngine;
using System.Collections.Generic;

public enum GearTile
{
    Debug,
    Bumper,
    Lightning,
    Fire,
    Ice,
    Wind,
    Earth,
    Dark,
    Light,
}

public static class GearTileElements
{
    /// <summary> Maps a tile to the element it grants; false for tiles that aren't elemental. </summary>
    public static bool TryGetElement(GearTile tile, out Element element)
    {
        switch (tile)
        {
            case GearTile.Fire: element = Element.Fire; return true;
            case GearTile.Ice: element = Element.Ice; return true;
            case GearTile.Lightning: element = Element.Lightning; return true;
            case GearTile.Wind: element = Element.Wind; return true;
            case GearTile.Earth: element = Element.Earth; return true;
            case GearTile.Dark: element = Element.Dark; return true;
            case GearTile.Light: element = Element.Light; return true;
            default: element = Element.Physical; return false;
        }
    }
}

/// <summary>
/// The ring that orbits the player. It holds one arc per <em>equipped</em> element, evenly distributed;
/// flicking the attack stick toward an arc grants that arc's element.
/// The gear does not rotate — arc angles are fixed so the flick direction is a stable, learnable target.
///
/// The arc count is derived from the loadout rather than authored, because the flick target size is
/// 360/count degrees: showing all elements at once would shrink every target and make selection feel
/// random. Equipping fewer elements is what keeps them hittable.
/// </summary>
public class GearManager : InitializeableGameComponent
{
    [Header("Arc Layout")]
    [Tooltip("Elements equipped at run start, one arc each. The slot-purchase system grows this at " +
             "runtime via SetEquippedLoadout.")]
    [SerializeField]
    private List<GearTile> startingLoadout = new()
    {
        GearTile.Wind,
        GearTile.Fire,
        GearTile.Ice,
        GearTile.Lightning,
        GearTile.Earth,
    };
    [Tooltip("Distance from the gear centre to the middle of the arc band.")]
    [SerializeField] private float radius = 10f;
    [Tooltip("Radial thickness of the arc band, centred on 'radius'.")]
    [SerializeField] private float arcThickness = 3f;
    [Tooltip("Empty degrees left between neighbouring arcs so they read as separate targets.")]
    [SerializeField] private float arcGapDegrees = 4f;
    [Tooltip("Rotates the whole layout. At 0 the first arc is centred on the +X axis.")]
    [SerializeField] private float arcOffsetDegrees = 0f;
    [Tooltip("Mesh subdivisions per arc. Higher = smoother curve.")]
    [SerializeField, Min(2)] private int segmentsPerArc = 12;

    [Header("Arc Rendering")]
    [Tooltip("Optional. Leave empty to build an unlit vertex-coloured material at runtime.")]
    [SerializeField] private Material? arcMaterial;
    [SerializeField] private string arcSortingLayer = "Default";
    [SerializeField] private int arcSortingOrder = 4;
    [SerializeField, Range(0f, 1f)] private float filledArcAlpha = 0.7f;
    [SerializeField, Range(0f, 1f)] private float emptyArcAlpha = 0.2f;
    [SerializeField] private Color emptyArcColor = new(0.5f, 0.5f, 0.5f, 1f);

    [Header("Imbue Grant")]
    [Tooltip("How long the element granted by a flick lasts.")]
    [SerializeField] private float imbueDuration = 5f;
    [SerializeField] private float imbueDamageMultiplier = 1.2f;

    [Header("Follow Settings")]
    [Tooltip("How fast the gear eases toward the player (lower = more trailing slide / more 'alive', higher = tighter).")]
    [SerializeField] private float followLagDecay = 8f;

    public static GearManager? Instance;

    private readonly List<GearArcVisual> arcVisuals = new();

    // runtime gear state — one entry per arc
    private List<GearTile?> slotTiles = new();

    // inventory
    private readonly List<GearTile> inventoryTiles = new();

    private Material? runtimeArcMaterial;
    private int highlightedArc = -1;

    /// <summary> One arc per equipped element — derived from the loadout, never authored directly. </summary>
    public int ArcCount => slotTiles.Count;
    public IReadOnlyList<GearTile?> GetSlots() => slotTiles;
    public IReadOnlyList<GearTile> GetInventory() => inventoryTiles;

    private float ArcStepDegrees => 360f / Mathf.Max(1, ArcCount);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetEquippedLoadout(startingLoadout);

        inventoryTiles.Add(GearTile.Fire);
        inventoryTiles.Add(GearTile.Fire);
        inventoryTiles.Add(GearTile.Ice);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (runtimeArcMaterial != null)
        {
            Destroy(runtimeArcMaterial);
            runtimeArcMaterial = null;
        }
    }

    /// <summary>
    /// Replaces the equipped elements and resizes the ring to match — one arc per element. Call this
    /// whenever the loadout changes (run start, buying an element slot).
    /// </summary>
    /// <remarks>
    /// Every arc must map to an element: an empty arc is a flick that silently does nothing, which reads
    /// as a bug rather than a miss. Taking the loadout wholesale rather than exposing an arc count is
    /// what keeps that guaranteed.
    /// </remarks>
    public void SetEquippedLoadout(IReadOnlyList<GearTile>? loadout)
    {
        slotTiles = new List<GearTile?>();

        if (loadout == null || loadout.Count == 0)
        {
            // No arcs means every flick whiffs. Survivable, but it's always a config error.
            Debug.LogWarning("GearManager: empty loadout — the gear ring has no arcs, so element flicks will do nothing.", this);
        }
        else
        {
            for (int i = 0; i < loadout.Count; i++)
            {
                slotTiles.Add(loadout[i]);
            }
        }

        BuildArcs();
        RefreshVisuals();
    }

    // ---------- Arc geometry ----------

    /// <summary> Centre angle of an arc in gear-local degrees (0 = +X axis, counter-clockwise). </summary>
    public float GetArcLocalAngle(int index) => arcOffsetDegrees + index * ArcStepDegrees;

    /// <summary>
    /// Which arc a world-space direction points at. Arcs tile the full circle with no dead zones, so any
    /// non-zero direction resolves to exactly one arc (the visual gap is cosmetic, not a miss window).
    /// </summary>
    public bool TryGetArcIndex(Vector2 worldDirection, out int index)
    {
        index = -1;

        if (ArcCount <= 0 || worldDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        float step = ArcStepDegrees;
        float worldAngle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
        float localAngle = worldAngle - transform.eulerAngles.z - arcOffsetDegrees;

        // Shift by half a step so arc i owns [i*step, (i+1)*step) after the floor.
        float shifted = Mathf.Repeat(localAngle + step * 0.5f, 360f);
        index = Mathf.Clamp(Mathf.FloorToInt(shifted / step), 0, ArcCount - 1);
        return true;
    }

    public bool TryGetArcElement(int index, out Element element)
    {
        element = Element.Physical;

        if (index < 0 || index >= slotTiles.Count || !slotTiles[index].HasValue)
        {
            return false;
        }

        return GearTileElements.TryGetElement(slotTiles[index]!.Value, out element);
    }

    // ---------- Flick interaction ----------

    /// <summary>
    /// Resolves a flick direction to an arc and grants that arc's element. Returns false when the arc is
    /// empty or holds a non-elemental tile, so the caller can treat the flick as a whiff.
    /// </summary>
    public bool TryGrantElementFromDirection(Vector2 worldDirection, out Element granted)
    {
        granted = Element.Physical;

        if (!TryGetArcIndex(worldDirection, out int index) || !TryGetArcElement(index, out granted))
        {
            return false;
        }

        if (GameManager.Instance == null)
        {
            return false;
        }

        GameManager.Instance.ApplyEmpowerment(granted, imbueDamageMultiplier, imbueDuration);
        return true;
    }

    public bool TryGrantElementFromDirection(Vector2 worldDirection) =>
        TryGrantElementFromDirection(worldDirection, out _);

    /// <summary> Lights up the arc a flick in this direction would grab. Pass a zero direction to clear. </summary>
    public void HighlightArcForDirection(Vector2 worldDirection)
    {
        SetHighlightedArc(TryGetArcIndex(worldDirection, out int index) ? index : -1);
    }

    public void ClearArcHighlight() => SetHighlightedArc(-1);

    private void SetHighlightedArc(int index)
    {
        if (highlightedArc == index)
        {
            return;
        }

        int previous = highlightedArc;
        highlightedArc = index;
        ApplyArcColor(previous);
        ApplyArcColor(highlightedArc);
    }

    // ---------- Tile / inventory state ----------

    public void SetTile(int index, GearTile? tile)
    {
        if (index < 0 || index >= slotTiles.Count) return;

        slotTiles[index] = tile;
    }

    public void AddToInventory(GearTile tile)
    {
        inventoryTiles.Add(tile);
    }

    public void RemoveFromInventory(GearTile tile)
    {
        inventoryTiles.Remove(tile);
    }

    // ---------- Visuals ----------

    /// <summary> Rebuilds the arc wedges from scratch. Safe to call after changing the arc count or radii. </summary>
    public void BuildArcs()
    {
        foreach (GearArcVisual arc in arcVisuals)
        {
            if (arc != null) Destroy(arc.gameObject);
        }

        arcVisuals.Clear();

        float sweep = Mathf.Max(1f, ArcStepDegrees - arcGapDegrees);
        float inner = Mathf.Max(0f, radius - arcThickness * 0.5f);
        float outer = radius + arcThickness * 0.5f;
        int sortingLayerId = SortingLayer.NameToID(arcSortingLayer);
        Material material = ResolveArcMaterial();

        for (int i = 0; i < ArcCount; i++)
        {
            var go = new GameObject($"Arc_{i}");
            go.transform.SetParent(transform, false);

            var arc = go.AddComponent<GearArcVisual>();
            arc.SetMaterial(material);
            arc.SetSorting(sortingLayerId, arcSortingOrder);
            arc.Rebuild(inner, outer, GetArcLocalAngle(i), sweep, segmentsPerArc);

            arcVisuals.Add(arc);
        }

        highlightedArc = -1;
    }

    public void RefreshVisuals()
    {
        for (int i = 0; i < arcVisuals.Count; i++)
        {
            ApplyArcColor(i);
        }
    }

    private void ApplyArcColor(int index)
    {
        if (index < 0 || index >= arcVisuals.Count)
        {
            return;
        }

        bool highlighted = index == highlightedArc;
        Color color;

        if (TryGetArcElement(index, out Element element))
        {
            color = highlighted ? ElementVisuals.GetGlowColor(element) : ElementVisuals.GetColor(element);
            color.a = highlighted ? 1f : filledArcAlpha;
        }
        else
        {
            color = emptyArcColor;
            color.a = highlighted ? Mathf.Min(1f, emptyArcAlpha * 2f) : emptyArcAlpha;
        }

        arcVisuals[index].SetColor(color);
    }

    private Material ResolveArcMaterial()
    {
        if (arcMaterial != null)
        {
            return arcMaterial;
        }

        if (runtimeArcMaterial != null)
        {
            return runtimeArcMaterial;
        }

        // Both of these are Cull Off / vertex-colour-multiplying, so wedge winding order doesn't matter.
        Shader? shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("GearManager: no sprite shader found for the arc material; assign one in the inspector.");
            shader = Shader.Find("Unlit/Color");
        }

        runtimeArcMaterial = new Material(shader) { name = "GearArc (runtime)" };
        return runtimeArcMaterial;
    }

    // ---------- Follow ----------

    private void Update()
    {
        GameManager? gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.player == null)
        {
            return;
        }

        // Smoothed follow: the gear eases toward the player each frame, so any sudden move (dash, blink,
        // ...) leaves a brief trailing slide. Frame-rate independent.
        Vector3 target = gameManager.player.transform.position;
        float t = 1f - Mathf.Exp(-followLagDecay * Time.deltaTime);
        transform.position = new Vector3(
            Mathf.Lerp(transform.position.x, target.x, t),
            Mathf.Lerp(transform.position.y, target.y, t),
            target.z);
    }

    public override void InitializeOnGameStart(IReadOnlyPlayerBlob playerBlob)
    {
        // TODO: aisara => Setup gear based on playerBlob data and subscribe to updates
    }
}
