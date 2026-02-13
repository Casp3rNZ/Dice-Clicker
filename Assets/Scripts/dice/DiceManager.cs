using System.Collections.Generic;
using UnityEngine;

namespace MyGame
{
    public class DiceManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject dicePrefab;

        [Header("Spawn")]
        [SerializeField] private GameObject spawnBox;
        [Header("Saves")]
        [SerializeField] private SaveManager saveManager;

        private readonly List<Die> DiceList = new List<Die>();
        public event System.Action<Die> OnDieCreated;

        public IReadOnlyList<Die> DiceList_ => DiceList;
        private int nextDiceToRollId = 0;

        private void Awake()
        {
            if (dicePrefab == null)
            {
                Debug.LogError("DiceManager: dicePrefab is not assigned.", this);
                return;
            }

            if(spawnBox == null)
            {
                Debug.LogError("DiceManager: spawnBox is not assigned.", this);
                return;
            }
            if(saveManager == null)
            {
                Debug.LogError("DiceManager: saveManager is not assigned.", this);
                return;
            }

            nextDiceToRollId = 0;

            GameSaveData currentSaveData = saveManager.GetAllCurrentData();
            List<itemData> unlockedItems = currentSaveData?.unlockedItemIds;
            int unlockedCount = unlockedItems?.Count ?? 0;

            // Spawn a default die if no items are unlocked (or save data is missing)
            if (unlockedCount == 0)
            {
                CreateDice(1);
                return;
            }

            // Else spawn dice based on unlocked items (each entry in diceLevels = one die)
            for (int i = 0; i < unlockedCount; i++)
            {
                itemData item = unlockedItems[i];
                if (item.diceLevels == null) continue;
                for (int x = 0; x < item.diceLevels.Count; x++)
                {
                    int level = item.diceLevels[x] > 0 ? item.diceLevels[x] : 1;
                    CreateDice(item.itemId, level);
                }
            }
        }

        public void CreateDice(int type, int level = 1)
        {
            int id = DiceList.Count;
            Vector3 randomPoint = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0,
                Random.Range(-0.5f, 0.5f)
            );
            Vector3 spawnPosition = spawnBox.transform.TransformPoint(randomPoint);
            Quaternion spawnRotation = Random.rotation;
            GameObject diceInstance = Instantiate(dicePrefab, spawnPosition, spawnRotation);

            // Apply scale based on level (caps at 500%)
            float scale = GetScaleForLevel(level);
            diceInstance.transform.localScale = Vector3.one * scale;

            // Script-driven appearance: swap materials based on dice type.
            if (diceInstance.TryGetComponent<DiceController>(out var diceController))
            {
                Material materialForType = null;
                Material pipMaterialForType = null;
                if (ShopManager.Instance != null)
                {
                    materialForType = ShopManager.Instance.GetMaterialForDiceType(type);
                    pipMaterialForType = ShopManager.Instance.GetPipMaterialForDiceType(type);
                }

                if (materialForType != null)
                    diceController.SetMaterial(materialForType);

                if (pipMaterialForType != null)
                    diceController.SetPipMaterial(pipMaterialForType);
            }

            var die = new Die(id, type, level, diceInstance);
            DiceList.Add(die);
            OnDieCreated?.Invoke(die);
        }

        /// <summary>
        /// Merges 10 dice of the given type and level into 1 die at level+1.
        /// Destroys exactly 10 matching GameObjects and spawns 1 replacement.
        /// </summary>
        public void MergeDice(int typeId, int mergeLevel)
        {
            int removed = 0;
            for (int i = DiceList.Count - 1; i >= 0 && removed < 10; i--)
            {
                if (DiceList[i].Dicetype == typeId && DiceList[i].Level == mergeLevel)
                {
                    if (DiceList[i].GameObject != null)
                        Destroy(DiceList[i].GameObject);
                    DiceList.RemoveAt(i);
                    removed++;
                }
            }

            nextDiceToRollId = 0;
            CreateDice(typeId, mergeLevel + 1);
        }

        /// <summary>
        /// Rebuilds all dice for a given type from the save data's diceLevels array.
        /// Used after cascading merges to ensure the scene matches save state.
        /// </summary>
        public void RebuildDiceForType(int typeId, List<int> diceLevels)
        {
            // Remove all existing dice of this type
            for (int i = DiceList.Count - 1; i >= 0; i--)
            {
                if (DiceList[i].Dicetype == typeId)
                {
                    if (DiceList[i].GameObject != null)
                        Destroy(DiceList[i].GameObject);
                    DiceList.RemoveAt(i);
                }
            }
            nextDiceToRollId = 0;

            // Respawn from save data
            if (diceLevels != null)
            {
                for (int i = 0; i < diceLevels.Count; i++)
                {
                    CreateDice(typeId, diceLevels[i] > 0 ? diceLevels[i] : 1);
                }
            }
        }

        /// <summary>
        /// Returns the visual scale multiplier for a given dice level.
        /// Scale increases by 50% per level, capping at 200% (2x).
        /// </summary>
        public static float GetScaleForLevel(int level)
        {
            return Mathf.Min(1f + (level - 1) * 0.5f, 2f);
        }

        public void RollNextDice()
        {
            if (DiceList == null || DiceList.Count == 0)
            {
                Debug.LogWarning("DiceManager: No dice available to roll.");
                return;
            }

            if (nextDiceToRollId < 0 || nextDiceToRollId >= DiceList.Count)
                nextDiceToRollId = 0;

            int count = DiceList.Count;
            int index = nextDiceToRollId;

            for (int attempt = 0; attempt < count; attempt++)
            {
                var die = DiceList[index];
                if (die != null && die.GameObject != null && die.GameObject.TryGetComponent<DiceController>(out var dice))
                {
                    dice.Roll();
                    nextDiceToRollId = (index + 1) % count;
                    return;
                }

                index = (index + 1) % count;
            }

            Debug.LogWarning("DiceManager: No valid dice available to roll.");
        }
    }

    [System.Serializable]
    public sealed class Die
    {
        public int Id { get; }
        public int Dicetype { get; set; }
        public int Level { get; set; }
        public GameObject GameObject { get; }

        public Die(int id, int dicetype, int level, GameObject gameObject)
        {
            Id = id;
            Dicetype = dicetype;
            Level = level;
            GameObject = gameObject;
        }
    }
}