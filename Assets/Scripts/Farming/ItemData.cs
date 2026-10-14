using UnityEngine;

namespace FarmingSystem
{
    public enum ItemType
    {
        Tool,
        Seed,
        Crop,
        Consumable
    }

    public enum ToolType
    {
        None,
        Hoe,
        WateringCan,
        Scythe,
        Axe
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

        [Header("Inventory & Visuals")]
        public Sprite icon;
        public int maxStack = 99;

        [Header("Farming Properties (Only for Seeds)")]
        [Tooltip("The crop this seed produces when planted on tilled soil.")]
        public CropData cropData;

        public bool IsTool => itemType == ItemType.Tool;
        public bool IsSeed => itemType == ItemType.Seed && cropData != null;
        public bool IsCrop => itemType == ItemType.Crop;
    }
}
