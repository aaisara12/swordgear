#nullable enable

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// The arc art preview (Assets/Scenes/Tools/GearArcPreview.unity): every element's arc, one column each, idle,
/// aimed at and active from top to bottom — live in the editor, no Play needed. Edit GearArcArt.asset, an arc's
/// material or an ArcBits prefab and the preview follows straight away.
/// </summary>
/// <remarks>
/// The preview arcs are built here, the way GearManager builds the real ones, and never saved: they're
/// throwaway copies of what the art asset describes, so the scene only holds this component and a camera.
/// In edit mode it also steps the arcs' particles and repaints, since Unity only animates particles and
/// shaders in the editor on request.
/// </remarks>
[ExecuteAlways]
public class GearArcPreview : MonoBehaviour
{
    private enum ArcState
    {
        Idle,
        Aimed,
        Active,
    }

    [SerializeField] private GearArcArt? art;
    [Tooltip("One column per element, left to right.")]
    [SerializeField] private List<Element> elements = new()
    {
        Element.Fire, Element.Ice, Element.Lightning, Element.Wind, Element.Earth, Element.Dark, Element.Light,
    };

    [Header("Arc shape (match the gear)")]
    [SerializeField] private float radius = 11f;
    [SerializeField] private float thickness = 3f;
    [Tooltip("Degrees one arc spans: 360 / the number of arcs, less the gap (seven arcs with a 4° gap: 47.4).")]
    [SerializeField] private float sweepDegrees = 47.4f;
    [SerializeField, Min(2)] private int segments = 12;
    [SerializeField] private float overflowInner = 0.5f;
    [SerializeField] private float overflowOuter = 3.5f;
    [SerializeField] private float overflowAlong = 0.6f;
    [Tooltip("An idle arc's opacity (GearManager's filled-arc alpha).")]
    [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.7f;

    [Header("Layout")]
    [Tooltip("Between elements, world units.")]
    [SerializeField] private float columnSpacing = 13f;
    [Tooltip("Between the idle, aimed and active rows, world units.")]
    [SerializeField] private float rowSpacing = 8.5f;

    private readonly List<GameObject> spawned = new();
    private readonly List<ParticleSystem> particles = new();
    private bool rebuildQueued;
#if UNITY_EDITOR
    private double lastTick;
#endif

    private void OnEnable()
    {
        Rebuild();
#if UNITY_EDITOR
        lastTick = EditorApplication.timeSinceStartup;
        EditorApplication.update += EditorTick;
        GearArcArt.LooksEdited += HandleLooksEdited;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
        GearArcArt.LooksEdited -= HandleLooksEdited;
#endif
        Clear();
    }

    private void OnValidate() => QueueRebuild();

    private void HandleLooksEdited(GearArcArt edited)
    {
        if (edited == art)
        {
            QueueRebuild();
        }
    }

    private void QueueRebuild()
    {
        // Objects can't be made or destroyed inside OnValidate (ours or the art asset's); rebuild just after it.
        if (rebuildQueued)
        {
            return;
        }

        rebuildQueued = true;
#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            rebuildQueued = false;
            if (this != null && isActiveAndEnabled)
            {
                Rebuild();
            }
        };
#endif
    }

    private void Rebuild()
    {
        Clear();

        if (art != null)
        {
            art.ApplyShared();
        }

        // The preview shows the arcs at full opacity, without the gear's background fade: in play GearManager fades
        // the whole gear (GearArcArt's Background settings), so judge the look here and the fade in play.
        Shader.SetGlobalFloat("_GearFadeOut", 0f);
        Shader.SetGlobalFloat("_GearDesaturate", 0f);

        float inner = Mathf.Max(0f, radius - thickness * 0.5f);
        float outer = radius + thickness * 0.5f;

        for (int column = 0; column < elements.Count; column++)
        {
            Element element = elements[column];
            ElementArcLook look = default;
            bool hasLook = art != null && art.TryGetLook(element, out look);
            Material? material = hasLook && look.material != null ? look.material : art != null ? art.DefaultMaterial : null;
            if (material == null)
            {
                continue;
            }

            for (int row = 0; row < 3; row++)
            {
                var state = (ArcState)row;
                var go = new GameObject($"{element} {state}") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);

                // Each arc is built around its own gear centre, sweeping round the top; place that centre so the
                // arc lands on its grid cell.
                float x = (column - (elements.Count - 1) * 0.5f) * columnSpacing;
                go.transform.localPosition = new Vector3(x, -row * rowSpacing - radius, 0f);

                var arc = go.AddComponent<GearArcVisual>();
                arc.SetMaterial(material);
                arc.Rebuild(inner, outer, 90f, sweepDegrees, segments, overflowInner, overflowOuter, overflowAlong);

                if (state == ArcState.Active && hasLook && look.bits != null)
                {
                    ArcBits bits = SpawnBits(look.bits, go.transform);
                    ParticleSystem[] systems = bits.GetComponentsInChildren<ParticleSystem>();
                    particles.AddRange(systems);
                    arc.SetBits(bits);
                    KeepFit(systems);
                }

                Color colour = ElementVisuals.GetColor(element);
                colour.a = state == ArcState.Idle ? idleAlpha : 1f;
                arc.SetTarget(colour, state == ArcState.Aimed ? 1f : 0f, state == ArcState.Active ? 1f : 0f);
                arc.SnapToTarget();
                spawned.Add(go);
            }
        }
    }

    private static ArcBits SpawnBits(ArcBits prefab, Transform parent)
    {
#if UNITY_EDITOR
        // A prefab instance in the editor, so edits to the prefab show here as you make them.
        if (!Application.isPlaying)
        {
            var instance = (ArcBits)PrefabUtility.InstantiatePrefab(prefab, parent);
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.DontSave;
            }

            return instance;
        }
#endif
        return Instantiate(prefab, parent);
    }

    /// <summary>
    /// Records the shapes ArcBits just fitted to the arc as overrides on the prefab instance; saving the prefab
    /// rebuilds its instances from the asset plus their overrides, which would otherwise drop the fit.
    /// </summary>
    private static void KeepFit(ParticleSystem[] systems)
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }

        foreach (ParticleSystem system in systems)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(system);
        }
#endif
    }

    private void Clear()
    {
        foreach (GameObject go in spawned)
        {
            if (go == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                // Edit mode never calls GearArcVisual.OnDestroy (it isn't [ExecuteAlways]), so free its mesh here.
                if (go.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    DestroyImmediate(filter.sharedMesh);
                }

                DestroyImmediate(go);
            }
        }

        spawned.Clear();
        particles.Clear();
    }

#if UNITY_EDITOR
    /// <summary> Edit mode: step the pieces' particles and repaint, so the preview moves as it will in play. </summary>
    private void EditorTick()
    {
        if (Application.isPlaying)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float dt = Mathf.Min((float)(now - lastTick), 0.1f);
        lastTick = now;

        foreach (ParticleSystem system in particles)
        {
            if (system != null)
            {
                system.Simulate(dt, false, false, false);
            }
        }

        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }
#endif
}
