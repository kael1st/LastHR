using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// UI element for an individual hotbar inventory slot.
    /// Displays item icon, stack quantity, hotkey badge, and active selection state.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("Slot Identity")]
        [SerializeField] private int slotIndex;

        [Header("UI Components")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Text quantityText;
        [SerializeField] private Text hotkeyText;
        [SerializeField] private Button button;

        private Action<int> onClickCallback;

        public int SlotIndex => slotIndex;

        public void Initialize(int index, Action<int> onClick)
        {
            slotIndex = index;
            onClickCallback = onClick;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClickCallback?.Invoke(slotIndex));
            }

            if (hotkeyText != null)
            {
                hotkeyText.text = (slotIndex + 1).ToString();
            }
        }

        public void SetData(InventorySlot slot, bool isSelected)
        {
            // Highlight frame
            if (highlightImage != null)
            {
                highlightImage.enabled = isSelected;
            }

            // Empty slot check
            if (slot == null || slot.IsEmpty || slot.Item == null)
            {
                if (iconImage != null)
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                }

                if (quantityText != null)
                {
                    quantityText.text = string.Empty;
                }
                return;
            }

            // Populated slot
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = slot.Item.icon;
            }

            if (quantityText != null)
            {
                // Only display stack count if quantity > 1 or item is stackable
                if (slot.Quantity > 1 || slot.Item.maxStack > 1)
                {
                    quantityText.text = slot.Quantity.ToString();
                }
                else
                {
                    quantityText.text = string.Empty;
                }
            }
        }
    }
}
