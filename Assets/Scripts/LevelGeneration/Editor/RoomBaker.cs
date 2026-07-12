#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Converts an authored <see cref="RoomDefinition"/> into a runtime arena prefab and its
/// <see cref="ArenaLayoutTemplate"/>. Reproduces the same geometry contract the hand-built arenas use
/// (Grid → WallTilemap painted with a wall tile + TilemapCollider2D, plus the three marker types), so
/// baked rooms drop into the existing RunManager/LevelLoader pipeline unchanged.
/// </summary>
public static class RoomBaker
{
    public const string DefaultWallTilePath = "Assets/Visuals/walltile1.asset";
    public const string DefaultPrefabFolder = "Assets/Visuals/Prefabs/BakedRooms";
    public const string DefaultTemplateFolder = "Assets/Scripts/LevelGeneration/ArenaLayouts";

    public struct BakeConfig
    {
        public TileBase? WallTile;
        public GameObject? CratePrefab;
        public float CellSize;
        public string PrefabFolder;
        public string TemplateFolder;

        public static BakeConfig Default() => new BakeConfig
        {
            WallTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultWallTilePath),
            CratePrefab = null,
            CellSize = 1f,
            PrefabFolder = DefaultPrefabFolder,
            TemplateFolder = DefaultTemplateFolder,
        };
    }

    /// <summary>
    /// Lint the room for playability. Returns a list of blocking problems (empty = good to bake):
    /// exactly one player spawn, at least one enemy spawn and exit, and every enemy-spawn/exit cell
    /// reachable from the player spawn without passing through walls or crates.
    /// </summary>
    public static List<string> Validate(RoomDefinition room)
    {
        var errors = new List<string>();
        if (room == null)
        {
            errors.Add("Room is null.");
            return errors;
        }

        room.EnsureSize();
        int players = 0, enemies = 0, exits = 0;
        for (int y = 0; y < room.Height; y++)
        {
            for (int x = 0; x < room.Width; x++)
            {
                switch (room.GetCell(x, y))
                {
                    case RoomCellType.PlayerSpawn: players++; break;
                    case RoomCellType.EnemySpawn: enemies++; break;
                    case RoomCellType.Exit: exits++; break;
                }
            }
        }

        if (players != 1)
        {
            errors.Add($"Need exactly 1 player spawn (found {players}).");
        }

        if (enemies < 1)
        {
            errors.Add("Need at least 1 enemy spawn (found 0).");
        }

        if (exits < 1)
        {
            errors.Add("Need at least 1 exit (found 0).");
        }

        if (players == 1)
        {
            bool[] reachable = FloodFillFromPlayer(room);
            int walledOff = 0;
            for (int y = 0; y < room.Height; y++)
            {
                for (int x = 0; x < room.Width; x++)
                {
                    RoomCellType c = room.GetCell(x, y);
                    if ((c == RoomCellType.EnemySpawn || c == RoomCellType.Exit) && !reachable[y * room.Width + x])
                    {
                        walledOff++;
                    }
                }
            }

            if (walledOff > 0)
            {
                errors.Add($"{walledOff} enemy-spawn/exit cell(s) are walled off from the player spawn.");
            }
        }

        return errors;
    }

    private static bool[] FloodFillFromPlayer(RoomDefinition room)
    {
        var visited = new bool[room.Width * room.Height];
        int sx = -1, sy = -1;
        for (int y = 0; y < room.Height && sy < 0; y++)
        {
            for (int x = 0; x < room.Width; x++)
            {
                if (room.GetCell(x, y) == RoomCellType.PlayerSpawn)
                {
                    sx = x;
                    sy = y;
                    break;
                }
            }
        }

        if (sx < 0)
        {
            return visited;
        }

        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(sx, sy));
        visited[sy * room.Width + sx] = true;
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        while (stack.Count > 0)
        {
            Vector2Int p = stack.Pop();
            foreach (Vector2Int d in dirs)
            {
                int nx = p.x + d.x, ny = p.y + d.y;
                if (!room.InBounds(nx, ny))
                {
                    continue;
                }

                int idx = ny * room.Width + nx;
                if (visited[idx])
                {
                    continue;
                }

                RoomCellType c = room.GetCell(nx, ny);
                if (c == RoomCellType.Wall || c == RoomCellType.Crate)
                {
                    continue; // solid — blocks reachability
                }

                visited[idx] = true;
                stack.Push(new Vector2Int(nx, ny));
            }
        }

        return visited;
    }

    /// <summary>
    /// Bakes <paramref name="room"/> into "<c>PrefabFolder/roomName.prefab</c>" and an
    /// ArenaLayoutTemplate at "<c>TemplateFolder/roomName.asset</c>". Returns the template, or null if
    /// validation failed (nothing is written in that case).
    /// </summary>
    public static ArenaLayoutTemplate? Bake(RoomDefinition room, BakeConfig config, string roomName)
    {
        List<string> errors = Validate(room);
        if (errors.Count > 0)
        {
            Debug.LogError($"RoomBaker: cannot bake '{roomName}':\n - {string.Join("\n - ", errors)}");
            return null;
        }

        if (config.WallTile == null)
        {
            Debug.LogError($"RoomBaker: no wall tile assigned (expected something like {DefaultWallTilePath}).");
            return null;
        }

        EnsureFolder(config.PrefabFolder);
        EnsureFolder(config.TemplateFolder);

        float cs = config.CellSize <= 0f ? 1f : config.CellSize;
        int w = room.Width, h = room.Height;
        // Centre the room on the origin, like the authored arenas.
        Vector3 gridOffset = new Vector3(-w * cs * 0.5f, -h * cs * 0.5f, 0f);

        var rootGO = new GameObject(roomName);
        try
        {
            var gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(rootGO.transform);
            gridGO.transform.localPosition = gridOffset;
            Grid grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(cs, cs, 0f);

            var tmGO = new GameObject("WallTilemap");
            tmGO.transform.SetParent(gridGO.transform);
            tmGO.transform.localPosition = Vector3.zero;
            Tilemap tilemap = tmGO.AddComponent<Tilemap>();
            tmGO.AddComponent<TilemapRenderer>();
            tmGO.AddComponent<TilemapCollider2D>();

            int enemyIndex = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    RoomCellType c = room.GetCell(x, y);
                    // Cell-centre in root-local space (markers/crates sit here).
                    Vector3 center = gridOffset + new Vector3((x + 0.5f) * cs, (y + 0.5f) * cs, 0f);
                    switch (c)
                    {
                        case RoomCellType.Wall:
                            tilemap.SetTile(new Vector3Int(x, y, 0), config.WallTile);
                            break;
                        case RoomCellType.PlayerSpawn:
                            MakeMarker<PlayerSpawnMarker>(rootGO, "PlayerSpawnMarker", center);
                            break;
                        case RoomCellType.Exit:
                            MakeMarker<ExitSpawnPoint>(rootGO, "ExitSpawnPoint", center);
                            break;
                        case RoomCellType.EnemySpawn:
                            MakeMarker<EnemySpawnPoint>(rootGO, $"Spawn ({enemyIndex++})", center);
                            break;
                        case RoomCellType.Crate:
                            PlaceCrate(rootGO, config.CratePrefab, center);
                            break;
                    }
                }
            }

            string prefabPath = $"{config.PrefabFolder}/{roomName}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath, out bool ok);
            if (!ok || savedPrefab == null)
            {
                Debug.LogError($"RoomBaker: failed to save prefab at {prefabPath}.");
                return null;
            }

            ArenaLayoutTemplate template = UpsertTemplate(config.TemplateFolder, roomName, savedPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"RoomBaker: baked '{roomName}' → {prefabPath} + template.");
            return template;
        }
        finally
        {
            Object.DestroyImmediate(rootGO);
        }
    }

    private static void MakeMarker<T>(GameObject root, string name, Vector3 localPos) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform);
        go.transform.localPosition = localPos;
        go.AddComponent<T>();
    }

    private static void PlaceCrate(GameObject root, GameObject? cratePrefab, Vector3 localPos)
    {
        if (cratePrefab == null)
        {
            Debug.LogWarning("RoomBaker: room has crate cells but no crate prefab is assigned; skipping.");
            return;
        }

        var crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, root.transform);
        crate.transform.localPosition = localPos;
    }

    private static ArenaLayoutTemplate UpsertTemplate(string folder, string roomName, GameObject prefab)
    {
        string path = $"{folder}/{roomName}.asset";
        ArenaLayoutTemplate template = AssetDatabase.LoadAssetAtPath<ArenaLayoutTemplate>(path);
        if (template == null)
        {
            template = ScriptableObject.CreateInstance<ArenaLayoutTemplate>();
            template.LevelPrefab = prefab;
            AssetDatabase.CreateAsset(template, path);
        }
        else
        {
            template.LevelPrefab = prefab;
            EditorUtility.SetDirty(template);
        }

        return template;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string[] parts = folder.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
