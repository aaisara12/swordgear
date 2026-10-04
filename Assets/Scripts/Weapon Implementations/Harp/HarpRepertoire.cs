#nullable enable

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Which tunes the player's harp knows this run: the starters, plus every tune learned since.
/// </summary>
/// <remarks>
/// Ownership is read from the player blob's inventory, where each learned tune is an item
/// (<see cref="LearnedItemId"/>), rather than kept here. That makes it last exactly as long as the run
/// without any save path of its own: it survives node changes with the rest of the inventory and is wiped
/// with it by <c>PlayerStatModifiers.ClearForNewRun</c>.
/// <para>
/// A run starts with one tune per family, so the very first tap is already even thirds.
/// </para>
/// </remarks>
public class HarpRepertoire : InitializeableUnrestrictedGameComponent
{
    public static HarpRepertoire? Instance { get; private set; }

    /// <summary> Inventory id prefix for a learned tune. Matched by nothing else that reads the inventory. </summary>
    public const string LearnedItemPrefix = "light-tune-";

    [Tooltip("Known from the start of every run. Keep one per family so the roll is even from the first tap.")]
    [SerializeField] private List<HarpTune?> starters = new();
    [Tooltip("Everything else the harp can learn during a run.")]
    [SerializeField] private List<HarpTune?> learnable = new();

    private PlayerBlob? playerBlob;
    private readonly List<HarpTune?> owned = new();

    public static string LearnedItemId(HarpTune tune) => LearnedItemPrefix + tune.name;

    /// <summary> Every tune the harp can play right now. </summary>
    public IReadOnlyList<HarpTune?> Owned
    {
        get
        {
            owned.Clear();
            foreach (HarpTune? tune in starters)
            {
                if (tune != null)
                {
                    owned.Add(tune);
                }
            }

            foreach (HarpTune? tune in learnable)
            {
                if (tune != null && Knows(tune))
                {
                    owned.Add(tune);
                }
            }

            return owned;
        }
    }

    public IReadOnlyList<HarpTune?> Learnable => learnable;

    /// <summary> True while some learnable tune is still unknown. </summary>
    public bool HasUnlearned
    {
        get
        {
            foreach (HarpTune? tune in learnable)
            {
                if (tune != null && !Knows(tune))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void InitializeOnGameStart_Dangerous(PlayerBlob blob)
    {
        playerBlob = blob;
    }

    public bool Knows(HarpTune tune) =>
        starters.Contains(tune) || (playerBlob != null && playerBlob.GetItemCount(LearnedItemId(tune)) > 0);
}
