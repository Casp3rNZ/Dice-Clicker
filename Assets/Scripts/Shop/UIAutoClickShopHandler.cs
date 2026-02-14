using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Numerics;

namespace MyGame
{
    /// <summary>
    /// Populates and updates the auto-click shop UI.
    /// Each entry maps to a <see cref="ShopItem"/> (dice tier) and shows
    /// auto-click upgrade pricing/quantity for that tier.
    /// Automatically fits the scroll content height to the number of visible items.
    /// </summary>
    public class UIAutoClickShopHandler : MonoBehaviour
    {
        [SerializeField] private GameObject shopItemContainer;
        [SerializeField] private GameObject shopItemPrefab;

        private readonly Dictionary<int, UIAutoClickShopItem> _itemMap =
            new Dictionary<int, UIAutoClickShopItem>();

        void Start()
        {
            if (AutoClickShopManager.Instance == null)
            {
                Debug.LogError("UIAutoClickShopHandler: AutoClickShopManager.Instance is null.", this);
                return;
            }

            if (shopItemContainer == null)
            {
                Debug.LogError("UIAutoClickShopHandler: shopItemContainer is not assigned.", this);
                return;
            }

            if (shopItemPrefab == null)
            {
                Debug.LogError("UIAutoClickShopHandler: shopItemPrefab is not assigned.", this);
                return;
            }

            ShopItem[] items = AutoClickShopManager.Instance.GetShopItems();
            if (items == null || items.Length == 0) return;

            for (int i = items.Length - 1; i >= 0; i--)
            {
                ShopItem item = items[i];
                GameObject go = Instantiate(shopItemPrefab, shopItemContainer.transform);
                UIAutoClickShopItem uiItem = go.GetComponent<UIAutoClickShopItem>();

                if (uiItem != null)
                {
                    int totalPurchased = 0;
                    if (SaveManager.Instance != null)
                        totalPurchased = SaveManager.Instance.GetAutoClickPurchaseCount(item.Id);

                    uiItem.Initialize(item, (purchased) =>
                    {
                        AutoClickShopManager.Instance.PurchaseUpgrade(purchased);
                        RefreshItemDisplay(purchased.Id);
                    }, totalPurchased);

                    _itemMap[item.Id] = uiItem;

                    // Hide auto-click entry until the dice tier has been purchased at least once
                    bool dicePurchased = SaveManager.Instance != null
                        && SaveManager.Instance.GetItemData(item.Id) != null;
                    go.SetActive(dicePurchased);
                }
                else
                {
                    Debug.LogError("UIAutoClickShopHandler: prefab missing UIAutoClickShopItem.", this);
                }
            }

        }

        public void RefreshItemDisplay(int itemId)
        {
            if (!_itemMap.TryGetValue(itemId, out var uiItem)) return;
            if (SaveManager.Instance == null) return;

            int purchased = SaveManager.Instance.GetAutoClickPurchaseCount(itemId);
            uiItem.UpdateItemQuantity(purchased);

            ShopItem item = FindItem(itemId);
            if (item != null)
            {
                BigInteger newPrice = AutoClickShopManager.GetAutoClickPrice(item, purchased);
                uiItem.UpdateItemPrice(newPrice);
                uiItem.UpdateCPS(item.autoClicksPerSecond * purchased);
            }
        }

        public void UpdateItemPrice(ShopItem item, BigInteger newPrice)
        {
            if (_itemMap.TryGetValue(item.Id, out var uiItem))
                uiItem.UpdateItemPrice(newPrice);
        }

        public void UpdateItemQuantity(ShopItem item, int totalPurchased)
        {
            if (_itemMap.TryGetValue(item.Id, out var uiItem))
                uiItem.UpdateItemQuantity(totalPurchased);
        }

        public void UpdateItemCPS(ShopItem item, double cps)
        {
            if (_itemMap.TryGetValue(item.Id, out var uiItem))
                uiItem.UpdateCPS(cps);
        }

        /// <summary>
        /// Reveals the auto-click shop entry for a dice tier that has just been
        /// purchased for the first time, then rebuilds the content layout.
        /// </summary>
        public void RevealItem(int itemId)
        {
            if (_itemMap.TryGetValue(itemId, out var uiItem))
            {
                uiItem.gameObject.SetActive(true);
            }
        }

        private ShopItem FindItem(int id)
        {
            ShopItem[] items = AutoClickShopManager.Instance.GetShopItems();
            if (items == null) return null;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].Id == id)
                    return items[i];
            }
            return null;
        }
    }
}
