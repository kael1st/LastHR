using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// Developer & Feature Testing HUD for the in-game Time mechanic.
    /// Provides an on-screen interactive, fully draggable UI panel with:
    /// - Draggable header bar: click & drag anywhere on the title bar or background to reposition
    /// - Custom Text Input for typing exact hours (e.g., 19.5, 7, 18.9)
    /// - Time Slider (0 to 24 hours) for live dragging
    /// - Quick-jump preset buttons (6 AM, 12 PM, 6:55 PM [Locked], 7:00 PM [Unlocked], 10 PM)
    /// - Step buttons (+1h, -1h, +30m)
    /// - Speed controls (1x, 5x, 10x, Pause)
    /// - Live unlock status badge (shows if Next Day is currently LOCKED or UNLOCKED)
    /// </summary>
    public class TimeTestingHUD : MonoBehaviour
    {
        public static TimeTestingHUD Instance { get; private set; }

        [Header("UI References (Auto-created if null)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text statusText;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private InputField hourInputField;
        [SerializeField] private Text sliderValueText;
        [SerializeField] private Button toggleVisibilityButton;

        [Header("Settings")]
        [SerializeField] private bool startOpen = true;

        private bool isInternalUpdate = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            BuildHUD();

            if (panelRoot != null)
                panelRoot.SetActive(startOpen);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged += OnTimeChanged;
                UpdateDisplay(TimeManager.Instance.CurrentHour, TimeManager.Instance.CurrentMinute);
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnTimeChanged -= OnTimeChanged;
        }

        private void Update()
        {
            // Toggle panel with Backquote / Tilde (`) or Backslash (\)
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Backslash))
            {
                ToggleVisibility();
            }

            // Sync slider if time is running normally and user is not dragging
            if (TimeManager.Instance != null && timeSlider != null && !isInternalUpdate)
            {
                isInternalUpdate = true;
                timeSlider.value = TimeManager.Instance.CurrentGameHourFloat;
                isInternalUpdate = false;
            }
        }

        private void OnTimeChanged(int hour, int minute)
        {
            UpdateDisplay(hour, minute);
        }

        private void UpdateDisplay(int hour, int minute)
        {
            if (TimeManager.Instance == null) return;

            string timeStr = TimeManager.Instance.GetFormattedTime();
            bool isUnlocked = TimeManager.Instance.IsNextDayUnlocked;
            string lockStatus = isUnlocked
                ? "<color=#55FF55><b>[NEXT DAY UNLOCKED]</b></color>"
                : "<color=#FF6666><b>[LOCKED (Needs >= 7:00 PM)]</b></color>";

            if (statusText != null)
            {
                statusText.text = $"<b>Time:</b> {timeStr}  {lockStatus}";
            }

            if (sliderValueText != null)
            {
                sliderValueText.text = $"{timeStr} ({TimeManager.Instance.CurrentGameHourFloat:F1}h)";
            }
        }

        public void ToggleVisibility()
        {
            if (panelRoot != null)
            {
                bool nextState = !panelRoot.activeSelf;
                panelRoot.SetActive(nextState);
            }
        }

        // ─── Button / Input Callbacks ───

        public void OnSetPreset(float hour)
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.SetGameHour(hour);
            if (hourInputField != null)
                hourInputField.text = hour.ToString("F1", CultureInfo.InvariantCulture);
        }

        public void OnAddHours(float deltaHours)
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.AddHours(deltaHours);
        }

        public void OnSliderChanged(float val)
        {
            if (isInternalUpdate || TimeManager.Instance == null) return;
            TimeManager.Instance.SetGameHour(val);
        }

        public void OnApplyInputField()
        {
            if (TimeManager.Instance == null || hourInputField == null) return;
            string text = hourInputField.text.Trim();
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedHour))
            {
                TimeManager.Instance.SetGameHour(parsedHour);
                if (InventoryUI.Instance != null)
                    InventoryUI.Instance.ShowNotification($"[Test] Time set to {TimeManager.Instance.GetFormattedTime()}", true);
            }
            else
            {
                if (InventoryUI.Instance != null)
                    InventoryUI.Instance.ShowNotification("Enter valid hour (e.g. 19 or 19.5)", false);
            }
        }

        public void OnSetSpeed(float speed)
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.TimeSpeedMultiplier = speed;
            if (InventoryUI.Instance != null)
                InventoryUI.Instance.ShowNotification($"[Test] Time Speed: {speed}x", true);
        }

        public void OnTogglePause()
        {
            if (TimeManager.Instance == null) return;
            bool nextPaused = !TimeManager.Instance.IsTimePaused;
            TimeManager.Instance.SetTimePaused(nextPaused);
            if (InventoryUI.Instance != null)
                InventoryUI.Instance.ShowNotification(nextPaused ? "[Test] Time PAUSED" : "[Test] Time RESUMED", true);
        }

        // ─── Programmatic UI Construction ───

        public void BuildHUD()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Destroy stale unhooked instances from editor scene saving if any
            Transform existingBtn = canvas.transform.Find("TimeTestToggleBtn");
            if (existingBtn != null) Destroy(existingBtn.gameObject);

            Transform existingPanel = canvas.transform.Find("TimeTestingPanel");
            if (existingPanel != null) Destroy(existingPanel.gameObject);

            // 1. Toggle Button (top center, completely clear of DayHUD and ControlsGuide)
            var toggleGO = new GameObject("TimeTestToggleBtn");
            toggleGO.transform.SetParent(canvas.transform, false);
            var toggleRT = toggleGO.AddComponent<RectTransform>();
            toggleRT.anchorMin = new Vector2(0.5f, 1f);
            toggleRT.anchorMax = new Vector2(0.5f, 1f);
            toggleRT.pivot     = new Vector2(0.5f, 1f);
            toggleRT.anchoredPosition = new Vector2(0, -10);
            toggleRT.sizeDelta        = new Vector2(170, 32);
            var toggleImg = toggleGO.AddComponent<Image>();
            toggleImg.color = new Color(0.18f, 0.22f, 0.35f, 0.95f);
            toggleVisibilityButton = toggleGO.AddComponent<Button>();
            toggleVisibilityButton.onClick.AddListener(ToggleVisibility);

            var toggleTxtGO = new GameObject("Text");
            toggleTxtGO.transform.SetParent(toggleGO.transform, false);
            var toggleTxtRT = toggleTxtGO.AddComponent<RectTransform>();
            toggleTxtRT.anchorMin = Vector2.zero; toggleTxtRT.anchorMax = Vector2.one;
            toggleTxtRT.offsetMin = toggleTxtRT.offsetMax = Vector2.zero;
            var toggleTxt = toggleTxtGO.AddComponent<Text>();
            toggleTxt.text = "⏱ TIME TESTER";
            toggleTxt.fontSize = 13;
            toggleTxt.fontStyle = FontStyle.Bold;
            toggleTxt.alignment = TextAnchor.MiddleCenter;
            toggleTxt.color = new Color(1f, 0.9f, 0.45f);
            AssignFont(toggleTxt);

            // 2. Main Panel Window (Draggable)
            panelRoot = new GameObject("TimeTestingPanel");
            panelRoot.transform.SetParent(canvas.transform, false);
            var pRT = panelRoot.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 0.5f);
            pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.pivot     = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = new Vector2(260, 70); // Placed safely in upper right, away from all default HUD
            pRT.sizeDelta        = new Vector2(340, 290);
            var pImg = panelRoot.AddComponent<Image>();
            pImg.color = new Color(0.08f, 0.1f, 0.15f, 0.96f);
            pImg.raycastTarget = true;

            // Make entire panel draggable from background
            var panelDrag = panelRoot.AddComponent<UIDraggablePanel>();
            panelDrag.SetTarget(pRT);

            // Header bar (Primary Drag handle with distinctive title bar)
            var headGO = new GameObject("HeaderBar");
            headGO.transform.SetParent(panelRoot.transform, false);
            var hRT = headGO.AddComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, 0);
            hRT.sizeDelta = new Vector2(0, 32);
            var hImg = headGO.AddComponent<Image>();
            hImg.color = new Color(0.15f, 0.2f, 0.3f, 1f);
            hImg.raycastTarget = true;

            var headDrag = headGO.AddComponent<UIDraggablePanel>();
            headDrag.SetTarget(pRT);

            // Header title text
            var hTxtGO = new GameObject("TitleText");
            hTxtGO.transform.SetParent(headGO.transform, false);
            var hTxtRT = hTxtGO.AddComponent<RectTransform>();
            hTxtRT.anchorMin = Vector2.zero; hTxtRT.anchorMax = Vector2.one;
            hTxtRT.offsetMin = new Vector2(10, 0); hTxtRT.offsetMax = new Vector2(-36, 0);
            var hTxt = hTxtGO.AddComponent<Text>();
            hTxt.text = "⏱ TIME TESTER  ⋮⋮ <i>(Drag to move)</i>";
            hTxt.fontSize = 13;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleLeft;
            hTxt.color = new Color(1f, 0.85f, 0.4f);
            hTxt.raycastTarget = false;
            AssignFont(hTxt);

            // Close button inside HeaderBar
            var closeGO = new GameObject("CloseBtn");
            closeGO.transform.SetParent(headGO.transform, false);
            var cRT = closeGO.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(1, 0.5f); cRT.anchorMax = new Vector2(1, 0.5f);
            cRT.pivot = new Vector2(1, 0.5f);
            cRT.anchoredPosition = new Vector2(-5, 0);
            cRT.sizeDelta = new Vector2(22, 22);
            var cImg = closeGO.AddComponent<Image>();
            cImg.color = new Color(0.45f, 0.18f, 0.18f, 0.95f);
            var cBtn = closeGO.AddComponent<Button>();
            cBtn.onClick.AddListener(ToggleVisibility);
            var cTxtGO = new GameObject("X");
            cTxtGO.transform.SetParent(closeGO.transform, false);
            var cTxtRT = cTxtGO.AddComponent<RectTransform>();
            cTxtRT.anchorMin = Vector2.zero; cTxtRT.anchorMax = Vector2.one;
            cTxtRT.offsetMin = cTxtRT.offsetMax = Vector2.zero;
            var cTxt = cTxtGO.AddComponent<Text>();
            cTxt.text = "✕"; cTxt.fontSize = 12; cTxt.alignment = TextAnchor.MiddleCenter; cTxt.color = Color.white;
            cTxt.raycastTarget = false;
            AssignFont(cTxt);

            // Status display
            var statGO = new GameObject("StatusText");
            statGO.transform.SetParent(panelRoot.transform, false);
            var sRT = statGO.AddComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.anchoredPosition = new Vector2(0, -38);
            sRT.sizeDelta = new Vector2(-16, 22);
            statusText = statGO.AddComponent<Text>();
            statusText.supportRichText = true;
            statusText.fontSize = 12;
            statusText.color = Color.white;
            statusText.alignment = TextAnchor.MiddleLeft;
            AssignFont(statusText);

            // Quick Preset Buttons row
            // 6 AM (Day start) | 12 PM (Noon) | 6:55 PM (Locked test) | 7:00 PM (Unlocked test) | 10 PM
            CreateButton(panelRoot.transform, "6 AM", new Vector2(8, -66), new Vector2(58, 26), () => OnSetPreset(6f), new Color(0.2f, 0.45f, 0.25f));
            CreateButton(panelRoot.transform, "12 PM", new Vector2(70, -66), new Vector2(58, 26), () => OnSetPreset(12f), new Color(0.2f, 0.35f, 0.5f));
            CreateButton(panelRoot.transform, "6:55 PM", new Vector2(132, -66), new Vector2(64, 26), () => OnSetPreset(18.916f), new Color(0.6f, 0.25f, 0.2f));
            CreateButton(panelRoot.transform, "7:00 PM", new Vector2(200, -66), new Vector2(66, 26), () => OnSetPreset(19f), new Color(0.2f, 0.6f, 0.3f));
            CreateButton(panelRoot.transform, "10 PM", new Vector2(270, -66), new Vector2(58, 26), () => OnSetPreset(22f), new Color(0.3f, 0.2f, 0.4f));

            // Step buttons row: -1h | +1h | +30m | Pause | 5x
            CreateButton(panelRoot.transform, "-1 Hour", new Vector2(8, -98), new Vector2(68, 24), () => OnAddHours(-1f), new Color(0.25f, 0.28f, 0.35f));
            CreateButton(panelRoot.transform, "+1 Hour", new Vector2(80, -98), new Vector2(68, 24), () => OnAddHours(1f), new Color(0.25f, 0.28f, 0.35f));
            CreateButton(panelRoot.transform, "+30 Min", new Vector2(152, -98), new Vector2(68, 24), () => OnAddHours(0.5f), new Color(0.25f, 0.28f, 0.35f));
            CreateButton(panelRoot.transform, "Pause/Play", new Vector2(224, -98), new Vector2(80, 24), OnTogglePause, new Color(0.45f, 0.35f, 0.15f));

            // Slider label
            var slLblGO = new GameObject("SliderLabel");
            slLblGO.transform.SetParent(panelRoot.transform, false);
            var slLblRT = slLblGO.AddComponent<RectTransform>();
            slLblRT.anchorMin = new Vector2(0, 1); slLblRT.anchorMax = new Vector2(1, 1);
            slLblRT.pivot = new Vector2(0, 1);
            slLblRT.anchoredPosition = new Vector2(8, -128);
            slLblRT.sizeDelta = new Vector2(180, 20);
            var slLbl = slLblGO.AddComponent<Text>();
            slLbl.text = "<b>Time Slider (0-24h):</b>";
            slLbl.fontSize = 12; slLbl.color = new Color(0.9f, 0.9f, 0.9f);
            AssignFont(slLbl);

            sliderValueText = CreateText(panelRoot.transform, "6:00 AM", new Vector2(170, -128), new Vector2(160, 20), 12, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.4f));

            // Interactive Slider
            var sliderGO = new GameObject("TimeSlider");
            sliderGO.transform.SetParent(panelRoot.transform, false);
            var sldRT = sliderGO.AddComponent<RectTransform>();
            sldRT.anchorMin = new Vector2(0, 1); sldRT.anchorMax = new Vector2(1, 1);
            sldRT.pivot = new Vector2(0.5f, 1);
            sldRT.anchoredPosition = new Vector2(0, -148);
            sldRT.sizeDelta = new Vector2(-20, 20);

            // Slider background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.22f, 0.28f);

            // Slider fill area
            var faGO = new GameObject("Fill Area");
            faGO.transform.SetParent(sliderGO.transform, false);
            var faRT = faGO.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = new Vector2(5, 0); faRT.offsetMax = new Vector2(-5, 0);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(faGO.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.25f, 0.55f, 0.85f);

            // Slider handle area
            var haGO = new GameObject("Handle Slide Area");
            haGO.transform.SetParent(sliderGO.transform, false);
            var haRT = haGO.AddComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);

            var hdlGO = new GameObject("Handle");
            hdlGO.transform.SetParent(haGO.transform, false);
            var hdlRT = hdlGO.AddComponent<RectTransform>();
            hdlRT.sizeDelta = new Vector2(20, 0);
            var hdlImg = hdlGO.AddComponent<Image>();
            hdlImg.color = new Color(0.95f, 0.85f, 0.4f);

            timeSlider = sliderGO.AddComponent<Slider>();
            timeSlider.fillRect = fillRT;
            timeSlider.handleRect = hdlRT;
            timeSlider.targetGraphic = hdlImg;
            timeSlider.direction = Slider.Direction.LeftToRight;
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 24f;
            timeSlider.value = TimeManager.Instance != null ? TimeManager.Instance.CurrentGameHourFloat : 6f;
            timeSlider.onValueChanged.AddListener(OnSliderChanged);

            // Custom Hour Input Field Placeholder row
            CreateText(panelRoot.transform, "<b>Set Custom Hour:</b>", new Vector2(8, -176), new Vector2(120, 24), 12, TextAnchor.MiddleLeft, Color.white);

            var inputGO = new GameObject("HourInputField");
            inputGO.transform.SetParent(panelRoot.transform, false);
            var inRT = inputGO.AddComponent<RectTransform>();
            inRT.anchorMin = new Vector2(0, 1); inRT.anchorMax = new Vector2(0, 1);
            inRT.pivot = new Vector2(0, 1);
            inRT.anchoredPosition = new Vector2(132, -174);
            inRT.sizeDelta = new Vector2(110, 26);
            var inImg = inputGO.AddComponent<Image>();
            inImg.color = new Color(0.15f, 0.17f, 0.22f);

            // Input text
            var inTextGO = new GameObject("Text");
            inTextGO.transform.SetParent(inputGO.transform, false);
            var inTextRT = inTextGO.AddComponent<RectTransform>();
            inTextRT.anchorMin = Vector2.zero; inTextRT.anchorMax = Vector2.one;
            inTextRT.offsetMin = new Vector2(6, 0); inTextRT.offsetMax = new Vector2(-6, 0);
            var inText = inTextGO.AddComponent<Text>();
            inText.fontSize = 12; inText.color = Color.white; inText.alignment = TextAnchor.MiddleLeft;
            AssignFont(inText);

            // Placeholder text
            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phRT = phGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(6, 0); phRT.offsetMax = new Vector2(-6, 0);
            var phText = phGO.AddComponent<Text>();
            phText.text = "e.g. 19 or 19.5";
            phText.fontStyle = FontStyle.Italic;
            phText.fontSize = 11;
            phText.color = new Color(0.6f, 0.6f, 0.65f);
            phText.alignment = TextAnchor.MiddleLeft;
            AssignFont(phText);

            hourInputField = inputGO.AddComponent<InputField>();
            hourInputField.textComponent = inText;
            hourInputField.placeholder = phText;
            hourInputField.characterLimit = 6;
            hourInputField.contentType = InputField.ContentType.DecimalNumber;
            hourInputField.onEndEdit.AddListener((_) => OnApplyInputField());

            CreateButton(panelRoot.transform, "Set", new Vector2(250, -174), new Vector2(58, 26), OnApplyInputField, new Color(0.25f, 0.5f, 0.8f));

            // Speed buttons row: 1x | 5x | 10x | 30x
            CreateText(panelRoot.transform, "<b>Sim Speed:</b>", new Vector2(8, -208), new Vector2(80, 22), 12, TextAnchor.MiddleLeft, Color.white);
            CreateButton(panelRoot.transform, "1x (Normal)", new Vector2(86, -208), new Vector2(76, 22), () => OnSetSpeed(1f), new Color(0.2f, 0.35f, 0.45f));
            CreateButton(panelRoot.transform, "5x", new Vector2(166, -208), new Vector2(40, 22), () => OnSetSpeed(5f), new Color(0.2f, 0.35f, 0.45f));
            CreateButton(panelRoot.transform, "10x", new Vector2(210, -208), new Vector2(44, 22), () => OnSetSpeed(10f), new Color(0.2f, 0.35f, 0.45f));
            CreateButton(panelRoot.transform, "30x", new Vector2(258, -208), new Vector2(46, 22), () => OnSetSpeed(30f), new Color(0.35f, 0.2f, 0.45f));

            // Hotkey Help footer
            var helpGO = new GameObject("HelpFooter");
            helpGO.transform.SetParent(panelRoot.transform, false);
            var helpRT = helpGO.AddComponent<RectTransform>();
            helpRT.anchorMin = new Vector2(0, 0); helpRT.anchorMax = new Vector2(1, 0);
            helpRT.pivot = new Vector2(0.5f, 0);
            helpRT.anchoredPosition = new Vector2(0, 8);
            helpRT.sizeDelta = new Vector2(-16, 42);
            var helpTxt = helpGO.AddComponent<Text>();
            helpTxt.text = "<color=#AAAAAA><b>Hotkeys:</b> F1=6AM, F2=12PM, F3=6:55PM, F4=7PM(Unlock), [ / ]=-/+1h, P=Pause, ` =Toggle Panel</color>";
            helpTxt.fontSize = 11;
            helpTxt.alignment = TextAnchor.UpperLeft;
            helpTxt.supportRichText = true;
            AssignFont(helpTxt);
        }

        private Text CreateText(Transform parent, string text, Vector2 anchoredPos, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            t.supportRichText = true;
            AssignFont(t);
            return t;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var btnGO = new GameObject(label + "Btn");
            btnGO.transform.SetParent(parent, false);
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = btnGO.AddComponent<Image>();
            img.color = color;
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 11;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            AssignFont(txt);
            return btn;
        }

        private void AssignFont(Text t)
        {
            if (t == null) return;
            var existing = FindObjectOfType<Text>();
            if (existing != null && existing.font != null)
                t.font = existing.font;
            else
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
