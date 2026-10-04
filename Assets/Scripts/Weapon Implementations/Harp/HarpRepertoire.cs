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

    /// <summary> Raised when the harp learns a tune, so it can be announced. </summary>
    public static event System.Action<HarpTune>? OnTuneLearned;

    /// <summary> Inventory id prefix for a learned tune. Matched by nothing else that reads the inventory. </summary>
    public const string LearnedItemPrefix = "light-tune-";

    [Tooltip("The repeatable upgrade whose every copy teaches one random unknown tune.")]
    [SerializeField] private UpgradeType learnUpgrade = UpgradeType.Light_LearnTune;

    [Tooltip("Known from the start of every run. Keep one per family so the roll is even from the first tap.")]
    [SerializeField] private List<HarpTune?> starters = new();
    [Tooltip("Everything else the harp can learn during a run.")]
    [SerializeField] private List<HarpTune?> learnable = new();

    private PlayerBlob? playerBlob;
    private readonly List<HarpTune?> owned = new();
    private bool syncing;

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
        if (playerBlob != null)
        {
            playerBlob.InventoryItems.DictionaryChanged -= HandleInventoryChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void InitializeOnGameStart_Dangerous(PlayerBlob blob)
    {
        if (playerBlob != null)
        {
            playerBlob.InventoryItems.DictionaryChanged -= HandleInventoryChanged;
        }

        playerBlob = blob;
        playerBlob.InventoryItems.DictionaryChanged += HandleInventoryChanged;
        SyncLearned();
    }

    public bool Knows(HarpTune tune) =>
        starters.Contains(tune) || (playerBlob != null && playerBlob.GetItemCount(LearnedItemId(tune)) > 0);

    private void HandleInventoryChanged(ObservableDictionaryChangedEventArgs<string, int> _) => SyncLearned();

    /// <summary>
    /// Learns one random unknown tune for every "Learn a tune" the player holds that hasn't paid out yet.
    /// </summary>
    /// <remarks>
    /// Counting rather than reacting to "an item was added" keeps this idempotent: it can run on every
    /// inventory change, at game start, or twice in a row, and the number of learned tunes only ever
    /// catches up with the number of purchases. Learning adds an item, which changes the inventory and
    /// calls back in here, so the nested call is skipped and the loop re-counts instead.
    /// </remarks>
    private void SyncLearned()
    {
        if (playerBlob == null || syncing)
        {
            return;
        }

        syncing = true;
        try
        {
            while (CountLearned() < CountLearnPurchases())
            {
                HarpTune? tune = RandomUnlearned();
                if (tune == null)
                {
                    // Bought more than there is to learn: nothing left to give.
                    break;
                }

                playerBlob.ReceiveItem(LearnedItemId(tune), 1);
                OnTuneLearned?.Invoke(tune);
            }
        }
        finally
        {
            syncing = false;
        }
    }

    private int CountLearnPurchases()
    {
        int count = 0;
        foreach (KeyValuePair<string, int> item in playerBlob!.InventoryItems)
        {
            // The id carries an element tag in front of the upgrade; the upgrade deserializer looks past it.
            if (item.Value > 0 && UpgradeTypeSerializer.TryDeserialize(item.Key, out UpgradeType upgrade) && upgrade == learnUpgrade)
            {
                count += item.Value;
            }
        }

        return count;
    }

    private int CountLearned()
    {
        int count = 0;
        foreach (HarpTune? tune in learnable)
        {
            if (tune != null && Knows(tune))
            {
                count++;
            }
        }

        return count;
    }

    private HarpTune? RandomUnlearned()
    {
        var unlearned = new List<HarpTune>();
        foreach (HarpTune? tune in learnable)
        {
            if (tune != null && !Knows(tune))
            {
                unlearned.Add(tune);
            }
        }

        return unlearned.Count > 0 ? unlearned[Random.Range(0, unlearned.Count)] : null;
    }
}
