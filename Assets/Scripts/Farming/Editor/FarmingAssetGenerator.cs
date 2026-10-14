#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmingSystem.Editor
{
    /// <summary>
    /// Editor utility to generate pixel-art textures and ScriptableObjects for:
    /// - 4 Rarity Tiers (Common, Rare, Legendary, Mythical)
    /// - 7 Seed & Crop varieties (Turnip, Carrot, Strawberry, Blueberry, Golden Melon, Starfruit, Heavenly Fruit)
    /// - Growth stage sprites for all crops
    /// - Inventory UI icons (Hotbar frames, selection reticle, dedicated backpack inventory button)
    /// </summary>
    public static class FarmingAssetGenerator
    {
        private const string SPRITES_PATH = "Assets/Sprites/Farming";
        private const string ITEMS_PATH = "Assets/ScriptableObjects/Items";

        [MenuItem("Farming/Generate All Farming Assets & Sprites")]
        public static void GenerateAll()
        {
            EnsureDirectories();
            GenerateSprites();
            AssetDatabase.Refresh();
            ConfigureTextureImporters();
            CreateScriptableObjects();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[Farming] All sprites and multi-tier ScriptableObject items generated successfully!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SPRITES_PATH)) Directory.CreateDirectory(SPRITES_PATH);
            if (!Directory.Exists(ITEMS_PATH)) Directory.CreateDirectory(ITEMS_PATH);
        }

        private static void SaveTexture(Texture2D tex, string fileName)
        {
            byte[] bytes = tex.EncodeToPNG();
            string path = Path.Combine(SPRITES_PATH, fileName);
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }

        private static Texture2D CreateTexture(int w, int h, Color clearColor)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = clearColor;
            tex.SetPixels(cols);
            return tex;
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Sprite Generation
        // ─────────────────────────────────────────────────────────────────────────

        private static void GenerateSprites()
        {
            // 1. Soil Tiles
            SaveTexture(CreateSoilUntilledTexture(), "soil_untilled.png");
            SaveTexture(CreateSoilTilledDryTexture(), "soil_tilled_dry.png");
            SaveTexture(CreateSoilTilledWateredTexture(), "soil_tilled_watered.png");

            // 2. Tools & Core Items
            SaveTexture(CreateHoeTexture(), "hoe.png");
            SaveTexture(CreateWateringCanTexture(), "watering_can.png");
            SaveTexture(CreateShovelTexture(), "shovel.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.85f, 0.72f, 0.52f), new Color(0.25f, 0.75f, 0.30f)), "seed_pack.png");
            SaveTexture(CreatePocketWatchTexture(), "pocket_watch.png");

            // 3. UI Assets & Dedicated Inventory Button Icon
            SaveTexture(CreateSlotFrameTexture(), "slot_frame.png");
            SaveTexture(CreateSlotSelectedTexture(), "slot_selected.png");
            SaveTexture(CreateTargetReticleTexture(), "target_reticle.png");
            SaveTexture(CreateInventoryBagIconTexture(), "inventory_bag_icon.png");
            SaveTexture(CreateCoinIconTexture(), "coin_icon.png");
            SaveTexture(CreateVendorStallTexture(), "vendor_stall.png");

            // ── Crop & Seed Variety Sprites ──────────────────────────────────────

            // [COMMON] Turnip
            SaveTexture(CreateTurnipItemTexture(), "turnip_item.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.85f, 0.82f, 0.75f), new Color(0.70f, 0.20f, 0.55f)), "seed_turnip.png");
            SaveTexture(CreateCropStage0Texture(), "crop_stage_0.png");
            SaveTexture(CreateCropStage1Texture(), "crop_stage_1.png");
            SaveTexture(CreateCropStage2Texture(), "crop_stage_2.png");
            SaveTexture(CreateCropStage3Texture(), "crop_stage_3.png");

            // [COMMON] Carrot
            SaveTexture(CreateCarrotItemTexture(), "carrot_item.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.88f, 0.80f, 0.70f), new Color(1.0f, 0.52f, 0.10f)), "seed_carrot.png");
            SaveTexture(CreateCropSproutTexture(new Color(0.35f, 0.85f, 0.25f)), "carrot_stage_0.png");
            SaveTexture(CreateCropTwinLeafTexture(new Color(0.30f, 0.80f, 0.20f)), "carrot_stage_1.png");
            SaveTexture(CreateCropBushyTexture(new Color(0.25f, 0.75f, 0.20f)), "carrot_stage_2.png");
            SaveTexture(CreateCarrotCropStage3Texture(), "carrot_stage_3.png");

            // [RARE] Strawberry
            SaveTexture(CreateStrawberryItemTexture(), "strawberry_item.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.65f, 0.80f, 0.95f), new Color(0.95f, 0.15f, 0.25f)), "seed_strawberry.png");
            SaveTexture(CreateCropSproutTexture(new Color(0.40f, 0.90f, 0.30f)), "strawberry_stage_0.png");
            SaveTexture(CreateCropTwinLeafTexture(new Color(0.35f, 0.85f, 0.25f)), "strawberry_stage_1.png");
            SaveTexture(CreateCropFlowerTexture(new Color(1f, 1f, 0.95f)), "strawberry_stage_2.png");
            SaveTexture(CreateStrawberryCropStage3Texture(), "strawberry_stage_3.png");

            // [RARE] Blueberry
            SaveTexture(CreateBlueberryItemTexture(), "blueberry_item.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.65f, 0.80f, 0.95f), new Color(0.25f, 0.40f, 0.95f)), "seed_blueberry.png");
            SaveTexture(CreateCropSproutTexture(new Color(0.35f, 0.85f, 0.25f)), "blueberry_stage_0.png");
            SaveTexture(CreateCropTwinLeafTexture(new Color(0.30f, 0.80f, 0.20f)), "blueberry_stage_1.png");
            SaveTexture(CreateCropFlowerTexture(new Color(0.75f, 0.85f, 1f)), "blueberry_stage_2.png");
            SaveTexture(CreateBlueberryCropStage3Texture(), "blueberry_stage_3.png");

            // [LEGENDARY] Golden Melon
            SaveTexture(CreateGoldenMelonItemTexture(), "golden_melon_item.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.95f, 0.82f, 0.40f), new Color(1.0f, 0.80f, 0.10f)), "seed_golden_melon.png");
            SaveTexture(CreateCropSproutTexture(new Color(0.60f, 0.85f, 0.20f)), "golden_melon_stage_0.png");
            SaveTexture(CreateCropTwinLeafTexture(new Color(0.55f, 0.80f, 0.20f)), "golden_melon_stage_1.png");
            SaveTexture(CreateGoldenMelonStage2Texture(), "golden_melon_stage_2.png");
            SaveTexture(CreateGoldenMelonStage3Texture(), "golden_melon_stage_3.png");

            // [LEGENDARY] Starfruit
            SaveTexture(CreateStarfruitItemTexture(), "starfruit_item.png");
            SaveTexture(CreateSeedPackTexture(new Color(0.95f, 0.82f, 0.40f), new Color(1.0f, 0.95f, 0.30f)), "seed_starfruit.png");
            SaveTexture(CreateCropSproutTexture(new Color(0.65f, 0.90f, 0.25f)), "starfruit_stage_0.png");
            SaveTexture(CreateCropTwinLeafTexture(new Color(0.60f, 0.85f, 0.25f)), "starfruit_stage_1.png");
            SaveTexture(CreateStarfruitStage2Texture(), "starfruit_stage_2.png");
            SaveTexture(CreateStarfruitStage3Texture(), "starfruit_stage_3.png");

            // [MYTHICAL] Heavenly Seeds & Heavenly Fruit
            SaveTexture(CreateHeavenlyFruitItemTexture(), "heavenly_fruit_item.png");
            SaveTexture(CreateHeavenlySeedsTexture(), "seed_heavenly.png");
            SaveTexture(CreateHeavenlyCropStage0Texture(), "heavenly_stage_0.png");
            SaveTexture(CreateHeavenlyCropStage1Texture(), "heavenly_stage_1.png");
            SaveTexture(CreateHeavenlyCropStage2Texture(), "heavenly_stage_2.png");
            SaveTexture(CreateHeavenlyCropStage3Texture(), "heavenly_stage_3.png");
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  Texture Generators
        // ─────────────────────────────────────────────────────────────────────────

        private static Texture2D CreateSoilUntilledTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color dirtBase = new Color(0.62f, 0.48f, 0.32f);
            Color dirtLight = new Color(0.68f, 0.54f, 0.38f);
            Color dirtDark = new Color(0.50f, 0.38f, 0.24f);
            Color borderGrass = new Color(0.28f, 0.58f, 0.25f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Min(x, Mathf.Min(size - 1 - x, Mathf.Min(y, size - 1 - y)));
                    if (dist == 0 || (dist == 1 && ((x + y) % 3 == 0)))
                        tex.SetPixel(x, y, borderGrass);
                    else
                    {
                        Color col = dirtBase;
                        if ((x * 7 + y * 13) % 7 == 0) col = dirtLight;
                        else if ((x * 11 + y * 5) % 5 == 0) col = dirtDark;
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSoilTilledDryTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color ridgeColor = new Color(0.48f, 0.32f, 0.18f);
            Color furrowDark = new Color(0.30f, 0.18f, 0.10f);
            Color furrowLight = new Color(0.55f, 0.38f, 0.22f);
            Color border = new Color(0.25f, 0.15f, 0.08f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    {
                        tex.SetPixel(x, y, border);
                        continue;
                    }
                    int furrowIndex = (y / 7) % 2;
                    Color c = furrowIndex == 0 ? ridgeColor : furrowDark;
                    if ((y % 7 == 0) && furrowIndex == 0) c = furrowLight;
                    if ((x * 3 + y * 5) % 9 == 0) c = Color.Lerp(c, Color.black, 0.15f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSoilTilledWateredTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color wetDark = new Color(0.18f, 0.12f, 0.08f);
            Color wetRidge = new Color(0.26f, 0.18f, 0.12f);
            Color wetShine = new Color(0.38f, 0.45f, 0.52f, 0.9f);
            Color border = new Color(0.12f, 0.08f, 0.05f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                    {
                        tex.SetPixel(x, y, border);
                        continue;
                    }
                    int furrowIndex = (y / 7) % 2;
                    Color c = furrowIndex == 0 ? wetRidge : wetDark;
                    if ((x == 8 && y == 16) || (x == 20 && y == 8) || (x == 15 && y == 24))
                        c = wetShine;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSeedPackTexture(Color pouchColor, Color emblemColor)
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color pouchShadow = Color.Lerp(pouchColor, Color.black, 0.25f);
            Color stitch = new Color(0.35f, 0.25f, 0.15f);

            for (int y = 5; y <= 24; y++)
            {
                for (int x = 8; x <= 23; x++)
                {
                    Color col = pouchColor;
                    if (x == 8 || x == 23 || y == 5) col = pouchShadow;
                    tex.SetPixel(x, y, col);
                }
            }
            for (int x = 7; x <= 24; x++)
            {
                tex.SetPixel(x, 25, pouchShadow);
                if (x % 2 == 0) tex.SetPixel(x, 23, stitch);
            }
            DrawDisc(tex, 16, 14, 4, emblemColor);
            tex.SetPixel(16, 19, emblemColor);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHoeTexture()
        {
            string path = Path.Combine(SPRITES_PATH, "hoe.png");
            if (File.Exists(path))
            {
                byte[] data = File.ReadAllBytes(path);
                Texture2D loaded = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                loaded.filterMode = FilterMode.Point;
                if (loaded.LoadImage(data)) return loaded;
            }

            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color wood = new Color(0.55f, 0.35f, 0.18f);
            Color woodHi = new Color(0.70f, 0.48f, 0.28f);
            Color steel = new Color(0.75f, 0.78f, 0.82f);
            Color steelDark = new Color(0.45f, 0.48f, 0.52f);
            Color steelBlade = new Color(0.92f, 0.95f, 0.98f);

            for (int i = 0; i < 16; i++)
            {
                tex.SetPixel(6 + i, 6 + i, wood);
                tex.SetPixel(5 + i, 6 + i, woodHi);
            }
            tex.SetPixel(21, 21, steelDark);
            tex.SetPixel(22, 21, steelDark);
            tex.SetPixel(21, 22, steelDark);
            for (int y = 0; y < 8; y++)
            {
                tex.SetPixel(22 + (y / 2), 22 - y, steel);
                tex.SetPixel(23 + (y / 2), 22 - y, steelBlade);
                tex.SetPixel(21 + (y / 2), 22 - y, steelDark);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateWateringCanTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color canBody = new Color(0.24f, 0.55f, 0.85f);
            Color canHi = new Color(0.45f, 0.72f, 0.95f);
            Color canShadow = new Color(0.14f, 0.38f, 0.65f);
            Color water = new Color(0.70f, 0.90f, 1.0f);

            for (int y = 8; y <= 18; y++)
            {
                for (int x = 10; x <= 22; x++)
                {
                    Color col = canBody;
                    if (y >= 16) col = canHi;
                    else if (y <= 10 || x >= 21) col = canShadow;
                    tex.SetPixel(x, y, col);
                }
            }
            for (int x = 11; x <= 21; x++) tex.SetPixel(x, 23, canHi);
            tex.SetPixel(11, 22, canBody);
            tex.SetPixel(11, 21, canBody);
            tex.SetPixel(21, 22, canBody);
            tex.SetPixel(21, 21, canBody);

            for (int i = 0; i < 7; i++)
            {
                tex.SetPixel(10 - i, 11 + i, canBody);
                tex.SetPixel(10 - i, 12 + i, canHi);
            }
            tex.SetPixel(3, 17, canShadow);
            tex.SetPixel(3, 18, canShadow);
            tex.SetPixel(1, 15, water);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateShovelTexture()
        {
            string path = Path.Combine(SPRITES_PATH, "shovel.png");
            if (File.Exists(path))
            {
                byte[] data = File.ReadAllBytes(path);
                Texture2D loaded = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                loaded.filterMode = FilterMode.Point;
                if (loaded.LoadImage(data)) return loaded;
            }

            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color wood = new Color(0.55f, 0.35f, 0.18f);
            Color woodHi = new Color(0.70f, 0.48f, 0.28f);
            Color steel = new Color(0.75f, 0.78f, 0.82f);
            Color steelDark = new Color(0.45f, 0.48f, 0.52f);
            Color steelBlade = new Color(0.92f, 0.95f, 0.98f);

            // Diagonal shaft from lower-left to upper-right
            for (int i = 0; i < 15; i++)
            {
                tex.SetPixel(5 + i, 5 + i, wood);
                tex.SetPixel(4 + i, 5 + i, woodHi);
            }
            // Grip handle at bottom-left
            tex.SetPixel(4, 3, wood);
            tex.SetPixel(3, 4, wood);
            tex.SetPixel(3, 3, woodHi);

            // Shovel spade blade at upper-right
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 4; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 5)
                    {
                        int px = 22 + dx;
                        int py = 22 + dy;
                        Color col = steel;
                        if (dy == 4 || dx == 3) col = steelBlade;
                        else if (dy == -3 || dx == -3) col = steelDark;
                        tex.SetPixel(px, py, col);
                    }
                }
            }
            // Collar connecting shaft to blade
            tex.SetPixel(19, 19, steelDark);
            tex.SetPixel(20, 20, steelDark);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePocketWatchTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color gold = new Color(1.0f, 0.84f, 0.20f);
            Color goldDark = new Color(0.70f, 0.50f, 0.08f);
            Color dialFace = new Color(0.96f, 0.96f, 0.92f);
            Color hand = new Color(0.20f, 0.20f, 0.25f);

            DrawDisc(tex, 16, 14, 11, gold);
            DrawDisc(tex, 16, 14, 8, dialFace);
            // Stem & ring on top
            DrawLine(tex, 16, 25, 16, 28, gold);
            DrawDisc(tex, 16, 28, 3, goldDark);
            tex.SetPixel(16, 28, Color.clear);

            // Clock hands at 10:10
            DrawLine(tex, 16, 14, 12, 17, hand);
            DrawLine(tex, 16, 14, 19, 18, hand);
            tex.SetPixel(16, 14, Color.red);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateInventoryBagIconTexture()
        {
            // Dedicated stylized backpack / leather pouch icon
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color leather = new Color(0.68f, 0.42f, 0.20f);
            Color leatherDark = new Color(0.48f, 0.28f, 0.12f);
            Color leatherHi = new Color(0.82f, 0.55f, 0.28f);
            Color buckle = new Color(1.0f, 0.85f, 0.25f);
            Color strap = new Color(0.35f, 0.20f, 0.08f);

            // Main backpack body (rounded pouch)
            for (int y = 5; y <= 21; y++)
            {
                for (int x = 6; x <= 25; x++)
                {
                    Color c = leather;
                    if (y >= 18) c = leatherHi;
                    else if (y <= 7 || x == 6 || x == 25) c = leatherDark;
                    tex.SetPixel(x, y, c);
                }
            }

            // Top flap
            for (int y = 18; y <= 24; y++)
            {
                for (int x = 7; x <= 24; x++)
                {
                    tex.SetPixel(x, y, leatherHi);
                }
            }

            // Top handle
            for (int x = 12; x <= 19; x++) tex.SetPixel(x, 27, leatherDark);
            tex.SetPixel(12, 26, leatherDark);
            tex.SetPixel(12, 25, leatherDark);
            tex.SetPixel(19, 26, leatherDark);
            tex.SetPixel(19, 25, leatherDark);

            // Vertical straps and gold buckles
            for (int y = 8; y <= 23; y++)
            {
                tex.SetPixel(11, y, strap);
                tex.SetPixel(20, y, strap);
            }
            tex.SetPixel(11, 15, buckle);
            tex.SetPixel(20, 15, buckle);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSlotFrameTexture()
        {
            int size = 48;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color bg = new Color(0.12f, 0.12f, 0.15f, 0.90f);
            Color borderDark = new Color(0.20f, 0.20f, 0.24f, 1f);
            Color borderLight = new Color(0.38f, 0.38f, 0.44f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 2 || y < 2 || x >= size - 2 || y >= size - 2)
                    {
                        Color c = (x < 2 || y >= size - 2) ? borderLight : borderDark;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bg);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSlotSelectedTexture()
        {
            int size = 48;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color gold = new Color(1.0f, 0.84f, 0.0f, 1f);
            Color brightGold = new Color(1.0f, 0.96f, 0.55f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 3 || y < 3 || x >= size - 3 || y >= size - 3)
                    {
                        Color c = (x < 2 || y >= size - 2) ? brightGold : gold;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTargetReticleTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color reticleColor = new Color(1f, 1f, 1f, 0.9f);
            int arm = 6;

            for (int i = 0; i < arm; i++)
            {
                tex.SetPixel(2 + i, 29, reticleColor);
                tex.SetPixel(2, 29 - i, reticleColor);
                tex.SetPixel(29 - i, 29, reticleColor);
                tex.SetPixel(29, 29 - i, reticleColor);
                tex.SetPixel(2 + i, 2, reticleColor);
                tex.SetPixel(2, 2 + i, reticleColor);
                tex.SetPixel(29 - i, 2, reticleColor);
                tex.SetPixel(29, 2 + i, reticleColor);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCoinIconTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color goldRim = new Color(0.85f, 0.60f, 0.05f);
            Color goldFace = new Color(1.0f, 0.84f, 0.15f);
            Color goldShine = new Color(1.0f, 0.96f, 0.55f);
            Color goldShadow = new Color(0.65f, 0.42f, 0.04f);

            DrawDisc(tex, 16, 16, 12, goldShadow);
            DrawDisc(tex, 16, 16, 11, goldRim);
            DrawDisc(tex, 16, 16, 9, goldFace);

            // Inner embossed star emblem
            tex.SetPixel(16, 16, goldShine);
            tex.SetPixel(16, 17, goldShine);
            tex.SetPixel(16, 15, goldShine);
            tex.SetPixel(15, 16, goldShine);
            tex.SetPixel(17, 16, goldShine);
            tex.SetPixel(16, 19, goldShine);
            tex.SetPixel(16, 13, goldShine);
            tex.SetPixel(13, 16, goldShine);
            tex.SetPixel(19, 16, goldShine);

            // Diagonal gleam
            tex.SetPixel(12, 22, Color.white);
            tex.SetPixel(13, 23, Color.white);
            tex.SetPixel(13, 22, goldShine);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateVendorStallTexture()
        {
            int size = 48;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color wood = new Color(0.55f, 0.35f, 0.18f);
            Color woodDark = new Color(0.38f, 0.22f, 0.10f);
            Color woodLight = new Color(0.70f, 0.48f, 0.26f);
            Color redCanopy = new Color(0.85f, 0.20f, 0.22f);
            Color whiteCanopy = new Color(0.95f, 0.95f, 0.92f);
            Color apronBlue = new Color(0.20f, 0.45f, 0.75f);
            Color skin = new Color(0.95f, 0.78f, 0.62f);
            Color hair = new Color(0.30f, 0.20f, 0.12f);

            // 1. Wooden stall base & counter (x: 6..41, y: 4..20)
            for (int y = 4; y <= 20; y++)
            {
                for (int x = 6; x <= 41; x++)
                {
                    Color c = wood;
                    if (y >= 18) c = woodLight; // counter top
                    else if (y == 4 || x == 6 || x == 41) c = woodDark;
                    else if (x % 7 == 0) c = woodDark; // vertical planks
                    tex.SetPixel(x, y, c);
                }
            }

            // 2. Friendly Merchant behind the counter (x: 20..28, y: 19..33)
            for (int y = 20; y <= 26; y++)
            {
                for (int x = 20; x <= 28; x++)
                    tex.SetPixel(x, y, apronBlue);
            }
            // Head / Face
            DrawDisc(tex, 24, 29, 4, skin);
            // Hair & Hat
            for (int x = 20; x <= 28; x++) tex.SetPixel(x, 32, hair);
            for (int x = 21; x <= 27; x++) tex.SetPixel(x, 33, hair);
            // Eyes & Smile
            tex.SetPixel(22, 29, Color.black);
            tex.SetPixel(26, 29, Color.black);
            tex.SetPixel(23, 27, new Color(0.75f, 0.3f, 0.3f));
            tex.SetPixel(24, 27, new Color(0.75f, 0.3f, 0.3f));
            tex.SetPixel(25, 27, new Color(0.75f, 0.3f, 0.3f));

            // 3. Counter Produce Display (Apples, Carrots, Turnips on counter)
            DrawDisc(tex, 12, 22, 3, new Color(0.95f, 0.2f, 0.2f)); // red fruit
            DrawDisc(tex, 35, 22, 3, new Color(1.0f, 0.55f, 0.1f)); // orange carrots
            DrawDisc(tex, 16, 21, 2, new Color(0.95f, 0.85f, 0.3f)); // golden fruit

            // 4. Canopy support poles (left at x: 7, 8; right at x: 39, 40)
            for (int y = 20; y <= 40; y++)
            {
                tex.SetPixel(7, y, woodDark);
                tex.SetPixel(8, y, wood);
                tex.SetPixel(39, y, wood);
                tex.SetPixel(40, y, woodDark);
            }

            // 5. Striped Awning / Canopy (x: 4..43, y: 35..45)
            for (int y = 35; y <= 45; y++)
            {
                for (int x = 4; x <= 43; x++)
                {
                    int stripe = ((x - 4) / 5) % 2;
                    Color c = stripe == 0 ? redCanopy : whiteCanopy;
                    if (y >= 43) c = Color.Lerp(c, Color.black, 0.15f);
                    tex.SetPixel(x, y, c);
                }
            }
            // Scalloped awning bottom hem
            for (int x = 4; x <= 43; x++)
            {
                if (x % 5 == 2) tex.SetPixel(x, 34, redCanopy);
            }

            tex.Apply();
            return tex;
        }

        // ── Item Art Generators ───────────────────────────────────────────────

        private static Texture2D CreateTurnipItemTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color white = new Color(0.96f, 0.96f, 0.92f);
            Color purple = new Color(0.72f, 0.18f, 0.52f);
            Color green = new Color(0.30f, 0.78f, 0.25f);

            DrawDisc(tex, 16, 12, 7, white);
            for (int y = 13; y <= 18; y++)
            {
                for (int x = 10; x <= 22; x++)
                {
                    if (tex.GetPixel(x, y).a > 0.5f) tex.SetPixel(x, y, purple);
                }
            }
            tex.SetPixel(16, 5, white);
            tex.SetPixel(16, 4, white);
            DrawDisc(tex, 16, 22, 4, green);
            DrawDisc(tex, 12, 20, 3, green);
            DrawDisc(tex, 20, 20, 3, green);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCarrotItemTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color orange = new Color(1.0f, 0.50f, 0.05f);
            Color orangeDark = new Color(0.85f, 0.35f, 0.02f);
            Color green = new Color(0.25f, 0.80f, 0.20f);

            // Diagonal tapered carrot root
            for (int i = 0; i < 15; i++)
            {
                int cx = 8 + i;
                int cy = 8 + i;
                int width = Mathf.Clamp(i / 3 + 1, 1, 4);
                for (int w = -width; w <= width; w++)
                {
                    Color c = w > 0 ? orangeDark : orange;
                    tex.SetPixel(cx + w, cy - w, c);
                }
            }
            tex.SetPixel(7, 7, orangeDark);
            // Leafy plume on top right
            DrawLine(tex, 22, 22, 25, 27, green);
            DrawLine(tex, 22, 22, 27, 24, green);
            DrawLine(tex, 22, 22, 23, 28, green);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStrawberryItemTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color red = new Color(0.95f, 0.15f, 0.22f);
            Color redDark = new Color(0.72f, 0.08f, 0.15f);
            Color seedYellow = new Color(1.0f, 0.95f, 0.40f);
            Color calyxGreen = new Color(0.25f, 0.75f, 0.20f);

            // Heart / berry shape
            DrawDisc(tex, 16, 14, 7, red);
            DrawDisc(tex, 14, 16, 5, red);
            DrawDisc(tex, 18, 16, 5, red);
            tex.SetPixel(16, 6, redDark);
            tex.SetPixel(15, 7, redDark);
            tex.SetPixel(17, 7, redDark);

            // Seeds
            tex.SetPixel(14, 15, seedYellow);
            tex.SetPixel(18, 15, seedYellow);
            tex.SetPixel(16, 12, seedYellow);
            tex.SetPixel(13, 11, seedYellow);
            tex.SetPixel(19, 11, seedYellow);
            tex.SetPixel(16, 9, seedYellow);

            // Green leafy cap
            DrawDisc(tex, 16, 21, 3, calyxGreen);
            DrawLine(tex, 16, 21, 16, 26, calyxGreen);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBlueberryItemTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color blue = new Color(0.25f, 0.45f, 0.92f);
            Color blueDark = new Color(0.12f, 0.25f, 0.65f);
            Color bloom = new Color(0.55f, 0.75f, 1.0f);
            Color stem = new Color(0.35f, 0.60f, 0.25f);

            // Cluster of 3 berries
            DrawDisc(tex, 13, 11, 5, blueDark);
            DrawDisc(tex, 19, 12, 5, blue);
            DrawDisc(tex, 15, 17, 5, blue);

            tex.SetPixel(14, 18, bloom);
            tex.SetPixel(18, 13, bloom);
            tex.SetPixel(12, 12, bloom);

            // Calyx crowns on berries
            tex.SetPixel(15, 17, blueDark);
            tex.SetPixel(19, 12, blueDark);

            // Little stem connecting them
            DrawLine(tex, 15, 21, 16, 26, stem);
            DrawLine(tex, 16, 24, 21, 26, stem);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGoldenMelonItemTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color gold = new Color(1.0f, 0.82f, 0.15f);
            Color goldDark = new Color(0.85f, 0.62f, 0.08f);
            Color goldLight = new Color(1.0f, 0.95f, 0.45f);
            Color rib = new Color(0.65f, 0.45f, 0.05f);

            // Large plump cantaloupe melon
            DrawDisc(tex, 16, 14, 9, gold);

            // Longitudinal ribs
            for (int y = 6; y <= 22; y++)
            {
                tex.SetPixel(16, y, rib);
                tex.SetPixel(12, y, rib);
                tex.SetPixel(20, y, rib);
            }
            tex.SetPixel(14, 18, goldLight);
            tex.SetPixel(18, 18, goldLight);

            // Stem curl
            tex.SetPixel(16, 24, new Color(0.4f, 0.7f, 0.2f));
            tex.SetPixel(17, 25, new Color(0.4f, 0.7f, 0.2f));
            tex.SetPixel(18, 24, new Color(0.4f, 0.7f, 0.2f));

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStarfruitItemTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color yellow = new Color(1.0f, 0.92f, 0.15f);
            Color yellowGlow = new Color(1.0f, 1.0f, 0.60f);
            Color amber = new Color(0.90f, 0.65f, 0.08f);

            // Distinct 5-pointed star silhouette
            int cx = 16, cy = 16;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float angle = Mathf.Atan2(dy, dx);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float starR = 9f + 4f * Mathf.Cos(angle * 5f);
                    if (r <= starR)
                    {
                        Color c = yellow;
                        if (r <= 4f) c = yellowGlow;
                        else if (starR - r <= 1.5f) c = amber;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHeavenlyFruitItemTexture()
        {
            // Exceptional Mythical divine celestial peach/lotus with ethereal halo!
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color magentaCore = new Color(0.95f, 0.35f, 0.95f);
            Color radiantPink = new Color(1.0f, 0.65f, 0.90f);
            Color divineGold = new Color(1.0f, 0.90f, 0.30f);
            Color celestialCyan = new Color(0.45f, 0.95f, 1.0f);
            Color auraColor = new Color(0.85f, 0.50f, 1.0f, 0.40f);

            // Outer celestial aura ring
            DrawDisc(tex, 16, 14, 12, auraColor);

            // Divine plump peach bulb
            DrawDisc(tex, 13, 14, 7, magentaCore);
            DrawDisc(tex, 19, 14, 7, magentaCore);
            DrawDisc(tex, 16, 12, 8, radiantPink);

            // Golden starlight highlights in center
            DrawDisc(tex, 16, 14, 3, divineGold);
            tex.SetPixel(16, 14, Color.white);

            // Pointed celestial tip
            tex.SetPixel(16, 22, radiantPink);
            tex.SetPixel(16, 23, celestialCyan);
            tex.SetPixel(16, 24, Color.white);

            // 4 Radiant starlight sparkles
            tex.SetPixel(7, 21, celestialCyan);
            tex.SetPixel(25, 21, celestialCyan);
            tex.SetPixel(8, 7, divineGold);
            tex.SetPixel(24, 7, divineGold);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHeavenlySeedsTexture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color purplePouch = new Color(0.38f, 0.18f, 0.55f);
            Color pouchTrim = new Color(1.0f, 0.85f, 0.30f);
            Color starlight = new Color(0.95f, 0.45f, 1.0f);

            // Pouch body
            for (int y = 5; y <= 24; y++)
            {
                for (int x = 8; x <= 23; x++)
                {
                    Color col = purplePouch;
                    if (x == 8 || x == 23 || y == 5) col = Color.Lerp(purplePouch, Color.black, 0.35f);
                    tex.SetPixel(x, y, col);
                }
            }
            // Gold trim
            for (int x = 7; x <= 24; x++) tex.SetPixel(x, 25, pouchTrim);

            // Glowing celestial star emblem in center
            DrawDisc(tex, 16, 14, 4, starlight);
            tex.SetPixel(16, 14, Color.white);
            tex.SetPixel(16, 19, pouchTrim);
            tex.SetPixel(16, 9, pouchTrim);
            tex.SetPixel(11, 14, pouchTrim);
            tex.SetPixel(21, 14, pouchTrim);

            tex.Apply();
            return tex;
        }

        // ── Crop Stage Texture Generators ────────────────────────────────────

        private static Texture2D CreateCropStage0Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color mound = new Color(0.40f, 0.25f, 0.15f);
            Color green = new Color(0.35f, 0.85f, 0.25f);

            DrawDisc(tex, 16, 12, 6, mound);
            tex.SetPixel(15, 14, green);
            tex.SetPixel(15, 15, green);
            tex.SetPixel(17, 14, green);
            tex.SetPixel(17, 15, green);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage1Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color stem = new Color(0.30f, 0.75f, 0.20f);
            Color leaf = new Color(0.40f, 0.90f, 0.25f);

            DrawLine(tex, 16, 8, 16, 16, stem);
            DrawDisc(tex, 13, 17, 2, leaf);
            DrawDisc(tex, 19, 17, 2, leaf);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage2Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color dark = new Color(0.18f, 0.55f, 0.12f);
            Color mid = new Color(0.32f, 0.78f, 0.22f);

            DrawDisc(tex, 16, 14, 7, dark);
            DrawDisc(tex, 14, 16, 5, mid);
            DrawDisc(tex, 18, 16, 5, mid);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage3Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color bulbWhite = new Color(0.95f, 0.95f, 0.90f);
            Color bulbPurple = new Color(0.70f, 0.20f, 0.55f);
            Color leafMid = new Color(0.28f, 0.72f, 0.18f);

            DrawDisc(tex, 16, 11, 7, bulbWhite);
            for (int y = 11; y <= 16; y++)
            {
                for (int x = 11; x <= 21; x++)
                {
                    if (tex.GetPixel(x, y).a > 0.5f)
                        tex.SetPixel(x, y, y >= 14 ? bulbPurple : Color.Lerp(bulbWhite, bulbPurple, 0.5f));
                }
            }
            DrawDisc(tex, 16, 22, 6, leafMid);
            DrawDisc(tex, 12, 20, 4, leafMid);
            DrawDisc(tex, 20, 20, 4, leafMid);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropSproutTexture(Color green)
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color mound = new Color(0.38f, 0.24f, 0.14f);
            DrawDisc(tex, 16, 10, 5, mound);
            DrawLine(tex, 16, 12, 16, 16, green);
            tex.SetPixel(15, 16, green);
            tex.SetPixel(17, 16, green);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropTwinLeafTexture(Color green)
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            DrawLine(tex, 16, 8, 16, 16, green);
            DrawDisc(tex, 12, 17, 3, green);
            DrawDisc(tex, 20, 17, 3, green);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropBushyTexture(Color green)
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            DrawDisc(tex, 16, 13, 7, green);
            DrawDisc(tex, 12, 16, 5, Color.Lerp(green, Color.yellow, 0.2f));
            DrawDisc(tex, 20, 16, 5, Color.Lerp(green, Color.yellow, 0.2f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropFlowerTexture(Color flowerColor)
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color green = new Color(0.25f, 0.70f, 0.18f);
            DrawDisc(tex, 16, 13, 7, green);
            // Blossoms
            DrawDisc(tex, 12, 17, 2, flowerColor);
            DrawDisc(tex, 20, 16, 2, flowerColor);
            DrawDisc(tex, 16, 19, 2, flowerColor);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCarrotCropStage3Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color orange = new Color(1.0f, 0.52f, 0.10f);
            Color green = new Color(0.25f, 0.80f, 0.20f);

            // Carrot top poking out of ground
            DrawDisc(tex, 16, 10, 5, orange);
            DrawDisc(tex, 16, 18, 5, green);
            DrawDisc(tex, 12, 19, 4, green);
            DrawDisc(tex, 20, 19, 4, green);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStrawberryCropStage3Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color green = new Color(0.22f, 0.65f, 0.18f);
            Color red = new Color(0.95f, 0.15f, 0.25f);

            DrawDisc(tex, 16, 14, 8, green);
            // Multiple ripe strawberries on bush
            DrawDisc(tex, 12, 11, 4, red);
            DrawDisc(tex, 20, 12, 4, red);
            DrawDisc(tex, 16, 8, 3, red);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBlueberryCropStage3Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color green = new Color(0.20f, 0.60f, 0.22f);
            Color blue = new Color(0.25f, 0.45f, 0.95f);

            DrawDisc(tex, 16, 15, 8, green);
            // Clusters of ripe blueberries
            DrawDisc(tex, 12, 13, 3, blue);
            DrawDisc(tex, 15, 11, 3, blue);
            DrawDisc(tex, 19, 14, 3, blue);
            DrawDisc(tex, 21, 11, 3, blue);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGoldenMelonStage2Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color vine = new Color(0.35f, 0.70f, 0.18f);
            Color smallMelon = new Color(0.75f, 0.85f, 0.30f);

            DrawDisc(tex, 16, 14, 7, vine);
            DrawDisc(tex, 16, 11, 5, smallMelon);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGoldenMelonStage3Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color vine = new Color(0.30f, 0.65f, 0.16f);
            Color gold = new Color(1.0f, 0.82f, 0.15f);

            DrawDisc(tex, 16, 16, 6, vine);
            DrawDisc(tex, 16, 11, 8, gold);
            // Cantaloupe ribs
            tex.SetPixel(16, 11, new Color(0.7f, 0.5f, 0.1f));
            tex.SetPixel(13, 11, new Color(0.7f, 0.5f, 0.1f));
            tex.SetPixel(19, 11, new Color(0.7f, 0.5f, 0.1f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStarfruitStage2Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color green = new Color(0.35f, 0.78f, 0.20f);
            Color paleStar = new Color(0.85f, 0.95f, 0.40f);

            DrawDisc(tex, 16, 16, 7, green);
            DrawDisc(tex, 16, 11, 4, paleStar);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStarfruitStage3Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color foliage = new Color(0.28f, 0.68f, 0.20f);
            Color starYellow = new Color(1.0f, 0.95f, 0.25f);

            DrawDisc(tex, 16, 18, 7, foliage);
            DrawDisc(tex, 16, 11, 6, starYellow);
            tex.SetPixel(16, 17, starYellow);
            tex.SetPixel(16, 5, starYellow);
            tex.SetPixel(10, 11, starYellow);
            tex.SetPixel(22, 11, starYellow);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHeavenlyCropStage0Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color aura = new Color(0.85f, 0.45f, 1.0f, 0.35f);
            Color stardustMound = new Color(0.35f, 0.20f, 0.45f);
            Color luminousSprout = new Color(0.55f, 0.95f, 1.0f);

            DrawDisc(tex, 16, 11, 7, aura);
            DrawDisc(tex, 16, 11, 5, stardustMound);
            DrawLine(tex, 16, 12, 16, 17, luminousSprout);
            tex.SetPixel(16, 17, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHeavenlyCropStage1Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color astralStem = new Color(0.40f, 0.85f, 0.95f);
            Color celestialLeaf = new Color(0.85f, 0.50f, 0.98f);

            DrawLine(tex, 16, 8, 16, 17, astralStem);
            DrawDisc(tex, 12, 18, 3, celestialLeaf);
            DrawDisc(tex, 20, 18, 3, celestialLeaf);
            tex.SetPixel(12, 18, Color.white);
            tex.SetPixel(20, 18, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHeavenlyCropStage2Texture()
        {
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color cosmicViolet = new Color(0.50f, 0.20f, 0.70f);
            Color auraBloom = new Color(0.95f, 0.55f, 1.0f);
            Color goldGlow = new Color(1.0f, 0.90f, 0.40f);

            DrawDisc(tex, 16, 14, 8, cosmicViolet);
            DrawDisc(tex, 16, 16, 5, auraBloom);
            DrawDisc(tex, 16, 16, 2, goldGlow);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHeavenlyCropStage3Texture()
        {
            // Fully mature Heavenly Fruit in the garden plot with glorious starlight particles!
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color celestialHalo = new Color(0.90f, 0.50f, 1.0f, 0.45f);
            Color cosmicFoliage = new Color(0.35f, 0.15f, 0.55f);
            Color peachPink = new Color(1.0f, 0.65f, 0.90f);
            Color divineGold = new Color(1.0f, 0.88f, 0.30f);

            // Shimmering halo background
            DrawDisc(tex, 16, 14, 11, celestialHalo);

            // Celestial leaves
            DrawDisc(tex, 16, 19, 6, cosmicFoliage);

            // The divine fruit
            DrawDisc(tex, 16, 12, 7, peachPink);
            DrawDisc(tex, 16, 13, 3, divineGold);
            tex.SetPixel(16, 13, Color.white);

            // Starlight glints
            tex.SetPixel(8, 22, divineGold);
            tex.SetPixel(24, 22, divineGold);
            tex.SetPixel(6, 10, Color.white);
            tex.SetPixel(26, 10, Color.white);

            tex.Apply();
            return tex;
        }

        private static void DrawDisc(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= r2)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < tex.width && y0 >= 0 && y0 < tex.height)
                    tex.SetPixel(x0, y0, color);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void ConfigureTextureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SPRITES_PATH });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        //  ScriptableObject Creation across all 4 Rarity Tiers
        // ─────────────────────────────────────────────────────────────────────────

        private static void CreateScriptableObjects()
        {
            // Core Tools
            Sprite hoeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/hoe.png");
            Sprite waterCanSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/watering_can.png");

            ItemData hoe = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/Hoe.asset");
            hoe.id = "tool_hoe";
            hoe.displayName = "Hoe";
            hoe.description = "Tills raw soil into farmland ready for seeds.";
            hoe.itemType = ItemType.Tool;
            hoe.toolType = ToolType.Hoe;
            hoe.rarity = RarityTier.Common;
            hoe.sellPrice = 50;
            hoe.buyPrice = 100;
            hoe.icon = hoeSprite;
            hoe.maxStack = 1;
            EditorUtility.SetDirty(hoe);

            ItemData waterCan = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/WateringCan.asset");
            waterCan.id = "tool_watering_can";
            waterCan.displayName = "Watering Can";
            waterCan.description = "Waters tilled soil. Crops need water each day to grow!";
            waterCan.itemType = ItemType.Tool;
            waterCan.toolType = ToolType.WateringCan;
            waterCan.rarity = RarityTier.Common;
            waterCan.sellPrice = 50;
            waterCan.buyPrice = 100;
            waterCan.icon = waterCanSprite;
            waterCan.maxStack = 1;
            EditorUtility.SetDirty(waterCan);

            Sprite shovelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/shovel.png");
            ItemData shovel = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/Shovel.asset");
            shovel.id = "tool_shovel";
            shovel.displayName = "Shovel";
            shovel.description = "Digs up and removes planted seeds or crops from a garden plot.";
            shovel.itemType = ItemType.Tool;
            shovel.toolType = ToolType.Shovel;
            shovel.rarity = RarityTier.Common;
            shovel.sellPrice = 25;
            shovel.buyPrice = 50;
            shovel.icon = shovelSprite;
            shovel.maxStack = 1;
            EditorUtility.SetDirty(shovel);

            // ═════════════════════════════════════════════════════════════════════
            //  TIER 1: COMMON CROPS (Basic, low cost, easy to obtain)
            // ═════════════════════════════════════════════════════════════════════

            // 1. Turnip
            CreateCropAndSeedPair(
                cropAsset: "Turnip",
                cropId: "crop_turnip",
                cropDisplayName: "Turnip",
                cropDesc: "A plump, sweet turnip. Easy to grow and a staple food for any homestead.",
                seedAsset: "TurnipSeeds",
                seedId: "seeds_turnip",
                seedDisplayName: "Turnip Seeds",
                seedDesc: "Common seeds. Fast 6s growth cycle (1 day). Easy to harvest.",
                rarity: RarityTier.Common,
                cropSellPrice: 15,
                seedSellPrice: 5,
                seedBuyPrice: 10,
                minYield: 1,
                maxYield: 2,
                secondsPerStage: 6f,
                daysToMature: 1,
                regrows: false,
                regrowStage: 0,
                cropSpriteName: "turnip_item.png",
                seedSpriteName: "seed_turnip.png",
                stageSpriteNames: new[] { "crop_stage_0.png", "crop_stage_1.png", "crop_stage_2.png", "crop_stage_3.png" }
            );

            // 2. Carrot
            CreateCropAndSeedPair(
                cropAsset: "Carrot",
                cropId: "crop_carrot",
                cropDisplayName: "Carrot",
                cropDesc: "A crunchy, crisp orange carrot packed with nutrients.",
                seedAsset: "CarrotSeeds",
                seedId: "seeds_carrot",
                seedDisplayName: "Carrot Seeds",
                seedDesc: "Common seeds. Produces multiple crisp carrots upon maturity.",
                rarity: RarityTier.Common,
                cropSellPrice: 10,
                seedSellPrice: 4,
                seedBuyPrice: 8,
                minYield: 1,
                maxYield: 3,
                secondsPerStage: 7f,
                daysToMature: 1,
                regrows: false,
                regrowStage: 0,
                cropSpriteName: "carrot_item.png",
                seedSpriteName: "seed_carrot.png",
                stageSpriteNames: new[] { "carrot_stage_0.png", "carrot_stage_1.png", "carrot_stage_2.png", "carrot_stage_3.png" }
            );

            // ═════════════════════════════════════════════════════════════════════
            //  TIER 2: RARE CROPS (More valuable, regrowth capability: 20-60 coins)
            // ═════════════════════════════════════════════════════════════════════

            // 3. Strawberry
            CreateCropAndSeedPair(
                cropAsset: "Strawberry",
                cropId: "crop_strawberry",
                cropDisplayName: "Strawberry",
                cropDesc: "A juicy, fragrant sweet strawberry. Produces 2 harvest cycles before the bush is exhausted.",
                seedAsset: "StrawberrySeeds",
                seedId: "seeds_strawberry",
                seedDisplayName: "Strawberry Seeds",
                seedDesc: "Rare seeds. Once mature, yields 2 harvest cycles of sweet berries.",
                rarity: RarityTier.Rare,
                cropSellPrice: 45,
                seedSellPrice: 18,
                seedBuyPrice: 35,
                minYield: 2,
                maxYield: 3,
                secondsPerStage: 10f,
                daysToMature: 2,
                regrows: true,
                regrowStage: 2,
                maxHarvestCycles: 2,
                cropSpriteName: "strawberry_item.png",
                seedSpriteName: "seed_strawberry.png",
                stageSpriteNames: new[] { "strawberry_stage_0.png", "strawberry_stage_1.png", "strawberry_stage_2.png", "strawberry_stage_3.png" }
            );

            // 4. Blueberry
            CreateCropAndSeedPair(
                cropAsset: "Blueberry",
                cropId: "crop_blueberry",
                cropDisplayName: "Blueberry",
                cropDesc: "A rich cluster of frosted indigo blueberries. High market demand.",
                seedAsset: "BlueberrySeeds",
                seedId: "seeds_blueberry",
                seedDisplayName: "Blueberry Seeds",
                seedDesc: "Rare seeds. Produces large multi-berry yields with recurring harvests.",
                rarity: RarityTier.Rare,
                cropSellPrice: 55,
                seedSellPrice: 22,
                seedBuyPrice: 45,
                minYield: 2,
                maxYield: 4,
                secondsPerStage: 12f,
                daysToMature: 2,
                regrows: true,
                regrowStage: 2,
                cropSpriteName: "blueberry_item.png",
                seedSpriteName: "seed_blueberry.png",
                stageSpriteNames: new[] { "blueberry_stage_0.png", "blueberry_stage_1.png", "blueberry_stage_2.png", "blueberry_stage_3.png" }
            );

            // ═════════════════════════════════════════════════════════════════════
            //  TIER 3: LEGENDARY CROPS (High-value, prized luxury crops: 100-300 coins)
            // ═════════════════════════════════════════════════════════════════════

            // 5. Golden Melon
            CreateCropAndSeedPair(
                cropAsset: "GoldenMelon",
                cropId: "crop_golden_melon",
                cropDisplayName: "Golden Melon",
                cropDesc: "A prized heavy melon with a shimmering golden rind. Commands a handsome fortune.",
                seedAsset: "GoldenMelonSeeds",
                seedId: "seeds_golden_melon",
                seedDisplayName: "Golden Melon Seeds",
                seedDesc: "Legendary seeds. Takes patience to cultivate, yielding lucrative golden melons.",
                rarity: RarityTier.Legendary,
                cropSellPrice: 220,
                seedSellPrice: 75,
                seedBuyPrice: 150,
                minYield: 1,
                maxYield: 2,
                secondsPerStage: 18f,
                daysToMature: 3,
                regrows: false,
                regrowStage: 0,
                cropSpriteName: "golden_melon_item.png",
                seedSpriteName: "seed_golden_melon.png",
                stageSpriteNames: new[] { "golden_melon_stage_0.png", "golden_melon_stage_1.png", "golden_melon_stage_2.png", "golden_melon_stage_3.png" }
            );

            // 6. Starfruit
            CreateCropAndSeedPair(
                cropAsset: "Starfruit",
                cropId: "crop_starfruit",
                cropDisplayName: "Starfruit",
                cropDesc: "A radiant star-shaped delicacy that glows with faint celestial luminescence.",
                seedAsset: "StarfruitSeeds",
                seedId: "seeds_starfruit",
                seedDisplayName: "Starfruit Seeds",
                seedDesc: "Legendary seeds. Absorbs starlight to produce exquisite five-pointed fruit.",
                rarity: RarityTier.Legendary,
                cropSellPrice: 280,
                seedSellPrice: 95,
                seedBuyPrice: 190,
                minYield: 1,
                maxYield: 2,
                secondsPerStage: 22f,
                daysToMature: 3,
                regrows: false,
                regrowStage: 0,
                cropSpriteName: "starfruit_item.png",
                seedSpriteName: "seed_starfruit.png",
                stageSpriteNames: new[] { "starfruit_stage_0.png", "starfruit_stage_1.png", "starfruit_stage_2.png", "starfruit_stage_3.png" }
            );

            // ═════════════════════════════════════════════════════════════════════
            //  TIER 4: MYTHICAL CROPS (Extremely rare, unique Heavenly Seeds)
            // ═════════════════════════════════════════════════════════════════════

            // 7. Heavenly Fruit & Heavenly Seeds
            CreateCropAndSeedPair(
                cropAsset: "HeavenlyFruit",
                cropId: "crop_heavenly_fruit",
                cropDisplayName: "Heavenly Fruit",
                cropDesc: "A transcendent celestial fruit said to blossom once every celestial epoch. Radiates divine aura and yields immense fortune.",
                seedAsset: "HeavenlySeeds",
                seedId: "seeds_heavenly",
                seedDisplayName: "Heavenly Seeds",
                seedDesc: "Mythical divine seeds shimmering with ethereal starlight. Capable of bearing the legendary Heavenly Fruit.",
                rarity: RarityTier.Mythical,
                cropSellPrice: 1200,
                seedSellPrice: 450,
                seedBuyPrice: 900,
                minYield: 1,
                maxYield: 2,
                secondsPerStage: 28f,
                daysToMature: 4,
                regrows: false,
                regrowStage: 0,
                cropSpriteName: "heavenly_fruit_item.png",
                seedSpriteName: "seed_heavenly.png",
                stageSpriteNames: new[] { "heavenly_stage_0.png", "heavenly_stage_1.png", "heavenly_stage_2.png", "heavenly_stage_3.png" }
            );
        }

        private static void CreateCropAndSeedPair(
            string cropAsset, string cropId, string cropDisplayName, string cropDesc,
            string seedAsset, string seedId, string seedDisplayName, string seedDesc,
            RarityTier rarity, int cropSellPrice, int seedSellPrice, int seedBuyPrice,
            int minYield, int maxYield, float secondsPerStage, int daysToMature,
            bool regrows, int regrowStage,
            string cropSpriteName, string seedSpriteName, string[] stageSpriteNames,
            int maxHarvestCycles = 0)
        {
            Sprite cropSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/{cropSpriteName}");
            Sprite seedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/{seedSpriteName}");

            Sprite[] stageSprites = new Sprite[stageSpriteNames.Length];
            for (int i = 0; i < stageSpriteNames.Length; i++)
            {
                stageSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/{stageSpriteNames[i]}");
            }

            // 1. Harvest Item
            ItemData harvestItem = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/{cropAsset}.asset");
            harvestItem.id = cropId;
            harvestItem.displayName = cropDisplayName;
            harvestItem.description = cropDesc;
            harvestItem.itemType = ItemType.Crop;
            harvestItem.toolType = ToolType.None;
            harvestItem.rarity = rarity;
            harvestItem.sellPrice = cropSellPrice;
            harvestItem.buyPrice = cropSellPrice * 2;
            harvestItem.icon = cropSprite;
            harvestItem.maxStack = 99;
            EditorUtility.SetDirty(harvestItem);

            // 2. Crop Data
            CropData cropData = GetOrCreateAsset<CropData>($"{ITEMS_PATH}/{cropAsset}Crop.asset");
            cropData.cropName = cropDisplayName;
            cropData.rarity = rarity;
            cropData.harvestItem = harvestItem;
            cropData.minHarvestYield = minYield;
            cropData.maxHarvestYield = maxYield;
            cropData.realtimeSecondsPerStage = secondsPerStage;
            cropData.daysToMature = daysToMature;
            cropData.stageSprites = stageSprites;
            cropData.regrows = regrows;
            cropData.regrowStage = regrowStage;
            cropData.maxHarvestCycles = maxHarvestCycles;
            EditorUtility.SetDirty(cropData);

            // 3. Seed Pack Item
            ItemData seedItem = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/{seedAsset}.asset");
            seedItem.id = seedId;
            seedItem.displayName = seedDisplayName;
            seedItem.description = seedDesc;
            seedItem.itemType = ItemType.Seed;
            seedItem.toolType = ToolType.None;
            seedItem.rarity = rarity;
            seedItem.sellPrice = seedSellPrice;
            seedItem.buyPrice = seedBuyPrice;
            seedItem.icon = seedSprite;
            seedItem.maxStack = 99;
            seedItem.cropData = cropData;
            EditorUtility.SetDirty(seedItem);
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }
    }
}
#endif
