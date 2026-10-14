using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    public enum VendorMode
    {
        Buy,
        Sell
    }

    /// <summary>
    /// Master UI controller for The Merchant interface.
    /// Manages:
    /// - Two operational tabs: [BUY SEEDS] and [SELL CROPS].
    /// - BUY MODE: Displays rotating 2-minute stock of seeds from MerchantShopManager with live countdown timer.
    /// - SELL MODE: Displays player's harvested goods across inventory with dynamic total payouts.
    /// - Quantity controls (slider, +/- buttons, max/all).
    /// - Purchase and sale validations with instant feedback toasts.
    /// - Real-time synchronization of player coins and inventory data.
    /// </summary>
    public class VendorUI : MonoBehaviour
    {
        public static VendorUI Instance { get; private set; }

        [Header("Window Root")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text windowTitleText;

        [Header("Currency Display")]
        [SerializeField] private Text playerCoinsText;
        [SerializeField] private Image coinIcon;

        [Header("Mode Navigation Tabs")]
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Image buyTabBg;
        [SerializeField] private Image sellTabBg;
        [SerializeField] private Text buyTabText;
        [SerializeField] private Text sellTabText;

        [Header("Header Info in Catalog Column")]
        [SerializeField] private Text listHeaderTitleText;
        [SerializeField] private Text restockTimerText;
        [SerializeField] private Button rerollStockButton; // Optional / Debug button

        [Header("List Container")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private Text emptyStateText;

        [Header("Selected Item & Transaction Controls")]
        [SerializeField] private GameObject selectionDetailsGroup;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private Text selectedNameText;
        [SerializeField] private Text selectedRarityText;
        [SerializeField] private Text selectedUnitPriceText;
        [SerializeField] private Text selectedStockOrOwnedText;

        [Header("Quantity Controls")]
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button maxQuantityButton;
        [SerializeField] private Slider quantitySlider;
        [SerializeField] private Text quantityDisplayText;

        [Header("Payout & Confirmation")]
        [SerializeField] private Text totalCalculationText;
        [SerializeField] private Button confirmActionButton;
        [SerializeField] private Text confirmActionButtonText;

        [Header("Feedback Toast")]
        [SerializeField] private Text feedbackText;
        [SerializeField] private float feedbackDuration = 3.0f;

        // Internal State
        private bool isOpen = false;
        private int openedFrame = -1;
        private int closedFrame = -1;
        private VendorMode currentMode = VendorMode.Buy;
        private ItemData currentSelectedItem = null;
        private MerchantStockOffer currentSelectedOffer = null;
        private int currentOfferIndex = -1;
        private int currentSelectedQuantity = 1;
        private int maxAvailableQuantity = 1;
        private Coroutine feedbackCoroutine;

        // Reusable list of row instances
        private List<VendorItemRowUI> spawnedRows = new List<VendorItemRowUI>();

        public bool IsOpen => isOpen;
        public int ClosedFrame => closedFrame;
        public VendorMode CurrentMode => currentMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseVendor);

            if (confirmActionButton != null)
                confirmActionButton.onClick.AddListener(OnConfirmActionClicked);

            if (minusButton != null)
                minusButton.onClick.AddListener(OnMinusClicked);

            if (plusButton != null)
                plusButton.onClick.AddListener(OnPlusClicked);

            if (maxQuantityButton != null)
                maxQuantityButton.onClick.AddListener(OnMaxQuantityClicked);

            if (quantitySlider != null)
                quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);

            EnsureRuntimeTabs();

            if (buyTabButton != null)
                buyTabButton.onClick.AddListener(() => SetMode(VendorMode.Buy));

            if (sellTabButton != null)
                sellTabButton.onClick.AddListener(() => SetMode(VendorMode.Sell));

            if (rerollStockButton != null)
                rerollStockButton.onClick.AddListener(OnRerollStockClicked);

            if (modalRoot != null)
                modalRoot.SetActive(false);

            if (feedbackText != null)
                feedbackText.text = string.Empty;
        }

        private void EnsureRuntimeTabs()
        {
            if (buyTabButton != null && sellTabButton != null) return;
            if (modalRoot == null) return;

            Transform cardTrans = modalRoot.transform.Find("VendorMainCard");
            if (cardTrans == null && modalRoot.transform.childCount > 0)
                cardTrans = modalRoot.transform.GetChild(0);
            if (cardTrans == null) return;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Look for existing buttons first
            if (buyTabButton == null)
            {
                var b = cardTrans.Find("BuyTabButton") ?? cardTrans.Find("RuntimeTabBar/BuyTabButton");
                if (b != null) buyTabButton = b.GetComponent<Button>();
            }
            if (sellTabButton == null)
            {
                var s = cardTrans.Find("SellTabButton") ?? cardTrans.Find("RuntimeTabBar/SellTabButton");
                if (s != null) sellTabButton = s.GetComponent<Button>();
            }

            if (buyTabButton != null && sellTabButton != null) return;

            // Create Tab Bar Container
            GameObject tabContainer = new GameObject("RuntimeTabBar");
            tabContainer.transform.SetParent(cardTrans, false);
            RectTransform tabRect = tabContainer.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f);
            tabRect.anchorMax = new Vector2(0.58f, 1f);
            tabRect.pivot = new Vector2(0f, 1f);
            tabRect.anchoredPosition = new Vector2(24f, -46f);
            tabRect.sizeDelta = new Vector2(-48f, 32f);

            // Buy Tab Button
            GameObject buyObj = new GameObject("BuyTabButton");
            buyObj.transform.SetParent(tabContainer.transform, false);
            RectTransform bRect = buyObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0f);
            bRect.anchorMax = new Vector2(0.48f, 1f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            buyTabBg = buyObj.AddComponent<Image>();
            buyTabBg.color = new Color(0.20f, 0.45f, 0.25f);
            buyTabButton = buyObj.AddComponent<Button>();

            GameObject buyTxtObj = new GameObject("Text");
            buyTxtObj.transform.SetParent(buyObj.transform, false);
            RectTransform btr = buyTxtObj.AddComponent<RectTransform>();
            btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one;
            btr.offsetMin = Vector2.zero; btr.offsetMax = Vector2.zero;
            buyTabText = buyTxtObj.AddComponent<Text>();
            buyTabText.font = defaultFont;
            buyTabText.fontSize = 13;
            buyTabText.fontStyle = FontStyle.Bold;
            buyTabText.alignment = TextAnchor.MiddleCenter;
            buyTabText.color = Color.white;
            buyTabText.text = "BUY SEEDS";

            // Sell Tab Button
            GameObject sellObj = new GameObject("SellTabButton");
            sellObj.transform.SetParent(tabContainer.transform, false);
            RectTransform sRect = sellObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.52f, 0f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            sellTabBg = sellObj.AddComponent<Image>();
            sellTabBg.color = new Color(0.18f, 0.16f, 0.14f);
            sellTabButton = sellObj.AddComponent<Button>();

            GameObject sellTxtObj = new GameObject("Text");
            sellTxtObj.transform.SetParent(sellObj.transform, false);
            RectTransform str = sellTxtObj.AddComponent<RectTransform>();
            str.anchorMin = Vector2.zero; str.anchorMax = Vector2.one;
            str.offsetMin = Vector2.zero; str.offsetMax = Vector2.zero;
            sellTabText = sellTxtObj.AddComponent<Text>();
            sellTabText.font = defaultFont;
            sellTabText.fontSize = 13;
            sellTabText.fontStyle = FontStyle.Bold;
            sellTabText.alignment = TextAnchor.MiddleCenter;
            sellTabText.color = new Color(0.75f, 0.75f, 0.75f);
            sellTabText.text = "SELL CROPS";

            // Restock timer text
            if (restockTimerText == null)
            {
                GameObject timerObj = new GameObject("RestockTimerText");
                timerObj.transform.SetParent(cardTrans, false);
                RectTransform tr = timerObj.AddComponent<RectTransform>();
                tr.anchorMin = new Vector2(0f, 1f);
                tr.anchorMax = new Vector2(0.58f, 1f);
                tr.pivot = new Vector2(0f, 1f);
                tr.anchoredPosition = new Vector2(24f, -84f);
                tr.sizeDelta = new Vector2(-48f, 20f);
                restockTimerText = timerObj.AddComponent<Text>();
                restockTimerText.font = defaultFont;
                restockTimerText.fontSize = 12;
                restockTimerText.alignment = TextAnchor.MiddleLeft;
                restockTimerText.color = new Color(0.85f, 0.85f, 0.90f);
            }
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
                UpdateCoinDisplay(CurrencyManager.Instance.Coins);
            }

            if (MerchantShopManager.Instance != null)
            {
                MerchantShopManager.Instance.OnStockRestocked += OnShopRestocked;
                MerchantShopManager.Instance.OnOfferPurchased += OnShopOfferPurchased;
            }
        }

        private void Update()
        {
            if (isOpen)
            {
                // Update live 2-minute cycle timer display when in Buy mode
                if (currentMode == VendorMode.Buy && restockTimerText != null && MerchantShopManager.Instance != null)
                {
                    string timer = MerchantShopManager.Instance.GetFormattedTimer();
                    bool hasMythical = MerchantShopManager.Instance.HasMythicalInStock;
                    string tag = hasMythical ? " <color=#FF66FF><b>[★ MYTHICAL IN STOCK! ★]</b></color>" : string.Empty;
                    restockTimerText.text = $"⏱ Restocks In: <color=#FFE066><b>{timer}</b></color>{tag}";
                }

                // Guard: Do not process close shortcuts on the same frame the UI was opened
                if (Time.frameCount == openedFrame) return;

                // Close shortcut keys
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
                {
                    CloseVendor();
                }
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
            }

            if (MerchantShopManager.Instance != null)
            {
                MerchantShopManager.Instance.OnStockRestocked -= OnShopRestocked;
                MerchantShopManager.Instance.OnOfferPurchased -= OnShopOfferPurchased;
            }
        }

        public void OpenVendor()
        {
            isOpen = true;
            openedFrame = Time.frameCount;
            if (modalRoot != null) modalRoot.SetActive(true);

            if (CurrencyManager.Instance != null)
                UpdateCoinDisplay(CurrencyManager.Instance.Coins);

            SetMode(currentMode, forceRefresh: true);
        }

        public void CloseVendor()
        {
            isOpen = false;
            closedFrame = Time.frameCount;
            if (modalRoot != null) modalRoot.SetActive(false);

            currentSelectedItem = null;
            currentSelectedOffer = null;
            currentOfferIndex = -1;

            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            if (feedbackText != null) feedbackText.text = string.Empty;
        }

        public void ToggleVendor()
        {
            if (isOpen) CloseVendor();
            else OpenVendor();
        }

        public void SetMode(VendorMode mode, bool forceRefresh = false)
        {
            if (currentMode == mode && !forceRefresh) return;
            currentMode = mode;

            UpdateTabVisuals();

            currentSelectedItem = null;
            currentSelectedOffer = null;
            currentOfferIndex = -1;
            currentSelectedQuantity = 1;

            if (currentMode == VendorMode.Buy)
            {
                if (listHeaderTitleText != null) listHeaderTitleText.text = "ROTATING SEED CATALOG";
                if (rerollStockButton != null) rerollStockButton.gameObject.SetActive(true);
                RefreshBuyCatalog();
            }
            else
            {
                if (listHeaderTitleText != null) listHeaderTitleText.text = "HARVESTED CROPS (Click to Sell)";
                if (restockTimerText != null) restockTimerText.text = "Sell harvested goods for coins";
                if (rerollStockButton != null) rerollStockButton.gameObject.SetActive(false);
                RefreshSellableList();
            }
        }

        private void UpdateTabVisuals()
        {
            Color activeColor = new Color(0.28f, 0.42f, 0.65f, 1f);
            Color inactiveColor = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            Color textActive = new Color(1.0f, 0.92f, 0.55f);
            Color textInactive = new Color(0.7f, 0.72f, 0.78f);

            if (buyTabBg != null)
                buyTabBg.color = currentMode == VendorMode.Buy ? activeColor : inactiveColor;
            if (sellTabBg != null)
                sellTabBg.color = currentMode == VendorMode.Sell ? activeColor : inactiveColor;

            if (buyTabText != null)
                buyTabText.color = currentMode == VendorMode.Buy ? textActive : textInactive;
            if (sellTabText != null)
                sellTabText.color = currentMode == VendorMode.Sell ? textActive : textInactive;
        }

        private void UpdateCoinDisplay(long coins)
        {
            if (playerCoinsText != null)
            {
                playerCoinsText.text = $"{coins:N0} G";
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  BUY MODE: Rotating Seed Catalog
        // ─────────────────────────────────────────────────────────────────────────

        public void RefreshBuyCatalog()
        {
            if (MerchantShopManager.Instance == null) return;

            var offers = MerchantShopManager.Instance.CurrentOffers;
            EnsureRowCache();

            int index = 0;
            for (int i = 0; i < offers.Count; i++)
            {
                var offer = offers[i];
                if (offer == null || offer.item == null) continue;

                VendorItemRowUI row = GetOrCreateRow(index);
                int capturedIndex = i;
                row.InitializeForBuy(offer, capturedIndex == currentOfferIndex, (selectedOffer) =>
                {
                    OnBuyOfferSelected(capturedIndex, selectedOffer);
                });
                index++;
            }

            // Hide extra pre-spawned rows
            for (int i = index; i < spawnedRows.Count; i++)
            {
                spawnedRows[i].gameObject.SetActive(false);
            }

            bool hasOffers = offers.Count > 0;
            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(!hasOffers);
                if (!hasOffers && emptyStateText != null)
                    emptyStateText.text = "Merchant is currently unpacking new wares...\nPlease wait for the next stock cycle.";
            }

            if (selectionDetailsGroup != null)
                selectionDetailsGroup.SetActive(hasOffers);

            if (!hasOffers)
            {
                UpdateSelectionPanel(null, 0, isBuy: true);
            }
            else
            {
                // Auto-select valid offer if current is invalid
                if (currentOfferIndex < 0 || currentOfferIndex >= offers.Count)
                {
                    OnBuyOfferSelected(0, offers[0]);
                }
                else
                {
                    OnBuyOfferSelected(currentOfferIndex, offers[currentOfferIndex]);
                }
            }
        }

        private void OnBuyOfferSelected(int offerIndex, MerchantStockOffer offer)
        {
            if (offer == null || offer.item == null) return;

            currentOfferIndex = offerIndex;
            currentSelectedOffer = offer;
            currentSelectedItem = offer.item;

            // Highlight row
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i].gameObject.activeSelf)
                {
                    spawnedRows[i].SetSelected(i == offerIndex);
                }
            }

            UpdateSelectionPanel(offer.item, offer.availableStock, isBuy: true, unitPrice: offer.pricePerUnit);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  SELL MODE: Harvested Produce
        // ─────────────────────────────────────────────────────────────────────────

        public void RefreshSellableList()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            var sellableCrops = inventory.GetSellableCrops();
            EnsureRowCache();

            int index = 0;
            bool stillHasSelectedItem = false;

            foreach (var kvp in sellableCrops)
            {
                ItemData item = kvp.Key;
                int count = kvp.Value;

                if (item == currentSelectedItem)
                    stillHasSelectedItem = true;

                VendorItemRowUI row = GetOrCreateRow(index);
                row.Initialize(item, count, item == currentSelectedItem, OnSellItemSelected);
                index++;
            }

            // Hide unused pre-spawned rows
            for (int i = index; i < spawnedRows.Count; i++)
            {
                spawnedRows[i].gameObject.SetActive(false);
            }

            bool hasItems = sellableCrops.Count > 0;
            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(!hasItems);
                if (!hasItems && emptyStateText != null)
                    emptyStateText.text = "No sellable crops in inventory.\n\nHarvest crops from your garden\nand bring them back to sell!";
            }

            if (selectionDetailsGroup != null)
                selectionDetailsGroup.SetActive(hasItems);

            if (!hasItems)
            {
                currentSelectedItem = null;
                UpdateSelectionPanel(null, 0, isBuy: false);
            }
            else if (!stillHasSelectedItem)
            {
                using (var enumerator = sellableCrops.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        OnSellItemSelected(enumerator.Current.Key);
                    }
                }
            }
            else
            {
                int owned = inventory.CountItem(currentSelectedItem);
                UpdateSelectionPanel(currentSelectedItem, owned, isBuy: false);
            }
        }

        private void OnSellItemSelected(ItemData item)
        {
            if (item == null || PlayerInventory.Instance == null) return;

            currentSelectedItem = item;
            int owned = PlayerInventory.Instance.CountItem(item);

            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i].gameObject.activeSelf)
                {
                    spawnedRows[i].SetSelected(spawnedRows[i].Item == currentSelectedItem);
                }
            }

            currentSelectedQuantity = Mathf.Clamp(currentSelectedQuantity, 1, owned);
            UpdateSelectionPanel(currentSelectedItem, owned, isBuy: false);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Selection Card & Quantity Controls
        // ─────────────────────────────────────────────────────────────────────────

        private void UpdateSelectionPanel(ItemData item, int availableCount, bool isBuy, int unitPrice = -1)
        {
            if (item == null)
            {
                if (confirmActionButton != null) confirmActionButton.interactable = false;
                if (totalCalculationText != null) totalCalculationText.text = "0 G";
                if (selectedNameText != null) selectedNameText.text = "No Item Selected";
                if (selectedRarityText != null) selectedRarityText.text = string.Empty;
                if (selectedUnitPriceText != null) selectedUnitPriceText.text = string.Empty;
                if (selectedStockOrOwnedText != null) selectedStockOrOwnedText.text = string.Empty;
                if (selectedIcon != null) selectedIcon.enabled = false;
                if (quantityDisplayText != null) quantityDisplayText.text = "0 / 0";
                return;
            }

            if (unitPrice < 0)
                unitPrice = isBuy ? (item.buyPrice > 0 ? item.buyPrice : 20) : item.sellPrice;

            maxAvailableQuantity = availableCount;

            // In Buy mode, also clamp max by what the player can afford
            if (isBuy && unitPrice > 0 && CurrencyManager.Instance != null)
            {
                int affordable = (int)(CurrencyManager.Instance.Coins / unitPrice);
                if (affordable < maxAvailableQuantity)
                {
                    // Still show available stock, but clamp slider
                }
            }

            if (maxAvailableQuantity > 0)
                currentSelectedQuantity = Mathf.Clamp(currentSelectedQuantity, 1, maxAvailableQuantity);
            else
                currentSelectedQuantity = 0;

            if (selectedIcon != null)
            {
                selectedIcon.sprite = item.icon;
                selectedIcon.enabled = item.icon != null;
            }

            string rarityHex = ItemData.GetRarityHex(item.rarity);
            if (selectedNameText != null)
            {
                selectedNameText.text = $"<color={rarityHex}>{item.displayName}</color>";
            }

            if (selectedRarityText != null)
            {
                string tag = item.rarity == RarityTier.Mythical ? "★ MYTHICAL EXCLUSIVE ★" : $"[{item.rarity.ToString().ToUpper()}]";
                selectedRarityText.text = $"<color={rarityHex}>{tag}</color>";
            }

            if (selectedUnitPriceText != null)
            {
                string label = isBuy ? "Buy Price" : "Unit Sell Price";
                selectedUnitPriceText.text = $"{label}: <b>{unitPrice} G</b>";
            }

            if (selectedStockOrOwnedText != null)
            {
                int owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.CountItem(item) : 0;
                if (isBuy)
                {
                    string stockStr = availableCount <= 0 ? "<color=#FF5555>SOLD OUT</color>" : $"{availableCount} left";
                    selectedStockOrOwnedText.text = $"In Stock: <b>{stockStr}</b> (Owned: {owned})";
                }
                else
                {
                    selectedStockOrOwnedText.text = $"Owned in Inventory: <b>{availableCount}</b>";
                }
            }

            // Configure slider
            if (quantitySlider != null)
            {
                quantitySlider.minValue = maxAvailableQuantity > 0 ? 1 : 0;
                quantitySlider.maxValue = Mathf.Max(1, maxAvailableQuantity);
                quantitySlider.SetValueWithoutNotify(currentSelectedQuantity);
            }

            UpdateTotalCalculation(unitPrice);
        }

        private void UpdateTotalCalculation(int unitPrice = -1)
        {
            if (currentSelectedItem == null) return;

            bool isBuy = currentMode == VendorMode.Buy;
            if (unitPrice < 0)
            {
                if (isBuy && currentSelectedOffer != null)
                    unitPrice = currentSelectedOffer.pricePerUnit;
                else
                    unitPrice = isBuy ? currentSelectedItem.buyPrice : currentSelectedItem.sellPrice;
            }

            long total = (long)unitPrice * currentSelectedQuantity;

            if (quantityDisplayText != null)
            {
                quantityDisplayText.text = $"{currentSelectedQuantity} / {maxAvailableQuantity}";
            }

            if (totalCalculationText != null)
            {
                string prefix = isBuy ? "Total Cost" : "Total Payout";
                totalCalculationText.text = $"{prefix}: <b>{total:N0} G</b>";
            }

            if (confirmActionButtonText != null)
            {
                if (isBuy)
                {
                    if (maxAvailableQuantity <= 0)
                        confirmActionButtonText.text = "SOLD OUT";
                    else
                        confirmActionButtonText.text = $"BUY (x{currentSelectedQuantity}) FOR {total:N0} G";
                }
                else
                {
                    confirmActionButtonText.text = $"SELL (x{currentSelectedQuantity}) FOR {total:N0} G";
                }
            }

            if (confirmActionButton != null)
            {
                if (isBuy)
                {
                    bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= total;
                    confirmActionButton.interactable = currentSelectedQuantity > 0 && maxAvailableQuantity > 0 && canAfford;
                }
                else
                {
                    confirmActionButton.interactable = currentSelectedQuantity > 0 && maxAvailableQuantity > 0;
                }
            }
        }

        private void OnMinusClicked()
        {
            if (currentSelectedQuantity > 1)
            {
                currentSelectedQuantity--;
                if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(currentSelectedQuantity);
                UpdateTotalCalculation();
            }
        }

        private void OnPlusClicked()
        {
            if (currentSelectedQuantity < maxAvailableQuantity)
            {
                currentSelectedQuantity++;
                if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(currentSelectedQuantity);
                UpdateTotalCalculation();
            }
        }

        private void OnMaxQuantityClicked()
        {
            if (currentMode == VendorMode.Buy && currentSelectedOffer != null && CurrencyManager.Instance != null)
            {
                int affordable = currentSelectedOffer.pricePerUnit > 0
                    ? (int)(CurrencyManager.Instance.Coins / currentSelectedOffer.pricePerUnit)
                    : maxAvailableQuantity;
                currentSelectedQuantity = Mathf.Clamp(Mathf.Min(maxAvailableQuantity, affordable), 1, maxAvailableQuantity);
            }
            else
            {
                currentSelectedQuantity = maxAvailableQuantity;
            }

            if (quantitySlider != null) quantitySlider.SetValueWithoutNotify(currentSelectedQuantity);
            UpdateTotalCalculation();
        }

        private void OnSliderValueChanged(float val)
        {
            currentSelectedQuantity = Mathf.RoundToInt(val);
            UpdateTotalCalculation();
        }

        private void OnConfirmActionClicked()
        {
            if (currentSelectedItem == null || currentSelectedQuantity <= 0) return;

            if (currentMode == VendorMode.Buy)
            {
                if (currentOfferIndex < 0 || MerchantShopManager.Instance == null) return;

                bool success = MerchantShopManager.Instance.TryPurchase(
                    currentOfferIndex,
                    currentSelectedQuantity,
                    PlayerInventory.Instance,
                    out string message
                );

                ShowFeedback(message, success);
                RefreshBuyCatalog();
            }
            else
            {
                // Sell Mode
                var result = VendorTransactionService.SellItem(
                    PlayerInventory.Instance,
                    currentSelectedItem,
                    currentSelectedQuantity
                );

                if (result.Success)
                {
                    ShowFeedback(result.Message, true);
                    RefreshSellableList();
                }
                else
                {
                    ShowFeedback(result.Message, false);
                }
            }
        }

        private void OnRerollStockClicked()
        {
            if (MerchantShopManager.Instance != null)
            {
                MerchantShopManager.Instance.ForceRestock();
                ShowFeedback("Restocked with fresh seed varieties!", true);
            }
        }

        private void OnShopRestocked()
        {
            if (isOpen && currentMode == VendorMode.Buy)
            {
                RefreshBuyCatalog();
            }
        }

        private void OnShopOfferPurchased(int index, MerchantStockOffer offer)
        {
            if (isOpen && currentMode == VendorMode.Buy)
            {
                RefreshBuyCatalog();
            }
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            if (feedbackText == null) return;

            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = StartCoroutine(FeedbackRoutine(message, isSuccess));
        }

        private IEnumerator FeedbackRoutine(string message, bool isSuccess)
        {
            feedbackText.text = message;
            feedbackText.color = isSuccess ? new Color(0.35f, 0.95f, 0.45f) : new Color(1.0f, 0.4f, 0.4f);

            yield return new WaitForSeconds(feedbackDuration);

            feedbackText.text = string.Empty;
            feedbackCoroutine = null;
        }

        private void EnsureRowCache()
        {
            if (spawnedRows.Count == 0 && itemsContainer != null)
            {
                spawnedRows.AddRange(itemsContainer.GetComponentsInChildren<VendorItemRowUI>(true));
            }
        }

        private VendorItemRowUI GetOrCreateRow(int index)
        {
            if (index < spawnedRows.Count)
            {
                var row = spawnedRows[index];
                row.gameObject.SetActive(true);
                return row;
            }

            GameObject rowObj = new GameObject($"VendorRow_{index}");
            rowObj.transform.SetParent(itemsContainer, false);
            var rect = rowObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 48f);
            var rowComp = rowObj.AddComponent<VendorItemRowUI>();
            spawnedRows.Add(rowComp);
            return rowComp;
        }
    }
}
