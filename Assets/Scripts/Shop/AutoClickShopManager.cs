using UnityEngine;
using System.Numerics;

namespace MyGame
{
    /// <summary>
    /// Handles purchasing auto-click upgrades.
    /// Each upgrade maps to a <see cref="ShopItem"/> dice tier and will only
    /// auto-roll dice of that type. Uses <see cref="ShopItem"/> for
    /// incremental pricing with each ShopItem's auto-click price fields.
    /// </summary>
    public class AutoClickShopManager : MonoBehaviour
    {
        public static AutoClickShopManager Instance { get; private set; }

        [SerializeField] private SaveManager saveManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private UIAutoClickShopHandler uiHandler;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple AutoClickShopManager instances. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (saveManager == null)
            {
                Debug.LogError("AutoClickShopManager: saveManager is not assigned.", this);
                return;
            }
            if (gameManager == null)
            {
                Debug.LogError("AutoClickShopManager: gameManager is not assigned.", this);
                return;
            }
            if (audioManager == null)
            {
                Debug.LogError("AutoClickShopManager: audioManager is not assigned.", this);
                return;
            }
            if (uiHandler == null)
            {
                Debug.LogError("AutoClickShopManager: uiHandler is not assigned.", this);
                return;
            }
        }

        /// <summary>
        /// The shop items (dice tiers) are sourced from <see cref="DiceShopManager"/>.
        /// </summary>
        public ShopItem[] GetShopItems()
        {
            return DiceShopManager.Instance != null ? DiceShopManager.Instance.ShopItems : null;
        }

        /// <summary>
        /// Calculates the current auto-click upgrade price for a dice tier.
        /// Uses the ShopItem's auto-click-specific base price and growth rate.
        /// </summary>
        public static BigInteger GetAutoClickPrice(ShopItem item, int purchased)
        {
            return item.GetAutoClickPrice(purchased);
        }

        /// <summary>
        /// Reveals the auto-click shop entry for the given dice tier.
        /// Called by <see cref="DiceShopManager"/> on first dice purchase.
        /// </summary>
        public void RevealAutoClickItem(int diceTypeId)
        {
            if (uiHandler != null)
                uiHandler.RevealItem(diceTypeId);
        }

        /// <summary>
        /// Attempts to purchase an auto-click upgrade for the given dice tier.
        /// </summary>
        public void PurchaseUpgrade(ShopItem item)
        {
            if (item == null)
            {
                Debug.LogError("AutoClickShopManager: Attempted to purchase a null item.", this);
                return;
            }

            BigInteger currentScore = saveManager.GetScore();
            int purchased = saveManager.GetAutoClickPurchaseCount(item.Id);
            BigInteger itemPrice = GetAutoClickPrice(item, purchased);

            if (currentScore < itemPrice)
            {
                audioManager.PlaySFX_ShopFail(0.8f);
                return;
            }

            // Deduct funds
            gameManager.AddToScore(-itemPrice);

            // Record the purchase
            saveManager.RecordAutoClickPurchase(item.Id);

            // Update auto-clicker runtime rate for this dice tier
            if (AutoClickerManager.Instance != null)
                AutoClickerManager.Instance.RefreshTier(item.Id, item.autoClicksPerSecond * (purchased + 1));

            // Update shop UI
            int newCount = purchased + 1;
            BigInteger nextPrice = GetAutoClickPrice(item, newCount);
            uiHandler.UpdateItemPrice(item, nextPrice);
            uiHandler.UpdateItemQuantity(item, newCount);
            uiHandler.UpdateItemCPS(item, item.autoClicksPerSecond * newCount);

            // Play purchase sound
            audioManager.PlaySFX_ShopSuccess(0.8f, 0.1f);

            Debug.Log($"Purchased auto-click for {item.Name} (total: {newCount})");
        }
    }
}
