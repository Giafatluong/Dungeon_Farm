using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Combat Preparation Canvases:
/// 1. DungeonEntranceArrowCanvas (Safe Entrance / Antechamber right arrow proceed button)
/// 2. CampCanvas (Camp Encounter UI: Eating, Cooking, Exercising, Resting, Ambush Risk Gauge, Modals)
/// directly inside Dungeon.unity as scene-authored Canvases.
/// Matches 100% the layout, sizes, colors, fonts, hierarchy, and visuals of existing UI builders.
/// All button events are wired as persistent listeners.
/// </summary>
public static class DungeonPreparationCanvasBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Dungeon.unity";
    private const string ARROW_CANVAS_NAME = "DungeonEntranceArrowCanvas";
    private const string CAMP_CANVAS_NAME = "CampCanvas";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build Both Combat Preparation Canvases (Dungeon)")]
    public static void BuildBothCanvases()
    {
        BuildDungeonEntranceArrowCanvas();
        BuildCampCanvas();
        Debug.Log("[DungeonPreparationCanvasBuilder] SUCCESS: Built both DungeonEntranceArrowCanvas and CampCanvas in Dungeon.unity!");
    }

    [MenuItem("Tools/Build Dungeon Entrance Arrow Canvas")]
    public static GameObject BuildDungeonEntranceArrowCanvasMenu()
    {
        return BuildDungeonEntranceArrowCanvas();
    }

    [MenuItem("Tools/Build Camp Canvas")]
    public static GameObject BuildCampCanvasMenu()
    {
        return BuildCampCanvas();
    }

    private static void EnsureFontLoaded()
    {
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null) font = DungeonUIAssetHelper.GetFontAsset();
        }
    }

    public static GameObject BuildDungeonEntranceArrowCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any existing DungeonEntranceArrowUI objects in the scene
        var existingUIs = Object.FindObjectsByType<DungeonEntranceArrowUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existingUIs.Length; i++)
        {
            if (existingUIs[i] != null)
            {
                Undo.DestroyObjectImmediate(existingUIs[i].gameObject);
            }
        }

        GameObject orphan = GameObject.Find(ARROW_CANVAS_NAME);
        if (orphan != null)
        {
            Undo.DestroyObjectImmediate(orphan);
        }

        EnsureFontLoaded();

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(ARROW_CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Dungeon Entrance Arrow Canvas");
        canvasGO.layer = uiLayer;
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 105; // Sits above CombatCanvas (100)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        DungeonEntranceArrowUI ui = canvasGO.AddComponent<DungeonEntranceArrowUI>();

        // 2. Entrance Arrow Container with Button
        Vector2 basePosition = new Vector2(-28f, 0f);
        GameObject container = new GameObject("EntranceArrowContainer", typeof(RectTransform), typeof(Image), typeof(Button));
        container.layer = uiLayer;
        container.transform.SetParent(canvasGO.transform, false);
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(1f, 0.5f);
        containerRT.anchorMax = new Vector2(1f, 0.5f);
        containerRT.pivot = new Vector2(1f, 0.5f);
        containerRT.sizeDelta = new Vector2(210f, 85f);
        containerRT.anchoredPosition = basePosition;

        Image bg = container.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.12f, 0.16f, 0.24f, 0.95f));

        Button arrowButton = container.GetComponent<Button>();
        arrowButton.targetGraphic = bg;
        UnityEventTools.AddPersistentListener(arrowButton.onClick, ui.OnArrowClicked);

        // 3. Arrow TMP Icon
        GameObject arrowGO = new GameObject("ArrowTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.layer = uiLayer;
        arrowGO.transform.SetParent(container.transform, false);
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.pivot = new Vector2(1f, 0.5f);
        arrowRT.sizeDelta = new Vector2(50f, 50f);
        arrowRT.anchoredPosition = new Vector2(-12f, 0f);

        TextMeshProUGUI arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        if (font != null) arrowTMP.font = font;
        arrowTMP.text = "->";
        arrowTMP.fontSize = 32f;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.color = new Color(1f, 0.85f, 0.25f);

        // 4. Title Text
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(container.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.5f);
        titleRT.anchorMax = new Vector2(0f, 0.5f);
        titleRT.pivot = new Vector2(0f, 0.5f);
        titleRT.anchoredPosition = new Vector2(16f, 14f);
        titleRT.sizeDelta = new Vector2(130f, 30f);

        TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) titleText.font = font;
        titleText.text = "PROCEED";
        titleText.fontSize = 20f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // 5. Hint Text
        GameObject hintGO = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.layer = uiLayer;
        hintGO.transform.SetParent(container.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0.5f);
        hintRT.anchorMax = new Vector2(0f, 0.5f);
        hintRT.pivot = new Vector2(0f, 0.5f);
        hintRT.anchoredPosition = new Vector2(16f, -14f);
        hintRT.sizeDelta = new Vector2(130f, 24f);

        TextMeshProUGUI hintText = hintGO.GetComponent<TextMeshProUGUI>();
        if (font != null) hintText.font = font;
        hintText.text = "Enter Stage 1 [Space / ->]";
        hintText.fontSize = 12f;
        hintText.color = new Color(0.75f, 0.85f, 1f);

        // Wire serialized references
        ui.container = container;
        ui.arrowButton = arrowButton;
        ui.arrowIconRT = arrowRT;
        ui.titleText = titleText;
        ui.hintText = hintText;
        ui.arrowTMP = arrowTMP;

        // Initial state: inactive until entrance stage shows it
        container.SetActive(false);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DungeonPreparationCanvasBuilder] DungeonEntranceArrowCanvas built & saved successfully.");
        return canvasGO;
    }

    public static GameObject BuildCampCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any empty/unbound CampUI component on CombatCanvas
        GameObject combatCanvasGO = GameObject.Find("CombatCanvas");
        if (combatCanvasGO != null)
        {
            CampUI[] combatCampUIs = combatCanvasGO.GetComponents<CampUI>();
            for (int i = 0; i < combatCampUIs.Length; i++)
            {
                if (combatCampUIs[i] != null)
                {
                    Undo.DestroyObjectImmediate(combatCampUIs[i]);
                    Debug.Log("[DungeonPreparationCanvasBuilder] Removed unassigned CampUI component from CombatCanvas.");
                }
            }
        }

        // Clean up any existing CampCanvas in the scene
        GameObject existingCampCanvas = GameObject.Find(CAMP_CANVAS_NAME);
        if (existingCampCanvas != null)
        {
            Undo.DestroyObjectImmediate(existingCampCanvas);
        }

        EnsureFontLoaded();

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(CAMP_CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Camp Canvas");
        canvasGO.layer = uiLayer;
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120; // Sits above CombatCanvas (100) and Arrow (105)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CampUI ui = canvasGO.AddComponent<CampUI>();

        // Find Scene dependencies
        ui.camp = Object.FindFirstObjectByType<Camp>(FindObjectsInactive.Include);
        ui.waveManager = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        ui.playerStats = Object.FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);

        // 2. Main Camp Panel (780 x 530)
        GameObject mainPanel = new GameObject("CampMainPanel", typeof(RectTransform), typeof(Image));
        mainPanel.layer = uiLayer;
        mainPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = mainPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(780, 530);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = mainPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(panelBg, new Color(0.10f, 0.12f, 0.17f, 0.98f));
        ui.mainCampPanel = mainPanel;

        // Camp Icon
        Sprite tentSprite = DungeonUIAssetHelper.GetTentSprite();
        if (tentSprite != null)
        {
            GameObject iconGO = new GameObject("CampIcon", typeof(RectTransform), typeof(Image));
            iconGO.layer = uiLayer;
            iconGO.transform.SetParent(mainPanel.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 1);
            iconRT.anchorMax = new Vector2(0.5f, 1);
            iconRT.pivot = new Vector2(0.5f, 1);
            iconRT.anchoredPosition = new Vector2(-190, -14);
            iconRT.sizeDelta = new Vector2(38, 38);
            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = tentSprite;
            iconImg.preserveAspect = true;
        }

        // Title
        GameObject titleGO = new GameObject("CampTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(mainPanel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(-40, 36);
        ui.campTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.campTitleText.font = font;
        ui.campTitleText.fontSize = 22;
        ui.campTitleText.fontStyle = FontStyles.Bold;
        ui.campTitleText.alignment = TextAlignmentOptions.Center;
        ui.campTitleText.text = "CAMP ENCOUNTER";
        ui.campTitleText.color = new Color(1f, 0.85f, 0.35f);

        // Description
        GameObject descGO = new GameObject("CampDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.layer = uiLayer;
        descGO.transform.SetParent(mainPanel.transform, false);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 1);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.pivot = new Vector2(0.5f, 1);
        descRT.anchoredPosition = new Vector2(0, -56);
        descRT.sizeDelta = new Vector2(-60, 48);
        ui.campDescText = descGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.campDescText.font = font;
        ui.campDescText.fontSize = 12.5f;
        ui.campDescText.alignment = TextAlignmentOptions.Center;
        ui.campDescText.text = "A warm campfire burns in the dungeon. Rest, eat, or cook to recover.\n<color=#FFAA55>Warning: Activities create noise and aroma that attract wandering monsters!</color>";
        ui.campDescText.color = new Color(0.85f, 0.88f, 0.95f);

        // Ambush Gauge Section
        GameObject riskSection = new GameObject("RiskSection", typeof(RectTransform));
        riskSection.layer = uiLayer;
        riskSection.transform.SetParent(mainPanel.transform, false);
        RectTransform riskRT = riskSection.GetComponent<RectTransform>();
        riskRT.anchorMin = new Vector2(0.5f, 1);
        riskRT.anchorMax = new Vector2(0.5f, 1);
        riskRT.anchoredPosition = new Vector2(0, -135);
        riskRT.sizeDelta = new Vector2(500, 60);

        GameObject riskTextGO = new GameObject("RiskPercentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        riskTextGO.layer = uiLayer;
        riskTextGO.transform.SetParent(riskSection.transform, false);
        RectTransform riskTextRT = riskTextGO.GetComponent<RectTransform>();
        riskTextRT.anchorMin = new Vector2(0, 1);
        riskTextRT.anchorMax = new Vector2(1, 1);
        riskTextRT.anchoredPosition = new Vector2(0, 0);
        riskTextRT.sizeDelta = new Vector2(0, 20);
        ui.ambushRiskPercentText = riskTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.ambushRiskPercentText.font = font;
        ui.ambushRiskPercentText.fontSize = 13.5f;
        ui.ambushRiskPercentText.alignment = TextAlignmentOptions.Center;
        ui.ambushRiskPercentText.text = "Ambush Risk: <b>0%</b>";

        GameObject gaugeBgGO = new GameObject("GaugeBg", typeof(RectTransform), typeof(Image));
        gaugeBgGO.layer = uiLayer;
        gaugeBgGO.transform.SetParent(riskSection.transform, false);
        RectTransform gaugeBgRT = gaugeBgGO.GetComponent<RectTransform>();
        gaugeBgRT.anchoredPosition = new Vector2(0, -16);
        gaugeBgRT.sizeDelta = new Vector2(460, 14);
        Image gaugeBgImg = gaugeBgGO.GetComponent<Image>();
        gaugeBgImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        GameObject gaugeFillGO = new GameObject("GaugeFill", typeof(RectTransform), typeof(Image));
        gaugeFillGO.layer = uiLayer;
        gaugeFillGO.transform.SetParent(gaugeBgGO.transform, false);
        RectTransform gaugeFillRT = gaugeFillGO.GetComponent<RectTransform>();
        gaugeFillRT.anchorMin = Vector2.zero;
        gaugeFillRT.anchorMax = Vector2.one;
        gaugeFillRT.sizeDelta = Vector2.zero;
        ui.ambushRiskFillImage = gaugeFillGO.GetComponent<Image>();
        ui.ambushRiskFillImage.type = Image.Type.Filled;
        ui.ambushRiskFillImage.fillMethod = Image.FillMethod.Horizontal;
        ui.ambushRiskFillImage.fillAmount = 0f;
        ui.ambushRiskFillImage.color = new Color(0.2f, 0.85f, 0.4f);

        GameObject riskStatusGO = new GameObject("RiskStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        riskStatusGO.layer = uiLayer;
        riskStatusGO.transform.SetParent(riskSection.transform, false);
        RectTransform riskStatusRT = riskStatusGO.GetComponent<RectTransform>();
        riskStatusRT.anchoredPosition = new Vector2(0, -38);
        riskStatusRT.sizeDelta = new Vector2(460, 18);
        ui.ambushRiskStatusText = riskStatusGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.ambushRiskStatusText.font = font;
        ui.ambushRiskStatusText.fontSize = 12;
        ui.ambushRiskStatusText.alignment = TextAlignmentOptions.Center;
        ui.ambushRiskStatusText.text = "The campsite is quiet and safe.";
        ui.ambushRiskStatusText.color = new Color(0.7f, 1f, 0.7f);

        // Player Status Section
        GameObject statusGO = new GameObject("PlayerStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.layer = uiLayer;
        statusGO.transform.SetParent(mainPanel.transform, false);
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.5f, 1);
        statusRT.anchorMax = new Vector2(0.5f, 1);
        statusRT.anchoredPosition = new Vector2(0, -188);
        statusRT.sizeDelta = new Vector2(600, 22);
        ui.playerStatusText = statusGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.playerStatusText.font = font;
        ui.playerStatusText.fontSize = 13.5f;
        ui.playerStatusText.alignment = TextAlignmentOptions.Center;
        ui.playerStatusText.text = "HP: 100/100   |   Fullness: 100/100";

        GameObject buffsGO = new GameObject("PlayerBuffsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        buffsGO.layer = uiLayer;
        buffsGO.transform.SetParent(mainPanel.transform, false);
        RectTransform buffsRT = buffsGO.GetComponent<RectTransform>();
        buffsRT.anchorMin = new Vector2(0.5f, 1);
        buffsRT.anchorMax = new Vector2(0.5f, 1);
        buffsRT.anchoredPosition = new Vector2(0, -210);
        buffsRT.sizeDelta = new Vector2(600, 18);
        ui.playerBuffsText = buffsGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.playerBuffsText.font = font;
        ui.playerBuffsText.fontSize = 12;
        ui.playerBuffsText.alignment = TextAlignmentOptions.Center;
        ui.playerBuffsText.text = "Buffs: None";

        // Action Buttons Grid
        GameObject btnGroup = new GameObject("ActionButtons", typeof(RectTransform));
        btnGroup.layer = uiLayer;
        btnGroup.transform.SetParent(mainPanel.transform, false);
        RectTransform btnGroupRT = btnGroup.GetComponent<RectTransform>();
        btnGroupRT.anchorMin = new Vector2(0.5f, 0);
        btnGroupRT.anchorMax = new Vector2(0.5f, 0);
        btnGroupRT.anchoredPosition = new Vector2(0, 140);
        btnGroupRT.sizeDelta = new Vector2(680, 110);

        ui.eatButton = CreateCampButton(btnGroup.transform, "EatBtn", "EAT FOOD\n(+15% Risk)", new Vector2(-230, 26), new Vector2(210, 48), new Color(0.2f, 0.6f, 0.35f), ui.OnEatButtonClicked, uiLayer);
        ui.cookButton = CreateCampButton(btnGroup.transform, "CookBtn", "COOK RECIPE\n(+20% Risk)", new Vector2(0, 26), new Vector2(210, 48), new Color(0.8f, 0.45f, 0.15f), ui.OnCookButtonClicked, uiLayer);
        ui.exerciseButton = CreateCampButton(btnGroup.transform, "ExerciseBtn", "EXERCISE (-25 Full)\n(+35% Risk)", new Vector2(230, 26), new Vector2(210, 48), new Color(0.25f, 0.5f, 0.75f), ui.OnExerciseButtonClicked, uiLayer);
        ui.restButton = CreateCampButton(btnGroup.transform, "RestBtn", "REST (+25 HP)\n(+10% Risk)", new Vector2(-230, -30), new Vector2(210, 48), new Color(0.2f, 0.55f, 0.6f), ui.OnRestButtonClicked, uiLayer);
        ui.continueButton = CreateCampButton(btnGroup.transform, "ContinueBtn", "CONTINUE FORWARD\n(Next Stage)", new Vector2(0, -30), new Vector2(210, 48), new Color(0.3f, 0.7f, 0.3f), ui.OnContinueButtonClicked, uiLayer);
        ui.returnHomeButton = CreateCampButton(btnGroup.transform, "ReturnBtn", "RETURN HOME\n(Keep All Loot)", new Vector2(230, -30), new Vector2(210, 48), new Color(0.45f, 0.3f, 0.6f), ui.OnReturnHomeButtonClicked, uiLayer);

        // Ambush Alert Banner
        ui.ambushAlertBanner = new GameObject("AmbushAlertBanner", typeof(RectTransform), typeof(Image));
        ui.ambushAlertBanner.layer = uiLayer;
        ui.ambushAlertBanner.transform.SetParent(mainPanel.transform, false);
        RectTransform alertRT = ui.ambushAlertBanner.GetComponent<RectTransform>();
        alertRT.anchorMin = new Vector2(0, 0.5f);
        alertRT.anchorMax = new Vector2(1, 0.5f);
        alertRT.sizeDelta = new Vector2(0, 90);
        alertRT.anchoredPosition = Vector2.zero;
        Image alertImg = ui.ambushAlertBanner.GetComponent<Image>();
        alertImg.color = new Color(0.85f, 0.15f, 0.15f, 0.98f);

        GameObject alertTextGO = new GameObject("AlertText", typeof(RectTransform), typeof(TextMeshProUGUI));
        alertTextGO.layer = uiLayer;
        alertTextGO.transform.SetParent(ui.ambushAlertBanner.transform, false);
        RectTransform alertTextRT = alertTextGO.GetComponent<RectTransform>();
        alertTextRT.anchorMin = Vector2.zero;
        alertTextRT.anchorMax = Vector2.one;
        alertTextRT.sizeDelta = Vector2.zero;
        ui.ambushAlertText = alertTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.ambushAlertText.font = font;
        ui.ambushAlertText.fontSize = 20;
        ui.ambushAlertText.fontStyle = FontStyles.Bold;
        ui.ambushAlertText.alignment = TextAlignmentOptions.Center;
        ui.ambushAlertText.text = "AMBUSH! Monsters detected your campsite!";
        ui.ambushAlertText.color = Color.white;
        ui.ambushAlertBanner.SetActive(false);

        // Initial state: main panel inactive
        mainPanel.SetActive(false);

        // 3. Modals
        BuildFoodModal(ui, canvasGO.transform, uiLayer);
        BuildCookModal(ui, canvasGO.transform, uiLayer);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DungeonPreparationCanvasBuilder] CampCanvas built & saved successfully.");
        return canvasGO;
    }

    private static Button CreateCampButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction action, int uiLayer)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = uiLayer;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        Button btn = go.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(btn, img, color, color * 1.15f, color * 0.85f);
        if (action != null)
        {
            UnityEventTools.AddPersistentListener(btn.onClick, action);
        }

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.layer = uiLayer;
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 12;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = label;
        tmp.color = Color.white;

        return btn;
    }

    private static void BuildFoodModal(CampUI ui, Transform parent, int uiLayer)
    {
        ui.foodModal = new GameObject("FoodModal", typeof(RectTransform), typeof(Image));
        ui.foodModal.layer = uiLayer;
        ui.foodModal.transform.SetParent(parent, false);
        RectTransform rt = ui.foodModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520, 440);
        rt.anchoredPosition = Vector2.zero;
        Image bg = ui.foodModal.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(ui.foodModal.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI tmp = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "SELECT FOOD TO EAT";

        // Scroll View
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = uiLayer;
        scrollGO.transform.SetParent(ui.foodModal.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(16, 60);
        scrollRT.offsetMax = new Vector2(-16, -48);

        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        // Viewport with RectMask2D
        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = uiLayer;
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-16, 0);

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = uiLayer;
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT;
        sr.content = contentRT;
        ui.foodListContainer = contentGO.transform;

        // Vertical Scrollbar
        GameObject sbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.layer = uiLayer;
        sbGO.transform.SetParent(scrollGO.transform, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(12, 0);
        sbRT.anchoredPosition = Vector2.zero;

        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject saGO = new GameObject("SlidingArea", typeof(RectTransform));
        saGO.layer = uiLayer;
        saGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = saGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.sizeDelta = Vector2.zero;

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.layer = uiLayer;
        handleGO.transform.SetParent(saGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(handleImg, new Color(0.2f, 0.6f, 0.35f, 1f));

        sb.handleRect = handleRT;
        sb.targetGraphic = handleImg;
        sr.verticalScrollbar = sb;

        // No Food Text
        GameObject noFoodGO = new GameObject("NoFoodText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noFoodGO.layer = uiLayer;
        noFoodGO.transform.SetParent(ui.foodModal.transform, false);
        RectTransform noFoodRT = noFoodGO.GetComponent<RectTransform>();
        noFoodRT.anchoredPosition = new Vector2(0, 10);
        noFoodRT.sizeDelta = new Vector2(400, 60);
        ui.noFoodText = noFoodGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.noFoodText.font = font;
        ui.noFoodText.fontSize = 13.5f;
        ui.noFoodText.alignment = TextAlignmentOptions.Center;
        ui.noFoodText.text = "No food in backpack!\n(Cook meals or harvest food from farm)";
        ui.noFoodText.color = new Color(0.75f, 0.75f, 0.8f);

        // Close Button
        ui.closeFoodModalButton = CreateCampButton(ui.foodModal.transform, "CloseBtn", "CLOSE", new Vector2(0, -188), new Vector2(130, 36), new Color(0.4f, 0.4f, 0.45f), ui.CloseFoodModal, uiLayer);
        ui.foodModal.SetActive(false);
    }

    private static void BuildCookModal(CampUI ui, Transform parent, int uiLayer)
    {
        ui.cookModal = new GameObject("CookModal", typeof(RectTransform), typeof(Image));
        ui.cookModal.layer = uiLayer;
        ui.cookModal.transform.SetParent(parent, false);
        RectTransform rt = ui.cookModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520, 440);
        rt.anchoredPosition = Vector2.zero;
        Image bg = ui.cookModal.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(ui.cookModal.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI tmp = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "SELECT RECIPE TO COOK";

        // Scroll View
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = uiLayer;
        scrollGO.transform.SetParent(ui.cookModal.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(16, 60);
        scrollRT.offsetMax = new Vector2(-16, -48);

        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        // Viewport with RectMask2D
        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = uiLayer;
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-16, 0);

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = uiLayer;
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT;
        sr.content = contentRT;
        ui.recipeListContainer = contentGO.transform;

        // Vertical Scrollbar
        GameObject sbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.layer = uiLayer;
        sbGO.transform.SetParent(scrollGO.transform, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(12, 0);
        sbRT.anchoredPosition = Vector2.zero;

        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject saGO = new GameObject("SlidingArea", typeof(RectTransform));
        saGO.layer = uiLayer;
        saGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = saGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.sizeDelta = Vector2.zero;

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.layer = uiLayer;
        handleGO.transform.SetParent(saGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(handleImg, new Color(0.8f, 0.45f, 0.15f, 1f));

        sb.handleRect = handleRT;
        sb.targetGraphic = handleImg;
        sr.verticalScrollbar = sb;

        // No Recipe Text
        GameObject noRecGO = new GameObject("NoRecipeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noRecGO.layer = uiLayer;
        noRecGO.transform.SetParent(ui.cookModal.transform, false);
        RectTransform noRecRT = noRecGO.GetComponent<RectTransform>();
        noRecRT.anchoredPosition = new Vector2(0, 10);
        noRecRT.sizeDelta = new Vector2(400, 60);
        ui.noRecipeText = noRecGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.noRecipeText.font = font;
        ui.noRecipeText.fontSize = 13.5f;
        ui.noRecipeText.alignment = TextAlignmentOptions.Center;
        ui.noRecipeText.text = "No recipes discovered yet!\n(Meet Merchants or offer gifts to unlock recipes)";
        ui.noRecipeText.color = new Color(0.75f, 0.75f, 0.8f);

        // Close Button
        ui.closeCookModalButton = CreateCampButton(ui.cookModal.transform, "CloseBtn", "CLOSE", new Vector2(0, -188), new Vector2(130, 36), new Color(0.4f, 0.4f, 0.45f), ui.CloseCookModal, uiLayer);
        ui.cookModal.SetActive(false);
    }
}
