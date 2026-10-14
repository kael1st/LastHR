#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

namespace FarmingSystem.Editor
{
    public static class FarmingSceneSetup
    {
        private const string SPRITES_PATH = "Assets/Sprites/Farming";
        private const string ITEMS_PATH = "Assets/ScriptableObjects/Items";

        [MenuItem("Farming/Setup Complete Farming Scene")]
        public static void SetupScene()
        {
            // 1. Ensure assets exist first
            FarmingAssetGenerator.GenerateAll();

            // 2. Load Item and Sprite assets
            ItemData hoe = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/Hoe.asset");
            ItemData waterCan = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/WateringCan.asset");
            ItemData seeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/TurnipSeeds.asset");
            ItemData harvest = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/Turnip.asset");
            CropData crop = AssetDatabase.LoadAssetAtPath<CropData>($"{ITEMS_PATH}/TurnipCrop.asset");

            Sprite untilled = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/soil_untilled.png");
            Sprite tilledDry = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/soil_tilled_dry.png");
            Sprite tilledWatered = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/soil_tilled_watered.png");
            Sprite reticle = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/target_reticle.png");
            Sprite slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/slot_frame.png");
            Sprite slotSelected = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/slot_selected.png");

            // 3. Configure Player
            SetupPlayer(hoe, waterCan, seeds);

            // 4. Configure GardenManager
            SetupGardenManager();

            // 5. Build Garden Patch
            SetupGardenPatch(untilled, tilledDry, tilledWatered, reticle, crop);

            // 6. Build UI Canvas and Hotbar
            SetupUI(slotFrame, slotSelected);

            // Mark scene dirty and save
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Farming] Complete farming scene and UI configured successfully!</color>");
        }

        private static void SetupPlayer(ItemData hoe, ItemData waterCan, ItemData seeds)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("No GameObject with tag 'Player' found in scene!");
                return;
            }

            // Ensure PlayerInventory
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) inventory = player.AddComponent<PlayerInventory>();

            // Assign starting loadout via SerializedObject for persistent undo/serialization
            SerializedObject invSo = new SerializedObject(inventory);
            invSo.FindProperty("startingHoe").objectReferenceValue = hoe;
            invSo.FindProperty("startingWateringCan").objectReferenceValue = waterCan;
            invSo.FindProperty("startingSeedPack").objectReferenceValue = seeds;
            invSo.FindProperty("startingSeedCount").intValue = 15;
            invSo.FindProperty("hotbarSize").intValue = 8;
            invSo.ApplyModifiedProperties();

            // Ensure FarmingController
            FarmingController farmingCtrl = player.GetComponent<FarmingController>();
            if (farmingCtrl == null) farmingCtrl = player.AddComponent<FarmingController>();

            SerializedObject farmSo = new SerializedObject(farmingCtrl);
            farmSo.FindProperty("facingTargetDistance").floatValue = 1.2f;
            farmSo.FindProperty("checkRadius").floatValue = 0.55f;
            farmSo.FindProperty("maxMouseInteractDistance").floatValue = 3.2f;
            farmSo.ApplyModifiedProperties();

            // Reset player position slightly up so the garden plot is visible below them
            player.transform.position = new Vector3(0f, 0.5f, 0f);
        }

        private static void SetupGardenManager()
        {
            GameObject managerObj = GameObject.Find("GardenManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("GardenManager");
            }

            if (managerObj.GetComponent<GardenManager>() == null)
            {
                managerObj.AddComponent<GardenManager>();
            }
        }

        private static void SetupGardenPatch(Sprite untilled, Sprite tilledDry, Sprite tilledWatered, Sprite reticle, CropData crop)
        {
            GameObject patch = GameObject.Find("GardenPatch");
            if (patch != null)
            {
                Object.DestroyImmediate(patch);
            }

            patch = new GameObject("GardenPatch");
            // Position garden patch right in front of the player
            patch.transform.position = new Vector3(0f, -1.8f, 0f);

            int cols = 4;
            int rows = 3;
            float spacingX = 1.15f;
            float spacingY = 1.15f;

            float startX = -(cols - 1) * spacingX * 0.5f;
            float startY = (rows - 1) * spacingY * 0.5f;

            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector3 localPos = new Vector3(startX + c * spacingX, startY - r * spacingY, 0f);
                    GameObject plotObj = new GameObject($"Plot_{c}_{r}");
                    plotObj.transform.SetParent(patch.transform, false);
                    plotObj.transform.localPosition = localPos;

                    var collider = plotObj.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(1f, 1f);
                    collider.isTrigger = true;

                    var plot = plotObj.AddComponent<GardenPlot>();
                    plot.ConfigureSprites(untilled, tilledDry, tilledWatered);
                    plot.ConfigureHighlightSprite(reticle);

                    // Pre-configure a few varied plots so the player immediately sees the farming progression:
                    // Row 0: Until ground
                    // Row 1 (c=0,1): Tilled dry
                    // Row 1 (c=2,3): Tilled watered with sprouting turnip
                    // Row 2 (c=0,1): Watered growing turnip
                    // Row 2 (c=2,3): Ripe turnip ready to harvest!
                    SerializedObject plotSo = new SerializedObject(plot);

                    if (r == 1 && c <= 1)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledDry;
                    }
                    else if (r == 1 && c > 1)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledWatered;
                        plotSo.FindProperty("currentCrop").objectReferenceValue = crop;
                        plotSo.FindProperty("currentGrowthStage").intValue = 1;
                    }
                    else if (r == 2 && c <= 1)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledWatered;
                        plotSo.FindProperty("currentCrop").objectReferenceValue = crop;
                        plotSo.FindProperty("currentGrowthStage").intValue = 2;
                    }
                    else if (r == 2 && c > 1)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledWatered;
                        plotSo.FindProperty("currentCrop").objectReferenceValue = crop;
                        plotSo.FindProperty("currentGrowthStage").intValue = 3;
                        plotSo.FindProperty("isRipe").boolValue = true;
                    }

                    plotSo.ApplyModifiedProperties();
                    plot.UpdateVisuals();
                    index++;
                }
            }
        }

        private static void SetupUI(Sprite slotFrame, Sprite slotSelected)
        {
            // 1. Ensure EventSystem
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            // 2. Ensure Canvas
            GameObject canvasObj = GameObject.Find("FarmingCanvas");
            if (canvasObj != null)
            {
                Object.DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("FarmingCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            InventoryUI inventoryUI = canvasObj.AddComponent<InventoryUI>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 3. Hotbar Container
            GameObject hotbarObj = new GameObject("HotbarContainer");
            hotbarObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hotbarRect = hotbarObj.AddComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 25f);
            hotbarRect.sizeDelta = new Vector2(580f, 75f);

            // Hotbar background backing bar
            Image hotbarBg = hotbarObj.AddComponent<Image>();
            hotbarBg.color = new Color(0.08f, 0.08f, 0.10f, 0.75f);

            HorizontalLayoutGroup layout = hotbarObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 4. Create 8 Hotbar Slots
            var slotUIList = new System.Collections.Generic.List<InventorySlotUI>();
            for (int i = 0; i < 8; i++)
            {
                GameObject slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(hotbarObj.transform, false);
                RectTransform slotRect = slotObj.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(60f, 60f);

                // Slot frame
                Image frameImg = slotObj.AddComponent<Image>();
                frameImg.sprite = slotFrame;
                frameImg.type = Image.Type.Simple;

                Button btn = slotObj.AddComponent<Button>();

                // Icon object
                GameObject iconObj = new GameObject("ItemIcon");
                iconObj.transform.SetParent(slotObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.12f, 0.12f);
                iconRect.anchorMax = new Vector2(0.88f, 0.88f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.enabled = false;

                // Selection highlight
                GameObject hlObj = new GameObject("HighlightFrame");
                hlObj.transform.SetParent(slotObj.transform, false);
                RectTransform hlRect = hlObj.AddComponent<RectTransform>();
                hlRect.anchorMin = Vector2.zero;
                hlRect.anchorMax = Vector2.one;
                hlRect.sizeDelta = new Vector2(6f, 6f);
                Image hlImg = hlObj.AddComponent<Image>();
                hlImg.sprite = slotSelected;
                hlImg.enabled = false;

                // Hotkey badge (Top Left)
                GameObject keyObj = new GameObject("HotkeyText");
                keyObj.transform.SetParent(slotObj.transform, false);
                RectTransform keyRect = keyObj.AddComponent<RectTransform>();
                keyRect.anchorMin = new Vector2(0f, 1f);
                keyRect.anchorMax = new Vector2(0f, 1f);
                keyRect.pivot = new Vector2(0f, 1f);
                keyRect.anchoredPosition = new Vector2(4f, -3f);
                keyRect.sizeDelta = new Vector2(20f, 16f);
                Text keyText = keyObj.AddComponent<Text>();
                keyText.font = defaultFont;
                keyText.fontSize = 12;
                keyText.fontStyle = FontStyle.Bold;
                keyText.alignment = TextAnchor.UpperLeft;
                keyText.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
                var keyShadow = keyObj.AddComponent<Shadow>();
                keyShadow.effectColor = Color.black;
                keyShadow.effectDistance = new Vector2(1f, -1f);

                // Quantity badge (Bottom Right)
                GameObject qtyObj = new GameObject("QuantityText");
                qtyObj.transform.SetParent(slotObj.transform, false);
                RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
                qtyRect.anchorMin = new Vector2(1f, 0f);
                qtyRect.anchorMax = new Vector2(1f, 0f);
                qtyRect.pivot = new Vector2(1f, 0f);
                qtyRect.anchoredPosition = new Vector2(-4f, 3f);
                qtyRect.sizeDelta = new Vector2(30f, 18f);
                Text qtyText = qtyObj.AddComponent<Text>();
                qtyText.font = defaultFont;
                qtyText.fontSize = 14;
                qtyText.fontStyle = FontStyle.Bold;
                qtyText.alignment = TextAnchor.LowerRight;
                qtyText.color = Color.white;
                var qtyOutline = qtyObj.AddComponent<Outline>();
                qtyOutline.effectColor = Color.black;
                qtyOutline.effectDistance = new Vector2(1f, -1f);

                // InventorySlotUI component
                InventorySlotUI slotComp = slotObj.AddComponent<InventorySlotUI>();
                SerializedObject slotSo = new SerializedObject(slotComp);
                slotSo.FindProperty("slotIndex").intValue = i;
                slotSo.FindProperty("frameImage").objectReferenceValue = frameImg;
                slotSo.FindProperty("iconImage").objectReferenceValue = iconImg;
                slotSo.FindProperty("highlightImage").objectReferenceValue = hlImg;
                slotSo.FindProperty("hotkeyText").objectReferenceValue = keyText;
                slotSo.FindProperty("quantityText").objectReferenceValue = qtyText;
                slotSo.FindProperty("button").objectReferenceValue = btn;
                slotSo.ApplyModifiedProperties();

                slotUIList.Add(slotComp);
            }

            // 5. Selected Item Banner (Above hotbar)
            GameObject bannerObj = new GameObject("SelectedItemBanner");
            bannerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0f);
            bannerRect.anchorMax = new Vector2(0.5f, 0f);
            bannerRect.pivot = new Vector2(0.5f, 0f);
            bannerRect.anchoredPosition = new Vector2(0f, 108f);
            bannerRect.sizeDelta = new Vector2(460f, 54f);

            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

            // Item Name
            GameObject nameObj = new GameObject("ItemNameText");
            nameObj.transform.SetParent(bannerObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(10f, 0f);
            nameRect.offsetMax = new Vector2(-10f, -4f);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = defaultFont;
            nameText.fontSize = 17;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = new Color(1f, 0.88f, 0.35f);

            // Item Description
            GameObject descObj = new GameObject("ItemDescText");
            descObj.transform.SetParent(bannerObj.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0.5f);
            descRect.offsetMin = new Vector2(10f, 4f);
            descRect.offsetMax = new Vector2(-10f, 0f);
            Text descText = descObj.AddComponent<Text>();
            descText.font = defaultFont;
            descText.fontSize = 12;
            descText.alignment = TextAnchor.MiddleCenter;
            descText.color = new Color(0.9f, 0.9f, 0.92f);

            // 6. Notification Banner / Toast
            GameObject notifObj = new GameObject("NotificationBanner");
            notifObj.transform.SetParent(canvasObj.transform, false);
            RectTransform notifRect = notifObj.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 0.88f);
            notifRect.anchorMax = new Vector2(0.5f, 0.88f);
            notifRect.pivot = new Vector2(0.5f, 0.5f);
            notifRect.anchoredPosition = Vector2.zero;
            notifRect.sizeDelta = new Vector2(480f, 44f);

            Image notifBg = notifObj.AddComponent<Image>();
            notifBg.color = new Color(0.08f, 0.08f, 0.10f, 0.90f);

            GameObject notifTextObj = new GameObject("Text");
            notifTextObj.transform.SetParent(notifObj.transform, false);
            RectTransform notifTextRect = notifTextObj.AddComponent<RectTransform>();
            notifTextRect.anchorMin = Vector2.zero;
            notifTextRect.anchorMax = Vector2.one;
            notifTextRect.offsetMin = new Vector2(10f, 4f);
            notifTextRect.offsetMax = new Vector2(-10f, -4f);
            Text notifText = notifTextObj.AddComponent<Text>();
            notifText.font = defaultFont;
            notifText.fontSize = 16;
            notifText.fontStyle = FontStyle.Bold;
            notifText.alignment = TextAnchor.MiddleCenter;
            notifText.color = new Color(1f, 0.95f, 0.5f);

            notifObj.SetActive(false); // Initially hidden

            // 7. Day & Calendar HUD (Top Right)
            GameObject dayHudObj = new GameObject("DayHUD");
            dayHudObj.transform.SetParent(canvasObj.transform, false);
            RectTransform dayHudRect = dayHudObj.AddComponent<RectTransform>();
            dayHudRect.anchorMin = new Vector2(1f, 1f);
            dayHudRect.anchorMax = new Vector2(1f, 1f);
            dayHudRect.pivot = new Vector2(1f, 1f);
            dayHudRect.anchoredPosition = new Vector2(-25f, -25f);
            dayHudRect.sizeDelta = new Vector2(220f, 85f);

            Image dayHudBg = dayHudObj.AddComponent<Image>();
            dayHudBg.color = new Color(0.10f, 0.10f, 0.12f, 0.85f);

            // Day Text
            GameObject dayTextObj = new GameObject("DayText");
            dayTextObj.transform.SetParent(dayHudObj.transform, false);
            RectTransform dayTextRect = dayTextObj.AddComponent<RectTransform>();
            dayTextRect.anchorMin = new Vector2(0f, 0.45f);
            dayTextRect.anchorMax = new Vector2(1f, 1f);
            dayTextRect.offsetMin = new Vector2(8f, 0f);
            dayTextRect.offsetMax = new Vector2(-8f, -4f);
            Text dayText = dayTextObj.AddComponent<Text>();
            dayText.font = defaultFont;
            dayText.fontSize = 17;
            dayText.fontStyle = FontStyle.Bold;
            dayText.alignment = TextAnchor.MiddleCenter;
            dayText.color = new Color(1f, 0.92f, 0.5f);

            // Advance Day Button
            GameObject advBtnObj = new GameObject("AdvanceDayButton");
            advBtnObj.transform.SetParent(dayHudObj.transform, false);
            RectTransform advBtnRect = advBtnObj.AddComponent<RectTransform>();
            advBtnRect.anchorMin = new Vector2(0.08f, 0.08f);
            advBtnRect.anchorMax = new Vector2(0.92f, 0.45f);
            advBtnRect.offsetMin = Vector2.zero;
            advBtnRect.offsetMax = Vector2.zero;

            Image advBtnImg = advBtnObj.AddComponent<Image>();
            advBtnImg.color = new Color(0.25f, 0.52f, 0.35f);
            Button advBtn = advBtnObj.AddComponent<Button>();

            GameObject advBtnTextObj = new GameObject("Text");
            advBtnTextObj.transform.SetParent(advBtnObj.transform, false);
            RectTransform advBtnTextRect = advBtnTextObj.AddComponent<RectTransform>();
            advBtnTextRect.anchorMin = Vector2.zero;
            advBtnTextRect.anchorMax = Vector2.one;
            advBtnTextRect.offsetMin = Vector2.zero;
            advBtnTextRect.offsetMax = Vector2.zero;
            Text advBtnText = advBtnTextObj.AddComponent<Text>();
            advBtnText.font = defaultFont;
            advBtnText.fontSize = 13;
            advBtnText.fontStyle = FontStyle.Bold;
            advBtnText.alignment = TextAnchor.MiddleCenter;
            advBtnText.color = Color.white;
            advBtnText.text = "Next Day [T]";

            // 8. Controls Guide Card (Top Left)
            GameObject guideObj = new GameObject("ControlsGuide");
            guideObj.transform.SetParent(canvasObj.transform, false);
            RectTransform guideRect = guideObj.AddComponent<RectTransform>();
            guideRect.anchorMin = new Vector2(0f, 1f);
            guideRect.anchorMax = new Vector2(0f, 1f);
            guideRect.pivot = new Vector2(0f, 1f);
            guideRect.anchoredPosition = new Vector2(25f, -25f);
            guideRect.sizeDelta = new Vector2(260f, 140f);

            Image guideBg = guideObj.AddComponent<Image>();
            guideBg.color = new Color(0.08f, 0.08f, 0.10f, 0.85f);

            GameObject guideTextObj = new GameObject("Text");
            guideTextObj.transform.SetParent(guideObj.transform, false);
            RectTransform guideTextRect = guideTextObj.AddComponent<RectTransform>();
            guideTextRect.anchorMin = Vector2.zero;
            guideTextRect.anchorMax = Vector2.one;
            guideTextRect.offsetMin = new Vector2(12f, 8f);
            guideTextRect.offsetMax = new Vector2(-12f, -8f);
            Text guideText = guideTextObj.AddComponent<Text>();
            guideText.font = defaultFont;
            guideText.fontSize = 12;
            guideText.lineSpacing = 1.15f;
            guideText.color = new Color(0.92f, 0.92f, 0.95f);
            guideText.text = "<b>FARMING CONTROLS:</b>\n" +
                             "• <b>WASD / Arrows</b>: Move\n" +
                             "• <b>1 - 8 / Scroll</b>: Select Item\n" +
                             "• <b>Space / E / Click</b>: Use Tool / Harvest\n" +
                             "• <b>T</b>: Advance to Next Day\n" +
                             "<i>[1] Hoe  [2] Water Can  [3] Seeds</i>";

            // Wire everything to InventoryUI via SerializedObject
            SerializedObject uiSo = new SerializedObject(inventoryUI);
            uiSo.FindProperty("hotbarContainer").objectReferenceValue = hotbarObj.transform;
            uiSo.FindProperty("selectedItemNameText").objectReferenceValue = nameText;
            uiSo.FindProperty("selectedItemDescText").objectReferenceValue = descText;
            uiSo.FindProperty("notificationPanel").objectReferenceValue = notifObj;
            uiSo.FindProperty("notificationText").objectReferenceValue = notifText;
            uiSo.FindProperty("dayText").objectReferenceValue = dayText;
            uiSo.FindProperty("advanceDayButton").objectReferenceValue = advBtn;

            SerializedProperty slotProp = uiSo.FindProperty("slotUIs");
            slotProp.ClearArray();
            for (int i = 0; i < slotUIList.Count; i++)
            {
                slotProp.InsertArrayElementAtIndex(i);
                slotProp.GetArrayElementAtIndex(i).objectReferenceValue = slotUIList[i];
            }
            uiSo.ApplyModifiedProperties();
        }
    }
}
#endif
