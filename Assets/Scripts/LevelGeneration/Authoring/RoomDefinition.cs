#nullable enable

using UnityEngine;

/// <summary>
/// What a single painted grid cell becomes when baked. The palette colours in the Room Painter
/// window map 1:1 to these values.
/// </summary>
public enum RoomCellType
{
    /// <summary>Walkable floor. Bakes to nothing — the scene background shows through.</summary>
    Empty = 0,
    /// <summary>Solid wall. Bakes to a wall tile on the WallTilemap (with collider).</summary>
    Wall,
    /// <summary>Destructible obstacle. Bakes to the configured crate prefab (if any).</summary>
    Crate,
    /// <summary>Player start. Bakes to a single <see cref="PlayerSpawnMarker"/>.</summary>
    PlayerSpawn,
    /// <summary>An enemy spawn location. Bakes to an <see cref="EnemySpawnPoint"/>.</summary>
    EnemySpawn,
    /// <summary>Exit portal location. Bakes to an <see cref="ExitSpawnPoint"/>.</summary>
    Exit,
    /// <summary>Solid to the player but not the thrown sword. Bakes to a wall tile on the LowWallTilemap
    /// (with collider), on the "LowWall" physics layer — excluded from SwordProjectile.terrainLayers so
    /// the sword flies straight through while the player is still blocked.
    /// Appended last so existing painted RoomDefinition assets keep their int values.</summary>
    LowWall,
    /// <summary>Solid box prop. Behaves exactly like <see cref="Wall"/> (WallTilemap, Arena layer, casts
    /// shadows) but painted with its own prop tile. Appended for int-serialization stability.</summary>
    PropBox1,
    /// <summary>Sword-permeable box prop. Behaves exactly like <see cref="LowWall"/> (LowWallTilemap,
    /// LowWall layer, no shadows) but painted with its own prop tile. Appended for stability.</summary>
    PropBox2,
}

/// <summary>
/// Authored, grid-based description of an arena. Painted in the Room Painter window and baked into
/// a runtime arena prefab + <see cref="ArenaLayoutTemplate"/> by RoomBaker. This asset is the source
/// of truth; the baked prefab is a regenerable build output, so re-baking after an edit is safe.
/// </summary>
[CreateAssetMenu(fileName = "RoomDefinition", menuName = "Scriptable Objects/Room Definition")]
public class RoomDefinition : ScriptableObject
{
    [SerializeField, Min(1)] private int width = 16;
    [SerializeField, Min(1)] private int height = 16;

    // Row-major, index = y * width + x. y grows upward (matches world +Y and the painter display).
    [SerializeField, HideInInspector] private RoomCellType[] cells = System.Array.Empty<RoomCellType>();

    public int Width => width;
    public int Height => height;

    public bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    public RoomCellType GetCell(int x, int y)
    {
        EnsureSize();
        return InBounds(x, y) ? cells[y * width + x] : RoomCellType.Empty;
    }

    public void SetCell(int x, int y, RoomCellType type)
    {
        EnsureSize();
        if (InBounds(x, y))
        {
            cells[y * width + x] = type;
        }
    }

    /// <summary>Resizes the grid, preserving overlapping content. New cells are <see cref="RoomCellType.Empty"/>.</summary>
    public void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);
        var next = new RoomCellType[newWidth * newHeight];
        if (cells != null && cells.Length == width * height)
        {
            int copyW = Mathf.Min(width, newWidth);
            int copyH = Mathf.Min(height, newHeight);
            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    next[y * newWidth + x] = cells[y * width + x];
                }
            }
        }

        width = newWidth;
        height = newHeight;
        cells = next;
    }

    /// <summary>Sets every cell back to <see cref="RoomCellType.Empty"/>.</summary>
    public void Clear()
    {
        EnsureSize();
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = RoomCellType.Empty;
        }
    }

    /// <summary>Paints the outer ring of cells as walls (a quick way to bound a room).</summary>
    public void FillBorderWalls()
    {
        for (int x = 0; x < width; x++)
        {
            SetCell(x, 0, RoomCellType.Wall);
            SetCell(x, height - 1, RoomCellType.Wall);
        }

        for (int y = 0; y < height; y++)
        {
            SetCell(0, y, RoomCellType.Wall);
            SetCell(width - 1, y, RoomCellType.Wall);
        }
    }

    /// <summary>Guarantees the backing array matches <see cref="Width"/>×<see cref="Height"/>.</summary>
    public void EnsureSize()
    {
        int needed = Mathf.Max(1, width) * Mathf.Max(1, height);
        if (cells == null || cells.Length != needed)
        {
            cells = new RoomCellType[needed];
        }
    }
}
