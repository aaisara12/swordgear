#if UNITY_EDITOR
#nullable enable

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time scene wiring for the defeat overlay and combat HUD visibility listeners.
/// Menu: Aaron/Setup Defeat Overlay
/// </summary>
public static class DefeatOverlaySetup
{
    private const string CombatHudScenePath = "Assets/Scenes/Main/CombatHUD.unity";
    private const string ArenaScenePath = "Assets/Scenes/Main/Arena.unity";

    [MenuItem("Aaron/Setup Defeat Overlay")]
    public static void SetupFromMenu()
    {
        Setup();
        Debug.Log("DefeatOverlaySetup: complete.");
    }

    public static void Setup()
    {
        AssetDatabase.ImportAsset(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/DefeatOverlayVisibilityChannel.asset",
            ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/DefeatContinueChannel.asset",
            ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/CombatHudVisibilityChannel.asset",
            ImportAssetOptions.ForceUpdate);

        SetupCombatHudScene();
        SetupArenaScene();
        AssetDatabase.SaveAssets();
    }

    private static void SetupCombatHudScene()
    {
        var scene = EditorSceneManager.OpenScene(CombatHudScenePath, OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("DefeatOverlaySetup: Canvas not found in CombatHUD.");
            return;
        }

        var combatHudChannel = AssetDatabase.LoadAssetAtPath<BoolEventChannelSO>(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/CombatHudVisibilityChannel.asset");
        var defeatVisibilityChannel = AssetDatabase.LoadAssetAtPath<BoolEventChannelSO>(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/DefeatOverlayVisibilityChannel.asset");
        var defeatContinueChannel = AssetDatabase.LoadAssetAtPath<TriggerEventChannelSO>(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/DefeatContinueChannel.asset");

        if (combatHudChannel == null || defeatVisibilityChannel == null || defeatContinueChannel == null)
        {
            Debug.LogError(
                $"DefeatOverlaySetup: missing event channel assets. " +
                $"combatHud={combatHudChannel != null}, defeatVisibility={defeatVisibilityChannel != null}, defeatContinue={defeatContinueChannel != null}");
            return;
        }

        WireVisibility(canvas.transform, "UltUI", combatHudChannel);
        WireVisibility(canvas.transform, "TotalPointsText", combatHudChannel);
        WireVisibility(canvas.transform, "WaveBanner", combatHudChannel);
        WireVisibility(canvas.transform, "MinimapRoot", combatHudChannel);
        WireVisibility(canvas.transform, "CombatHUDRoot", combatHudChannel);

        var healthBar = GameObject.Find("PlayerHealthBarRoot");
        if (healthBar != null)
        {
            WireVisibilityOnObject(healthBar, combatHudChannel);
        }

        var defeatPanel = GameObject.Find("DefeatOverlay");
        if (defeatPanel == null)
        {
            defeatPanel = CreateDefeatOverlay(canvas.transform);
        }

        // Controller must sit on an active object so Awake runs and subscribes to the channel.
        var controllerHost = GameObject.Find("DefeatOverlayController");
        if (controllerHost == null)
        {
            controllerHost = new GameObject("DefeatOverlayController");
            controllerHost.transform.SetParent(canvas.transform, false);
        }

        controllerHost.SetActive(true);

        var misplacedController = defeatPanel.GetComponent<DefeatStateController>();
        if (misplacedController != null)
        {
            Object.DestroyImmediate(misplacedController);
        }

        var controller = controllerHost.GetComponent<DefeatStateController>();
        if (controller == null)
        {
            controller = controllerHost.AddComponent<DefeatStateController>();
        }

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("visibilityChannel").objectReferenceValue = defeatVisibilityChannel;
        serialized.FindProperty("combatHudVisibilityChannel").objectReferenceValue = combatHudChannel;
        serialized.FindProperty("continueChannel").objectReferenceValue = defeatContinueChannel;
        serialized.FindProperty("view").objectReferenceValue = defeatPanel;
        serialized.FindProperty("overlayGroup").objectReferenceValue = defeatPanel.GetComponent<CanvasGroup>();
        serialized.FindProperty("headlineText").objectReferenceValue =
            defeatPanel.transform.Find("Headline")?.GetComponent<TMP_Text>();
        serialized.FindProperty("subtitleText").objectReferenceValue =
            defeatPanel.transform.Find("Subtitle")?.GetComponent<TMP_Text>();
        serialized.FindProperty("skipHintText").objectReferenceValue =
            defeatPanel.transform.Find("SkipHint")?.GetComponent<TMP_Text>();
        serialized.FindProperty("skipButton").objectReferenceValue =
            defeatPanel.GetComponent<Button>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        defeatPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupArenaScene()
    {
        var scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        var joysticks = GameObject.Find("Simulated Joysticks Displayer");
        if (joysticks == null)
        {
            Debug.LogWarning("DefeatOverlaySetup: Simulated Joysticks Displayer not found in Arena.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        var combatHudChannel = AssetDatabase.LoadAssetAtPath<BoolEventChannelSO>(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/CombatHudVisibilityChannel.asset");
        if (combatHudChannel == null)
        {
            Debug.LogError("DefeatOverlaySetup: CombatHudVisibilityChannel asset missing.");
            return;
        }

        WireVisibilityOnObject(joysticks, combatHudChannel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void WireVisibility(Transform parent, string objectName, BoolEventChannelSO channel)
    {
        var targetTransform = parent.Find(objectName);
        if (targetTransform == null)
        {
            Debug.LogWarning($"DefeatOverlaySetup: '{objectName}' not found under {parent.name}.");
            return;
        }

        WireVisibilityOnObject(targetTransform.gameObject, channel);
    }

    private static void WireVisibilityOnObject(GameObject target, BoolEventChannelSO channel)
    {
        var visibility = target.GetComponent<ChannelDrivenVisibility>();
        if (visibility == null)
        {
            visibility = target.AddComponent<ChannelDrivenVisibility>();
        }

        var serialized = new SerializedObject(visibility);
        serialized.FindProperty("visibilityChannel").objectReferenceValue = channel;
        serialized.FindProperty("target").objectReferenceValue = target;
        serialized.FindProperty("initiallyVisible").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateDefeatOverlay(Transform canvas)
    {
        var root = new GameObject("DefeatOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button));
        root.transform.SetParent(canvas, false);

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = root.GetComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        image.raycastTarget = true;

        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;

        var button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        CreateText(root.transform, "Headline", "DEFEATED", 96, FontStyles.Bold, new Vector2(0, 80));
        CreateText(root.transform, "Subtitle", "Your run is over.", 36, FontStyles.Normal, new Vector2(0, -20));
        CreateText(root.transform, "SkipHint", "Tap to continue", 24, FontStyles.Italic, new Vector2(0, -220));

        return root;
    }

    private static void CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900, 120);
        rect.anchoredPosition = anchoredPosition;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }
}
#endif
