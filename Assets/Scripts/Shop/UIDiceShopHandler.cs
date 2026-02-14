using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using System;


namespace MyGame
{
    public class UIDiceShopHandler : MonoBehaviour
    {
        [SerializeField] private GameObject shopItemContainer;
        [SerializeField] private GameObject shopItemPrefab;

        private readonly Dictionary<int, UIDiceShopItem> _shopItemMap = new Dictionary<int, UIDiceShopItem>();

        void Start()
        {
            if (DiceShopManager.Instance == null)
            {
                Debug.LogError("UIDiceShopHandler: DiceShopManager.Instance is null.", this);
                return;
            }

            if (shopItemContainer == null)
            {
                Debug.LogError("UIDiceShopHandler: shopItemContainer is not assigned.", this);
                return;
            }

            if (shopItemPrefab == null)
            {
                Debug.LogError("UIDiceShopHandler: shopItemPrefab is not assigned.", this);
                return;
            }
            for (int i = DiceShopManager.Instance.ShopItems.Length - 1; i >= 0; i--)
            {
                ShopItem item = DiceShopManager.Instance.ShopItems[i];
                GameObject newShopItem = Instantiate(shopItemPrefab, shopItemContainer.transform);
                UIDiceShopItem uiShopItem = newShopItem.GetComponent<UIDiceShopItem>();
                if (uiShopItem != null)
                {
                    int totalPurchased = 0;
                    if (SaveManager.Instance != null)
                    {
                        var data = SaveManager.Instance.GetItemData(item.Id);
                        if (data != null)
                            totalPurchased = data.totalPurchased;
                    }

                    uiShopItem.Initialize(item, (purchasedItem) =>
                    {
                        DiceShopManager.Instance.PurchaseItem(purchasedItem);
                        InitItemDisplay(purchasedItem.Id);
                    }, totalPurchased);

                    _shopItemMap[item.Id] = uiShopItem;
                }
                else
                {
                    Debug.LogError("UIDiceShopHandler: shopItemPrefab is missing a UIDiceShopItem component.", this);
                }
            }
        }

        public void InitItemDisplay(int itemId)
        {
            if (_shopItemMap.TryGetValue(itemId, out var uiItem) && SaveManager.Instance != null)
            {
                var data = SaveManager.Instance.GetItemData(itemId);
                if (data != null)
                {
                    uiItem.UpdateItemQuantity(data.totalPurchased);

                    ShopItem item = null;
                    foreach (var si in DiceShopManager.Instance.ShopItems)
                    {
                        if (si != null && si.Id == itemId) { item = si; break; }
                    }

                    if (item != null)
                    {
                        BigInteger newPrice = item.GetPrice(data.totalPurchased);
                        uiItem.UpdateItemPrice(newPrice);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the displayed price for the specified shop item based on the new price provided. 
        /// </summary>
        /// <param name="item">The shop item whose price is to be updated.</param>
        /// <param name="newPrice">The new price to be displayed.</param>
        public void UpdateItemPrice(ShopItem item, BigInteger newPrice)
        {
            if (_shopItemMap.TryGetValue(item.Id, out var uiItem))
            {
                uiItem.UpdateItemPrice(newPrice);
            }
        }

        /// <summary>
        /// Updates the displayed quantity for the specified shop item based on the total number of times it has been purchased. 
        /// </summary>
        /// <param name="item">The shop item whose quantity is to be updated.</param>
        /// <param name="totalPurchased">The total number of times the item has been purchased.</param>
        public void UpdateItemQuantity(ShopItem item, int totalPurchased)
        {
            if (_shopItemMap.TryGetValue(item.Id, out var uiItem))
            {
                uiItem.UpdateItemQuantity(totalPurchased);
            }
        }
    }
}