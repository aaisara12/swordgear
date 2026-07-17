#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Grid-drawing tool for authoring arenas. Paint a <see cref="RoomDefinition"/> cell-by-cell, then
/// bake it into an arena prefab + <see cref="ArenaLayoutTemplate"/> via <see cref="RoomBaker"/>.
/// Menu: Henry → Room Painter.
/// </summary>
public class RoomPainterWindow : EditorWindow
{
    [SerializeField] private RoomDefinition? room;
    [SerializeField] private RoomCellType selectedType = RoomCellType.Wall;

    // Bake config (persisted while the window is open).
    [SerializeField] private TileBase? wallTile;
    [SerializeField] private TileBase? lowWallTile;
    [SerializeField] private GameObject? cratePrefab;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool blackBackdrop = false;
    [SerializeField] private Sprite? wallBackdropSprite;
    [SerializeField] private Sprite? floorSprite;
    [SerializeField] private float floorScale = RoomBaker.DefaultFloorScale;
    [SerializeField] private string prefabFolder = RoomBaker.DefaultPrefabFolder;
    [SerializeField] private string templateFolder = RoomBaker.DefaultTemplateFolder;

    [SerializeField] private int pendingWidth = 16;
    [SerializeField] private int pendingHeight = 16;
    [SerializeField] private bool bakeFoldout = true;

    private Vector2 scroll;
    private const float CellPixels = 22f;

    [MenuItem("Henry/Room Painter")]
    public static void Open()
    {
        var window = GetWindow<RoomPainterWindow>("Room Painter");
        window.minSize = new Vector2(420f, 520f);
    }

    private void OnEnable()
    {
        if (wallTile == null)
        {
            wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(RoomBaker.DefaultWallTilePath);
        }

        if (lowWallTile == null)
        {
            lowWallTile = AssetDatabase.LoadAssetAtPath<TileBase>(RoomBaker.DefaultLowWallTilePath);
        }

        if (wallBackdropSprite == null)
        {
            wallBackdropSprite = RoomBaker.BakeConfig.Default().WallBackdropSprite;
        }

        if (floorSprite == null)
        {
            floorSprite = RoomBaker.BakeConfig.Default().FloorSprite;
        }
    }

    private void OnGUI()
    {
        DrawRoomAssetSection();
        if (room == null)
        {
            EditorGUILayout.HelpBox("Assign or create a Room Definition to start painting.", MessageType.Info);
            return;
        }

        room.EnsureSize();
        DrawSizeSection();
        DrawPalette();
        DrawValidation();
        EditorGUILayout.Space(4f);
        DrawGrid();
        EditorGUILayout.Space(6f);
        DrawBakeSection();
    }

    private void DrawRoomAssetSection()
    {
        EditorGUILayout.LabelField("Room", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            var picked = (RoomDefinition?)EditorGUILayout.ObjectField(room, typeof(RoomDefinition), false);
            if (picked != room)
            {
                room = picked;
                if (room != null)
                {
                    room.EnsureSize();
                    pendingWidth = room.Width;
                    pendingHeight = room.Height;
                }
            }

            if (GUILayout.Button("New…", GUILayout.Width(60f)))
            {
                CreateNewRoom();
            }
        }
    }

    private void CreateNewRoom()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "New Room Definition", "RoomDefinition", "asset", "Where to save the room?");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var created = CreateInstance<RoomDefinition>();
        created.Resize(pendingWidth, pendingHeight);
        created.FillBorderWalls();
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        room = created;
        pendingWidth = room.Width;
        pendingHeight = room.Height;
    }

    private void DrawSizeSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            pendingWidth = Mathf.Max(1, EditorGUILayout.IntField("Width", pendingWidth));
            pendingHeight = Mathf.Max(1, EditorGUILayout.IntField("Height", pendingHeight));
            if (GUILayout.Button("Apply", GUILayout.Width(60f)))
            {
                Undo.RecordObject(room, "Resize Room");
                room!.Resize(pendingWidth, pendingHeight);
                MarkRoomDirty();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Fill Border Walls"))
            {
                Undo.RecordObject(room, "Fill Border Walls");
                room!.FillBorderWalls();
                MarkRoomDirty();
            }

            if (GUILayout.Button("Clear All"))
            {
                Undo.RecordObject(room, "Clear Room");
                room!.Clear();
                MarkRoomDirty();
            }

            if (GUILayout.Button("Save Asset"))
            {
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void DrawPalette()
    {
        EditorGUILayout.LabelField("Palette (click to select, then paint the grid)", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (RoomCellType type in System.Enum.GetValues(typeof(RoomCellType)))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = ColorFor(type);
                var style = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = selectedType == type ? FontStyle.Bold : FontStyle.Normal,
                };
                string label = selectedType == type ? $"▸ {type}" : type.ToString();
                if (GUILayout.Button(label, style, GUILayout.Height(26f)))
                {
                    selectedType = type;
                }

                GUI.backgroundColor = prev;
            }
        }

        EditorGUILayout.LabelField("Left-click paints selected · Right-click erases to Empty", EditorStyles.miniLabel);
    }

    private void DrawValidation()
    {
        List<string> errors = RoomBaker.Validate(room!);
        if (errors.Count == 0)
        {
            EditorGUILayout.HelpBox("Valid — ready to bake.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Not bakeable yet:\n• " + string.Join("\n• ", errors), MessageType.Warning);
        }
    }

    private void DrawGrid()
    {
        int w = room!.Width, h = room.Height;
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(Mathf.Min(h * CellPixels + 8f, 360f)));
        Rect gridRect = GUILayoutUtility.GetRect(w * CellPixels, h * CellPixels, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

        // Draw cells. Row y = 0 is the bottom, so display top-down as (h - 1 - y).
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var r = new Rect(
                    gridRect.x + x * CellPixels,
                    gridRect.y + (h - 1 - y) * CellPixels,
                    CellPixels - 1f,
                    CellPixels - 1f);
                EditorGUI.DrawRect(r, ColorFor(room.GetCell(x, y)));
            }
        }

        HandleGridInput(gridRect, w, h);
        EditorGUILayout.EndScrollView();
    }

    private void HandleGridInput(Rect gridRect, int w, int h)
    {
        Event e = Event.current;
        bool paint = e.type == EventType.MouseDown || e.type == EventType.MouseDrag;
        if (!paint || !gridRect.Contains(e.mousePosition))
        {
            return;
        }

        int cx = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / CellPixels);
        int cyTop = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / CellPixels);
        int cy = (h - 1) - cyTop;
        if (!room!.InBounds(cx, cy))
        {
            return;
        }

        RoomCellType target = e.button == 1 ? RoomCellType.Empty : selectedType;
        if (room.GetCell(cx, cy) != target)
        {
            if (e.type == EventType.MouseDown)
            {
                Undo.RecordObject(room, "Paint Room");
            }

            room.SetCell(cx, cy, target);
            MarkRoomDirty();
        }

        e.Use();
    }

    private void DrawBakeSection()
    {
        bakeFoldout = EditorGUILayout.Foldout(bakeFoldout, "Bake Settings", true);
        if (bakeFoldout)
        {
            EditorGUI.indentLevel++;
            wallTile = (TileBase?)EditorGUILayout.ObjectField("Wall Tile", wallTile, typeof(TileBase), false);
            lowWallTile = (TileBase?)EditorGUILayout.ObjectField("Low Wall Tile", lowWallTile, typeof(TileBase), false);
            cratePrefab = (GameObject?)EditorGUILayout.ObjectField("Crate Prefab", cratePrefab, typeof(GameObject), false);
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            floorSprite = (Sprite?)EditorGUILayout.ObjectField("Floor Sprite", floorSprite, typeof(Sprite), false);
            floorScale = EditorGUILayout.FloatField("Floor Tile Scale", floorScale);
            blackBackdrop = EditorGUILayout.Toggle("Black Backdrop", blackBackdrop);
            wallBackdropSprite = (Sprite?)EditorGUILayout.ObjectField("Wall Backdrop Sprite", wallBackdropSprite, typeof(Sprite), false);
            prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
            templateFolder = EditorGUILayout.TextField("Template Folder", templateFolder);
            EditorGUI.indentLevel--;
        }

        using (new EditorGUI.DisabledScope(room == null || wallTile == null))
        {
            if (GUILayout.Button("Bake to Arena Prefab", GUILayout.Height(30f)))
            {
                BakeCurrent();
            }
        }
    }

    private void BakeCurrent()
    {
        AssetDatabase.SaveAssets();
        var config = new RoomBaker.BakeConfig
        {
            WallTile = wallTile,
            LowWallTile = lowWallTile,
            CratePrefab = cratePrefab,
            CellSize = cellSize,
            BlackBackdrop = blackBackdrop,
            WallBackdropSprite = wallBackdropSprite,
            FloorSprite = floorSprite,
            FloorTileScale = floorScale,
            PrefabFolder = prefabFolder,
            TemplateFolder = templateFolder,
        };

        ArenaLayoutTemplate? template = RoomBaker.Bake(room!, config, room!.name);
        if (template != null)
        {
            EditorGUIUtility.PingObject(template);
            Selection.activeObject = template;
        }
    }

    private void MarkRoomDirty()
    {
        if (room != null)
        {
            EditorUtility.SetDirty(room);
        }

        Repaint();
    }

    private static Color ColorFor(RoomCellType type) => type switch
    {
        RoomCellType.Empty => new Color(0.16f, 0.16f, 0.18f),
        RoomCellType.Wall => new Color(0.52f, 0.52f, 0.58f),
        RoomCellType.LowWall => new Color(0.42f, 0.58f, 0.62f),
        RoomCellType.Crate => new Color(0.62f, 0.42f, 0.20f),
        RoomCellType.PlayerSpawn => new Color(0.24f, 0.72f, 0.34f),
        RoomCellType.EnemySpawn => new Color(0.82f, 0.26f, 0.26f),
        RoomCellType.Exit => new Color(0.26f, 0.52f, 0.92f),
        _ => Color.magenta,
    };
}
#endif
