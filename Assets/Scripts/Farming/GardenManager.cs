using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Coordinates all garden plots, tracks time/days, and handles day cycle transitions.
    /// Captures a snapshot before each AdvanceDay into a history stack so the Pocket Watch
    /// can rewind back to the previous day (including all plots, crops, soil states, and inventory items).
    /// </summary>
    public class GardenManager : MonoBehaviour
    {
        public static GardenManager Instance { get; private set; }

        [Header("Calendar")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private string season = "Spring";

        [Header("Registered Plots")]
        [SerializeField] private List<GardenPlot> registeredPlots = new List<GardenPlot>();

        [Header("Item & Crop Database (Auto-discovered if empty)")]
        [SerializeField] private List<ItemData> knownItems = new List<ItemData>();
        [SerializeField] private List<CropData> knownCrops = new List<CropData>();

        public int CurrentDay => currentDay;
        public string Season => season;

        /// <summary>Stack of daily snapshots. Each AdvanceDay pushes the previous day's state.</summary>
        private Stack<GameStateSnapshot> snapshotHistory = new Stack<GameStateSnapshot>();

        /// <summary>True when at least one previous day snapshot is recorded and rewind is possible.</summary>
        public bool HasPreviousSnapshot => snapshotHistory != null && snapshotHistory.Count > 0;

        public event Action<int, string> OnDayChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RefreshPlotsList();
            AutoPopulateDatabases();
        }

        public void RegisterPlot(GardenPlot plot)
        {
            if (plot != null && !registeredPlots.Contains(plot))
                registeredPlots.Add(plot);
        }

        public void UnregisterPlot(GardenPlot plot)
        {
            if (plot != null)
                registeredPlots.Remove(plot);
        }

        public void RefreshPlotsList()
        {
            registeredPlots.Clear();
            registeredPlots.AddRange(FindObjectsOfType<GardenPlot>());
        }

        private void AutoPopulateDatabases()
        {
            // Gather known items from PlayerInventory starting items & Resources
            if (PlayerInventory.Instance != null)
            {
                var inv = PlayerInventory.Instance;
                foreach (var s in inv.Slots)
                {
                    if (s != null && s.Item != null && !knownItems.Contains(s.Item))
                        knownItems.Add(s.Item);
                }
            }

            var loadedItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in loadedItems)
            {
                if (item != null && !knownItems.Contains(item))
                    knownItems.Add(item);
            }

            var loadedCrops = Resources.FindObjectsOfTypeAll<CropData>();
            foreach (var crop in loadedCrops)
            {
                if (crop != null && !knownCrops.Contains(crop))
                    knownCrops.Add(crop);
            }
        }

        /// <summary>
        /// Saves a snapshot of the current state, then advances to the next day.
        /// </summary>
        public void AdvanceDay()
        {
            // Capture the world state BEFORE advancing, pushing onto history stack
            snapshotHistory.Push(CaptureSnapshot());

            currentDay++;

            RefreshPlotsList();
            int wateredCount = 0;

            foreach (var plot in registeredPlots)
            {
                if (plot != null)
                {
                    if (plot.IsWatered && plot.HasCrop && !plot.IsRipe)
                        wateredCount++;
                    plot.AdvanceDay();
                }
            }

            // Reset in-game clock to the start of the new day
            if (TimeManager.Instance != null)
                TimeManager.Instance.ResetToStartOfDay();

            OnDayChanged?.Invoke(currentDay, season);

            string message = wateredCount > 0
                ? $"Day {currentDay} ({season}) - {wateredCount} crop(s) grew!"
                : $"Day {currentDay} ({season})";

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.ShowNotification(message, true);
                InventoryUI.Instance.UpdateDayDisplay(currentDay, season);
            }
        }

        /// <summary>
        /// Restores the game world to the snapshot captured on the previous day.
        /// Pops from history stack so player can rewind day-by-day.
        /// </summary>
        public void RewindDay()
        {
            if (!HasPreviousSnapshot)
            {
                if (InventoryUI.Instance != null)
                    InventoryUI.Instance.ShowNotification("The Pocket Watch has no memory of yesterday...", false);
                return;
            }

            var snapshot = snapshotHistory.Pop();
            ApplySnapshot(snapshot);

            // Reset clock to start of the restored day
            if (TimeManager.Instance != null)
                TimeManager.Instance.ResetToStartOfDay();

            OnDayChanged?.Invoke(currentDay, season);

            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.ShowNotification($"Time rewound back to Day {currentDay}!", true);
                InventoryUI.Instance.UpdateDayDisplay(currentDay, season);
                InventoryUI.Instance.RefreshUI();
            }
        }

        // ─────────────────── Snapshot helpers ───────────────────

        private GameStateSnapshot CaptureSnapshot()
        {
            var snap = new GameStateSnapshot();
            snap.day    = currentDay;
            snap.season = season;

            // Inventory
            if (PlayerInventory.Instance != null)
            {
                foreach (var slot in PlayerInventory.Instance.Slots)
                {
                    snap.inventorySlots.Add(new GameStateSnapshot.SlotSnapshot
                    {
                        itemId   = slot.IsEmpty ? string.Empty : slot.Item.id,
                        quantity = slot.IsEmpty ? 0 : slot.Quantity
                    });
                }
            }

            // Garden plots
            RefreshPlotsList();
            foreach (var plot in registeredPlots)
            {
                if (plot == null) continue;
                snap.plotSnapshots.Add(new GameStateSnapshot.PlotSnapshot
                {
                    plotPath     = GetHierarchyPath(plot.transform),
                    soilState    = plot.SoilState,
                    cropDataName = plot.CurrentCrop != null ? plot.CurrentCrop.name : string.Empty,
                    growthStage  = plot.CurrentGrowthStage,
                    isRipe       = plot.IsRipe,
                    stageTimer   = 0f
                });
            }

            return snap;
        }

        private void ApplySnapshot(GameStateSnapshot snap)
        {
            // Restore calendar
            currentDay = snap.day;
            season     = snap.season;

            // Restore inventory
            if (PlayerInventory.Instance != null && snap.inventorySlots != null)
            {
                var allItems = GetItemLookup();
                var slots    = PlayerInventory.Instance.Slots;
                for (int i = 0; i < slots.Length && i < snap.inventorySlots.Count; i++)
                {
                    var ss = snap.inventorySlots[i];
                    if (string.IsNullOrEmpty(ss.itemId) || ss.quantity <= 0)
                    {
                        slots[i].Clear();
                    }
                    else if (allItems.TryGetValue(ss.itemId, out ItemData item))
                    {
                        slots[i].Set(item, ss.quantity);
                    }
                    else
                    {
                        slots[i].Clear();
                    }
                }
                PlayerInventory.Instance.NotifyInventoryChanged();
            }

            // Restore garden plots
            RefreshPlotsList();
            if (snap.plotSnapshots != null)
            {
                var allCrops = GetCropLookup();

                foreach (var ps in snap.plotSnapshots)
                {
                    GardenPlot target = null;
                    foreach (var plot in registeredPlots)
                    {
                        if (plot != null && GetHierarchyPath(plot.transform) == ps.plotPath)
                        {
                            target = plot;
                            break;
                        }
                    }
                    if (target == null) continue;

                    CropData crop = null;
                    if (!string.IsNullOrEmpty(ps.cropDataName))
                        allCrops.TryGetValue(ps.cropDataName, out crop);

                    target.RestoreFromSnapshot(ps.soilState, crop, ps.growthStage, ps.isRipe, ps.stageTimer);
                }
            }
        }

        // ─────────────────── Utility helpers ───────────────────

        private static string GetHierarchyPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetHierarchyPath(t.parent) + "/" + t.name;
        }

        private Dictionary<string, ItemData> GetItemLookup()
        {
            var dict = new Dictionary<string, ItemData>();
            foreach (var item in knownItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.id) && !dict.ContainsKey(item.id))
                    dict[item.id] = item;
            }
            return dict;
        }

        private Dictionary<string, CropData> GetCropLookup()
        {
            var dict = new Dictionary<string, CropData>();
            foreach (var crop in knownCrops)
            {
                if (crop != null && !dict.ContainsKey(crop.name))
                    dict[crop.name] = crop;
            }
            return dict;
        }
    }
}
