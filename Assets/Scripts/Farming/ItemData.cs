using UnityEngine;

namespace FarmingSystem
{
    public enum ItemType
    {
        Tool,
        Seed,
        Crop,
        Consumable,
        Special     // Used for unique items like the Pocket Watch
    }

    public enum ToolType
    {
        None,
        Hoe,
        WateringCan,
        Scythe,
        Axe,
        Shovel
    }

    /// <summary>
    /// Special item sub-type for items that trigger unique world interactions.
    /// </summary>
    public enum SpecialItemType
    {
        None,
        PocketWatch     // Rewinds time to the previous day
    }

    public enum RarityTier
    {
        Common,
        Rare,
        Legendary,
        Mythical
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Farming/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Identity")]
        public string id = "item_id";
        public string displayName = "Item Name";
        [TextArea(2, 4)]
        public string description = "Item description.";

        [Header("Classification")]
        public ItemType itemType = ItemType.Tool;
        public ToolType toolType = ToolType.None;

        [Header("Rarity & Value")]
        public RarityTier rarity = RarityTier.Common;
        public int sellPrice = 10;
        public int buyPrice = 20;

        [Header("Special Item")]
        [Tooltip("Only relevant when itemType is Special.")]
        public SpecialItemType specialItemType = SpecialItemType.None;

        [Header("Inventory & Visuals")]
        public Sprite icon;
        public int maxStack = 99;

        [Header("Farming Properties (Only for Seeds)")]
        [Tooltip("The crop this seed produces when planted on tilled soil.")]
        public CropData cropData;

        public bool IsTool        => itemType == ItemType.Tool;
        public bool IsShovel      => IsTool && toolType == ToolType.Shovel;
        public bool IsSeed        => itemType == ItemType.Seed && cropData != null;
        public bool IsCrop        => itemType == ItemType.Crop;
        public bool IsSpecial     => itemType == ItemType.Special;
        public bool IsPocketWatch => IsSpecial && specialItemType == SpecialItemType.PocketWatch;
        public bool IsSellableToVendor => itemType == ItemType.Crop && sellPrice > 0;

        public Color GetRarityColor() => GetRarityColor(rarity);

        public static Color GetRarityColor(RarityTier tier)
        {
            switch (tier)
            {
                case RarityTier.Common:
                    return new Color(0.82f, 0.85f, 0.88f); // Crisp clean silver-white
                case RarityTier.Rare:
                    return new Color(0.28f, 0.65f, 1.0f);  // Azure cyan-blue
                case RarityTier.Legendary:
                    return new Color(1.0f, 0.76f, 0.12f);  // Radiant golden amber
                case RarityTier.Mythical:
                    return new Color(0.94f, 0.38f, 1.0f);  // Luminous cosmic magenta
                default:
                    return Color.white;
            }
        }

        public static string GetRarityHex(RarityTier tier)
        {
            switch (tier)
            {
                case RarityTier.Common:    return "#D2D8DF";
                case RarityTier.Rare:      return "#47A7FF";
                case RarityTier.Legendary: return "#FFC21F";
                case RarityTier.Mythical:  return "#F060FF";
                default:                   return "#FFFFFF";
            }
        }
    }
}
