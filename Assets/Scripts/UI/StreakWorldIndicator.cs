#nullable enable

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the player's hot streaks in the world: an icon per streak over their head with a chevron per
/// stack, a callout for every stack gained, and a loud one when a hit busts the streak.
/// </summary>
/// <remarks>
/// Icons say "what is buffed and how much" at a glance (a sword with two chevrons is damage up, twice);
/// the callout carries the exact numbers at the moment you earn them. The look is authored in the prefab:
/// each icon's pop and burst, the chevrons' climb and the metronome's swing are AnimationClips. This
/// decides which icons and chevrons show, where they sit, and how fast a tempo icon ticks.
/// <para>
/// It follows the player's position rather than being parented to them, because the player transform
/// turns to face targets and icons over their head would spin with it.
/// </para>
/// </remarks>
public class StreakWorldIndicator : MonoBehaviour
{
    [System.Serializable]
    private class Icon
    {
        [Tooltip("The streak this icon shows. Matches the StreakTune's streakId.")]
        public string streakId = "";
        public GameObject? root;
        [Tooltip("Plays 'Pop' (its default state, so also on appearing) and 'Bust'.")]
        public Animator? animator;
        [Tooltip("One marker per stack, bottom to top.")]
        public GameObject[] stackMarks = System.Array.Empty<GameObject>();
        [Tooltip("Optional. An animator whose speed rises with stacks, e.g. the metronome's swing.")]
        public Animator? tempo;
        [Tooltip("How much faster the tempo animator runs per stack beyond the first.")]
        public float tempoPerStack = 0.4f;
    }

    [SerializeField] private Icon[] icons = System.Array.Empty<Icon>();
    [Tooltip("Height of the icons' centre above the player's centre, in the Bob node's (scaled) space, clear of their head.")]
    [SerializeField] private float iconHeight = 1.3f;
    [Tooltip("Horizontal distance between icons when several streaks are live.")]
    [SerializeField] private float iconSpacing = 1.25f;
    [Tooltip("Seconds the Bust clip needs before a busted icon is hidden. Match the clip's length.")]
    [SerializeField] private float bustSeconds = 0.35f;

    [Header("Callouts")]
    [Tooltip("World text popped above the player. Must have a TMP_Text child and release itself when done.")]
    [SerializeField] private GameObject? calloutPrefab;
    [SerializeField] private Vector2 calloutOffset = new(0f, 4.9f);
    [SerializeField] private float calloutSeconds = 1.1f;
    [Tooltip("Height between callouts that are on screen at the same time, so quick pulls stack rather " +
             "than print over each other. Must clear the callout clip's rise (0.7) plus a line of text.")]
    [SerializeField] private float calloutLaneSpacing = 1.4f;
    [SerializeField] private Color gainColor = new(0.95f, 0.86f, 1f, 1f);
    [SerializeField] private string bustText = "STREAK BROKEN";
    [SerializeField] private Color bustColor = new(1f, 0.38f, 0.38f, 1f);

    [Header("Bust Sound")]
    [Tooltip("Plucked together on a bust: a semitone cluster down low reads as a string going sour.")]
    [SerializeField] private int[] bustSemitones = { -13, -12 };
    [SerializeField, Range(0f, 1f)] private float bustVolume = 0.7f;

    /// <summary> The indicator over the player, for anything else that needs a callout there. </summary>
    public static StreakWorldIndicator? Instance { get; private set; }

    private static readonly int PopState = Animator.StringToHash("Pop");
    private static readonly int BustState = Animator.StringToHash("Bust");

    private int lastBustFrame = -1;
    private Coroutine? bustRoutine;
    private readonly List<float> calloutSpawnTimes = new();
    private readonly List<Icon> visible = new();

    private void Awake()
    {
        Instance = this;

        if (calloutPrefab == null)
        {
            Debug.LogError("StreakWorldIndicator: calloutPrefab is null");
            return;
        }
    }

    private void OnEnable()
    {
        PlayerStatModifiers.OnStreakChanged += HandleStreakChanged;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerStatModifiers.OnStreakChanged -= HandleStreakChanged;
        bustRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary> Pops a callout over the player, in the same lanes as the streak callouts. </summary>
    public void Announce(string text, Color color) => Callout(text, color);

    private void LateUpdate()
    {
        GameManager? gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.player != null)
        {
            transform.position = gameManager.player.transform.position;
        }
    }

    private void HandleStreakChanged(StreakChange change, string streakId)
    {
        switch (change)
        {
            case StreakChange.Gained:
                CancelBust();
                Refresh();

                StreakState? streak = PlayerStatModifiers.Instance?.GetStreak(streakId);
                if (streak != null)
                {
                    // Per stack, not the running total: the callout says what this pull just won you; the
                    // icon's chevrons carry the total.
                    Callout(StreakText.Describe(streak.Bonuses, 1), gainColor);
                }

                Icon? icon = FindIcon(streakId);
                if (icon?.animator != null && icon.animator.isActiveAndEnabled)
                {
                    icon.animator.Play(PopState, 0, 0f);
                }

                break;

            case StreakChange.Busted:
                // A bust ends every streak at once, one event each; only react to the first.
                if (lastBustFrame == Time.frameCount)
                {
                    break;
                }

                lastBustFrame = Time.frameCount;
                Callout(bustText, bustColor);
                foreach (int semitones in bustSemitones)
                {
                    AudioSystem.Play(AudioSystem.Sound.Harp_Pluck, bustVolume, Mathf.Pow(2f, semitones / 12f));
                }

                // Burst what was showing, then hide it. Not refreshed first: that would hide the icons
                // before the burst could play.
                foreach (Icon shown in icons)
                {
                    if (shown.root != null && shown.root.activeSelf && shown.animator != null)
                    {
                        shown.animator.Play(BustState, 0, 0f);
                    }
                }

                CancelBust();
                bustRoutine = StartCoroutine(RefreshAfterBust());
                break;

            default:
                Refresh();
                break;
        }
    }

    private IEnumerator RefreshAfterBust()
    {
        // Realtime: the bust arrives with a hit, and the hit's hit-stop slows scaled time.
        yield return new WaitForSecondsRealtime(bustSeconds);
        bustRoutine = null;
        Refresh();
    }

    private void CancelBust()
    {
        if (bustRoutine != null)
        {
            StopCoroutine(bustRoutine);
            bustRoutine = null;
        }
    }

    /// <summary> Shows an icon per live streak with a chevron per stack, packed side by side over the head. </summary>
    private void Refresh()
    {
        PlayerStatModifiers? modifiers = PlayerStatModifiers.Instance;
        visible.Clear();

        foreach (Icon icon in icons)
        {
            if (icon.root == null)
            {
                continue;
            }

            StreakState? streak = modifiers?.GetStreak(icon.streakId);
            if (streak == null)
            {
                if (icon.root.activeSelf)
                {
                    icon.root.SetActive(false);
                }

                continue;
            }

            if (!icon.root.activeSelf)
            {
                icon.root.SetActive(true);
            }

            for (int i = 0; i < icon.stackMarks.Length; i++)
            {
                if (icon.stackMarks[i] != null)
                {
                    icon.stackMarks[i].SetActive(i < streak.Stacks);
                }
            }

            if (icon.tempo != null)
            {
                icon.tempo.speed = 1f + icon.tempoPerStack * (streak.Stacks - 1);
            }

            visible.Add(icon);
        }

        // Centre the row of live icons over the head, so a lone streak sits right on top.
        for (int i = 0; i < visible.Count; i++)
        {
            float x = (i - (visible.Count - 1) * 0.5f) * iconSpacing;
            visible[i].root!.transform.localPosition = new Vector3(x, iconHeight, 0f);
        }
    }

    private Icon? FindIcon(string streakId)
    {
        foreach (Icon icon in icons)
        {
            if (icon.streakId == streakId)
            {
                return icon;
            }
        }

        return null;
    }

    private void Callout(string text, Color color)
    {
        if (calloutPrefab == null || PrefabPool.Instance == null)
        {
            return;
        }

        // One lane per callout still on screen: a second pull inside the first callout's lifetime lands
        // above it instead of on top of it.
        calloutSpawnTimes.RemoveAll(t => Time.time - t >= calloutSeconds);
        int lane = calloutSpawnTimes.Count;
        calloutSpawnTimes.Add(Time.time);

        Vector3 position = transform.position + (Vector3)calloutOffset + Vector3.up * (lane * calloutLaneSpacing);
        GameObject callout = PrefabPool.Instance.Spawn(calloutPrefab, position, Quaternion.identity);

        TMP_Text? label = callout.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = text;
            label.color = color;
        }

        callout.GetComponent<PooledInstance>()?.ReleaseAfter(calloutSeconds);
    }
}
