#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using Shop;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for quickly tuning augment ScriptableObjects (names, tiers, costs, stat values).
/// Menu: Henry/Augment Tuner
/// </summary>
public class AugmentTunerWindow : EditorWindow
{
    private const string DefaultCatalogPath = "Assets/Aaron/ScriptableObjects/AugmentCatalog.asset";
    private const string RollSettingsPath = "Assets/Aaron/ScriptableObjects/AugmentTierRollSettings.asset";
    private const string CoreSystemsPrefabPath = "Assets/Aaron/Prefabs/CoreSystems.prefab";
    private const string TestScenePath = "Assets/Aaron/Scenes/AugmentPickerTest.unity";

    private LoadableStoreItemCatalog? _catalog;
    private AugmentTierRollSettings? _rollSettings;
    private SerializedObject? _rollSettingsSerialized;
    private readonly List<LoadableStoreItem> _items = new();
    private Vector2 _listScroll;
    private Vector2 _detailScroll;
    private int _selectedIndex = -1;
    private string _search = string.Empty;
    private int _tierFilterIndex;

    private SerializedObject? _selectedSerialized;
    private SerializedProperty? _displayNameProp;
    private SerializedProperty? _descriptionProp;
    private SerializedProperty? _costProp;
    private SerializedProperty? _qualityTierProp;
    private SerializedProperty? _iconProp;
    private SerializedProperty? _statBoostsProp;

    [MenuItem("Henry/Augment Tuner")]
    public static void Open()
    {
        var window = GetWindow<AugmentTunerWindow>("Augment Tuner");
        window.minSize = new Vector2(720f, 520f);
        window.Show();
    }

    [MenuItem("Henry/Wire Augment Tier Roll Settings")]
    public static void WireRollSettingsFromMenu()
    {
        AugmentTierRollSettings settings = EnsureRollSettingsAsset();
        WireRollSettingsToCoreSystems(settings);
    }

    private void OnEnable()
    {
        if (_catalog == null)
        {
            _catalog = AssetDatabase.LoadAssetAtPath<LoadableStoreItemCatalog>(DefaultCatalogPath);
        }

        _rollSettings = EnsureRollSettingsAsset();
        _rollSettingsSerialized = _rollSettings != null ? new SerializedObject(_rollSettings) : null;

        ReloadItems();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawTierRollSettings();

        EditorGUILayout.Space(4f);

        EditorGUILayout.BeginHorizontal();
        DrawItemList();
        DrawSelectedItemEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var newCatalog = (LoadableStoreItemCatalog?)EditorGUILayout.ObjectField(
            _catalog,
            typeof(LoadableStoreItemCatalog),
            false,
            GUILayout.Width(260f));

        if (newCatalog != _catalog)
        {
            _catalog = newCatalog;
            ReloadItems();
        }

        if (GUILayout.Button("Reload Folder", EditorStyles.toolbarButton, GUILayout.Width(100f)))
        {
            ReloadItems();
        }

        if (GUILayout.Button("Setup Card Visuals", EditorStyles.toolbarButton, GUILayout.Width(120f)))
        {
            AugmentCardVisualSetup.SetupFromMenu();
        }

        if (GUILayout.Button("Open Picker Test", EditorStyles.toolbarButton, GUILayout.Width(120f)))
        {
            if (EditorSceneManagerHelper.OpenScene(TestScenePath))
            {
                EditorApplication.isPlaying = true;
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _search = EditorGUILayout.TextField("Search", _search);
        _tierFilterIndex = EditorGUILayout.Popup("Tier", _tierFilterIndex, new[]
        {
            "All",
            "Low (Bronze)",
            "Medium (Silver)",
            "High (Gold)",
            "Elite (Diamond)",
        });
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTierRollSettings()
    {
        EditorGUILayout.LabelField("Offer Tier Roll Weights", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Relative chance for each tier when rolling an augment offer. Combo performance still sets a minimum floor at runtime.",
            MessageType.None);

        var newSettings = (AugmentTierRollSettings?)EditorGUILayout.ObjectField(
            "Roll Settings Asset",
            _rollSettings,
            typeof(AugmentTierRollSettings),
            false);

        if (newSettings != _rollSettings)
        {
            _rollSettings = newSettings;
            _rollSettingsSerialized = _rollSettings != null ? new SerializedObject(_rollSettings) : null;
        }

        if (_rollSettingsSerialized == null)
        {
            if (GUILayout.Button("Create Roll Settings Asset"))
            {
                _rollSettings = EnsureRollSettingsAsset();
                _rollSettingsSerialized = _rollSettings != null ? new SerializedObject(_rollSettings) : null;
            }

            return;
        }

        _rollSettingsSerialized.Update();

        SerializedProperty weightsProp = _rollSettingsSerialized.FindProperty("weights");
        if (weightsProp != null)
        {
            EditorGUILayout.PropertyField(weightsProp.FindPropertyRelative("Bronze"), new GUIContent("Bronze %"));
            EditorGUILayout.PropertyField(weightsProp.FindPropertyRelative("Silver"), new GUIContent("Silver %"));
            EditorGUILayout.PropertyField(weightsProp.FindPropertyRelative("Gold"), new GUIContent("Gold %"));
            EditorGUILayout.PropertyField(weightsProp.FindPropertyRelative("Diamond"), new GUIContent("Diamond %"));

            float bronze = weightsProp.FindPropertyRelative("Bronze")?.floatValue ?? 0f;
            float silver = weightsProp.FindPropertyRelative("Silver")?.floatValue ?? 0f;
            float gold = weightsProp.FindPropertyRelative("Gold")?.floatValue ?? 0f;
            float diamond = weightsProp.FindPropertyRelative("Diamond")?.floatValue ?? 0f;
            float total = bronze + silver + gold + diamond;
            EditorGUILayout.LabelField("Weight total", total.ToString("0.##"));
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Roll Settings"))
        {
            _rollSettingsSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_rollSettings);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Wire CoreSystems Prefab"))
        {
            WireRollSettingsToCoreSystems(_rollSettings);
        }

        EditorGUILayout.EndHorizontal();

        _rollSettingsSerialized.ApplyModifiedProperties();
    }

    private static AugmentTierRollSettings EnsureRollSettingsAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AugmentTierRollSettings>(RollSettingsPath);
        if (existing != null)
        {
            return existing;
        }

        string? directory = System.IO.Path.GetDirectoryName(RollSettingsPath);
        if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        var asset = ScriptableObject.CreateInstance<AugmentTierRollSettings>();
        AssetDatabase.CreateAsset(asset, RollSettingsPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created {RollSettingsPath}");
        return asset;
    }

    private static void WireRollSettingsToCoreSystems(AugmentTierRollSettings? settings)
    {
        if (settings == null)
        {
            settings = EnsureRollSettingsAsset();
        }

        GameObject? prefabRoot = PrefabUtility.LoadPrefabContents(CoreSystemsPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"AugmentTunerWindow: missing prefab at {CoreSystemsPrefabPath}");
            return;
        }

        try
        {
            var manager = prefabRoot.GetComponentInChildren<InGameAugmentsManager>(true);
            if (manager == null)
            {
                Debug.LogError("AugmentTunerWindow: InGameAugmentsManager not found on CoreSystems prefab.");
                return;
            }

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("tierRollSettings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CoreSystemsPrefabPath);
            Debug.Log("AugmentTunerWindow: wired tier roll settings on CoreSystems prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private void DrawItemList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(260f));
        EditorGUILayout.LabelField("Augments", EditorStyles.boldLabel);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));

        for (int i = 0; i < _items.Count; i++)
        {
            LoadableStoreItem item = _items[i];
            if (!PassesFilters(item))
            {
                continue;
            }

            string label = $"{TierBadge(item.QualityTier)} {AugmentTierVisuals.GetTierDisplayName(item.QualityTier)} — {item.DisplayName}";
            bool selected = i == _selectedIndex;
            if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton))
            {
                if (!selected)
                {
                    SelectItem(i);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedItemEditor()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);

        if (_selectedIndex < 0 || _selectedIndex >= _items.Count || _selectedSerialized == null)
        {
            EditorGUILayout.HelpBox("Select an augment to edit.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        LoadableStoreItem item = _items[_selectedIndex];
        AugmentQualityTier editTier = _qualityTierProp != null
            ? (AugmentQualityTier)_qualityTierProp.enumValueIndex
            : item.QualityTier;
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        _selectedSerialized.Update();

        DrawTierPreview(editTier);

        EditorGUILayout.LabelField("Quality Tier", EditorStyles.boldLabel);
        if (_qualityTierProp != null)
        {
            AugmentQualityTier currentTier = (AugmentQualityTier)_qualityTierProp.enumValueIndex;
            AugmentQualityTier newTier = DrawTierPopup(currentTier);
            if (newTier != currentTier)
            {
                _qualityTierProp.enumValueIndex = (int)newTier;
                editTier = newTier;
            }
        }

        DrawTierStyleSummary(editTier);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Item Data", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_displayNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);
        EditorGUILayout.PropertyField(_costProp);
        EditorGUILayout.PropertyField(_iconProp);

        if (_statBoostsProp != null)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Stat Boosts", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_statBoostsProp, includeChildren: true);
        }
        else if (item is ElementUpgradeLoadableStoreItem)
        {
            EditorGUILayout.HelpBox("Element upgrade augment — stat boosts are not applicable.", MessageType.None);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Asset"))
        {
            SaveSelected();
        }

        if (GUILayout.Button("Select in Project"))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("Delete Augment"))
        {
            DeleteSelected();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        _selectedSerialized?.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawTierPreview(AugmentQualityTier tier)
    {
        AugmentTierCardStyle style = AugmentTierVisuals.GetCardStyle(tier);
        Rect rect = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));

        EditorGUI.DrawRect(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f),
            new Color(style.AuraColor.r, style.AuraColor.g, style.AuraColor.b, 0.35f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height * 0.55f, rect.width, rect.height * 0.45f), style.ShadowColor);
        EditorGUI.DrawRect(rect, style.BaseColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height * 0.32f),
            new Color(style.HighlightColor.r, style.HighlightColor.g, style.HighlightColor.b, 0.55f));
        EditorGUI.DrawRect(new Rect(rect.x + rect.width * 0.15f, rect.y + rect.height * 0.1f, rect.width * 0.22f, rect.height * 0.75f),
            new Color(1f, 1f, 1f, 0.18f));

        var borderRect = new Rect(rect.x, rect.y, rect.width, 4f);
        EditorGUI.DrawRect(borderRect, style.BorderColor);

        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
        };
        GUI.Label(rect, AugmentTierVisuals.GetTierDisplayName(tier), labelStyle);
        EditorGUILayout.Space(4f);
    }

    private static AugmentQualityTier DrawTierPopup(AugmentQualityTier current)
    {
        string[] labels =
        {
            "Bronze (Low)",
            "Silver (Medium)",
            "Gold (High)",
            "Diamond (Elite)",
        };

        int index = EditorGUILayout.Popup("Card Tier", (int)current, labels);
        return (AugmentQualityTier)Mathf.Clamp(index, 0, labels.Length - 1);
    }

    private static void DrawTierStyleSummary(AugmentQualityTier tier)
    {
        AugmentTierCardStyle style = AugmentTierVisuals.GetCardStyle(tier);
        EditorGUILayout.HelpBox(
            $"Shop card: rim glow {style.GlowStrength:0.0}, inner flare {style.FlareIntensity:0.0}" +
            $"{(style.SweepStrength > 0.01f ? ", light sweep" : string.Empty)}" +
            $"{(style.SparkleStrength > 0.01f ? ", sparkles" : string.Empty)}.",
            MessageType.None);
    }

    private void ReloadItems()
    {
        _items.Clear();
        _selectedIndex = -1;
        _selectedSerialized = null;

        if (_catalog == null)
        {
            return;
        }

        _catalog.Load();
        EditorUtility.SetDirty(_catalog);
        AssetDatabase.SaveAssets();

        foreach (IStoreItem entry in _catalog.GetItems())
        {
            if (entry is LoadableStoreItem loadable)
            {
                _items.Add(loadable);
            }
        }

        _items.Sort((a, b) =>
        {
            int tierCompare = a.QualityTier.CompareTo(b.QualityTier);
            return tierCompare != 0
                ? tierCompare
                : string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal);
        });
    }

    private void SelectItem(int index)
    {
        _selectedIndex = index;
        _selectedSerialized = new SerializedObject(_items[index]);
        _displayNameProp = _selectedSerialized.FindProperty("displayName");
        _descriptionProp = _selectedSerialized.FindProperty("description");
        _costProp = _selectedSerialized.FindProperty("cost");
        _qualityTierProp = _selectedSerialized.FindProperty("qualityTier");
        _iconProp = _selectedSerialized.FindProperty("icon");
        _statBoostsProp = _selectedSerialized.FindProperty("statBoosts");
    }

    private void SaveSelected()
    {
        if (_selectedSerialized == null || _selectedIndex < 0)
        {
            return;
        }

        if (_statBoostsProp != null)
        {
            SyncStatBoostId(_selectedSerialized, _statBoostsProp);
        }

        _selectedSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_items[_selectedIndex]);
        AssetDatabase.SaveAssets();
        ReloadItems();
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            return;
        }

        LoadableStoreItem item = _items[_selectedIndex];
        bool confirmed = EditorUtility.DisplayDialog(
            "Delete Augment",
            $"Permanently delete '{item.DisplayName}' ({item.Id})? This deletes the asset file and cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(item);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();

        if (_catalog != null)
        {
            EditorUtility.SetDirty(_catalog);
        }

        ReloadItems();
    }

    private static void SyncStatBoostId(SerializedObject serialized, SerializedProperty statBoostsProp)
    {
        var entries = new List<StatBoostEntry>();
        for (int i = 0; i < statBoostsProp.arraySize; i++)
        {
            SerializedProperty elem = statBoostsProp.GetArrayElementAtIndex(i);
            SerializedProperty kindProp = elem.FindPropertyRelative("kind");
            SerializedProperty valueProp = elem.FindPropertyRelative("value");
            if (kindProp == null || valueProp == null)
            {
                continue;
            }

            entries.Add(new StatBoostEntry
            {
                kind = (StatBoostKind)kindProp.enumValueIndex,
                value = valueProp.floatValue,
            });
        }

        SerializedProperty? idProp = serialized.FindProperty("id");
        if (idProp != null)
        {
            idProp.stringValue = StatBoostSerializer.Serialize(entries);
        }
    }

    private bool PassesFilters(LoadableStoreItem item)
    {
        if (_tierFilterIndex > 0 && (int)item.QualityTier != _tierFilterIndex - 1)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_search))
        {
            return true;
        }

        string needle = _search.Trim();
        return item.DisplayName.Contains(needle, System.StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(needle, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string TierBadge(AugmentQualityTier tier) => tier switch
    {
        AugmentQualityTier.Medium => "[S]",
        AugmentQualityTier.High => "[G]",
        AugmentQualityTier.Elite => "[D]",
        _ => "[B]",
    };
}

internal static class EditorSceneManagerHelper
{
    public static bool OpenScene(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"Scene not found: {scenePath}");
            return false;
        }

        if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            return true;
        }

        return false;
    }
}
#endif
