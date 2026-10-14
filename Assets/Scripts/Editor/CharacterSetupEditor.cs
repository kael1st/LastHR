#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility to configure the character sprites and wire them
/// up to the Player GameObject's PlayerVisuals2D component.
/// Accessible via the "Character" top menu.
/// </summary>
public static class CharacterSetupEditor
{
    private const string SPRITES_PATH = "Assets/Sprites/Character";

    // ------------------------------------------------------------------
    // Menu Items
    // ------------------------------------------------------------------
    [MenuItem("Character/1 – Configure Texture Importers")]
    public static void ConfigureTextureImporters()
    {
        if (!Directory.Exists(SPRITES_PATH))
        {
            Debug.LogError($"[CharacterSetup] Sprites folder not found: {SPRITES_PATH}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SPRITES_PATH });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType          = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit  = 32;
                importer.filterMode           = FilterMode.Point;
                importer.textureCompression   = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                count++;
            }
        }
        Debug.Log($"<color=cyan>[CharacterSetup] Configured {count} character textures (32 PPU, Point filter).</color>");
    }

    [MenuItem("Character/2 – Apply Character To Player")]
    public static void ApplyToPlayer()
    {
        // 1. Find Player
        GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[CharacterSetup] No GameObject with tag 'Player' found in the active scene!");
            return;
        }

        // 2. Load Idle Sprites
        Sprite idleDown  = Load("player_idle_down.png");
        Sprite idleUp    = Load("player_idle_up.png");
        Sprite idleRight = Load("player_idle_side.png");  // Right-facing (side)
        Sprite idleLeft  = Load("player_idle_left.png");  // Left-facing (mirrored)

        // 3. Load Walk Cycles
        Sprite[] walkDown  = LoadFrames("player_walk_down_{0}.png",  4);
        Sprite[] walkUp    = LoadFrames("player_walk_up_{0}.png",    4);
        Sprite[] walkRight = LoadFrames("player_walk_side_{0}.png",  4);  // Right walk
        Sprite[] walkLeft  = LoadFrames("player_walk_left_{0}.png",  4);  // Left walk

        // 4. SpriteRenderer – reset colour tint from capsule placeholder
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Undo.RecordObject(sr, "Reset Player SpriteRenderer");
            sr.sprite = idleDown;
            sr.color  = Color.white;
            EditorUtility.SetDirty(sr);
        }

        // 5. Add / configure PlayerVisuals2D
        PlayerVisuals2D visuals = player.GetComponent<PlayerVisuals2D>();
        if (visuals == null)
            visuals = Undo.AddComponent<PlayerVisuals2D>(player);

        SerializedObject so = new SerializedObject(visuals);

        // Idle sprites
        so.FindProperty("idleDown").objectReferenceValue  = idleDown;
        so.FindProperty("idleUp").objectReferenceValue    = idleUp;
        so.FindProperty("idleRight").objectReferenceValue = idleRight;
        so.FindProperty("idleLeft").objectReferenceValue  = idleLeft;

        // Walk cycles
        AssignSpriteArray(so, "walkDown",  walkDown);
        AssignSpriteArray(so, "walkUp",    walkUp);
        AssignSpriteArray(so, "walkRight", walkRight);
        AssignSpriteArray(so, "walkLeft",  walkLeft);

        // Animation settings
        so.FindProperty("walkFPS").floatValue              = 6f;
        so.FindProperty("sprintFPSMultiplier").floatValue  = 1.5f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(visuals);

        // 6. Disable built-in sprite flipper on TopDownPlayerController
        TopDownPlayerController ctrl = player.GetComponent<TopDownPlayerController>();
        if (ctrl != null)
        {
            SerializedObject ctrlSo = new SerializedObject(ctrl);
            ctrlSo.FindProperty("flipSpriteOnLeft").boolValue = false;
            ctrlSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);
        }

        // 7. Hide old FaceIndicator child (replaced by directional sprites)
        Transform fi = player.transform.Find("FaceIndicator");
        if (fi != null)
        {
            Undo.RecordObject(fi.gameObject, "Hide FaceIndicator");
            fi.gameObject.SetActive(false);
            EditorUtility.SetDirty(fi.gameObject);
        }

        // 8. Mark scene dirty & save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=green>[CharacterSetup] Player updated with character sprites + animations!</color>");
    }

    [MenuItem("Character/3 – Full Setup (Import + Apply)")]
    public static void FullSetup()
    {
        ConfigureTextureImporters();
        AssetDatabase.Refresh();
        ApplyToPlayer();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private static Sprite Load(string filename)
    {
        string path = $"{SPRITES_PATH}/{filename}";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"[CharacterSetup] Sprite not found: {path}");
        return s;
    }

    private static Sprite[] LoadFrames(string pattern, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = Load(string.Format(pattern, i));
        return frames;
    }

    private static void AssignSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
    {
        SerializedProperty prop = so.FindProperty(propName);
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
#endif
