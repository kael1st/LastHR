#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;

namespace FarmingSystem.Editor
{
    public static class FarmingSceneSetup
    {
        private const string SPRITES_PATH = "Assets/Sprites/Farming";
        private const string ITEMS_PATH = "Assets/ScriptableObjects/Items";

        [MenuItem("Farming/Setup Complete Farming Scene")]
        public static void SetupScene()
        {
            // 1. Ensure all sprites and ScriptableObjects across all 4 rarity tiers exist
            FarmingAssetGenerator.GenerateAll();

            // 2. Load Item & Sprite assets
            ItemData hoe = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/Hoe.asset");
            ItemData waterCan = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/WateringCan.asset");
            ItemData shovel = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/Shovel.asset");
            ItemData pocketWatch = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/PocketWatch.asset");
            if (pocketWatch == null)
                pocketWatch = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/PocketWatch.asset");

            // Crop & Seed ScriptableObjects
            ItemData turnipSeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/TurnipSeeds.asset");
            ItemData carrotSeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/CarrotSeeds.asset");
            ItemData strawberrySeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/StrawberrySeeds.asset");
            ItemData blueberrySeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/BlueberrySeeds.asset");
            ItemData goldenMelonSeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/GoldenMelonSeeds.asset");
            ItemData starfruitSeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/StarfruitSeeds.asset");
            ItemData heavenlySeeds = AssetDatabase.LoadAssetAtPath<ItemData>($"{ITEMS_PATH}/HeavenlySeeds.asset");

            CropData turnipCrop = AssetDatabase.LoadAssetAtPath<CropData>($"{ITEMS_PATH}/TurnipCrop.asset");
            CropData strawberryCrop = AssetDatabase.LoadAssetAtPath<CropData>($"{ITEMS_PATH}/StrawberryCrop.asset");
            CropData starfruitCrop = AssetDatabase.LoadAssetAtPath<CropData>($"{ITEMS_PATH}/StarfruitCrop.asset");
            CropData heavenlyCrop = AssetDatabase.LoadAssetAtPath<CropData>($"{ITEMS_PATH}/HeavenlyFruitCrop.asset");

            Sprite untilled = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/soil_untilled.png");
            Sprite tilledDry = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/soil_tilled_dry.png");
            Sprite tilledWatered = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/soil_tilled_watered.png");
            Sprite reticle = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/target_reticle.png");
            Sprite slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/slot_frame.png");
            Sprite slotSelected = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/slot_selected.png");
            Sprite invBagIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/inventory_bag_icon.png");
            Sprite coinIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/coin_icon.png");
            Sprite vendorStall = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/vendor_stall.png");

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 3. Centralized Currency System
            SetupCurrencyManager();

            // 4. Interactive Merchant NPC / Stall in the world
            SetupStreetVendor(vendorStall, defaultFont);

            // 5. Merchant Shop Manager (2-minute rotating catalog)
            SetupMerchantShopManager();

            // 6. Configure Player with tools and comprehensive starting seeds across all 4 tiers
            SetupPlayer(hoe, waterCan, shovel, turnipSeeds, pocketWatch, new[] {
                carrotSeeds,
                strawberrySeeds,
                blueberrySeeds,
                goldenMelonSeeds,
                starfruitSeeds,
                heavenlySeeds
            });

            // 6. Configure GardenManager
            SetupGardenManager();

            // 7. Build Garden Patch showcasing various crops
            SetupGardenPatch(untilled, tilledDry, tilledWatered, reticle, turnipCrop, strawberryCrop, starfruitCrop, heavenlyCrop);

            // 8. Build Revamped UI Canvas (Hotbar, Inventory, Coin HUD, Vendor Selling Interface)
            SetupUI(slotFrame, slotSelected, invBagIcon, coinIcon, defaultFont);

            // Mark scene dirty and save
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Farming] Revamped multi-tier farming scene, inventory UI, currency system, and street vendor configured successfully!</color>");
        }

        private static void SetupPlayer(ItemData hoe, ItemData waterCan, ItemData shovel, ItemData turnipSeeds, ItemData pocketWatch, ItemData[] extraSeeds)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("No GameObject with tag 'Player' found in scene!");
                return;
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) inventory = player.AddComponent<PlayerInventory>();

            SerializedObject invSo = new SerializedObject(inventory);
            invSo.FindProperty("hotbarSize").intValue = 9;
            invSo.FindProperty("mainInventorySize").intValue = 27;
            invSo.FindProperty("startingHoe").objectReferenceValue = hoe;
            invSo.FindProperty("startingWateringCan").objectReferenceValue = waterCan;
            invSo.FindProperty("startingSeedPack").objectReferenceValue = turnipSeeds;
            invSo.FindProperty("startingSeedCount").intValue = 10;
            invSo.FindProperty("startingPocketWatch").objectReferenceValue = pocketWatch;
            invSo.FindProperty("startingShovel").objectReferenceValue = shovel;

            SerializedProperty extraProp = invSo.FindProperty("extraStartingItems");
            extraProp.ClearArray();
            if (extraSeeds != null)
            {
                for (int i = 0; i < extraSeeds.Length; i++)
                {
                    extraProp.InsertArrayElementAtIndex(i);
                    extraProp.GetArrayElementAtIndex(i).objectReferenceValue = extraSeeds[i];
                }
            }
            invSo.ApplyModifiedProperties();

            // Re-initialize inventory slots
            inventory.InitializeSlots();

            // Ensure FarmingController
            FarmingController farmingCtrl = player.GetComponent<FarmingController>();
            if (farmingCtrl == null) farmingCtrl = player.AddComponent<FarmingController>();

            SerializedObject farmSo = new SerializedObject(farmingCtrl);
            farmSo.FindProperty("facingTargetDistance").floatValue = 1.2f;
            farmSo.FindProperty("checkRadius").floatValue = 0.55f;
            farmSo.FindProperty("maxMouseInteractDistance").floatValue = 3.2f;
            farmSo.FindProperty("interactKey").intValue = (int)KeyCode.E;
            farmSo.ApplyModifiedProperties();

            player.transform.position = new Vector3(0f, 0.5f, 0f);
        }

        private static void SetupCurrencyManager()
        {
            GameObject managerObj = GameObject.Find("CurrencyManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("CurrencyManager");
            }

            CurrencyManager cm = managerObj.GetComponent<CurrencyManager>();
            if (cm == null)
            {
                cm = managerObj.AddComponent<CurrencyManager>();
            }

            SerializedObject cmSo = new SerializedObject(cm);
            var startCoinsProp = cmSo.FindProperty("startingCoins");
            if (startCoinsProp != null) startCoinsProp.longValue = 100;
            cmSo.ApplyModifiedProperties();
        }

        private static void SetupStreetVendor(Sprite vendorStall, Font font)
        {
            GameObject vendorObj = GameObject.Find("TheMerchant");
            if (vendorObj == null) vendorObj = GameObject.Find("StreetVendor");
            if (vendorObj == null) vendorObj = new GameObject("TheMerchant");
            vendorObj.name = "TheMerchant";

            vendorObj.transform.position = new Vector3(3.6f, 0.2f, 0f);

            SpriteRenderer sr = vendorObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = vendorObj.AddComponent<SpriteRenderer>();
            sr.sprite = vendorStall;
            sr.sortingOrder = 2;

            BoxCollider2D col = vendorObj.GetComponent<BoxCollider2D>();
            if (col == null) col = vendorObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 1.5f);
            col.isTrigger = true;

            StreetVendor vendor = vendorObj.GetComponent<StreetVendor>();
            if (vendor == null) vendor = vendorObj.AddComponent<StreetVendor>();

            Transform existingPrompt = vendorObj.transform.Find("InteractionPrompt");
            GameObject promptObj;
            if (existingPrompt != null)
            {
                promptObj = existingPrompt.gameObject;
            }
            else
            {
                promptObj = new GameObject("InteractionPrompt");
                promptObj.transform.SetParent(vendorObj.transform, false);
                promptObj.transform.localPosition = new Vector3(0f, 1.15f, 0f);

                TextMesh tm = promptObj.AddComponent<TextMesh>();
                tm.text = "[E] The Merchant";
                tm.font = font;
                tm.fontSize = 24;
                tm.characterSize = 0.055f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(1.0f, 0.95f, 0.6f);

                MeshRenderer mr = promptObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sortingOrder = 20;
                }
            }

            TextMesh promptTm = promptObj.GetComponent<TextMesh>();
            if (promptTm != null) promptTm.text = "[E] The Merchant";
            promptObj.SetActive(false);

            SerializedObject vendorSo = new SerializedObject(vendor);
            vendorSo.FindProperty("vendorName").stringValue = "The Merchant";
            vendorSo.FindProperty("interactDistance").floatValue = 2.4f;
            vendorSo.FindProperty("stallRenderer").objectReferenceValue = sr;
            vendorSo.FindProperty("interactionPrompt").objectReferenceValue = promptObj;
            vendorSo.ApplyModifiedProperties();
        }

        private static void SetupMerchantShopManager()
        {
            GameObject shopMgrObj = GameObject.Find("MerchantShopManager");
            if (shopMgrObj == null)
            {
                shopMgrObj = new GameObject("MerchantShopManager");
            }

            MerchantShopManager msm = shopMgrObj.GetComponent<MerchantShopManager>();
            if (msm == null)
            {
                msm = shopMgrObj.AddComponent<MerchantShopManager>();
            }

            msm.LoadSeedDatabaseIfEmpty();
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

        private static void SetupGardenPatch(
            Sprite untilled, Sprite tilledDry, Sprite tilledWatered, Sprite reticle,
            CropData turnip, CropData strawberry, CropData starfruit, CropData heavenly)
        {
            GameObject patch = GameObject.Find("GardenPatch");
            if (patch != null) Object.DestroyImmediate(patch);

            patch = new GameObject("GardenPatch");
            patch.transform.position = new Vector3(0f, -1.8f, 0f);

            int cols = 4;
            int rows = 3;
            float spacingX = 1.15f;
            float spacingY = 1.15f;

            float startX = -(cols - 1) * spacingX * 0.5f;
            float startY = (rows - 1) * spacingY * 0.5f;

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

                    SerializedObject plotSo = new SerializedObject(plot);

                    // Row 0: Untilled ground (c=0,1), Tilled dry (c=2,3)
                    if (r == 0 && c >= 2)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledDry;
                    }
                    // Row 1: Watered growing Turnips
                    else if (r == 1)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledWatered;
                        plotSo.FindProperty("currentCrop").objectReferenceValue = turnip;
                        plotSo.FindProperty("currentGrowthStage").intValue = c + 1 >= turnip.MaxStage ? turnip.MaxStage - 1 : c + 1;
                    }
                    // Row 2: Ripe showcase crops across all tiers!
                    else if (r == 2)
                    {
                        plotSo.FindProperty("soilState").enumValueIndex = (int)PlotSoilState.TilledWatered;
                        CropData targetCrop = turnip;
                        if (c == 1 && strawberry != null) targetCrop = strawberry;
                        else if (c == 2 && starfruit != null) targetCrop = starfruit;
                        else if (c == 3 && heavenly != null) targetCrop = heavenly;

                        plotSo.FindProperty("currentCrop").objectReferenceValue = targetCrop;
                        plotSo.FindProperty("currentGrowthStage").intValue = targetCrop.MaxStage;
                        plotSo.FindProperty("isRipe").boolValue = true;
                    }

                    plotSo.ApplyModifiedProperties();
                    plot.UpdateVisuals();
                }
            }
        }

        private static void SetupUI(Sprite slotFrame, Sprite slotSelected, Sprite invBagIcon, Sprite coinIcon, Font defaultFont)
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
            if (canvasObj == null)
            {
                canvasObj = new GameObject("FarmingCanvas");
                canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            InventoryUI inventoryUI = canvasObj.GetComponent<InventoryUI>();
            if (inventoryUI == null) inventoryUI = canvasObj.AddComponent<InventoryUI>();

            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // Clean up old UI elements that we are revamping
            string[] toRemove = { "HotbarContainer", "SelectedItemBanner", "ControlsGuide", "MainInventoryPanel", "InventoryToggleButton", "DragGhost", "ItemTooltip", "CoinHUD", "VendorModalPanel" };
            foreach (var name in toRemove)
            {
                Transform old = canvasObj.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // ─────────────────────────────────────────────────────────────────
            // 3. Hotbar HUD Container (9 Slots)
            // ─────────────────────────────────────────────────────────────────
            GameObject hotbarObj = new GameObject("HotbarContainer");
            hotbarObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hotbarRect = hotbarObj.AddComponent<RectTransform>();
            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(0f, 20f);
            hotbarRect.sizeDelta = new Vector2(620f, 75f);

            Image hotbarBg = hotbarObj.AddComponent<Image>();
            hotbarBg.color = new Color(0.08f, 0.08f, 0.10f, 0.85f);

            HorizontalLayoutGroup hotbarLayout = hotbarObj.AddComponent<HorizontalLayoutGroup>();
            hotbarLayout.spacing = 8f;
            hotbarLayout.padding = new RectOffset(10, 10, 6, 6);
            hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
            hotbarLayout.childControlWidth = false;
            hotbarLayout.childControlHeight = false;

            var hotbarSlotUIList = new List<InventorySlotUI>();
            for (int i = 0; i < 9; i++)
            {
                var slotComp = CreateSlotUI(hotbarObj.transform, $"HotbarSlot_{i}", i, true, 58f, slotFrame, slotSelected, defaultFont);
                hotbarSlotUIList.Add(slotComp);
            }

            // ─────────────────────────────────────────────────────────────────
            // 4. Dedicated On-Screen Inventory Button (HUD)
            // ─────────────────────────────────────────────────────────────────
            GameObject invBtnObj = new GameObject("InventoryToggleButton");
            invBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform invBtnRect = invBtnObj.AddComponent<RectTransform>();
            invBtnRect.anchorMin = new Vector2(0.5f, 0f);
            invBtnRect.anchorMax = new Vector2(0.5f, 0f);
            invBtnRect.pivot = new Vector2(0f, 0f);
            invBtnRect.anchoredPosition = new Vector2(325f, 20f);
            invBtnRect.sizeDelta = new Vector2(75f, 75f);

            Image invBtnBg = invBtnObj.AddComponent<Image>();
            invBtnBg.color = new Color(0.12f, 0.12f, 0.15f, 0.90f);
            Button invToggleBtn = invBtnObj.AddComponent<Button>();

            // Bag Icon inside button
            GameObject bagIconObj = new GameObject("BagIcon");
            bagIconObj.transform.SetParent(invBtnObj.transform, false);
            RectTransform bagIconRect = bagIconObj.AddComponent<RectTransform>();
            bagIconRect.anchorMin = new Vector2(0.15f, 0.28f);
            bagIconRect.anchorMax = new Vector2(0.85f, 0.92f);
            bagIconRect.offsetMin = Vector2.zero;
            bagIconRect.offsetMax = Vector2.zero;
            Image bagIconImg = bagIconObj.AddComponent<Image>();
            bagIconImg.sprite = invBagIcon;
            bagIconImg.preserveAspect = true;

            // Shortcut label inside button: "INV [E]"
            GameObject invLabelObj = new GameObject("Label");
            invLabelObj.transform.SetParent(invBtnObj.transform, false);
            RectTransform invLabelRect = invLabelObj.AddComponent<RectTransform>();
            invLabelRect.anchorMin = new Vector2(0f, 0f);
            invLabelRect.anchorMax = new Vector2(1f, 0.32f);
            invLabelRect.offsetMin = Vector2.zero;
            invLabelRect.offsetMax = Vector2.zero;
            Text invLabelText = invLabelObj.AddComponent<Text>();
            invLabelText.font = defaultFont;
            invLabelText.fontSize = 10;
            invLabelText.fontStyle = FontStyle.Bold;
            invLabelText.alignment = TextAnchor.MiddleCenter;
            invLabelText.color = new Color(1f, 0.88f, 0.35f);
            invLabelText.text = "INVENTORY";

            // ─────────────────────────────────────────────────────────────────
            // 5. Selected Item Banner (Above Hotbar)
            // ─────────────────────────────────────────────────────────────────
            GameObject bannerObj = new GameObject("SelectedItemBanner");
            bannerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0f);
            bannerRect.anchorMax = new Vector2(0.5f, 0f);
            bannerRect.pivot = new Vector2(0.5f, 0f);
            bannerRect.anchoredPosition = new Vector2(0f, 102f);
            bannerRect.sizeDelta = new Vector2(520f, 50f);

            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0.08f, 0.08f, 0.10f, 0.85f);

            GameObject nameObj = new GameObject("ItemNameText");
            nameObj.transform.SetParent(bannerObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(10f, 0f);
            nameRect.offsetMax = new Vector2(-10f, -2f);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = defaultFont;
            nameText.fontSize = 16;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = new Color(1f, 0.88f, 0.35f);

            GameObject descObj = new GameObject("ItemDescText");
            descObj.transform.SetParent(bannerObj.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0.5f);
            descRect.offsetMin = new Vector2(10f, 2f);
            descRect.offsetMax = new Vector2(-10f, 0f);
            Text descText = descObj.AddComponent<Text>();
            descText.font = defaultFont;
            descText.fontSize = 12;
            descText.alignment = TextAnchor.MiddleCenter;
            descText.color = new Color(0.9f, 0.9f, 0.92f);

            // ─────────────────────────────────────────────────────────────────
            // 6. Main Inventory Window (Modal)
            // ─────────────────────────────────────────────────────────────────
            GameObject invPanelObj = new GameObject("MainInventoryPanel");
            invPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform invPanelRect = invPanelObj.AddComponent<RectTransform>();
            invPanelRect.anchorMin = Vector2.zero;
            invPanelRect.anchorMax = Vector2.one;
            invPanelRect.offsetMin = Vector2.zero;
            invPanelRect.offsetMax = Vector2.zero;

            // Semi-darkened modal background backdrop
            Image invModalBackdrop = invPanelObj.AddComponent<Image>();
            invModalBackdrop.color = new Color(0f, 0f, 0f, 0.65f);

            // Center window box
            GameObject windowBoxObj = new GameObject("WindowBox");
            windowBoxObj.transform.SetParent(invPanelObj.transform, false);
            RectTransform windowBoxRect = windowBoxObj.AddComponent<RectTransform>();
            windowBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowBoxRect.pivot = new Vector2(0.5f, 0.5f);
            windowBoxRect.anchoredPosition = Vector2.zero;
            windowBoxRect.sizeDelta = new Vector2(820f, 540f);

            Image windowBg = windowBoxObj.AddComponent<Image>();
            windowBg.color = new Color(0.10f, 0.10f, 0.13f, 0.98f);
            var windowOutline = windowBoxObj.AddComponent<Outline>();
            windowOutline.effectColor = new Color(0.28f, 0.28f, 0.35f);
            windowOutline.effectDistance = new Vector2(2f, -2f);

            // Title Bar
            GameObject titleBarObj = new GameObject("TitleBar");
            titleBarObj.transform.SetParent(windowBoxObj.transform, false);
            RectTransform titleBarRect = titleBarObj.AddComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0f, 1f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            titleBarRect.pivot = new Vector2(0.5f, 1f);
            titleBarRect.anchoredPosition = Vector2.zero;
            titleBarRect.sizeDelta = new Vector2(0f, 48f);

            Image titleBarBg = titleBarObj.AddComponent<Image>();
            titleBarBg.color = new Color(0.15f, 0.15f, 0.20f);

            // Title Text
            GameObject titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(titleBarObj.transform, false);
            RectTransform titleTextRect = titleTextObj.AddComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0f, 0f);
            titleTextRect.anchorMax = new Vector2(1f, 1f);
            titleTextRect.offsetMin = new Vector2(20f, 0f);
            titleTextRect.offsetMax = new Vector2(-60f, 0f);
            Text titleText = titleTextObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(1.0f, 0.85f, 0.30f);
            titleText.text = "INVENTORY & STORAGE";

            // Close Button [X]
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(titleBarObj.transform, false);
            RectTransform closeBtnRect = closeBtnObj.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1f, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1f, 0.5f);
            closeBtnRect.pivot = new Vector2(1f, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-10f, 0f);
            closeBtnRect.sizeDelta = new Vector2(36f, 32f);

            Image closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.70f, 0.20f, 0.20f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();

            GameObject closeBtnTextObj = new GameObject("Text");
            closeBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform closeBtnTextRect = closeBtnTextObj.AddComponent<RectTransform>();
            closeBtnTextRect.anchorMin = Vector2.zero;
            closeBtnTextRect.anchorMax = Vector2.one;
            closeBtnTextRect.offsetMin = Vector2.zero;
            closeBtnTextRect.offsetMax = Vector2.zero;
            Text closeBtnText = closeBtnTextObj.AddComponent<Text>();
            closeBtnText.font = defaultFont;
            closeBtnText.fontSize = 16;
            closeBtnText.fontStyle = FontStyle.Bold;
            closeBtnText.alignment = TextAnchor.MiddleCenter;
            closeBtnText.color = Color.white;
            closeBtnText.text = "X";

            // ── Left Column: Item Details / Inspection Card ──────────────────
            GameObject cardObj = new GameObject("ItemDetailCard");
            cardObj.transform.SetParent(windowBoxObj.transform, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 0f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 0.5f);
            cardRect.anchoredPosition = new Vector2(20f, -24f);
            cardRect.sizeDelta = new Vector2(250f, -68f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.07f, 0.07f, 0.09f, 0.95f);

            // Preview Icon
            GameObject previewIconObj = new GameObject("PreviewIcon");
            previewIconObj.transform.SetParent(cardObj.transform, false);
            RectTransform previewIconRect = previewIconObj.AddComponent<RectTransform>();
            previewIconRect.anchorMin = new Vector2(0.5f, 1f);
            previewIconRect.anchorMax = new Vector2(0.5f, 1f);
            previewIconRect.pivot = new Vector2(0.5f, 1f);
            previewIconRect.anchoredPosition = new Vector2(0f, -15f);
            previewIconRect.sizeDelta = new Vector2(74f, 74f);
            Image previewIconImg = previewIconObj.AddComponent<Image>();
            previewIconImg.preserveAspect = true;

            // Detail Name
            GameObject detailNameObj = new GameObject("DetailName");
            detailNameObj.transform.SetParent(cardObj.transform, false);
            RectTransform detailNameRect = detailNameObj.AddComponent<RectTransform>();
            detailNameRect.anchorMin = new Vector2(0f, 1f);
            detailNameRect.anchorMax = new Vector2(1f, 1f);
            detailNameRect.pivot = new Vector2(0.5f, 1f);
            detailNameRect.anchoredPosition = new Vector2(0f, -96f);
            detailNameRect.sizeDelta = new Vector2(-20f, 26f);
            Text detailName = detailNameObj.AddComponent<Text>();
            detailName.font = defaultFont;
            detailName.fontSize = 17;
            detailName.fontStyle = FontStyle.Bold;
            detailName.alignment = TextAnchor.MiddleCenter;
            detailName.color = Color.white;

            // Rarity Badge
            GameObject detailRarityObj = new GameObject("DetailRarity");
            detailRarityObj.transform.SetParent(cardObj.transform, false);
            RectTransform detailRarityRect = detailRarityObj.AddComponent<RectTransform>();
            detailRarityRect.anchorMin = new Vector2(0f, 1f);
            detailRarityRect.anchorMax = new Vector2(1f, 1f);
            detailRarityRect.pivot = new Vector2(0.5f, 1f);
            detailRarityRect.anchoredPosition = new Vector2(0f, -124f);
            detailRarityRect.sizeDelta = new Vector2(-20f, 20f);
            Text detailRarity = detailRarityObj.AddComponent<Text>();
            detailRarity.font = defaultFont;
            detailRarity.fontSize = 13;
            detailRarity.fontStyle = FontStyle.Bold;
            detailRarity.alignment = TextAnchor.MiddleCenter;

            // Sell Price
            GameObject detailPriceObj = new GameObject("DetailPrice");
            detailPriceObj.transform.SetParent(cardObj.transform, false);
            RectTransform detailPriceRect = detailPriceObj.AddComponent<RectTransform>();
            detailPriceRect.anchorMin = new Vector2(0f, 1f);
            detailPriceRect.anchorMax = new Vector2(1f, 1f);
            detailPriceRect.pivot = new Vector2(0.5f, 1f);
            detailPriceRect.anchoredPosition = new Vector2(0f, -146f);
            detailPriceRect.sizeDelta = new Vector2(-20f, 22f);
            Text detailPrice = detailPriceObj.AddComponent<Text>();
            detailPrice.font = defaultFont;
            detailPrice.fontSize = 14;
            detailPrice.fontStyle = FontStyle.Bold;
            detailPrice.alignment = TextAnchor.MiddleCenter;
            detailPrice.color = new Color(1.0f, 0.88f, 0.35f);

            // Description
            GameObject detailDescObj = new GameObject("DetailDesc");
            detailDescObj.transform.SetParent(cardObj.transform, false);
            RectTransform detailDescRect = detailDescObj.AddComponent<RectTransform>();
            detailDescRect.anchorMin = new Vector2(0f, 0.35f);
            detailDescRect.anchorMax = new Vector2(1f, 0.65f);
            detailDescRect.offsetMin = new Vector2(12f, 0f);
            detailDescRect.offsetMax = new Vector2(-12f, 0f);
            Text detailDesc = detailDescObj.AddComponent<Text>();
            detailDesc.font = defaultFont;
            detailDesc.fontSize = 12;
            detailDesc.alignment = TextAnchor.UpperCenter;
            detailDesc.color = new Color(0.85f, 0.85f, 0.88f);

            // Extra Stats (Growth / Yield)
            GameObject detailStatsObj = new GameObject("DetailStats");
            detailStatsObj.transform.SetParent(cardObj.transform, false);
            RectTransform detailStatsRect = detailStatsObj.AddComponent<RectTransform>();
            detailStatsRect.anchorMin = new Vector2(0f, 0f);
            detailStatsRect.anchorMax = new Vector2(1f, 0.32f);
            detailStatsRect.offsetMin = new Vector2(12f, 8f);
            detailStatsRect.offsetMax = new Vector2(-12f, -8f);
            Text detailStats = detailStatsObj.AddComponent<Text>();
            detailStats.font = defaultFont;
            detailStats.fontSize = 12;
            detailStats.lineSpacing = 1.15f;
            detailStats.alignment = TextAnchor.MiddleCenter;
            detailStats.color = new Color(0.75f, 0.90f, 0.80f);

            // ── Right Column: Storage Grids & Hotbar Mirror ──────────────────
            GameObject rightAreaObj = new GameObject("SlotArea");
            rightAreaObj.transform.SetParent(windowBoxObj.transform, false);
            RectTransform rightAreaRect = rightAreaObj.AddComponent<RectTransform>();
            rightAreaRect.anchorMin = new Vector2(0f, 0f);
            rightAreaRect.anchorMax = new Vector2(1f, 1f);
            rightAreaRect.offsetMin = new Vector2(290f, 15f);
            rightAreaRect.offsetMax = new Vector2(-20f, -55f);

            // Storage Section Title
            GameObject storageLabelObj = new GameObject("StorageLabel");
            storageLabelObj.transform.SetParent(rightAreaObj.transform, false);
            RectTransform storageLabelRect = storageLabelObj.AddComponent<RectTransform>();
            storageLabelRect.anchorMin = new Vector2(0f, 1f);
            storageLabelRect.anchorMax = new Vector2(1f, 1f);
            storageLabelRect.pivot = new Vector2(0f, 1f);
            storageLabelRect.anchoredPosition = new Vector2(4f, -4f);
            storageLabelRect.sizeDelta = new Vector2(0f, 22f);
            Text storageLabel = storageLabelObj.AddComponent<Text>();
            storageLabel.font = defaultFont;
            storageLabel.fontSize = 14;
            storageLabel.fontStyle = FontStyle.Bold;
            storageLabel.color = new Color(0.82f, 0.82f, 0.88f);
            storageLabel.text = "MAIN STORAGE (Drag items to organize / stack)";

            // Main Storage Grid (3 Rows x 9 Columns = 27 Slots, indices 9..35)
            GameObject storageGridObj = new GameObject("MainStorageGrid");
            storageGridObj.transform.SetParent(rightAreaObj.transform, false);
            RectTransform storageGridRect = storageGridObj.AddComponent<RectTransform>();
            storageGridRect.anchorMin = new Vector2(0f, 1f);
            storageGridRect.anchorMax = new Vector2(1f, 1f);
            storageGridRect.pivot = new Vector2(0f, 1f);
            storageGridRect.anchoredPosition = new Vector2(0f, -30f);
            storageGridRect.sizeDelta = new Vector2(500f, 175f);

            GridLayoutGroup storageGrid = storageGridObj.AddComponent<GridLayoutGroup>();
            storageGrid.cellSize = new Vector2(50f, 50f);
            storageGrid.spacing = new Vector2(6f, 6f);
            storageGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            storageGrid.constraintCount = 9;

            var mainSlotUIList = new List<InventorySlotUI>();
            for (int i = 0; i < 27; i++)
            {
                int slotIndex = 9 + i;
                var slotComp = CreateSlotUI(storageGridObj.transform, $"StorageSlot_{i}", slotIndex, false, 50f, slotFrame, slotSelected, defaultFont);
                mainSlotUIList.Add(slotComp);
            }

            // Hotbar Mirror Section Title
            GameObject hotbarLabelObj = new GameObject("HotbarLabel");
            hotbarLabelObj.transform.SetParent(rightAreaObj.transform, false);
            RectTransform hotbarLabelRect = hotbarLabelObj.AddComponent<RectTransform>();
            hotbarLabelRect.anchorMin = new Vector2(0f, 0f);
            hotbarLabelRect.anchorMax = new Vector2(1f, 0f);
            hotbarLabelRect.pivot = new Vector2(0f, 0f);
            hotbarLabelRect.anchoredPosition = new Vector2(4f, 75f);
            hotbarLabelRect.sizeDelta = new Vector2(0f, 22f);
            Text hotbarLabel = hotbarLabelObj.AddComponent<Text>();
            hotbarLabel.font = defaultFont;
            hotbarLabel.fontSize = 14;
            hotbarLabel.fontStyle = FontStyle.Bold;
            hotbarLabel.color = new Color(1.0f, 0.84f, 0.35f);
            hotbarLabel.text = "HOTBAR (Slots 1 - 9)";

            // Hotbar Mirror Grid (1 Row x 9 Columns = 9 Slots, indices 0..8)
            GameObject mirrorGridObj = new GameObject("HotbarMirrorGrid");
            mirrorGridObj.transform.SetParent(rightAreaObj.transform, false);
            RectTransform mirrorGridRect = mirrorGridObj.AddComponent<RectTransform>();
            mirrorGridRect.anchorMin = new Vector2(0f, 0f);
            mirrorGridRect.anchorMax = new Vector2(1f, 0f);
            mirrorGridRect.pivot = new Vector2(0f, 0f);
            mirrorGridRect.anchoredPosition = new Vector2(0f, 15f);
            mirrorGridRect.sizeDelta = new Vector2(500f, 55f);

            GridLayoutGroup mirrorGrid = mirrorGridObj.AddComponent<GridLayoutGroup>();
            mirrorGrid.cellSize = new Vector2(50f, 50f);
            mirrorGrid.spacing = new Vector2(6f, 6f);
            mirrorGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            mirrorGrid.constraintCount = 9;

            var mirrorSlotUIList = new List<InventorySlotUI>();
            for (int i = 0; i < 9; i++)
            {
                var slotComp = CreateSlotUI(mirrorGridObj.transform, $"MirrorSlot_{i}", i, true, 50f, slotFrame, slotSelected, defaultFont);
                mirrorSlotUIList.Add(slotComp);
            }

            invPanelObj.SetActive(false); // Initially hidden

            // ─────────────────────────────────────────────────────────────────
            // 7. Drag & Drop Ghost Visual (Root Canvas child)
            // ─────────────────────────────────────────────────────────────────
            GameObject dragGhostObj = new GameObject("DragGhost");
            dragGhostObj.transform.SetParent(canvasObj.transform, false);
            RectTransform dragGhostRect = dragGhostObj.AddComponent<RectTransform>();
            dragGhostRect.sizeDelta = new Vector2(52f, 52f);
            dragGhostRect.pivot = new Vector2(0.5f, 0.5f);

            Image dragGhostImg = dragGhostObj.AddComponent<Image>();
            dragGhostImg.preserveAspect = true;
            dragGhostImg.raycastTarget = false;

            GameObject dragQtyObj = new GameObject("Quantity");
            dragQtyObj.transform.SetParent(dragGhostObj.transform, false);
            RectTransform dragQtyRect = dragQtyObj.AddComponent<RectTransform>();
            dragQtyRect.anchorMin = new Vector2(1f, 0f);
            dragQtyRect.anchorMax = new Vector2(1f, 0f);
            dragQtyRect.pivot = new Vector2(1f, 0f);
            dragQtyRect.anchoredPosition = new Vector2(-2f, 2f);
            dragQtyRect.sizeDelta = new Vector2(30f, 16f);
            Text dragQtyText = dragQtyObj.AddComponent<Text>();
            dragQtyText.font = defaultFont;
            dragQtyText.fontSize = 13;
            dragQtyText.fontStyle = FontStyle.Bold;
            dragQtyText.alignment = TextAnchor.LowerRight;
            dragQtyText.color = Color.white;
            dragQtyText.raycastTarget = false;
            var dragOutline = dragQtyObj.AddComponent<Outline>();
            dragOutline.effectColor = Color.black;

            dragGhostObj.SetActive(false);

            // ─────────────────────────────────────────────────────────────────
            // 8. Hover Tooltip (Root Canvas child)
            // ─────────────────────────────────────────────────────────────────
            GameObject tooltipObj = new GameObject("ItemTooltip");
            tooltipObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tooltipRect = tooltipObj.AddComponent<RectTransform>();
            tooltipRect.pivot = new Vector2(0f, 0f);
            tooltipRect.sizeDelta = new Vector2(250f, 150f);

            Image tooltipBg = tooltipObj.AddComponent<Image>();
            tooltipBg.color = new Color(0.06f, 0.06f, 0.08f, 0.96f);
            tooltipBg.raycastTarget = false;
            var tooltipOutline = tooltipObj.AddComponent<Outline>();
            tooltipOutline.effectColor = new Color(0.35f, 0.35f, 0.42f);
            tooltipOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Tooltip Name
            GameObject ttNameObj = new GameObject("Name");
            ttNameObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform ttNameRect = ttNameObj.AddComponent<RectTransform>();
            ttNameRect.anchorMin = new Vector2(0f, 1f);
            ttNameRect.anchorMax = new Vector2(1f, 1f);
            ttNameRect.pivot = new Vector2(0f, 1f);
            ttNameRect.anchoredPosition = new Vector2(10f, -8f);
            ttNameRect.sizeDelta = new Vector2(-20f, 22f);
            Text ttName = ttNameObj.AddComponent<Text>();
            ttName.font = defaultFont;
            ttName.fontSize = 15;
            ttName.fontStyle = FontStyle.Bold;
            ttName.color = Color.white;
            ttName.raycastTarget = false;

            // Tooltip Rarity
            GameObject ttRarityObj = new GameObject("Rarity");
            ttRarityObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform ttRarityRect = ttRarityObj.AddComponent<RectTransform>();
            ttRarityRect.anchorMin = new Vector2(0f, 1f);
            ttRarityRect.anchorMax = new Vector2(1f, 1f);
            ttRarityRect.pivot = new Vector2(0f, 1f);
            ttRarityRect.anchoredPosition = new Vector2(10f, -30f);
            ttRarityRect.sizeDelta = new Vector2(-20f, 18f);
            Text ttRarity = ttRarityObj.AddComponent<Text>();
            ttRarity.font = defaultFont;
            ttRarity.fontSize = 12;
            ttRarity.fontStyle = FontStyle.Bold;
            ttRarity.raycastTarget = false;

            // Tooltip Price
            GameObject ttPriceObj = new GameObject("Price");
            ttPriceObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform ttPriceRect = ttPriceObj.AddComponent<RectTransform>();
            ttPriceRect.anchorMin = new Vector2(0f, 1f);
            ttPriceRect.anchorMax = new Vector2(1f, 1f);
            ttPriceRect.pivot = new Vector2(0f, 1f);
            ttPriceRect.anchoredPosition = new Vector2(10f, -48f);
            ttPriceRect.sizeDelta = new Vector2(-20f, 18f);
            Text ttPrice = ttPriceObj.AddComponent<Text>();
            ttPrice.font = defaultFont;
            ttPrice.fontSize = 12;
            ttPrice.fontStyle = FontStyle.Bold;
            ttPrice.color = new Color(1.0f, 0.88f, 0.35f);
            ttPrice.raycastTarget = false;

            // Tooltip Description
            GameObject ttDescObj = new GameObject("Desc");
            ttDescObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform ttDescRect = ttDescObj.AddComponent<RectTransform>();
            ttDescRect.anchorMin = new Vector2(0f, 0f);
            ttDescRect.anchorMax = new Vector2(1f, 1f);
            ttDescRect.offsetMin = new Vector2(10f, 26f);
            ttDescRect.offsetMax = new Vector2(-10f, -68f);
            Text ttDesc = ttDescObj.AddComponent<Text>();
            ttDesc.font = defaultFont;
            ttDesc.fontSize = 11;
            ttDesc.color = new Color(0.85f, 0.85f, 0.88f);
            ttDesc.raycastTarget = false;

            // Tooltip Extra Stats
            GameObject ttStatsObj = new GameObject("Stats");
            ttStatsObj.transform.SetParent(tooltipObj.transform, false);
            RectTransform ttStatsRect = ttStatsObj.AddComponent<RectTransform>();
            ttStatsRect.anchorMin = new Vector2(0f, 0f);
            ttStatsRect.anchorMax = new Vector2(1f, 0f);
            ttStatsRect.pivot = new Vector2(0f, 0f);
            ttStatsRect.anchoredPosition = new Vector2(10f, 6f);
            ttStatsRect.sizeDelta = new Vector2(-20f, 20f);
            Text ttStats = ttStatsObj.AddComponent<Text>();
            ttStats.font = defaultFont;
            ttStats.fontSize = 11;
            ttStats.fontStyle = FontStyle.Italic;
            ttStats.color = new Color(0.65f, 0.90f, 0.75f);
            ttStats.raycastTarget = false;

            tooltipObj.SetActive(false);

            // ─────────────────────────────────────────────────────────────────
            // 9. Updated Controls Guide Card (Top Left)
            // ─────────────────────────────────────────────────────────────────
            GameObject guideObj = new GameObject("ControlsGuide");
            guideObj.transform.SetParent(canvasObj.transform, false);
            RectTransform guideRect = guideObj.AddComponent<RectTransform>();
            guideRect.anchorMin = new Vector2(0f, 1f);
            guideRect.anchorMax = new Vector2(0f, 1f);
            guideRect.pivot = new Vector2(0f, 1f);
            guideRect.anchoredPosition = new Vector2(25f, -25f);
            guideRect.sizeDelta = new Vector2(295f, 180f);

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
            guideText.fontSize = 11;
            guideText.lineSpacing = 1.15f;
            guideText.color = new Color(0.92f, 0.92f, 0.95f);
            guideText.text = "<b>FARMING, INVENTORY & THE MERCHANT:</b>\n" +
                             "• <b>WASD</b>: Move character\n" +
                             "• <b>1 - 9 / Scroll</b>: Select Hotbar Slot\n" +
                             "• <b>Left Click</b>: Till / Water / Shovel / Plant / Harvest\n" +
                             "• <b>E</b>: Talk to The Merchant\n" +
                             "• Click <b>[INVENTORY]</b>: Open / Close Storage\n" +
                             "• <b>The Merchant</b>: BUY rotating seeds (2m) & SELL crops\n" +
                             "• <b>Shovel</b>: Digs up and removes planted seeds/crops\n" +
                             "• <b>T</b>: Advance to Next Day (after 7 PM)\n" +
                             "<i>[1] Hoe  [2] Water Can  [3] Shovel  [4..9] Seeds</i>";

            // ─────────────────────────────────────────────────────────────────
            // 10. Wire Everything to InventoryUI SerializedObject
            // ─────────────────────────────────────────────────────────────────
            SerializedObject uiSo = new SerializedObject(inventoryUI);

            // Hotbar HUD
            uiSo.FindProperty("hotbarContainer").objectReferenceValue = hotbarObj.transform;
            SerializedProperty hotbarListProp = uiSo.FindProperty("hotbarSlotUIs");
            hotbarListProp.ClearArray();
            for (int i = 0; i < hotbarSlotUIList.Count; i++)
            {
                hotbarListProp.InsertArrayElementAtIndex(i);
                hotbarListProp.GetArrayElementAtIndex(i).objectReferenceValue = hotbarSlotUIList[i];
            }

            // Main Inventory Modal
            uiSo.FindProperty("mainInventoryPanel").objectReferenceValue = invPanelObj;
            uiSo.FindProperty("mainStorageGrid").objectReferenceValue = storageGridObj.transform;
            uiSo.FindProperty("hotbarMirrorGrid").objectReferenceValue = mirrorGridObj.transform;
            uiSo.FindProperty("inventoryCloseButton").objectReferenceValue = closeBtn;

            SerializedProperty mainListProp = uiSo.FindProperty("mainStorageSlotUIs");
            mainListProp.ClearArray();
            for (int i = 0; i < mainSlotUIList.Count; i++)
            {
                mainListProp.InsertArrayElementAtIndex(i);
                mainListProp.GetArrayElementAtIndex(i).objectReferenceValue = mainSlotUIList[i];
            }

            SerializedProperty mirrorListProp = uiSo.FindProperty("mirrorSlotUIs");
            mirrorListProp.ClearArray();
            for (int i = 0; i < mirrorSlotUIList.Count; i++)
            {
                mirrorListProp.InsertArrayElementAtIndex(i);
                mirrorListProp.GetArrayElementAtIndex(i).objectReferenceValue = mirrorSlotUIList[i];
            }

            // Dedicated Inventory Button
            uiSo.FindProperty("inventoryToggleButton").objectReferenceValue = invToggleBtn;

            // Selected Item Banner
            uiSo.FindProperty("selectedItemNameText").objectReferenceValue = nameText;
            uiSo.FindProperty("selectedItemDescText").objectReferenceValue = descText;

            // Details Card
            uiSo.FindProperty("detailIconImage").objectReferenceValue = previewIconImg;
            uiSo.FindProperty("detailNameText").objectReferenceValue = detailName;
            uiSo.FindProperty("detailRarityText").objectReferenceValue = detailRarity;
            uiSo.FindProperty("detailPriceText").objectReferenceValue = detailPrice;
            uiSo.FindProperty("detailDescText").objectReferenceValue = detailDesc;
            uiSo.FindProperty("detailExtraStatsText").objectReferenceValue = detailStats;

            // Drag Ghost
            uiSo.FindProperty("dragGhostObject").objectReferenceValue = dragGhostObj;
            uiSo.FindProperty("dragGhostIcon").objectReferenceValue = dragGhostImg;
            uiSo.FindProperty("dragGhostQuantityText").objectReferenceValue = dragQtyText;

            // Tooltip
            uiSo.FindProperty("tooltipPanel").objectReferenceValue = tooltipObj;
            uiSo.FindProperty("tooltipNameText").objectReferenceValue = ttName;
            uiSo.FindProperty("tooltipRarityText").objectReferenceValue = ttRarity;
            uiSo.FindProperty("tooltipPriceText").objectReferenceValue = ttPrice;
            uiSo.FindProperty("tooltipDescText").objectReferenceValue = ttDesc;
            uiSo.FindProperty("tooltipStatsText").objectReferenceValue = ttStats;

            // Day HUD / Calendar / Dialog references (existing in scene)
            Transform dayHud = canvasObj.transform.Find("DayHUD");
            if (dayHud != null)
            {
                Transform dayTextTrans = dayHud.Find("DayText");
                if (dayTextTrans != null) uiSo.FindProperty("dayText").objectReferenceValue = dayTextTrans.GetComponent<Text>();

                Transform advBtnTrans = dayHud.Find("AdvanceDayButton");
                if (advBtnTrans != null) uiSo.FindProperty("advanceDayButton").objectReferenceValue = advBtnTrans.GetComponent<Button>();

                Transform clockTextTrans = dayHud.Find("ClockText");
                if (clockTextTrans != null) uiSo.FindProperty("clockText").objectReferenceValue = clockTextTrans.GetComponent<Text>();

                Transform lockedTextTrans = dayHud.Find("NextDayLockedText");
                if (lockedTextTrans != null) uiSo.FindProperty("nextDayLockedText").objectReferenceValue = lockedTextTrans.GetComponent<Text>();
            }

            Transform notifPanel = canvasObj.transform.Find("NotificationBanner");
            if (notifPanel != null)
            {
                uiSo.FindProperty("notificationPanel").objectReferenceValue = notifPanel.gameObject;
                Transform notifTextTrans = notifPanel.Find("Text");
                if (notifTextTrans != null) uiSo.FindProperty("notificationText").objectReferenceValue = notifTextTrans.GetComponent<Text>();
            }

            Transform confirmDialog = canvasObj.transform.Find("ConfirmDialog");
            if (confirmDialog != null)
            {
                uiSo.FindProperty("confirmDialogPanel").objectReferenceValue = confirmDialog.gameObject;
                Transform dialogBox = confirmDialog.Find("DialogBox");
                if (dialogBox != null)
                {
                    Transform t = dialogBox.Find("TitleText");
                    if (t != null) uiSo.FindProperty("confirmTitleText").objectReferenceValue = t.GetComponent<Text>();
                    Transform b = dialogBox.Find("BodyText");
                    if (b != null) uiSo.FindProperty("confirmBodyText").objectReferenceValue = b.GetComponent<Text>();
                    Transform y = dialogBox.Find("YesButton");
                    if (y != null) uiSo.FindProperty("confirmYesButton").objectReferenceValue = y.GetComponent<Button>();
                    Transform n = dialogBox.Find("NoButton");
                    if (n != null) uiSo.FindProperty("confirmNoButton").objectReferenceValue = n.GetComponent<Button>();
                }
            }

            uiSo.ApplyModifiedProperties();

            // ─────────────────────────────────────────────────────────────────
            // 11. Coin HUD (Anchored Top-Right, directly below DayHUD)
            // ─────────────────────────────────────────────────────────────────
            GameObject coinHudObj = new GameObject("CoinHUD");
            coinHudObj.transform.SetParent(canvasObj.transform, false);
            RectTransform coinHudRect = coinHudObj.AddComponent<RectTransform>();
            coinHudRect.anchorMin = new Vector2(1f, 1f);
            coinHudRect.anchorMax = new Vector2(1f, 1f);
            coinHudRect.pivot = new Vector2(1f, 1f);
            coinHudRect.anchoredPosition = new Vector2(-25f, -118f);
            coinHudRect.sizeDelta = new Vector2(220f, 36f);

            Image coinHudBg = coinHudObj.AddComponent<Image>();
            coinHudBg.color = new Color(0.08f, 0.08f, 0.10f, 0.88f);

            // Coin Icon
            GameObject coinIconObj = new GameObject("CoinIcon");
            coinIconObj.transform.SetParent(coinHudObj.transform, false);
            RectTransform coinIconRect = coinIconObj.AddComponent<RectTransform>();
            coinIconRect.anchorMin = new Vector2(0f, 0.5f);
            coinIconRect.anchorMax = new Vector2(0f, 0.5f);
            coinIconRect.pivot = new Vector2(0f, 0.5f);
            coinIconRect.anchoredPosition = new Vector2(10f, 0f);
            coinIconRect.sizeDelta = new Vector2(24f, 24f);

            Image coinIconImg = coinIconObj.AddComponent<Image>();
            coinIconImg.sprite = coinIcon;
            coinIconImg.preserveAspect = true;

            // Coin Balance Text
            GameObject coinBalanceObj = new GameObject("CoinBalanceText");
            coinBalanceObj.transform.SetParent(coinHudObj.transform, false);
            RectTransform coinBalanceRect = coinBalanceObj.AddComponent<RectTransform>();
            coinBalanceRect.anchorMin = new Vector2(0f, 0f);
            coinBalanceRect.anchorMax = new Vector2(1f, 1f);
            coinBalanceRect.offsetMin = new Vector2(40f, 0f);
            coinBalanceRect.offsetMax = new Vector2(-12f, 0f);

            Text coinBalanceText = coinBalanceObj.AddComponent<Text>();
            coinBalanceText.font = defaultFont;
            coinBalanceText.fontSize = 16;
            coinBalanceText.fontStyle = FontStyle.Bold;
            coinBalanceText.alignment = TextAnchor.MiddleRight;
            coinBalanceText.color = new Color(1.0f, 0.88f, 0.32f);
            coinBalanceText.text = "100 G";

            CoinHUD coinHUDComp = coinHudObj.AddComponent<CoinHUD>();
            SerializedObject coinHudSo = new SerializedObject(coinHUDComp);
            coinHudSo.FindProperty("coinText").objectReferenceValue = coinBalanceText;
            coinHudSo.FindProperty("coinIcon").objectReferenceValue = coinIconImg;
            coinHudSo.FindProperty("pulseTarget").objectReferenceValue = coinHudObj.transform;
            coinHudSo.ApplyModifiedProperties();

            // ─────────────────────────────────────────────────────────────────
            // 12. Street Vendor Selling Modal Interface
            // ─────────────────────────────────────────────────────────────────
            GameObject vendorModalObj = new GameObject("VendorModalPanel");
            vendorModalObj.transform.SetParent(canvasObj.transform, false);
            RectTransform vendorModalRect = vendorModalObj.AddComponent<RectTransform>();
            vendorModalRect.anchorMin = Vector2.zero;
            vendorModalRect.anchorMax = Vector2.one;
            vendorModalRect.offsetMin = Vector2.zero;
            vendorModalRect.offsetMax = Vector2.zero;

            Image modalScrim = vendorModalObj.AddComponent<Image>();
            modalScrim.color = new Color(0f, 0f, 0f, 0.65f);

            // Vendor Window Frame
            GameObject vendorBoxObj = new GameObject("VendorWindow");
            vendorBoxObj.transform.SetParent(vendorModalObj.transform, false);
            RectTransform vendorBoxRect = vendorBoxObj.AddComponent<RectTransform>();
            vendorBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            vendorBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            vendorBoxRect.pivot = new Vector2(0.5f, 0.5f);
            vendorBoxRect.sizeDelta = new Vector2(860f, 530f);

            Image vendorBoxBg = vendorBoxObj.AddComponent<Image>();
            vendorBoxBg.color = new Color(0.10f, 0.10f, 0.13f, 0.98f);
            var vendorOutline = vendorBoxObj.AddComponent<Outline>();
            vendorOutline.effectColor = new Color(0.40f, 0.38f, 0.30f, 1f);
            vendorOutline.effectDistance = new Vector2(2f, -2f);

            // Title Bar
            GameObject vendorTitleObj = new GameObject("TitleBar");
            vendorTitleObj.transform.SetParent(vendorBoxObj.transform, false);
            RectTransform vTitleRect = vendorTitleObj.AddComponent<RectTransform>();
            vTitleRect.anchorMin = new Vector2(0f, 1f);
            vTitleRect.anchorMax = new Vector2(1f, 1f);
            vTitleRect.pivot = new Vector2(0.5f, 1f);
            vTitleRect.anchoredPosition = new Vector2(0f, 0f);
            vTitleRect.sizeDelta = new Vector2(0f, 55f);

            Image titleBg = vendorTitleObj.AddComponent<Image>();
            titleBg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            // Title Text
            GameObject vtTextObj = new GameObject("TitleText");
            vtTextObj.transform.SetParent(vendorTitleObj.transform, false);
            RectTransform vtTextRect = vtTextObj.AddComponent<RectTransform>();
            vtTextRect.anchorMin = new Vector2(0f, 0f);
            vtTextRect.anchorMax = new Vector2(0.28f, 1f);
            vtTextRect.offsetMin = new Vector2(18f, 0f);
            vtTextRect.offsetMax = new Vector2(0f, 0f);
            Text vtText = vtTextObj.AddComponent<Text>();
            vtText.font = defaultFont;
            vtText.fontSize = 18;
            vtText.fontStyle = FontStyle.Bold;
            vtText.alignment = TextAnchor.MiddleLeft;
            vtText.color = new Color(1.0f, 0.88f, 0.35f);
            vtText.text = "THE MERCHANT";

            // Tab 1: BUY SEEDS
            GameObject buyTabObj = new GameObject("BuyTabButton");
            buyTabObj.transform.SetParent(vendorTitleObj.transform, false);
            RectTransform buyTabRect = buyTabObj.AddComponent<RectTransform>();
            buyTabRect.anchorMin = new Vector2(0f, 0.5f);
            buyTabRect.anchorMax = new Vector2(0f, 0.5f);
            buyTabRect.pivot = new Vector2(0f, 0.5f);
            buyTabRect.anchoredPosition = new Vector2(195f, 0f);
            buyTabRect.sizeDelta = new Vector2(115f, 34f);
            Image buyTabBg = buyTabObj.AddComponent<Image>();
            buyTabBg.color = new Color(0.28f, 0.42f, 0.65f, 1f);
            Button buyTabBtn = buyTabObj.AddComponent<Button>();
            Text buyTabTxt = CreateButtonText(buyTabObj.transform, "BUY SEEDS", defaultFont, 12);
            buyTabTxt.fontStyle = FontStyle.Bold;

            // Tab 2: SELL CROPS
            GameObject sellTabObj = new GameObject("SellTabButton");
            sellTabObj.transform.SetParent(vendorTitleObj.transform, false);
            RectTransform sellTabRect = sellTabObj.AddComponent<RectTransform>();
            sellTabRect.anchorMin = new Vector2(0f, 0.5f);
            sellTabRect.anchorMax = new Vector2(0f, 0.5f);
            sellTabRect.pivot = new Vector2(0f, 0.5f);
            sellTabRect.anchoredPosition = new Vector2(320f, 0f);
            sellTabRect.sizeDelta = new Vector2(115f, 34f);
            Image sellTabBg = sellTabObj.AddComponent<Image>();
            sellTabBg.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            Button sellTabBtn = sellTabObj.AddComponent<Button>();
            Text sellTabTxt = CreateButtonText(sellTabObj.transform, "SELL CROPS", defaultFont, 12);
            sellTabTxt.fontStyle = FontStyle.Bold;

            // Player Coin Balance in Header
            GameObject headerCoinsObj = new GameObject("HeaderCoins");
            headerCoinsObj.transform.SetParent(vendorTitleObj.transform, false);
            RectTransform hcRect = headerCoinsObj.AddComponent<RectTransform>();
            hcRect.anchorMin = new Vector2(0.55f, 0f);
            hcRect.anchorMax = new Vector2(1f, 1f);
            hcRect.offsetMin = new Vector2(0f, 0f);
            hcRect.offsetMax = new Vector2(-60f, 0f);

            Text headerCoinsText = headerCoinsObj.AddComponent<Text>();
            headerCoinsText.font = defaultFont;
            headerCoinsText.fontSize = 15;
            headerCoinsText.fontStyle = FontStyle.Bold;
            headerCoinsText.alignment = TextAnchor.MiddleRight;
            headerCoinsText.color = new Color(1.0f, 0.88f, 0.35f);
            headerCoinsText.text = "100 G";

            // Close Button [X]
            GameObject vCloseBtnObj = new GameObject("CloseButton");
            vCloseBtnObj.transform.SetParent(vendorTitleObj.transform, false);
            RectTransform vCloseBtnRect = vCloseBtnObj.AddComponent<RectTransform>();
            vCloseBtnRect.anchorMin = new Vector2(1f, 0.5f);
            vCloseBtnRect.anchorMax = new Vector2(1f, 0.5f);
            vCloseBtnRect.pivot = new Vector2(1f, 0.5f);
            vCloseBtnRect.anchoredPosition = new Vector2(-12f, 0f);
            vCloseBtnRect.sizeDelta = new Vector2(36f, 36f);

            Image vCloseBtnBg = vCloseBtnObj.AddComponent<Image>();
            vCloseBtnBg.color = new Color(0.35f, 0.12f, 0.12f, 0.9f);
            Button vendorCloseBtn = vCloseBtnObj.AddComponent<Button>();
            CreateButtonText(vCloseBtnObj.transform, "X", defaultFont, 18);

            // ── Left Column: Eligible Crops / Seed Catalog List ───────────────
            GameObject leftListObj = new GameObject("CropsListArea");
            leftListObj.transform.SetParent(vendorBoxObj.transform, false);
            RectTransform leftListRect = leftListObj.AddComponent<RectTransform>();
            leftListRect.anchorMin = new Vector2(0f, 0f);
            leftListRect.anchorMax = new Vector2(0.53f, 1f);
            leftListRect.offsetMin = new Vector2(16f, 16f);
            leftListRect.offsetMax = new Vector2(-8f, -65f);

            Image leftBg = leftListObj.AddComponent<Image>();
            leftBg.color = new Color(0.06f, 0.06f, 0.08f, 0.9f);

            // Left Header: Title
            GameObject listHeaderObj = new GameObject("ListHeader");
            listHeaderObj.transform.SetParent(leftListObj.transform, false);
            RectTransform lhRect = listHeaderObj.AddComponent<RectTransform>();
            lhRect.anchorMin = new Vector2(0f, 1f);
            lhRect.anchorMax = new Vector2(1f, 1f);
            lhRect.pivot = new Vector2(0f, 1f);
            lhRect.anchoredPosition = new Vector2(10f, -6f);
            lhRect.sizeDelta = new Vector2(-100f, 20f);
            Text lhText = listHeaderObj.AddComponent<Text>();
            lhText.font = defaultFont;
            lhText.fontSize = 12;
            lhText.fontStyle = FontStyle.Bold;
            lhText.color = new Color(0.85f, 0.88f, 0.95f);
            lhText.text = "ROTATING SEED CATALOG";

            // Live Timer Text
            GameObject timerObj = new GameObject("RestockTimerText");
            timerObj.transform.SetParent(leftListObj.transform, false);
            RectTransform tmRect = timerObj.AddComponent<RectTransform>();
            tmRect.anchorMin = new Vector2(0f, 1f);
            tmRect.anchorMax = new Vector2(1f, 1f);
            tmRect.pivot = new Vector2(0f, 1f);
            tmRect.anchoredPosition = new Vector2(10f, -26f);
            tmRect.sizeDelta = new Vector2(-100f, 18f);
            Text timerText = timerObj.AddComponent<Text>();
            timerText.font = defaultFont;
            timerText.fontSize = 11;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = new Color(1.0f, 0.88f, 0.35f);
            timerText.text = "⏱ Restocks In: 02:00";

            // Reroll/Restock button (pill)
            GameObject rerollObj = new GameObject("RerollButton");
            rerollObj.transform.SetParent(leftListObj.transform, false);
            RectTransform rrRect = rerollObj.AddComponent<RectTransform>();
            rrRect.anchorMin = new Vector2(1f, 1f);
            rrRect.anchorMax = new Vector2(1f, 1f);
            rrRect.pivot = new Vector2(1f, 1f);
            rrRect.anchoredPosition = new Vector2(-10f, -8f);
            rrRect.sizeDelta = new Vector2(78f, 24f);
            Image rrBg = rerollObj.AddComponent<Image>();
            rrBg.color = new Color(0.22f, 0.28f, 0.40f);
            Button rerollBtn = rerollObj.AddComponent<Button>();
            Text rrTxt = CreateButtonText(rerollObj.transform, "⟳ Restock", defaultFont, 10);
            rrTxt.fontStyle = FontStyle.Bold;

            // Scroll View / Rows Container
            GameObject rowsContainerObj = new GameObject("RowsContainer");
            rowsContainerObj.transform.SetParent(leftListObj.transform, false);
            RectTransform rcRect = rowsContainerObj.AddComponent<RectTransform>();
            rcRect.anchorMin = new Vector2(0f, 0f);
            rcRect.anchorMax = new Vector2(1f, 1f);
            rcRect.offsetMin = new Vector2(8f, 8f);
            rcRect.offsetMax = new Vector2(-8f, -48f);

            VerticalLayoutGroup rowsLayout = rowsContainerObj.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 6f;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = false;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            // Pre-spawn 8 styled VendorItemRowUI slots
            for (int i = 0; i < 8; i++)
            {
                CreateVendorRowUI(rowsContainerObj.transform, $"VendorRow_{i}", slotFrame, slotSelected, coinIcon, defaultFont);
            }

            // Empty State Object
            GameObject emptyStateObj = new GameObject("EmptyState");
            emptyStateObj.transform.SetParent(leftListObj.transform, false);
            RectTransform esRect = emptyStateObj.AddComponent<RectTransform>();
            esRect.anchorMin = Vector2.zero;
            esRect.anchorMax = Vector2.one;
            esRect.offsetMin = new Vector2(20f, 20f);
            esRect.offsetMax = new Vector2(-20f, -20f);

            Text emptyText = emptyStateObj.AddComponent<Text>();
            emptyText.font = defaultFont;
            emptyText.fontSize = 15;
            emptyText.lineSpacing = 1.2f;
            emptyText.alignment = TextAnchor.MiddleCenter;
            emptyText.color = new Color(0.6f, 0.6f, 0.65f);
            emptyText.text = "No sellable crops in inventory.\n\nHarvest some crops from the garden\nand bring them back to sell!";
            emptyStateObj.SetActive(false);

            // ── Right Column: Selected Crop Details & Selling Controls ───────
            GameObject rightSellObj = new GameObject("SellDetailArea");
            rightSellObj.transform.SetParent(vendorBoxObj.transform, false);
            RectTransform rightSellRect = rightSellObj.AddComponent<RectTransform>();
            rightSellRect.anchorMin = new Vector2(0.53f, 0f);
            rightSellRect.anchorMax = new Vector2(1f, 1f);
            rightSellRect.offsetMin = new Vector2(8f, 16f);
            rightSellRect.offsetMax = new Vector2(-16f, -65f);

            Image rightBg = rightSellObj.AddComponent<Image>();
            rightBg.color = new Color(0.07f, 0.07f, 0.09f, 0.9f);

            // Selection Details Group Root
            GameObject selDetailsObj = new GameObject("SelectionDetailsGroup");
            selDetailsObj.transform.SetParent(rightSellObj.transform, false);
            RectTransform sdRect = selDetailsObj.AddComponent<RectTransform>();
            sdRect.anchorMin = Vector2.zero;
            sdRect.anchorMax = Vector2.one;
            sdRect.offsetMin = Vector2.zero;
            sdRect.offsetMax = Vector2.zero;

            // Selected Crop Preview Card (Top portion)
            GameObject cropCardObj = new GameObject("CropCard");
            cropCardObj.transform.SetParent(selDetailsObj.transform, false);
            RectTransform ccRect = cropCardObj.AddComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0f, 1f);
            ccRect.anchorMax = new Vector2(1f, 1f);
            ccRect.pivot = new Vector2(0.5f, 1f);
            ccRect.anchoredPosition = new Vector2(0f, -10f);
            ccRect.sizeDelta = new Vector2(-24f, 120f);

            Image cropCardBg = cropCardObj.AddComponent<Image>();
            cropCardBg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

            // Selected Icon (64x64)
            GameObject selIconObj = new GameObject("SelectedIcon");
            selIconObj.transform.SetParent(cropCardObj.transform, false);
            RectTransform sIconRect = selIconObj.AddComponent<RectTransform>();
            sIconRect.anchorMin = new Vector2(0f, 0.5f);
            sIconRect.anchorMax = new Vector2(0f, 0.5f);
            sIconRect.pivot = new Vector2(0f, 0.5f);
            sIconRect.anchoredPosition = new Vector2(14f, 0f);
            sIconRect.sizeDelta = new Vector2(64f, 64f);
            Image selIconImg = selIconObj.AddComponent<Image>();
            selIconImg.preserveAspect = true;

            // Selected Name Text
            GameObject selNameObj = new GameObject("SelectedName");
            selNameObj.transform.SetParent(cropCardObj.transform, false);
            RectTransform sNameRect = selNameObj.AddComponent<RectTransform>();
            sNameRect.anchorMin = new Vector2(0f, 1f);
            sNameRect.anchorMax = new Vector2(1f, 1f);
            sNameRect.pivot = new Vector2(0f, 1f);
            sNameRect.anchoredPosition = new Vector2(90f, -14f);
            sNameRect.sizeDelta = new Vector2(-95f, 22f);
            Text selNameText = selNameObj.AddComponent<Text>();
            selNameText.font = defaultFont;
            selNameText.fontSize = 16;
            selNameText.fontStyle = FontStyle.Bold;
            selNameText.color = Color.white;
            selNameText.text = "Turnip";

            // Selected Rarity Badge Text
            GameObject selRarityObj = new GameObject("SelectedRarity");
            selRarityObj.transform.SetParent(cropCardObj.transform, false);
            RectTransform sRarityRect = selRarityObj.AddComponent<RectTransform>();
            sRarityRect.anchorMin = new Vector2(0f, 1f);
            sRarityRect.anchorMax = new Vector2(1f, 1f);
            sRarityRect.pivot = new Vector2(0f, 1f);
            sRarityRect.anchoredPosition = new Vector2(90f, -38f);
            sRarityRect.sizeDelta = new Vector2(-95f, 20f);
            Text selRarityText = selRarityObj.AddComponent<Text>();
            selRarityText.font = defaultFont;
            selRarityText.fontSize = 12;
            selRarityText.fontStyle = FontStyle.Bold;
            selRarityText.color = Color.white;
            selRarityText.text = "[COMMON]";

            // Selected Unit Price Text
            GameObject selUnitPriceObj = new GameObject("SelectedUnitPrice");
            selUnitPriceObj.transform.SetParent(cropCardObj.transform, false);
            RectTransform sPriceRect = selUnitPriceObj.AddComponent<RectTransform>();
            sPriceRect.anchorMin = new Vector2(0f, 1f);
            sPriceRect.anchorMax = new Vector2(1f, 1f);
            sPriceRect.pivot = new Vector2(0f, 1f);
            sPriceRect.anchoredPosition = new Vector2(90f, -60f);
            sPriceRect.sizeDelta = new Vector2(-95f, 20f);
            Text selUnitPriceText = selUnitPriceObj.AddComponent<Text>();
            selUnitPriceText.font = defaultFont;
            selUnitPriceText.fontSize = 13;
            selUnitPriceText.fontStyle = FontStyle.Bold;
            selUnitPriceText.color = new Color(1.0f, 0.88f, 0.35f);
            selUnitPriceText.text = "Unit Price: 15 G";

            // Selected Owned Text
            GameObject selOwnedObj = new GameObject("SelectedOwned");
            selOwnedObj.transform.SetParent(cropCardObj.transform, false);
            RectTransform sOwnedRect = selOwnedObj.AddComponent<RectTransform>();
            sOwnedRect.anchorMin = new Vector2(0f, 1f);
            sOwnedRect.anchorMax = new Vector2(1f, 1f);
            sOwnedRect.pivot = new Vector2(0f, 1f);
            sOwnedRect.anchoredPosition = new Vector2(90f, -82f);
            sOwnedRect.sizeDelta = new Vector2(-95f, 20f);
            Text selOwnedText = selOwnedObj.AddComponent<Text>();
            selOwnedText.font = defaultFont;
            selOwnedText.fontSize = 13;
            selOwnedText.color = new Color(0.8f, 0.82f, 0.88f);
            selOwnedText.text = "Owned: 5";

            // Quantity Adjustment Section (Middle portion)
            GameObject qtySectionObj = new GameObject("QuantitySection");
            qtySectionObj.transform.SetParent(selDetailsObj.transform, false);
            RectTransform qsRect = qtySectionObj.AddComponent<RectTransform>();
            qsRect.anchorMin = new Vector2(0f, 0.5f);
            qsRect.anchorMax = new Vector2(1f, 0.5f);
            qsRect.pivot = new Vector2(0.5f, 0.5f);
            qsRect.anchoredPosition = new Vector2(0f, -8f);
            qsRect.sizeDelta = new Vector2(-24f, 85f);

            // Quantity Display Text
            GameObject qtyDispObj = new GameObject("QuantityDisplayText");
            qtyDispObj.transform.SetParent(qtySectionObj.transform, false);
            RectTransform qdRect = qtyDispObj.AddComponent<RectTransform>();
            qdRect.anchorMin = new Vector2(0f, 1f);
            qdRect.anchorMax = new Vector2(1f, 1f);
            qdRect.pivot = new Vector2(0.5f, 1f);
            qdRect.anchoredPosition = new Vector2(0f, 0f);
            qdRect.sizeDelta = new Vector2(0f, 24f);
            Text qtyDispText = qtyDispObj.AddComponent<Text>();
            qtyDispText.font = defaultFont;
            qtyDispText.fontSize = 15;
            qtyDispText.fontStyle = FontStyle.Bold;
            qtyDispText.alignment = TextAnchor.MiddleCenter;
            qtyDispText.color = Color.white;
            qtyDispText.text = "Sell Quantity: 1 / 5";

            // Minus Button [-]
            GameObject minusBtnObj = new GameObject("MinusButton");
            minusBtnObj.transform.SetParent(qtySectionObj.transform, false);
            RectTransform minusRect = minusBtnObj.AddComponent<RectTransform>();
            minusRect.anchorMin = new Vector2(0f, 0f);
            minusRect.anchorMax = new Vector2(0f, 0f);
            minusRect.pivot = new Vector2(0f, 0f);
            minusRect.anchoredPosition = new Vector2(0f, 10f);
            minusRect.sizeDelta = new Vector2(38f, 32f);
            Image minusBg = minusBtnObj.AddComponent<Image>();
            minusBg.color = new Color(0.22f, 0.22f, 0.28f);
            Button minusBtn = minusBtnObj.AddComponent<Button>();
            CreateButtonText(minusBtnObj.transform, "-", defaultFont, 18);

            // Plus Button [+]
            GameObject plusBtnObj = new GameObject("PlusButton");
            plusBtnObj.transform.SetParent(qtySectionObj.transform, false);
            RectTransform plusRect = plusBtnObj.AddComponent<RectTransform>();
            plusRect.anchorMin = new Vector2(0f, 0f);
            plusRect.anchorMax = new Vector2(0f, 0f);
            plusRect.pivot = new Vector2(0f, 0f);
            plusRect.anchoredPosition = new Vector2(245f, 10f);
            plusRect.sizeDelta = new Vector2(38f, 32f);
            Image plusBg = plusBtnObj.AddComponent<Image>();
            plusBg.color = new Color(0.22f, 0.22f, 0.28f);
            Button plusBtn = plusBtnObj.AddComponent<Button>();
            CreateButtonText(plusBtnObj.transform, "+", defaultFont, 18);

            // Sell All Button [ALL]
            GameObject sellAllBtnObj = new GameObject("SellAllButton");
            sellAllBtnObj.transform.SetParent(qtySectionObj.transform, false);
            RectTransform sellAllRect = sellAllBtnObj.AddComponent<RectTransform>();
            sellAllRect.anchorMin = new Vector2(1f, 0f);
            sellAllRect.anchorMax = new Vector2(1f, 0f);
            sellAllRect.pivot = new Vector2(1f, 0f);
            sellAllRect.anchoredPosition = new Vector2(0f, 10f);
            sellAllRect.sizeDelta = new Vector2(60f, 32f);
            Image sellAllBg = sellAllBtnObj.AddComponent<Image>();
            sellAllBg.color = new Color(0.25f, 0.35f, 0.45f);
            Button sellAllBtn = sellAllBtnObj.AddComponent<Button>();
            CreateButtonText(sellAllBtnObj.transform, "ALL", defaultFont, 13);

            // Quantity Slider (Refined, proportional track & slim handle)
            GameObject sliderObj = new GameObject("QuantitySlider");
            sliderObj.transform.SetParent(qtySectionObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(0f, 0f);
            sliderRect.pivot = new Vector2(0f, 0f);
            sliderRect.anchoredPosition = new Vector2(46f, 19f);
            sliderRect.sizeDelta = new Vector2(190f, 14f);

            Image sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

            // Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform faRect = fillAreaObj.AddComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.offsetMin = new Vector2(4f, 0f);
            faRect.offsetMax = new Vector2(-4f, 0f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.28f, 0.62f, 0.88f);

            // Handle Slide Area
            GameObject handleSlideArea = new GameObject("Handle Slide Area");
            handleSlideArea.transform.SetParent(sliderObj.transform, false);
            RectTransform hsaRect = handleSlideArea.AddComponent<RectTransform>();
            hsaRect.anchorMin = Vector2.zero;
            hsaRect.anchorMax = Vector2.one;
            hsaRect.offsetMin = new Vector2(5f, 0f);
            hsaRect.offsetMax = new Vector2(-5f, 0f);

            // Handle (Slim, sleek knob proportional to track)
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleSlideArea.transform, false);
            RectTransform hRect = handleObj.AddComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(10f, 16f);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = new Color(1.0f, 0.90f, 0.60f);

            Slider qSlider = sliderObj.AddComponent<Slider>();
            qSlider.fillRect = fillRect;
            qSlider.handleRect = hRect;
            qSlider.targetGraphic = handleImg;
            qSlider.minValue = 1;
            qSlider.maxValue = 10;
            qSlider.wholeNumbers = true;

            // Payout Summary & Confirmation (Bottom portion)
            GameObject payoutSummaryObj = new GameObject("PayoutSummary");
            payoutSummaryObj.transform.SetParent(selDetailsObj.transform, false);
            RectTransform psRect = payoutSummaryObj.AddComponent<RectTransform>();
            psRect.anchorMin = new Vector2(0f, 0f);
            psRect.anchorMax = new Vector2(1f, 0f);
            psRect.pivot = new Vector2(0.5f, 0f);
            psRect.anchoredPosition = new Vector2(0f, 10f);
            psRect.sizeDelta = new Vector2(-24f, 115f);

            // Total Payout Display Text
            GameObject payoutObj = new GameObject("TotalPayoutText");
            payoutObj.transform.SetParent(payoutSummaryObj.transform, false);
            RectTransform pRect = payoutObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 1f);
            pRect.anchorMax = new Vector2(1f, 1f);
            pRect.pivot = new Vector2(0.5f, 1f);
            pRect.anchoredPosition = new Vector2(0f, 0f);
            pRect.sizeDelta = new Vector2(0f, 26f);
            Text payoutText = payoutObj.AddComponent<Text>();
            payoutText.font = defaultFont;
            payoutText.fontSize = 17;
            payoutText.fontStyle = FontStyle.Bold;
            payoutText.alignment = TextAnchor.MiddleCenter;
            payoutText.color = new Color(1.0f, 0.88f, 0.35f);
            payoutText.text = "Payout: +45 Coins";

            // Confirm Sell Button
            GameObject sellBtnObj = new GameObject("ConfirmSellButton");
            sellBtnObj.transform.SetParent(payoutSummaryObj.transform, false);
            RectTransform sbRect = sellBtnObj.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0f, 0f);
            sbRect.anchorMax = new Vector2(1f, 0f);
            sbRect.pivot = new Vector2(0.5f, 0f);
            sbRect.anchoredPosition = new Vector2(0f, 32f);
            sbRect.sizeDelta = new Vector2(0f, 42f);
            Image sellBtnBg = sellBtnObj.AddComponent<Image>();
            sellBtnBg.color = new Color(0.18f, 0.55f, 0.28f);
            Button confirmSellBtn = sellBtnObj.AddComponent<Button>();

            GameObject sellBtnTextObj = new GameObject("Text");
            sellBtnTextObj.transform.SetParent(sellBtnObj.transform, false);
            RectTransform sbtRect = sellBtnTextObj.AddComponent<RectTransform>();
            sbtRect.anchorMin = Vector2.zero;
            sbtRect.anchorMax = Vector2.one;
            Text confirmText = sellBtnTextObj.AddComponent<Text>();
            confirmText.font = defaultFont;
            confirmText.fontSize = 15;
            confirmText.fontStyle = FontStyle.Bold;
            confirmText.alignment = TextAnchor.MiddleCenter;
            confirmText.color = Color.white;
            confirmText.text = "SELL 1 TURNIP (+15 G)";

            // Feedback Toast Text
            GameObject feedbackObj = new GameObject("FeedbackToast");
            feedbackObj.transform.SetParent(payoutSummaryObj.transform, false);
            RectTransform fbRect = feedbackObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0f, 0f);
            fbRect.anchorMax = new Vector2(1f, 0f);
            fbRect.pivot = new Vector2(0.5f, 0f);
            fbRect.anchoredPosition = new Vector2(0f, 4f);
            fbRect.sizeDelta = new Vector2(0f, 22f);
            Text fbText = feedbackObj.AddComponent<Text>();
            fbText.font = defaultFont;
            fbText.fontSize = 13;
            fbText.fontStyle = FontStyle.Bold;
            fbText.alignment = TextAnchor.MiddleCenter;
            fbText.color = new Color(0.35f, 0.95f, 0.45f);
            fbText.text = "";

            // Initially hide vendor panel
            vendorModalObj.SetActive(false);

            // Wire VendorUI component on FarmingCanvas
            VendorUI vendorUI = canvasObj.GetComponent<VendorUI>();
            if (vendorUI == null) vendorUI = canvasObj.AddComponent<VendorUI>();

            SerializedObject vSo = new SerializedObject(vendorUI);
            vSo.FindProperty("modalRoot").objectReferenceValue = vendorModalObj;
            vSo.FindProperty("closeButton").objectReferenceValue = vendorCloseBtn;
            vSo.FindProperty("windowTitleText").objectReferenceValue = vtText;
            vSo.FindProperty("playerCoinsText").objectReferenceValue = headerCoinsText;
            vSo.FindProperty("coinIcon").objectReferenceValue = coinIconImg;
            vSo.FindProperty("buyTabButton").objectReferenceValue = buyTabBtn;
            vSo.FindProperty("sellTabButton").objectReferenceValue = sellTabBtn;
            vSo.FindProperty("buyTabBg").objectReferenceValue = buyTabBg;
            vSo.FindProperty("sellTabBg").objectReferenceValue = sellTabBg;
            vSo.FindProperty("buyTabText").objectReferenceValue = buyTabTxt;
            vSo.FindProperty("sellTabText").objectReferenceValue = sellTabTxt;
            vSo.FindProperty("listHeaderTitleText").objectReferenceValue = lhText;
            vSo.FindProperty("restockTimerText").objectReferenceValue = timerText;
            vSo.FindProperty("rerollStockButton").objectReferenceValue = rerollBtn;
            vSo.FindProperty("itemsContainer").objectReferenceValue = rowsContainerObj.transform;
            vSo.FindProperty("emptyStateObject").objectReferenceValue = emptyStateObj;
            vSo.FindProperty("emptyStateText").objectReferenceValue = emptyText;
            vSo.FindProperty("selectionDetailsGroup").objectReferenceValue = selDetailsObj;
            vSo.FindProperty("selectedIcon").objectReferenceValue = selIconImg;
            vSo.FindProperty("selectedNameText").objectReferenceValue = selNameText;
            vSo.FindProperty("selectedRarityText").objectReferenceValue = selRarityText;
            vSo.FindProperty("selectedUnitPriceText").objectReferenceValue = selUnitPriceText;
            vSo.FindProperty("selectedStockOrOwnedText").objectReferenceValue = selOwnedText;
            vSo.FindProperty("minusButton").objectReferenceValue = minusBtn;
            vSo.FindProperty("plusButton").objectReferenceValue = plusBtn;
            vSo.FindProperty("maxQuantityButton").objectReferenceValue = sellAllBtn;
            vSo.FindProperty("quantitySlider").objectReferenceValue = qSlider;
            vSo.FindProperty("quantityDisplayText").objectReferenceValue = qtyDispText;
            vSo.FindProperty("totalCalculationText").objectReferenceValue = payoutText;
            vSo.FindProperty("confirmActionButton").objectReferenceValue = confirmSellBtn;
            vSo.FindProperty("confirmActionButtonText").objectReferenceValue = confirmText;
            vSo.FindProperty("feedbackText").objectReferenceValue = fbText;
            vSo.FindProperty("feedbackDuration").floatValue = 3.0f;
            vSo.ApplyModifiedProperties();
        }

        private static InventorySlotUI CreateSlotUI(
            Transform parent, string name, int slotIndex, bool isHotbar, float size,
            Sprite slotFrame, Sprite slotSelected, Font font)
        {
            GameObject slotObj = new GameObject(name);
            slotObj.transform.SetParent(parent, false);
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(size, size);

            // Slot Background Frame
            Image frameImg = slotObj.AddComponent<Image>();
            frameImg.sprite = slotFrame;
            frameImg.type = Image.Type.Simple;

            Button btn = slotObj.AddComponent<Button>();

            // Item Icon
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
            iconImg.raycastTarget = false;

            // Selection Highlight Frame
            GameObject hlObj = new GameObject("HighlightFrame");
            hlObj.transform.SetParent(slotObj.transform, false);
            RectTransform hlRect = hlObj.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.sizeDelta = new Vector2(6f, 6f);
            Image hlImg = hlObj.AddComponent<Image>();
            hlImg.sprite = slotSelected;
            hlImg.enabled = false;
            hlImg.raycastTarget = false;

            // Rarity Accent Dot (Bottom Left)
            GameObject rarityObj = new GameObject("RarityIndicator");
            rarityObj.transform.SetParent(slotObj.transform, false);
            RectTransform rarityRect = rarityObj.AddComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(0f, 0f);
            rarityRect.anchorMax = new Vector2(0f, 0f);
            rarityRect.pivot = new Vector2(0f, 0f);
            rarityRect.anchoredPosition = new Vector2(4f, 4f);
            rarityRect.sizeDelta = new Vector2(7f, 7f);
            Image rarityImg = rarityObj.AddComponent<Image>();
            rarityImg.color = Color.white;
            rarityImg.enabled = false;
            rarityImg.raycastTarget = false;

            // Hotkey Badge (Top Left)
            GameObject keyObj = new GameObject("HotkeyText");
            keyObj.transform.SetParent(slotObj.transform, false);
            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(4f, -3f);
            keyRect.sizeDelta = new Vector2(18f, 14f);
            Text keyText = keyObj.AddComponent<Text>();
            keyText.font = font;
            keyText.fontSize = Mathf.RoundToInt(size * 0.22f);
            keyText.fontStyle = FontStyle.Bold;
            keyText.alignment = TextAnchor.UpperLeft;
            keyText.color = new Color(0.85f, 0.85f, 0.9f, 0.9f);
            keyText.raycastTarget = false;
            var keyShadow = keyObj.AddComponent<Shadow>();
            keyShadow.effectColor = Color.black;
            keyShadow.effectDistance = new Vector2(1f, -1f);

            // Quantity Badge (Bottom Right)
            GameObject qtyObj = new GameObject("QuantityText");
            qtyObj.transform.SetParent(slotObj.transform, false);
            RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(1f, 0f);
            qtyRect.anchorMax = new Vector2(1f, 0f);
            qtyRect.pivot = new Vector2(1f, 0f);
            qtyRect.anchoredPosition = new Vector2(-3f, 3f);
            qtyRect.sizeDelta = new Vector2(28f, 16f);
            Text qtyText = qtyObj.AddComponent<Text>();
            qtyText.font = font;
            qtyText.fontSize = Mathf.RoundToInt(size * 0.24f);
            qtyText.fontStyle = FontStyle.Bold;
            qtyText.alignment = TextAnchor.LowerRight;
            qtyText.color = Color.white;
            qtyText.raycastTarget = false;
            var qtyOutline = qtyObj.AddComponent<Outline>();
            qtyOutline.effectColor = Color.black;
            qtyOutline.effectDistance = new Vector2(1f, -1f);

            // InventorySlotUI component
            InventorySlotUI slotComp = slotObj.AddComponent<InventorySlotUI>();
            SerializedObject slotSo = new SerializedObject(slotComp);
            slotSo.FindProperty("slotIndex").intValue = slotIndex;
            slotSo.FindProperty("isHotbarSlot").boolValue = isHotbar;
            slotSo.FindProperty("frameImage").objectReferenceValue = frameImg;
            slotSo.FindProperty("iconImage").objectReferenceValue = iconImg;
            slotSo.FindProperty("highlightImage").objectReferenceValue = hlImg;
            slotSo.FindProperty("rarityIndicator").objectReferenceValue = rarityImg;
            slotSo.FindProperty("hotkeyText").objectReferenceValue = keyText;
            slotSo.FindProperty("quantityText").objectReferenceValue = qtyText;
            slotSo.FindProperty("button").objectReferenceValue = btn;
            slotSo.ApplyModifiedProperties();

            return slotComp;
        }

        private static VendorItemRowUI CreateVendorRowUI(
            Transform parent, string name, Sprite slotFrame, Sprite slotSelected, Sprite coinIcon, Font font)
        {
            GameObject rowObj = new GameObject(name);
            rowObj.transform.SetParent(parent, false);
            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(420f, 48f);

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
            Button btn = rowObj.AddComponent<Button>();

            // Selection Highlight Frame
            GameObject hlObj = new GameObject("SelectionHighlight");
            hlObj.transform.SetParent(rowObj.transform, false);
            RectTransform hlRect = hlObj.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.sizeDelta = new Vector2(4f, 4f);
            Image hlImg = hlObj.AddComponent<Image>();
            hlImg.sprite = slotSelected;
            hlImg.enabled = false;
            hlImg.raycastTarget = false;

            // Crop Icon Frame & Image
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(rowObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Crop Name Text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(rowObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(50f, 7f);
            nameRect.sizeDelta = new Vector2(170f, 20f);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // Rarity Badge Text
            GameObject rarityObj = new GameObject("RarityBadge");
            rarityObj.transform.SetParent(rowObj.transform, false);
            RectTransform rarityRect = rarityObj.AddComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(0f, 0.5f);
            rarityRect.anchorMax = new Vector2(0f, 0.5f);
            rarityRect.pivot = new Vector2(0f, 0.5f);
            rarityRect.anchoredPosition = new Vector2(50f, -10f);
            rarityRect.sizeDelta = new Vector2(170f, 16f);
            Text rarityText = rarityObj.AddComponent<Text>();
            rarityText.font = font;
            rarityText.fontSize = 11;
            rarityText.fontStyle = FontStyle.Bold;
            rarityText.color = Color.gray;
            rarityText.raycastTarget = false;

            // Count Text (e.g. "x 5")
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(rowObj.transform, false);
            RectTransform countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1f, 0.5f);
            countRect.anchorMax = new Vector2(1f, 0.5f);
            countRect.pivot = new Vector2(1f, 0.5f);
            countRect.anchoredPosition = new Vector2(-105f, 0f);
            countRect.sizeDelta = new Vector2(60f, 22f);
            Text countText = countObj.AddComponent<Text>();
            countText.font = font;
            countText.fontSize = 13;
            countText.fontStyle = FontStyle.Bold;
            countText.alignment = TextAnchor.MiddleRight;
            countText.color = new Color(0.85f, 0.85f, 0.90f);
            countText.raycastTarget = false;

            // Price Text (e.g. "15 G")
            GameObject priceObj = new GameObject("Price");
            priceObj.transform.SetParent(rowObj.transform, false);
            RectTransform priceRect = priceObj.AddComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(1f, 0.5f);
            priceRect.anchorMax = new Vector2(1f, 0.5f);
            priceRect.pivot = new Vector2(1f, 0.5f);
            priceRect.anchoredPosition = new Vector2(-12f, 0f);
            priceRect.sizeDelta = new Vector2(85f, 22f);
            Text priceText = priceObj.AddComponent<Text>();
            priceText.font = font;
            priceText.fontSize = 14;
            priceText.fontStyle = FontStyle.Bold;
            priceText.alignment = TextAnchor.MiddleRight;
            priceText.color = new Color(1.0f, 0.88f, 0.35f);
            priceText.raycastTarget = false;

            VendorItemRowUI rowComp = rowObj.AddComponent<VendorItemRowUI>();
            SerializedObject rSo = new SerializedObject(rowComp);
            rSo.FindProperty("iconImage").objectReferenceValue = iconImg;
            rSo.FindProperty("nameText").objectReferenceValue = nameText;
            rSo.FindProperty("rarityBadgeText").objectReferenceValue = rarityText;
            rSo.FindProperty("countText").objectReferenceValue = countText;
            rSo.FindProperty("priceText").objectReferenceValue = priceText;
            rSo.FindProperty("selectionHighlight").objectReferenceValue = hlImg;
            rSo.FindProperty("button").objectReferenceValue = btn;
            rSo.ApplyModifiedProperties();

            rowObj.SetActive(false);
            return rowComp;
        }

        private static Text CreateButtonText(Transform parent, string text, Font font, int fontSize)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text t = textObj.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = text;
            return t;
        }
    }
}
#endif
