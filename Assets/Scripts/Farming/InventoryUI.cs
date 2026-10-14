using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// Master UI Controller for the Farming & Inventory system.
    /// Manages the Hotbar UI, active item banner, farming notifications, calendar HUD,
    /// in-game clock display, and confirmation dialogs for Next Day / Pocket Watch rewind.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("Hotbar")]
        [SerializeField] private Transform hotbarContainer;
        [SerializeField] private InventorySlotUI slotPrefab;
        [SerializeField] private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

        [Header("Item Info Banner")]
        [SerializeField] private Text selectedItemNameText;
        [SerializeField] private Text selectedItemDescText;

        [Header("Notification / Action Toast")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private Text notificationText;
        [SerializeField] private float notificationDuration = 2.5f;

        [Header("Calendar / Day HUD")]
        [SerializeField] private Text dayText;
        [SerializeField] private Button advanceDayButton;

        [Header("Clock HUD")]
        [Tooltip("Text element that shows the current in-game time (e.g. '7:05 PM').")]
        [SerializeField] private Text clockText;
        [Tooltip("(Optional) Text shown when the time requirement for Next Day is not yet met.")]
        [SerializeField] private Text nextDayLockedText;

        // ── Confirmation Dialog ────────────────────────────────────────────────────
        [Header("Confirmation Dialog")]
        [Tooltip("Root panel of the confirmation dialog. Will be shown/hidden programmatically.")]
        [SerializeField] private GameObject confirmDialogPanel;
        [SerializeField] private Text confirmTitleText;
        [SerializeField] private Text confirmBodyText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        // Internal state
        private Coroutine notificationCoroutine;

        // ── Pending action stored while dialog is open ─────────────────────────
        private enum PendingAction { None, NextDay, PocketWatch }
        private PendingAction pendingAction = PendingAction.None;

        // ─────────────────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (advanceDayButton != null)
                advanceDayButton.onClick.AddListener(OnAdvanceDayClicked);

            // Confirmation dialog buttons
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);

            // Hide dialog on start
            if (confirmDialogPanel != null)
                confirmDialogPanel.SetActive(false);
        }

        private void Start()
        {
            SetupSlots();

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

            // Subscribe to time updates
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTimeChanged += OnTimeChanged;

            UpdateClockDisplay();
            UpdateNextDayButtonState();
        }

        private void Update()
        {
            // Keep clock & button state current every frame
            // (cheap, only text/interactable changes; OnTimeChanged handles the heavy work)
            UpdateNextDayButtonState();
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
        //  Slot setup & UI refresh
        // ─────────────────────────────────────────────────────────────────────────

        private void SetupSlots()
        {
            if (hotbarContainer == null) return;

            if (slotUIs.Count == 0)
                slotUIs.AddRange(hotbarContainer.GetComponentsInChildren<InventorySlotUI>(true));

            for (int i = 0; i < slotUIs.Count; i++)
                slotUIs[i].Initialize(i, OnSlotClicked);
        }

        public void RefreshUI()
        {
            if (PlayerInventory.Instance == null) return;

            var inventorySlots = PlayerInventory.Instance.Slots;
            int selectedIndex  = PlayerInventory.Instance.SelectedSlotIndex;

            for (int i = 0; i < slotUIs.Count; i++)
            {
                if (i < inventorySlots.Length)
                    slotUIs[i].SetData(inventorySlots[i], i == selectedIndex);
            }

            UpdateSelectedItemBanner();
        }

        private void OnSlotSelected(int index)
        {
            for (int i = 0; i < slotUIs.Count; i++)
            {
                slotUIs[i].SetData(
                    PlayerInventory.Instance != null && i < PlayerInventory.Instance.Slots.Length
                        ? PlayerInventory.Instance.Slots[i] : null,
                    i == index
                );
            }
            UpdateSelectedItemBanner();
        }

        private void OnSlotClicked(int index)
        {
            if (PlayerInventory.Instance != null)
            {
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
        }

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
                    selectedItemNameText.text = $"{item.displayName}{countStr}";
                }
                if (selectedItemDescText != null)
                    selectedItemDescText.text = item.description;
            }
            else
            {
                if (selectedItemNameText != null) selectedItemNameText.text = "Empty Hands";
                if (selectedItemDescText  != null) selectedItemDescText.text  = "Select a tool (1: Hoe, 2: Water Can, 3: Seeds) or interact with Space/Click.";
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Notification toast
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
        //  Calendar / Day display
        // ─────────────────────────────────────────────────────────────────────────

        public void UpdateDayDisplay(int day, string season)
        {
            if (dayText != null)
                dayText.text = $"{season} - Day {day}";
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Clock display
        // ─────────────────────────────────────────────────────────────────────────

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

        /// <summary>
        /// Enables/disables the Advance Day button based on whether it's >= 7 PM.
        /// Also updates the locked hint text if one is assigned.
        /// </summary>
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

        // ─────────────────────────────────────────────────────────────────────────
        //  Next Day — public entry point (called by button AND keyboard shortcut)
        // ─────────────────────────────────────────────────────────────────────────

        private void OnAdvanceDayClicked()
        {
            RequestNextDay();
        }

        /// <summary>
        /// Called by the Advance Day button or keyboard shortcut.
        /// Checks the 7 PM gate, then shows a confirmation dialog.
        /// </summary>
        public void RequestNextDay()
        {
            // 7 PM gate
            if (TimeManager.Instance != null && !TimeManager.Instance.IsNextDayUnlocked)
            {
                int unlockHour = TimeManager.Instance.NextDayUnlockHour;
                int displayH   = unlockHour > 12 ? unlockHour - 12 : unlockHour;
                ShowNotification($"You can only rest after {displayH}:00 PM!", false);
                return;
            }

            // Open confirmation dialog
            pendingAction = PendingAction.NextDay;
            ShowConfirmDialog(
                "End the Day?",
                "Sleep and advance to the next morning?\n(Crops will grow overnight)"
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Pocket Watch — public entry point (called by FarmingController)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by FarmingController when the player uses the Pocket Watch item.
        /// Validates that a rewind is possible, then shows a confirmation dialog.
        /// </summary>
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
                // No dialog panel wired up — execute immediately (fallback)
                ExecutePendingAction();
                return;
            }

            if (confirmTitleText != null) confirmTitleText.text = title;
            if (confirmBodyText  != null) confirmBodyText.text  = body;

            confirmDialogPanel.SetActive(true);

            // Pause in-game time while the dialog is open
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetTimePaused(true);
        }

        private void HideConfirmDialog()
        {
            if (confirmDialogPanel != null)
                confirmDialogPanel.SetActive(false);

            // Resume time
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
