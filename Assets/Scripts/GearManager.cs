using UnityEngine;
using System.Collections.Generic;

public enum GearTile
{
    Debug,
    Bumper
}

[System.Serializable]
class TilePrefabPair
{
    public GearTile gearTile;
    public GameObject prefab;

}

public class GearManager : MonoBehaviour
{
    [Header("Slot Settings")]
    public int slotCount = 8;
    public float radius = 2f;
    public GameObject slotPrefab; // Optional: for visualizing slots

    private List<Transform> slots = new List<Transform>();



    [Header("Tile Prefabs")]
    [SerializeField] List<TilePrefabPair> tilePrefabPairs = new();
 
    private Dictionary<GearTile, GameObject> tilePrefabs;

    private void Awake()
    {
        // Build dictionary
        tilePrefabs = new Dictionary<GearTile, GameObject>();
        foreach (TilePrefabPair pair in tilePrefabPairs)
        {
            tilePrefabs[pair.gearTile] = pair.prefab;
        }
    }

    private void Start()
    {
        SpawnSlots();

        // Debug 
        SpawnFullRing(GearTile.Debug);
    }

    void SpawnFullRing(GearTile tile)
    {
        for (int i = 0; i < slots.Count; ++i)
        {
            SpawnTileAt(i, tile);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.player != null)
        {
            // Follow player position 
            transform.position = GameManager.Instance.player.transform.position;
        }
    }

    /// <summary>
    /// Creates slot transforms arranged in a circle, and orients them so their
    /// transform.up points toward the center.
    /// </summary>
    public void SpawnSlots()
    {
        // Clear existing slots if this is called again
        foreach (Transform t in slots)
            if (t != null) Destroy(t.gameObject);

        slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            float angle = i * Mathf.PI * 2f / slotCount;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            Transform slot;

            // If a slot prefab is provided, instantiate it
            if (slotPrefab != null)
            {
                GameObject obj = Instantiate(slotPrefab, transform);
                obj.transform.localPosition = pos;
                slot = obj.transform;
            }
            else
            {
                // Otherwise create an empty GameObject
                GameObject obj = new GameObject("Slot_" + i);
                obj.transform.parent = transform;
                obj.transform.localPosition = pos;
                slot = obj.transform;
            }

            // Make slot.up point toward the center
            Vector2 dirToCenter = (Vector2)transform.position - (Vector2)slot.position;
            slot.up = dirToCenter.normalized;

            slots.Add(slot);
        }
    }

    /// <summary>
    /// Spawns a tile prefab at the slot with index i.
    /// </summary>
    public GameObject SpawnTileAt(int i, GearTile tile)
    {
        if (i < 0 || i >= slots.Count)
        {
            Debug.LogError($"GearManager: Slot index {i} out of range.");
            return null;
        }

        if (!tilePrefabs.ContainsKey(tile) || tilePrefabs[tile] == null)
        {
            Debug.LogError($"GearManager: No prefab registered for {tile}.");
            return null;
        }

        GameObject prefab = tilePrefabs[tile];
        Transform slot = slots[i];

        GameObject instance = Instantiate(prefab, slot.position, slot.rotation, slot);
        return instance;
    }
}
