using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Statue/Offering Canvas
/// directly inside Base.unity as a scene-authored Canvas.
/// Matches 100% the layout, sizes, colors, fonts, hierarchy, and visuals of StatueUI.
/// All button events are wired as persistent listeners.
/// </summary>
public static class StatueCanvasBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Base.unity";
    private const string CANVAS_NAME = "StatueCanvas";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SLOT_PREFAB_PATH = "Assets/_Assets/_Prefabs/inventorySlot.prefab";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build Statue Canvas")]
    public static void BuildStatueCanvasMenu()
    {
        BuildStatueCanvas();
    }

    public static GameObject BuildStatueCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any legacy or duplicate StatueUI objects in the scene
        StatueUI[] existingUIs = Object.FindObjectsByType<StatueUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existingUIs.Length; i++)
        {
            if (existingUIs[i] != null)
            {
                Undo.DestroyObjectImmediate(existingUIs[i].gameObject);
            }
        }

        // Also clean up any orphan StatueCanvas object
        GameObject orphan = GameObject.Find(CANVAS_NAME);
        if (orphan != null)
        {
            Undo.DestroyObjectImmediate(orphan);
        }

        // Load font & slot prefab
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) font = DungeonUIAssetHelper.GetFontAsset();

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
        if (slotPrefab == null) slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Statue Canvas");
        canvasGO.layer = uiLayer;
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 22; // Sits above Kitchen (20), below DungeonEntrance (25) & MainMenu (100)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        StatueUI ui = canvasGO.AddComponent<StatueUI>();

        // 2. Fullscreen Dark Overlay (StatuePanel)
        GameObject overlayGO = new GameObject("StatuePanel", typeof(RectTransform), typeof(Image));
        overlayGO.layer = uiLayer;
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        StretchFull(overlayRT);

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
        overlayImg.raycastTarget = true;

        // 3. Central Window Panel (1100 x 660)
        GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.layer = uiLayer;
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(1100, 660);
        windowRT.anchoredPosition = Vector2.zero;

        Image windowBg = windowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(windowBg, DungeonUIAssetHelper.PanelStyle.Wood);

        // 4. Header: Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(windowGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(-100, 32);

        TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "ANCIENT SHRINE (DIVINE BLESSING STATUE)";
        titleText.color = new Color(1f, 0.86f, 0.35f);

        // 5. Header: Subtitle
        GameObject subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.layer = uiLayer;
        subtitleGO.transform.SetParent(windowGO.transform, false);
        RectTransform subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0, 1);
        subtitleRT.anchorMax = new Vector2(1, 1);
        subtitleRT.pivot = new Vector2(0.5f, 1);
        subtitleRT.anchoredPosition = new Vector2(0, -50);
        subtitleRT.sizeDelta = new Vector2(-100, 20);

        TextMeshProUGUI subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(subtitleText);
        subtitleText.fontSize = 12;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "Make sacred offerings to gain permanent stats and unlock tactical build paths.";
        subtitleText.color = new Color(0.75f, 0.82f, 0.92f);

        // 6. Sub-Header Bar (Blessing Banner)
        GameObject blessingBannerGO = new GameObject("BlessingBanner", typeof(RectTransform), typeof(Image));
        blessingBannerGO.layer = uiLayer;
        blessingBannerGO.transform.SetParent(windowGO.transform, false);
        RectTransform bannerRT = blessingBannerGO.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0, 1);
        bannerRT.anchorMax = new Vector2(1, 1);
        bannerRT.pivot = new Vector2(0.5f, 1);
        bannerRT.anchoredPosition = new Vector2(0, -76);
        bannerRT.sizeDelta = new Vector2(-48, 34);

        Image bannerBg = blessingBannerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(bannerBg, DungeonUIAssetHelper.PlaqueStyle.Gold);

        // Blessing Status Text (left side of bar)
        GameObject bStatusGO = new GameObject("BlessingStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bStatusGO.layer = uiLayer;
        bStatusGO.transform.SetParent(blessingBannerGO.transform, false);
        RectTransform bStatusRT = bStatusGO.GetComponent<RectTransform>();
        bStatusRT.anchorMin = new Vector2(0, 0);
        bStatusRT.anchorMax = new Vector2(0.79f, 1);
        bStatusRT.offsetMin = new Vector2(14, 0);
        bStatusRT.offsetMax = new Vector2(-6, 0);

        TextMeshProUGUI blessingStatusText = bStatusGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(blessingStatusText);
        blessingStatusText.fontSize = 11.5f;
        blessingStatusText.alignment = TextAlignmentOptions.MidlineLeft;
        blessingStatusText.text = "<b><color=#FFCC44>[NO BLESSING CHOSEN]</color></b> Choose a blessing to empower your run before entering the Dungeon!";
        blessingStatusText.color = new Color(0.9f, 0.95f, 1f);

        // Blessing Selection Button (right side of bar)
        GameObject bBtnGO = new GameObject("BtnSelectBlessing", typeof(RectTransform), typeof(Image), typeof(Button));
        bBtnGO.layer = uiLayer;
        bBtnGO.transform.SetParent(blessingBannerGO.transform, false);
        RectTransform bBtnRT = bBtnGO.GetComponent<RectTransform>();
        bBtnRT.anchorMin = new Vector2(0.80f, 0);
        bBtnRT.anchorMax = new Vector2(1, 1);
        bBtnRT.offsetMin = new Vector2(4, 3);
        bBtnRT.offsetMax = new Vector2(-6, -3);

        Image bBtnBg = bBtnGO.GetComponent<Image>();
        Button blessingSelectButton = bBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(blessingSelectButton, bBtnBg, DungeonUIAssetHelper.ButtonStyle.Victory);
        UnityEventTools.AddPersistentListener(blessingSelectButton.onClick, ui.ShowBlessingSelectionModal);

        GameObject bBtnTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        bBtnTxtGO.layer = uiLayer;
        bBtnTxtGO.transform.SetParent(bBtnGO.transform, false);
        StretchFull(bBtnTxtGO.GetComponent<RectTransform>());

        TextMeshProUGUI blessingButtonText = bBtnTxtGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(blessingButtonText);
        blessingButtonText.fontSize = 12;
        blessingButtonText.fontStyle = FontStyles.Bold;
        blessingButtonText.alignment = TextAlignmentOptions.Center;
        blessingButtonText.text = "Change Build";
        blessingButtonText.color = Color.white;

        // 7. Feedback / Hint Strip
        GameObject feedbackGO = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.layer = uiLayer;
        feedbackGO.transform.SetParent(windowGO.transform, false);
        RectTransform feedbackRT = feedbackGO.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 1);
        feedbackRT.anchorMax = new Vector2(1, 1);
        feedbackRT.pivot = new Vector2(0.5f, 1);
        feedbackRT.anchoredPosition = new Vector2(0, -116);
        feedbackRT.sizeDelta = new Vector2(-48, 18);

        TextMeshProUGUI feedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(feedbackText);
        feedbackText.fontSize = 12;
        feedbackText.fontStyle = FontStyles.Italic;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.text = "Drag and drop items from your backpack or click requirement slots to offer.";
        feedbackText.color = new Color(0.85f, 0.92f, 1f);

        // 8. Close Button (X)
        GameObject closeGO = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.layer = uiLayer;
        closeGO.transform.SetParent(windowGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-14, -14);
        closeRT.sizeDelta = new Vector2(30, 30);

        Image closeBg = closeGO.GetComponent<Image>();
        Button closeButton = closeGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(closeButton, closeBg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        UnityEventTools.AddPersistentListener(closeButton.onClick, ui.Close);

        GameObject xTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.layer = uiLayer;
        xTextGO.transform.SetParent(closeGO.transform, false);
        StretchFull(xTextGO.GetComponent<RectTransform>());

        TextMeshProUGUI xTMP = xTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(xTMP);
        xTMP.fontSize = 16;
        xTMP.fontStyle = FontStyles.Bold;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.text = "X";
        xTMP.color = Color.white;

        // 9. Left Column: Offerings Column (610 x 460)
        GameObject offColGO = CreateColumnFrame(windowGO.transform, "OfferingsColumn", "DIVINE OFFERINGS LIST",
            new Vector2(24, -140), new Vector2(610, 460), new Vector2(0, 1), new Color(0.85f, 0.7f, 0.25f), uiLayer);
        Transform offeringsContainer = CreateScrollableList(offColGO.transform, "OfferingsContent", new Color(0.85f, 0.65f, 0.2f), uiLayer);

        // 10. Right Column: Backpack Column (420 x 460)
        GameObject bpColGO = CreateColumnFrame(windowGO.transform, "BackpackColumn", "PLAYER INVENTORY (BACKPACK)",
            new Vector2(-24, -140), new Vector2(420, 460), new Vector2(1, 1), new Color(0.2f, 0.7f, 0.95f), uiLayer);
        Transform backpackSlotsParent = CreateScrollableGrid(bpColGO.transform, "BackpackSlots", 5, new Color(0.25f, 0.75f, 0.95f), uiLayer);

        // 11. Footer: Permanent Stats Summary (Docked at bottom)
        GameObject footerGO = new GameObject("Footer", typeof(RectTransform), typeof(Image));
        footerGO.layer = uiLayer;
        footerGO.transform.SetParent(windowGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 12);
        footerRT.sizeDelta = new Vector2(-48, 36);

        Image footerBg = footerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(footerBg, DungeonUIAssetHelper.PlaqueStyle.Wood);

        GameObject statsTextGO = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statsTextGO.layer = uiLayer;
        statsTextGO.transform.SetParent(footerGO.transform, false);
        StretchFull(statsTextGO.GetComponent<RectTransform>());

        TextMeshProUGUI permanentStatsText = statsTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(permanentStatsText);
        permanentStatsText.fontSize = 13;
        permanentStatsText.fontStyle = FontStyles.Bold;
        permanentStatsText.alignment = TextAlignmentOptions.Center;
        permanentStatsText.text = "<b>Blessed Permanent Stats:</b>   <color=#FF7777>ATK: 10 (+0)</color>   |   <color=#77AAFF>DEF: 0 (+0)</color>   |   <color=#FFFF77>Speed: 10 (+0)</color>";
        permanentStatsText.color = new Color(0.9f, 0.95f, 1f);

        // 12. Deactivate StatuePanel by default
        overlayGO.SetActive(false);

        // 13. Bind Serialized Properties on StatueUI
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("statuePanel").objectReferenceValue = overlayGO;
        so.FindProperty("offeringsContainer").objectReferenceValue = offeringsContainer;
        so.FindProperty("backpackSlotsParent").objectReferenceValue = backpackSlotsParent;
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("subtitleText").objectReferenceValue = subtitleText;
        so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        so.FindProperty("permanentStatsText").objectReferenceValue = permanentStatsText;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.FindProperty("blessingStatusText").objectReferenceValue = blessingStatusText;
        so.FindProperty("blessingSelectButton").objectReferenceValue = blessingSelectButton;
        so.FindProperty("blessingButtonText").objectReferenceValue = blessingButtonText;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvasGO;
        Debug.Log("[StatueCanvasBuilder] StatueCanvas built and saved into Base.unity successfully!");
        return canvasGO;
    }

    private static GameObject CreateColumnFrame(Transform parent, string name, string headerText, Vector2 pos, Vector2 size, Vector2 pivotAndAnchorX, Color headerColor, int layer)
    {
        GameObject colGO = new GameObject(name, typeof(RectTransform), typeof(Image));
        colGO.layer = layer;
        colGO.transform.SetParent(parent, false);
        RectTransform rt = colGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(pivotAndAnchorX.x, 1);
        rt.anchorMax = new Vector2(pivotAndAnchorX.x, 1);
        rt.pivot = pivotAndAnchorX;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = colGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(img, DungeonUIAssetHelper.PanelStyle.Slate);

        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerGO.layer = layer;
        headerGO.transform.SetParent(colGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -10);
        headerRT.sizeDelta = new Vector2(-20, 26);

        TextMeshProUGUI tmp = headerGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = headerText;
        tmp.color = headerColor;

        return colGO;
    }

    private static Transform CreateScrollableList(Transform parent, string name, Color handleColor, int layer)
    {
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = layer;
        scrollGO.transform.SetParent(parent, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -40);

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = layer;
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-20, 0);

        GameObject contentGO = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = layer;
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 8, 8);
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateScrollbar(scrollGO.transform, scrollRect, vpRT, contentRT, handleColor, layer);
        return contentGO.transform;
    }

    private static Transform CreateScrollableGrid(Transform parent, string name, int cols, Color handleColor, int layer)
    {
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = layer;
        scrollGO.transform.SetParent(parent, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -40);

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = layer;
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-20, 0);

        GameObject contentGO = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = layer;
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        GridLayoutGroup glg = contentGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(66, 66);
        glg.spacing = new Vector2(8, 8);
        glg.padding = new RectOffset(6, 6, 8, 8);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = cols;
        glg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateScrollbar(scrollGO.transform, scrollRect, vpRT, contentRT, handleColor, layer);
        return contentGO.transform;
    }

    private static void CreateScrollbar(Transform parent, ScrollRect scrollRect, RectTransform viewport, RectTransform content, Color handleColor, int layer)
    {
        GameObject sbGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.layer = layer;
        sbGO.transform.SetParent(parent, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(14, 0);

        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

        Scrollbar scrollbar = sbGO.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject saGO = new GameObject("Sliding Area", typeof(RectTransform));
        saGO.layer = layer;
        saGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = saGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.offsetMin = new Vector2(2, 4);
        saRT.offsetMax = new Vector2(-2, -4);

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.layer = layer;
        handleGO.transform.SetParent(saGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        handleImg.color = handleColor;

        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp != null && font != null) tmp.font = font;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
