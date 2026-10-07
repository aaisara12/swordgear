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
    private Element _grantElement = Element.Fire;
    private Vector2 _scrollPosition;
    private Rect _windowRect = new Rect(16f, 16f, 360f, 480f);

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
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Debug Menu (`)");
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

        DrawUltimateDebugSection();
        DrawCombatDebugSection();
        DrawLoadingDebugSection();

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
        // Grants go through the same element tagging the real offer flow uses, so the ledger counts them.
        IAugmentStoreItem granted = ResolveGrant(augment);
        int owned = _playerBlob!.GetItemCount(granted.Id);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"{augment.DisplayName} [{granted.Element}] (x{owned})", GUILayout.Width(220f));
        if (GUILayout.Button("Grant", GUILayout.Width(60f)))
        {
            _playerBlob.ReceiveItem(granted.Id, 1);
        }

        if (GUILayout.Button("+5", GUILayout.Width(40f)))
        {
            _playerBlob.ReceiveItem(granted.Id, 5);
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Element upgrades always come as their own element; everything else is granted as the element selected in
    /// the ultimate section, so a specific ult level can be dialled in deliberately rather than rolled for.
    /// </summary>
    private IAugmentStoreItem ResolveGrant(IStoreItem augment)
    {
        if (augment is ElementUpgradeLoadableStoreItem upgradeItem
            && UpgradeTypeSerializer.TryGetElement(upgradeItem.ElementUpgrade, out Element upgradeElement))
        {
            return new ElementTaggedStoreItem(augment, upgradeElement);
        }

        return new ElementTaggedStoreItem(augment, _grantElement);
    }

    private void DrawUltimateDebugSection()
    {
        GUILayout.Space(6f);

        UltimateChargeTracker? tracker = UltimateChargeTracker.Instance;
        string levelText = tracker != null
            ? (tracker.IsUnlocked
                ? $"Lv {tracker.OverchargeLevel + 1} (overcharge {tracker.OverchargeLevel}" +
                  $"{(tracker.IsOverchargeCapped ? ", capped" : "")}, {tracker.ChargeProgress:P0} charged)"
                : "locked")
            : "no tracker";
        GUILayout.Label($"Ultimate: {levelText}");
        GUILayout.Label($"  {DescribeNextOverchargeSet(tracker)}");

        GUILayout.Label($"Augments — {DescribeLedger()}");

        GUILayout.Label("Grant stat augments as:");
        GUILayout.BeginHorizontal();
        foreach (Element element in AugmentElementSerializer.RollableElements)
        {
            bool selected = _grantElement == element;
            if (GUILayout.Toggle(selected, element.ToString(), GUI.skin.button) && !selected)
            {
                _grantElement = element;
            }
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Spells out what the next overcharge level still needs, per element, so a stalled level is immediately
    /// attributable: the ability's requirements, the ledger's counts, or neither.
    /// </summary>
    private static string DescribeNextOverchargeSet(UltimateChargeTracker? tracker)
    {
        UltimateAbilitySO? ability = tracker?.ActiveUltimate;
        if (ability == null)
            return "no ultimate assigned";

        if (ability.Requirements.Count == 0)
            return $"{ability.name}: no element requirements — can never overcharge";

        if (tracker!.IsOverchargeCapped)
            return $"{ability.name}: overcharge capped at {ability.MaxOverchargeLevel}";

        var parts = new List<string>();
        foreach (UltimateAbilitySO.ElementRequirement requirement in ability.Requirements)
        {
            int owned = AugmentElementLedger.GetCount(requirement.element);
            int needed = (tracker.CompletedSets + 1) * requirement.count;
            parts.Add($"{requirement.element} {owned}/{needed}");
        }

        return $"{ability.name}: sets {tracker.CompletedSets}, next needs {string.Join("  ", parts)}";
    }

    private static string DescribeLedger()
    {
        var parts = new List<string>();
        foreach (Element element in AugmentElementSerializer.RollableElements)
        {
            parts.Add($"{element} {AugmentElementLedger.GetCount(element)}");
        }

        return string.Join("  ", parts);
    }

    private void DrawCombatDebugSection()
    {
        LevelLoader? loader = LevelLoader.Instance;
        if (loader == null)
        {
            return;
        }

        GUILayout.Space(6f);
        GUILayout.Label("Combat");
        GUILayout.Label($"Wave {Mathf.Clamp(loader.CurrentWaveIndex + 1, 1, Mathf.Max(1, loader.TotalWaveCount))} / {loader.TotalWaveCount}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Wave"))
        {
            loader.DebugClearCurrentWave();
        }

        if (GUILayout.Button("Complete Level"))
        {
            loader.DebugCompleteLevel();
        }

        GUILayout.EndHorizontal();

        if (loader.IsLevelComplete)
        {
            GUILayout.Label("Level complete — walk into the exit portal.");
        }
    }

    private void DrawLoadingDebugSection()
    {
        GUILayout.Space(6f);
        GUILayout.Label("Loading Overlay");

        LoadingScreenAnimator? loading = FindFirstObjectByType<LoadingScreenAnimator>();
        if (loading == null)
        {
            GUILayout.Label("LoadingScreenAnimator not found (Loading scene not loaded).");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Show Boot Loading..."))
        {
            loading.DebugShowBootLoadingOverlay();
        }

        if (GUILayout.Button("Hide Overlay"))
        {
            loading.FadeOutLoadingScreen();
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("Restart Dots"))
        {
            loading.ShowBootLoadingLabel();
        }
    }
#endif
}
