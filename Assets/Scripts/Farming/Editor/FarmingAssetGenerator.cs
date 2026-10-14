#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmingSystem.Editor
{
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

            Debug.Log("<color=green>[Farming] All sprites and ScriptableObject items generated successfully!</color>");
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

        private static void GenerateSprites()
        {
            // 1. Soil Untilled
            SaveTexture(CreateSoilUntilledTexture(), "soil_untilled.png");

            // 2. Soil Tilled Dry
            SaveTexture(CreateSoilTilledDryTexture(), "soil_tilled_dry.png");

            // 3. Soil Tilled Watered
            SaveTexture(CreateSoilTilledWateredTexture(), "soil_tilled_watered.png");

            // 4. Crop Stages
            SaveTexture(CreateCropStage0Texture(), "crop_stage_0.png");
            SaveTexture(CreateCropStage1Texture(), "crop_stage_1.png");
            SaveTexture(CreateCropStage2Texture(), "crop_stage_2.png");
            SaveTexture(CreateCropStage3Texture(), "crop_stage_3.png");

            // 5. Tools & Items
            SaveTexture(CreateHoeTexture(), "hoe.png");
            SaveTexture(CreateWateringCanTexture(), "watering_can.png");
            SaveTexture(CreateSeedPackTexture(), "seed_pack.png");
            SaveTexture(CreateTurnipItemTexture(), "turnip_item.png");

            // 6. UI Frames & Reticle
            SaveTexture(CreateSlotFrameTexture(), "slot_frame.png");
            SaveTexture(CreateSlotSelectedTexture(), "slot_selected.png");
            SaveTexture(CreateTargetReticleTexture(), "target_reticle.png");
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
                    float distFromEdge = Mathf.Min(x, Mathf.Min(size - 1 - x, Mathf.Min(y, size - 1 - y)));
                    if (distFromEdge == 0)
                    {
                        tex.SetPixel(x, y, borderGrass);
                    }
                    else if (distFromEdge == 1 && ((x + y) % 3 == 0))
                    {
                        tex.SetPixel(x, y, borderGrass);
                    }
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

                    // 4 horizontal furrows
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

                    // Water shimmer droplets
                    if ((x == 8 && y == 16) || (x == 20 && y == 8) || (x == 15 && y == 24) || (x == 24 && y == 22))
                    {
                        c = wetShine;
                    }

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage0Texture()
        {
            // Seed mound with tiny green sprouts
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color mound = new Color(0.40f, 0.25f, 0.15f);
            Color green = new Color(0.35f, 0.85f, 0.25f);
            Color darkGreen = new Color(0.20f, 0.60f, 0.15f);

            // Soil mound in center
            DrawDisc(tex, 16, 12, 6, mound);
            // 2 tiny green sprout tips
            tex.SetPixel(15, 14, green);
            tex.SetPixel(15, 15, green);
            tex.SetPixel(14, 16, green);
            tex.SetPixel(17, 14, green);
            tex.SetPixel(17, 15, green);
            tex.SetPixel(18, 16, darkGreen);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage1Texture()
        {
            // Small twin-leaf sprout
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color stem = new Color(0.30f, 0.75f, 0.20f);
            Color leaf = new Color(0.40f, 0.90f, 0.25f);
            Color highlight = new Color(0.60f, 0.98f, 0.40f);

            // Stem
            DrawLine(tex, 16, 8, 16, 16, stem);

            // Left leaf
            tex.SetPixel(15, 15, leaf);
            tex.SetPixel(14, 16, leaf);
            tex.SetPixel(13, 17, highlight);
            tex.SetPixel(12, 17, leaf);
            tex.SetPixel(13, 16, leaf);

            // Right leaf
            tex.SetPixel(17, 15, leaf);
            tex.SetPixel(18, 16, leaf);
            tex.SetPixel(19, 17, highlight);
            tex.SetPixel(20, 17, leaf);
            tex.SetPixel(19, 16, leaf);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage2Texture()
        {
            // Bushy leafy green plant
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color dark = new Color(0.18f, 0.55f, 0.12f);
            Color mid = new Color(0.32f, 0.78f, 0.22f);
            Color light = new Color(0.55f, 0.92f, 0.35f);

            DrawDisc(tex, 16, 14, 7, dark);
            DrawDisc(tex, 14, 16, 5, mid);
            DrawDisc(tex, 18, 16, 5, mid);
            DrawDisc(tex, 16, 18, 4, light);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCropStage3Texture()
        {
            // Mature ripe turnip bursting out of ground with lush leaves
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);

            Color bulbWhite = new Color(0.95f, 0.95f, 0.90f);
            Color bulbPurple = new Color(0.70f, 0.20f, 0.55f);
            Color bulbShadow = new Color(0.50f, 0.12f, 0.40f);
            Color leafMid = new Color(0.28f, 0.72f, 0.18f);
            Color leafLight = new Color(0.48f, 0.88f, 0.30f);

            // Plump bulb in lower center
            DrawDisc(tex, 16, 11, 7, bulbWhite);

            // Purple top half of turnip
            for (int y = 11; y <= 16; y++)
            {
                for (int x = 11; x <= 21; x++)
                {
                    if (tex.GetPixel(x, y).a > 0.5f)
                    {
                        tex.SetPixel(x, y, y >= 14 ? bulbPurple : Color.Lerp(bulbWhite, bulbPurple, 0.5f));
                    }
                }
            }

            // Turnip bottom outline shadow
            tex.SetPixel(16, 4, bulbShadow);
            tex.SetPixel(15, 5, bulbShadow);
            tex.SetPixel(17, 5, bulbShadow);

            // Big bushy leafy crown on top
            DrawDisc(tex, 16, 22, 6, leafMid);
            DrawDisc(tex, 12, 20, 4, leafMid);
            DrawDisc(tex, 20, 20, 4, leafMid);
            DrawDisc(tex, 16, 24, 4, leafLight);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHoeTexture()
        {
            // Crisp wooden handle with angled steel blade
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color wood = new Color(0.55f, 0.35f, 0.18f);
            Color woodHi = new Color(0.70f, 0.48f, 0.28f);
            Color steel = new Color(0.75f, 0.78f, 0.82f);
            Color steelDark = new Color(0.45f, 0.48f, 0.52f);
            Color steelBlade = new Color(0.92f, 0.95f, 0.98f);

            // Diagonal handle from (6, 6) to (20, 20)
            for (int i = 0; i < 16; i++)
            {
                tex.SetPixel(6 + i, 6 + i, wood);
                tex.SetPixel(5 + i, 6 + i, woodHi);
            }

            // Hoe head connector at (21, 21)
            tex.SetPixel(21, 21, steelDark);
            tex.SetPixel(22, 21, steelDark);
            tex.SetPixel(21, 22, steelDark);

            // Hoe metal blade angled downwards (22, 21) to (26, 14)
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
            // Classic blue vintage watering can
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color canBody = new Color(0.24f, 0.55f, 0.85f);
            Color canHi = new Color(0.45f, 0.72f, 0.95f);
            Color canShadow = new Color(0.14f, 0.38f, 0.65f);
            Color water = new Color(0.70f, 0.90f, 1.0f);

            // Can body (rounded box at x: 10..22, y: 8..18)
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

            // Top arch handle
            for (int x = 11; x <= 21; x++)
            {
                tex.SetPixel(x, 23, canHi);
            }
            tex.SetPixel(11, 22, canBody);
            tex.SetPixel(11, 21, canBody);
            tex.SetPixel(11, 20, canBody);
            tex.SetPixel(11, 19, canBody);
            tex.SetPixel(21, 22, canBody);
            tex.SetPixel(21, 21, canBody);
            tex.SetPixel(21, 20, canBody);
            tex.SetPixel(21, 19, canBody);

            // Spout pointing left & up (from x=10, y=10 to x=3, y=18)
            for (int i = 0; i < 7; i++)
            {
                tex.SetPixel(10 - i, 11 + i, canBody);
                tex.SetPixel(10 - i, 12 + i, canHi);
            }
            // Rose sprinkler head
            tex.SetPixel(3, 17, canShadow);
            tex.SetPixel(3, 18, canShadow);
            tex.SetPixel(3, 19, canShadow);
            tex.SetPixel(2, 18, canShadow);

            // 2 tiny water drops
            tex.SetPixel(1, 16, water);
            tex.SetPixel(1, 13, water);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSeedPackTexture()
        {
            // Paper seed pouch with cute green leaf emblem
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color pouch = new Color(0.85f, 0.72f, 0.52f);
            Color pouchShadow = new Color(0.68f, 0.52f, 0.35f);
            Color stitch = new Color(0.45f, 0.32f, 0.20f);
            Color leaf = new Color(0.25f, 0.75f, 0.30f);

            // Main pouch rectangle (x: 8..23, y: 5..24)
            for (int y = 5; y <= 24; y++)
            {
                for (int x = 8; x <= 23; x++)
                {
                    Color col = pouch;
                    if (x == 8 || x == 23 || y == 5) col = pouchShadow;
                    tex.SetPixel(x, y, col);
                }
            }

            // Folded stitched top
            for (int x = 7; x <= 24; x++)
            {
                tex.SetPixel(x, 25, pouchShadow);
                if (x % 2 == 0) tex.SetPixel(x, 23, stitch);
            }

            // Green leaf in center
            DrawDisc(tex, 16, 14, 4, leaf);
            tex.SetPixel(16, 19, leaf);
            tex.SetPixel(16, 18, leaf);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateTurnipItemTexture()
        {
            // Juicy harvested turnip icon
            int size = 32;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color white = new Color(0.96f, 0.96f, 0.92f);
            Color purple = new Color(0.72f, 0.18f, 0.52f);
            Color green = new Color(0.30f, 0.78f, 0.25f);
            Color greenHi = new Color(0.55f, 0.92f, 0.35f);

            // Bulb
            DrawDisc(tex, 16, 12, 7, white);
            for (int y = 13; y <= 18; y++)
            {
                for (int x = 10; x <= 22; x++)
                {
                    if (tex.GetPixel(x, y).a > 0.5f)
                    {
                        tex.SetPixel(x, y, purple);
                    }
                }
            }
            // Root tail
            tex.SetPixel(16, 5, white);
            tex.SetPixel(16, 4, white);
            tex.SetPixel(15, 3, white);

            // Leaf sprigs
            DrawDisc(tex, 16, 22, 4, green);
            DrawDisc(tex, 12, 20, 3, green);
            DrawDisc(tex, 20, 20, 3, green);
            tex.SetPixel(16, 25, greenHi);

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSlotFrameTexture()
        {
            int size = 48;
            Texture2D tex = CreateTexture(size, size, Color.clear);
            Color bg = new Color(0.12f, 0.12f, 0.14f, 0.85f);
            Color borderDark = new Color(0.22f, 0.22f, 0.26f, 1f);
            Color borderLight = new Color(0.40f, 0.40f, 0.46f, 1f);

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
            Color brightGold = new Color(1.0f, 0.95f, 0.5f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 3px outer border
                    if (x < 3 || y < 3 || x >= size - 3 || y >= size - 3)
                    {
                        Color c = (x < 2 || y >= size - 2) ? brightGold : gold;
                        tex.SetPixel(x, y, c);
                    }
                    // Golden corner notches
                    else if ((x < 6 && (y < 6 || y >= size - 6)) || (x >= size - 6 && (y < 6 || y >= size - 6)))
                    {
                        tex.SetPixel(x, y, new Color(1f, 0.84f, 0f, 0.45f));
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
                // Top-Left
                tex.SetPixel(2 + i, 29, reticleColor);
                tex.SetPixel(2, 29 - i, reticleColor);

                // Top-Right
                tex.SetPixel(29 - i, 29, reticleColor);
                tex.SetPixel(29, 29 - i, reticleColor);

                // Bottom-Left
                tex.SetPixel(2 + i, 2, reticleColor);
                tex.SetPixel(2, 2 + i, reticleColor);

                // Bottom-Right
                tex.SetPixel(29 - i, 2, reticleColor);
                tex.SetPixel(29, 2 + i, reticleColor);
            }

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

        private static void CreateScriptableObjects()
        {
            // Load sprites
            Sprite hoeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/hoe.png");
            Sprite waterCanSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/watering_can.png");
            Sprite seedPackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/seed_pack.png");
            Sprite turnipItemSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/turnip_item.png");

            Sprite stage0 = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/crop_stage_0.png");
            Sprite stage1 = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/crop_stage_1.png");
            Sprite stage2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/crop_stage_2.png");
            Sprite stage3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/crop_stage_3.png");

            // 1. Hoe
            ItemData hoe = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/Hoe.asset");
            hoe.id = "tool_hoe";
            hoe.displayName = "Hoe";
            hoe.description = "Tills raw soil into farmland ready for seeds.";
            hoe.itemType = ItemType.Tool;
            hoe.toolType = ToolType.Hoe;
            hoe.icon = hoeSprite;
            hoe.maxStack = 1;
            EditorUtility.SetDirty(hoe);

            // 2. Watering Can
            ItemData waterCan = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/WateringCan.asset");
            waterCan.id = "tool_watering_can";
            waterCan.displayName = "Watering Can";
            waterCan.description = "Waters tilled soil. Crops need water to grow!";
            waterCan.itemType = ItemType.Tool;
            waterCan.toolType = ToolType.WateringCan;
            waterCan.icon = waterCanSprite;
            waterCan.maxStack = 1;
            EditorUtility.SetDirty(waterCan);

            // 3. Harvest Item: Turnip
            ItemData turnipCropItem = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/Turnip.asset");
            turnipCropItem.id = "crop_turnip";
            turnipCropItem.displayName = "Turnip";
            turnipCropItem.description = "A plump, sweet turnip. Freshly harvested from the garden!";
            turnipCropItem.itemType = ItemType.Crop;
            turnipCropItem.toolType = ToolType.None;
            turnipCropItem.icon = turnipItemSprite;
            turnipCropItem.maxStack = 99;
            EditorUtility.SetDirty(turnipCropItem);

            // 4. Crop Data: Turnip
            CropData turnipCrop = GetOrCreateAsset<CropData>($"{ITEMS_PATH}/TurnipCrop.asset");
            turnipCrop.cropName = "Turnip";
            turnipCrop.harvestItem = turnipCropItem;
            turnipCrop.minHarvestYield = 1;
            turnipCrop.maxHarvestYield = 2;
            turnipCrop.realtimeSecondsPerStage = 6f;
            turnipCrop.stageSprites = new Sprite[] { stage0, stage1, stage2, stage3 };
            turnipCrop.regrows = false;
            EditorUtility.SetDirty(turnipCrop);

            // 5. Seed Pack: Turnip Seeds
            ItemData turnipSeeds = GetOrCreateAsset<ItemData>($"{ITEMS_PATH}/TurnipSeeds.asset");
            turnipSeeds.id = "seeds_turnip";
            turnipSeeds.displayName = "Turnip Seeds";
            turnipSeeds.description = "Plant on tilled soil. Grows in 4 stages when watered.";
            turnipSeeds.itemType = ItemType.Seed;
            turnipSeeds.toolType = ToolType.None;
            turnipSeeds.icon = seedPackSprite;
            turnipSeeds.maxStack = 99;
            turnipSeeds.cropData = turnipCrop;
            EditorUtility.SetDirty(turnipSeeds);
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
