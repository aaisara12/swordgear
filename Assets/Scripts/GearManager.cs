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
    [Tooltip("What every element's arc looks like: its material and the pieces that fly off it while active. " +
             "Leave empty to fall back to a plain arc built at runtime.")]
    [SerializeField] private GearArcArt? art;
    [SerializeField] private string arcSortingLayer = "Default";
    [SerializeField] private int arcSortingOrder = 4;
    [SerializeField, Range(0f, 1f)] private float filledArcAlpha = 0.7f;
    [SerializeField, Range(0f, 1f)] private float emptyArcAlpha = 0.2f;
    [SerializeField] private Color emptyArcColor = new(0.5f, 0.5f, 0.5f, 1f);
    [Tooltip("How quickly arcs ease between idle, aimed-at and active, per second.")]
    [SerializeField] private float arcEaseRate = 14f;
    [Tooltip("How far (world units) an aimed-at or active arc's element may spill past its band: inward, " +
             "outward, and past each end. Idle arcs don't spill, so this costs nothing until an arc is in play.")]
    [SerializeField] private float arcOverflowInner = 0.5f;
    [SerializeField] private float arcOverflowOuter = 3.5f;
    [SerializeField] private float arcOverflowAlong = 0.6f;

    [Header("Hub")]
    [Tooltip("The cog behind the arcs (Swordgear/Gear Hub on a quad centred on the gear). Its teeth sit in the " +
             "gaps between arcs, so it's resized and re-toothed whenever the loadout changes; it takes the " +
             "imbued element's colour and clicks round a notch on every grant.")]
    [SerializeField] private Renderer? hubRenderer;
    [Tooltip("World units the hub's quad reaches past the arcs' outer edge, to fit its teeth.")]
    [SerializeField] private float hubReach = 2.5f;

    [Header("Imbue Grant")]
    [Tooltip("An imbue lasts until the next flick, or until a new arena clears it: it doesn't time out.")]
    [SerializeField] private float imbueDamageMultiplier = 1.2f;

    [Header("Follow Settings")]
    [Tooltip("How fast the gear eases toward the player (lower = more trailing slide / more 'alive', higher = tighter).")]
    [SerializeField] private float followLagDecay = 8f;

    public static GearManager? Instance;

    /// <summary>
    /// A flick granted an element from an arc: (element, arc index). Fires only for a real grant — not on boot,
    /// not when an imbue expires back to Physical — and again when the same element is re-flicked, which is
    /// what the switch effects want (ElementManager.OnActiveElementChanged covers neither case).
    /// </summary>
    public static event System.Action<Element, int>? OnElementGranted;

    private static readonly int HubShapeId = Shader.PropertyToID("_HubShape");
    private static readonly int HubTintId = Shader.PropertyToID("_Tint");
    private static readonly int HubNotchId = Shader.PropertyToID("_Notch");
    private static readonly int HubClickTimeId = Shader.PropertyToID("_ClickTime");
    private static readonly int HubScaleId = Shader.PropertyToID("_HubScale");
    private static readonly int GearFadeOutId = Shader.PropertyToID("_GearFadeOut");
    private static readonly int GearDesaturateId = Shader.PropertyToID("_GearDesaturate");

    private readonly List<GearArcVisual> arcVisuals = new();

    // runtime gear state — one entry per arc
    private List<GearTile?> slotTiles = new();

    // inventory
    private readonly List<GearTile> inventoryTiles = new();

    private Material? runtimeArcMaterial;
    private int highlightedArc = -1;
    private Element activeElement = Element.Physical;
    private MaterialPropertyBlock? hubBlock;
    private int hubNotch;
    private float hubClickTime = -100f;

    // Presence: 0 = sitting back in the background, 1 = lifted forward (picking, or just switched).
    private GearArcArt? fallbackArt;
    private Vector3 baseScale = Vector3.one;
    private float presence;
    private bool picking;
    private int grantFrame = -1;
    private float liftUntil = -1f;
    private float appliedFadeOut = -1f;
    private float appliedDesaturate = -1f;
    private float appliedScale = -1f;

    /// <summary> One arc per equipped element — derived from the loadout, never authored directly. </summary>
    public int ArcCount => slotTiles.Count;
    public IReadOnlyList<GearTile?> GetSlots() => slotTiles;
    public IReadOnlyList<GearTile> GetInventory() => inventoryTiles;

    private float ArcStepDegrees => 360f / Mathf.Max(1, ArcCount);

    private void Awake()
    {
        Instance = this;
        baseScale = transform.localScale;
        ApplySharedArcArt();
    }

    /// <summary>
    /// The arc shaders read the shared tile and state settings as globals. The art asset pushes them when it
    /// loads; this makes sure, and falls back to the defaults when no art is assigned.
    /// </summary>
    private void ApplySharedArcArt()
    {
        if (art != null)
        {
            art.ApplyShared();
            return;
        }

        fallbackArt = ScriptableObject.CreateInstance<GearArcArt>();
        fallbackArt.ApplyShared();
    }

    private void OnEnable()
    {
        ElementManager.OnActiveElementChanged += HandleActiveElementChanged;
        appliedFadeOut = -1f;
    }

    private void OnDisable()
    {
        ElementManager.OnActiveElementChanged -= HandleActiveElementChanged;

        // The fade is global; leave nothing faded behind for whatever draws arcs next (the arc preview, say).
        Shader.SetGlobalFloat(GearFadeOutId, 0f);
        Shader.SetGlobalFloat(GearDesaturateId, 0f);
    }

    /// <summary> The imbued element's arc lights up as active; Physical (no imbue) lights none. </summary>
    private void HandleActiveElementChanged(Element element)
    {
        activeElement = element;
        RefreshVisuals();
        UpdateHub();
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

        if (fallbackArt != null)
        {
            Destroy(fallbackArt);
            fallbackArt = null;
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

        // New arcs start in their state rather than fading in from white.
        foreach (GearArcVisual arc in arcVisuals)
        {
            arc.SnapToTarget();
        }
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

    /// <summary> World position of the middle of an arc's band: where switch effects leave it from. </summary>
    public bool TryGetArcCentre(int index, out Vector3 worldPosition)
    {
        worldPosition = transform.position;

        if (index < 0 || index >= ArcCount)
        {
            return false;
        }

        float radians = (GetArcLocalAngle(index) + transform.eulerAngles.z) * Mathf.Deg2Rad;
        // lossyScale: the gear shrinks a touch while it sits back (UpdatePresence).
        worldPosition += new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius * transform.lossyScale.x;
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

        GameManager.Instance.ApplyEmpowerment(granted, imbueDamageMultiplier);
        grantFrame = Time.frameCount;
        Lift(Look.SwitchHold);
        ClickHub();
        if (index < arcVisuals.Count)
        {
            arcVisuals[index].Flare();
        }

        OnElementGranted?.Invoke(granted, index);
        return true;
    }

    public bool TryGrantElementFromDirection(Vector2 worldDirection) =>
        TryGrantElementFromDirection(worldDirection, out _);

    /// <summary>
    /// Lights up the arc a flick in this direction would grab (none for a zero direction, e.g. the stick
    /// passing through its centre).
    /// </summary>
    /// <remarks>
    /// Called every frame the player aims at the gear (sword in hand), so it also marks them as picking: the
    /// gear stays lifted forward until <see cref="ClearArcHighlight"/>, which is how picking ends.
    /// </remarks>
    public void HighlightArcForDirection(Vector2 worldDirection)
    {
        picking = true;
        SetHighlightedArc(TryGetArcIndex(worldDirection, out int index) ? index : -1);
    }

    public void ClearArcHighlight()
    {
        // Picking ended. Without a switch the gear lingers for the pick hold; a switch (granted just before
        // this, in the same frame) has set its own.
        if (picking && grantFrame != Time.frameCount)
        {
            Lift(Look.PickHold);
        }

        picking = false;
        SetHighlightedArc(-1);
    }

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
        Material fallback = ResolveFallbackMaterial();

        for (int i = 0; i < ArcCount; i++)
        {
            var go = new GameObject($"Arc_{i}");
            go.transform.SetParent(transform, false);

            ElementArcLook look = default;
            bool hasLook = art != null && TryGetArcElement(i, out Element element) && art.TryGetLook(element, out look);

            var arc = go.AddComponent<GearArcVisual>();
            arc.EaseRate = arcEaseRate;
            arc.SetMaterial(hasLook && look.material != null ? look.material : fallback);
            arc.SetSorting(sortingLayerId, arcSortingOrder);
            arc.Rebuild(inner, outer, GetArcLocalAngle(i), sweep, segmentsPerArc,
                arcOverflowInner, arcOverflowOuter, arcOverflowAlong);

            // The pieces are an authored prefab; placing it under the arc is all that happens here.
            if (hasLook && look.bits != null)
            {
                arc.SetBits(Instantiate(look.bits, go.transform));
            }

            arcVisuals.Add(arc);
        }

        highlightedArc = -1;
        appliedFadeOut = -1f;   // the new pieces take the current fade on the next update
        UpdateHub();
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
        bool active = false;
        Color color;

        if (TryGetArcElement(index, out Element element))
        {
            active = element == activeElement && element != Element.Physical;

            // The brightening itself happens in the shader (into HDR, so it blooms); the vertex colour only
            // carries the element's hue and how solid the arc is.
            color = ElementVisuals.GetColor(element);
            color.a = highlighted || active ? 1f : filledArcAlpha;
        }
        else
        {
            color = emptyArcColor;
            color.a = highlighted ? Mathf.Min(1f, emptyArcAlpha * 2f) : emptyArcAlpha;
        }

        arcVisuals[index].SetTarget(color, highlighted ? 1f : 0f, active ? 1f : 0f);

        // The active arc spills furthest, so it draws over its neighbours; an aimed-at arc's outline next.
        int lift = active ? 2 : highlighted ? 1 : 0;
        arcVisuals[index].SetSorting(SortingLayer.NameToID(arcSortingLayer), arcSortingOrder + lift);
    }

    // ---------- Hub ----------

    /// <summary> Fits the hub to the current arcs and tints it with the imbued element. </summary>
    private void UpdateHub()
    {
        if (hubRenderer == null)
        {
            return;
        }

        float inner = Mathf.Max(0f, radius - arcThickness * 0.5f);
        float outer = radius + arcThickness * 0.5f;
        hubRenderer.transform.localScale = Vector3.one * 2f * (outer + hubReach);

        Color tint = ElementVisuals.GetColor(activeElement);
        tint.a = activeElement == Element.Physical ? 0f : 1f;

        hubBlock ??= new MaterialPropertyBlock();
        hubRenderer.GetPropertyBlock(hubBlock);
        hubBlock.SetVector(HubShapeId, new Vector4(inner, outer, Mathf.Max(1, ArcCount), arcOffsetDegrees * Mathf.Deg2Rad));
        hubBlock.SetColor(HubTintId, tint);
        hubBlock.SetFloat(HubNotchId, hubNotch);
        hubBlock.SetFloat(HubClickTimeId, hubClickTime);
        hubRenderer.SetPropertyBlock(hubBlock);
    }

    /// <summary>
    /// Clicks the hub round one notch. The spring and flash play out in the shader from the click's time,
    /// stamped on the shader's clock (URP sets _Time.y from Time.time), so nothing here animates.
    /// </summary>
    private void ClickHub()
    {
        hubNotch = (hubNotch + 1) % Mathf.Max(1, ArcCount);
        hubClickTime = Time.time;
        UpdateHub();
    }

    /// <summary> The arc for an element with no look of its own: the art's default, else one built here. </summary>
    private Material ResolveFallbackMaterial()
    {
        if (art != null && art.DefaultMaterial != null)
        {
            return art.DefaultMaterial;
        }

        if (runtimeArcMaterial != null)
        {
            return runtimeArcMaterial;
        }

        // All of these are Cull Off / vertex-colour-multiplying, so wedge winding order doesn't matter. Only
        // the first shows the aimed-at / active states; the others are a last resort.
        Shader? shader = Shader.Find("Swordgear/Gear Arc")
                         ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("GearManager: no sprite shader found for the arc material; assign one in the inspector.");
            shader = Shader.Find("Unlit/Color");
        }

        runtimeArcMaterial = new Material(shader) { name = "GearArc (runtime)" };
        return runtimeArcMaterial;
    }

    // ---------- Presence ----------

    private GearArcArt Look => art != null ? art : (fallbackArt ??= ScriptableObject.CreateInstance<GearArcArt>());

    /// <summary>
    /// Brings the gear forward for at least this many seconds (a switch; the tutorial's reveal). Picking holds
    /// it forward on top of this.
    /// </summary>
    public void Lift(float seconds)
    {
        liftUntil = Mathf.Max(liftUntil, Time.unscaledTime + seconds);
    }

    /// <summary>
    /// The gear sits back in the background — fainter, greyer, a touch smaller — so it doesn't pull the eye
    /// during play, and lifts forward while the player picks an element and for a moment after a switch. The
    /// amounts and timings are on the art asset (Background); the arcs, hub and pieces all fade through
    /// GearPresence.hlsl.
    /// </summary>
    private void UpdatePresence()
    {
        GearArcArt look = Look;
        float now = Time.unscaledTime;   // a hit-stop shouldn't freeze the gear mid-lift
        bool forward = picking || now < liftUntil;
        float seconds = forward ? look.LiftTime : look.SettleTime;
        presence = Mathf.MoveTowards(presence, forward ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(seconds, 0.001f));

        float shown = Mathf.SmoothStep(0f, 1f, presence);
        float back = 1f - shown;
        float scale = Mathf.Lerp(look.RecededScale, 1f, shown);
        float fadeOut = 1f - Mathf.Lerp(look.RecededOpacity, look.LiftedOpacity, shown);
        float desaturate = back * (1f - look.RecededColour);
        if (Mathf.Approximately(fadeOut, appliedFadeOut) && Mathf.Approximately(desaturate, appliedDesaturate)
            && Mathf.Approximately(scale, appliedScale))
        {
            return;
        }

        appliedFadeOut = fadeOut;
        appliedDesaturate = desaturate;
        appliedScale = scale;
        transform.localScale = baseScale * scale;

        // The hub draws its cog in world units from the gear's centre; tell it the gear's scale so it shrinks
        // with the arcs.
        if (hubRenderer != null)
        {
            hubBlock ??= new MaterialPropertyBlock();
            hubRenderer.GetPropertyBlock(hubBlock);
            hubBlock.SetFloat(HubScaleId, transform.lossyScale.x);
            hubRenderer.SetPropertyBlock(hubBlock);
        }

        Shader.SetGlobalFloat(GearFadeOutId, fadeOut);
        Shader.SetGlobalFloat(GearDesaturateId, desaturate);
        foreach (GearArcVisual arc in arcVisuals)
        {
            if (arc != null)
            {
                arc.SetPieceRecede(fadeOut, desaturate);
            }
        }
    }

    // ---------- Follow ----------

    private void Update()
    {
        UpdatePresence();

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
