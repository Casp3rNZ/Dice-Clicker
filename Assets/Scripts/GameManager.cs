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
        [SerializeField] private DiceManager diceManager;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private AudioManager audioManager;

        [SerializeField] private PopupTextHandler popupTextHandler;
        [SerializeField] private UnityEngine.Vector3 popupTextOffset = new UnityEngine.Vector3(0f, 1.2f, 0f);
        [SerializeField] private GameObject uiCanvas;
        [SerializeField] private TMP_Text scoreText;
        [Header("Score Font Scaling")]
        [Tooltip("Font size when the score has very few digits (≤6).")]
        [SerializeField] private float maxFontSize = 72f;
        [Tooltip("Minimum font size no matter how many digits.")]
        [SerializeField] private float minFontSize = 30f;
        [Tooltip("Number of digits at which font begins shrinking.")]
        [SerializeField] private int shrinkStartDigits = 7;
        [Tooltip("Number of digits at which font reaches minimum size.")]
        [SerializeField] private int shrinkEndDigits = 30;

        private BigInteger score = BigInteger.Zero;
        private BigInteger displayedScore = BigInteger.Zero;

        private readonly HashSet<DiceController> subscribedDice = new HashSet<DiceController>();

        private void OnEnable()
        {
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

        private void Update()
        {
            // Update UI score
            if (scoreText != null)
            {
                if(displayedScore == score)
                    return;
                if (displayedScore > score)
                {
                    BigInteger gap = displayedScore - score;
                    displayedScore -= BigInteger.One + gap / 10;
                    if (displayedScore < score) displayedScore = score;
                }
                else
                {
                    audioManager.PlaySFX_ScoreCounterTick(0.05f);
                    BigInteger gap = score - displayedScore;
                    displayedScore += BigInteger.One + gap / 10;
                    if (displayedScore > score) displayedScore = score;
                }
                scoreText.text = FormatBigInteger(displayedScore);
                AdjustScoreFontSize(scoreText.text);
                scoreText.GetComponent<ImageAligner>()?.AlignImage();
            }
        }

        public void AddToScore(BigInteger amount)
        {
            score += amount;
            saveManager?.UpdateScore(score);
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
        private static string FormatBigInteger(BigInteger value)
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

        /// <summary>
        /// Scales the score TMP_Text font size so the number always fits in one line.
        /// Lerps between maxFontSize and minFontSize based on digit count.
        /// </summary>
        private void AdjustScoreFontSize(string formattedText)
        {
            if (scoreText == null) return;
            if (formattedText.Length <= shrinkStartDigits)
            {
                scoreText.fontSize = maxFontSize;
                return;
            }

            // Linearly interpolate between max and min over the digit range
            float t = Mathf.InverseLerp(shrinkStartDigits, shrinkEndDigits, formattedText.Length);
            scoreText.fontSize = Mathf.Lerp(maxFontSize, minFontSize, t);
        }
    }
}