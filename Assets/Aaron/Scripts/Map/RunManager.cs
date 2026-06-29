#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent owner of run-long state and the node-map flow. Lives on CoreSystems.prefab so it
/// survives Map &lt;-&gt; Arena scene swaps. Generates the map at run start, tracks the current node,
/// resets run-long state (HP / ultimate / combo) on a new run, and routes node selection to either
/// the Arena scene (Combat/Boss/Shop) or a Map overlay (Augment/Rest).
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager? Instance { get; private set; }

    [Header("Map Generation")]
    [SerializeField] private MapGenerationSettings generationSettings = new MapGenerationSettings();
    [Tooltip("Dev/testing override. When assigned, this fixed map is used instead of procedural generation.")]
    [SerializeField] private FixedMapDefinition? fixedMapOverride;
    [Tooltip("Arena layout used for every combat step until procedural encounter gen is wired.")]
    [SerializeField] private ArenaLayoutTemplate? fallbackCombatLayout;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int fixedSeed = 12345;

    [Header("Scene Transitions")]
    [SerializeField] private StringEventChannelSO? sceneChangeRequestChannel;
    [SerializeField] private SceneReference mapScene = new SceneReference();
    [SerializeField] private SceneReference arenaScene = new SceneReference();
    [Tooltip("Where to go when the run is complete or cleared (e.g. the Title scene).")]
    [SerializeField] private SceneReference runEndScene = new SceneReference();

    [Header("Map Overlays")]
    [Tooltip("Triggers the in-game augment generation/UI (InGameAugmentsManager).")]
    [SerializeField] private TriggerEventChannelSO? showAugmentChannel;
    [Tooltip("Augment UI visibility channel - raised false to close the overlay after a choice.")]
    [SerializeField] private BoolEventChannelSO? augmentVisibilityChannel;
    [SerializeField] private BoolEventChannelSO? restVisibilityChannel;

    [Header("Stage Complete")]
    [SerializeField] private TriggerEventChannelSO? stageCompleteContinueChannel;

    [Header("Run Lifecycle")]
    [SerializeField] private TriggerEventChannelSO? playerDefeatedChannel;

    [Header("Run-long State")]
    [SerializeField] private UltimateChargeTracker? ultimateChargeTracker;

    private RunMap? _currentMap;
    private LinearRunState? _linearRun;
    private int _lastSeed;
    private int _lastMapStepIndex = -1;
    private bool _augmentNodeActive;

    public RunMap? CurrentMap => _currentMap;
    public LinearRunState? Run => _linearRun;
    public RunStep? CurrentStep => _linearRun?.CurrentStep;
    public MapNode? CurrentNode => _currentMap?.CurrentNode;
    public bool HasActiveRun => _linearRun != null;

    /// <summary>Raised whenever the linear run queue or position changes.</summary>
    public event Action? OnRunChanged;

    /// <summary>Raised whenever the map or current position changes so the Map UI can refresh.</summary>
    public event Action? OnMapChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (stageCompleteContinueChannel != null)
        {
            stageCompleteContinueChannel.OnEventTriggered += HandleStageCompleteContinue;
        }

        if (playerDefeatedChannel != null)
        {
            playerDefeatedChannel.OnEventTriggered += HandlePlayerDefeated;
        }

        if (augmentVisibilityChannel != null)
        {
            augmentVisibilityChannel.OnDataChanged += HandleAugmentVisibilityChanged;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (stageCompleteContinueChannel != null)
        {
            stageCompleteContinueChannel.OnEventTriggered -= HandleStageCompleteContinue;
        }

        if (playerDefeatedChannel != null)
        {
            playerDefeatedChannel.OnEventTriggered -= HandlePlayerDefeated;
        }

        if (augmentVisibilityChannel != null)
        {
            augmentVisibilityChannel.OnDataChanged -= HandleAugmentVisibilityChanged;
        }
    }

    #region Run lifecycle

    /// <summary>
    /// Generates a fresh run map, resets run-long state to full, and opens the Map scene.
    /// Hooked to the Title "Play" button.
    /// </summary>
    public void StartNewRun()
    {
        GenerateFreshRun();
        RequestScene(mapScene);
    }

    /// <summary>
    /// Ensures a run exists without requesting a scene change. Called by the Map scene on entry so the
    /// Title "Play" button can simply open the Map scene and have a fresh run generated lazily (and so a
    /// post-defeat re-entry regenerates a brand-new map).
    /// </summary>
    public void EnsureRunStarted()
    {
        if (_linearRun != null)
        {
            return;
        }
        GenerateFreshRun();
    }

    private void GenerateFreshRun()
    {
        _lastSeed = useRandomSeed ? Environment.TickCount : fixedSeed;
        Debug.Log($"RunManager: generating linear run with seed {_lastSeed}.");
        _linearRun = LinearRunGenerator.GenerateInitialBlock(generationSettings.combatLayouts, _lastSeed);
        _currentMap = null;
        _lastMapStepIndex = -1;
        ResetRunLongState();
        OnRunChanged?.Invoke();
        OnMapChanged?.Invoke();
    }

    private void ResetRunLongState()
    {
        PlayerGameplayManager.Instance?.InitializeHealthForNewRun();
        ComboSystem.Instance?.ResetForNewRun();

        UltimateChargeTracker? tracker = ultimateChargeTracker != null
            ? ultimateChargeTracker
            : UltimateChargeTracker.Instance;
        tracker?.ResetForNewRun();

        Time.timeScale = 1f;
    }

    /// <summary>Clears run state (on defeat or after completion). The next run regenerates a fresh map.</summary>
    public void ClearRun()
    {
        _linearRun = null;
        _currentMap = null;
        _lastMapStepIndex = -1;
        OnRunChanged?.Invoke();
        OnMapChanged?.Invoke();
    }

    private void HandlePlayerDefeated()
    {
        ClearRun();
    }

    #endregion

    #region Linear step flow

    /// <summary>Layout for combat Arena loads. Falls back to the first entry in <see cref="MapGenerationSettings.combatLayouts"/>.</summary>
    public ArenaLayoutTemplate? ResolveCombatLayout()
    {
        if (fallbackCombatLayout != null)
        {
            return fallbackCombatLayout;
        }

        IReadOnlyList<ArenaLayoutTemplate> layouts = generationSettings.combatLayouts;
        return layouts.Count > 0 ? layouts[0] : null;
    }

    /// <summary>Wave list for combat steps using the run seed and designer wave pool (no EncounterBuilder).</summary>
    public List<EnemyWaveConfig> BuildCombatWaves()
    {
        List<EnemyWaveConfig> pool = generationSettings.combatWaves;
        if (pool == null || pool.Count == 0)
        {
            return new List<EnemyWaveConfig>();
        }

        int seed = _linearRun?.Seed ?? _lastSeed;
        var rng = new System.Random(seed);
        int minWaves = generationSettings.minWavesPerCombat;
        int maxWaves = generationSettings.maxWavesPerCombat;
        int waveCount = rng.Next(minWaves, maxWaves + 1);

        var waves = new List<EnemyWaveConfig>(waveCount);
        for (int i = 0; i < waveCount; i++)
        {
            waves.Add(pool[rng.Next(pool.Count)]);
        }

        return waves;
    }

    /// <summary>
    /// Trail segment to animate when re-entering the map after advancing a step.
    /// Returns -1 on first map visit or when the step index has not increased.
    /// </summary>
    public int GetTrailTransitionSegmentIndex()
    {
        if (_linearRun == null || _lastMapStepIndex < 0)
        {
            return -1;
        }

        int current = _linearRun.CurrentStepIndex;
        return current > _lastMapStepIndex ? _lastMapStepIndex : -1;
    }

    /// <summary>Records the step shown on the map before leaving for arena/hub.</summary>
    public void MarkMapStepDisplayed()
    {
        _lastMapStepIndex = _linearRun?.CurrentStepIndex ?? -1;
    }

    /// <summary>Called when the map interstitial finishes — loads the scene for the current step.</summary>
    public void OnMapInterstitialComplete()
    {
        if (_linearRun == null)
        {
            return;
        }

        MarkMapStepDisplayed();
        BeginCurrentStep();
    }

    /// <summary>Loads Arena for combat steps. Upgrade hub wiring comes in a later commit.</summary>
    public void BeginCurrentStep()
    {
        RunStep? step = _linearRun?.CurrentStep;
        if (step == null)
        {
            Debug.LogError("RunManager: BeginCurrentStep called with no current step.");
            return;
        }

        if (step.Type == RunStepType.Combat)
        {
            RequestScene(arenaScene);
            return;
        }

        Debug.LogWarning("RunManager: Upgrade step scene load is not implemented yet.");
    }

    #endregion

    #region Node selection / flow

    /// <summary>
    /// Called by the Map UI when the player picks a node. Validates reachability, then routes to the
    /// Arena scene (Combat/Boss/Shop) or opens the appropriate Map overlay (Augment/Rest).
    /// </summary>
    public void SelectNode(int id)
    {
        if (_currentMap == null)
        {
            Debug.LogError("RunManager: SelectNode called with no active run.");
            return;
        }

        if (!_currentMap.IsSelectable(id))
        {
            Debug.LogWarning($"RunManager: node {id} is not currently selectable.");
            return;
        }

        _currentMap.SetCurrentNode(id);
        MapNode? node = _currentMap.CurrentNode;
        if (node == null)
        {
            return;
        }

        OnMapChanged?.Invoke();

        switch (node.Type)
        {
            case NodeType.Combat:
            case NodeType.Boss:
            case NodeType.Shop:
                RequestScene(arenaScene);
                break;
            case NodeType.Augment:
                OpenAugmentOverlay();
                break;
            case NodeType.Rest:
                OpenRestOverlay();
                break;
        }
    }

    private void OpenAugmentOverlay()
    {
        if (showAugmentChannel == null)
        {
            Debug.LogError("RunManager: showAugmentChannel is null; cannot open augment overlay.");
            return;
        }
        _augmentNodeActive = true;
        showAugmentChannel.RaiseEventTriggered();
    }

    // The augment UI raises its visibility channel false once a choice has been made (and the panel closes).
    // We use that as the completion signal for an Augment node, which avoids coupling to the augment UI's
    // internal onAugmentChosen wiring.
    private void HandleAugmentVisibilityChanged(bool isVisible)
    {
        if (isVisible || !_augmentNodeActive)
        {
            return;
        }
        _augmentNodeActive = false;
        HandleAugmentChosen();
    }

    private void OpenRestOverlay()
    {
        if (restVisibilityChannel == null)
        {
            Debug.LogError("RunManager: restVisibilityChannel is null; cannot open rest overlay.");
            return;
        }
        restVisibilityChannel.RaiseDataChanged(true);
    }

    #endregion

    #region Node completion

    /// <summary>
    /// Completes the current Arena node (Combat/Shop) and returns to the Map scene.
    /// Combat reaches this via the Stage Complete "Continue" button; Shop via its exit.
    /// </summary>
    public void ReturnToMapAfterNode()
    {
        if (_currentMap == null)
        {
            return;
        }

        _currentMap.MarkCurrentNodeCompleted();
        Time.timeScale = 1f;
        RequestScene(mapScene);
        OnMapChanged?.Invoke();
    }

    /// <summary>Called when an Augment overlay choice is made: bank performance into tier, mark done, refresh map.</summary>
    public void HandleAugmentChosen()
    {
        augmentVisibilityChannel?.RaiseDataChanged(false);
        ComboSystem.Instance?.ResetPointsSinceLastAugment();
        CompleteOverlayNode();
    }

    /// <summary>Called when the Rest overlay is confirmed: full heal, close overlay, mark done, refresh map.</summary>
    public void ConfirmRest()
    {
        PlayerGameplayManager.Instance?.FullHeal();
        restVisibilityChannel?.RaiseDataChanged(false);
        CompleteOverlayNode();
    }

    private void CompleteOverlayNode()
    {
        _currentMap?.MarkCurrentNodeCompleted();
        OnMapChanged?.Invoke();
    }

    /// <summary>Boss defeated: mark complete and end the run.</summary>
    public void CompleteRun()
    {
        _currentMap?.MarkCurrentNodeCompleted();
        Time.timeScale = 1f;
        Debug.Log("RunManager: run complete!");
        RequestScene(runEndScene);
        ClearRun();
    }

    private void HandleStageCompleteContinue()
    {
        ReturnToMapAfterNode();
    }

    #endregion

    private void RequestScene(SceneReference scene)
    {
        if (sceneChangeRequestChannel == null)
        {
            Debug.LogError("RunManager: sceneChangeRequestChannel is null; cannot change scene.");
            return;
        }

        if (string.IsNullOrEmpty(scene.sceneName))
        {
            Debug.LogError("RunManager: target scene name is empty.");
            return;
        }

        sceneChangeRequestChannel.RaiseDataChanged(scene.sceneName);
    }
}
