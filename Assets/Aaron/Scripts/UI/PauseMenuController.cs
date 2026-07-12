#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Persistent pause overlay: on-screen button + Escape, master volume, quit to title.
/// Lives in the additive PauseMenu scene.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController? Instance { get; private set; }

    public static bool IsPaused => Instance != null && Instance._isPaused;

    private const string MapSceneName = "Map";
    private const string ArenaSceneName = "Arena";
    private const string TitleSceneName = "TitleScene";

    [Header("UI")]
    [SerializeField] private GameObject? pauseButtonRoot;
    [SerializeField] private GameObject? pausePanelRoot;
    [SerializeField] private Button? pauseButton;
    [SerializeField] private Button? resumeButton;
    [SerializeField] private Button? returnToTitleButton;
    [SerializeField] private Slider? masterVolumeSlider;

    [Header("Channels")]
    [SerializeField] private BoolEventChannelSO? loadingScreenChannel;
    [SerializeField] private BoolEventChannelSO? defeatVisibilityChannel;
    [SerializeField] private BoolEventChannelSO? augmentShopVisibilityChannel;
    [SerializeField] private BoolEventChannelSO? stageCompleteVisibilityChannel;
    [SerializeField] private BoolEventChannelSO? restVisibilityChannel;
    [SerializeField] private StringEventChannelSO? sceneChangeRequestChannel;

    [Header("Audio")]
    [SerializeField] private UnityEngine.Audio.AudioMixer? audioMixer;

    [Header("Deps (optional — resolved at runtime if null)")]
    [SerializeField] private SceneTransitioner? sceneTransitioner;
    [SerializeField] private PlayerGameplayInputManager? inputManager;

    private bool _isPaused;
    private bool _seeded;
    private bool _isLoading;
    private bool _isDefeatVisible;
    private float _savedTimeScale = 1f;
    private string? _currentMainScene;
    private bool _suppressVolumeCallback;
    private Coroutine? _refreshButtonRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pausePanelRoot == null)
        {
            Debug.LogError("PauseMenuController: pausePanelRoot is null");
            return;
        }

        if (pauseButtonRoot == null)
        {
            Debug.LogError("PauseMenuController: pauseButtonRoot is null");
            return;
        }

        if (sceneTransitioner == null)
        {
            sceneTransitioner = FindFirstObjectByType<SceneTransitioner>();
        }

        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<PlayerGameplayInputManager>();
        }

        pausePanelRoot.SetActive(false);
        pauseButtonRoot.SetActive(false);

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(ReturnToTitle);
        }

        if (masterVolumeSlider != null)
        {
            _suppressVolumeCallback = true;
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = MasterVolumeSettings.GetLinear();
            _suppressVolumeCallback = false;
            masterVolumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        if (loadingScreenChannel != null)
        {
            loadingScreenChannel.OnDataChanged += HandleLoadingChanged;
        }

        if (defeatVisibilityChannel != null)
        {
            defeatVisibilityChannel.OnDataChanged += HandleDefeatVisibilityChanged;
        }

        if (sceneTransitioner != null)
        {
            sceneTransitioner.OnSceneTransitionFinished.AddListener(HandleSceneTransitionFinished);
            if (!string.IsNullOrEmpty(sceneTransitioner.CurrentMainScene))
            {
                SeedFromScene(sceneTransitioner.CurrentMainScene);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.RemoveListener(ReturnToTitle);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }

        if (loadingScreenChannel != null)
        {
            loadingScreenChannel.OnDataChanged -= HandleLoadingChanged;
        }

        if (defeatVisibilityChannel != null)
        {
            defeatVisibilityChannel.OnDataChanged -= HandleDefeatVisibilityChanged;
        }

        if (sceneTransitioner != null)
        {
            sceneTransitioner.OnSceneTransitionFinished.RemoveListener(HandleSceneTransitionFinished);
        }
    }

    private void Update()
    {
        Keyboard? keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (_isPaused)
        {
            if (CanPause)
            {
                Resume();
            }
            else
            {
                ForceClose();
            }

            return;
        }

        if (CanPause)
        {
            OpenPause();
        }
    }

    public bool CanPause
    {
        get
        {
            if (!_seeded || _isLoading || _isDefeatVisible)
            {
                return false;
            }

            if (sceneTransitioner != null && sceneTransitioner.IsTransitioning)
            {
                return false;
            }

            if (PlayerGameplayManager.Instance != null && PlayerGameplayManager.Instance.IsDefeated)
            {
                return false;
            }

            return _currentMainScene == MapSceneName || _currentMainScene == ArenaSceneName;
        }
    }

    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
            return;
        }

        if (CanPause)
        {
            OpenPause();
        }
    }

    public void OpenPause()
    {
        if (_isPaused || !CanPause)
        {
            return;
        }

        _isPaused = true;
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (pausePanelRoot != null)
        {
            pausePanelRoot.SetActive(true);
        }

        inputManager?.DisableGameplayInput();
        RefreshButtonVisibility();
    }

    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        Time.timeScale = _savedTimeScale;

        if (pausePanelRoot != null)
        {
            pausePanelRoot.SetActive(false);
        }

        TryReenableGameplayInput();
        RefreshButtonVisibility();
    }

    public void ReturnToTitle()
    {
        DismissPersistentOverlays();

        // Clear run before restoring time so Map interstitial cannot advance a cleared run.
        RunManager.Instance?.ClearRun();

        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanelRoot != null)
        {
            pausePanelRoot.SetActive(false);
        }

        RefreshButtonVisibility();

        if (sceneChangeRequestChannel == null)
        {
            Debug.LogError("PauseMenuController: sceneChangeRequestChannel is null; cannot return to title.");
            return;
        }

        sceneChangeRequestChannel.RaiseDataChanged(TitleSceneName);
    }

    private void ForceClose()
    {
        if (!_isPaused)
        {
            RefreshButtonVisibility();
            return;
        }

        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanelRoot != null)
        {
            pausePanelRoot.SetActive(false);
        }

        // Do not re-enable gameplay input on forced close (e.g. defeat).
        RefreshButtonVisibility();
    }

    private void DismissPersistentOverlays()
    {
        augmentShopVisibilityChannel?.RaiseDataChanged(false);
        stageCompleteVisibilityChannel?.RaiseDataChanged(false);
        restVisibilityChannel?.RaiseDataChanged(false);
    }

    private void HandleVolumeChanged(float value)
    {
        if (_suppressVolumeCallback)
        {
            return;
        }

        MasterVolumeSettings.SetLinear(value, audioMixer);
    }

    private void HandleLoadingChanged(bool isLoading)
    {
        _isLoading = isLoading;
        if (_isPaused && !CanPause)
        {
            ForceClose();
            return;
        }

        RefreshButtonVisibility();
    }

    private void HandleDefeatVisibilityChanged(bool isVisible)
    {
        _isDefeatVisible = isVisible;
        if (_isPaused && !CanPause)
        {
            ForceClose();
            return;
        }

        RefreshButtonVisibility();
    }

    private void HandleSceneTransitionFinished(string sceneName)
    {
        SeedFromScene(sceneName);
        if (_isPaused && !CanPause)
        {
            ForceClose();
            return;
        }

        // Finished fires before IsTransitioning clears; refresh next frame so the button can show.
        if (_refreshButtonRoutine != null)
        {
            StopCoroutine(_refreshButtonRoutine);
        }

        _refreshButtonRoutine = StartCoroutine(RefreshButtonAfterTransitionSettles());
    }

    private IEnumerator RefreshButtonAfterTransitionSettles()
    {
        yield return null;
        while (sceneTransitioner != null && sceneTransitioner.IsTransitioning)
        {
            yield return null;
        }

        RefreshButtonVisibility();
        _refreshButtonRoutine = null;
    }

    private void SeedFromScene(string sceneName)
    {
        _currentMainScene = sceneName;
        _seeded = true;
        // _isLoading is owned solely by the loading-screen channel. Do not copy IsTransitioning
        // here — OnSceneTransitionFinished runs while the transition task is still completing.
    }

    private void RefreshButtonVisibility()
    {
        if (pauseButtonRoot == null)
        {
            return;
        }

        pauseButtonRoot.SetActive(CanPause && !_isPaused);
    }

    private void TryReenableGameplayInput()
    {
        if (inputManager == null)
        {
            return;
        }

        // Only re-enable when a pawn is still linked (Arena). Map has no pawn.
        if (PlayerGameplayManager.Instance != null
            && PlayerGameplayManager.Instance.IsDefeated)
        {
            return;
        }

        inputManager.EnableGameplayInput();
    }
}
