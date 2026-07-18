using UnityEngine;
using UnityEngine.Tilemaps;

// Shared breadth-first flow field over the room's wall tilemaps. Every enemy chases the same target,
// so one field serves them all instead of running A* per agent.
public class EnemyFlowField : MonoBehaviour
{
    public static EnemyFlowField Instance { get; private set; }

    private const float RebuildInterval = 0.1f;
    private const int Unreached = int.MaxValue;

    private static readonly Vector2Int[] Neighbours =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    private Tilemap wallTilemap;
    private Tilemap lowWallTilemap;
    private Tilemap voidTilemap;
    private BoundsInt bounds;
    private bool[] walkable;
    private int[] distance;
    private Vector2[] flow;
    private int[] queue;

    private float nextRebuildTime;
    private Vector3Int lastGoalCell;
    private bool hasField;

    public bool HasField => hasField;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Build(GameObject room)
    {
        hasField = false;
        wallTilemap = null;
        lowWallTilemap = null;
        voidTilemap = null;

        if (room == null)
        {
            return;
        }

        foreach (Tilemap tilemap in room.GetComponentsInChildren<Tilemap>(true))
        {
            if (tilemap.gameObject.name == "WallTilemap")
            {
                wallTilemap = tilemap;
            }
            else if (tilemap.gameObject.name == "LowWallTilemap")
            {
                lowWallTilemap = tilemap;
            }
            else if (tilemap.gameObject.name == "VoidTilemap")
            {
                voidTilemap = tilemap;
            }
        }

        if (wallTilemap == null)
        {
            Debug.LogWarning("EnemyFlowField: room has no WallTilemap; enemies will fall back to direct chase.");
            return;
        }

        bounds = wallTilemap.cellBounds;
        if (lowWallTilemap != null)
        {
            BoundsInt low = lowWallTilemap.cellBounds;
            Vector3Int min = Vector3Int.Min(bounds.min, low.min);
            Vector3Int max = Vector3Int.Max(bounds.max, low.max);
            bounds = new BoundsInt(min.x, min.y, 0, max.x - min.x, max.y - min.y, 1);
        }

        int count = bounds.size.x * bounds.size.y;
        walkable = new bool[count];
        distance = new int[count];
        flow = new Vector2[count];
        queue = new int[count];

        for (int y = 0; y < bounds.size.y; y++)
        {
            for (int x = 0; x < bounds.size.x; x++)
            {
                Vector3Int cell = new Vector3Int(bounds.min.x + x, bounds.min.y + y, 0);
                bool blocked = wallTilemap.HasTile(cell)
                    || (lowWallTilemap != null && lowWallTilemap.HasTile(cell))
                    || (voidTilemap != null && voidTilemap.HasTile(cell));
                walkable[y * bounds.size.x + x] = !blocked;
            }
        }

        hasField = true;
        nextRebuildTime = 0f;
        lastGoalCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private void Update()
    {
        if (!hasField)
        {
            return;
        }

        Transform target = ResolveTarget();
        if (target == null || Time.time < nextRebuildTime)
        {
            return;
        }

        Vector3Int goal = wallTilemap.WorldToCell(target.position);
        if (goal == lastGoalCell)
        {
            return;
        }

        nextRebuildTime = Time.time + RebuildInterval;
        lastGoalCell = goal;
        Recompute(goal);
    }

    private static Transform ResolveTarget()
    {
        GameObject player = GameManager.Instance != null ? GameManager.Instance.player : null;
        return player != null ? player.transform : null;
    }

    private void Recompute(Vector3Int goalCell)
    {
        for (int i = 0; i < distance.Length; i++)
        {
            distance[i] = Unreached;
            flow[i] = Vector2.zero;
        }

        int goalIndex = CellToIndex(goalCell);
        if (goalIndex < 0)
        {
            goalIndex = NearestWalkableIndex(goalCell);
            if (goalIndex < 0)
            {
                return;
            }
        }
        else if (!walkable[goalIndex])
        {
            goalIndex = NearestWalkableIndex(goalCell);
            if (goalIndex < 0)
            {
                return;
            }
        }

        int head = 0;
        int tail = 0;
        distance[goalIndex] = 0;
        queue[tail++] = goalIndex;

        while (head < tail)
        {
            int index = queue[head++];
            int cx = index % bounds.size.x;
            int cy = index / bounds.size.x;
            int nextDistance = distance[index] + 1;

            for (int n = 0; n < Neighbours.Length; n++)
            {
                int nx = cx + Neighbours[n].x;
                int ny = cy + Neighbours[n].y;
                if (nx < 0 || ny < 0 || nx >= bounds.size.x || ny >= bounds.size.y)
                {
                    continue;
                }

                int neighbour = ny * bounds.size.x + nx;
                if (!walkable[neighbour] || distance[neighbour] <= nextDistance)
                {
                    continue;
                }

                // Diagonals may not cut a corner, or bodies clip the wall they are squeezing past.
                if (Neighbours[n].x != 0 && Neighbours[n].y != 0
                    && (!walkable[cy * bounds.size.x + nx] || !walkable[ny * bounds.size.x + cx]))
                {
                    continue;
                }

                distance[neighbour] = nextDistance;
                queue[tail++] = neighbour;
            }
        }

        BuildFlowVectors();
    }

    private void BuildFlowVectors()
    {
        for (int y = 0; y < bounds.size.y; y++)
        {
            for (int x = 0; x < bounds.size.x; x++)
            {
                int index = y * bounds.size.x + x;
                if (!walkable[index] || distance[index] == Unreached)
                {
                    continue;
                }

                int best = distance[index];
                Vector2 bestDir = Vector2.zero;
                for (int n = 0; n < Neighbours.Length; n++)
                {
                    int nx = x + Neighbours[n].x;
                    int ny = y + Neighbours[n].y;
                    if (nx < 0 || ny < 0 || nx >= bounds.size.x || ny >= bounds.size.y)
                    {
                        continue;
                    }

                    int neighbour = ny * bounds.size.x + nx;
                    if (!walkable[neighbour] || distance[neighbour] >= best)
                    {
                        continue;
                    }

                    // Same corner rule as the BFS, or the flow vector points diagonally through a wall.
                    if (Neighbours[n].x != 0 && Neighbours[n].y != 0
                        && (!walkable[y * bounds.size.x + nx] || !walkable[ny * bounds.size.x + x]))
                    {
                        continue;
                    }

                    best = distance[neighbour];
                    bestDir = new Vector2(Neighbours[n].x, Neighbours[n].y);
                }

                flow[index] = bestDir.sqrMagnitude > 0f ? bestDir.normalized : Vector2.zero;
            }
        }
    }

    public bool TryGetDirection(Vector2 worldPosition, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!hasField)
        {
            return false;
        }

        int index = CellToIndex(wallTilemap.WorldToCell(worldPosition));
        if (index < 0 || distance[index] == Unreached || flow[index] == Vector2.zero)
        {
            return false;
        }

        direction = flow[index];
        return true;
    }

    // Cells are 2 units but bodies are ~1.5 wide, so following raw flow scrapes walls whenever an agent
    // drifts off the centre line. Bias toward the next cell's centre to keep a body-width corridor.
    public bool TryGetSteeredDirection(Vector2 worldPosition, float centeringBias, out Vector2 direction)
    {
        if (!TryGetDirection(worldPosition, out Vector2 flowDirection))
        {
            direction = Vector2.zero;
            return false;
        }

        Vector2 cellCentre = GetCellCenterWorld(worldPosition + flowDirection * 0.01f);
        direction = (flowDirection + (cellCentre - worldPosition) * centeringBias).normalized;
        return true;
    }

    public Vector3 GetCellCenterWorld(Vector2 worldPosition)
    {
        return wallTilemap.GetCellCenterWorld(wallTilemap.WorldToCell(worldPosition));
    }

    private int CellToIndex(Vector3Int cell)
    {
        int x = cell.x - bounds.min.x;
        int y = cell.y - bounds.min.y;
        if (x < 0 || y < 0 || x >= bounds.size.x || y >= bounds.size.y)
        {
            return -1;
        }

        return y * bounds.size.x + x;
    }

    private int NearestWalkableIndex(Vector3Int cell)
    {
        int originX = Mathf.Clamp(cell.x - bounds.min.x, 0, bounds.size.x - 1);
        int originY = Mathf.Clamp(cell.y - bounds.min.y, 0, bounds.size.y - 1);
        int maxRadius = Mathf.Max(bounds.size.x, bounds.size.y);

        for (int radius = 0; radius < maxRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                    {
                        continue;
                    }

                    int x = originX + dx;
                    int y = originY + dy;
                    if (x < 0 || y < 0 || x >= bounds.size.x || y >= bounds.size.y)
                    {
                        continue;
                    }

                    int index = y * bounds.size.x + x;
                    if (walkable[index])
                    {
                        return index;
                    }
                }
            }
        }

        return -1;
    }
}
