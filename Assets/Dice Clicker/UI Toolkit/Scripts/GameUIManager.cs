using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace MyGame
{
    
    [RequireComponent(typeof(UIDocument))]
    public class GameUIManager : MonoBehaviour
    {
        // Singleton instance
        public static GameUIManager Instance { get; private set; }

        // UI refs
        [SerializeField] private VisualTreeAsset diceShopItemTemplate;
        [SerializeField] private VisualTreeAsset autoClickShopItemTemplate;
        [SerializeField] private ShopItem[] shopItems;

        // Animation settings for button clicks
        [SerializeField] private float buttonClickFadeDuration = 0.05f;
        private Color buttonClickFadeColor = new Color(111f/255f, 168f/255f, 56f/255f, 1f);  // #6FA838 - slightly darker green 

        // Shop data
        public ShopItem[] ShopItems;
        private ShopItem[] autoClickShopItems;
        private Dictionary<int, VisualElement> _autoClickShopItemUIMap = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, VisualElement> _diceShopItemUIMap = new Dictionary<int, VisualElement>();

        // Unity UI-Toolkit element IDs
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
        private readonly string ElementID_RewardedAdPrompt = "RAdPopupIcon";
        private readonly string ElementID_RewardedAdConfirmationWindow = "RAdConfirmationWindow";
        private readonly string ElementID_RewardedAdAcceptButton = "Accept";
        private readonly string ElementID_RewardedAdCancelButton = "Decline";
        private readonly string ElementID_VideoSettingDropDown = "VideoSettingDropDown";
        private readonly string ElementID_SFXToggle = "SFXToggle";
        private readonly string ElementID_MusicToggle = "MusicToggle";

        // Shop UI vars
        public string CurrentlyOpenShop = null;
        private readonly Color Color_ShopButtonActive = new Color32(0xE9, 0xCA, 0x53, 0xFF);
        private readonly Color Color_ShopButtonInactive = new Color32(0xBC, 0xBC, 0xBC, 0x00);
        private Dictionary<string, string> _windowToButtonMap;

        // Score UI vars
        private Label ScoreCounterText;
        private BigInteger displayedScore = BigInteger.Zero;

        // Settings UI vars
        private bool settings_SFXEnabled = true;
        private bool settings_musicEnabled = true;
        private int settings_VideoQualityLevel = 2;

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

            var root = GetComponent<UIDocument>().rootVisualElement;

            // Check required refs

            // Dice Shop
            if (diceShopItemTemplate == null)
            {
                Debug.LogError("GameUIManager: diceShopItemTemplate is not assigned.", this);
            }
            ListView diceShopListView = root.Q<ListView>(ElementID_DiceShopListView);
            if (diceShopListView == null)
            {
                Debug.LogError("GameUIManager: Could not find DiceShopListView in the UI document.", this);
                return;
            }
            Button diceShopButton = root.Q<Button>(ElementID_DiceShopButton);
            if (diceShopButton == null)
            {
                Debug.LogError("GameUIManager: Could not find DiceShopButton in the UI document.", this);
            }

            // Auto-Click Shop
            if(autoClickShopItemTemplate == null)
            {
                Debug.LogError("GameUIManager: autoClickShopItemTemplate is not assigned.", this);
            }
            ListView autoClickShopListView = root.Q<ListView>(ElementID_AutoClickShopListView);
            if (autoClickShopListView == null)
            {
                Debug.LogError("GameUIManager: Could not find AutoClickShopListView in the UI document.", this);
                return;
            }
            Button autoClickShopButton = root.Q<Button>(ElementID_AutoClickShopButton);
            if (autoClickShopButton == null)
            {
                Debug.LogError("GameUIManager: Could not find AutoClickShopButton in the UI document.", this);
            }

            // World Shop
            Button worldShopButton = root.Q<Button>(ElementID_WorldShopButton);
            if (worldShopButton == null)
            {
                Debug.LogError("GameUIManager: Could not find WorldShopButton in the UI document.", this);
            }

            // Settings
            Button settingsButton = root.Q<Button>(ElementID_SettingsButton);
            if (settingsButton == null)  
            {
                Debug.LogError("GameUIManager: Could not find SettingsButton in the UI document.", this);
            }
            
            // Global
            Button rollButton = root.Q<Button>(ElementID_RollButton);
            if (rollButton == null)
            {
                Debug.LogError("GameUIManager: Could not find RollButton in the UI document.", this);
            }
            ScoreCounterText = root.Q<Label>(ElementID_ScoreText);
            if (ScoreCounterText == null)
            {
                Debug.LogError("GameUIManager: Could not find ScoreLabel in the UI document.", this);
            }

            ShopItems = shopItems.OrderBy(item => item.Id).ToArray();
            if(ShopItems == null || ShopItems.Length == 0)
            {
                Debug.LogError("GameUIManager: shopItems array is not assigned or empty.", this);
                return;
            }

            // --- Init Dice Shop --- 

            diceShopListView.selectionType = SelectionType.None;
            diceShopListView.makeItem = () => diceShopItemTemplate.Instantiate();
            diceShopListView.bindItem = (element, index) =>
            {
                element.style.height = 300;
                // Set base Item values        
                ShopItem shopItem = ShopItems[index];
                if (shopItem == null)
                {
                    Debug.LogError($"GameUIManager: shopItem at index {index} is null.", this);
                    return;
                }

                Label priceLabel = element.Q<Label>(ElementID_PriceValue);
                Label nameLabel = element.Q<Label>(ElementID_ItemName);
                Label qtyLabel = element.Q<Label>(ElementID_QuantityValue);
                Image thumbnail = element.Q<Image>(ElementID_DicePreviewThumbnail);

                if (priceLabel == null)
                {
                    Debug.LogError($"GameUIManager: PriceValue label not found in shop item UI at index {index}.", this);
                }
                else
                {
                    priceLabel.text = GameManager.FormatBigInteger(BigInteger.Parse(shopItem.price));
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
                if (thumbnail == null)
                {
                    Debug.LogError($"GameUIManager: DicePreview image not found in shop item UI at index {index}.", this);
                }
                else
                {
                    Texture tex = DicePreviewRenderer.Instance.GetPreview(shopItem);
                    if (tex != null)
                    {
                        thumbnail.image = tex;
                    }
                    else
                    {
                        Debug.LogError($"GameUIManager: No preview texture found for shop item {shopItem.Name} (ID: {shopItem.Id}).", this);
                    }
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
                    purchaseButton.clicked += () =>
                    {
                        StartCoroutine(AnimateButtonClickFade(purchaseButton));
                        PurchaseDiceShopItem(shopItem);
                    };
                }
            };
            diceShopListView.itemsSource = ShopItems;


            // --- Auto-Click Shop ---
            autoClickShopItems  = CreateAutoClickShopItemList();
            autoClickShopListView.selectionType = SelectionType.None;
            autoClickShopListView.makeItem = () => autoClickShopItemTemplate.Instantiate();
            autoClickShopListView.bindItem = (element, index) =>
            {
                element.style.height = 300;
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
                    priceLabel.text = GameManager.FormatBigInteger(shopItem.GetAutoClickPrice(0));
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
                        thumbnail.image = tex;
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
                    UpdateACShopItemUI_Price(shopItem.Id, shopItem.GetAutoClickPrice(data.totalPurchased));
                    UpdateACShopItemUI_Quantity(shopItem.Id, data.totalPurchased);
                    if(data.totalPurchased > 0)
                        UpdateACShopItemUI_CPS(shopItem.Id, shopItem.autoClicksPerSecond * data.totalPurchased);
                }

                // Add purchase button listener
                Button purchaseButton = element.Q<Button>(ElementID_PurchaseButton);
                if (purchaseButton != null)
                {
                   purchaseButton.clicked += () =>
                   {
                       StartCoroutine(AnimateButtonClickFade(purchaseButton));
                       PurchaseAutoClickShopItem(shopItem);
                   };
                }
                else
                {
                    Debug.LogError($"GameUIManager: PurchaseButton not found for auto-click shop item ID {shopItem.Id}.", this);
                }

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
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            }
            ScrollView autoClickScrollView = autoClickShopListView.Q<ScrollView>();
            if (autoClickScrollView != null)
            {
                autoClickScrollView.style.flexGrow = 1;
                autoClickScrollView.style.width = Length.Percent(100);
                autoClickScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            }



            // Init Settings UI
            settingsData currentsettings = SaveManager.Instance.GetSettings();
            if (currentsettings != null)
            {
                settings_SFXEnabled = currentsettings.sfxEnabled;
                settings_musicEnabled = currentsettings.musicEnabled;
                settings_VideoQualityLevel = currentsettings.videoQualityID;

                AudioManager.Instance.SetSFXEnabled(settings_SFXEnabled);
                AudioManager.Instance.SetMusicEnabled(settings_musicEnabled);
                QualitySettings.SetQualityLevel(settings_VideoQualityLevel, true);
            }
            else
            {
                Debug.LogWarning("GameUIManager: No settings data found in save file. Using default settings.", this);
            }


            DropdownField videoDropdown = root.Q<DropdownField>(ElementID_VideoSettingDropDown);
            if (videoDropdown != null)
            {
                videoDropdown.choices = new List<string> { "Low", "Medium", "High" };
                videoDropdown.value = settings_VideoQualityLevel switch
                {
                    0 => "Low",
                    1 => "Medium",
                    2 => "High",
                    _ => "High"
                };
                videoDropdown.RegisterValueChangedCallback(evt =>
                {
                    Debug.Log("Video quality dropdown changed to: " + evt.newValue);
                    switch (evt.newValue)
                    {
                        case "Low":
                            settings_VideoQualityLevel = 0;
                            QualitySettings.SetQualityLevel(settings_VideoQualityLevel, true);
                            break;
                        case "Medium":
                            settings_VideoQualityLevel = 1;
                            QualitySettings.SetQualityLevel(settings_VideoQualityLevel, true);
                            break;
                        case "High":
                            settings_VideoQualityLevel = 2;
                            QualitySettings.SetQualityLevel(settings_VideoQualityLevel, true);
                            break;
                    }
                    SaveManager.Instance.UpdateVideoQuality(settings_VideoQualityLevel);
                });
            }
            else
            {
                Debug.LogError("GameUIManager: Could not find VideoSettingDropDown in the UI document.", this);
            }

            Toggle SFXToggle = root.Q<Toggle>(ElementID_SFXToggle);
            if (SFXToggle != null)
            {
                SFXToggle.value = settings_SFXEnabled;
                SFXToggle.RegisterValueChangedCallback(evt =>
                {
                    settings_SFXEnabled = evt.newValue;
                    AudioManager.Instance.SetSFXEnabled(settings_SFXEnabled);
                    SaveManager.Instance.UpdateSFXEnabled(settings_SFXEnabled);
                });
            }
            else
            {
                Debug.LogError("GameUIManager: Could not find SFXToggle in the UI document.", this);
            }

            Toggle MusicToggle = root.Q<Toggle>(ElementID_MusicToggle);
            if (MusicToggle != null)
            {
                MusicToggle.value = settings_musicEnabled;
                MusicToggle.RegisterValueChangedCallback(evt =>
                {
                    settings_musicEnabled = evt.newValue;
                    AudioManager.Instance.SetMusicEnabled(settings_musicEnabled);
                    SaveManager.Instance.UpdateMusicEnabled(settings_musicEnabled);
                });
            }
            else
            {
                Debug.LogError("GameUIManager: Could not find MusicToggle in the UI document.", this);
            }


        }

        public Material GetMaterialForDiceType(int diceType)
        {
            ShopItem shopItem = ShopItems.FirstOrDefault(item => item.Id == diceType);
            if (shopItem != null)
            {
                return shopItem.diceMaterial;
            }
            Debug.LogError($"GameUIManager: No ShopItem found for dice type {diceType}. Cannot get material.", this);
            return null;
        }

        public Material GetPipMaterialForDiceType(int diceType)
        {
            ShopItem shopItem = ShopItems.FirstOrDefault(item => item.Id == diceType);
            if (shopItem != null)
            {
                return shopItem.pipMaterial;
            }
            Debug.LogError($"GameUIManager: No ShopItem found for dice type {diceType}. Cannot get pip material.", this);
            return null;
        }

        private void RollHandle()
        {
            Debug.Log("Roll button clicked.");
            DiceManager.Instance.RollNextDice();
        }

        /// <summary>
        /// Animates a UI button with a color fade and scale effect on click using USS transitions.
        /// </summary>
        private IEnumerator AnimateButtonClickFade(Button button)
        {
            if (button == null) yield break;

            // Scale down and set to darker green
            button.style.scale = new Scale(new UnityEngine.Vector2(0.95f, 0.95f));
            button.style.backgroundColor = buttonClickFadeColor;
            
            // Wait for fade + hold at darker color
            yield return new WaitForSeconds(buttonClickFadeDuration);
            
            // Return to original - USS will handle transition 
            button.style.scale = new Scale(new UnityEngine.Vector2(1f, 1f));
            button.style.backgroundColor = StyleKeyword.Null;
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

            // Lazily init the window → button lookup
            _windowToButtonMap ??= new Dictionary<string, string>
            {
                { ElementID_DiceShopWindow,      ElementID_DiceShopButton      },
                { ElementID_AutoClickShopWindow, ElementID_AutoClickShopButton },
                { ElementID_WorldShopWindow,     ElementID_WorldShopButton     },
                { ElementID_SettingsWindow,      ElementID_SettingsButton      },
            };
 
            // Close + deactivate the currently open shop if it differs from the target
            if (CurrentlyOpenShop != null && CurrentlyOpenShop != windowID)
            {
                var currentShopWindow = root.Q(CurrentlyOpenShop);
                if (currentShopWindow != null)
                    currentShopWindow.style.bottom = new Length(-70, LengthUnit.Percent);

                SetShopButtonHighlight(root, CurrentlyOpenShop, false);
                CurrentlyOpenShop = null;
            }

            VisualElement shopWindow = root.Q(windowID);
            if (shopWindow != null)
            {
                if (CurrentlyOpenShop == windowID)
                {
                    // Close it
                    shopWindow.style.bottom = new Length(-70, LengthUnit.Percent);
                    SetShopButtonHighlight(root, windowID, false);
                    CurrentlyOpenShop = null;
                }
                else
                {
                    // Open it
                    shopWindow.style.bottom = new Length(10, LengthUnit.Percent);
                    SetShopButtonHighlight(root, windowID, true);
                    CurrentlyOpenShop = windowID;
                }
            }
            else
            {
                Debug.LogError($"GameUIManager: Could not find {windowID} in the UI document.", this);
            }
        }

        private void SetShopButtonHighlight(VisualElement root, string windowID, bool active)
        {
            if (_windowToButtonMap != null && _windowToButtonMap.TryGetValue(windowID, out string buttonID))
            {
                Button btn = root.Q<Button>(buttonID);
                if (btn != null)
                    btn.style.backgroundColor = active ? Color_ShopButtonActive : Color_ShopButtonInactive;
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
            // Sort to match the order in shopItems
            newAutoClickShopItems.Sort((a, b) => a.Id.CompareTo(b.Id));
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
            BigInteger itemPrice = item.GetAutoClickPrice(purchased);
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
            BigInteger nextPrice = item.GetAutoClickPrice(purchased + 1);
            UpdateACShopItemUI_Price(item.Id, nextPrice);
            UpdateACShopItemUI_Quantity(item.Id, purchased + 1);
            UpdateACShopItemUI_CPS(item.Id, item.autoClicksPerSecond * (purchased + 1));

            // Play purchase sound
            AudioManager.Instance.PlaySFX_ShopSuccess(0.8f, 0.1f);

            Debug.Log($"Purchased auto-click upgrade for item: {item.name}");
        }

        public int GetMultiplierForDiceType(int diceTypeId)
        {
            ShopItem item = Array.Find(shopItems, x => x.Id == diceTypeId);
            return item != null ? item.multiplier : 1;
        }
        
        public void ShowRewardedAdPrompt()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement promptButton = root.Q<VisualElement>(ElementID_RewardedAdPrompt);
            if (promptButton != null)
            {
                promptButton.RegisterCallback<ClickEvent>(OnRewardedAdPromptClicked);
                promptButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                Debug.LogError("GameUIManager: Could not find Rewarded Ad Prompt button in the UI document.", this);
            }
        }

        public void OnRewardedAdPromptClicked(ClickEvent evt)
        {
            // Implementation for handling rewarded ad prompt click
            var root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement confirmationWindow = root.Q(ElementID_RewardedAdConfirmationWindow);
            VisualElement promptButton = root.Q<VisualElement>(ElementID_RewardedAdPrompt);
            if (confirmationWindow != null)
            {
                confirmationWindow.style.display = DisplayStyle.Flex;
                Button acceptButton = confirmationWindow.Q<Button>(ElementID_RewardedAdAcceptButton);
                Button cancelButton = confirmationWindow.Q<Button>(ElementID_RewardedAdCancelButton);
                if (acceptButton != null)
                {
                    acceptButton.clicked += () =>
                    {
                        AdManager.Instance.ShowAd();
                        confirmationWindow.style.display = DisplayStyle.None;
                        promptButton.style.display = DisplayStyle.None;
                        HideRewardedAdPrompt();
                    };
                }
                else
                {
                    Debug.LogError("GameUIManager: Could not find Accept button in the Rewarded Ad Confirmation Window.", this);
                }
                if (cancelButton != null)
                {
                    cancelButton.clicked += () =>
                    {
                        confirmationWindow.style.display = DisplayStyle.None;
                    };
                }
                else
                {
                    Debug.LogError("GameUIManager: Could not find Decline button in the Rewarded Ad Confirmation Window.", this);
                }
            }
            else
            {
                Debug.LogError("GameUIManager: Could not find Rewarded Ad Confirmation Window in the UI document.", this);
            }

        }

        public void HideRewardedAdPrompt()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement promptButton = root.Q<VisualElement>(ElementID_RewardedAdPrompt);
            if (promptButton != null)
            {
                promptButton.UnregisterCallback<ClickEvent>(OnRewardedAdPromptClicked);
                promptButton.style.display = DisplayStyle.None;
            }
            else
            {
                Debug.LogError("GameUIManager: Could not find Rewarded Ad Prompt button in the UI document.", this);
            }
        }

    }
}