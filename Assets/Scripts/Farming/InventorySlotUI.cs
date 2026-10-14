using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// UI element for an inventory slot (used for both Hotbar and Main Storage).
    /// Fully supports:
    /// - Icon display, stack counts, and hotkey labels (1-9).
    /// - Active hotbar selection highlight.
    /// - Rarity tier styling (color-coded frame/indicator for Common, Rare, Legendary, Mythical).
    /// - Full Minecraft-style drag-and-drop and slot swapping via Unity EventSystem.
    /// - Hover tooltips for item details and selling value.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Slot Identity")]
        [SerializeField] private int slotIndex;
        [SerializeField] private bool isHotbarSlot = true;

        [Header("UI Components")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Image rarityIndicator;
        [SerializeField] private Text quantityText;
        [SerializeField] private Text hotkeyText;
        [SerializeField] private Button button;

        private Action<int> onClickCallback;
        private ItemData currentItem;
        private int currentQuantity;
        private bool isCurrentlySelected;

        public int SlotIndex => slotIndex;
        public bool IsHotbarSlot => isHotbarSlot;
        public ItemData CurrentItem => currentItem;
        public int CurrentQuantity => currentQuantity;

        public void Initialize(int index, bool hotbar, Action<int> onClick)
        {
            slotIndex = index;
            isHotbarSlot = hotbar;
            onClickCallback = onClick;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleButtonClick);
            }

            if (hotkeyText != null)
            {
                if (isHotbarSlot && slotIndex < 9)
                {
                    hotkeyText.text = (slotIndex + 1).ToString();
                    hotkeyText.enabled = true;
                }
                else
                {
                    hotkeyText.text = string.Empty;
                    hotkeyText.enabled = false;
                }
            }
        }

        private void HandleButtonClick()
        {
            onClickCallback?.Invoke(slotIndex);
        }

        public void SetData(InventorySlot slot, bool isSelected)
        {
            isCurrentlySelected = isSelected;

            // Highlight frame (for hotbar active slot)
            if (highlightImage != null)
            {
                highlightImage.enabled = isSelected;
            }

            // Empty slot check
            if (slot == null || slot.IsEmpty || slot.Item == null)
            {
                currentItem = null;
                currentQuantity = 0;

                if (iconImage != null)
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                    iconImage.color = Color.white;
                }

                if (quantityText != null)
                {
                    quantityText.text = string.Empty;
                }

                if (rarityIndicator != null)
                {
                    rarityIndicator.enabled = false;
                }
                return;
            }

            // Populated slot
            currentItem = slot.Item;
            currentQuantity = slot.Quantity;

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = currentItem.icon;
                iconImage.color = Color.white;
            }

            if (quantityText != null)
            {
                if (currentQuantity > 1 || currentItem.maxStack > 1)
                {
                    quantityText.text = currentQuantity.ToString();
                    quantityText.enabled = true;
                }
                else
                {
                    quantityText.text = string.Empty;
                    quantityText.enabled = false;
                }
            }

            // Rarity indicator
            if (rarityIndicator != null)
            {
                rarityIndicator.enabled = true;
                rarityIndicator.color = currentItem.GetRarityColor();
            }
        }

        public void SetGhostVisualDimmed(bool dimmed)
        {
            if (iconImage != null && currentItem != null)
            {
                iconImage.color = dimmed ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Drag and Drop
        // ─────────────────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentItem == null || currentQuantity <= 0) return;

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.StartItemDrag(this, slotIndex, currentItem, currentQuantity);
                SetGhostVisualDimmed(true);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsDragging)
            {
                InventoryUI.Instance.UpdateItemDrag(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SetGhostVisualDimmed(false);

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.EndItemDrag();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsDragging)
            {
                InventoryUI.Instance.DropItemOnSlot(this, slotIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // If dragging is handled, OnDrop takes care of it.
                // If inventory UI is handling click-to-pick / click-to-place:
                if (InventoryUI.Instance != null && InventoryUI.Instance.IsClickTransferActive)
                {
                    InventoryUI.Instance.HandleSlotClickTransfer(slotIndex);
                    return;
                }

                onClickCallback?.Invoke(slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Right click: Split stack or use
                if (InventoryUI.Instance != null && InventoryUI.Instance.IsInventoryOpen)
                {
                    InventoryUI.Instance.HandleSlotRightClick(slotIndex);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (highlightImage != null && !isCurrentlySelected)
            {
                highlightImage.enabled = true;
                highlightImage.color = new Color(1f, 1f, 1f, 0.45f);
            }

            if (currentItem != null && InventoryUI.Instance != null && !InventoryUI.Instance.IsDragging)
            {
                InventoryUI.Instance.ShowTooltip(currentItem, currentQuantity, transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlightImage != null && !isCurrentlySelected)
            {
                highlightImage.enabled = false;
                highlightImage.color = Color.white;
            }

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.HideTooltip();
            }
        }
    }
}
