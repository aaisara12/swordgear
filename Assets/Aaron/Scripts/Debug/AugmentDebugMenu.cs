#nullable enable

using System.Collections.Generic;
using Shop;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Runtime debug menu for granting augments during playtests. Toggle with ` (backquote).
/// </summary>
public class AugmentDebugMenu : InitializeableUnrestrictedGameComponent
{
    [SerializeField] private LoadableStoreItemCatalog[] augmentCatalogs = System.Array.Empty<LoadableStoreItemCatalog>();
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private int debugGoldAmount = 1000;

    private PlayerBlob? _playerBlob;
    private readonly List<IStoreItem> _augments = new List<IStoreItem>();
    private bool _isVisible;
    private Vector2 _scrollPosition;
    private Rect _windowRect = new Rect(16f, 16f, 360f, 420f);

    public override void InitializeOnGameStart_Dangerous(PlayerBlob playerBlob)
    {
        _playerBlob = playerBlob;
        RebuildAugmentList();
    }

    private void RebuildAugmentList()
    {
        _augments.Clear();
        var seenIds = new HashSet<string>();

        foreach (LoadableStoreItemCatalog? catalog in augmentCatalogs)
        {
            if (catalog == null)
            {
                continue;
            }

            foreach (IStoreItem item in catalog.GetItems())
            {
                if (seenIds.Add(item.Id))
                {
                    _augments.Add(item);
                }
            }
        }

        _augments.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal));
    }

    private void Update()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return;
#else
        if (WasTogglePressedThisFrame())
        {
            _isVisible = !_isVisible;
        }
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool WasTogglePressedThisFrame()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        return toggleKey switch
        {
            KeyCode.BackQuote => Keyboard.current.backquoteKey.wasPressedThisFrame,
            KeyCode.Backslash => Keyboard.current.backslashKey.wasPressedThisFrame,
            KeyCode.Equals => Keyboard.current.equalsKey.wasPressedThisFrame,
            _ => false
        };
#else
        return false;
#endif
    }

    private void OnGUI()
    {
        if (!_isVisible || _playerBlob == null)
        {
            return;
        }

        GUI.depth = 1000;
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, $"Augment Debug (`)");
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label($"Gold: {_playerBlob!.WalletLedger}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"+{debugGoldAmount} Gold"))
        {
            _playerBlob.WalletLedger += debugGoldAmount;
        }

        if (GUILayout.Button("Clear Inventory"))
        {
            _playerBlob.ClearInventory();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        if (_augments.Count == 0)
        {
            GUILayout.Label("No augments found. Assign augment catalogs on AugmentDebugMenu.");
        }

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        foreach (IStoreItem augment in _augments)
        {
            DrawAugmentRow(augment);
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }

    private void DrawAugmentRow(IStoreItem augment)
    {
        int owned = _playerBlob!.GetItemCount(augment.Id);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{augment.DisplayName} (x{owned})", GUILayout.Width(220f));
        if (GUILayout.Button("Grant", GUILayout.Width(60f)))
        {
            _playerBlob.ReceiveItem(augment.Id, 1);
        }

        if (GUILayout.Button("+5", GUILayout.Width(40f)))
        {
            _playerBlob.ReceiveItem(augment.Id, 5);
        }

        GUILayout.EndHorizontal();
    }
#endif
}
