#if UNITY_EDITOR
#nullable enable

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Creates PauseMenu.unity, wires PauseMenuController, adds build settings entry,
/// and attaches AuxiliarySceneAdder + AudioSystem mixer on CoreSystems.
/// Menu: SwordGear/Setup/Setup Pause Menu
/// </summary>
public static class PauseMenuSetup
{
    private const string PauseScenePath = "Assets/Aaron/Scenes/PauseMenu.unity";
    private const string CoreSystemsPrefabPath = "Assets/Aaron/Prefabs/CoreSystems.prefab";
    private const string MixerPath = "Assets/Audio/MainMixer.mixer";

    [MenuItem("SwordGear/Setup/Setup Pause Menu")]
    public static void SetupFromMenu()
    {
        Setup();
        Debug.Log("PauseMenuSetup: complete.");
    }

    public static void Setup()
    {
        CreateOrRebuildPauseScene();
        EnsureBuildSettingsEntry();
        WireCoreSystems();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrRebuildPauseScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        var root = new GameObject("PauseMenuRoot");
        SceneManagerMove(root, scene);

        var canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var pauseButtonRoot = CreatePauseButton(canvasGo.transform);
        var panelRoot = CreatePausePanel(canvasGo.transform, out Button resumeButton, out Button returnButton, out Slider volumeSlider);

        var controllerGo = new GameObject("PauseMenuController");
        controllerGo.transform.SetParent(root.transform, false);
        var controller = controllerGo.AddComponent<PauseMenuController>();

        var so = new SerializedObject(controller);
        so.FindProperty("pauseButtonRoot").objectReferenceValue = pauseButtonRoot;
        so.FindProperty("pausePanelRoot").objectReferenceValue = panelRoot;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButtonRoot.GetComponent<Button>();
        so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
        so.FindProperty("returnToTitleButton").objectReferenceValue = returnButton;
        so.FindProperty("masterVolumeSlider").objectReferenceValue = volumeSlider;
        so.FindProperty("loadingScreenChannel").objectReferenceValue = LoadBoolChannel(
            "Assets/Aaron/ScriptableObjects/EventChannels/EnableLoadingScreen.asset");
        so.FindProperty("defeatVisibilityChannel").objectReferenceValue = LoadBoolChannel(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/DefeatOverlayVisibilityChannel.asset");
        so.FindProperty("augmentShopVisibilityChannel").objectReferenceValue = LoadBoolChannel(
            "Assets/Aaron/ScriptableObjects/EventChannels/Augments Shop/EnableAugmentShopEventChannel.asset");
        so.FindProperty("stageCompleteVisibilityChannel").objectReferenceValue = LoadBoolChannel(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/StageCompleteVisibilityChannel.asset");
        so.FindProperty("restVisibilityChannel").objectReferenceValue = LoadBoolChannel(
            "Assets/Aaron/ScriptableObjects/EventChannels/MapRun/RestNodeVisibilityChannel.asset");
        so.FindProperty("sceneChangeRequestChannel").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<StringEventChannelSO>(
                "Assets/Aaron/ScriptableObjects/EventChannels/SceneTransitionEventChannel.asset");
        so.FindProperty("audioMixer").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        pauseButtonRoot.SetActive(false);
        panelRoot.SetActive(false);

        EditorSceneManager.SaveScene(scene, PauseScenePath);
        EditorSceneManager.CloseScene(scene, true);
    }

    private static GameObject CreatePauseButton(Transform parent)
    {
        var buttonGo = CreateUiButton(parent, "PauseButton", "II", new Vector2(-48f, -48f), new Vector2(96f, 96f));
        var rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-32f, -32f);
        return buttonGo;
    }

    private static GameObject CreatePausePanel(
        Transform parent,
        out Button resumeButton,
        out Button returnButton,
        out Slider volumeSlider)
    {
        var panelRoot = new GameObject("PausePanel", typeof(RectTransform));
        panelRoot.transform.SetParent(parent, false);
        var rootRt = panelRoot.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
        dimmer.transform.SetParent(panelRoot.transform, false);
        StretchFull(dimmer.GetComponent<RectTransform>());
        var dimmerImage = dimmer.GetComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.65f);
        dimmerImage.raycastTarget = true;

        var card = new GameObject("PanelCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panelRoot.transform, false);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(520f, 420f);
        card.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        CreateTmpLabel(card.transform, "Title", "Paused", 48f, new Vector2(0f, 150f), new Vector2(440f, 60f));

        resumeButton = CreateUiButton(card.transform, "ResumeButton", "Resume", new Vector2(0f, 60f), new Vector2(320f, 64f))
            .GetComponent<Button>();

        CreateTmpLabel(card.transform, "VolumeLabel", "Master Volume", 28f, new Vector2(0f, -10f), new Vector2(320f, 36f));

        volumeSlider = CreateSlider(card.transform, "MasterVolumeSlider", new Vector2(0f, -60f), new Vector2(360f, 28f));

        returnButton = CreateUiButton(card.transform, "ReturnToTitleButton", "Return to Title", new Vector2(0f, -150f), new Vector2(320f, 64f))
            .GetComponent<Button>();

        return panelRoot;
    }

    private static GameObject CreateUiButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f, 1f);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return go;
    }

    private static void CreateTmpLabel(Transform parent, string name, string text, float fontSize, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        StretchFull(fillAreaRt);
        fillAreaRt.offsetMin = new Vector2(5f, 0f);
        fillAreaRt.offsetMax = new Vector2(-5f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        StretchFull(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = new Color(0.45f, 0.7f, 0.95f, 1f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(24f, 24f);
        handle.GetComponent<Image>().color = Color.white;

        var slider = go.GetComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static BoolEventChannelSO? LoadBoolChannel(string path)
    {
        return AssetDatabase.LoadAssetAtPath<BoolEventChannelSO>(path);
    }

    private static void EnsureBuildSettingsEntry()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var existing in scenes)
        {
            if (existing.path == PauseScenePath)
            {
                existing.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(PauseScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void WireCoreSystems()
    {
        var root = PrefabUtility.LoadPrefabContents(CoreSystemsPrefabPath);
        try
        {
            // Audio mixer on AudioManager
            var audio = root.GetComponentInChildren<AudioSystem>(true);
            if (audio != null)
            {
                var audioSo = new SerializedObject(audio);
                var mixerProp = audioSo.FindProperty("masterMixer");
                if (mixerProp != null)
                {
                    mixerProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
                    audioSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Avoid duplicate PauseMenu AuxiliarySceneAdder
            bool alreadyHasPauseAdder = false;
            foreach (var adder in root.GetComponentsInChildren<AuxiliarySceneAdder>(true))
            {
                var adderSo = new SerializedObject(adder);
                var sceneNameProp = adderSo.FindProperty("auxiliaryScene.sceneName");
                if (sceneNameProp != null && sceneNameProp.stringValue == "PauseMenu")
                {
                    alreadyHasPauseAdder = true;
                    break;
                }
            }

            if (!alreadyHasPauseAdder)
            {
                var inputManager = FindChild(root.transform, "Gameplay Input Manager");
                Transform parent = inputManager != null ? inputManager : root.transform;

                var adder = parent.gameObject.AddComponent<AuxiliarySceneAdder>();
                var adderSo = new SerializedObject(adder);
                var transitioner = root.GetComponentInChildren<SceneTransitioner>(true);
                adderSo.FindProperty("sceneTransitioner").objectReferenceValue = transitioner;
                adderSo.FindProperty("auxiliaryScene.sceneName").stringValue = "PauseMenu";
                adderSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, CoreSystemsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform? FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static void SceneManagerMove(GameObject go, UnityEngine.SceneManagement.Scene scene)
    {
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    }
}
#endif
