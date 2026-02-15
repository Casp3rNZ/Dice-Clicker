using UnityEngine;
using System;
using UnityEngine.UI;
namespace MyGame
{
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("LevelPlay Rewarded Ad Prefabs")]
        [SerializeField] private GameObject rewardedAdPromptPrefab;
        [SerializeField] private GameObject confirmationWindowPrefab;
        [SerializeField] private Transform uiParent;

        [Header("Managers")]
        [SerializeField] private GameManager gameManager;

        private GameObject _activePrompt;
        private GameObject _activeConfirmation;
        private RewardType _pendingRewardType;

        public enum RewardType
        {
            ThirtyMinutesAutoClickerIncome = 0,
            // TODO: Add more reward types 
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ShowRewardedAdPrompt(RewardType rewardType)
        {
            if (_activePrompt != null) Destroy(_activePrompt);
            _pendingRewardType = rewardType;
            _activePrompt = Instantiate(rewardedAdPromptPrefab, uiParent);
            var button = _activePrompt.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(OnPromptClicked);
        }

        private void OnPromptClicked()
        {
            if (_activeConfirmation != null) Destroy(_activeConfirmation);
            _activeConfirmation = Instantiate(confirmationWindowPrefab, uiParent);
        }

        public void ShowLevelPlayRewardedAd(RewardType rewardType)
        {
            // TODO: actual LevelPlay API call
            Debug.Log($"Showing rewarded ad for reward: {rewardType}");
            // Simulate ad watched successfully:
            OnRewardedAdCompleted(rewardType);
        }

        private void OnRewardedAdCompleted(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.ThirtyMinutesAutoClickerIncome:
                    GrantThirtyMinutesAutoClickerIncome();
                    break;
                // Add more cases for other reward types
            }
        }

        private void GrantThirtyMinutesAutoClickerIncome()
        {
            if ( gameManager == null)
            {
                Debug.LogError("AdManager: Missing manager references for reward.");
                return;
            }
            double totalIncome = CalculateAutoClickerIncome(30 * 60); // 30 minutes in seconds
            gameManager.AddToScore(new System.Numerics.BigInteger(totalIncome));
            Debug.Log($"Granted {totalIncome} autoclicker income for 30 minutes.");
        }

        private double CalculateAutoClickerIncome(double seconds)
        {
            // This is a placeholder. Replace with your actual autoclicker income calculation logic.
            // Example: sum all tiers' CPS * seconds * average dice value
            double total = 0;
            // TODO: Implement actual calculation using AutoClickerManager data
            return total;
        }
    }
}