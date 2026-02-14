using UnityEngine;
using System;
using System.Numerics;

namespace MyGame
{

    public class DiceShopManager : MonoBehaviour
    {
        public static DiceShopManager Instance { get; private set; }
        [SerializeField] private DiceManager diceManager;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private UIDiceShopHandler uiShopHandler;


            
        [SerializeField] private ShopItem[] shopItems;
        
        public ShopItem[] ShopItems => shopItems;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple instances of DiceShopManager detected. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (diceManager == null)
            {
                Debug.LogError("DiceShopManager: diceManager is not assigned.", this);
                return;
            }
            if (saveManager == null)
            {
                Debug.LogError("DiceShopManager: saveManager is not assigned.", this);
                return;
            }
            if (gameManager == null)
            {
                Debug.LogError("DiceShopManager: gameManager is not assigned.", this);
                return;
            }
            if(uiShopHandler == null)
            {
                Debug.LogError("DiceShopManager: uiShopHandler is not assigned.", this);
                return;
            }
            if(audioManager == null)
            {
                Debug.LogError("DiceShopManager: audioManager is not assigned.", this);
                return;
            }
        }

        /// <summary>
        /// Returns the multiplier for a given dice type based on the shop item data. 
        /// If the item is not found or has an invalid multiplier, returns 1.
        /// </summary>
        /// <param name="typeId">The ID of the dice type.</param>
        /// <returns>The multiplier for the specified dice type, or 1 if not found or invalid.</returns>
        public int GetMultiplierForDiceType(int typeId)
        {
            if (shopItems == null || shopItems.Length == 0)
                return 1;

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem item = shopItems[i];
                if (item != null && item.Id == typeId)
                {
                    return item.multiplier < 1 ? 1 : item.multiplier;
                }
            }

            return 1;
        }

        /// <summary>
        /// Returns the material for a given dice type based on the shop item data.
        /// Used by DicePreviewRenderer to render the correct dice appearance in the shop UI.
        /// </summary>
        /// <param name="typeId">The ID of the dice type.</param>
        /// <returns>The material for the specified dice type, or null if not found.</returns>
        public Material GetMaterialForDiceType(int typeId)
        {
            if (shopItems == null || shopItems.Length == 0)
                return null;

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem item = shopItems[i];
                if (item != null && item.Id == typeId)
                    return item.diceMaterial;
            }

            return null;
        }

        /// <summary>
        /// Returns the pip material for a given dice type based on the shop item data.
        /// Used by DicePreviewRenderer to render the correct dice appearance in the shop UI.
        /// </summary>
        /// <param name="typeId">The ID of the dice type.</param>
        /// <returns>The pip material for the specified dice type, or null if not found.</returns>
        public Material GetPipMaterialForDiceType(int typeId)
        {
            if (shopItems == null || shopItems.Length == 0)
                return null;

            for (int i = 0; i < shopItems.Length; i++)
            {
                ShopItem item = shopItems[i];
                if (item != null && item.Id == typeId)
                    return item.pipMaterial;
            }

            return null;
        }

        /// <summary>
        /// Handles the purchase of a shop item. 
        /// Validates the purchase, updates game state, and triggers UI updates.
        /// </summary>
        /// <param name="item">The shop item to be purchased.</param>
        public void PurchaseItem(ShopItem item)
        {
            if (item == null)        
            {
                Debug.LogError("DiceShopManager: Attempted to purchase a null item.", this);
                return;
            }
            // Validate price
            BigInteger currentScore = saveManager.GetScore();

            itemData itemData = saveManager.GetItemData(item.Id);
            int purchased = itemData != null ? itemData.totalPurchased : 0;
            BigInteger itemPrice = item.GetPrice(purchased);
            if (currentScore < itemPrice)
            {
                audioManager.PlaySFX_ShopFail(0.8f);
                return;
            }

            // Deduct funds
            gameManager.AddToScore(-itemPrice);

            // Update save data — adds a level-1 die and performs cascading merges
            MergeResult result = saveManager.UnlockItem(item.Id);

            if (result.didMerge)
            {
                // A merge (possibly cascading) occurred — rebuild all dice of this type
                // from save data so the scene is in sync.
                itemData data = saveManager.GetItemData(item.Id);
                diceManager.RebuildDiceForType(item.Id, data?.diceLevels);

                foreach (int lv in result.mergedAtLevels)
                    UnityEngine.Debug.Log($"Merge! 10x Lv{lv} {item.Name} → 1x Lv{lv + 1}");
            }
            else
            {
                // No merge — just spawn the new level-1 die
                diceManager.CreateDice(item.Id, 1);
            }

            // Update Shop UI
            BigInteger nextPrice = item.GetPrice(purchased + 1);
            uiShopHandler.UpdateItemPrice(item, nextPrice);
            uiShopHandler.UpdateItemQuantity(item, purchased + 1);

            // Reveal the auto-click shop entry on first dice purchase
            if (purchased == 0 && AutoClickShopManager.Instance != null)
                AutoClickShopManager.Instance.RevealAutoClickItem(item.Id);

            // Play purchase sound
            audioManager.PlaySFX_ShopSuccess(0.8f, 0.1f);

            //  TODO:
            // Display purchase confirmation popup text
            
            UnityEngine.Debug.Log($"Purchased item: {item.name}");
        }
    }
}
