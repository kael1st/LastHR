#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class CharacterSetupEditor
{
    private const string CHARACTER_SPRITES_PATH = "Assets/Sprites/Character";

    [MenuItem("Character/Setup Character Sprites & Player")]
    public static void SetupAll()
    {
        ConfigureCharacterImporters();
        AssetDatabase.Refresh();
        ApplyToPlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>[CharacterSetup] Character sprites imported and configured on Player successfully!</color>");
    }

    [MenuItem("Character/Configure Character Texture Importers")]
    public static void ConfigureCharacterImporters()
    {
        if (!Directory.Exists(CHARACTER_SPRITES_PATH))
        {
            Debug.LogError($"Character sprites path does not exist: {CHARACTER_SPRITES_PATH}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CHARACTER_SPRITES_PATH });
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
        Debug.Log($"[CharacterSetup] Configured {guids.Length} character textures at 32 PPU Point filtering.");
    }

    [MenuItem("Character/Apply Character To Player In Scene")]
    public static void ApplyToPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player == null)
        {
            Debug.LogError("No GameObject with tag or name 'Player' found in the active scene!");
            return;
        }

        // 1. Load Character Sprites
        Sprite idleDown = AssetDatabase.LoadAssetAtPath<Sprite>($"{CHARACTER_SPRITES_PATH}/player_idle_down.png");
        Sprite idleUp = AssetDatabase.LoadAssetAtPath<Sprite>($"{CHARACTER_SPRITES_PATH}/player_idle_up.png");
        Sprite idleSide = AssetDatabase.LoadAssetAtPath<Sprite>($"{CHARACTER_SPRITES_PATH}/player_idle_side.png");

        Sprite[] walkDown = new Sprite[4];
        Sprite[] walkUp = new Sprite[4];
        Sprite[] walkSide = new Sprite[4];

        for (int i = 0; i < 4; i++)
        {
            walkDown[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{CHARACTER_SPRITES_PATH}/player_walk_down_{i}.png");
            walkUp[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{CHARACTER_SPRITES_PATH}/player_walk_up_{i}.png");
            walkSide[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{CHARACTER_SPRITES_PATH}/player_walk_side_{i}.png");
        }

        // 2. Configure SpriteRenderer on Player
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Undo.RecordObject(sr, "Configure Player SpriteRenderer");
            sr.sprite = idleDown;
            sr.color = Color.white; // Reset capsule blue tint to pure white
            EditorUtility.SetDirty(sr);
        }

        // 3. Configure PlayerVisuals2D component
        PlayerVisuals2D visuals = player.GetComponent<PlayerVisuals2D>();
        if (visuals == null)
        {
            visuals = Undo.AddComponent<PlayerVisuals2D>(player);
        }

        SerializedObject so = new SerializedObject(visuals);
        so.FindProperty("idleDown").objectReferenceValue = idleDown;
        so.FindProperty("idleUp").objectReferenceValue = idleUp;
        so.FindProperty("idleSide").objectReferenceValue = idleSide;

        SerializedProperty walkDownProp = so.FindProperty("walkDown");
        walkDownProp.arraySize = 4;
        for (int i = 0; i < 4; i++) walkDownProp.GetArrayElementAtIndex(i).objectReferenceValue = walkDown[i];

        SerializedProperty walkUpProp = so.FindProperty("walkUp");
        walkUpProp.arraySize = 4;
        for (int i = 0; i < 4; i++) walkUpProp.GetArrayElementAtIndex(i).objectReferenceValue = walkUp[i];

        SerializedProperty walkSideProp = so.FindProperty("walkSide");
        walkSideProp.arraySize = 4;
        for (int i = 0; i < 4; i++) walkSideProp.GetArrayElementAtIndex(i).objectReferenceValue = walkSide[i];

        so.FindProperty("walkFPS").floatValue = 6f;
        so.FindProperty("sprintFPSMultiplier").floatValue = 1.4f;
        so.FindProperty("flipOnLeft").boolValue = true;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(visuals);

        // 4. Configure TopDownPlayerController
        TopDownPlayerController controller = player.GetComponent<TopDownPlayerController>();
        if (controller != null)
        {
            SerializedObject ctrlSo = new SerializedObject(controller);
            // Disable built-in sprite flip to let PlayerVisuals2D manage it consistently
            ctrlSo.FindProperty("flipSpriteOnLeft").boolValue = false;
            ctrlSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        // 5. Deactivate old FaceIndicator child
        Transform faceIndicator = player.transform.Find("FaceIndicator");
        if (faceIndicator != null)
        {
            Undo.RecordObject(faceIndicator.gameObject, "Hide FaceIndicator");
            faceIndicator.gameObject.SetActive(false);
            EditorUtility.SetDirty(faceIndicator.gameObject);
        }

        // 6. Mark scene dirty and save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=green>[CharacterSetup] Player GameObject updated with new reference character visuals!</color>");
    }
}
#endif
