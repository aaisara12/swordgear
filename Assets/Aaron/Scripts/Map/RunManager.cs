#nullable enable

using System;
using System.Collections.Generic;
using Shop;
using UnityEngine;

/// <summary>
/// Persistent owner of run-long state and the linear rail flow. Lives on CoreSystems.prefab so it
/// survives Map &lt;-&gt; Arena scene swaps. Generates the linear step queue at run start, tracks the
/// current step, resets run-long state (HP / ultimate / combo) on a new run, and routes step
/// completion to the Map interstitial or Arena scene.
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager? Instance { get; private set; }

    [Header("Map Generation")]
    [SerializeField] private MapGenerationSettings generationSettings = new MapGenerationSettings();
    [Obsolete("DEPRECATED: Branching map override; linear runs ignore this field.")]
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

    [Header("Encounter (M6)")]
    [Tooltip("Roster of all 20 enemy archetypes + elemental stat knobs. Required for spawn scaling.")]
    [SerializeField] private EnemyCatalog? enemyCatalog;
    [Tooltip("Threat budgets, theme/role weights, elite rules for WaveComposer (Commit 21).")]
    [SerializeField] private WaveComposerSettings? waveComposerSettings;

    private RunMap? _currentMap;
    private LinearRunState? _linearRun;
    private int _lastSeed;
    private int _lastMapStepIndex = -1;
    private bool _augmentNodeActive;

    [Obsolete("DEPRECATED: Branching map state; use Run instead.")]
    public RunMap? CurrentMap => _currentMap;
    public LinearRunState? Run => _linearRun;
    public RunStep? CurrentStep => _linearRun?.CurrentStep;
    public EnemyCatalog? EnemyCatalog => enemyCatalog;
    public WaveComposerSettings? WaveComposerSettings => waveComposerSettings;

    /// <summary>
    /// Optional one-shot tier override for the next augment offer (e.g. guaranteed Diamond at the upgrade hub).
    /// Consumed by <see cref="InGameAugmentsManager"/> when the offer is generated.
    /// </summary>
    public AugmentQualityTier? PendingAugmentTierOverride { get; private set; }

    public void SetAugmentTierOverride(AugmentQualityTier tier)
    {
        PendingAugmentTierOverride = tier;
    }

    public bool TryConsumeAugmentTierOverride(out AugmentQualityTier tier)
    {
        if (PendingAugmentTierOverride == null)
        {
            tier = default;
            return false;
        }

        tier = PendingAugmentTierOverride.Value;
        PendingAugmentTierOverride = null;
        return true;
    }

    [Obsolete("DEPRECATED: Branching map node; use CurrentStep instead.")]
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
        PrerollUpcomingEncounters();
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

    /// <summary>
    /// Layout for combat Arena loads. Prefers the current step's seeded pick (<see cref="RunStep.Layout"/>,
    /// chosen from <see cref="MapGenerationSettings.combatLayouts"/> by <see cref="LinearRunGenerator"/> — Commit 24)
    /// so different combats load different arenas deterministically. Falls back to
    /// <c>fallbackCombatLayout</c>, then the first pool entry.
    /// </summary>
    public ArenaLayoutTemplate? ResolveCombatLayout()
    {
        ArenaLayoutTemplate? stepLayout = _linearRun?.CurrentStep?.Layout;
        if (stepLayout != null)
        {
            return stepLayout;
        }

        if (fallbackCombatLayout != null)
        {
            return fallbackCombatLayout;
        }

        IReadOnlyList<ArenaLayoutTemplate> layouts = generationSettings.combatLayouts;
        return layouts.Count > 0 ? layouts[0] : null;
    }

    /// <summary>Layout for upgrade hub loads from <see cref="MapGenerationSettings.shopLayout"/>.</summary>
    public ArenaLayoutTemplate? ResolveShopLayout()
    {
        return generationSettings.shopLayout;
    }

    /// <summary>
    /// Deterministic 0..3 quarter-turn rotation for a combat step (Commit 25), decorrelated from the encounter
    /// seed so orientation and composition vary independently. Same run seed + step → same orientation.
    /// </summary>
    public static int SeededRotationSteps(int runSeed, int stepIndex)
    {
        unchecked
        {
            int seed = (((runSeed * 397) ^ stepIndex) * 31) + 0x51ED2703;
            return new System.Random(seed).Next(0, 4);
        }
    }

    /// <summary>Builds the arena blueprint for the current linear step (combat or upgrade hub).</summary>
    public LevelBlueprint? BuildBlueprintForCurrentStep()
    {
        RunStep? step = _linearRun?.CurrentStep;
        if (step == null)
        {
            Debug.LogError("RunManager: BuildBlueprintForCurrentStep called with no current step.");
            return null;
        }

        if (step.Type == RunStepType.Combat)
        {
            ArenaLayoutTemplate? layout = ResolveCombatLayout();
            if (layout == null || layout.LevelPrefab == null)
            {
                Debug.LogError("RunManager: no combat arena layout assigned.");
                return null;
            }

            CombatEncounter? encounter = BuildCombatEncounter();
            if (encounter == null || encounter.WaveCount == 0)
            {
                Debug.LogError("RunManager: failed to compose combat encounter (catalog/settings missing or empty).");
                return null;
            }

            return new LevelBlueprint
            {
                Layout = layout,
                Encounter = encounter,
                Waves = new List<EnemyWaveConfig>(),
                IsShopLevel = false,
                RotationSteps = SeededRotationSteps(_linearRun!.Seed, step.StepIndex),
            };
        }

        if (step.Type == RunStepType.Upgrade)
        {
            ArenaLayoutTemplate? layout = ResolveShopLayout();
            if (layout == null || layout.LevelPrefab == null)
            {
                Debug.LogError("RunManager: no shop arena layout assigned.");
                return null;
            }

            return new LevelBlueprint
            {
                Layout = layout,
                Encounter = null!,
                Waves = new List<EnemyWaveConfig>(),
                IsShopLevel = true
            };
        }

        Debug.LogError($"RunManager: unsupported step type {step.Type}.");
        return null;
    }

    /// <summary>
    /// Returns the current combat step's encounter, reusing the pre-rolled one cached on the step when present
    /// (Commit 22) and composing + caching on demand otherwise. Deterministic either way.
    /// </summary>
    public CombatEncounter? BuildCombatEncounter()
    {
        if (_linearRun == null || _linearRun.CurrentStep == null)
        {
            Debug.LogError("RunManager: BuildCombatEncounter called with no current combat step.");
            return null;
        }

        RunStep step = _linearRun.CurrentStep;
        step.Encounter ??= ComposeEncounterForStep(step);
        return step.Encounter;
    }

    /// <summary>
    /// Composes the deterministic encounter for a single combat step. Returns null for non-combat steps or
    /// when the catalog / settings are missing.
    /// </summary>
    private CombatEncounter? ComposeEncounterForStep(RunStep step)
    {
        if (enemyCatalog == null)
        {
            Debug.LogError("RunManager: enemyCatalog is not assigned.");
            return null;
        }

        if (waveComposerSettings == null)
        {
            Debug.LogError("RunManager: waveComposerSettings is not assigned.");
            return null;
        }

        if (_linearRun == null || !EncounterContext.TryFrom(_linearRun, step, out EncounterContext context))
        {
            return null;
        }

        return EncounterBuilder.Build(context, enemyCatalog, waveComposerSettings);
    }

    /// <summary>
    /// Pre-rolls (composes + caches) the encounter for every queued combat step that lacks one (Commit 22).
    /// Runs at run start and whenever a block is appended, so the upgrade-hub preview can read the next
    /// combats and each fight reuses the exact composition without re-rolling. A no-op once all are composed.
    /// </summary>
    public void PrerollUpcomingEncounters()
    {
        if (_linearRun == null || enemyCatalog == null || waveComposerSettings == null)
        {
            return;
        }

        foreach (RunStep step in _linearRun.Steps)
        {
            if (step.Type == RunStepType.Combat && step.Encounter == null)
            {
                step.Encounter = ComposeEncounterForStep(step);
            }
        }
    }

    /// <summary>
    /// DEPRECATED: legacy ScriptableObject wave pool. Prefer <see cref="BuildCombatEncounter"/>.
    /// </summary>
    [Obsolete("Use BuildCombatEncounter / WaveComposer instead of the combatWaves pool.")]
    public List<EnemyWaveConfig> BuildCombatWaves()
    {
        List<EnemyWaveConfig> pool = generationSettings.combatWaves;
        if (pool == null || pool.Count == 0)
        {
            return new List<EnemyWaveConfig>();
        }

        int seed = _linearRun?.Seed ?? _lastSeed;
        int stepIndex = _linearRun?.CurrentStep?.StepIndex ?? 0;
        // Per-combat determinism: same runSeed + globalStepIndex → same wave picks.
        unchecked
        {
            seed = (seed * 397) ^ stepIndex;
        }

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

    /// <summary>Loads Arena for combat or upgrade hub steps.</summary>
    public void BeginCurrentStep()
    {
        RunStep? step = _linearRun?.CurrentStep;
        if (step == null)
        {
            Debug.LogError("RunManager: BeginCurrentStep called with no current step.");
            return;
        }

        if (step.Type == RunStepType.Combat || step.Type == RunStepType.Upgrade)
        {
            RequestScene(arenaScene);
            return;
        }

        Debug.LogError($"RunManager: unsupported step type {step.Type}.");
    }

    /// <summary>
    /// Called when the combat exit portal is entered. Advances the linear step and returns to the map
    /// interstitial so the token can animate onto the next node before loading the next scene.
    /// </summary>
    public void HandleCombatPortalExited()
    {
        if (_linearRun == null)
        {
            Debug.LogError("RunManager: HandleCombatPortalExited called with no active linear run.");
            return;
        }

        bool advanced = _linearRun.TryAdvanceToNextStep();
        Time.timeScale = 1f;

        if (!advanced)
        {
            Debug.Log("RunManager: combat portal exited on final step; run complete.");
            RequestScene(runEndScene);
            ClearRun();
            return;
        }

        EnsureMoreStepsQueued();
        OnRunChanged?.Invoke();
        RequestScene(mapScene);
    }

    /// <summary>
    /// Called when the player leaves the upgrade hub. Advances past the upgrade step and returns to the map
    /// interstitial so the token can animate onto the next combat before loading the arena.
    /// </summary>
    public void HandleUpgradeComplete()
    {
        if (_linearRun == null)
        {
            Debug.LogError("RunManager: HandleUpgradeComplete called with no active linear run.");
            return;
        }

        RunStep? current = _linearRun.CurrentStep;
        if (current == null || current.Type != RunStepType.Upgrade)
        {
            Debug.LogWarning("RunManager: HandleUpgradeComplete called outside an upgrade step.");
            return;
        }

        bool advanced = _linearRun.TryAdvanceToNextStep();
        Time.timeScale = 1f;

        if (!advanced)
        {
            Debug.Log("RunManager: upgrade completed on final step; run complete.");
            RequestScene(runEndScene);
            ClearRun();
            return;
        }

        EnsureMoreStepsQueued();
        OnRunChanged?.Invoke();
        RequestScene(mapScene);
    }

    /// <summary>
    /// When the player reaches the trailing Upgrade step, appends the next Combat×3 + Upgrade block
    /// so the rail can show upcoming combats before the hub is implemented (commit 11+).
    /// </summary>
    public bool EnsureMoreStepsQueued()
    {
        if (_linearRun == null)
        {
            return false;
        }

        RunStep? current = _linearRun.CurrentStep;
        if (current == null || current.Type != RunStepType.Upgrade)
        {
            return false;
        }

        int currentIndex = _linearRun.CurrentStepIndex;
        int tailIndex = _linearRun.Steps.Count - 1;
        if (currentIndex != tailIndex)
        {
            return false;
        }

        IReadOnlyList<ArenaLayoutTemplate> layouts = generationSettings.combatLayouts;
        if (layouts == null || layouts.Count == 0)
        {
            Debug.LogError("RunManager: cannot queue next block — combatLayouts is empty.");
            return false;
        }

        int nextBlockIndex = _linearRun.QueuedBlockCount;
        int startStepIndex = _linearRun.Steps.Count;
        List<RunStep> nextBlock = LinearRunGenerator.GenerateNextBlock(
            layouts,
            _linearRun.Seed,
            nextBlockIndex,
            startStepIndex);

        if (nextBlock.Count == 0)
        {
            return false;
        }

        _linearRun.AppendSteps(nextBlock);
        PrerollUpcomingEncounters();
        Debug.Log(
            $"RunManager: queued linear block {nextBlockIndex + 1} " +
            $"({LinearRunGenerator.CombatsPerBlock} combats + upgrade).");
        return true;
    }

    #endregion

    #region DEPRECATED — branching map node selection / flow

    /// <summary>
    /// DEPRECATED — branching map node picker. Linear runs advance via portal / hub exit instead.
    /// </summary>
    [Obsolete("DEPRECATED: Branching map node selection; linear runs use HandleCombatPortalExited / HandleUpgradeComplete.")]
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

    /// <summary>Opens the in-game augment UI with the normal tier % roll (post-combat rewards).</summary>
    public void OfferStandardAugmentPick()
    {
        PendingAugmentTierOverride = null;
        if (showAugmentChannel == null)
        {
            Debug.LogError("RunManager: showAugmentChannel is null; cannot open augment overlay.");
            return;
        }

        showAugmentChannel.RaiseEventTriggered();
    }

    /// <summary>Opens the in-game augment UI forced to Diamond (upgrade hub).</summary>
    public void OfferDiamondAugmentPick()
    {
        PendingAugmentTierOverride = AugmentQualityTier.Elite;
        if (showAugmentChannel == null)
        {
            Debug.LogError("RunManager: showAugmentChannel is null; cannot open augment overlay.");
            return;
        }

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

    #region DEPRECATED — branching map node completion

    /// <summary>
    /// DEPRECATED — completes a branching map Arena node and returns to the map picker.
    /// </summary>
    [Obsolete("DEPRECATED: Branching map completion; linear runs use HandleCombatPortalExited / HandleUpgradeComplete.")]
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

    /// <summary>DEPRECATED — branching map Augment overlay completion.</summary>
    [Obsolete("DEPRECATED: Branching map Augment overlay.")]
    public void HandleAugmentChosen()
    {
        augmentVisibilityChannel?.RaiseDataChanged(false);
        ComboSystem.Instance?.ResetPointsSinceLastAugment();
        CompleteOverlayNode();
    }

    /// <summary>DEPRECATED — branching map Rest overlay confirmation.</summary>
    [Obsolete("DEPRECATED: Branching map Rest overlay.")]
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

    /// <summary>DEPRECATED — branching map boss defeated.</summary>
    [Obsolete("DEPRECATED: Branching map boss completion.")]
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
