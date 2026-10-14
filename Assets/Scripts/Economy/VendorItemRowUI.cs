using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// UI element representing an eligible sellable crop in the Street Vendor interface.
    /// Displays item icon, rarity badge, owned count, and unit price.
    /// </summary>
    public class VendorItemRowUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text rarityBadgeText;
        [SerializeField] private Text countText;
        [SerializeField] private Text priceText;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Button button;

        private ItemData currentItem;
        private int currentQuantity;
        private Action<ItemData> onSelectCallback;

        public ItemData Item => currentItem;
        public int Quantity => currentQuantity;

        public void Initialize(ItemData item, int quantity, bool isSelected, Action<ItemData> onSelect)
        {
            currentItem = item;
            currentQuantity = quantity;
            onSelectCallback = onSelect;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelectCallback?.Invoke(currentItem));
            }

            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = item.icon != null;
            }

            string rarityHex = ItemData.GetRarityHex(item.rarity);

            if (nameText != null)
            {
                nameText.text = $"<color={rarityHex}>{item.displayName}</color>";
            }

            if (rarityBadgeText != null)
            {
                rarityBadgeText.text = $"<color={rarityHex}>[{item.rarity.ToString().ToUpper()}]</color>";
            }

            if (countText != null)
            {
                countText.text = $"x{quantity}";
            }

            if (priceText != null)
            {
                priceText.text = $"{item.sellPrice} G ea";
            }

            SetSelected(isSelected);
        }

        public void InitializeForBuy(MerchantStockOffer offer, bool isSelected, Action<MerchantStockOffer> onSelect)
        {
            if (offer == null || offer.item == null) return;
            currentItem = offer.item;
            currentQuantity = offer.availableStock;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelect?.Invoke(offer));
            }

            if (iconImage != null)
            {
                iconImage.sprite = offer.item.icon;
                iconImage.enabled = offer.item.icon != null;
            }

            string rarityHex = ItemData.GetRarityHex(offer.item.rarity);

            if (nameText != null)
            {
                nameText.text = $"<color={rarityHex}>{offer.item.displayName}</color>";
            }

            if (rarityBadgeText != null)
            {
                string tag = offer.item.rarity == RarityTier.Mythical ? "★ MYTHICAL ★" : $"[{offer.item.rarity.ToString().ToUpper()}]";
                rarityBadgeText.text = $"<color={rarityHex}>{tag}</color>";
            }

            if (countText != null)
            {
                countText.text = offer.IsSoldOut ? "<color=#FF5555>SOLD OUT</color>" : $"Stock: {offer.availableStock}";
            }

            if (priceText != null)
            {
                priceText.text = $"{offer.pricePerUnit} G";
            }

            SetSelected(isSelected);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.enabled = selected;
            }
        }
    }
}
