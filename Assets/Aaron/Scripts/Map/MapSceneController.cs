#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the run map (node buttons + connecting edges) from <see cref="RunManager.CurrentMap"/> and
/// forwards node selection to the RunManager. Only currently-reachable nodes are interactable.
/// Hides the combat HUD and deactivates the player pawn while the map is shown.
/// </summary>
public class MapSceneController : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private RectTransform? nodeContainer;
    [SerializeField] private RectTransform? edgeContainer;

    [Header("Prefabs")]
    [SerializeField] private MapNodeButton? nodeButtonPrefab;
    [SerializeField] private Image? edgePrefab;

    [Header("Layout")]
    [SerializeField] private float columnSpacing = 220f;
    [SerializeField] private float rowSpacing = 140f;
    [SerializeField] private float edgeThickness = 6f;

    [Header("Other Scenes")]
    [SerializeField] private BoolEventChannelSO? combatHudVisibilityChannel;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void OnEnable()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnMapChanged += Rebuild;
            // Lazily generate a run when entering the Map directly (e.g. from the Title "Play" button,
            // or re-entering after a defeat cleared the previous run).
            RunManager.Instance.EnsureRunStarted();
        }

        // Map is a non-combat context: hide the HUD and put the pawn away.
        combatHudVisibilityChannel?.RaiseDataChanged(false);
        PlayerGameplayManager.Instance?.DespawnPawn();
        Time.timeScale = 1f;

        Rebuild();
    }

    private void OnDisable()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnMapChanged -= Rebuild;
        }
    }

    private void Rebuild()
    {
        ClearSpawned();

        RunMap? map = RunManager.Instance?.CurrentMap;
        if (map == null || nodeContainer == null || nodeButtonPrefab == null)
        {
            return;
        }

        Dictionary<int, Vector2> positions = ComputePositions(map);
        var selectableIds = new HashSet<int>(map.GetSelectableNodes().Select(n => n.Id));

        DrawEdges(map, positions);

        foreach (MapNode node in map.Nodes)
        {
            MapNodeButton button = Instantiate(nodeButtonPrefab, nodeContainer);
            button.RectTransform.anchoredPosition = positions[node.Id];
            bool isCurrent = map.CurrentNodeId == node.Id;
            button.Setup(node, selectableIds.Contains(node.Id), isCurrent, OnNodeClicked);
            _spawned.Add(button.gameObject);
        }
    }

    private Dictionary<int, Vector2> ComputePositions(RunMap map)
    {
        var positions = new Dictionary<int, Vector2>();

        int minColumn = map.Nodes.Min(n => n.Column);
        int maxColumn = map.Nodes.Max(n => n.Column);
        float columnCenter = (minColumn + maxColumn) / 2f;

        foreach (var columnGroup in map.Nodes.GroupBy(n => n.Column))
        {
            var nodesInColumn = columnGroup.OrderBy(n => n.Row).ToList();
            int count = nodesInColumn.Count;
            for (int i = 0; i < count; i++)
            {
                MapNode node = nodesInColumn[i];
                float x = (node.Column - columnCenter) * columnSpacing;
                float y = (i - (count - 1) / 2f) * rowSpacing;
                positions[node.Id] = new Vector2(x, y);
            }
        }

        return positions;
    }

    private void DrawEdges(RunMap map, Dictionary<int, Vector2> positions)
    {
        if (edgeContainer == null || edgePrefab == null)
        {
            return;
        }

        foreach (MapNode node in map.Nodes)
        {
            if (!positions.TryGetValue(node.Id, out Vector2 from))
            {
                continue;
            }

            foreach (int nextId in node.Next)
            {
                if (!positions.TryGetValue(nextId, out Vector2 to))
                {
                    continue;
                }

                Image edge = Instantiate(edgePrefab, edgeContainer);
                RectTransform rt = edge.rectTransform;
                Vector2 delta = to - from;
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = from;
                rt.sizeDelta = new Vector2(delta.magnitude, edgeThickness);
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                _spawned.Add(edge.gameObject);
            }
        }
    }

    private void OnNodeClicked(int nodeId)
    {
        RunManager.Instance?.SelectNode(nodeId);
    }

    private void ClearSpawned()
    {
        foreach (GameObject go in _spawned)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        _spawned.Clear();
    }
}
