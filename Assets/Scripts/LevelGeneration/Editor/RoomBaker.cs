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
    public const string DefaultWallSpritePath = "Assets/Visuals/swordgear_wallset1_tile1.png";
    public const string DefaultPrefabFolder = "Assets/Visuals/Prefabs/BakedRooms";
    public const string DefaultTemplateFolder = "Assets/Scripts/LevelGeneration/ArenaLayouts";

    /// <summary>World size of the black void quad baked behind each room.</summary>
    public const float BackdropSize = 400f;

    public const string DefaultFloorSpritePath = "Assets/Visuals/swordgear_updated_floortile.png";
    /// <summary>World units each floor tile renders at (matches the old scene floor's ~20x scale).</summary>
    public const float DefaultFloorScale = 20f;
    /// <summary>Uniform world scale applied to the baked room so tiles match the authored arenas (Grid scale 2).</summary>
    public const float DefaultWorldScale = 2f;
    /// <summary>Tint the old scene floor tilemap used to darken/desaturate the floor.</summary>
    public static readonly Color DefaultFloorTint = new Color(0.27224994f, 0.3489652f, 0.3584906f, 1f);

    public struct BakeConfig
    {
        public TileBase? WallTile;
        public GameObject? CratePrefab;
        public float CellSize;
        public string PrefabFolder;
        public string TemplateFolder;

        /// <summary>When true, bakes a large black quad behind the room so the arena floor and everything outside
        /// it read as a black void — no apron, no default background.</summary>
        public bool BlackBackdrop;

        /// <summary>Opaque sprite tinted black for the void quad. Defaults to the wall tile's sprite.</summary>
        public Sprite? WallBackdropSprite;

        /// <summary>Tiled floor sprite baked into the room, clipped to the room bounds.</summary>
        public Sprite? FloorSprite;

        /// <summary>World units each floor tile renders at (e.g. 20 = the old scene floor scale).</summary>
        public float FloorTileScale;

        /// <summary>Tint multiplied onto the floor sprite (matches the old scene floor's darkening).</summary>
        public Color FloorTint;

        /// <summary>Uniform world scale of the baked room (2 = the authored arenas' tile scale).</summary>
        public float WorldScale;

        public static BakeConfig Default() => new BakeConfig
        {
            WallTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultWallTilePath),
            CratePrefab = null,
            CellSize = 1f,
            PrefabFolder = DefaultPrefabFolder,
            TemplateFolder = DefaultTemplateFolder,
            BlackBackdrop = false,
            WallBackdropSprite = LoadWallBackdropSprite(),
            FloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultFloorSpritePath),
            FloorTileScale = DefaultFloorScale,
            FloorTint = DefaultFloorTint,
            WorldScale = DefaultWorldScale,
        };
    }

    /// <summary>
    /// Sprite tiled behind the margin bands. Prefers the wall RuleTile's first fill sprite (so the backdrop
    /// matches the arena walls), then its default sprite, then a direct wall sprite asset.
    /// </summary>
    private static Sprite? LoadWallBackdropSprite()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Object>(DefaultWallTilePath);
        if (tile != null)
        {
            var so = new SerializedObject(tile);
            SerializedProperty rules = so.FindProperty("m_TilingRules");
            if (rules != null && rules.arraySize > 0)
            {
                SerializedProperty sprites = rules.GetArrayElementAtIndex(0).FindPropertyRelative("m_Sprites");
                if (sprites != null && sprites.arraySize > 0 && sprites.GetArrayElementAtIndex(0).objectReferenceValue is Sprite ruleSprite)
                {
                    return ruleSprite;
                }
            }

            SerializedProperty defaultSprite = so.FindProperty("m_DefaultSprite");
            if (defaultSprite != null && defaultSprite.objectReferenceValue is Sprite ds)
            {
                return ds;
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(DefaultWallSpritePath);
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
        float ws = config.WorldScale <= 0f ? DefaultWorldScale : config.WorldScale;
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

            // Tiled floor behind the walls, clipped to the room bounds so nothing shows outside it.
            if (config.FloorSprite != null)
            {
                Color floorTint = config.FloorTint.a <= 0f ? DefaultFloorTint : config.FloorTint;
                CreateFloor(rootGO, config.FloorSprite, w * cs, h * cs, config.FloorTileScale <= 0f ? DefaultFloorScale : config.FloorTileScale, floorTint);
            }

            // Optional black void quad behind everything (unused when the arena camera clears to black).
            if (config.BlackBackdrop && config.WallBackdropSprite != null)
            {
                CreateBlackBackdrop(rootGO, config.WallBackdropSprite);
            }

            // Apply the world scale AFTER all children are parented so it scales the room uniformly
            // (SetParent keeps world scale, which would otherwise cancel a scale set on the empty root).
            rootGO.transform.localScale = new Vector3(ws, ws, ws);

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

    private static void CreateFloor(GameObject root, Sprite sprite, float worldW, float worldH, float scale, Color tint)
    {
        var go = new GameObject("Floor");
        go.transform.SetParent(root.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(worldW / scale, worldH / scale);
        sr.sortingOrder = -10; // behind the walls, in front of the black void
    }

    private static void CreateBlackBackdrop(GameObject root, Sprite sprite)
    {
        var go = new GameObject("Backdrop");
        go.transform.SetParent(root.transform);
        go.transform.localPosition = Vector3.zero;
        Vector2 spriteSize = sprite.bounds.size;
        float sx = spriteSize.x > 0f ? BackdropSize / spriteSize.x : BackdropSize;
        float sy = spriteSize.y > 0f ? BackdropSize / spriteSize.y : BackdropSize;
        go.transform.localScale = new Vector3(sx, sy, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.black;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingOrder = -1000; // behind everything
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
