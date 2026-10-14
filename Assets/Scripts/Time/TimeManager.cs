using System;
using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Manages in-game clock time. Tracks hours and minutes progressing in real-time.
    /// The "Next Day" button is only accessible once in-game time has reached or passed 7 PM (19:00).
    /// Includes developer testing tools (hotkeys, time presets, speed scaling).
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Time Settings")]
        [Tooltip("How many real-time seconds equal one in-game hour. Default = 60s (1 game hour per real minute).")]
        [SerializeField] private float realSecondsPerGameHour = 60f;

        [Tooltip("The in-game hour the day starts at (e.g., 6 = 6 AM).")]
        [SerializeField] private int dayStartHour = 6;

        [Tooltip("Earliest in-game hour the player can advance to the next day (24-hour format). Default: 19 = 7 PM.")]
        [SerializeField] private int nextDayUnlockHour = 19;

        [Header("Developer Testing / Debug")]
        [Tooltip("Enable hotkeys (F1-F5, [, ]) for quick time manipulation.")]
        [SerializeField] private bool enableDebugHotkeys = true;

        [Tooltip("Time progression speed multiplier (1 = normal, 5 = 5x faster, 0 = paused).")]
        [Range(0f, 60f)]
        [SerializeField] private float timeSpeedMultiplier = 1f;

        private float currentGameHour;
        private bool timePaused = false;

        // ─── Public accessors ───
        /// <summary>Current in-game hour (0–23).</summary>
        public int CurrentHour => Mathf.FloorToInt(currentGameHour) % 24;

        /// <summary>Current in-game minute (0–59).</summary>
        public int CurrentMinute => Mathf.FloorToInt((currentGameHour % 1f) * 60f);

        public float CurrentGameHourFloat => currentGameHour;

        /// <summary>True when the in-game time has reached or passed the Next Day unlock hour (7 PM).</summary>
        public bool IsNextDayUnlocked => CurrentHour >= nextDayUnlockHour;

        public int NextDayUnlockHour => nextDayUnlockHour;
        public int DayStartHour      => dayStartHour;
        public float TimeSpeedMultiplier
        {
            get => timeSpeedMultiplier;
            set => timeSpeedMultiplier = Mathf.Max(0f, value);
        }
        public bool IsTimePaused => timePaused;

        /// <summary>Fired whenever the displayed minute (or hour) changes.</summary>
        public event Action<int, int> OnTimeChanged;  // (hour, minute)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ResetToStartOfDay();
        }

        private void Update()
        {
            if (enableDebugHotkeys)
            {
                HandleDebugHotkeys();
            }

            if (timePaused || timeSpeedMultiplier <= 0f) return;

            float prevHour = currentGameHour;
            currentGameHour += (Time.deltaTime * timeSpeedMultiplier) / realSecondsPerGameHour;

            // Cap at midnight (24:00) — the player must manually advance to the next day
            if (currentGameHour >= 24f) currentGameHour = 24f;

            // Fire event when the minute or hour changes
            int prevMin = Mathf.FloorToInt((prevHour % 1f) * 60f);
            if (CurrentMinute != prevMin || Mathf.FloorToInt(prevHour) != CurrentHour)
                OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
        }

        // ─── Testing & Control APIs ───

        /// <summary>Set the in-game hour directly (0.0 to 24.0, e.g. 19.5 = 7:30 PM).</summary>
        public void SetGameHour(float hour)
        {
            currentGameHour = Mathf.Clamp(hour, 0f, 24f);
            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
        }

        /// <summary>Set time by hour and minute (e.g. SetTime(19, 0) = 7:00 PM).</summary>
        public void SetTime(int hour, int minute)
        {
            hour = Mathf.Clamp(hour, 0, 23);
            minute = Mathf.Clamp(minute, 0, 59);
            SetGameHour(hour + (minute / 60f));
        }

        /// <summary>Add or subtract hours (e.g. +1.0f or -1.0f).</summary>
        public void AddHours(float deltaHours)
        {
            SetGameHour(currentGameHour + deltaHours);
        }

        /// <summary>Resets in-game time to the start of the day (dayStartHour, default 6 AM).</summary>
        public void ResetToStartOfDay()
        {
            currentGameHour = dayStartHour;
            OnTimeChanged?.Invoke(CurrentHour, CurrentMinute);
        }

        /// <summary>Pause or resume the flow of in-game time.</summary>
        public void SetTimePaused(bool paused) => timePaused = paused;

        /// <summary>Returns formatted time string such as "7:05 PM".</summary>
        public string GetFormattedTime()
        {
            int hour = CurrentHour;
            int minute = CurrentMinute;
            string amPm = hour >= 12 ? "PM" : "AM";
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            return $"{displayHour}:{minute:D2} {amPm}";
        }

        private void HandleDebugHotkeys()
        {
            // F1: Day Start (6:00 AM)
            if (Input.GetKeyDown(KeyCode.F1))
            {
                SetTime(6, 0);
                NotifyDebug($"[Test] Time set to 6:00 AM (Day Start)");
            }
            // F2: Noon (12:00 PM)
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                SetTime(12, 0);
                NotifyDebug($"[Test] Time set to 12:00 PM (Noon)");
            }
            // F3: 6:55 PM (5 mins before unlock - Test Locked button)
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                SetTime(18, 55);
                NotifyDebug($"[Test] Time set to 6:55 PM (Locked: Next Day unavailable)");
            }
            // F4: 7:00 PM (Unlock threshold - Test Unlocked button)
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                SetTime(19, 0);
                NotifyDebug($"[Test] Time set to 7:00 PM (UNLOCKED: Next Day available!)");
            }
            // F5: 10:00 PM (Late Night)
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                SetTime(22, 0);
                NotifyDebug($"[Test] Time set to 10:00 PM (Night)");
            }
            // [ : -1 Hour
            else if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                AddHours(-1f);
                NotifyDebug($"[Test] Time -1h -> {GetFormattedTime()}");
            }
            // ] : +1 Hour
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                AddHours(1f);
                NotifyDebug($"[Test] Time +1h -> {GetFormattedTime()}");
            }
            // P : Toggle Pause time
            else if (Input.GetKeyDown(KeyCode.P))
            {
                timePaused = !timePaused;
                NotifyDebug(timePaused ? "[Test] In-game Time PAUSED" : "[Test] In-game Time RESUMED");
            }
        }

        private void NotifyDebug(string msg)
        {
            if (InventoryUI.Instance != null)
                InventoryUI.Instance.ShowNotification(msg, true);
            else
                Debug.Log(msg);
        }

        // ─── Unity Context Menu Helpers for Inspector ───
        [ContextMenu("Set to 6:00 AM (Morning)")]
        private void CtxSet6AM() => SetTime(6, 0);

        [ContextMenu("Set to 12:00 PM (Noon)")]
        private void CtxSet12PM() => SetTime(12, 0);

        [ContextMenu("Set to 6:55 PM (Locked)")]
        private void CtxSet655PM() => SetTime(18, 55);

        [ContextMenu("Set to 7:00 PM (Unlocked)")]
        private void CtxSet700PM() => SetTime(19, 0);

        [ContextMenu("Set to 10:00 PM (Night)")]
        private void CtxSet10PM() => SetTime(22, 0);
    }
}
