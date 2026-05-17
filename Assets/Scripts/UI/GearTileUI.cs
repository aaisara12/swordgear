using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GearTileUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GearEditorUI editor;
    private RectTransform rectTransform;
    private Canvas canvas;

    public GearTile Tile { get; private set; }

    private Transform originalParent;

    private CanvasGroup canvasGroup;

    Action detachCallback = null;

    public void Initialize(GearEditorUI editor, GearTile tile)
    {
        this.editor = editor;
        this.Tile = tile;

        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetDetachCallback(Action callback)
    {
        detachCallback = callback;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(canvas.transform);

        canvasGroup.blocksRaycasts = false;

        if (detachCallback != null)
        {
            detachCallback.Invoke();
            detachCallback = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (originalParent == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(originalParent);
        canvasGroup.blocksRaycasts = true;
    }
}