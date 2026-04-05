using UnityEngine;
using UnityEngine.EventSystems;

public class GearSlotUI : MonoBehaviour, IDropHandler
{
    private GearEditorUI editor;
    private int slotIndex;

    public void Initialize(GearEditorUI editor, int index)
    {
        this.editor = editor;
        this.slotIndex = index;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GearTileUI tile = eventData.pointerDrag.GetComponent<GearTileUI>();

        if (tile != null)
        {
            editor.PlaceTileInSlot(tile, slotIndex);
            Destroy(tile.gameObject);  // To avoid duplicates
        }
    }
}