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
        if (EditorSceneManager.GetActiveScene().name != "Dungeon")
        {
            if (EditorUtility.DisplayDialog("Mở Scene Dungeon", "Bạn có muốn mở scene Dungeon để cấu hình không?", "Mở Dungeon", "Hủy"))
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/Dungeon.unity");
            }
            else
            {
                return;
            }
        }

        // 1. Tạo DungeonManager & các hệ thống combat
        GameObject dmGO = GameObject.Find("DungeonManager");
        if (dmGO == null)
        {
            dmGO = new GameObject("DungeonManager");
            Undo.RegisterCreatedObjectUndo(dmGO, "Create DungeonManager");
        }

        WaveManager waveManager = dmGO.GetComponent<WaveManager>();
        if (waveManager == null) waveManager = dmGO.AddComponent<WaveManager>();

        CombatManager combatManager = dmGO.GetComponent<CombatManager>();
        if (combatManager == null) combatManager = dmGO.AddComponent<CombatManager>();

        TurnManager turnManager = dmGO.GetComponent<TurnManager>();
        if (turnManager == null) turnManager = dmGO.AddComponent<TurnManager>();

        SpeedManager speedManager = dmGO.GetComponent<SpeedManager>();
        if (speedManager == null) speedManager = dmGO.AddComponent<SpeedManager>();

        EnemyTargetSelector targetSelector = dmGO.GetComponent<EnemyTargetSelector>();
        if (targetSelector == null) targetSelector = dmGO.AddComponent<EnemyTargetSelector>();

        HealthBarManager healthBarManager = dmGO.GetComponent<HealthBarManager>();
        if (healthBarManager == null) healthBarManager = dmGO.AddComponent<HealthBarManager>();

        FloorGenerator floorGenerator = dmGO.GetComponent<FloorGenerator>();
        if (floorGenerator == null) floorGenerator = dmGO.AddComponent<FloorGenerator>();

        // 2. Tạo Camp
        GameObject campGO = GameObject.Find("Camp");
        if (campGO == null)
        {
            campGO = new GameObject("Camp");
            campGO.transform.SetParent(dmGO.transform);
        }
        Camp camp = campGO.GetComponent<Camp>();
        if (camp == null) camp = campGO.AddComponent<Camp>();

        // 3. Tạo MerchantEvent
        GameObject merchantGO = GameObject.Find("MerchantEvent");
        if (merchantGO == null)
        {
            merchantGO = new GameObject("MerchantEvent");
            merchantGO.transform.SetParent(dmGO.transform);
        }
        MerchantEvent merchantEvent = merchantGO.GetComponent<MerchantEvent>();
        if (merchantEvent == null) merchantEvent = merchantGO.AddComponent<MerchantEvent>();

        // 4. Tạo Spawn Point & Combat Positions
        GameObject spawnPointGO = GameObject.Find("EnemySpawnPoint");
        if (spawnPointGO == null)
        {
            spawnPointGO = new GameObject("EnemySpawnPoint");
            spawnPointGO.transform.position = new Vector3(8f, 0f, 0f);
            spawnPointGO.transform.SetParent(dmGO.transform);
        }

        GameObject posRoot = GameObject.Find("CombatPositions");
        if (posRoot == null)
        {
            posRoot = new GameObject("CombatPositions");
            posRoot.transform.SetParent(dmGO.transform);
        }

        Transform[] combatPositions = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            string posName = $"Position_{i + 1}";
            Transform pos = posRoot.transform.Find(posName);
            if (pos == null)
            {
                GameObject pGO = new GameObject(posName);
                pGO.transform.SetParent(posRoot.transform);
                pGO.transform.position = new Vector3(3f + (i * 2f), 0f, 0f);
                pos = pGO.transform;
            }
            combatPositions[i] = pos;
        }

        // 5. Kết nối các tham chiếu bằng SerializedObject
        SerializedObject soWave = new SerializedObject(waveManager);
        soWave.FindProperty("enemySpawnPoint").objectReferenceValue = spawnPointGO.transform;
        soWave.FindProperty("combatManager").objectReferenceValue = combatManager;
        soWave.FindProperty("healthBarManager").objectReferenceValue = healthBarManager;
        soWave.FindProperty("targetSelector").objectReferenceValue = targetSelector;
        soWave.FindProperty("camp").objectReferenceValue = camp;
        soWave.FindProperty("merchantEvent").objectReferenceValue = merchantEvent;
        soWave.FindProperty("floorGenerator").objectReferenceValue = floorGenerator;

        SerializedProperty propPositions = soWave.FindProperty("combatPositions");
        propPositions.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            propPositions.GetArrayElementAtIndex(i).objectReferenceValue = combatPositions[i];
        }

        FloorData floor01 = AssetDatabase.LoadAssetAtPath<FloorData>(FLOOR01_ASSET_PATH);
        if (floor01 != null)
        {
            soWave.FindProperty("floorData").objectReferenceValue = floor01;
        }
        soWave.ApplyModifiedProperties();

        SerializedObject soCombat = new SerializedObject(combatManager);
        soCombat.FindProperty("targetSelector").objectReferenceValue = targetSelector;
        soCombat.FindProperty("speedManager").objectReferenceValue = speedManager;
        soCombat.FindProperty("turnManager").objectReferenceValue = turnManager;
        soCombat.FindProperty("healthBarManager").objectReferenceValue = healthBarManager;
        soCombat.FindProperty("waveManager").objectReferenceValue = waveManager;
        soCombat.ApplyModifiedProperties();

        SerializedObject soTurn = new SerializedObject(turnManager);
        soTurn.FindProperty("speedManager").objectReferenceValue = speedManager;
        soTurn.FindProperty("combatManager").objectReferenceValue = combatManager;
        soTurn.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("✅ Cấu hình Dungeon Scene thành công với đầy đủ hệ thống quản lý Combat, Camp, Merchant, Wave!");
    }
}
#endif
