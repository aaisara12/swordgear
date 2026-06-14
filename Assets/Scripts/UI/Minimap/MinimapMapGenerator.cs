#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Rasterizes wall tilemaps from an instantiated room into a schematic minimap texture.
/// Works with any runtime-spawned level prefab — no per-level hardcoding.
/// </summary>
public static class MinimapMapGenerator
{
    public const int Resolution = 256;

    private static readonly Color32 FloorColor = new(0x12, 0x12, 0x1F, 0xFF);
    private static readonly Color32 WallColor = new(0x9A, 0x9A, 0xBF, 0xFF);

    private readonly struct WallTilemapData
    {
        public readonly Tilemap Tilemap;
        public readonly BoundsInt CellBounds;
        public readonly Vector3 WorldCellSize;

        public WallTilemapData(Tilemap tilemap, BoundsInt cellBounds, Vector3 worldCellSize)
        {
            Tilemap = tilemap;
            CellBounds = cellBounds;
            WorldCellSize = worldCellSize;
        }
    }

    public static (Texture2D? map, Bounds bounds, bool hasContent) Generate(GameObject roomRoot)
    {
        List<WallTilemapData> wallTilemaps = CollectWallTilemaps(roomRoot);
        if (wallTilemaps.Count == 0)
        {
            Debug.LogWarning($"MinimapMapGenerator: No wall tilemaps found on '{roomRoot.name}'.");
            return (null, default, false);
        }

        if (!TryComputeTilemapBounds(wallTilemaps, out Bounds bounds))
        {
            Debug.LogWarning($"MinimapMapGenerator: Wall tilemaps on '{roomRoot.name}' contain no tiles.");
            return (null, default, false);
        }

        Color32[] pixels = new Color32[Resolution * Resolution];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = FloorColor;
        }

        RasterizeTilemaps(wallTilemaps, bounds, pixels);

        Texture2D map = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        map.SetPixels32(pixels);
        map.Apply();
        return (map, bounds, true);
    }

    public static Vector2 WorldToNormalized(Vector2 worldPos, Bounds bounds)
    {
        return new Vector2(
            Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldPos.x),
            Mathf.InverseLerp(bounds.min.y, bounds.max.y, worldPos.y));
    }

    private static List<WallTilemapData> CollectWallTilemaps(GameObject roomRoot)
    {
        Tilemap[] allTilemaps = roomRoot.GetComponentsInChildren<Tilemap>(true);
        List<WallTilemapData> wallTilemaps = new List<WallTilemapData>(allTilemaps.Length);

        for (int i = 0; i < allTilemaps.Length; i++)
        {
            Tilemap tilemap = allTilemaps[i];
            if (!tilemap.gameObject.activeInHierarchy)
            {
                continue;
            }

            TilemapCollider2D? collider = tilemap.GetComponent<TilemapCollider2D>();
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            tilemap.CompressBounds();
            wallTilemaps.Add(new WallTilemapData(tilemap, tilemap.cellBounds, GetWorldCellSize(tilemap)));
        }

        return wallTilemaps;
    }

    private static bool TryComputeTilemapBounds(List<WallTilemapData> wallTilemaps, out Bounds bounds)
    {
        bool hasContentBounds = false;
        Bounds contentBounds = default;

        for (int i = 0; i < wallTilemaps.Count; i++)
        {
            WallTilemapData wallTilemap = wallTilemaps[i];
            foreach (Vector3Int cell in wallTilemap.CellBounds.allPositionsWithin)
            {
                if (!wallTilemap.Tilemap.HasTile(cell))
                {
                    continue;
                }

                Bounds tileBounds = new Bounds(wallTilemap.Tilemap.GetCellCenterWorld(cell), wallTilemap.WorldCellSize);
                if (!hasContentBounds)
                {
                    contentBounds = tileBounds;
                    hasContentBounds = true;
                }
                else
                {
                    contentBounds.Encapsulate(tileBounds);
                }
            }
        }

        if (!hasContentBounds)
        {
            bounds = default;
            return false;
        }

        contentBounds.Expand(0.5f);
        bounds = contentBounds;
        return true;
    }

    private static void RasterizeTilemaps(List<WallTilemapData> wallTilemaps, Bounds bounds, Color32[] pixels)
    {
        for (int i = 0; i < wallTilemaps.Count; i++)
        {
            WallTilemapData wallTilemap = wallTilemaps[i];
            foreach (Vector3Int cell in wallTilemap.CellBounds.allPositionsWithin)
            {
                if (!wallTilemap.Tilemap.HasTile(cell))
                {
                    continue;
                }

                Bounds tileBounds = new Bounds(wallTilemap.Tilemap.GetCellCenterWorld(cell), wallTilemap.WorldCellSize);
                StampWorldBounds(tileBounds, bounds, pixels);
            }
        }
    }

    private static Vector3 GetWorldCellSize(Tilemap tilemap)
    {
        Vector3 cellSize = tilemap.cellSize;
        return new Vector3(
            Mathf.Abs(cellSize.x * tilemap.transform.lossyScale.x),
            Mathf.Abs(cellSize.y * tilemap.transform.lossyScale.y),
            0.1f);
    }

    private static void StampWorldBounds(Bounds worldBounds, Bounds roomBounds, Color32[] pixels)
    {
        int minX = WorldToGridIndexMin(worldBounds.min.x, roomBounds.min.x, roomBounds.max.x);
        int maxX = WorldToGridIndexMax(worldBounds.max.x, roomBounds.min.x, roomBounds.max.x);
        int minY = WorldToGridIndexMin(worldBounds.min.y, roomBounds.min.y, roomBounds.max.y);
        int maxY = WorldToGridIndexMax(worldBounds.max.y, roomBounds.min.y, roomBounds.max.y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                pixels[y * Resolution + x] = WallColor;
            }
        }
    }

    private static int WorldToGridIndexMin(float worldValue, float min, float max)
    {
        float normalized = Mathf.InverseLerp(min, max, worldValue);
        return Mathf.Clamp(Mathf.FloorToInt(normalized * Resolution), 0, Resolution - 1);
    }

    private static int WorldToGridIndexMax(float worldValue, float min, float max)
    {
        float normalized = Mathf.InverseLerp(min, max, worldValue);
        return Mathf.Clamp(Mathf.CeilToInt(normalized * Resolution) - 1, 0, Resolution - 1);
    }
}
