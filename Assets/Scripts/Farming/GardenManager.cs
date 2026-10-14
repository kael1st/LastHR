using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Coordinates all garden plots, tracks time/days, and handles day cycle transitions.
    /// </summary>
    public class GardenManager : MonoBehaviour
    {
        public static GardenManager Instance { get; private set; }

        [Header("Calendar")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private string season = "Spring";

        [Header("Registered Plots")]
        [SerializeField] private List<GardenPlot> registeredPlots = new List<GardenPlot>();

        public int CurrentDay => currentDay;
        public string Season => season;

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
        }

        public void RegisterPlot(GardenPlot plot)
        {
            if (plot != null && !registeredPlots.Contains(plot))
            {
                registeredPlots.Add(plot);
            }
        }

        public void UnregisterPlot(GardenPlot plot)
        {
            if (plot != null)
            {
                registeredPlots.Remove(plot);
            }
        }

        public void RefreshPlotsList()
        {
            registeredPlots.Clear();
            registeredPlots.AddRange(FindObjectsOfType<GardenPlot>());
        }

        public void AdvanceDay()
        {
            currentDay++;

            RefreshPlotsList();
            int wateredCount = 0;

            foreach (var plot in registeredPlots)
            {
                if (plot != null)
                {
                    if (plot.IsWatered && plot.HasCrop && !plot.IsRipe)
                    {
                        wateredCount++;
                    }
                    plot.AdvanceDay();
                }
            }

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
    }
}
