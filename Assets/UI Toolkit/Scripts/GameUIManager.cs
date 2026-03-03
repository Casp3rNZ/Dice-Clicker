using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;


namespace MyGame
{
    
    [RequireComponent(typeof(UIDocument))]
    public class GameUIManager : MonoBehaviour
    {

        public static GameUIManager Instance { get; private set; }
        [SerializeField] private VisualTreeAsset diceShopItemTemplate;
        [SerializeField] private VisualTreeAsset autoClickShopItemTemplate;
        [SerializeField] private ShopItem[] shopItems;
        private ShopItem[] autoClickShopItems;
        private Dictionary<int, VisualElement> _autoClickShopItemUIMap = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, VisualElement> _diceShopItemUIMap = new Dictionary<int, VisualElement>();
        private readonly string ElementID_PriceValue = "PriceValue";
        private readonly string ElementID_ItemName = "ItemName";
        private readonly string ElementID_QuantityValue = "QtyValue";
        private readonly string ElementID_PurchaseButton = "PurchaseButton";
        private readonly string ElementID_ScoreText = "ScoreLabel";
        private readonly string ElementID_DiceShopWindow = "DiceShopWindow";
        private readonly string ElementID_DiceShopListView = "DiceShopListView";
        private readonly string ElementID_DiceShopButton = "DiceShopButton";

        private readonly string ElementID_AutoClickShopWindow = "AutoClickShopWindow";
        private readonly string ElementID_AutoClickShopListView = "AutoClickShopListView";
        private readonly string ElementID_AutoClickShopButton = "AutoClickShopButton";
        private readonly string ElementID_CPSValue = "CPSValue";
        private readonly string ElementID_RollButton = "RollButton";
        

        private readonly string ElementID_WorldShopWindow = "WorldShopWindow";
        private readonly string ElementID_WorldShopButton = "WorldShopButton";
        private readonly string ElementID_SettingsWindow = "SettingsWindow";
        private readonly string ElementID_SettingsButton = "SettingsButton";

        private readonly string ElementID_DicePreviewThumbnail = "DicePreview";

        public static string CurrentlyOpenShop = null;
        private Label ScoreCounterText;
        private BigInteger displayedScore = BigInteger.Zero;


        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            // Check required refs

            // Dice Shop
            if (diceShopItemTemplate == null)
            {
                Debug.LogError("GameUIManager: diceShopItemTemplate is not assigned.", this);
            }
            ListView diceShopListView = GetComponent<UIDocument>().rootVisualElement.Q<ListView>(ElementID_DiceShopListView);
            if (diceShopListView == null)
            {
                Debug.LogError("GameUIManager: Could not find DiceShopListView in the UI document.", this);
                return;
            }
            Button diceShopButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>(ElementID_DiceShopButton);
            if (diceShopButton == null)
            {
                Debug.LogError("GameUIManager: Could not find DiceShopButton in the UI document.", this);
            }

            // Auto-Click Shop
            if(autoClickShopItemTemplate == null)
            {
                Debug.LogError("GameUIManager: autoClickShopItemTemplate is not assigned.", this);
            }
            ListView autoClickShopListView = GetComponent<UIDocument>().rootVisualElement.Q<ListView>(ElementID_AutoClickShopListView);
            if (autoClickShopListView == null)
            {
                Debug.LogError("GameUIManager: Could not find AutoClickShopListView in the UI document.", this);
                return;
            }
            Button autoClickShopButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>(ElementID_AutoClickShopButton);
            if (autoClickShopButton == null)
            {
                Debug.LogError("GameUIManager: Could not find AutoClickShopButton in the UI document.", this);
            }

            // World Shop
            Button worldShopButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>(ElementID_WorldShopButton);
            if (worldShopButton == null)
            {
                Debug.LogError("GameUIManager: Could not find WorldShopButton in the UI document.", this);
            }

            // Settings
            Button settingsButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>(ElementID_SettingsButton);
            if (settingsButton == null)  
            {
                Debug.LogError("GameUIManager: Could not find SettingsButton in the UI document.", this);
            }
            
            // Global
            Button rollButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>(ElementID_RollButton);
            if (rollButton == null)
            {
                Debug.LogError("GameUIManager: Could not find RollButton in the UI document.", this);
            }
            ScoreCounterText = GetComponent<UIDocument>().rootVisualElement.Q<Label>(ElementID_ScoreText);
            if (ScoreCounterText == null)
            {
                Debug.LogError("GameUIManager: Could not find ScoreLabel in the UI document.", this);
            }

            if(shopItems == null || shopItems.Length == 0)
            {
                Debug.LogError("GameUIManager: shopItems array is not assigned or empty.", this);
                return;
            }

            // --- Init Dice Shop --- 

            diceShopListView.selectionType = SelectionType.None;
            diceShopListView.makeItem = () => diceShopItemTemplate.Instantiate();

            diceShopListView.bindItem = (element, index) =>
            {
                element.style.height = 250;
                // Set base Item values        
                ShopItem shopItem = shopItems[index];
                if (shopItem == null)
                {
                    Debug.LogError($"GameUIManager: shopItem at index {index} is null.", this);
                    return;
                }

                Label priceLabel = element.Q<Label>(ElementID_PriceValue);
                Label nameLabel = element.Q<Label>(ElementID_ItemName);
                Label qtyLabel = element.Q<Label>(ElementID_QuantityValue);

                if (priceLabel == null)
                {
                    Debug.LogError($"GameUIManager: PriceValue label not found in shop item UI at index {index}.", this);
                }
                else
                {
                    priceLabel.text = shopItem.price.ToString("N0");
                }

                if (nameLabel == null)
                {
                    Debug.LogError($"GameUIManager: ItemName label not found in shop item UI at index {index}.", this);
                }
                else
                {
                    nameLabel.text = shopItem.Name;
                }

                if (qtyLabel == null)
                {
                    Debug.LogError($"GameUIManager: QtyValue label not found in shop item UI at index {index}.", this);
                }
                else
                {
                    qtyLabel.text = "x0";
                }

                _diceShopItemUIMap[shopItem.Id] = element;

                // If save file valid, update values
                itemData data = SaveManager.Instance.GetItemData(shopItem.Id);
                if (data != null)
                {
                    UpdateDShopItem_Price(shopItem.Id, shopItem.GetPrice(data.totalPurchased));
                    UpdateDShopItem_Quantity(shopItem.Id, data.totalPurchased);
                }

                // Add button listener
                Button purchaseButton = element.Q<Button>(ElementID_PurchaseButton);
                if (purchaseButton == null)
                {
                    Debug.LogError($"GameUIManager: PurchaseButton not found in shop item UI at index {index}.", this);
                }
                else
                {
                    purchaseButton.clicked += () => PurchaseDiceShopItem(shopItem);
                }
            };
            diceShopListView.itemsSource = shopItems;


            // --- Auto-Click Shop ---
            autoClickShopItems  = CreateAutoClickShopItemList();
            autoClickShopListView.selectionType = SelectionType.None;
            autoClickShopListView.makeItem = () => autoClickShopItemTemplate.Instantiate();
            autoClickShopListView.bindItem = (element, index) =>
            {
                element.style.height = 250;
                Debug.Log("Binding AutoClickShop item at index: " + index);
                // Set base Item values
                ShopItem shopItem = autoClickShopItems[index];
                if (shopItem == null)
                {
                    Debug.LogError($"GameUIManager: shopItem at index {index} is null.", this);
                    return;
                }

                Label priceLabel = element.Q<Label>(ElementID_PriceValue);
                Label nameLabel = element.Q<Label>(ElementID_ItemName);
                Label qtyLabel = element.Q<Label>(ElementID_QuantityValue);
                Label cpsLabel = element.Q<Label>(ElementID_CPSValue);
                Image thumbnail = element.Q<Image>(ElementID_DicePreviewThumbnail);

                if (priceLabel == null)
                {
                    Debug.LogError($"GameUIManager: PriceValue label not found in auto-click shop item UI at index {index}.", this);
                }
                else
                {
                    priceLabel.text = AutoClickShopManager.GetAutoClickPrice(shopItem, 0).ToString();
                }
                if (nameLabel == null)
                {
                    Debug.LogError($"GameUIManager: ItemName label not found in auto-click shop item UI at index {index}.", this);
                }
                else
                {
                    nameLabel.text = shopItem.Name;
                }
                if (qtyLabel == null)
                {
                    Debug.LogError($"GameUIManager: QtyValue label not found in auto-click shop item UI at index {index}.", this);
                }
                else
                {
                    qtyLabel.text = "x0";
                }
                if (cpsLabel == null)
                {
                    Debug.LogError($"GameUIManager: CPSValue label not found in auto-click shop item UI at index {index}.", this);
                }
                else
                {
                    cpsLabel.text = "0 /Sec";
                }
                if (thumbnail == null)
                {
                    Debug.LogError($"GameUIManager: DicePreview image not found in auto-click shop item UI at index {index}.", this);
                }
                else
                {
                    Texture tex = DicePreviewRenderer.Instance.GetPreview(shopItem);
                    if (tex != null)
                    {
                        thumbnail.sprite = Sprite.Create((Texture2D)tex, new Rect(0, 0, tex.width, tex.height), new UnityEngine.Vector2(0.5f, 0.5f));
                    }
                    else
                    {
                        Debug.LogError($"GameUIManager: No preview texture found for shop item {shopItem.Name} (ID: {shopItem.Id}).", this);
                    }
                   
                }

                _autoClickShopItemUIMap[shopItem.Id] = element;

                // If save file valid, update values
                itemData data = SaveManager.Instance.GetItemData(shopItem.Id);
                if (data != null)
                {
                    UpdateACShopItemUI_Price(shopItem.Id, AutoClickShopManager.GetAutoClickPrice(shopItem, data.totalPurchased));
                    UpdateACShopItemUI_Quantity(shopItem.Id, data.totalPurchased);
                    if(data.totalPurchased > 0)
                        UpdateACShopItemUI_CPS(shopItem.Id, shopItem.autoClicksPerSecond * data.totalPurchased);
                }

                // Add purchase button listener
                Button purchaseButton = element.Q<Button>(ElementID_PurchaseButton);
                if (purchaseButton != null)
                {
                   purchaseButton.clicked += () => PurchaseAutoClickShopItem(shopItem);
                }
                else
                {
                    Debug.LogError($"GameUIManager: PurchaseButton not found for auto-click shop item ID {shopItem.Id}.", this);
                }

                Debug.Log($"Bound auto-click shop item: {shopItem.Name} (ID: {shopItem.Id})") ;
            };
            // register source after defining callbacks to avoid null reference errors
            autoClickShopListView.itemsSource = autoClickShopItems;

            // Register button listener for opening the dice shop window
            diceShopButton.clicked += () => ToggleShop(ElementID_DiceShopWindow);
            autoClickShopButton.clicked += () => ToggleShop(ElementID_AutoClickShopWindow);
            worldShopButton.clicked += () => ToggleShop(ElementID_WorldShopWindow);
            settingsButton.clicked += () => ToggleShop(ElementID_SettingsWindow);

            rollButton.clicked += () => RollHandle();

            // fix scroll view sizing issues
            ScrollView scrollView = diceShopListView.Q<ScrollView>();
            if (scrollView != null)
            {
                scrollView.style.flexGrow = 1;
                scrollView.style.width = Length.Percent(100);
            }
            ScrollView autoClickScrollView = autoClickShopListView.Q<ScrollView>();
            if (autoClickScrollView != null)
            {
                autoClickScrollView.style.flexGrow = 1;
                autoClickScrollView.style.width = Length.Percent(100);
            }

        }

        private void RollHandle()
        {
            Debug.Log("Roll button clicked.");
            DiceManager.Instance.RollNextDice();
        }

        private void Update()
        {
            // Update UI score
            BigInteger score = SaveManager.Instance != null ? SaveManager.Instance.GetScore() : BigInteger.Zero;
            if (ScoreCounterText != null)
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
                    AudioManager.Instance.PlaySFX_ScoreCounterTick(0.05f);
                    BigInteger gap = score - displayedScore;
                    displayedScore += BigInteger.One + gap / 10;
                    if (displayedScore > score) displayedScore = score;
                }
                ScoreCounterText.text = GameManager.FormatBigInteger(displayedScore);
            }
        }

        public void UpdateDShopItem_Price(int itemId, BigInteger newPrice)
        {
            if (_diceShopItemUIMap.TryGetValue(itemId, out VisualElement itemUI))
            {
                Label priceLabel = itemUI.Q<Label>(ElementID_PriceValue);
                if (priceLabel != null)
                {
                    priceLabel.text = GameManager.FormatBigInteger(newPrice);
                }
                else
                {
                    Debug.LogError($"GameUIManager: PriceValue label not found for item ID {itemId}.", this);
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: No UI element found for item ID {itemId}.", this);
            }
        }

        public void UpdateDShopItem_Quantity(int itemId, int newQuantity)
        {
            if (_diceShopItemUIMap.TryGetValue(itemId, out VisualElement itemUI))
            {
                Label quantityLabel = itemUI.Q<Label>(ElementID_QuantityValue);
                if (quantityLabel != null)
                {
                    quantityLabel.text = $"x{newQuantity}";
                }
                else
                {
                    Debug.LogError($"GameUIManager: QuantityValue label not found for item ID {itemId}.", this);
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: No UI element found for item ID {itemId}.", this);
            }
        }

        private void ToggleShop(string windowID)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Close currently open shop if it's not the one being toggled
            if (CurrentlyOpenShop != null && CurrentlyOpenShop != windowID)
            {
                var currentShopWindow = root.Q(CurrentlyOpenShop);
                if (currentShopWindow != null)
                    currentShopWindow.style.bottom = new Length(-75, LengthUnit.Percent);
                CurrentlyOpenShop = null;
            }

            VisualElement shopWindow = root.Q(windowID);
            if (shopWindow != null)
            {
                if (CurrentlyOpenShop == windowID)
                {
                    shopWindow.style.bottom = new Length(-75, LengthUnit.Percent);
                    CurrentlyOpenShop = null;
                }
                else
                {
                    shopWindow.style.bottom = 0;
                    CurrentlyOpenShop = windowID;
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: Could not find {windowID} in the UI document.", this);
            }

        }

        /// <summary>
        /// Handles the purchase of a Dice shop item. 
        /// Validates the purchase, updates game state, and triggers UI updates.
        /// </summary>
        /// <param name="item">The shop item to be purchased.</param>
        public void PurchaseDiceShopItem(ShopItem item)
        {
            if (item == null)        
            {
                Debug.LogError("GameUIManager: Attempted to purchase a null item.", this);
                return;
            }
            // Validate price
            BigInteger currentScore = SaveManager.Instance.GetScore();

            itemData itemData = SaveManager.Instance.GetItemData(item.Id);
            int purchased = itemData != null ? itemData.totalPurchased : 0;
            BigInteger itemPrice = item.GetPrice(purchased);
            if (currentScore < itemPrice)
            {
                AudioManager.Instance.PlaySFX_ShopFail(0.8f);
                return;
            }

            // Deduct funds
            GameManager.Instance.AddToScore(-itemPrice);
            // Update save data — adds a level-1 die and performs cascading merges
            MergeResult result = SaveManager.Instance.UnlockItem(item.Id);
            if (result.didMerge)
            {
                // A merge (possibly cascading) occurred — rebuild all dice of this type
                // from save data so the scene is in sync.
                itemData data = SaveManager.Instance.GetItemData(item.Id);
                DiceManager.Instance.RebuildDiceForType(item.Id, data.diceLevels);

                foreach (int lv in result.mergedAtLevels)
                    Debug.Log($"Merge! 10x Lv{lv} {item.Name} → 1x Lv{lv + 1}");
            }
            else
            {
                // No merge — just spawn the new level-1 die
                DiceManager.Instance.CreateDice(item.Id, 1);
            }

            // Update Shop UI
            BigInteger nextPrice = item.GetPrice(purchased + 1);

            UpdateDShopItem_Price(item.Id, nextPrice);
            UpdateDShopItem_Quantity(item.Id, purchased + 1);

            // Play purchase sound
            AudioManager.Instance.PlaySFX_ShopSuccess(0.8f, 0.1f);

            //  TODO:
            // Display purchase confirmation popup text
            
            Debug.Log($"Purchased item: {item.name}");
        }
    
        private ShopItem[] CreateAutoClickShopItemList()
        {
            List<ShopItem> newAutoClickShopItems = new List<ShopItem>();
            GameSaveData saveData = SaveManager.Instance.GetAllCurrentData();
            foreach (itemData item in saveData.unlockedItemIds)
            {
                ShopItem shopItem = Array.Find(shopItems, x => x.Id == item.itemId);
                if (shopItem != null)
                {
                    newAutoClickShopItems.Add(shopItem);
                }
            }
            return newAutoClickShopItems.ToArray();
        }

        public void UpdateACShopItemUI_Price(int itemId, BigInteger newPrice)
        {
            // Unlike the Dice shop, Auto-click upgrades are dynamically populated based on the currently unlocked dice,
            // and cannot be indexed by ShopItem.Id alone.
            VisualElement itemUI = null;
            foreach (var kvp in _autoClickShopItemUIMap)
            {
                if (kvp.Key == itemId)
                {
                    itemUI = kvp.Value;
                    break;
                }
            }
            if (itemUI != null)
            {
                Label priceLabel = itemUI.Q<Label>(ElementID_PriceValue);
                if (priceLabel != null)
                {
                    priceLabel.text = GameManager.FormatBigInteger(newPrice);
                }
                else
                {
                    Debug.LogError($"GameUIManager: PriceValue label not found for auto-click shop item ID {itemId}.", this);
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: No UI element found for auto-click shop item ID {itemId}.", this);
            }
        }

        public void UpdateACShopItemUI_Quantity(int itemId, int newQuantity)
        {
            VisualElement itemUI = null;
            foreach (var kvp in _autoClickShopItemUIMap)
            {
                if (kvp.Key == itemId)
                {
                    itemUI = kvp.Value;
                    break;
                }
            }
            if (itemUI != null)
            {
                Label quantityLabel = itemUI.Q<Label>(ElementID_QuantityValue);
                if (quantityLabel != null)
                {
                    quantityLabel.text = $"x{newQuantity}";
                }
                else
                {
                    Debug.LogError($"GameUIManager: QuantityValue label not found for auto-click shop item ID {itemId}.", this);
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: No UI element found for auto-click shop item ID {itemId}.", this);
            }
        }

        public void UpdateACShopItemUI_CPS(int itemId, double newCPS)
        {
            VisualElement itemUI = null;
            foreach (var kvp in _autoClickShopItemUIMap)
            {
                if (kvp.Key == itemId)
                {
                    itemUI = kvp.Value;
                    break;
                }
            }
            if (itemUI != null)
            {
                Label cpsLabel = itemUI.Q<Label>(ElementID_CPSValue);
                if (cpsLabel != null)
                {
                    cpsLabel.text = newCPS.ToString("N2") + " /Sec";
                }
                else
                {
                    Debug.LogError($"GameUIManager: CPSValue label not found for auto-click shop item ID {itemId}.", this);
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: No UI element found for auto-click shop item ID {itemId}.", this);
            }
        }

        private void PurchaseAutoClickShopItem(ShopItem item)
        {
            if (item == null)        
            {
                Debug.LogError("GameUIManager: Attempted to purchase a null auto-click shop item.", this);
                return;
            }
            // Validate price
            BigInteger currentScore = SaveManager.Instance.GetScore();

            itemData itemData = SaveManager.Instance.GetItemData(item.Id);
            int purchased = itemData != null ? itemData.totalPurchased : 0;
            BigInteger itemPrice = AutoClickShopManager.GetAutoClickPrice(item, purchased);
            if (currentScore < itemPrice)
            {
                AudioManager.Instance.PlaySFX_ShopFail(0.8f);
                return;
            }

            // Deduct funds
            GameManager.Instance.AddToScore(-itemPrice);
            // Update save data
            SaveManager.Instance.RecordAutoClickPurchase(item.Id);
            // Update auto-clicker runtime rate for this dice tier
            if (AutoClickerManager.Instance != null)
                AutoClickerManager.Instance.RefreshTier(item.Id, item.autoClicksPerSecond * (purchased + 1));
            // Update Shop UI
            BigInteger nextPrice = AutoClickShopManager.GetAutoClickPrice(item, purchased + 1);
            UpdateACShopItemUI_Price(item.Id, nextPrice);
            UpdateACShopItemUI_Quantity(item.Id, purchased + 1);
            UpdateACShopItemUI_CPS(item.Id, item.autoClicksPerSecond * (purchased + 1));

            // Play purchase sound
            AudioManager.Instance.PlaySFX_ShopSuccess(0.8f, 0.1f);

            Debug.Log($"Purchased auto-click upgrade for item: {item.name}");
        }


    }
}