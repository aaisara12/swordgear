using UnityEngine;
using UnityEngine.UI; // Required for Button/Text if you use UI elements
using System.Collections.Generic;
using TMPro;

public class TestGameManager : MonoBehaviour
{

    // --- Public Fields for Testing & Setup ---

    [Header("UI References (For Preview/Start)")]
    // Assign a Text component in the Inspector to show the generated levels
    public Text previewText;

    // Assign a Button component to trigger the start of the round
    public Button startRoundButton;

    [Header("Game State")]
    private List<LevelBlueprint> currentRoundBlueprints;
    private int currentLevelInRound = 0;

    void Start()
    {
        // 1. Immediately Generate the Round and Show Previews
        GenerateAndShowPreviews();

        // 2. Set up the button click listener
        if (startRoundButton != null)
        {
            startRoundButton.onClick.AddListener(OnStartRoundClicked);
            startRoundButton.interactable = true;
            startRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "START ROUND";
        }

        LevelLoader.Instance.OnLevelClear += HandleLevelClear;
    }

    // --- Step 1: Generate and Preview ---
    private void GenerateAndShowPreviews()
    {
        // Get the generated blueprints from the RoundGenerator singleton
        currentRoundBlueprints = RoundGenerator.Instance.GenerateNewRound();

        string preview = "--- ROUND GENERATED ---\n\n";

        // Display the details of the 3 generated levels
        for (int i = 0; i < currentRoundBlueprints.Count; i++)
        {
            LevelBlueprint bp = currentRoundBlueprints[i];

            // Get a basic description of the wave sequence
            string waveSummary = $"({bp.Waves.Count} Waves: ";
            foreach (var group in bp.Waves)
            {
                // Just display the count of the first enemy type in the group for brevity
                waveSummary += $"{group.Enemies[0].Count}x {group.Enemies[0].EnemyPrefab.name}, ";
            }
            waveSummary = waveSummary.TrimEnd(' ', ',') + ")";

            preview += $"Level {i + 1}:\n";
            preview += $"  Layout: {bp.Layout.name}\n";
            preview += $"  Enemies: {waveSummary}\n";
            preview += $"  Exit: {bp.Transition.name}\n\n";
        }

        // Update the UI Text component
        if (previewText != null)
        {
            previewText.text = preview;
        }

        Debug.Log("Round generated. Ready to start.");
    }

    // --- Step 2: Begin Gameplay Loop ---
    public void OnStartRoundClicked()
    {
        // Disable the button immediately to prevent double-clicking
        if (startRoundButton != null)
        {
            startRoundButton.interactable = false;
        }

        if (currentLevelInRound == 0)
        {
            // ACTION 1: Starting the round (Level 1)
            Debug.Log("Starting Round: Loading Level 1.");
        }
        else
        {
            // ACTION 2: Proceeding to Level 2 or 3
            Debug.Log($"User pressed PROCEED. Loading Level {currentLevelInRound + 1}.");
        }

        // The index is already incremented in HandleLevelClear() for transitions,
        // or is 0 for the initial start, so we just execute the load.
        LoadNextLevel();
    }

    // --- Step 3: Level Loading and Management ---
    private void LoadNextLevel()
    {
        if (currentLevelInRound >= currentRoundBlueprints.Count)
        {
            Debug.LogError("Tried to load level out of bounds! Should have ended the round.");
            return;
        }

        LevelBlueprint nextBlueprint = currentRoundBlueprints[currentLevelInRound];
        Debug.Log("loading next level");
        LevelLoader.Instance.LoadLevel(nextBlueprint);
    }

    // --- Step 4: Level Completion Handler (Triggers next step) ---
    // --- Level Progression Handler (Triggers next step) ---
    private void HandleLevelClear()
    {
        // The previous level is officially cleared.
        currentLevelInRound++;

        if (currentLevelInRound < currentRoundBlueprints.Count)
        {
            // Levels 1 or 2 cleared: STOP and enable the button for manual transition
            Debug.Log($"Level {currentLevelInRound} Cleared! Waiting for user to proceed.");

            // Re-enable the button and change its text
            if (startRoundButton != null)
            {
                startRoundButton.interactable = true;
                startRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = $"PROCEED TO LEVEL {currentLevelInRound + 1}";
            }

            // Remove the old arena from the scene here if necessary!
        }
        else
        {
            // Level 3 is done: End the round
            Debug.Log("Round finished! Returning to shop.");
            TransitionToShop();
        }
    }

    private void TransitionToShop()
    {
        // Unsubscribe cleanup
        LevelLoader.Instance.OnLevelClear -= HandleLevelClear;

        if (startRoundButton != null)
        {
            startRoundButton.interactable = true;
            startRoundButton.GetComponentInChildren<TextMeshProUGUI>().text = "START NEW ROUND";
            previewText.text = "ROUND FINISHED. Press START NEW ROUND to generate a new round.";
        }

        currentLevelInRound = 0;
        // Generate new blueprints for the next test cycle
        GenerateAndShowPreviews();
        // Re-subscribe after generating new blueprints
        LevelLoader.Instance.OnLevelClear += HandleLevelClear;
    }
}