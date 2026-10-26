using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Events Canvases:
/// 1. DungeonEventCanvas (Slay the Spire style Mystery / Shrine / Fountain / Encounter events)
/// 2. MerchantCanvas (Wandering Merchant Dungeon Outpost trade interface)
/// directly inside Dungeon.unity as scene-authored Canvases.
/// Matches 100% the layout, sizes, colors, fonts, hierarchy, and visuals of existing UI builders.
/// All button events are wired as persistent listeners.
/// </summary>
public static class DungeonEventsCanvasBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Dungeon.unity";
    private const string EVENT_CANVAS_NAME = "DungeonEventCanvas";
    private const string MERCHANT_CANVAS_NAME = "MerchantCanvas";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SLOT_PREFAB_PATH = "Assets/_Assets/_Prefabs/inventorySlot.prefab";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build Both Event Canvases (Dungeon)")]
    public static void BuildBothEventCanvases()
    {
        BuildDungeonEventCanvas();
        BuildMerchantCanvas();
        Debug.Log("[DungeonEventsCanvasBuilder] SUCCESS: Built both DungeonEventCanvas and MerchantCanvas in Dungeon.unity!");
    }

    [MenuItem("Tools/Build Dungeon Event Canvas")]
    public static GameObject BuildDungeonEventCanvasMenu()
    {
        return BuildDungeonEventCanvas();
    }

    [MenuItem("Tools/Build Merchant Canvas")]
    public static GameObject BuildMerchantCanvasMenu()
    {
        return BuildMerchantCanvas();
    }

    private static void EnsureFontLoaded()
    {
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null) font = DungeonUIAssetHelper.GetFontAsset();
        }
    }

    public static GameObject BuildDungeonEventCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any empty/unbound DungeonEventUI on CombatCanvas
        GameObject combatCanvasGO = GameObject.Find("CombatCanvas");
        if (combatCanvasGO != null)
        {
            DungeonEventUI[] combatEventUIs = combatCanvasGO.GetComponents<DungeonEventUI>();
            for (int i = 0; i < combatEventUIs.Length; i++)
            {
                if (combatEventUIs[i] != null)
                {
                    Undo.DestroyObjectImmediate(combatEventUIs[i]);
                    Debug.Log("[DungeonEventsCanvasBuilder] Removed unassigned DungeonEventUI component from CombatCanvas.");
                }
            }
        }

        // Clean up any existing DungeonEventCanvas
        GameObject existingCanvas = GameObject.Find(EVENT_CANVAS_NAME);
        if (existingCanvas != null)
        {
            Undo.DestroyObjectImmediate(existingCanvas);
        }

        EnsureFontLoaded();

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(EVENT_CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Dungeon Event Canvas");
        canvasGO.layer = uiLayer;
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 115; // Above CombatCanvas (100) & EntranceArrow (105)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        DungeonEventUI ui = canvasGO.AddComponent<DungeonEventUI>();
        ui.canvas = canvas;

        // 2. Fullscreen Dark Overlay (DungeonEventPanel)
        GameObject overlayGO = new GameObject("DungeonEventPanel", typeof(RectTransform), typeof(Image));
        overlayGO.layer = uiLayer;
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        ui.eventPanel = overlayGO;

        // 3. Central Window Modal (760 x 520)
        GameObject modalGO = new GameObject("EventModal", typeof(RectTransform), typeof(Image));
        modalGO.layer = uiLayer;
        modalGO.transform.SetParent(overlayGO.transform, false);
        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(760, 520);
        modalRT.anchoredPosition = Vector2.zero;

        Image modalImg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.12f, 0.13f, 0.18f, 0.98f));

        // 4. Header Banner
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGO.layer = uiLayer;
        headerGO.transform.SetParent(modalGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 68);
        headerRT.anchoredPosition = Vector2.zero;

        Image headerImg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.20f, 0.15f, 0.28f, 1f));

        // Category Text
        GameObject catGO = new GameObject("CategoryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        catGO.layer = uiLayer;
        catGO.transform.SetParent(headerGO.transform, false);
        RectTransform catRT = catGO.GetComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 1);
        catRT.anchorMax = new Vector2(1, 1);
        catRT.offsetMin = new Vector2(24, -26);
        catRT.offsetMax = new Vector2(-24, -6);

        ui.categoryText = catGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.categoryText.font = font;
        ui.categoryText.fontSize = 12f;
        ui.categoryText.fontStyle = FontStyles.Bold;
        ui.categoryText.alignment = TextAlignmentOptions.MidlineLeft;
        ui.categoryText.text = "[ EVENT ]";

        // Title Text
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(24, 6);
        titleRT.offsetMax = new Vector2(-24, -28);

        ui.titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.titleText.font = font;
        ui.titleText.fontSize = 19f;
        ui.titleText.fontStyle = FontStyles.Bold;
        ui.titleText.color = new Color(1f, 0.88f, 0.25f);
        ui.titleText.alignment = TextAlignmentOptions.MidlineLeft;
        ui.titleText.text = "EVENT TITLE";

        // 5. Narrative Box
        GameObject narrativeBoxGO = new GameObject("NarrativeBox", typeof(RectTransform), typeof(Image));
        narrativeBoxGO.layer = uiLayer;
        narrativeBoxGO.transform.SetParent(modalGO.transform, false);
        RectTransform narrBoxRT = narrativeBoxGO.GetComponent<RectTransform>();
        narrBoxRT.anchorMin = new Vector2(0, 0.52f);
        narrBoxRT.anchorMax = new Vector2(1, 1);
        narrBoxRT.offsetMin = new Vector2(24, 0);
        narrBoxRT.offsetMax = new Vector2(-24, -76);

        Image narrBoxImg = narrativeBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(narrBoxImg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        GameObject narrTextGO = new GameObject("NarrativeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        narrTextGO.layer = uiLayer;
        narrTextGO.transform.SetParent(narrativeBoxGO.transform, false);
        RectTransform narrTextRT = narrTextGO.GetComponent<RectTransform>();
        narrTextRT.anchorMin = Vector2.zero;
        narrTextRT.anchorMax = Vector2.one;
        narrTextRT.offsetMin = new Vector2(16, 12);
        narrTextRT.offsetMax = new Vector2(-16, -12);

        ui.narrativeText = narrTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.narrativeText.font = font;
        ui.narrativeText.fontSize = 13.5f;
        ui.narrativeText.lineSpacing = 6f;
        ui.narrativeText.color = new Color(0.92f, 0.94f, 0.98f);
        ui.narrativeText.alignment = TextAlignmentOptions.TopLeft;
        ui.narrativeText.text = "Event narrative text...";

        // 6. Choices Container
        GameObject choicesGO = new GameObject("ChoicesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        choicesGO.layer = uiLayer;
        choicesGO.transform.SetParent(modalGO.transform, false);
        ui.choicesContainer = choicesGO.GetComponent<RectTransform>();
        ui.choicesContainer.anchorMin = new Vector2(0, 0);
        ui.choicesContainer.anchorMax = new Vector2(1, 0.50f);
        ui.choicesContainer.offsetMin = new Vector2(24, 52);
        ui.choicesContainer.offsetMax = new Vector2(-24, 0);

        VerticalLayoutGroup vlg = choicesGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 7. Footer: StatusFeedbackText & ContinueButton
        GameObject feedbackGO = new GameObject("StatusFeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.layer = uiLayer;
        feedbackGO.transform.SetParent(modalGO.transform, false);
        RectTransform fbRT = feedbackGO.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0, 0);
        fbRT.anchorMax = new Vector2(0.68f, 0);
        fbRT.offsetMin = new Vector2(24, 12);
        fbRT.offsetMax = new Vector2(0, 48);

        ui.statusFeedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.statusFeedbackText.font = font;
        ui.statusFeedbackText.fontSize = 12.5f;
        ui.statusFeedbackText.alignment = TextAlignmentOptions.MidlineLeft;
        ui.statusFeedbackText.text = "";

        GameObject btnGO = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.layer = uiLayer;
        btnGO.transform.SetParent(modalGO.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0);
        btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(1, 0);
        btnRT.anchoredPosition = new Vector2(-24, 12);
        btnRT.sizeDelta = new Vector2(160, 38);

        Image btnImg = btnGO.GetComponent<Image>();
        ui.continueButton = btnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(ui.continueButton, btnImg, new Color(0.18f, 0.55f, 0.28f));
        UnityEventTools.AddPersistentListener(ui.continueButton.onClick, ui.OnContinueClicked);

        GameObject btnTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGO.layer = uiLayer;
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTxt = btnTxtGO.GetComponent<TextMeshProUGUI>();
        if (font != null) btnTxt.font = font;
        btnTxt.text = "CONTINUE [->]";
        btnTxt.fontSize = 13f;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;

        // Initial state: inactive
        ui.continueButton.gameObject.SetActive(false);
        overlayGO.SetActive(false);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DungeonEventsCanvasBuilder] DungeonEventCanvas built & saved successfully.");
        return canvasGO;
    }

    public static GameObject BuildMerchantCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any empty/unbound MerchantUI on CombatCanvas
        GameObject combatCanvasGO = GameObject.Find("CombatCanvas");
        if (combatCanvasGO != null)
        {
            MerchantUI[] combatMerchantUIs = combatCanvasGO.GetComponents<MerchantUI>();
            for (int i = 0; i < combatMerchantUIs.Length; i++)
            {
                if (combatMerchantUIs[i] != null)
                {
                    Undo.DestroyObjectImmediate(combatMerchantUIs[i]);
                    Debug.Log("[DungeonEventsCanvasBuilder] Removed unassigned MerchantUI component from CombatCanvas.");
                }
            }
        }

        // Clean up any existing MerchantCanvas
        GameObject existingCanvas = GameObject.Find(MERCHANT_CANVAS_NAME);
        if (existingCanvas != null)
        {
            Undo.DestroyObjectImmediate(existingCanvas);
        }

        EnsureFontLoaded();

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(MERCHANT_CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Merchant Canvas");
        canvasGO.layer = uiLayer;
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130; // Sits above all other dialogs
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        MerchantUI ui = canvasGO.AddComponent<MerchantUI>();
        ui.canvas = canvas;

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
        if (slotPrefab == null) slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
        ui.slotPrefab = slotPrefab;

        // 2. Fullscreen Dark Overlay (MerchantPanel)
        GameObject overlayGO = new GameObject("MerchantPanel", typeof(RectTransform), typeof(Image));
        overlayGO.layer = uiLayer;
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        ui.merchantPanel = overlayGO;

        // 3. Central Window Panel (980 x 600)
        GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.layer = uiLayer;
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(980, 600);
        windowRT.anchoredPosition = Vector2.zero;

        Image windowBg = windowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(windowBg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 4. Header: Title, Subtitle, Feedback
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(windowGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -14);
        titleRT.sizeDelta = new Vector2(-60, 32);

        ui.titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.titleText.font = font;
        ui.titleText.fontSize = 21;
        ui.titleText.fontStyle = FontStyles.Bold;
        ui.titleText.alignment = TextAlignmentOptions.Center;
        ui.titleText.text = "WANDERING MERCHANT - DUNGEON OUTPOST";
        ui.titleText.color = new Color(1f, 0.85f, 0.35f);

        GameObject subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.layer = uiLayer;
        subtitleGO.transform.SetParent(windowGO.transform, false);
        RectTransform subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0, 1);
        subtitleRT.anchorMax = new Vector2(1, 1);
        subtitleRT.pivot = new Vector2(0.5f, 1);
        subtitleRT.anchoredPosition = new Vector2(0, -44);
        subtitleRT.sizeDelta = new Vector2(-60, 22);

        ui.subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.subtitleText.font = font;
        ui.subtitleText.fontSize = 12.5f;
        ui.subtitleText.alignment = TextAlignmentOptions.Center;
        ui.subtitleText.text = "A mysterious trader braving the dungeon. Barter seeds, recipes, and survival rations.";
        ui.subtitleText.color = new Color(0.82f, 0.86f, 0.94f);

        GameObject feedbackGO = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.layer = uiLayer;
        feedbackGO.transform.SetParent(windowGO.transform, false);
        RectTransform feedbackRT = feedbackGO.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 1);
        feedbackRT.anchorMax = new Vector2(1, 1);
        feedbackRT.pivot = new Vector2(0.5f, 1);
        feedbackRT.anchoredPosition = new Vector2(0, -68);
        feedbackRT.sizeDelta = new Vector2(-60, 24);

        ui.feedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.feedbackText.font = font;
        ui.feedbackText.fontSize = 13;
        ui.feedbackText.fontStyle = FontStyles.Bold;
        ui.feedbackText.alignment = TextAlignmentOptions.Center;
        ui.feedbackText.text = "Welcome traveler! What would you like to barter today?";
        ui.feedbackText.color = new Color(0.88f, 0.94f, 1f);

        // 5. Close Button (X)
        GameObject closeGO = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.layer = uiLayer;
        closeGO.transform.SetParent(windowGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-14, -14);
        closeRT.sizeDelta = new Vector2(32, 32);

        Image closeBg = closeGO.GetComponent<Image>();
        ui.closeButton = closeGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(ui.closeButton, closeBg, new Color(0.6f, 0.18f, 0.18f, 1f), new Color(0.85f, 0.25f, 0.25f, 1f));
        UnityEventTools.AddPersistentListener(ui.closeButton.onClick, ui.Close);

        GameObject xTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.layer = uiLayer;
        xTextGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI xTMP = xTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) xTMP.font = font;
        xTMP.fontSize = 16;
        xTMP.fontStyle = FontStyles.Bold;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.text = "X";
        xTMP.color = Color.white;
        xTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32);

        // 6. Left Column: Merchant Wares (Scrollable)
        GameObject waresColGO = new GameObject("WaresColumn", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        waresColGO.layer = uiLayer;
        waresColGO.transform.SetParent(windowGO.transform, false);
        RectTransform waresColRT = waresColGO.GetComponent<RectTransform>();
        waresColRT.anchorMin = new Vector2(0, 0);
        waresColRT.anchorMax = new Vector2(0, 1);
        waresColRT.pivot = new Vector2(0, 0.5f);
        waresColRT.anchoredPosition = new Vector2(20, -18);
        waresColRT.sizeDelta = new Vector2(530, -140);

        Image waresColBg = waresColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(waresColBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.layer = uiLayer;
        viewportGO.transform.SetParent(waresColGO.transform, false);
        RectTransform vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(6, 6);
        vpRT.offsetMax = new Vector2(-22, -6);

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = uiLayer;
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = waresColGO.GetComponent<ScrollRect>();
        sr.viewport = vpRT;
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        GameObject waresSbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        waresSbGO.layer = uiLayer;
        waresSbGO.transform.SetParent(waresColGO.transform, false);
        RectTransform waresSbRT = waresSbGO.GetComponent<RectTransform>();
        waresSbRT.anchorMin = new Vector2(1, 0);
        waresSbRT.anchorMax = new Vector2(1, 1);
        waresSbRT.pivot = new Vector2(1, 0.5f);
        waresSbRT.sizeDelta = new Vector2(12, -12);
        waresSbRT.anchoredPosition = new Vector2(-6, 0);

        Image waresSbBg = waresSbGO.GetComponent<Image>();
        waresSbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar waresSb = waresSbGO.GetComponent<Scrollbar>();
        waresSb.direction = Scrollbar.Direction.BottomToTop;

        GameObject waresSaGO = new GameObject("SlidingArea", typeof(RectTransform));
        waresSaGO.layer = uiLayer;
        waresSaGO.transform.SetParent(waresSbGO.transform, false);
        RectTransform waresSaRT = waresSaGO.GetComponent<RectTransform>();
        waresSaRT.anchorMin = Vector2.zero;
        waresSaRT.anchorMax = Vector2.one;
        waresSaRT.sizeDelta = Vector2.zero;

        GameObject waresHandleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        waresHandleGO.layer = uiLayer;
        waresHandleGO.transform.SetParent(waresSaGO.transform, false);
        RectTransform waresHandleRT = waresHandleGO.GetComponent<RectTransform>();
        waresHandleRT.sizeDelta = Vector2.zero;

        Image waresHandleImg = waresHandleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(waresHandleImg, new Color(0.85f, 0.65f, 0.2f, 1f));

        waresSb.handleRect = waresHandleRT;
        waresSb.targetGraphic = waresHandleImg;
        sr.verticalScrollbar = waresSb;

        ui.waresContainer = contentGO.transform;

        // 7. Right Column: Backpack Section
        GameObject backpackColGO = new GameObject("BackpackColumn", typeof(RectTransform), typeof(Image));
        backpackColGO.layer = uiLayer;
        backpackColGO.transform.SetParent(windowGO.transform, false);
        RectTransform bpColRT = backpackColGO.GetComponent<RectTransform>();
        bpColRT.anchorMin = new Vector2(1, 0);
        bpColRT.anchorMax = new Vector2(1, 1);
        bpColRT.pivot = new Vector2(1, 0.5f);
        bpColRT.anchoredPosition = new Vector2(-20, -18);
        bpColRT.sizeDelta = new Vector2(390, -140);

        Image bpColBg = backpackColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bpColBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        GameObject bpHeaderGO = new GameObject("BackpackHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpHeaderGO.layer = uiLayer;
        bpHeaderGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpHeaderRT = bpHeaderGO.GetComponent<RectTransform>();
        bpHeaderRT.anchorMin = new Vector2(0, 1);
        bpHeaderRT.anchorMax = new Vector2(1, 1);
        bpHeaderRT.pivot = new Vector2(0.5f, 1);
        bpHeaderRT.anchoredPosition = new Vector2(0, -8);
        bpHeaderRT.sizeDelta = new Vector2(-20, 26);

        ui.backpackHeaderTitle = bpHeaderGO.GetComponent<TextMeshProUGUI>();
        if (font != null) ui.backpackHeaderTitle.font = font;
        ui.backpackHeaderTitle.fontSize = 13.5f;
        ui.backpackHeaderTitle.fontStyle = FontStyles.Bold;
        ui.backpackHeaderTitle.alignment = TextAlignmentOptions.Center;
        ui.backpackHeaderTitle.text = "PLAYER BACKPACK";
        ui.backpackHeaderTitle.color = new Color(0.4f, 0.85f, 1f);

        GameObject bpScrollGO = new GameObject("BackpackScrollArea", typeof(RectTransform), typeof(ScrollRect));
        bpScrollGO.layer = uiLayer;
        bpScrollGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpScrollRT = bpScrollGO.GetComponent<RectTransform>();
        bpScrollRT.anchorMin = Vector2.zero;
        bpScrollRT.anchorMax = Vector2.one;
        bpScrollRT.offsetMin = new Vector2(8, 8);
        bpScrollRT.offsetMax = new Vector2(-8, -38);

        ScrollRect bpScrollRect = bpScrollGO.GetComponent<ScrollRect>();
        bpScrollRect.horizontal = false;
        bpScrollRect.vertical = true;
        bpScrollRect.movementType = ScrollRect.MovementType.Clamped;
        bpScrollRect.scrollSensitivity = 25f;

        GameObject bpVpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        bpVpGO.layer = uiLayer;
        bpVpGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform bpVpRT = bpVpGO.GetComponent<RectTransform>();
        bpVpRT.anchorMin = Vector2.zero;
        bpVpRT.anchorMax = Vector2.one;
        bpVpRT.offsetMin = Vector2.zero;
        bpVpRT.offsetMax = new Vector2(-16, 0);

        GameObject bpGridGO = new GameObject("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        bpGridGO.layer = uiLayer;
        bpGridGO.transform.SetParent(bpVpGO.transform, false);
        RectTransform bpGridRT = bpGridGO.GetComponent<RectTransform>();
        bpGridRT.anchorMin = new Vector2(0, 1);
        bpGridRT.anchorMax = new Vector2(1, 1);
        bpGridRT.pivot = new Vector2(0.5f, 1);
        bpGridRT.anchoredPosition = Vector2.zero;
        bpGridRT.sizeDelta = Vector2.zero;

        GridLayoutGroup bpGLG = bpGridGO.GetComponent<GridLayoutGroup>();
        bpGLG.cellSize = new Vector2(50, 50);
        bpGLG.spacing = new Vector2(7, 7);
        bpGLG.padding = new RectOffset(6, 6, 6, 6);
        bpGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGLG.constraintCount = 6;
        bpGLG.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter bpCsf = bpGridGO.GetComponent<ContentSizeFitter>();
        bpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bpScrollRect.viewport = bpVpRT;
        bpScrollRect.content = bpGridRT;

        GameObject bpSbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        bpSbGO.layer = uiLayer;
        bpSbGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform bpSbRT = bpSbGO.GetComponent<RectTransform>();
        bpSbRT.anchorMin = new Vector2(1, 0);
        bpSbRT.anchorMax = new Vector2(1, 1);
        bpSbRT.pivot = new Vector2(1, 0.5f);
        bpSbRT.sizeDelta = new Vector2(12, 0);
        bpSbRT.anchoredPosition = Vector2.zero;

        Image bpSbBg = bpSbGO.GetComponent<Image>();
        bpSbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar bpSb = bpSbGO.GetComponent<Scrollbar>();
        bpSb.direction = Scrollbar.Direction.BottomToTop;

        GameObject bpSaGO = new GameObject("SlidingArea", typeof(RectTransform));
        bpSaGO.layer = uiLayer;
        bpSaGO.transform.SetParent(bpSbGO.transform, false);
        RectTransform bpSaRT = bpSaGO.GetComponent<RectTransform>();
        bpSaRT.anchorMin = Vector2.zero;
        bpSaRT.anchorMax = Vector2.one;
        bpSaRT.sizeDelta = Vector2.zero;

        GameObject bpHandleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        bpHandleGO.layer = uiLayer;
        bpHandleGO.transform.SetParent(bpSaGO.transform, false);
        RectTransform bpHandleRT = bpHandleGO.GetComponent<RectTransform>();
        bpHandleRT.sizeDelta = Vector2.zero;

        Image bpHandleImg = bpHandleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bpHandleImg, new Color(0.25f, 0.42f, 0.65f, 1f));

        bpSb.handleRect = bpHandleRT;
        bpSb.targetGraphic = bpHandleImg;
        bpScrollRect.verticalScrollbar = bpSb;

        ui.backpackSlotsParent = bpGridGO.transform;

        // 8. Footer: Continue Journey Button
        GameObject continueGO = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
        continueGO.layer = uiLayer;
        continueGO.transform.SetParent(windowGO.transform, false);
        RectTransform continueRT = continueGO.GetComponent<RectTransform>();
        continueRT.anchorMin = new Vector2(0.5f, 0);
        continueRT.anchorMax = new Vector2(0.5f, 0);
        continueRT.pivot = new Vector2(0.5f, 0);
        continueRT.anchoredPosition = new Vector2(0, 12);
        continueRT.sizeDelta = new Vector2(300, 42);

        Image continueBg = continueGO.GetComponent<Image>();
        ui.continueButton = continueGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(ui.continueButton, continueBg, new Color(0.18f, 0.55f, 0.32f, 1f), new Color(0.24f, 0.70f, 0.40f, 1f));
        UnityEventTools.AddPersistentListener(ui.continueButton.onClick, ui.Close);

        GameObject continueTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        continueTextGO.layer = uiLayer;
        continueTextGO.transform.SetParent(continueGO.transform, false);
        TextMeshProUGUI continueTMP = continueTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) continueTMP.font = font;
        continueTMP.fontSize = 13.5f;
        continueTMP.fontStyle = FontStyles.Bold;
        continueTMP.alignment = TextAlignmentOptions.Center;
        continueTMP.text = "CONTINUE EXPEDITION >>";
        continueTMP.color = Color.white;
        continueTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 42);

        // Initial state: inactive
        overlayGO.SetActive(false);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DungeonEventsCanvasBuilder] MerchantCanvas built & saved successfully.");
        return canvasGO;
    }
}
