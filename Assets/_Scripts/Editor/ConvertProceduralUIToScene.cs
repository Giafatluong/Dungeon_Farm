#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ConvertProceduralUIToScene
{
    private const string MarkerPath = "C:/Users/dohuu/.gemini/antigravity/brain/580ed1c5-c4be-4165-a3e5-6281c5d87d84/scratch/converter_done.txt";

    [MenuItem("Tools/Convert Procedural UI To Scene Objects")]
    public static void RunConversion()
    {
        StringWriter log = new StringWriter();
        log.WriteLine($"=== STARTING DIRECT SCENE UI CONVERSION AT {System.DateTime.Now} ===");

        try
        {
            Directory.CreateDirectory("Assets/_Assets/_Prefabs/UI");

            // 1. Process Base.unity
            ConvertBaseScene(log);

            // 2. Process Dungeon.unity
            ConvertDungeonScene(log);

            // 3. Save Prefabs
            SaveUIPrefabs(log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.WriteLine("=== ALL SCENE CANVASES AND PREFABS CREATED SUCCESSFULLY ===");
            File.WriteAllText(MarkerPath, log.ToString());
            Debug.Log("[ConvertProceduralUIToScene] SUCCESS! Check log at: " + MarkerPath);
        }
        catch (System.Exception ex)
        {
            log.WriteLine($"ERROR: {ex.Message}\n{ex.StackTrace}");
            File.WriteAllText(MarkerPath, log.ToString());
            Debug.LogError("[ConvertProceduralUIToScene] FAILED: " + ex);
        }
    }

    private static void ConvertBaseScene(StringWriter log)
    {
        string scenePath = "Assets/_Scenes/Base.unity";
        log.WriteLine($"\n--- Processing {scenePath} ---");
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // A. Clean up duplicate [LoadingScreenCanvas]
        CleanupDuplicateLoadingCanvases(log);

        // Ensure [LoadingScreenUI]
        LoadingScreenUI loadingUI = Object.FindFirstObjectByType<LoadingScreenUI>(FindObjectsInactive.Include);
        if (loadingUI == null)
        {
            GameObject loadingGO = new GameObject("[LoadingScreenUI]");
            loadingUI = loadingGO.AddComponent<LoadingScreenUI>();
            loadingUI.EnsureUIConstructed();
            log.WriteLine("Created [LoadingScreenUI] in Base.unity");
        }
        else
        {
            loadingUI.EnsureUIConstructed();
            log.WriteLine("Ensured single [LoadingScreenUI] in Base.unity");
        }

        // B. DayTransitionUI
        DayTransitionUI dayUI = Object.FindFirstObjectByType<DayTransitionUI>(FindObjectsInactive.Include);
        if (dayUI == null)
        {
            GameObject dayGO = new GameObject("DayTransitionUI");
            dayUI = dayGO.AddComponent<DayTransitionUI>();
            dayUI.EnsureUIConstructed();
            log.WriteLine("Created DayTransitionUI in Base.unity");
        }
        else
        {
            dayUI.EnsureUIConstructed();
            log.WriteLine("Ensured existing DayTransitionUI in Base.unity");
        }

        // C. StatueUI - BlessingModalPanel
        StatueUI statueUI = Object.FindFirstObjectByType<StatueUI>(FindObjectsInactive.Include);
        if (statueUI != null)
        {
            statueUI.EnsureProceduralBlessingModal();
            if (statueUI.blessingModalPanel != null)
            {
                statueUI.blessingModalPanel.SetActive(false);
            }
            log.WriteLine("Ensured BlessingModalPanel in StatueCanvas of Base.unity");
        }

        if (dayUI != null && dayUI.morningReportModal != null)
        {
            dayUI.morningReportModal.SetActive(false);
        }

        // D. Player components: InteractionPromptUI & FarmCursorHighlight
        GameObject playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            InteractionPromptUI promptUI = playerGO.GetComponent<InteractionPromptUI>();
            if (promptUI == null) promptUI = playerGO.AddComponent<InteractionPromptUI>();
            promptUI.BuildUI();
            log.WriteLine("Ensured InteractionPromptCanvas under Player in Base.unity");

            if (playerGO.GetComponent<FarmCursorHighlight>() == null)
            {
                playerGO.AddComponent<FarmCursorHighlight>();
                log.WriteLine("Added FarmCursorHighlight directly to Player in Base.unity");
            }
        }

        // E. DungeonEntranceCanvas & EnterDungeonPanel
        DungeonEntranceUI entranceUI = Object.FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
        if (entranceUI != null)
        {
            GameObject canvasGO = GameObject.Find("DungeonEntranceCanvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("DungeonEntranceCanvas");
                Canvas c = canvasGO.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 100;

                CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.matchWidthOrHeight = 0.5f;

                canvasGO.AddComponent<GraphicRaycaster>();
                log.WriteLine("Created DungeonEntranceCanvas in Base.unity");
            }

            GameObject panel = canvasGO.transform.Find("EnterDungeonPanel")?.gameObject;
            if (panel == null)
            {
                panel = new GameObject("EnterDungeonPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(canvasGO.transform, false);
                RectTransform panelRT = panel.GetComponent<RectTransform>();
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.sizeDelta = Vector2.zero;
                Image panelBg = panel.GetComponent<Image>();
                panelBg.color = new Color(0.02f, 0.03f, 0.06f, 0.75f);
            }
            else
            {
                // Remove redundant nested Canvas if present
                Canvas nestedC = panel.GetComponent<Canvas>();
                if (nestedC != null) Object.DestroyImmediate(nestedC);
                GraphicRaycaster gr = panel.GetComponent<GraphicRaycaster>();
                if (gr != null) Object.DestroyImmediate(gr);
            }

            entranceUI.enterDungeonPanel = panel;
            entranceUI.EnsureUIConstructed();
            panel.SetActive(false);
            log.WriteLine("Configured DungeonEntranceCanvas with FloorSelectionModal in Base.unity");
        }

        // F. DungeonEntrance Prompt Canvas
        DungeonEntrance entrance = Object.FindFirstObjectByType<DungeonEntrance>(FindObjectsInactive.Include);
        if (entrance != null)
        {
            entrance.EnsurePromptUI();
            log.WriteLine("Ensured DungeonEntrancePromptCanvas in Base.unity");
        }

        // G. ItemTooltipCanvas
        ItemTooltipUI tooltipUI = Object.FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
        if (tooltipUI == null)
        {
            GameObject tooltipCanvasGO = new GameObject("[ItemTooltipCanvas]");
            Canvas c = tooltipCanvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 9999;

            CanvasScaler cs = tooltipCanvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;

            CanvasGroup cg = tooltipCanvasGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            tooltipUI = tooltipCanvasGO.AddComponent<ItemTooltipUI>();
            tooltipUI.parentCanvas = c;
            tooltipUI.canvasRT = tooltipCanvasGO.GetComponent<RectTransform>();
            tooltipUI.BuildUIHierarchy(tooltipCanvasGO.transform);
            log.WriteLine("Created [ItemTooltipCanvas] in Base.unity");
        }
        else
        {
            tooltipUI.BindSceneReferences();
            log.WriteLine("Ensured [ItemTooltipCanvas] in Base.unity");
        }

        // H. TutorialManager, [TutorialCanvas] & [IntroCutsceneCanvas]
        GameObject tutMgrGO = GameObject.Find("TutorialManager");
        if (tutMgrGO == null)
        {
            tutMgrGO = new GameObject("TutorialManager");
            log.WriteLine("Created TutorialManager GameObject in Base.unity");
        }
        TutorialManager tm = tutMgrGO.GetComponent<TutorialManager>();
        if (tm == null) tm = tutMgrGO.AddComponent<TutorialManager>();

        TutorialDialogueUI tutUI = tutMgrGO.GetComponent<TutorialDialogueUI>();
        if (tutUI == null) tutUI = tutMgrGO.AddComponent<TutorialDialogueUI>();

        IntroCutsceneController intro = tutMgrGO.GetComponent<IntroCutsceneController>();
        if (intro == null) intro = tutMgrGO.AddComponent<IntroCutsceneController>();

        // Build [TutorialCanvas] in scene
        GameObject tutCanvasGO = GameObject.Find("[TutorialCanvas]");
        if (tutCanvasGO == null)
        {
            tutUI.EnsureUIConstructed();
            log.WriteLine("Created [TutorialCanvas] directly in Base.unity");
        }
        else
        {
            tutUI.masterCanvasGO = tutCanvasGO;
            tutUI.masterCanvas = tutCanvasGO.GetComponent<Canvas>();
            tutUI.BindSceneReferences();
            log.WriteLine("Ensured existing [TutorialCanvas] in Base.unity");
        }

        // Build [IntroCutsceneCanvas] in scene
        GameObject introCanvasGO = GameObject.Find("[IntroCutsceneCanvas]");
        if (introCanvasGO == null)
        {
            intro.EnsureUIConstructed();
            log.WriteLine("Created [IntroCutsceneCanvas] directly in Base.unity");
        }
        else
        {
            intro.cutsceneCanvasGO = introCanvasGO;
            intro.BindSceneReferences();
            log.WriteLine("Ensured existing [IntroCutsceneCanvas] in Base.unity");
        }
        if (intro.cutsceneCanvasGO != null) intro.cutsceneCanvasGO.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.WriteLine($"Saved {scenePath} successfully.");
    }

    private static void ConvertDungeonScene(StringWriter log)
    {
        string scenePath = "Assets/_Scenes/Dungeon.unity";
        log.WriteLine($"\n--- Processing {scenePath} ---");
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Clean duplicate loading canvases
        CleanupDuplicateLoadingCanvases(log);

        Canvas mainCanvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (mainCanvas == null)
        {
            mainCanvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }
        Transform canvasT = mainCanvas != null ? mainCanvas.transform : null;

        // A. LoadingScreenUI
        LoadingScreenUI loadingUI = Object.FindFirstObjectByType<LoadingScreenUI>(FindObjectsInactive.Include);
        if (loadingUI == null)
        {
            GameObject loadingGO = new GameObject("[LoadingScreenUI]");
            loadingUI = loadingGO.AddComponent<LoadingScreenUI>();
            loadingUI.EnsureUIConstructed();
            log.WriteLine("Created [LoadingScreenUI] in Dungeon.unity");
        }
        else
        {
            loadingUI.EnsureUIConstructed();
            log.WriteLine("Ensured single [LoadingScreenUI] in Dungeon.unity");
        }

        // Clean duplicate LootPanels under CombatCanvas
        if (mainCanvas != null)
        {
            List<GameObject> lootPanels = new List<GameObject>();
            for (int i = 0; i < mainCanvas.transform.childCount; i++)
            {
                Transform ch = mainCanvas.transform.GetChild(i);
                if (ch.name == "LootPanel") lootPanels.Add(ch.gameObject);
            }
            if (lootPanels.Count > 1)
            {
                GameObject keep = lootPanels[lootPanels.Count - 1]; // Keep the latest configured one
                foreach (var lp in lootPanels)
                {
                    if (lp != keep)
                    {
                        log.WriteLine($"Removed duplicate LootPanel (children: {lp.transform.childCount}) in Dungeon.unity");
                        Object.DestroyImmediate(lp);
                    }
                }
            }
        }

        // B. LootUI (under CombatCanvas)
        LootUI lootUI = Object.FindFirstObjectByType<LootUI>(FindObjectsInactive.Include);
        if (lootUI == null)
        {
            GameObject go = new GameObject("LootUI");
            if (canvasT != null) go.transform.SetParent(canvasT, false);
            lootUI = go.AddComponent<LootUI>();
            lootUI.canvas = mainCanvas;
            lootUI.EnsureUIBuilt();
            if (lootUI.lootPanel != null) lootUI.lootPanel.SetActive(false);
            log.WriteLine("Created LootUI under CombatCanvas in Dungeon.unity");
        }
        else
        {
            lootUI.EnsureUIBuilt();
            if (lootUI.lootPanel != null) lootUI.lootPanel.SetActive(false);
            log.WriteLine("Ensured existing LootUI in Dungeon.unity");
        }

        // C. DungeonSummaryUI
        DungeonSummaryUI summaryUI = Object.FindFirstObjectByType<DungeonSummaryUI>(FindObjectsInactive.Include);
        if (summaryUI == null)
        {
            GameObject go = new GameObject("DungeonSummaryUI");
            if (canvasT != null) go.transform.SetParent(canvasT, false);
            summaryUI = go.AddComponent<DungeonSummaryUI>();
            summaryUI.canvas = mainCanvas;
            summaryUI.EnsureUIBuilt();
            log.WriteLine("Created DungeonSummaryUI in Dungeon.unity");
        }
        else
        {
            summaryUI.EnsureUIBuilt();
            log.WriteLine("Ensured existing DungeonSummaryUI in Dungeon.unity");
        }

        // D. FloorVictoryUI
        FloorVictoryUI victoryUI = Object.FindFirstObjectByType<FloorVictoryUI>(FindObjectsInactive.Include);
        if (victoryUI == null)
        {
            GameObject go = new GameObject("FloorVictoryUI");
            if (canvasT != null) go.transform.SetParent(canvasT, false);
            victoryUI = go.AddComponent<FloorVictoryUI>();
            victoryUI.canvas = mainCanvas;
            victoryUI.EnsureUIBuilt();
            log.WriteLine("Created FloorVictoryUI in Dungeon.unity");
        }
        else
        {
            victoryUI.EnsureUIBuilt();
            log.WriteLine("Ensured existing FloorVictoryUI in Dungeon.unity");
        }

        // E. DungeonEventUI
        DungeonEventUI eventUI = Object.FindFirstObjectByType<DungeonEventUI>(FindObjectsInactive.Include);
        if (eventUI == null)
        {
            GameObject go = new GameObject("DungeonEventUI");
            if (canvasT != null) go.transform.SetParent(canvasT, false);
            eventUI = go.AddComponent<DungeonEventUI>();
            eventUI.canvas = mainCanvas;
            eventUI.EnsureUIBuilt();
            log.WriteLine("Created DungeonEventUI in Dungeon.unity");
        }
        else
        {
            eventUI.EnsureUIBuilt();
            log.WriteLine("Ensured existing DungeonEventUI in Dungeon.unity");
        }

        // F. RewardUI
        RewardUI rewardUI = Object.FindFirstObjectByType<RewardUI>(FindObjectsInactive.Include);
        if (rewardUI == null)
        {
            GameObject go = new GameObject("RewardUI");
            if (canvasT != null) go.transform.SetParent(canvasT, false);
            rewardUI = go.AddComponent<RewardUI>();
            rewardUI.canvas = mainCanvas;
            rewardUI.EnsureUIBuilt();
            log.WriteLine("Created RewardUI in Dungeon.unity");
        }
        else
        {
            rewardUI.EnsureUIBuilt();
            log.WriteLine("Ensured existing RewardUI in Dungeon.unity");
        }

        // G. MerchantUI
        MerchantUI merchantUI = Object.FindFirstObjectByType<MerchantUI>(FindObjectsInactive.Include);
        if (merchantUI == null)
        {
            GameObject go = new GameObject("MerchantUI");
            if (canvasT != null) go.transform.SetParent(canvasT, false);
            merchantUI = go.AddComponent<MerchantUI>();
            merchantUI.canvas = mainCanvas;
            merchantUI.EnsureUIBuilt();
            log.WriteLine("Created MerchantUI in Dungeon.unity");
        }
        else
        {
            merchantUI.EnsureUIBuilt();
            log.WriteLine("Ensured existing MerchantUI in Dungeon.unity");
        }

        // H. DungeonEventStation (DungeonEventInteractable)
        DungeonEventInteractable eventStation = Object.FindFirstObjectByType<DungeonEventInteractable>(FindObjectsInactive.Include);
        if (eventStation == null)
        {
            GameObject stationGO = new GameObject("DungeonEventStation");
            eventStation = stationGO.AddComponent<DungeonEventInteractable>();
            log.WriteLine("Created DungeonEventStation in Dungeon.unity");
        }

        WaveManager waveMgr = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (waveMgr != null)
        {
            SerializedObject so = new SerializedObject(waveMgr);
            SerializedProperty prop = so.FindProperty("eventInteractable");
            if (prop != null)
            {
                prop.objectReferenceValue = eventStation;
                so.ApplyModifiedProperties();
                log.WriteLine("Bound eventInteractable on WaveManager in Dungeon.unity");
            }
        }

        // I. ItemTooltipUI Canvas in Dungeon.unity
        ItemTooltipUI dungeonTooltipUI = Object.FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
        if (dungeonTooltipUI == null)
        {
            GameObject tooltipCanvasGO = new GameObject("[ItemTooltipCanvas]");
            Canvas c = tooltipCanvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 9999;

            CanvasScaler cs = tooltipCanvasGO.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;

            CanvasGroup cg = tooltipCanvasGO.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            dungeonTooltipUI = tooltipCanvasGO.AddComponent<ItemTooltipUI>();
            dungeonTooltipUI.parentCanvas = c;
            dungeonTooltipUI.canvasRT = tooltipCanvasGO.GetComponent<RectTransform>();
            dungeonTooltipUI.BuildUIHierarchy(tooltipCanvasGO.transform);
            log.WriteLine("Created [ItemTooltipCanvas] in Dungeon.unity");
        }
        else
        {
            dungeonTooltipUI.BindSceneReferences();
            log.WriteLine("Ensured [ItemTooltipCanvas] in Dungeon.unity");
        }

        // J. Player InteractionPromptUI in Dungeon.unity
        GameObject dungeonPlayerGO = GameObject.Find("PlayerCombat") ?? GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
        if (dungeonPlayerGO != null)
        {
            InteractionPromptUI promptUI = dungeonPlayerGO.GetComponent<InteractionPromptUI>();
            if (promptUI == null) promptUI = dungeonPlayerGO.AddComponent<InteractionPromptUI>();
            promptUI.BuildUI();
            log.WriteLine($"Ensured InteractionPromptCanvas under {dungeonPlayerGO.name} in Dungeon.unity");
        }

        // K. DungeonExitPortal & DungeonExitPromptCanvas
        DungeonExitInteractable exitInteractable = Object.FindFirstObjectByType<DungeonExitInteractable>(FindObjectsInactive.Include);
        if (exitInteractable == null)
        {
            GameObject exitGO = new GameObject("DungeonExitPortal");
            exitInteractable = exitGO.AddComponent<DungeonExitInteractable>();
            log.WriteLine("Created DungeonExitPortal in Dungeon.unity");
        }
        exitInteractable.BuildPromptUI();
        log.WriteLine("Ensured DungeonExitPromptCanvas in Dungeon.unity");
        exitInteractable.gameObject.SetActive(false);

        // L. EventPromptCanvas on DungeonEventStation
        if (eventStation != null)
        {
            eventStation.BuildPromptUI();
            log.WriteLine("Ensured EventPromptCanvas on DungeonEventStation in Dungeon.unity");
        }

        // M. [DungeonTutorialController] & [DungeonTutorialCanvas]
        DungeonTutorialController tutCtrl = Object.FindFirstObjectByType<DungeonTutorialController>(FindObjectsInactive.Include);
        if (tutCtrl == null)
        {
            GameObject tutCtrlGO = new GameObject("[DungeonTutorialController]");
            tutCtrl = tutCtrlGO.AddComponent<DungeonTutorialController>();
            log.WriteLine("Created [DungeonTutorialController] in Dungeon.unity");
        }
        tutCtrl.ConstructUI();
        if (tutCtrl.overlayCanvasGO != null) tutCtrl.overlayCanvasGO.SetActive(false);
        log.WriteLine("Ensured [DungeonTutorialCanvas] in Dungeon.unity");

        if (summaryUI != null && summaryUI.summaryPanel != null) summaryUI.summaryPanel.SetActive(false);
        if (victoryUI != null && victoryUI.victoryPanel != null) victoryUI.victoryPanel.SetActive(false);
        if (eventUI != null && eventUI.eventPanel != null) eventUI.eventPanel.SetActive(false);
        if (rewardUI != null && rewardUI.rewardPanel != null) rewardUI.rewardPanel.SetActive(false);
        if (merchantUI != null && merchantUI.merchantPanel != null) merchantUI.merchantPanel.SetActive(false);
        if (eventStation != null) eventStation.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.WriteLine($"Saved {scenePath} successfully.");
    }

    private static void CleanupDuplicateLoadingCanvases(StringWriter log)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> loadingCanvases = new List<GameObject>();
        foreach (var c in canvases)
        {
            if (c != null && c.name == "[LoadingScreenCanvas]")
            {
                loadingCanvases.Add(c.gameObject);
            }
        }

        if (loadingCanvases.Count > 1)
        {
            log.WriteLine($"Found {loadingCanvases.Count} duplicate [LoadingScreenCanvas], cleaning up to keep 1...");
            GameObject best = loadingCanvases[0];
            foreach (var go in loadingCanvases)
            {
                if (go.transform.childCount > best.transform.childCount)
                    best = go;
            }
            foreach (var go in loadingCanvases)
            {
                if (go != best)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }

    private static void SaveUIPrefabs(StringWriter log)
    {
        log.WriteLine("\n--- Saving UI Prefabs to Assets/_Assets/_Prefabs/UI ---");
        var baseScene = EditorSceneManager.OpenScene("Assets/_Scenes/Base.unity", OpenSceneMode.Single);

        SavePrefabIfFound("DungeonEntranceCanvas", "Assets/_Assets/_Prefabs/UI/DungeonEntranceCanvas.prefab", log);
        SavePrefabIfFound("[ItemTooltipCanvas]", "Assets/_Assets/_Prefabs/UI/ItemTooltipCanvas.prefab", log);
        SavePrefabIfFound("[TutorialCanvas]", "Assets/_Assets/_Prefabs/UI/TutorialCanvas.prefab", log);
        SavePrefabIfFound("[IntroCutsceneCanvas]", "Assets/_Assets/_Prefabs/UI/IntroCutsceneCanvas.prefab", log);
        SavePrefabIfFound("[LoadingScreenCanvas]", "Assets/_Assets/_Prefabs/UI/LoadingScreenCanvas.prefab", log);
        SavePrefabIfFound("DungeonEntrancePromptCanvas", "Assets/_Assets/_Prefabs/UI/DungeonEntrancePromptCanvas.prefab", log);
        SavePrefabIfFound("TutorialManager", "Assets/_Assets/_Prefabs/UI/TutorialManager.prefab", log);
        SavePrefabIfFound("InteractionPromptCanvas", "Assets/_Assets/_Prefabs/UI/InteractionPromptCanvas.prefab", log);

        EditorSceneManager.MarkSceneDirty(baseScene);
        EditorSceneManager.SaveScene(baseScene);

        var dungeonScene = EditorSceneManager.OpenScene("Assets/_Scenes/Dungeon.unity", OpenSceneMode.Single);
        GameObject lootGO = FindSceneGameObject("LootUI");
        if (lootGO != null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(lootGO, "Assets/_Assets/_Prefabs/UI/LootUI.prefab", InteractionMode.AutomatedAction);
            log.WriteLine("Saved Prefab: Assets/_Assets/_Prefabs/UI/LootUI.prefab");
        }

        SavePrefabIfFound("DungeonExitPromptCanvas", "Assets/_Assets/_Prefabs/UI/DungeonExitPromptCanvas.prefab", log);
        SavePrefabIfFound("EventPromptCanvas", "Assets/_Assets/_Prefabs/UI/EventPromptCanvas.prefab", log);
        SavePrefabIfFound("[DungeonTutorialCanvas]", "Assets/_Assets/_Prefabs/UI/DungeonTutorialCanvas.prefab", log);
        SavePrefabIfFound("[DungeonTutorialController]", "Assets/_Assets/_Prefabs/UI/DungeonTutorialController.prefab", log);
        SavePrefabIfFound("DungeonExitPortal", "Assets/_Assets/_Prefabs/UI/DungeonExitPortal.prefab", log);
        EditorSceneManager.MarkSceneDirty(dungeonScene);
        EditorSceneManager.SaveScene(dungeonScene);
    }

    private static void SavePrefabIfFound(string goName, string prefabPath, StringWriter log)
    {
        GameObject go = FindSceneGameObject(goName);
        if (go != null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
            log.WriteLine($"Saved Prefab: {prefabPath}");
        }
        else
        {
            log.WriteLine($"WARNING: Could not find '{goName}' to save as prefab '{prefabPath}'");
        }
    }

    private static GameObject FindSceneGameObject(string name)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            if (r.name == name) return r;
            Transform[] children = r.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                if (t.gameObject.name == name) return t.gameObject;
            }
        }
        return GameObject.Find(name);
    }
}
#endif
