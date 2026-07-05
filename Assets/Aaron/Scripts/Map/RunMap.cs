#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DEPRECATED — branching map state replaced by <see cref="LinearRunState"/>.
/// The branching graph of nodes for a single run, plus the player's current position within it.
/// </summary>
[Obsolete("DEPRECATED: Branching map replaced by LinearRunState. Retained for reference.")]
public class RunMap
{
    public IReadOnlyList<MapNode> Nodes { get; }

    // -1 means the player hasn't entered any node yet (start of the run).
    public int CurrentNodeId { get; private set; } = -1;

    private readonly Dictionary<int, MapNode> _byId;
    private readonly int _firstColumn;

    public RunMap(IEnumerable<MapNode> nodes)
    {
        Nodes = nodes.ToList();
        _byId = Nodes.ToDictionary(n => n.Id);
        _firstColumn = Nodes.Count > 0 ? Nodes.Min(n => n.Column) : 0;
    }

    public MapNode? GetNode(int id)
    {
        return _byId.TryGetValue(id, out MapNode node) ? node : null;
    }

    public MapNode? CurrentNode => CurrentNodeId >= 0 ? GetNode(CurrentNodeId) : null;

    /// <summary>
    /// Nodes the player may currently move to: the first column at run start, otherwise the
    /// (incomplete) successors of the current node.
    /// </summary>
    public IEnumerable<MapNode> GetSelectableNodes()
    {
        if (CurrentNodeId < 0)
        {
            return Nodes.Where(n => n.Column == _firstColumn);
        }

        MapNode? current = CurrentNode;
        if (current == null)
        {
            return Enumerable.Empty<MapNode>();
        }

        return current.Next
            .Select(GetNode)
            .Where(n => n != null)
            .Cast<MapNode>();
    }

    public bool IsSelectable(int id)
    {
        return GetSelectableNodes().Any(n => n.Id == id);
    }

    public void SetCurrentNode(int id)
    {
        CurrentNodeId = id;
    }

    public void MarkCurrentNodeCompleted()
    {
        MapNode? current = CurrentNode;
        if (current != null)
        {
            current.Completed = true;
        }
    }

    public bool IsBossDefeated => Nodes.Any(n => n.IsBoss && n.Completed);
}
