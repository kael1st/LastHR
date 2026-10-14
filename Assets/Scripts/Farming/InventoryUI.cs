using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// Master UI Controller for the Farming & Inventory system.
    /// Manages:
    /// 1. Hotbar HUD (9 slots, Minecraft-style number selection, stack counts).
    /// 2. Main Inventory Window (27 storage slots + 9 hotbar mirror slots).
    /// 3. Cursor Drag-and-Drop, swapping, and stacking between all slots.
    /// 4. Detailed item tooltips displaying Rarity Tiers, Selling Prices, and Crop stats.
    /// 5. On-screen Inventory Button and Keyboard shortcut (E / I / Escape).
    /// 6. Farming notifications, In-game Clock, Calendar, and Confirmation Dialogs.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("Hotbar HUD (Bottom Screen)")]
        [SerializeField] private Transform hotbarContainer;
        [SerializeField] private List<InventorySlotUI> hotbarSlotUIs = new List<InventorySlotUI>();

        [Header("Main Inventory Window")]
        [SerializeField] private GameObject mainInventoryPanel;
        [SerializeField] private Transform mainStorageGrid;
        [SerializeField] private Transform hotbarMirrorGrid;
        [SerializeField] private Button inventoryCloseButton;
        [SerializeField] private List<InventorySlotUI> mainStorageSlotUIs = new List<InventorySlotUI>();
        [SerializeField] private List<InventorySlotUI> mirrorSlotUIs = new List<InventorySlotUI>();

        [Header("Dedicated Inventory Button (HUD)")]
        [SerializeField] private Button inventoryToggleButton;

        [Header("Item Info Banner (Hotbar HUD)")]
        [SerializeField] private Text selectedItemNameText;
        [SerializeField] private Text selectedItemDescText;

        [Header("Item Details Card (Inside Main Inventory)")]
        [SerializeField] private Image detailIconImage;
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailRarityText;
        [SerializeField] private Text detailPriceText;
        [SerializeField] private Text detailDescText;
        [SerializeField] private Text detailExtraStatsText;

        [Header("Hover Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text tooltipNameText;
        [SerializeField] private Text tooltipRarityText;
        [SerializeField] private Text tooltipPriceText;
        [SerializeField] private Text tooltipDescText;
        [SerializeField] private Text tooltipStatsText;

        [Header("Drag and Drop Ghost Visual")]
        [SerializeField] private GameObject dragGhostObject;
        [SerializeField] private Image dragGhostIcon;
        [SerializeField] private Text dragGhostQuantityText;

        [Header("Notification / Action Toast")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private Text notificationText;
        [SerializeField] private float notificationDuration = 2.5f;

        [Header("Calendar / Day HUD")]
        [SerializeField] private Text dayText;
        [SerializeField] private Button advanceDayButton;

        [Header("Clock HUD")]
        [SerializeField] private Text clockText;
        [SerializeField] private Text nextDayLockedText;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmDialogPanel;
        [SerializeField] private Text confirmTitleText;
        [SerializeField] private Text confirmBodyText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        // Internal State
        private bool isInventoryOpen = false;
        private int draggedSlotIndex = -1;
        private InventorySlotUI dragSourceUI = null;
        private Coroutine notificationCoroutine;

        // Click-to-transfer state (alternative to dragging)
        private int clickTransferSourceIndex = -1;

        private enum PendingAction { None, NextDay, PocketWatch }
        private PendingAction pendingAction = PendingAction.None;

        public bool IsInventoryOpen => isInventoryOpen;
        public bool IsDragging => draggedSlotIndex >= 0;
        public bool IsClickTransferActive => clickTransferSourceIndex >= 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Advance Day Button
            if (advanceDayButton != null)
                advanceDayButton.onClick.AddListener(OnAdvanceDayClicked);

            // Inventory toggle and close buttons
            if (inventoryToggleButton != null)
                inventoryToggleButton.onClick.AddListener(ToggleInventory);

            if (inventoryCloseButton != null)
                inventoryCloseButton.onClick.AddListener(CloseInventory);

            // Confirmation Dialog buttons
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);

            // Hide panels initially
            if (confirmDialogPanel != null)
                confirmDialogPanel.SetActive(false);

            if (mainInventoryPanel != null)
                mainInventoryPanel.SetActive(false);

            if (dragGhostObject != null)
                dragGhostObject.SetActive(false);

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void Start()
        {
            SetupAllSlots();

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged    += RefreshUI;
                PlayerInventory.Instance.OnSelectedSlotChanged += OnSlotSelected;
                RefreshUI();
                OnSlotSelected(PlayerInventory.Instance.SelectedSlotIndex);
            }

            if (GardenManager.Instance != null)
                UpdateDayDisplay(GardenManager.Instance.CurrentDay, GardenManager.Instance.Season);
            else
                UpdateDayDisplay(1, "Spring");

            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTimeChanged += OnTimeChanged;

            UpdateClockDisplay();
            UpdateNextDayButtonState();
        }

        private void Update()
        {
            UpdateNextDayButtonState();
            HandleKeyboardShortcuts();

            // Update tooltip position to follow cursor when active
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                UpdateTooltipPosition();
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged    -= RefreshUI;
                PlayerInventory.Instance.OnSelectedSlotChanged -= OnSlotSelected;
            }
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTimeChanged -= OnTimeChanged;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Keyboard Shortcuts
        // ─────────────────────────────────────────────────────────────────────────

        private void HandleKeyboardShortcuts()
        {
            // Do not handle if vendor modal is currently open
            if (VendorUI.Instance != null && VendorUI.Instance.IsOpen)
            {
                return;
            }

            // Inventory is only accessible by clicking the dedicated on-screen [INVENTORY] button.
            // Escape key will close the main inventory if it is currently open.
            if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInventory();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Inventory Toggle & Window Management
        // ─────────────────────────────────────────────────────────────────────────

        public void ToggleInventory()
        {
            SetInventoryOpen(!isInventoryOpen);
        }

        public void OpenInventory()
        {
            SetInventoryOpen(true);
        }

        public void CloseInventory()
        {
            SetInventoryOpen(false);
        }

        public void SetInventoryOpen(bool open)
        {
            isInventoryOpen = open;

            if (mainInventoryPanel != null)
                mainInventoryPanel.SetActive(isInventoryOpen);

            // Cancel any ongoing drag or click transfer when closing
            if (!isInventoryOpen)
            {
                CancelDrag();
                CancelClickTransfer();
                HideTooltip();
            }

            RefreshUI();

            // If an item is selected in hotbar, show its info in the inventory preview
            if (isInventoryOpen && PlayerInventory.Instance != null)
            {
                var held = PlayerInventory.Instance.GetSelectedItem();
                var slot = PlayerInventory.Instance.GetSelectedSlot();
                UpdateDetailCard(held, slot != null ? slot.Quantity : 0);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Slot Setup & UI Refresh
        // ─────────────────────────────────────────────────────────────────────────

        public void SetupAllSlots()
        {
            // 1. Hotbar HUD Slots (indices 0..8)
            if (hotbarContainer != null && hotbarSlotUIs.Count == 0)
            {
                hotbarSlotUIs.AddRange(hotbarContainer.GetComponentsInChildren<InventorySlotUI>(true));
            }
            for (int i = 0; i < hotbarSlotUIs.Count; i++)
            {
                hotbarSlotUIs[i].Initialize(i, true, OnHotbarSlotClicked);
            }

            // 2. Main Storage Slots (indices 9..35)
            if (mainStorageGrid != null && mainStorageSlotUIs.Count == 0)
            {
                mainStorageSlotUIs.AddRange(mainStorageGrid.GetComponentsInChildren<InventorySlotUI>(true));
            }
            for (int i = 0; i < mainStorageSlotUIs.Count; i++)
            {
                int actualIndex = 9 + i;
                mainStorageSlotUIs[i].Initialize(actualIndex, false, OnInventorySlotClicked);
            }

            // 3. Hotbar Mirror Slots inside Inventory Modal (indices 0..8)
            if (hotbarMirrorGrid != null && mirrorSlotUIs.Count == 0)
            {
                mirrorSlotUIs.AddRange(hotbarMirrorGrid.GetComponentsInChildren<InventorySlotUI>(true));
            }
            for (int i = 0; i < mirrorSlotUIs.Count; i++)
            {
                mirrorSlotUIs[i].Initialize(i, true, OnInventorySlotClicked);
            }
        }

        public void RefreshUI()
        {
            if (PlayerInventory.Instance == null) return;

            var inventorySlots = PlayerInventory.Instance.Slots;
            int selectedIndex  = PlayerInventory.Instance.SelectedSlotIndex;

            // 1. Refresh Hotbar HUD
            for (int i = 0; i < hotbarSlotUIs.Count; i++)
            {
                if (i < inventorySlots.Length)
                    hotbarSlotUIs[i].SetData(inventorySlots[i], i == selectedIndex);
            }

            // 2. Refresh Main Storage Slots (indices 9..35)
            for (int i = 0; i < mainStorageSlotUIs.Count; i++)
            {
                int actualIndex = 9 + i;
                if (actualIndex < inventorySlots.Length)
                    mainStorageSlotUIs[i].SetData(inventorySlots[actualIndex], false);
            }

            // 3. Refresh Hotbar Mirror Slots in Inventory Window (indices 0..8)
            for (int i = 0; i < mirrorSlotUIs.Count; i++)
            {
                if (i < inventorySlots.Length)
                    mirrorSlotUIs[i].SetData(inventorySlots[i], i == selectedIndex);
            }

            UpdateSelectedItemBanner();
        }

        private void OnSlotSelected(int index)
        {
            if (PlayerInventory.Instance == null) return;
            var slots = PlayerInventory.Instance.Slots;

            for (int i = 0; i < hotbarSlotUIs.Count; i++)
            {
                hotbarSlotUIs[i].SetData(i < slots.Length ? slots[i] : null, i == index);
            }

            for (int i = 0; i < mirrorSlotUIs.Count; i++)
            {
                mirrorSlotUIs[i].SetData(i < slots.Length ? slots[i] : null, i == index);
            }

            UpdateSelectedItemBanner();

            var activeSlot = PlayerInventory.Instance.GetSelectedSlot();
            if (activeSlot != null)
                UpdateDetailCard(activeSlot.Item, activeSlot.Quantity);
        }

        private void OnHotbarSlotClicked(int index)
        {
            if (PlayerInventory.Instance == null) return;

            // Pocket Watch check
            if (PlayerInventory.Instance.SelectedSlotIndex == index)
            {
                var activeSlot = PlayerInventory.Instance.GetSelectedSlot();
                if (activeSlot != null && !activeSlot.IsEmpty && activeSlot.Item != null && activeSlot.Item.IsPocketWatch)
                {
                    RequestPocketWatchUse();
                    return;
                }
            }

            PlayerInventory.Instance.SelectSlot(index);
        }

        private void OnInventorySlotClicked(int index)
        {
            if (PlayerInventory.Instance == null) return;

            var slot = PlayerInventory.Instance.GetSlot(index);
            if (slot != null && !slot.IsEmpty)
            {
                UpdateDetailCard(slot.Item, slot.Quantity);
            }

            // If clicked hotbar row in inventory, also select it
            if (index < PlayerInventory.Instance.HotbarSize)
            {
                PlayerInventory.Instance.SelectSlot(index);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Drag and Drop System
        // ─────────────────────────────────────────────────────────────────────────

        public void StartItemDrag(InventorySlotUI sourceUI, int slotIndex, ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return;

            draggedSlotIndex = slotIndex;
            dragSourceUI = sourceUI;

            if (dragGhostObject != null)
            {
                if (dragGhostIcon != null)
                {
                    dragGhostIcon.sprite = item.icon;
                    dragGhostIcon.enabled = true;
                }
                if (dragGhostQuantityText != null)
                {
                    dragGhostQuantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
                }
                dragGhostObject.SetActive(true);
                UpdateItemDrag(Input.mousePosition);
            }

            HideTooltip();
        }

        public void UpdateItemDrag(Vector2 screenPosition)
        {
            if (dragGhostObject != null && dragGhostObject.activeSelf)
            {
                dragGhostObject.transform.position = screenPosition;
            }
        }

        public void DropItemOnSlot(InventorySlotUI targetUI, int targetSlotIndex)
        {
            if (!IsDragging) return;

            if (PlayerInventory.Instance != null && draggedSlotIndex >= 0)
            {
                PlayerInventory.Instance.MoveOrStack(draggedSlotIndex, targetSlotIndex);
            }

            EndItemDrag();
        }

        public void EndItemDrag()
        {
            if (dragSourceUI != null)
            {
                dragSourceUI.SetGhostVisualDimmed(false);
                dragSourceUI = null;
            }

            draggedSlotIndex = -1;

            if (dragGhostObject != null)
            {
                dragGhostObject.SetActive(false);
            }

            RefreshUI();
        }

        public void CancelDrag()
        {
            EndItemDrag();
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Click-to-Transfer (Accessible alternative to click-and-drag)
        // ─────────────────────────────────────────────────────────────────────────

        public void HandleSlotClickTransfer(int targetSlotIndex)
        {
            if (clickTransferSourceIndex < 0) return;

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.MoveOrStack(clickTransferSourceIndex, targetSlotIndex);
            }

            CancelClickTransfer();
            RefreshUI();
        }

        public void CancelClickTransfer()
        {
            clickTransferSourceIndex = -1;
            if (dragGhostObject != null) dragGhostObject.SetActive(false);
        }

        public void HandleSlotRightClick(int slotIndex)
        {
            if (PlayerInventory.Instance == null) return;
            var slot = PlayerInventory.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty) return;

            // Right click splits stack into first empty slot or cursor
            for (int i = 0; i < PlayerInventory.Instance.Slots.Length; i++)
            {
                if (PlayerInventory.Instance.Slots[i].IsEmpty)
                {
                    PlayerInventory.Instance.SplitStack(slotIndex, i);
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Item Detail Card & Selected Item Banner
        // ─────────────────────────────────────────────────────────────────────────

        private void UpdateSelectedItemBanner()
        {
            if (PlayerInventory.Instance == null) return;

            var activeSlot = PlayerInventory.Instance.GetSelectedSlot();
            if (activeSlot != null && !activeSlot.IsEmpty && activeSlot.Item != null)
            {
                var item = activeSlot.Item;
                if (selectedItemNameText != null)
                {
                    string countStr = activeSlot.Quantity > 1 ? $" (x{activeSlot.Quantity})" : string.Empty;
                    string rarityColor = ItemData.GetRarityHex(item.rarity);
                    selectedItemNameText.text = $"<color={rarityColor}>{item.displayName}</color>{countStr}";
                }
                if (selectedItemDescText != null)
                {
                    string extra = item.IsSeed && item.cropData != null
                        ? $" | Growth: {item.cropData.realtimeSecondsPerStage * item.cropData.MaxStage:0}s ({item.cropData.daysToMature}d) | Yield: {item.cropData.minHarvestYield}-{item.cropData.maxHarvestYield}"
                        : $" | Sell Value: {item.sellPrice} G";
                    selectedItemDescText.text = $"{item.description}{extra}";
                }
            }
            else
            {
                if (selectedItemNameText != null) selectedItemNameText.text = "Empty Hands";
                if (selectedItemDescText != null) selectedItemDescText.text = "Select a tool (1: Hoe, 2: Water Can, 3: Seeds) or press [E] to open Inventory.";
            }
        }

        public void UpdateDetailCard(ItemData item, int quantity)
        {
            if (item == null)
            {
                if (detailIconImage != null) { detailIconImage.enabled = false; detailIconImage.sprite = null; }
                if (detailNameText != null) detailNameText.text = "No Item Selected";
                if (detailRarityText != null) detailRarityText.text = string.Empty;
                if (detailPriceText != null) detailPriceText.text = string.Empty;
                if (detailDescText != null) detailDescText.text = "Click or hover over an item to inspect its properties.";
                if (detailExtraStatsText != null) detailExtraStatsText.text = string.Empty;
                return;
            }

            if (detailIconImage != null)
            {
                detailIconImage.enabled = true;
                detailIconImage.sprite = item.icon;
            }

            string rarityHex = ItemData.GetRarityHex(item.rarity);

            if (detailNameText != null)
            {
                string countStr = quantity > 1 ? $" x{quantity}" : string.Empty;
                detailNameText.text = $"<color={rarityHex}>{item.displayName}</color>{countStr}";
            }

            if (detailRarityText != null)
            {
                detailRarityText.text = $"<color={rarityHex}>[{item.rarity.ToString().ToUpper()}]</color>";
            }

            if (detailPriceText != null)
            {
                detailPriceText.text = $"Sell Value: <b>{item.sellPrice} G</b>";
            }

            if (detailDescText != null)
            {
                detailDescText.text = item.description;
            }

            if (detailExtraStatsText != null)
            {
                if (item.IsSeed && item.cropData != null)
                {
                    var cd = item.cropData;
                    detailExtraStatsText.text = $"Growth: {cd.realtimeSecondsPerStage * cd.MaxStage:0}s ({cd.daysToMature} Days)\nYield: {cd.minHarvestYield} - {cd.maxHarvestYield}x {cd.cropName}" +
                        (cd.regrows ? " (Regrows)" : "");
                }
                else if (item.IsCrop)
                {
                    detailExtraStatsText.text = $"Fresh harvest from your garden. Ready to sell or store.";
                }
                else if (item.IsPocketWatch)
                {
                    detailExtraStatsText.text = "Mystical relic. Use to rewind time back to yesterday.";
                }
                else
                {
                    detailExtraStatsText.text = $"Stack Limit: {item.maxStack}";
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Hover Tooltip
        // ─────────────────────────────────────────────────────────────────────────

        public void ShowTooltip(ItemData item, int quantity, Vector3 worldPosition)
        {
            if (item == null || tooltipPanel == null) return;

            string rarityHex = ItemData.GetRarityHex(item.rarity);

            if (tooltipNameText != null)
            {
                string countStr = quantity > 1 ? $" (x{quantity})" : string.Empty;
                tooltipNameText.text = $"<color={rarityHex}>{item.displayName}</color>{countStr}";
            }

            if (tooltipRarityText != null)
            {
                tooltipRarityText.text = $"<color={rarityHex}>[{item.rarity.ToString().ToUpper()}]</color>";
            }

            if (tooltipPriceText != null)
            {
                tooltipPriceText.text = $"Sell: {item.sellPrice} G";
            }

            if (tooltipDescText != null)
            {
                tooltipDescText.text = item.description;
            }

            if (tooltipStatsText != null)
            {
                if (item.IsSeed && item.cropData != null)
                {
                    var cd = item.cropData;
                    tooltipStatsText.text = $"Growth: {cd.realtimeSecondsPerStage * cd.MaxStage:0}s ({cd.daysToMature}d) | Yield: {cd.minHarvestYield}-{cd.maxHarvestYield}" +
                        (cd.regrows ? " (Regrows)" : "");
                    tooltipStatsText.enabled = true;
                }
                else
                {
                    tooltipStatsText.text = string.Empty;
                    tooltipStatsText.enabled = false;
                }
            }

            tooltipPanel.SetActive(true);
            UpdateTooltipPosition();
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        private void UpdateTooltipPosition()
        {
            if (tooltipPanel == null) return;

            Vector2 mousePos = Input.mousePosition;
            // Offset slightly above and to the right of cursor
            tooltipPanel.transform.position = mousePos + new Vector2(18f, 18f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Notification Toast
        // ─────────────────────────────────────────────────────────────────────────

        public void ShowNotification(string message, bool isSuccess = true)
        {
            if (notificationPanel == null || notificationText == null) return;

            if (notificationCoroutine != null)
                StopCoroutine(notificationCoroutine);

            notificationCoroutine = StartCoroutine(NotificationRoutine(message, isSuccess));
        }

        private IEnumerator NotificationRoutine(string message, bool isSuccess)
        {
            notificationText.text  = message;
            notificationText.color = isSuccess ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.6f, 0.6f);
            notificationPanel.SetActive(true);

            yield return new WaitForSeconds(notificationDuration);

            notificationPanel.SetActive(false);
            notificationCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Calendar & Clock HUD
        // ─────────────────────────────────────────────────────────────────────────

        public void UpdateDayDisplay(int day, string season)
        {
            if (dayText != null)
                dayText.text = $"{season} - Day {day}";
        }

        private void OnTimeChanged(int hour, int minute)
        {
            UpdateClockDisplay();
            UpdateNextDayButtonState();
        }

        private void UpdateClockDisplay()
        {
            if (clockText == null) return;
            clockText.text = TimeManager.Instance != null
                ? TimeManager.Instance.GetFormattedTime()
                : "—";
        }

        private void UpdateNextDayButtonState()
        {
            bool unlocked = TimeManager.Instance == null || TimeManager.Instance.IsNextDayUnlocked;

            if (advanceDayButton != null)
                advanceDayButton.interactable = unlocked;

            if (nextDayLockedText != null)
            {
                if (!unlocked && TimeManager.Instance != null)
                {
                    int unlockHour = TimeManager.Instance.NextDayUnlockHour;
                    int displayH   = unlockHour > 12 ? unlockHour - 12 : unlockHour;
                    nextDayLockedText.text    = $"Available at {displayH}:00 PM";
                    nextDayLockedText.enabled = true;
                }
                else
                {
                    nextDayLockedText.enabled = false;
                }
            }
        }

        private void OnAdvanceDayClicked()
        {
            RequestNextDay();
        }

        public void RequestNextDay()
        {
            if (TimeManager.Instance != null && !TimeManager.Instance.IsNextDayUnlocked)
            {
                int unlockHour = TimeManager.Instance.NextDayUnlockHour;
                int displayH   = unlockHour > 12 ? unlockHour - 12 : unlockHour;
                ShowNotification($"You can only rest after {displayH}:00 PM!", false);
                return;
            }

            pendingAction = PendingAction.NextDay;
            ShowConfirmDialog(
                "End the Day?",
                "Sleep and advance to the next morning?\n(Crops will grow overnight)"
            );
        }

        public void RequestPocketWatchUse()
        {
            if (GardenManager.Instance == null || !GardenManager.Instance.HasPreviousSnapshot)
            {
                ShowNotification("The Pocket Watch has no memory of yesterday...", false);
                return;
            }

            pendingAction = PendingAction.PocketWatch;
            int prevDay = GardenManager.Instance.CurrentDay - 1;
            ShowConfirmDialog(
                "Rewind Time?",
                $"Use the Pocket Watch to rewind back to Day {prevDay}?\n\nAll progress from today will be lost, and your items & crops will return to how they were."
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Confirmation Dialog
        // ─────────────────────────────────────────────────────────────────────────

        private void ShowConfirmDialog(string title, string body)
        {
            if (confirmDialogPanel == null)
            {
                ExecutePendingAction();
                return;
            }

            if (confirmTitleText != null) confirmTitleText.text = title;
            if (confirmBodyText  != null) confirmBodyText.text  = body;

            confirmDialogPanel.SetActive(true);

            if (TimeManager.Instance != null)
                TimeManager.Instance.SetTimePaused(true);
        }

        private void HideConfirmDialog()
        {
            if (confirmDialogPanel != null)
                confirmDialogPanel.SetActive(false);

            if (TimeManager.Instance != null)
                TimeManager.Instance.SetTimePaused(false);
        }

        private void OnConfirmYes()
        {
            HideConfirmDialog();
            ExecutePendingAction();
            pendingAction = PendingAction.None;
        }

        private void OnConfirmNo()
        {
            HideConfirmDialog();
            pendingAction = PendingAction.None;
        }

        private void ExecutePendingAction()
        {
            switch (pendingAction)
            {
                case PendingAction.NextDay:
                    if (GardenManager.Instance != null)
                        GardenManager.Instance.AdvanceDay();
                    break;

                case PendingAction.PocketWatch:
                    if (GardenManager.Instance != null)
                        GardenManager.Instance.RewindDay();
                    break;
            }
        }
    }
}
