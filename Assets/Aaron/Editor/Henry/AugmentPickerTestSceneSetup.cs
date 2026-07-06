#if UNITY_EDITOR
#nullable enable

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Creates or updates a standalone scene for rolling augment picker offers via a refresh button.
/// Menu: Henry/Setup Augment Picker Test Scene
/// </summary>
public static class AugmentPickerTestSceneSetup
{
    private const string ScenePath = "Assets/Aaron/Scenes/AugmentPickerTest.unity";
    private const string AugmentShopPrefabPath = "Assets/Aaron/Prefabs/Shop/Augment Shop UI.prefab";
    private const string ShowNextAugmentChannelPath =
        "Assets/Aaron/ScriptableObjects/EventChannels/Augments Shop/ShowNextAugmentSetChannel.asset";
    private const string AugmentVisibilityChannelPath =
        "Assets/Aaron/ScriptableObjects/EventChannels/Augments Shop/EnableAugmentShopEventChannel.asset";
    private const string AugmentModelChannelPath =
        "Assets/Aaron/ScriptableObjects/EventChannels/Augments Shop/AugmentShopUiModelEventChannel.asset";
    private const string TestCatalogPath = "Assets/Aaron/ScriptableObjects/TestCatalog.asset";

    [MenuItem("Henry/Setup Augment Picker Test Scene")]
    public static void SetupFromMenu()
    {
        Setup();
        Debug.Log($"AugmentPickerTestSceneSetup: saved {ScenePath}. Press Play, then use Refresh Augments.");
    }

    [MenuItem("Henry/Open Augment Picker Test Scene")]
    public static void OpenFromMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    public static void Setup()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        }

        EnsureEventSystem();

        var augmentShopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AugmentShopPrefabPath);
        if (augmentShopPrefab == null)
        {
            Debug.LogError($"AugmentPickerTestSceneSetup: missing prefab at {AugmentShopPrefabPath}");
            return;
        }

        var augmentShopInstance = (GameObject)PrefabUtility.InstantiatePrefab(augmentShopPrefab, scene);
        augmentShopInstance.name = "Augment Shop UI";

        var showChannel = AssetDatabase.LoadAssetAtPath<TriggerEventChannelSO>(ShowNextAugmentChannelPath);
        var visibilityChannel = AssetDatabase.LoadAssetAtPath<BoolEventChannelSO>(AugmentVisibilityChannelPath);
        var modelChannel = AssetDatabase.LoadAssetAtPath<ItemShopModelEventChannelSO>(AugmentModelChannelPath);
        var catalog = AssetDatabase.LoadAssetAtPath<LoadableStoreItemCatalog>(TestCatalogPath);

        if (showChannel == null || visibilityChannel == null || modelChannel == null || catalog == null)
        {
            Debug.LogError("AugmentPickerTestSceneSetup: missing event channel or catalog assets.");
            return;
        }

        var testSystems = new GameObject("TestSystems");
        var augmentManager = testSystems.AddComponent<InGameAugmentsManager>();
        var harness = testSystems.AddComponent<AugmentPickerTestHarness>();

        var managerSo = new SerializedObject(augmentManager);
        managerSo.FindProperty("triggerAugmentGenerationEventChannel").objectReferenceValue = showChannel;
        managerSo.FindProperty("uiVisibilityEventChannel").objectReferenceValue = visibilityChannel;
        managerSo.FindProperty("uiDataEventChannel").objectReferenceValue = modelChannel;
        managerSo.FindProperty("augmentsCatalog").objectReferenceValue = catalog;
        managerSo.FindProperty("useDebugMinimumTier").boolValue = true;
        managerSo.FindProperty("useDebugExactTier").boolValue = true;
        managerSo.FindProperty("debugMinimumTier").enumValueIndex = 0;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        var harnessSo = new SerializedObject(harness);
        harnessSo.FindProperty("refreshAugmentsChannel").objectReferenceValue = showChannel;
        harnessSo.FindProperty("augmentsManager").objectReferenceValue = augmentManager;
        harnessSo.FindProperty("refreshOnStart").boolValue = true;
        harnessSo.FindProperty("useDebugMinimumTier").boolValue = true;
        harnessSo.FindProperty("debugMinimumTier").enumValueIndex = 0;
        harnessSo.ApplyModifiedPropertiesWithoutUndo();

        CreateDebugUi(harness);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateDebugUi(AugmentPickerTestHarness harness)
    {
        var canvasGo = new GameObject("DebugCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var controls = canvasGo.AddComponent<AugmentPickerTestControls>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(400f, 120f);

        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);

        var labelGo = new GameObject("Hint");
        labelGo.transform.SetParent(panelGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -8f);
        labelRect.sizeDelta = new Vector2(-24f, 30f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "Pick tier, then refresh augments.";
        label.fontSize = 14f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.TopLeft;

        CreateTierButton(panelGo.transform, controls, "Bronze", new Vector2(12f, -44f), controls.SetMinimumTierBronze);
        CreateTierButton(panelGo.transform, controls, "Silver", new Vector2(102f, -44f), controls.SetMinimumTierSilver);
        CreateTierButton(panelGo.transform, controls, "Gold", new Vector2(192f, -44f), controls.SetMinimumTierGold);
        CreateTierButton(panelGo.transform, controls, "Diamond", new Vector2(272f, -44f), controls.SetMinimumTierDiamond);

        var buttonGo = new GameObject("RefreshButton");
        buttonGo.transform.SetParent(panelGo.transform, false);
        var buttonRect = buttonGo.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 12f);
        buttonRect.sizeDelta = new Vector2(-24f, 40f);

        var buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.45f, 0.85f, 1f);
        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        CreateButtonLabel(buttonGo.transform, "Refresh Augments", 18f);

        var controlsSo = new SerializedObject(controls);
        controlsSo.FindProperty("harness").objectReferenceValue = harness;
        controlsSo.ApplyModifiedPropertiesWithoutUndo();

        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, controls.RefreshAugments);
    }

    private static void CreateButtonLabel(Transform parent, string text, float fontSize)
    {
        var labelGo = new GameObject("Text");
        labelGo.transform.SetParent(parent, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateTierButton(
        Transform parent,
        AugmentPickerTestControls controls,
        string label,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick)
    {
        var buttonGo = new GameObject($"{label}Button");
        buttonGo.transform.SetParent(parent, false);
        var buttonRect = buttonGo.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(80f, 28f);

        var buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        CreateButtonLabel(buttonGo.transform, label, 13f);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, onClick);
    }
}
#endif
