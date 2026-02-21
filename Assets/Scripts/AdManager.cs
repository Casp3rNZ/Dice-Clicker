using UnityEngine;
using System;
using System.Collections;
using System.Numerics;
using UnityEngine.UI;
using Unity.Services.LevelPlay;


namespace MyGame
{
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("LevelPlay Rewarded Ad Prefabs")]
        [SerializeField] private UIRewAdIcon rewardedAdPrompt;
        [SerializeField] private UIRewAdConfirmWindow confirmationWindowController;
        [SerializeField] private Transform uiParent;

        [Header("Managers")]
        [SerializeField] private GameManager gameManager;

        // UI elements

        private RewardType _pendingRewardType = 0;

        // LevelPlay rewarded ad instance
        private LevelPlayRewardedAd _RewardedAd_30MinInc;
        private string LevelPlayAppID = "254056a35";
        private string LevelPlayRewardedAdID_30MinInc = "4thkspy91dt93hd2";

        public enum RewardType
        {
            TimedIncome = 0,
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
            // Register OnInitFailed and OnInitSuccess listeners
            LevelPlay.OnInitSuccess += AdsInitSuccess;
            LevelPlay.OnInitFailed += AdsInitFailed;

            // SDK init
            LevelPlay.Init(LevelPlayAppID);
        }

        private void AdsInitSuccess(LevelPlayConfiguration config)
        {
            LevelPlay.LaunchTestSuite();
            _RewardedAd_30MinInc = new LevelPlayRewardedAd(LevelPlayRewardedAdID_30MinInc);
            // Register to Rewarded events
            _RewardedAd_30MinInc.OnAdLoaded += AdLoaded;
            // RewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
            // RewardedAd.OnAdDisplayed += RewardedOnAdDisplayedEvent;
            // RewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailedEvent;
            _RewardedAd_30MinInc.OnAdRewarded += RewardedAdCompleted; 
            // RewardedAd.OnAdClosed += RewardedOnAdClosedEvent;
            // // Optional 
            // RewardedAd.OnAdClicked += RewardedOnAdClickedEvent;
            // RewardedAd.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;
            StartCoroutine(LoadAdAfterDelay(60f));
            Debug.Log("LevelPlay SDK initialized successfully.");
        }

        private void AdLoaded(LevelPlayAdInfo adInfo)
        {
            // Log ad info for last played ad (optional)
            string auctionID = adInfo.AuctionId;
            string adUnit = adInfo.AdUnitId;
            string country = adInfo.Country;
            string ab = adInfo.Ab;
            string segmentName = adInfo.SegmentName;
            string adNetwork = adInfo.AdNetwork;
            string instanceName = adInfo.InstanceName;
            string instanceId = adInfo.InstanceId;
            double? revenue = adInfo.Revenue;
            string precision = adInfo.Precision;
            string encryptedCPM = adInfo.EncryptedCPM;
            LevelPlayAdSize adSize = adInfo.AdSize;
            // Ad loaded, display popup
            rewardedAdPrompt.ShowIcon();
        }

        public void ShowAd()
        {
            if (_RewardedAd_30MinInc.IsAdReady())
            {
                rewardedAdPrompt.HideIcon();
                confirmationWindowController.Close();
                _RewardedAd_30MinInc.ShowAd();
            }
            else
            {
                Debug.Log("Rewarded Ad not ready");
            }
        }

        private void AdsInitFailed(LevelPlayInitError error)
        {
            Debug.LogError($"LevelPlay SDK initialization failed: {error}");
        }

        private void OnPromptClicked()
        {
            confirmationWindowController.Open();
        }

        private void RewardedAdCompleted(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            switch (_pendingRewardType)
            {
                case RewardType.TimedIncome:
                    GrantTimedIncome(reward.Amount);
                    Debug.Log($"Granted timed income reward: {reward.Amount}.");
                    break;
                // Add more cases for other reward types
            }
            StartCoroutine(LoadAdAfterDelay(60f)); // Preload next ad after 60 seconds
        }

        private IEnumerator LoadAdAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            _RewardedAd_30MinInc.LoadAd();
            Debug.Log($"AdManager: Loaded next rewarded ad after {delaySeconds} seconds.");
        }

        private void GrantTimedIncome(int minutes = 30)
        {
            if ( gameManager == null)
            {
                Debug.LogError("AdManager: Missing manager references for reward.");
                return;
            }
            BigInteger totalIncome = CalculateAutoClickerIncome(minutes);
            gameManager.AddToScore(totalIncome);
            Debug.Log($"Granted {totalIncome} autoclicker income for {minutes} minutes.");
        }

        private BigInteger CalculateAutoClickerIncome(double minutes)
        {
            BigInteger avgIncome_60Sec = gameManager.GetAverageIncomePerSecondLast60Seconds();
            BigInteger totalIncome = avgIncome_60Sec * 60 * (BigInteger)minutes;
            return totalIncome;
        }
    }
}