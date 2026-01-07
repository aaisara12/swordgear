#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

    public class RoundStarter : MonoBehaviour
    {
        public static RoundStarter? Instance;
        [SerializeField] private UnityEvent onRoundFinished = new UnityEvent();
        
        private List<LevelBlueprint> currentRoundBlueprints = new List<LevelBlueprint>();
        private int currentLevelInRound = 0;

        private LevelLoader? levelLoader;
        private int levelsCleared = 0;

        void Awake() { Instance = this; }

        public void StartRound()
        {
            Debug.Log("Starting round");
            levelsCleared = 0;
            currentLevelInRound = 0;
            currentRoundBlueprints = RoundGenerator.Instance.GenerateNewRound();
            LoadLevel(currentLevelInRound);
        }
        
        private void Start()
        {
            levelLoader = LevelLoader.Instance;

            if (levelLoader == null)
            {
                Debug.LogError($"{nameof(levelLoader)} is null");
                return;
            }
            
            levelLoader.OnLevelClear += HandleLevelClear;
        }
        
        private void LoadLevel(int levelIndex)
        {
            if (levelIndex >= currentRoundBlueprints.Count)
            {
                Debug.LogError("Tried to load level out of bounds! Should have ended the round.");
                return;
            }

            // Remove the old arena from the scene here if necessary!
            LevelBlueprint nextBlueprint = currentRoundBlueprints[levelIndex];
            LevelLoader.Instance.LoadLevel(nextBlueprint);
        }
        
        private void HandleLevelClear()
        {
            levelsCleared++;
            
            if (levelsCleared >= currentRoundBlueprints.Count)
            {
                onRoundFinished.Invoke();
            }
            else
            {
                LoadLevel(levelsCleared);
            }
        }

        private void OnDestroy()
        {
            if (levelLoader != null)
            {
                levelLoader.OnLevelClear -= HandleLevelClear;
            }
        }
    }
