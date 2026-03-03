using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Numerics;
using TMPro;

namespace MyGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [SerializeField] private DiceManager diceManager;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private PopupTextHandler popupTextHandler;
        [SerializeField] private UnityEngine.Vector3 popupTextOffset = new UnityEngine.Vector3(0f, 1.2f, 0f);
        [SerializeField] private GameObject uiCanvas;
        private BigInteger score = BigInteger.Zero;

        // For tracking income over the last 60 seconds
        private struct IncomeEntry
        {
            public float time;
            public BigInteger amount;
        }
        private readonly Queue<IncomeEntry> incomeHistory = new Queue<IncomeEntry>();
        private BigInteger incomeSumLast60s = BigInteger.Zero;

        private readonly HashSet<DiceController> subscribedDice = new HashSet<DiceController>();

        private void OnEnable()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple instances of GameManager detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            if (diceManager != null)
                diceManager.OnDieCreated += HandleDieCreated;
        }

        private void OnDisable()
        {
            if (diceManager != null)
                diceManager.OnDieCreated -= HandleDieCreated;
        }

        private void HandleDieCreated(Die die)
        {
            SubscribeToDie(die);
        }

        private void Start()
        {
            // Unity defaults to 30 FPS on mobile — unlock to 60
            Application.targetFrameRate = 60;

            if (diceManager == null)
            {
                Debug.LogError("GameManager: diceManager is not assigned.", this);
                return;
            }

            if (saveManager == null)
            {
                Debug.LogError("GameManager: saveManager is not assigned.", this);
                return;
            }

            if (audioManager == null)
            {
                Debug.LogError("GameManager: audioManager is not assigned.", this);
                return;
            }

            // Subscribe to dice settled events
            foreach (var die in diceManager.DiceList_)
            {
                SubscribeToDie(die);
            }
        }

        private void SubscribeToDie(Die die)
        {
            if (die == null || die.GameObject == null)
                return;
            if (DiceShopManager.Instance == null)
                return;
            var diceController = die.GameObject.GetComponent<DiceController>();
            if (diceController == null)
                return;
            if (!subscribedDice.Add(diceController))
                return;

            diceController.OnDiceSettled += (int result) =>
            {
                BigInteger baseMultiplier = DiceShopManager.Instance.GetMultiplierForDiceType(die.Dicetype);
                BigInteger levelMultiplier = GetLevelMultiplier(die.Level);
                BigInteger modifiedResult = result * baseMultiplier * levelMultiplier;
                AddToScore(modifiedResult);
                if (die.GameObject != null)
                    ShowPopupText(modifiedResult, die.GameObject.transform.position);
            };
        }

        public void AddToScore(BigInteger amount)
        {
            score += amount;
            saveManager?.UpdateScore(score);
            TrackIncome(amount);
        }

        private void TrackIncome(BigInteger amount)
        {
            float now = Time.time;
            incomeHistory.Enqueue(new IncomeEntry { time = now, amount = amount });
            incomeSumLast60s += amount;

            // Remove entries older than 60 seconds
            while (incomeHistory.Count > 0 && now - incomeHistory.Peek().time > 60f)
            {
                incomeSumLast60s -= incomeHistory.Dequeue().amount;
            }
        }

        /// <summary>
        /// Returns the total income earned over the last 60 seconds.
        /// </summary>
        private BigInteger GetIncomeLast60Seconds()
        {
            // Clean up old entries in case this is called infrequently
            float now = Time.time;
            while (incomeHistory.Count > 0 && now - incomeHistory.Peek().time > 60f)
            {
                incomeSumLast60s -= incomeHistory.Dequeue().amount;
            }
            return incomeSumLast60s;
        }

        /// <summary>
        /// Returns the average income per second over the last 60 seconds.
        /// </summary>
        public BigInteger GetAverageIncomePerSecondLast60Seconds()
        {
            return GetIncomeLast60Seconds() / 60;
        }

        public void SetScore(BigInteger newScore)
        {
            // for use from save manager on init, no need to update save 
            score = newScore;
        }

        private void ShowPopupText(BigInteger value, UnityEngine.Vector3 diceWorldPos)
        {
            if (popupTextHandler == null)
                return;

            var popup = Instantiate(popupTextHandler, diceWorldPos + popupTextOffset, UnityEngine.Quaternion.identity);
            popup.Play(FormatBigInteger(value));
        }

        /// <summary>
        /// Returns the score multiplier for a given dice level.
        /// Level 1 = 1x, Level 2 = 10x, Level 3 = 100x, etc. (10^(level-1))
        /// </summary>
        public static BigInteger GetLevelMultiplier(int level)
        {
            if (level <= 1) return BigInteger.One;
            return BigInteger.Pow(10, level - 1);
        }

        /// <summary>
        /// Formats a BigInteger with thousand separators (e.g. 1,234,567).
        /// </summary>
        public static string FormatBigInteger(BigInteger value)
        {
            // For values that fit in a long, use built-in formatting
            if (value >= long.MinValue && value <= long.MaxValue)
                return ((long)value).ToString("N0");

            // For truly huge values, insert commas manually
            string raw = value.ToString();
            bool negative = raw[0] == '-';
            if (negative) raw = raw.Substring(1);

            int insertCount = (raw.Length - 1) / 3;
            char[] result = new char[raw.Length + insertCount];
            int ri = result.Length - 1;
            for (int i = raw.Length - 1, digits = 0; i >= 0; i--, digits++)
            {
                if (digits > 0 && digits % 3 == 0)
                    result[ri--] = ',';
                result[ri--] = raw[i];
            }

            string formatted = new string(result, ri + 1, result.Length - ri - 1);
            return negative ? "-" + formatted : formatted;
        }
    }
}