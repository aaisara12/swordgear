using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
class TileUIPrefabPair
{
    public GearTile tile;
    public GameObject prefab;
}

public class GearEditorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GearManager gearManager;
    [SerializeField] private GameObject editorRoot;

    [Header("UI Parents")]
    [SerializeField] private Transform gearSlotContainer;
    [SerializeField] private Transform inventoryContainer;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject gearSlotUIPrefab;
    [SerializeField] private List<TileUIPrefabPair> tileUIPrefabs;

    private Dictionary<GearTile, GameObject> tilePrefabMap;

    private List<GearSlotUI> slotUIs = new List<GearSlotUI>();

    private bool isOpen = false;

    private void Awake()
    {
        tilePrefabMap = new Dictionary<GearTile, GameObject>();
        foreach (var pair in tileUIPrefabs)
        {
            tilePrefabMap[pair.tile] = pair.prefab;
        }

        editorRoot.SetActive(false);
    }

    public void ToggleEditor()
    {
        isOpen = !isOpen;
        editorRoot.SetActive(isOpen);

        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            BuildUI();
        }
    }

    private void BuildUI()
    {
        if (gearManager == null)
        {
            gearManager = FindFirstObjectByType<GearManager>();
            if (gearManager == null)
            {
                Debug.LogError("GearManager not found in scene!");
                return;
            }
        }
        ClearUI();

        BuildSlots();
        BuildInventory();
    }

    private void BuildSlots()
    {
        var slots = gearManager.GetSlots();

        float radius = 250f; // UI radius (tweak in inspector later)

        for (int i = 0; i < slots.Count; i++)
        {
            float angle = i * Mathf.PI * 2f / slots.Count;

            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            GameObject obj = Instantiate(gearSlotUIPrefab, gearSlotContainer);
            RectTransform rt = obj.GetComponent<RectTransform>();

            rt.anchoredPosition = pos;

            // Rotate so "up" points toward center (optional but nice)
            Vector2 dirToCenter = -pos.normalized;
            float rot = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg - 90f;
            rt.rotation = Quaternion.Euler(0, 0, rot);

            GearSlotUI slotUI = obj.GetComponent<GearSlotUI>();
            slotUI.Initialize(this, i);

            slotUIs.Add(slotUI);

            // Spawn tile if exists
            if (slots[i].HasValue)
            {
                CreateTileUI(slots[i].Value, obj.transform, i);
            }
        }
    }

    private void BuildInventory()
    {
        var inventory = gearManager.GetInventory();

        foreach (var tile in inventory)
        {
            CreateTileUI(tile, inventoryContainer);
        }
    }

    public GameObject CreateTileUI(GearTile tile, Transform parent, int slotIndex = -1)
    {
        if (!tilePrefabMap.ContainsKey(tile))
        {
            Debug.LogError($"No UI prefab for {tile}");
            return null;
        }

        GameObject obj = Instantiate(tilePrefabMap[tile], parent);
        GearTileUI tileUI = obj.GetComponent<GearTileUI>();

        tileUI.Initialize(this, tile);
        
        // Slightly jank, this handles registering removing a tile from a slot. 
        if (slotIndex != -1)
        {
            tileUI.SetDetachCallback(() => { RemoveTileFromSlot(tileUI, slotIndex); });
        }

        return obj;
    }

    private void ClearUI()
    {
        foreach (Transform child in gearSlotContainer)
            Destroy(child.gameObject);

        foreach (Transform child in inventoryContainer)
            Destroy(child.gameObject);

        slotUIs.Clear();
    }

    public void SetEditorOpen(bool open)
    {
        isOpen = open;

        if (editorRoot != null)
        {
            editorRoot.SetActive(open);
        }

        // Optional: pause game while editing
        Time.timeScale = open ? 0f : 1f;

        // Optional: disable player input here later
        // Example:
        // GameManager.Instance.player.SetInputEnabled(!open);

        Debug.Log($"Gear Editor {(open ? "Opened" : "Closed")}");
    }

    public void PlaceTileInSlot(GearTileUI tileUI, int slotIndex)
    {
        GearTile tile = tileUI.Tile;

        // Remove from inventory
        gearManager.RemoveFromInventory(tile);

        // If slot already had a tile, return it to inventory
        var current = gearManager.GetSlots()[slotIndex];
        if (current.HasValue)
        {
            gearManager.AddToInventory(current.Value);
        }

        // Set new tile
        gearManager.SetTile(slotIndex, tile);

        // Refresh both systems
        gearManager.RefreshVisuals();
        BuildUI();
    }

    public void RemoveTileFromSlot(GearTileUI tileUI, int slotIndex)
    {
        GearTile tile = tileUI.Tile;

        // Add to inventory
        gearManager.AddToInventory(tile);

        // Clear tile slot
        gearManager.SetTile(slotIndex, null);

        // Refresh both systems
        gearManager.RefreshVisuals();
        BuildUI();
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    // Exits the shop node and returns to the run map. Restores time scale in case the editor pause leaked.
    public void EndShopRound()
    {
        Time.timeScale = 1f;

        if (RunManager.Instance != null)
        {
            RunManager.Instance.ReturnToMapAfterNode();
            return;
        }

        // Fallback for legacy round flow (until rounds are fully retired).
        LevelLoader.Instance.AdvanceManually();
    }
}