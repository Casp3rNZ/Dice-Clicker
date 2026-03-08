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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple AutoClickShopManager instances. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
        /// Attempts to purchase an auto-click upgrade for the given dice tier.
        /// </summary>
        public void PurchaseUpgrade(ShopItem item)
        {
            if (item == null)
            {
                Debug.LogError("AutoClickShopManager: Attempted to purchase a null item.", this);
                return;
            }

            BigInteger currentScore = SaveManager.Instance.GetScore();
            int purchased = SaveManager.Instance.GetAutoClickPurchaseCount(item.Id);
            BigInteger itemPrice = GetAutoClickPrice(item, purchased);

            if (currentScore < itemPrice)
            {
                AudioManager.Instance.PlaySFX_ShopFail(0.8f);
                return;
            }

            // Deduct funds
            GameManager.Instance.AddToScore(-itemPrice);

            // Record the purchase
            SaveManager.Instance.RecordAutoClickPurchase(item.Id);

            // Update auto-clicker runtime rate for this dice tier
            if (AutoClickerManager.Instance != null)
                AutoClickerManager.Instance.RefreshTier(item.Id, item.autoClicksPerSecond * (purchased + 1));

            // Update shop UI
            int newCount = purchased + 1;
            BigInteger nextPrice = GetAutoClickPrice(item, newCount);
            GameUIManager.Instance.UpdateACShopItemUI_Price(item.Id, nextPrice);
            GameUIManager.Instance.UpdateACShopItemUI_Quantity(item.Id, newCount);
            GameUIManager.Instance.UpdateACShopItemUI_CPS(item.Id, item.autoClicksPerSecond * newCount);

            // Play purchase sound
            AudioManager.Instance.PlaySFX_ShopSuccess(0.8f, 0.1f);

            Debug.Log($"Purchased auto-click for {item.Name} (total: {newCount})");
        }
    }
}
