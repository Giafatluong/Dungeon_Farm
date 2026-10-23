using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Editor utility that repairs the scene hierarchy, fixes camera depth & tracking,
/// ensures the player is visible and operational, and cleanly configures all Canvases in Base.unity.
/// </summary>
public static class SceneCanvasSetup
{
    private const string BASE_SCENE_PATH = "Assets/_Scenes/Base.unity";

    [MenuItem("Tools/Setup Scene Canvases and Fix Player")]
    public static void FixSceneAndCanvases()
    {
        Debug.Log("[SceneCanvasSetup] Starting scene repair and canvas configuration...");

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != BASE_SCENE_PATH)
        {
            activeScene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
        }

        // 1. Unparent children from any arbitrary "Manager" GameObject and destroy it
        GameObject managerGO = GameObject.Find("Manager");
        if (managerGO != null)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < managerGO.transform.childCount; i++)
            {
                children.Add(managerGO.transform.GetChild(i));
            }
            foreach (var ch in children)
            {
                ch.SetParent(null);
            }
            Object.DestroyImmediate(managerGO);
            Debug.Log("[SceneCanvasSetup] Unparented children from Manager and removed Manager object.");
        }

        // 2. Locate and Fix Player
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) playerGO = GameObject.Find("Player");

        if (playerGO != null)
        {
            playerGO.SetActive(true);
            playerGO.transform.SetParent(null);

            // Ensure valid coordinates
            Vector3 pPos = playerGO.transform.position;
            if (float.IsNaN(pPos.x) || float.IsNaN(pPos.y) || float.IsNaN(pPos.z))
            {
                playerGO.transform.position = new Vector3(8.02f, -5.65f, 0f);
            }
            else
            {
                playerGO.transform.position = new Vector3(pPos.x, pPos.y, 0f);
            }
            playerGO.transform.localScale = Vector3.one;

            // SpriteRenderer
            SpriteRenderer sr = playerGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 3;
                sr.color = Color.white;
            }

            // Rigidbody2D
            Rigidbody2D rb = playerGO.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            // PlayerMovement
            PlayerMovement pm = playerGO.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = true;
                pm.speed = 5f;
            }

            // PlayerStats
            PlayerStats ps = playerGO.GetComponent<PlayerStats>();
            if (ps != null)
            {
                ps.enabled = true;
                ps.ResolveItemContainer();
            }

            Debug.Log($"[SceneCanvasSetup] Player validated at position {playerGO.transform.position}");
        }
        else
        {
            Debug.LogError("[SceneCanvasSetup] Player GameObject not found in Base.unity!");
        }

        // 3. Locate and Fix Main Camera & CameraController
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = GameObject.FindWithTag("MainCamera");
            if (camGO == null) camGO = GameObject.Find("Main Camera");
            if (camGO != null) mainCam = camGO.GetComponent<Camera>();
        }

        if (mainCam != null)
        {
            GameObject camGO = mainCam.gameObject;
            camGO.SetActive(true);
            camGO.transform.SetParent(null);
            camGO.tag = "MainCamera";

            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 1000f;
            mainCam.cullingMask = -1;

            Vector3 targetCamPos = playerGO != null
                ? new Vector3(playerGO.transform.position.x, playerGO.transform.position.y + 1.5f, -10f)
                : new Vector3(0f, 0f, -10f);

            camGO.transform.position = targetCamPos;
            camGO.transform.rotation = Quaternion.identity;

            CameraController cc = camGO.GetComponent<CameraController>();
            if (cc == null) cc = camGO.AddComponent<CameraController>();
            cc.player = playerGO;
            cc.offset = new Vector3(0f, 1.5f, -10f);

            Debug.Log($"[SceneCanvasSetup] Main Camera configured at {camGO.transform.position} with offset (0, 1.5, -10).");
        }

        // 4. Clean up root managers (DayManager, CookingManager, SceneTransitionManager, EventSystem)
        EnsureRootObject("DayManager", Vector3.zero);
        EnsureRootObject("CookingManager", Vector3.zero);
        EnsureRootObject("SceneTransManager", Vector3.zero);
        EnsureRootObject("ProgressionManager", Vector3.zero);

        // Ensure EventSystem
        UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            esGO.transform.SetParent(null);
            Debug.Log("[SceneCanvasSetup] Created missing EventSystem.");
        }
        else
        {
            es.transform.SetParent(null);
            if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // 5. Setup Canvas 1: Main HUD Canvas ("Canvas")
        GameObject hudCanvasGO = GameObject.Find("Canvas");
        if (hudCanvasGO != null)
        {
            hudCanvasGO.transform.SetParent(null);
            Canvas hudCanvas = hudCanvasGO.GetComponent<Canvas>();
            if (hudCanvas != null)
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = 0;
            }

            CanvasScaler scaler = hudCanvasGO.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            GraphicRaycaster gr = hudCanvasGO.GetComponent<GraphicRaycaster>();
            if (gr != null) gr.enabled = true;

            // Configure HUD Panels
            Transform curItemT = hudCanvasGO.transform.Find("CurrentItem");
            if (curItemT != null) curItemT.gameObject.SetActive(true);

            Transform invPanelT = hudCanvasGO.transform.Find("inventoryPanel");
            if (invPanelT != null) invPanelT.gameObject.SetActive(false);

            Transform chestPanelT = hudCanvasGO.transform.Find("ChestPanel");
            if (chestPanelT != null) chestPanelT.gameObject.SetActive(false);

            Transform enterDungeonT = hudCanvasGO.transform.Find("EnterDungeonPanel");
            if (enterDungeonT != null) enterDungeonT.gameObject.SetActive(false);

            Transform nextDayT = hudCanvasGO.transform.Find("NextDayCanvas");
            if (nextDayT != null) nextDayT.gameObject.SetActive(false);

            // Configure InventoryController
            InventoryController ic = hudCanvasGO.GetComponent<InventoryController>();
            if (ic != null)
            {
                ic.isInteractable = true;
                if (invPanelT != null)
                {
                    SerializedObject so = new SerializedObject(ic);
                    so.FindProperty("inventoryPanel").objectReferenceValue = invPanelT.GetComponent<InventoryPanel>();
                    if (curItemT != null)
                    {
                        so.FindProperty("currentItemPanel").objectReferenceValue = curItemT.GetComponent<CurrentItemPanel>();
                    }
                    so.FindProperty("isInteractable").boolValue = true;
                    so.ApplyModifiedProperties();
                }
            }

            // Unparent DungeonEnterUI if it's placed inside Canvas
            Transform dungeonEnterT = hudCanvasGO.transform.Find("DungeonEnterUI");
            if (dungeonEnterT != null)
            {
                dungeonEnterT.SetParent(null);
                Debug.Log("[SceneCanvasSetup] Unparented DungeonEnterUI from Canvas to Root.");
            }

            Debug.Log("[SceneCanvasSetup] Main HUD Canvas configured (ScreenSpaceOverlay, 1920x1080).");
        }

        // 6. Setup Canvas 2: World Space HealthBar Canvas ("HealthBarCanvas")
        GameObject hbCanvasGO = GameObject.Find("HealthBarcanvas");
        if (hbCanvasGO == null) hbCanvasGO = GameObject.Find("HealthBarCanvas");

        if (hbCanvasGO != null)
        {
            hbCanvasGO.name = "HealthBarCanvas";
            hbCanvasGO.transform.SetParent(null); // Must NEVER be nested under ScreenSpaceOverlay Canvas!
            hbCanvasGO.transform.position = Vector3.zero;

            Canvas hbCanvas = hbCanvasGO.GetComponent<Canvas>();
            if (hbCanvas != null)
            {
                hbCanvas.renderMode = RenderMode.WorldSpace;
                hbCanvas.worldCamera = mainCam;
                hbCanvas.sortingOrder = 15;
            }

            HealthBarManager hbm = Object.FindFirstObjectByType<HealthBarManager>();
            if (hbm != null)
            {
                SerializedObject soHbm = new SerializedObject(hbm);
                soHbm.FindProperty("healthBarCanvas").objectReferenceValue = hbCanvasGO.transform;
                if (playerGO != null)
                {
                    soHbm.FindProperty("playerStats").objectReferenceValue = playerGO.GetComponent<PlayerStats>();
                }
                soHbm.ApplyModifiedProperties();
            }

            Debug.Log("[SceneCanvasSetup] HealthBarCanvas configured as Root WorldSpace Canvas.");
        }

        // 7. Setup Canvas 3: KitchenCanvas (Family Cooking Pot & Recipe Notebook)
        // Find all KitchenCanvas objects in scene and remove duplicates
        var allKitchenUIs = Object.FindObjectsByType<KitchenUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject primaryKitchenCanvas = null;

        if (allKitchenUIs.Length > 0)
        {
            primaryKitchenCanvas = allKitchenUIs[0].gameObject;
            for (int i = 1; i < allKitchenUIs.Length; i++)
            {
                if (allKitchenUIs[i] != null && allKitchenUIs[i].gameObject != primaryKitchenCanvas)
                {
                    Object.DestroyImmediate(allKitchenUIs[i].gameObject);
                    Debug.Log("[SceneCanvasSetup] Removed duplicate KitchenCanvas.");
                }
            }
        }

        if (primaryKitchenCanvas == null)
        {
            GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Assets/_Prefabs/KitchenCanvas.prefab");
            if (pref != null)
            {
                primaryKitchenCanvas = (GameObject)PrefabUtility.InstantiatePrefab(pref, activeScene);
            }
        }

        if (primaryKitchenCanvas != null)
        {
            primaryKitchenCanvas.name = "KitchenCanvas";
            primaryKitchenCanvas.transform.SetParent(null);

            Canvas kCanvas = primaryKitchenCanvas.GetComponent<Canvas>();
            if (kCanvas != null)
            {
                kCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                kCanvas.sortingOrder = 20;
            }

            CanvasScaler kScaler = primaryKitchenCanvas.GetComponent<CanvasScaler>();
            if (kScaler != null)
            {
                kScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                kScaler.referenceResolution = new Vector2(1920, 1080);
                kScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                kScaler.matchWidthOrHeight = 0.5f;
            }

            KitchenUI kui = primaryKitchenCanvas.GetComponent<KitchenUI>();
            if (kui != null)
            {
                kui.AutoBindReferences();
            }

            // CRITICAL: Deactivate KitchenOverlay and Toast by default so screen is never covered!
            Transform overlayT = primaryKitchenCanvas.transform.Find("KitchenOverlay");
            if (overlayT != null)
            {
                overlayT.gameObject.SetActive(false);
            }

            Transform toastT = primaryKitchenCanvas.transform.Find("FeedbackToast");
            if (toastT != null)
            {
                toastT.gameObject.SetActive(false);
            }

            Debug.Log("[SceneCanvasSetup] KitchenCanvas configured cleanly (Overlay deactivated by default).");
        }

        // 8. Mark dirty & save scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[SceneCanvasSetup] SUCCESS: Base.unity scene and all canvases saved successfully!");
    }

    private static void EnsureRootObject(string name, Vector3 pos)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            go.transform.SetParent(null);
            go.transform.position = pos;
        }
    }
}
