#nullable enable

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single clickable node on the run map. Displays the node type and reflects its state
/// (selectable / completed / locked), and reports clicks back to the map controller.
/// </summary>
public class MapNodeButton : MonoBehaviour
{
    [SerializeField] private Button? button;
    [SerializeField] private TMP_Text? label;
    [SerializeField] private Image? background;

    [Header("State Colors")]
    [SerializeField] private Color selectableColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.5f, 0.2f, 1f);
    [SerializeField] private Color currentColor = new Color(0.95f, 0.85f, 0.2f, 1f);

    public RectTransform RectTransform => (RectTransform)transform;

    private int _nodeId;
    private Action<int>? _onClicked;

    public void Setup(MapNode node, bool isSelectable, bool isCurrent, Action<int> onClicked)
    {
        _nodeId = node.Id;
        _onClicked = onClicked;

        if (label != null)
        {
            label.text = node.Type.ToString();
        }

        if (background != null)
        {
            if (isCurrent)
            {
                background.color = currentColor;
            }
            else if (node.Completed)
            {
                background.color = completedColor;
            }
            else if (isSelectable)
            {
                background.color = selectableColor;
            }
            else
            {
                background.color = lockedColor;
            }
        }

        if (button != null)
        {
            button.interactable = isSelectable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        _onClicked?.Invoke(_nodeId);
    }
}
