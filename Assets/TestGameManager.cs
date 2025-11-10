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

        // Subscribe to the LevelLoader event for level-to-level transition
        LevelLoader.Instance.OnLevelClear += HandleLevelClear;

        Debug.Log("Round generated. Ready to start.");
    }

    // --- Step 2: Begin Gameplay Loop ---
    public void OnStartRoundClicked()
    {
        // Disable the button to prevent double-clicking
        if (startRoundButton != null)
        {
            startRoundButton.interactable = false;
        }

        Debug.Log("Starting Round: Loading Level 1.");
        currentLevelInRound = 0;
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
    private void HandleLevelClear()
    {
        // Check if we just loaded the level or if we were waiting for an enemy to die
        if (currentLevelInRound < currentRoundBlueprints.Count)
        {
            Debug.Log($"Level {currentLevelInRound + 1} Cleared!");
            currentLevelInRound++;

            if (currentLevelInRound < currentRoundBlueprints.Count)
            {
                // Load the next level (2 or 3)
                Debug.Log($"Loading Level {currentLevelInRound + 1}...");
                LoadNextLevel();
            }
            else
            {
                // Level 3 is done!
                Debug.Log("Round COMPLETE! Gameplay loop finished.");
                TransitionToShop();
            }
        }
    }

    private void TransitionToShop()
    {
        // Dummy implementation of Step 9:
        Debug.Log("Unsubscribing from LevelLoader events.");
        LevelLoader.Instance.OnLevelClear -= HandleLevelClear;

        // Re-enable the button to start a new round for testing purposes
        if (startRoundButton != null)
        {
            startRoundButton.interactable = true;
            previewText.text = "ROUND FINISHED. Press START ROUND to generate a new round.";
        }

        // In a real game, this would load the shop scene or open the shop UI panel.
    }
}