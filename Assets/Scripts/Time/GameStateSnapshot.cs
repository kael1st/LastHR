using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// A lightweight snapshot of the game state captured at the start of each day.
    /// Stores inventory contents and all garden plot states so the Pocket Watch can restore them.
    /// </summary>
    [Serializable]
    public class GameStateSnapshot
    {
        // ────────── Inventory ──────────
        [Serializable]
        public class SlotSnapshot
        {
            /// <summary>ItemData.id of the item in this slot, or empty if the slot is empty.</summary>
            public string itemId;
            public int quantity;
        }

        public List<SlotSnapshot> inventorySlots = new List<SlotSnapshot>();

        // ────────── Garden Plots ──────────
        [Serializable]
        public class PlotSnapshot
        {
            /// <summary>Full scene hierarchy path used to identify the plot after restore.</summary>
            public string plotPath;
            public PlotSoilState soilState;
            /// <summary>Name of the CropData ScriptableObject, or empty if no crop is planted.</summary>
            public string cropDataName;
            public int growthStage;
            public bool isRipe;
            public float stageTimer;
        }

        public List<PlotSnapshot> plotSnapshots = new List<PlotSnapshot>();

        // ────────── Calendar ──────────
        public int day;
        public string season;
    }
}
