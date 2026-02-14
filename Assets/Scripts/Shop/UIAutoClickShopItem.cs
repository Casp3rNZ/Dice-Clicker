using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Numerics;

namespace MyGame
{
    /// <summary>
    /// UI element for a single auto-click upgrade entry in the clicker shop.
    /// Each entry maps to a <see cref="ShopItem"/> (dice tier).
    /// </summary>
    public class UIAutoClickShopItem : MonoBehaviour
    {
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private RawImage dicePreviewImage;
        [SerializeField] private TMP_Text cpsText;

        public void Initialize(ShopItem item, Action<ShopItem> onPurchase, int totalPurchased = 0)
        {
            if (item == null)
            {
                Debug.LogError("UIAutoClickShopItem: Initialize called with null item.");
                return;
            }
            if (purchaseButton == null || nameText == null || priceText == null || quantityText == null || cpsText == null)
            {
                Debug.LogError("UIAutoClickShopItem: One or more UI components are not assigned.", this);
                return;
            }

            nameText.text = item.Name;

            if (descriptionText != null)
                descriptionText.text = item.Description;

            BigInteger price = AutoClickShopManager.GetAutoClickPrice(item, totalPurchased);
            priceText.text = price.ToString("N0");

            purchaseButton.interactable = true;
            purchaseButton.onClick.AddListener(() => onPurchase(item));

            if (totalPurchased > 0)
                quantityText.text = "x" + totalPurchased.ToString("N0");

            UpdateCPS(item.autoClicksPerSecond * totalPurchased);


            // Show dice preview thumbnail for this tier
            if (dicePreviewImage != null && DicePreviewRenderer.Instance != null)
            {
                Texture preview = DicePreviewRenderer.Instance.GetPreview(item);
                if (preview != null)
                    dicePreviewImage.texture = preview;
            }
        }

        public void UpdateItemQuantity(int totalPurchased)
        {
            if (quantityText != null)
                quantityText.text = "x" + totalPurchased.ToString("N0");
        }

        public void UpdateItemPrice(BigInteger newPrice)
        {
            if (priceText != null)
                priceText.text = newPrice.ToString("N0");
        }

        public void UpdateCPS(double cps)
        {
            if (cpsText != null)
                cpsText.text = cps.ToString("N2") + " /S";
        }
    }
}
