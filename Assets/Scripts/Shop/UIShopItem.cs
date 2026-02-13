using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Numerics;

namespace MyGame
{
    public class UIShopItem : MonoBehaviour
    {

        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text nameText;
        //[SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private RawImage dicePreviewImage;

        private ShopItem originalItemData;

        public void Initialize(ShopItem item, System.Action<ShopItem> onPurchase, int totalPurchased = 0)
        {
            if(item == null)
            {
                Debug.LogError("UIShopItem: Initialize called with null item.");
                return;
            }
            if(purchaseButton == null || nameText == null || priceText == null || quantityText == null)
            {
                Debug.LogError("UIShopItem: One or more UI components are not assigned.", this);
                return;
            }
            nameText.text = item.Name;
            //descriptionText.text = item.Description;
            originalItemData = item;
            BigInteger price = item.price;
            if (totalPurchased > 0)
                price = (BigInteger)(item.price * Math.Pow(item.priceGrowthRate, totalPurchased));
            priceText.text = price.ToString("N0");
            purchaseButton.interactable = true;
            purchaseButton.onClick.AddListener(() => onPurchase(item));

            if (totalPurchased > 0)
                quantityText.text = "x" + totalPurchased.ToString("N0");

            // Render a 3D dice preview thumbnail
            if (dicePreviewImage != null && DicePreviewRenderer.Instance != null)
            {
                Texture preview = DicePreviewRenderer.Instance.GetPreview(item);
                if (preview != null)
                    dicePreviewImage.texture = preview;
            }
        }

        /// <summary>
        /// Updates the displayed quantity and price for this shop item based on the total number of times it has been purchased.
        /// </summary>
        /// <param name="totalPurchased">The total number of times this item has been purchased.</param>
        public void UpdateItemQuantity(int totalPurchased)
        {
            if (quantityText != null)
                quantityText.text = "x" + totalPurchased.ToString("N0");
        }

        /// <summary>
        /// Updates the displayed price for this shop item based on the total number of times it has been purchased and the price increase per purchase defined in the original item data.
        /// </summary>
        /// <param name="newPrice">The new price to display for this shop item.</param>
        public void UpdateItemPrice(BigInteger newPrice)
        {
            if (priceText != null)
                priceText.text = newPrice.ToString("N0");
        }
    }
}