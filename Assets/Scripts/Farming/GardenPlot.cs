using System.Collections;
using UnityEngine;

namespace FarmingSystem
{
    public enum PlotSoilState
    {
        Untilled,
        TilledDry,
        TilledWatered
    }

    /// <summary>
    /// Represents a single tile/plot of farmland.
    /// Handles tilling, watering, planting, real-time/day-cycle growth, and harvesting.
    /// </summary>
    public class GardenPlot : MonoBehaviour
    {
        [Header("Plot Status")]
        [SerializeField] private PlotSoilState soilState = PlotSoilState.Untilled;
        [SerializeField] private CropData currentCrop;
        [SerializeField] private int currentGrowthStage = 0;
        [SerializeField] private bool isRipe = false;
        [SerializeField] private float stageTimer = 0f;

        [Header("Growth Settings")]
        [Tooltip("If true, watered crops grow automatically in real-time over seconds. Can also be advanced via Day Cycle.")]
        [SerializeField] private bool enableRealtimeGrowth = true;

        [Header("Visual Renderers")]
        [SerializeField] private SpriteRenderer soilRenderer;
        [SerializeField] private SpriteRenderer cropRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;

        [Header("Soil Sprites")]
        [SerializeField] private Sprite untilledSprite;
        [SerializeField] private Sprite tilledDrySprite;
        [SerializeField] private Sprite tilledWateredSprite;

        // Public properties
        public PlotSoilState SoilState => soilState;
        public CropData CurrentCrop => currentCrop;
        public int CurrentGrowthStage => currentGrowthStage;
        public bool IsRipe => isRipe;
        public bool HasCrop => currentCrop != null;
        public bool IsWatered => soilState == PlotSoilState.TilledWatered;
        public bool IsTilled => soilState != PlotSoilState.Untilled;

        private void Awake()
        {
            EnsureComponents();
        }

        private void Start()
        {
            UpdateVisuals();
            SetHighlight(false);
        }

        private void Update()
        {
            if (enableRealtimeGrowth && IsWatered && HasCrop && !isRipe)
            {
                UpdateRealtimeGrowth();
            }
        }

        private void EnsureComponents()
        {
            if (soilRenderer == null)
            {
                Transform soilTrans = transform.Find("SoilVisual");
                if (soilTrans != null)
                    soilRenderer = soilTrans.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject soilObj = new GameObject("SoilVisual");
                    soilObj.transform.SetParent(transform, false);
                    soilRenderer = soilObj.AddComponent<SpriteRenderer>();
                }
            }

            // Ensure soil is behind characters and crops
            soilRenderer.sortingOrder = -500;

            if (cropRenderer == null)
            {
                Transform cropTrans = transform.Find("CropVisual");
                if (cropTrans != null)
                    cropRenderer = cropTrans.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject cropObj = new GameObject("CropVisual");
                    cropObj.transform.SetParent(transform, false);
                    cropObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                    cropRenderer = cropObj.AddComponent<SpriteRenderer>();
                }
            }

            // Crop sorting based on Y for proper depth
            cropRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + 10;

            if (highlightRenderer == null)
            {
                Transform hlTrans = transform.Find("HighlightVisual");
                if (hlTrans != null)
                    highlightRenderer = hlTrans.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject hlObj = new GameObject("HighlightVisual");
                    hlObj.transform.SetParent(transform, false);
                    highlightRenderer = hlObj.AddComponent<SpriteRenderer>();
                }
            }
            highlightRenderer.sortingOrder = -490;
        }

        public void ConfigureSprites(Sprite untilled, Sprite tilledDry, Sprite tilledWatered)
        {
            EnsureComponents();
            untilledSprite = untilled;
            tilledDrySprite = tilledDry;
            tilledWateredSprite = tilledWatered;
            UpdateVisuals();
        }

        public void ConfigureHighlightSprite(Sprite highlight)
        {
            EnsureComponents();
            if (highlightRenderer != null)
            {
                highlightRenderer.sprite = highlight;
            }
        }

        public void SetHighlight(bool active)
        {
            EnsureComponents();
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = active;
            }
        }

        public void UpdateVisuals()
        {
            EnsureComponents();

            // 1. Soil Sprite
            if (soilRenderer != null)
            {
                switch (soilState)
                {
                    case PlotSoilState.Untilled:
                        soilRenderer.sprite = untilledSprite;
                        soilRenderer.color = Color.white;
                        break;
                    case PlotSoilState.TilledDry:
                        soilRenderer.sprite = tilledDrySprite != null ? tilledDrySprite : untilledSprite;
                        soilRenderer.color = Color.white;
                        break;
                    case PlotSoilState.TilledWatered:
                        soilRenderer.sprite = tilledWateredSprite != null ? tilledWateredSprite : tilledDrySprite;
                        soilRenderer.color = Color.white;
                        break;
                }
            }

            // 2. Crop Sprite
            if (cropRenderer != null)
            {
                if (currentCrop != null)
                {
                    cropRenderer.enabled = true;
                    cropRenderer.sprite = currentCrop.GetSpriteForStage(currentGrowthStage);
                }
                else
                {
                    cropRenderer.enabled = false;
                    cropRenderer.sprite = null;
                }
            }
        }

        private void UpdateRealtimeGrowth()
        {
            stageTimer += Time.deltaTime;
            float duration = currentCrop.realtimeSecondsPerStage;

            if (stageTimer >= duration)
            {
                stageTimer = 0f;
                AdvanceGrowthStage();
            }
        }

        public void AdvanceGrowthStage()
        {
            if (currentCrop == null || isRipe) return;

            currentGrowthStage++;
            if (currentGrowthStage >= currentCrop.MaxStage)
            {
                currentGrowthStage = currentCrop.MaxStage;
                isRipe = true;
            }

            UpdateVisuals();
            if (Application.isPlaying && cropRenderer != null)
            {
                StartCoroutine(AnimateBounce(cropRenderer.transform, 1.15f, 0.15f));
            }
        }

        public void AdvanceDay()
        {
            // Harvest Moon mechanic: Watered soil advances growth by 1 stage, then dries out
            if (soilState == PlotSoilState.TilledWatered)
            {
                if (HasCrop && !isRipe)
                {
                    AdvanceGrowthStage();
                }
                // Soil dries out the next day
                soilState = PlotSoilState.TilledDry;
            }

            stageTimer = 0f;
            UpdateVisuals();
        }

        public bool Till()
        {
            if (soilState != PlotSoilState.Untilled) return false;

            soilState = PlotSoilState.TilledDry;
            UpdateVisuals();
            if (Application.isPlaying && soilRenderer != null)
            {
                StartCoroutine(AnimateBounce(soilRenderer.transform, 1.1f, 0.12f));
            }
            return true;
        }

        public bool Water()
        {
            if (soilState == PlotSoilState.Untilled) return false;
            if (soilState == PlotSoilState.TilledWatered) return false;

            soilState = PlotSoilState.TilledWatered;
            UpdateVisuals();
            if (Application.isPlaying && soilRenderer != null)
            {
                StartCoroutine(AnimateBounce(soilRenderer.transform, 1.08f, 0.12f));
            }
            return true;
        }

        public bool Plant(ItemData seedItem)
        {
            if (soilState == PlotSoilState.Untilled) return false;
            if (HasCrop) return false;
            if (seedItem == null || seedItem.cropData == null) return false;

            currentCrop = seedItem.cropData;
            currentGrowthStage = 0;
            isRipe = false;
            stageTimer = 0f;

            UpdateVisuals();
            if (Application.isPlaying && cropRenderer != null)
            {
                StartCoroutine(AnimateBounce(cropRenderer.transform, 1.25f, 0.2f));
            }
            return true;
        }

        public bool Harvest(PlayerInventory inventory, out ItemData harvestedItem, out int yieldAmount)
        {
            harvestedItem = null;
            yieldAmount = 0;

            if (!HasCrop || !isRipe) return false;

            harvestedItem = currentCrop.harvestItem;
            yieldAmount = Random.Range(currentCrop.minHarvestYield, currentCrop.maxHarvestYield + 1);

            if (inventory != null && harvestedItem != null)
            {
                inventory.AddItem(harvestedItem, yieldAmount);
            }

            // Regrow or clear
            if (currentCrop.regrows)
            {
                currentGrowthStage = currentCrop.regrowStage;
                isRipe = false;
                stageTimer = 0f;
            }
            else
            {
                currentCrop = null;
                currentGrowthStage = 0;
                isRipe = false;
                stageTimer = 0f;
            }

            UpdateVisuals();
            return true;
        }

        /// <summary>
        /// Unified interaction handler based on active inventory item.
        /// </summary>
        public bool Interact(PlayerInventory inventory, out string feedbackMessage)
        {
            feedbackMessage = string.Empty;

            // 1. If ripe, harvest first regardless of tool held!
            if (HasCrop && isRipe)
            {
                string cropName = currentCrop.cropName;
                if (Harvest(inventory, out ItemData harvestedItem, out int count))
                {
                    feedbackMessage = $"Harvested {count}x {cropName}!";
                    return true;
                }
            }

            // Get currently held item
            ItemData heldItem = inventory != null ? inventory.GetSelectedItem() : null;

            // 2. Hoe interaction
            if (heldItem != null && heldItem.IsTool && heldItem.toolType == ToolType.Hoe)
            {
                if (soilState == PlotSoilState.Untilled)
                {
                    Till();
                    feedbackMessage = "Tilled the soil!";
                    return true;
                }
                else
                {
                    feedbackMessage = "Plot is already tilled.";
                    return false;
                }
            }

            // 3. Watering Can interaction
            if (heldItem != null && heldItem.IsTool && heldItem.toolType == ToolType.WateringCan)
            {
                if (soilState == PlotSoilState.Untilled)
                {
                    feedbackMessage = "Must till soil with Hoe first!";
                    return false;
                }
                else if (soilState == PlotSoilState.TilledWatered)
                {
                    feedbackMessage = "Plot is already watered.";
                    return false;
                }
                else
                {
                    Water();
                    feedbackMessage = "Watered the soil!";
                    return true;
                }
            }

            // 4. Seed Pack interaction
            if (heldItem != null && heldItem.IsSeed)
            {
                if (soilState == PlotSoilState.Untilled)
                {
                    feedbackMessage = "Must till soil before planting seeds!";
                    return false;
                }
                else if (HasCrop)
                {
                    feedbackMessage = "A crop is already planted here.";
                    return false;
                }
                else
                {
                    string cropName = heldItem.cropData.cropName;
                    if (Plant(heldItem))
                    {
                        inventory.ConsumeSelectedItem(1);
                        feedbackMessage = $"Planted {cropName} seeds!";
                        return true;
                    }
                }
            }

            // 5. Context feedback for inspect / other
            if (HasCrop)
            {
                string waterStatus = IsWatered ? "Watered (Growing)" : "Needs Water!";
                feedbackMessage = $"{currentCrop.cropName} (Stage {currentGrowthStage + 1}/{currentCrop.MaxStage + 1}) - {waterStatus}";
                return false;
            }

            if (soilState == PlotSoilState.Untilled)
            {
                feedbackMessage = "Use the Hoe [1] to till this soil.";
            }
            else if (soilState == PlotSoilState.TilledDry)
            {
                feedbackMessage = "Plant seeds [3] or water with Watering Can [2].";
            }
            else
            {
                feedbackMessage = "Soil is watered! Plant seeds [3].";
            }

            return false;
        }

        private IEnumerator AnimateBounce(Transform target, float maxScale, float duration)
        {
            if (target == null) yield break;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(maxScale, 1.0f, t);
                target.localScale = originalScale * scale;
                yield return null;
            }

            target.localScale = originalScale;
        }
    }
}
