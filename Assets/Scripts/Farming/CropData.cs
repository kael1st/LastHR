using UnityEngine;

namespace FarmingSystem
{
    [CreateAssetMenu(fileName = "NewCrop", menuName = "Farming/Crop Data")]
    public class CropData : ScriptableObject
    {
        [Header("Crop Info")]
        public string cropName = "Turnip";
        public RarityTier rarity = RarityTier.Common;

        [Header("Harvest Reward")]
        [Tooltip("The item given to the player when harvested.")]
        public ItemData harvestItem;
        public int minHarvestYield = 1;
        public int maxHarvestYield = 2;

        [Header("Growth Stages")]
        [Tooltip("Number of seconds per stage when watered in real-time mode.")]
        public float realtimeSecondsPerStage = 6f;
        [Tooltip("Number of in-game days to mature (advances 1 stage per watered day).")]
        public int daysToMature = 3;

        [Tooltip("Sprites corresponding to each growth stage (0 = Planted Seed, 1 = Sprout, 2 = Growing, 3 = Mature).")]
        public Sprite[] stageSprites = new Sprite[4];

        [Header("Regrowth (Optional)")]
        [Tooltip("If true, crop produces produce repeatedly without replanting.")]
        public bool regrows = false;
        [Tooltip("The stage the crop resets to after harvesting if regrows is true.")]
        public int regrowStage = 1;
        [Tooltip("Maximum harvest cycles before the bush/plant is exhausted and removed (0 = unlimited).")]
        public int maxHarvestCycles = 0;

        public int MaxStage => stageSprites != null && stageSprites.Length > 0 ? stageSprites.Length - 1 : 3;

        public Sprite GetSpriteForStage(int stage)
        {
            if (stageSprites == null || stageSprites.Length == 0) return null;
            int clamped = Mathf.Clamp(stage, 0, stageSprites.Length - 1);
            return stageSprites[clamped];
        }
    }
}
