using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// Master UI Controller for the Farming & Inventory system.
    /// Manages the Hotbar UI, active item banner, farming notifications, and calendar HUD.
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

        private Coroutine notificationCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (advanceDayButton != null)
            {
                advanceDayButton.onClick.AddListener(OnAdvanceDayClicked);
            }
        }

        private void Start()
        {
            SetupSlots();

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += RefreshUI;
                PlayerInventory.Instance.OnSelectedSlotChanged += OnSlotSelected;
                RefreshUI();
                OnSlotSelected(PlayerInventory.Instance.SelectedSlotIndex);
            }

            if (GardenManager.Instance != null)
            {
                UpdateDayDisplay(GardenManager.Instance.CurrentDay, GardenManager.Instance.Season);
            }
            else
            {
                UpdateDayDisplay(1, "Spring");
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshUI;
                PlayerInventory.Instance.OnSelectedSlotChanged -= OnSlotSelected;
            }
        }

        private void SetupSlots()
        {
            if (hotbarContainer == null) return;

            // If slots are already placed in hierarchy
            if (slotUIs.Count == 0)
            {
                slotUIs.AddRange(hotbarContainer.GetComponentsInChildren<InventorySlotUI>(true));
            }

            for (int i = 0; i < slotUIs.Count; i++)
            {
                slotUIs[i].Initialize(i, OnSlotClicked);
            }
        }

        public void RefreshUI()
        {
            if (PlayerInventory.Instance == null) return;

            var inventorySlots = PlayerInventory.Instance.Slots;
            int selectedIndex = PlayerInventory.Instance.SelectedSlotIndex;

            for (int i = 0; i < slotUIs.Count; i++)
            {
                if (i < inventorySlots.Length)
                {
                    slotUIs[i].SetData(inventorySlots[i], i == selectedIndex);
                }
            }

            UpdateSelectedItemBanner();
        }

        private void OnSlotSelected(int index)
        {
            for (int i = 0; i < slotUIs.Count; i++)
            {
                slotUIs[i].SetData(
                    PlayerInventory.Instance != null && i < PlayerInventory.Instance.Slots.Length ? PlayerInventory.Instance.Slots[i] : null,
                    i == index
                );
            }

            UpdateSelectedItemBanner();
        }

        private void OnSlotClicked(int index)
        {
            if (PlayerInventory.Instance != null)
            {
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
                {
                    selectedItemDescText.text = item.description;
                }
            }
            else
            {
                if (selectedItemNameText != null) selectedItemNameText.text = "Empty Hands";
                if (selectedItemDescText != null) selectedItemDescText.text = "Select a tool (1: Hoe, 2: Water Can, 3: Seeds) or interact with Space/Click.";
            }
        }

        public void ShowNotification(string message, bool isSuccess = true)
        {
            if (notificationPanel == null || notificationText == null) return;

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }

            notificationCoroutine = StartCoroutine(NotificationRoutine(message, isSuccess));
        }

        private IEnumerator NotificationRoutine(string message, bool isSuccess)
        {
            notificationText.text = message;
            notificationText.color = isSuccess ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.6f, 0.6f);
            notificationPanel.SetActive(true);

            yield return new WaitForSeconds(notificationDuration);

            notificationPanel.SetActive(false);
            notificationCoroutine = null;
        }

        public void UpdateDayDisplay(int day, string season)
        {
            if (dayText != null)
            {
                dayText.text = $"{season} - Day {day}";
            }
        }

        private void OnAdvanceDayClicked()
        {
            if (GardenManager.Instance != null)
            {
                GardenManager.Instance.AdvanceDay();
            }
        }
    }
}
