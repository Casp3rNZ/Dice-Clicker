using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Numerics;

namespace MyGame
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance;

        [Header("Autosave")]
        [SerializeField] private bool autoSaveEnabled = true;
        [Min(1f)]
        [SerializeField] private int autoSaveIntervalSeconds = 30;
        [SerializeField] private GameManager gameManager;

        private Coroutine _autoSaveRoutine;

        private GameSaveData _currentSaveData;
        private string _saveFilePath;

        private void EnsureSavePathInitialized()
        {
            if (!string.IsNullOrEmpty(_saveFilePath))
                return;

            InitializeSavePath();
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                EnsureSavePathInitialized();
                Debug.Log($"Save path: {_saveFilePath}");
                LoadGame();

                BigInteger loadedScore = ParseScore(_currentSaveData.playerScore);
                if (loadedScore > BigInteger.Zero)
                {
                    gameManager.SetScore(loadedScore);
                    Debug.Log($"Loaded score: {loadedScore}");
                }

                if (autoSaveEnabled)
                    StartAutoSave();
            }
            else
            {
                Destroy(gameObject);
            }
            if(gameManager == null)
            {
                Debug.LogError("SaveManager: gameManager is not assigned.", this);
                return;
            }
        }

        private void OnDisable()
        {
            SaveGame();
            StopAutoSave();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public void StartAutoSave()
        {
            if (_autoSaveRoutine != null)
                return;

            _autoSaveRoutine = StartCoroutine(AutoSaveLoop());
        }

        public void StopAutoSave()
        {
            if (_autoSaveRoutine == null)
                return;

            StopCoroutine(_autoSaveRoutine);
            _autoSaveRoutine = null;
        }

        private IEnumerator AutoSaveLoop()
        {
            var wait = new WaitForSecondsRealtime(autoSaveIntervalSeconds);
            while (true)
            {
                yield return wait;
                SaveGame();
            }
        }

        void InitializeSavePath()
        {
            _saveFilePath = Path.Combine(Application.persistentDataPath, "dice.json");
        }

        public void SaveGame()
        {
            EnsureSavePathInitialized();

            if (_currentSaveData == null)
            {
                _currentSaveData = new GameSaveData();
            }

            string directory = Path.GetDirectoryName(_saveFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            string jsonData = JsonUtility.ToJson(_currentSaveData, true);
            File.WriteAllText(_saveFilePath, jsonData);
            Debug.Log($"Game Saved: {_saveFilePath}");
        }

        public void LoadGame()
        {
            EnsureSavePathInitialized();

            if (File.Exists(_saveFilePath))
            {
                string jsonData = File.ReadAllText(_saveFilePath);
                _currentSaveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                Debug.Log("Save Loaded.");
            }
            else
            {
                // Create new data if no file exists
                _currentSaveData = new GameSaveData();
                _currentSaveData.unlockedItemIds = new List<itemData>() {
                        new itemData { 
                            itemId = 1, 
                            diceLevels = new List<int> { 1 }, 
                            totalPurchased = 1 }
                };
                Debug.Log($"No save file found at '{_saveFilePath}', creating new data.");
            }
        }

        public GameSaveData GetAllCurrentData()
        {
            // Ensure we always return valid data
            if (_currentSaveData == null)
                LoadGame();

            return _currentSaveData;
        }

        public BigInteger GetScore()
        {
            if (_currentSaveData == null) LoadGame();
            return ParseScore(_currentSaveData.playerScore);
        }

        public void UpdateScore(BigInteger newScore)
        {
            if (_currentSaveData == null) LoadGame();
            _currentSaveData.playerScore = newScore.ToString();
        }

        private static BigInteger ParseScore(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return BigInteger.Zero;
            return BigInteger.TryParse(raw, out var result) ? result : BigInteger.Zero;
        }

        /// <summary>
        /// Adds a level-1 die for the given item, increments totalPurchased,
        /// then performs cascading merges (10 dice at same level → 1 die at level+1).
        /// Returns a MergeResult describing what happened.
        /// </summary>
        public MergeResult UnlockItem(int itemId)
        {
            if (_currentSaveData == null) LoadGame();
            if (_currentSaveData.unlockedItemIds == null)
                _currentSaveData.unlockedItemIds = new List<itemData>();

            itemData existing = _currentSaveData.unlockedItemIds.Find(item => item.itemId == itemId);
            if (existing == null)
            {
                existing = new itemData
                {
                    itemId = itemId,
                    diceLevels = new List<int>(),
                    totalPurchased = 0
                };
                _currentSaveData.unlockedItemIds.Add(existing);
            }

            existing.diceLevels.Add(1);
            existing.totalPurchased += 1;

            // Cascading merge: keep merging while any level has 10+ dice
            var mergedLevels = new List<int>();
            bool anyMerged = false;
            bool changed = true;
            while (changed)
            {
                changed = false;
                // Count dice at each level
                var levelCounts = new Dictionary<int, int>();
                for (int i = 0; i < existing.diceLevels.Count; i++)
                {
                    int lv = existing.diceLevels[i];
                    if (!levelCounts.ContainsKey(lv)) levelCounts[lv] = 0;
                    levelCounts[lv]++;
                }

                foreach (var kvp in levelCounts)
                {
                    if (kvp.Value >= 10)
                    {
                        int mergeLevel = kvp.Key;
                        // Remove 10 dice of this level
                        int removed = 0;
                        for (int i = existing.diceLevels.Count - 1; i >= 0 && removed < 10; i--)
                        {
                            if (existing.diceLevels[i] == mergeLevel)
                            {
                                existing.diceLevels.RemoveAt(i);
                                removed++;
                            }
                        }
                        // Add 1 die at level+1
                        existing.diceLevels.Add(mergeLevel + 1);
                        mergedLevels.Add(mergeLevel);
                        anyMerged = true;
                        changed = true;
                        break; // restart scan after mutation
                    }
                }
            }

            return new MergeResult
            {
                didMerge = anyMerged,
                mergedAtLevels = mergedLevels
            };
        }

        /// <summary>
        /// Returns the save data for a specific item, or null if not found.
        /// </summary>
        public itemData GetItemData(int itemId)
        {
            if (_currentSaveData == null) LoadGame();
            if (_currentSaveData.unlockedItemIds == null) return null;
            return _currentSaveData.unlockedItemIds.Find(item => item.itemId == itemId);
        }

        // ─────────────── Auto-Click Upgrade Data ───────────────

        /// <summary>
        /// Returns how many times the given auto-click upgrade has been purchased.
        /// </summary>
        public int GetAutoClickPurchaseCount(int diceTypeId)
        {
            if (_currentSaveData == null) LoadGame();
            if (_currentSaveData.autoClickUpgrades == null) return 0;
            var data = _currentSaveData.autoClickUpgrades.Find(d => d.diceTypeId == diceTypeId);
            return data != null ? data.totalPurchased : 0;
        }

        /// <summary>
        /// Records a single purchase of the given auto-click upgrade.
        /// </summary>
        public void RecordAutoClickPurchase(int diceTypeId)
        {
            if (_currentSaveData == null) LoadGame();
            if (_currentSaveData.autoClickUpgrades == null)
                _currentSaveData.autoClickUpgrades = new List<autoClickData>();

            var data = _currentSaveData.autoClickUpgrades.Find(d => d.diceTypeId == diceTypeId);
            if (data == null)
            {
                data = new autoClickData { diceTypeId = diceTypeId, totalPurchased = 0 };
                _currentSaveData.autoClickUpgrades.Add(data);
            }
            data.totalPurchased++;
        }
    }

    /// <summary>
    /// Result of a dice purchase + merge attempt.
    /// </summary>
    public class MergeResult
    {
        public bool didMerge;
        /// <summary>The levels at which merges occurred (in cascade order).</summary>
        public List<int> mergedAtLevels;
    }

    [System.Serializable]
    public class GameSaveData
    {
        /// <summary>
        /// Score stored as a string so BigInteger survives JsonUtility serialization.
        /// </summary>
        public string playerScore = "0";
        public List<itemData> unlockedItemIds;
        public List<autoClickData> autoClickUpgrades;
        public string CS;
    }

    [System.Serializable]
    public class itemData
    {
        public int itemId;
        /// <summary>
        /// One entry per individual die owned. The value is that die's level.
        /// e.g. [1, 1, 1, 2] = three level-1 dice and one level-2 die.
        /// </summary>
        public List<int> diceLevels = new List<int>();
        /// <summary>
        /// Lifetime total purchases for this tier (for UI display).
        /// </summary>
        public int totalPurchased;
    }

    [System.Serializable]
    public class autoClickData
    {
        /// <summary>
        /// The dice tier (ShopItem.Id) this auto-click upgrade applies to.
        /// </summary>
        public int diceTypeId;
        /// <summary>
        /// Lifetime total purchases for this dice tier's auto-click upgrade.
        /// </summary>
        public int totalPurchased;
    }
}