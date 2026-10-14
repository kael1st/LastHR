using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingSystem
{
    [Serializable]
    public class MerchantStockOffer
    {
        public ItemData item;
        public int availableStock;
        public int maxStock;
        public int pricePerUnit;

        public bool IsSoldOut => availableStock <= 0;
    }

    /// <summary>
    /// Central manager for The Merchant's rotating seed catalog.
    /// Automatically rolls and restocks a new catalog of seeds every 2 minutes (120 seconds).
    /// Implements rarity weighting where Common/Rare seeds appear frequently, Legendary appears occasionally,
    /// and Mythical Heavenly Seeds are exceptionally rare and exclusive (approx. 2-3% roll on the wildcard slot).
    /// </summary>
    public class MerchantShopManager : MonoBehaviour
    {
        private static MerchantShopManager _instance;
        public static MerchantShopManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MerchantShopManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MerchantShopManager");
                        _instance = go.AddComponent<MerchantShopManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public const float CYCLE_DURATION = 120f; // 2 minutes

        [Header("Cycle Timer")]
        [SerializeField] private float timeRemaining = CYCLE_DURATION;
        [SerializeField] private bool autoRestockEnabled = true;

        [Header("Current Stock Offers")]
        [SerializeField] private List<MerchantStockOffer> currentOffers = new List<MerchantStockOffer>();

        [Header("Available Seeds Database")]
        [SerializeField] private List<ItemData> commonSeeds = new List<ItemData>();
        [SerializeField] private List<ItemData> rareSeeds = new List<ItemData>();
        [SerializeField] private List<ItemData> legendarySeeds = new List<ItemData>();
        [SerializeField] private List<ItemData> mythicalSeeds = new List<ItemData>();

        // Events
        public event Action OnStockRestocked;
        public event Action<float> OnTimerTick;
        public event Action<int, MerchantStockOffer> OnOfferPurchased;

        public float TimeRemaining => timeRemaining;
        public float CycleDuration => CYCLE_DURATION;
        public IReadOnlyList<MerchantStockOffer> CurrentOffers => currentOffers;
        public bool HasMythicalInStock
        {
            get
            {
                foreach (var o in currentOffers)
                {
                    if (o.item != null && o.item.rarity == RarityTier.Mythical && !o.IsSoldOut)
                        return true;
                }
                return false;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSeedDatabaseIfEmpty();
            if (currentOffers.Count == 0)
            {
                RollNewStock();
            }
        }

        private void Update()
        {
            if (!autoRestockEnabled) return;

            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(timeRemaining);

            if (timeRemaining <= 0f)
            {
                timeRemaining = CYCLE_DURATION;
                RollNewStock();
            }
        }

        public void LoadSeedDatabaseIfEmpty()
        {
            if (commonSeeds.Count > 0 || rareSeeds.Count > 0 || legendarySeeds.Count > 0 || mythicalSeeds.Count > 0)
                return;

            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (!item.IsSeed) continue;

                switch (item.rarity)
                {
                    case RarityTier.Common:
                        if (!commonSeeds.Contains(item)) commonSeeds.Add(item);
                        break;
                    case RarityTier.Rare:
                        if (!rareSeeds.Contains(item)) rareSeeds.Add(item);
                        break;
                    case RarityTier.Legendary:
                        if (!legendarySeeds.Contains(item)) legendarySeeds.Add(item);
                        break;
                    case RarityTier.Mythical:
                        if (!mythicalSeeds.Contains(item)) mythicalSeeds.Add(item);
                        break;
                }
            }
        }

        /// <summary>
        /// Rolls a fresh 4-slot catalog of seeds based on rarity weights.
        /// Slot 0: Guaranteed Common (high volume)
        /// Slot 1: Common or Rare (75% / 25%)
        /// Slot 2: Rare or Legendary (75% / 25%)
        /// Slot 3: Wildcard with rare chance for Mythical Heavenly Seeds (2.5% Mythical, 15% Legendary, 35% Rare, 47.5% Common)
        /// </summary>
        public void RollNewStock()
        {
            LoadSeedDatabaseIfEmpty();
            currentOffers.Clear();

            var pickedItems = new HashSet<ItemData>();

            // Slot 0: Common
            ItemData s0 = PickRandomSeed(commonSeeds, pickedItems) ?? PickAnySeed(pickedItems);
            if (s0 != null) AddOffer(s0, pickedItems, UnityEngine.Random.Range(8, 15), s0.buyPrice > 0 ? s0.buyPrice : 10);

            // Slot 1: Common (75%) or Rare (25%)
            float r1 = UnityEngine.Random.value;
            ItemData s1 = null;
            if (r1 < 0.25f && rareSeeds.Count > 0)
                s1 = PickRandomSeed(rareSeeds, pickedItems);
            else
                s1 = PickRandomSeed(commonSeeds, pickedItems);
            if (s1 == null) s1 = PickAnySeed(pickedItems);
            if (s1 != null) AddOffer(s1, pickedItems, UnityEngine.Random.Range(5, 10), s1.buyPrice > 0 ? s1.buyPrice : 20);

            // Slot 2: Rare (75%) or Legendary (25%)
            float r2 = UnityEngine.Random.value;
            ItemData s2 = null;
            if (r2 < 0.25f && legendarySeeds.Count > 0)
                s2 = PickRandomSeed(legendarySeeds, pickedItems);
            else
                s2 = PickRandomSeed(rareSeeds, pickedItems);
            if (s2 == null) s2 = PickAnySeed(pickedItems);
            if (s2 != null) AddOffer(s2, pickedItems, UnityEngine.Random.Range(3, 6), s2.buyPrice > 0 ? s2.buyPrice : 50);

            // Slot 3: Wildcard with Exclusive Mythical Chance
            // 2.5% Mythical (Exclusive!), 15% Legendary, 35% Rare, 47.5% Common
            float r3 = UnityEngine.Random.value;
            ItemData s3 = null;
            int stock3 = 1;

            if (r3 < 0.025f && mythicalSeeds.Count > 0)
            {
                s3 = PickRandomSeed(mythicalSeeds, pickedItems);
                stock3 = 1; // Only 1 Heavenly Seed per exclusive cycle!
            }
            else if (r3 < 0.175f && legendarySeeds.Count > 0)
            {
                s3 = PickRandomSeed(legendarySeeds, pickedItems);
                stock3 = UnityEngine.Random.Range(1, 3);
            }
            else if (r3 < 0.525f && rareSeeds.Count > 0)
            {
                s3 = PickRandomSeed(rareSeeds, pickedItems);
                stock3 = UnityEngine.Random.Range(2, 5);
            }
            else
            {
                s3 = PickRandomSeed(commonSeeds, pickedItems);
                stock3 = UnityEngine.Random.Range(4, 8);
            }

            if (s3 == null) s3 = PickAnySeed(pickedItems);
            if (s3 != null)
            {
                int price = s3.buyPrice > 0 ? s3.buyPrice : (s3.rarity == RarityTier.Mythical ? 800 : 150);
                AddOffer(s3, pickedItems, stock3, price);
            }

            OnStockRestocked?.Invoke();
        }

        private void AddOffer(ItemData item, HashSet<ItemData> pickedItems, int stock, int price)
        {
            if (item == null) return;
            pickedItems.Add(item);
            currentOffers.Add(new MerchantStockOffer
            {
                item = item,
                availableStock = stock,
                maxStock = stock,
                pricePerUnit = price
            });
        }

        private ItemData PickRandomSeed(List<ItemData> pool, HashSet<ItemData> exclude)
        {
            var valid = new List<ItemData>();
            foreach (var it in pool)
            {
                if (it != null && !exclude.Contains(it)) valid.Add(it);
            }
            if (valid.Count == 0) return null;
            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        private ItemData PickAnySeed(HashSet<ItemData> exclude)
        {
            var all = new List<ItemData>();
            all.AddRange(commonSeeds);
            all.AddRange(rareSeeds);
            all.AddRange(legendarySeeds);
            all.AddRange(mythicalSeeds);

            var valid = new List<ItemData>();
            foreach (var it in all)
            {
                if (it != null && !exclude.Contains(it)) valid.Add(it);
            }
            if (valid.Count == 0) return all.Count > 0 ? all[0] : null;
            return valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        public bool TryPurchase(int offerIndex, int quantity, PlayerInventory inventory, out string message)
        {
            message = string.Empty;

            if (offerIndex < 0 || offerIndex >= currentOffers.Count)
            {
                message = "Invalid shop offer selected.";
                return false;
            }

            var offer = currentOffers[offerIndex];
            if (offer == null || offer.item == null)
            {
                message = "Offer is not available.";
                return false;
            }

            if (quantity <= 0)
            {
                message = "Quantity must be at least 1.";
                return false;
            }

            if (offer.availableStock < quantity)
            {
                message = $"Only {offer.availableStock} in stock!";
                return false;
            }

            long totalCost = (long)offer.pricePerUnit * quantity;

            if (CurrencyManager.Instance == null || CurrencyManager.Instance.Coins < totalCost)
            {
                long current = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : 0;
                message = $"Not enough coins! Need {totalCost:N0} G (You have {current:N0} G).";
                return false;
            }

            if (inventory != null && !inventory.CanAddItem(offer.item, quantity))
            {
                message = "Inventory is full! Free up storage space first.";
                return false;
            }

            // Deduct coins
            bool spent = CurrencyManager.Instance.TrySpendCoins(totalCost);
            if (!spent)
            {
                message = "Failed to process payment.";
                return false;
            }

            // Add item to inventory
            if (inventory != null)
            {
                inventory.AddItem(offer.item, quantity);
            }

            // Decrement shop stock
            offer.availableStock -= quantity;

            message = $"Purchased {quantity}x {offer.item.displayName} for {totalCost:N0} G!";
            OnOfferPurchased?.Invoke(offerIndex, offer);
            return true;
        }

        /// <summary>
        /// Force reset the timer and restock immediately (useful for testing or rerolling).
        /// </summary>
        public void ForceRestock()
        {
            timeRemaining = CYCLE_DURATION;
            RollNewStock();
        }

        public string GetFormattedTimer()
        {
            int m = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) / 60f);
            int s = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
