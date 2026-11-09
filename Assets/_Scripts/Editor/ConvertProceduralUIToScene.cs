#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ConvertProceduralUIToScene
{
    private const string MarkerPath = "C:/Users/dohuu/.gemini/antigravity/brain/9473fc46-3e0d-4cef-8b98-06cb283cdd31/scratch/converter_done.txt";

    // Note: Automatic conversion is disabled so user scene edits are not overwritten.
    // Use the Unity Menu: Tools -> Convert Procedural UI To Scene Objects if manual conversion is needed.
    [MenuItem("Tools/Convert Procedural UI To Scene Objects")]
    public static void RunConversion()
    {
        StringWriter log = new StringWriter();
        log.WriteLine($"=== STARTING PROCEDURAL UI CONVERSION AT {System.DateTime.Now} ===");

        try
        {
            // 1. Process Base.unity
            ConvertBaseScene(log);

            // 2. Process Dungeon.unity
            ConvertDungeonScene(log);

            log.WriteLine("=== ALL PROCEDURAL UIS CONVERTED SUCCESSFULLY ===");
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

        // A. LoadingScreenUI
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
            log.WriteLine("Ensured existing [LoadingScreenUI] in Base.unity");
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
            log.WriteLine("Created real BlessingModalPanel (8 cards + close btn) in StatueCanvas of Base.unity");
        }

        if (dayUI != null && dayUI.morningReportModal != null)
        {
            dayUI.morningReportModal.SetActive(false);
        }

        // D. Player components: InteractionPromptUI & FarmCursorHighlight
        GameObject playerGO = GameObject.Find("Player");
        if (playerGO != null)
        {
            if (playerGO.GetComponent<InteractionPromptUI>() == null)
            {
                playerGO.AddComponent<InteractionPromptUI>();
                log.WriteLine("Added InteractionPromptUI directly to Player in Base.unity");
            }
            if (playerGO.GetComponent<FarmCursorHighlight>() == null)
            {
                playerGO.AddComponent<FarmCursorHighlight>();
                log.WriteLine("Added FarmCursorHighlight directly to Player in Base.unity");
            }
        }

        if (statueUI != null && statueUI.blessingModalPanel != null)
        {
            statueUI.blessingModalPanel.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.WriteLine($"Saved {scenePath} successfully.");
    }

    private static void ConvertDungeonScene(StringWriter log)
    {
        string scenePath = "Assets/_Scenes/Dungeon.unity";
        log.WriteLine($"\n--- Processing {scenePath} ---");
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

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
            log.WriteLine("Ensured existing [LoadingScreenUI] in Dungeon.unity");
        }

        // B. DungeonSummaryUI
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

        // C. FloorVictoryUI
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

        // D. DungeonEventUI
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

        // E. RewardUI
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

        // F. MerchantUI
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

        // G. DungeonEventStation (DungeonEventInteractable)
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
}
#endif
