#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class SceneSetupHelper
{
    private const string INVENTORY_ASSET_PATH = "Assets/_Assets/ScriptableObjects/Inventory.asset";
    private const string FLOOR01_ASSET_PATH = "Assets/_Assets/ScriptableObjects/Floor/Floor_01.asset";

    [MenuItem("Tools/Food Build Setup/1. Setup Base Scene", false, 1)]
    public static void SetupBaseScene()
    {
        // Đảm bảo đang mở scene Base
        if (EditorSceneManager.GetActiveScene().name != "Base")
        {
            if (EditorUtility.DisplayDialog("Mở Scene Base", "Bạn có muốn mở scene Base để cấu hình không?", "Mở Base", "Hủy"))
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/Base.unity");
            }
            else
            {
                return;
            }
        }

        ItemContainer inventory = AssetDatabase.LoadAssetAtPath<ItemContainer>(INVENTORY_ASSET_PATH);
        ItemContainer chestStorage = AssetDatabase.LoadAssetAtPath<ItemContainer>("Assets/_Assets/ScriptableObjects/HomeChest.asset");
        if (chestStorage == null)
        {
            chestStorage = AssetDatabase.LoadAssetAtPath<ItemContainer>("Assets/_Assets/ScriptableObjects/ChestStorage.asset");
        }

        // 1. Setup ProgressionManager
        ProgressionManager progManager = Object.FindFirstObjectByType<ProgressionManager>();
        if (progManager == null)
        {
            GameObject progGO = new GameObject("ProgressionManager");
            progManager = progGO.AddComponent<ProgressionManager>();
            Undo.RegisterCreatedObjectUndo(progGO, "Create ProgressionManager");
            Debug.Log("[Setup] Đã tạo ProgressionManager trong Base scene.");
        }

        // 2. Setup CookingManager
        CookingManager cookManager = Object.FindFirstObjectByType<CookingManager>();
        if (cookManager == null)
        {
            GameObject cookGO = new GameObject("CookingManager");
            cookManager = cookGO.AddComponent<CookingManager>();
            Undo.RegisterCreatedObjectUndo(cookGO, "Create CookingManager");
            Debug.Log("[Setup] Đã tạo CookingManager trong Base scene.");
        }

        // 3. Clean up accidental HomeChest and extra BoxCollider2D on 'Ruong' (the farm field tilemap)
        GameObject ruongGO = GameObject.Find("Ruong");
        if (ruongGO != null)
        {
            HomeChest accidentalChest = ruongGO.GetComponent<HomeChest>();
            if (accidentalChest != null)
            {
                Object.DestroyImmediate(accidentalChest);
                Debug.Log("[Setup] Đã xóa HomeChest nhầm lẫn trên Ruong (Ruộng đất).");
            }
            BoxCollider2D accidentalCol = ruongGO.GetComponent<BoxCollider2D>();
            if (accidentalCol != null)
            {
                Object.DestroyImmediate(accidentalCol);
            }
        }

        // 4. Setup HomeChest on 'Chest_Anim_0' (the visual chest)
        GameObject chestGO = GameObject.Find("Chest_Anim_0");
        if (chestGO == null) chestGO = GameObject.Find("HomeChest");
        if (chestGO == null)
        {
            chestGO = new GameObject("HomeChest");
            Undo.RegisterCreatedObjectUndo(chestGO, "Create HomeChest");
        }

        HomeChest chest = chestGO.GetComponent<HomeChest>();
        if (chest == null)
        {
            chest = chestGO.AddComponent<HomeChest>();
        }

        if (chest != null)
        {
            if (chestStorage != null) chest.chestContainer = chestStorage;
            if (inventory != null) chest.backpackContainer = inventory;
            Debug.Log("[Setup] Đã cấu hình HomeChest (Rương an toàn ở nhà).");
        }

        // 5. Setup Statue & Offerings
        Statue statue = Object.FindFirstObjectByType<Statue>();
        if (statue == null)
        {
            GameObject existingStatueGO = GameObject.Find("Statue");
            if (existingStatueGO != null)
            {
                statue = existingStatueGO.GetComponent<Statue>();
                if (statue == null) statue = existingStatueGO.AddComponent<Statue>();
            }
            else
            {
                GameObject statueGO = new GameObject("Statue");
                statue = statueGO.AddComponent<Statue>();
                Undo.RegisterCreatedObjectUndo(statueGO, "Create Statue");
            }
            Debug.Log("[Setup] Đã gắn component Statue.");
        }

        if (statue != null)
        {
            BoxCollider2D col = statue.GetComponent<BoxCollider2D>();
            if (col == null) col = statue.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(40.5f, 11.5f);
            col.size = new Vector2(4f, 5f);

            if (inventory != null)
            {
                statue.playerInventory = inventory;
            }

            // Gán các combo Lễ Vật (Offerings) từ thư mục Offerings
            string[] guids = AssetDatabase.FindAssets("t:Offering", new[] { "Assets/_Assets/ScriptableObjects/Offerings" });
            statue.offerings = new List<Offering>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Offering off = AssetDatabase.LoadAssetAtPath<Offering>(path);
                if (off != null && !statue.offerings.Contains(off))
                {
                    statue.offerings.Add(off);
                }
            }
            Debug.Log($"[Setup] Đã gán {statue.offerings.Count} combo Lễ Vật cho Tượng.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("✅ Cấu hình Base Scene thành công!");
    }

    [MenuItem("Tools/Food Build Setup/2. Setup Dungeon Scene", false, 2)]
    public static void SetupDungeonScene()
    {
        DungeonSceneSetup.SetupFullDungeonScene();
    }
}
#endif
