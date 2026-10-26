using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Home Chest Canvas
/// directly inside Base.unity as a scene-authored Canvas and backup prefab.
/// Perfectly styled in authentic pixel-art fantasy style (UI_Frame, UI_Slot, UI_Toolbar).
/// Layout:
/// - 980 x 740 Centered Modal Window
/// - Top Header Bar (Chest Icon, Title, Subtitle, Close Button)
/// - Upper Panel: Rương Nhà (33 slots, 11x3 grid, Sort & Take All buttons, capacity badge)
/// - Middle Divider Bar: Quick Stack, Store All, Take All, Sort All
/// - Lower Panel: Ba Lô Cá Nhân (33 slots, 11x3 grid, Sort & Store All buttons, capacity badge)
/// - Footer Bar: Keyboard & mouse shortcut hints + selected item preview
/// </summary>
public static class ChestCanvasBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Base.unity";
    private const string PREFAB_PATH = "Assets/_Assets/_Prefabs/ChestCanvas.prefab";
    private const string CANVAS_NAME = "ChestCanvas";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SLOT_PREFAB_PATH = "Assets/_Assets/_Prefabs/inventorySlot.prefab";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build Chest Canvas")]
    public static void BuildChestCanvasMenu()
    {
        BuildChestCanvas();
    }

    public static GameObject BuildChestCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any legacy or duplicate ChestUI objects in the scene
        ChestUI[] existingUIs = Object.FindObjectsByType<ChestUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existingUIs.Length; i++)
        {
            if (existingUIs[i] != null)
            {
                Undo.DestroyObjectImmediate(existingUIs[i].gameObject);
            }
        }

        // Clean up orphan ChestCanvas or legacy hudCanvas/ChestPanel
        GameObject orphan = GameObject.Find(CANVAS_NAME);
        if (orphan != null)
        {
            Undo.DestroyObjectImmediate(orphan);
        }

        GameObject hudCanvasGO = GameObject.Find("Canvas");
        if (hudCanvasGO != null)
        {
            Transform legacyPanel = hudCanvasGO.transform.Find("ChestPanel");
            if (legacyPanel != null)
            {
                Undo.DestroyObjectImmediate(legacyPanel.gameObject);
                Debug.Log("[ChestCanvasBuilder] Removed legacy hudCanvas/ChestPanel.");
            }
        }

        // Load fonts, sprites & slot prefab
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) font = DungeonUIAssetHelper.GetFontAsset();

        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Ui_Inventory/UI_Frame.png")
                          ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Frame.png");
        Sprite slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Ui_Inventory/UI_Slot.png");
        Sprite toolbarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Ui_Inventory/UI_Toolbar.png");
        Sprite headerPlaqueSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Header Plaque.png") ?? toolbarSprite;
        Sprite chestIconSprite = DungeonUIAssetHelper.GetChestSprite();

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
        if (slotPrefab == null) slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Chest Canvas");
        canvasGO.layer = uiLayer;
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 21; // Sits above Kitchen (20), below Statue (22) & MainMenu (100)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        ChestUI ui = canvasGO.AddComponent<ChestUI>();

        // 2. Fullscreen Dark Overlay (ChestOverlay)
        GameObject overlayGO = new GameObject("ChestOverlay", typeof(RectTransform), typeof(Image));
        overlayGO.layer = uiLayer;
        overlayGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        StretchFull(overlayRT);

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.04f, 0.06f, 0.88f);
        overlayImg.raycastTarget = true;

        // 3. Central Window Panel (980 x 740)
        GameObject windowGO = new GameObject("ChestWindow", typeof(RectTransform), typeof(Image));
        windowGO.layer = uiLayer;
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(980, 740);
        windowRT.anchoredPosition = Vector2.zero;

        Image windowBg = windowGO.GetComponent<Image>();
        windowBg.color = new Color(0.12f, 0.08f, 0.06f, 0.98f);
        if (frameSprite != null)
        {
            windowBg.sprite = frameSprite;
            windowBg.type = Image.Type.Sliced;
        }

        // Inner border/shadow
        GameObject innerBorderGO = new GameObject("InnerBorder", typeof(RectTransform), typeof(Image));
        innerBorderGO.layer = uiLayer;
        innerBorderGO.transform.SetParent(windowGO.transform, false);
        RectTransform ibRT = innerBorderGO.GetComponent<RectTransform>();
        ibRT.anchorMin = Vector2.zero;
        ibRT.anchorMax = Vector2.one;
        ibRT.offsetMin = new Vector2(10, 10);
        ibRT.offsetMax = new Vector2(-10, -10);
        Image ibImg = innerBorderGO.GetComponent<Image>();
        ibImg.color = new Color(0f, 0f, 0f, 0.35f);
        ibImg.raycastTarget = false;

        // 4. Top Header Bar (936 x 54, anchored at top y = -14)
        GameObject headerBarGO = new GameObject("TopHeaderBar", typeof(RectTransform), typeof(Image));
        headerBarGO.layer = uiLayer;
        headerBarGO.transform.SetParent(windowGO.transform, false);
        RectTransform headerRT = headerBarGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.5f, 1f);
        headerRT.anchorMax = new Vector2(0.5f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0, -14);
        headerRT.sizeDelta = new Vector2(936, 54);

        Image headerBg = headerBarGO.GetComponent<Image>();
        headerBg.color = new Color(0.18f, 0.14f, 0.10f, 0.95f);
        if (headerPlaqueSprite != null)
        {
            headerBg.sprite = headerPlaqueSprite;
            headerBg.type = Image.Type.Sliced;
        }

        // Header Chest Icon
        if (chestIconSprite != null)
        {
            GameObject iconGO = new GameObject("ChestIcon", typeof(RectTransform), typeof(Image));
            iconGO.layer = uiLayer;
            iconGO.transform.SetParent(headerBarGO.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.5f);
            iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot = new Vector2(0, 0.5f);
            iconRT.anchoredPosition = new Vector2(16, 0);
            iconRT.sizeDelta = new Vector2(36, 36);

            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = chestIconSprite;
            iconImg.preserveAspect = true;
        }

        // Header Title Text
        GameObject titleGO = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(headerBarGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.5f);
        titleRT.anchorMax = new Vector2(0.8f, 0.5f);
        titleRT.pivot = new Vector2(0, 0.5f);
        titleRT.anchoredPosition = new Vector2(62, 10);
        titleRT.sizeDelta = new Vector2(600, 24);

        TextMeshProUGUI headerTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(headerTitleText);
        headerTitleText.fontSize = 17;
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.text = "RƯƠNG CHỨA ĐỒ (HOME CHEST)";
        headerTitleText.color = new Color(1f, 0.88f, 0.45f);

        // Header Subtitle Text
        GameObject subGO = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.layer = uiLayer;
        subGO.transform.SetParent(headerBarGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0.5f);
        subRT.anchorMax = new Vector2(0.8f, 0.5f);
        subRT.pivot = new Vector2(0, 0.5f);
        subRT.anchoredPosition = new Vector2(62, -12);
        subRT.sizeDelta = new Vector2(600, 18);

        TextMeshProUGUI headerSubtitleText = subGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(headerSubtitleText);
        headerSubtitleText.fontSize = 11;
        headerSubtitleText.text = "Lưu trữ an toàn tuyệt đối • Vật phẩm cất giữ không bị mất khi vào Hầm Ngục";
        headerSubtitleText.color = new Color(0.72f, 0.78f, 0.86f);

        // Header Close Button [X]
        GameObject closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.layer = uiLayer;
        closeBtnGO.transform.SetParent(headerBarGO.transform, false);
        RectTransform closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 0.5f);
        closeBtnRT.anchorMax = new Vector2(1, 0.5f);
        closeBtnRT.pivot = new Vector2(1, 0.5f);
        closeBtnRT.anchoredPosition = new Vector2(-12, 0);
        closeBtnRT.sizeDelta = new Vector2(34, 34);

        Image closeBtnImg = closeBtnGO.GetComponent<Image>();
        Button closeButton = closeBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(closeButton, closeBtnImg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        UnityEventTools.AddPersistentListener(closeButton.onClick, ui.Close);

        GameObject closeTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtGO.layer = uiLayer;
        closeTxtGO.transform.SetParent(closeBtnGO.transform, false);
        StretchFull(closeTxtGO.GetComponent<RectTransform>());
        TextMeshProUGUI closeTxt = closeTxtGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(closeTxt);
        closeTxt.fontSize = 15;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.text = "✕";
        closeTxt.color = new Color(1f, 0.9f, 0.8f);

        // 5. Upper Section: Chest Section (936 x 240, anchored at y = 146)
        GameObject chestSecGO = new GameObject("ChestSection", typeof(RectTransform));
        chestSecGO.layer = uiLayer;
        chestSecGO.transform.SetParent(windowGO.transform, false);
        RectTransform chestSecRT = chestSecGO.GetComponent<RectTransform>();
        chestSecRT.anchorMin = new Vector2(0.5f, 0.5f);
        chestSecRT.anchorMax = new Vector2(0.5f, 0.5f);
        chestSecRT.pivot = new Vector2(0.5f, 0.5f);
        chestSecRT.anchoredPosition = new Vector2(0, 146);
        chestSecRT.sizeDelta = new Vector2(936, 240);

        // SectionHeader (Top of Chest Section)
        GameObject csHeaderGO = new GameObject("SectionHeader", typeof(RectTransform));
        csHeaderGO.layer = uiLayer;
        csHeaderGO.transform.SetParent(chestSecGO.transform, false);
        RectTransform cshRT = csHeaderGO.GetComponent<RectTransform>();
        cshRT.anchorMin = new Vector2(0, 1);
        cshRT.anchorMax = new Vector2(1, 1);
        cshRT.pivot = new Vector2(0.5f, 1);
        cshRT.anchoredPosition = new Vector2(0, 0);
        cshRT.sizeDelta = new Vector2(0, 26);

        // Title tag
        GameObject csTitleGO = new GameObject("TitleTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        csTitleGO.layer = uiLayer;
        csTitleGO.transform.SetParent(csHeaderGO.transform, false);
        RectTransform cstRT = csTitleGO.GetComponent<RectTransform>();
        cstRT.anchorMin = new Vector2(0, 0.5f);
        cstRT.anchorMax = new Vector2(0, 0.5f);
        cstRT.pivot = new Vector2(0, 0.5f);
        cstRT.anchoredPosition = new Vector2(6, 0);
        cstRT.sizeDelta = new Vector2(240, 24);

        TextMeshProUGUI csTitle = csTitleGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(csTitle);
        csTitle.fontSize = 13.5f;
        csTitle.fontStyle = FontStyles.Bold;
        csTitle.alignment = TextAlignmentOptions.MidlineLeft;
        csTitle.text = "📦 <color=#FFE082>RƯƠNG LƯU TRỮ</color>";

        // Capacity Text
        GameObject csCapGO = new GameObject("CapacityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        csCapGO.layer = uiLayer;
        csCapGO.transform.SetParent(csHeaderGO.transform, false);
        RectTransform cscRT = csCapGO.GetComponent<RectTransform>();
        cscRT.anchorMin = new Vector2(0, 0.5f);
        cscRT.anchorMax = new Vector2(0, 0.5f);
        cscRT.pivot = new Vector2(0, 0.5f);
        cscRT.anchoredPosition = new Vector2(220, 0);
        cscRT.sizeDelta = new Vector2(180, 24);

        TextMeshProUGUI chestCapacityText = csCapGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(chestCapacityText);
        chestCapacityText.fontSize = 12f;
        chestCapacityText.alignment = TextAlignmentOptions.MidlineLeft;
        chestCapacityText.text = "Sức chứa: <color=#FFE082><b>0</b></color> / 33 ô";
        chestCapacityText.color = new Color(0.85f, 0.9f, 0.95f);

        // Header Buttons: Sort Chest & Take All
        Button sortChestBtn = CreateCompactButton(csHeaderGO.transform, "Btn_SortChest", "🔀 Sắp xếp rương", new Vector2(-120, 0), new Vector2(110, 24), uiLayer, out _);
        UnityEventTools.AddPersistentListener(sortChestBtn.onClick, ui.OnSortChestClicked);

        Button takeAllChestBtn = CreateCompactButton(csHeaderGO.transform, "Btn_TakeAll", "⬆️ Lấy tất cả", new Vector2(-4, 0), new Vector2(100, 24), uiLayer, out _);
        UnityEventTools.AddPersistentListener(takeAllChestBtn.onClick, ui.OnTakeAllClicked);

        // Chest Frame Box (Slate dark panel)
        GameObject chestFrameGO = new GameObject("ChestFrame", typeof(RectTransform), typeof(Image));
        chestFrameGO.layer = uiLayer;
        chestFrameGO.transform.SetParent(chestSecGO.transform, false);
        RectTransform cfRT = chestFrameGO.GetComponent<RectTransform>();
        cfRT.anchorMin = new Vector2(0, 0);
        cfRT.anchorMax = new Vector2(1, 1);
        cfRT.offsetMin = new Vector2(0, 0);
        cfRT.offsetMax = new Vector2(0, -28);

        Image cfImg = chestFrameGO.GetComponent<Image>();
        cfImg.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);
        if (frameSprite != null)
        {
            cfImg.sprite = frameSprite;
            cfImg.type = Image.Type.Sliced;
        }

        // Chest Slots Grid (11 cols x 3 rows = 33 slots)
        GameObject chestSlotsGO = new GameObject("ChestSlots", typeof(RectTransform), typeof(GridLayoutGroup));
        chestSlotsGO.layer = uiLayer;
        chestSlotsGO.transform.SetParent(chestFrameGO.transform, false);
        StretchFull(chestSlotsGO.GetComponent<RectTransform>());

        GridLayoutGroup csGrid = chestSlotsGO.GetComponent<GridLayoutGroup>();
        csGrid.cellSize = new Vector2(60, 60);
        csGrid.spacing = new Vector2(8, 8);
        csGrid.padding = new RectOffset(18, 18, 6, 6);
        csGrid.childAlignment = TextAnchor.MiddleCenter;

        // Instantiate 33 slots
        for (int i = 0; i < 33; i++)
        {
            GameObject sGO = PrefabUtility.InstantiatePrefab(slotPrefab, chestSlotsGO.transform) as GameObject;
            if (sGO == null) sGO = Object.Instantiate(slotPrefab, chestSlotsGO.transform);
            sGO.name = $"inventorySlot ({i})";
            sGO.SetActive(true);
        }

        // 6. Middle Divider Bar (936 x 40, anchored at y = -6)
        GameObject midBarGO = new GameObject("MiddleDividerBar", typeof(RectTransform), typeof(Image));
        midBarGO.layer = uiLayer;
        midBarGO.transform.SetParent(windowGO.transform, false);
        RectTransform midRT = midBarGO.GetComponent<RectTransform>();
        midRT.anchorMin = new Vector2(0.5f, 0.5f);
        midRT.anchorMax = new Vector2(0.5f, 0.5f);
        midRT.pivot = new Vector2(0.5f, 0.5f);
        midRT.anchoredPosition = new Vector2(0, -6);
        midRT.sizeDelta = new Vector2(936, 40);

        Image midBg = midBarGO.GetComponent<Image>();
        midBg.color = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        if (headerPlaqueSprite != null)
        {
            midBg.sprite = headerPlaqueSprite;
            midBg.type = Image.Type.Sliced;
        }

        // 4 Central Buttons: Quick Stack, Store All, Take All, Sort All
        Button quickStackBtn = CreateTactileActionBarButton(midBarGO.transform, "Btn_QuickStack", "⚡ Gộp Chồng Nhanh", -320, 190, 30, DungeonUIAssetHelper.ButtonStyle.Victory, uiLayer);
        UnityEventTools.AddPersistentListener(quickStackBtn.onClick, ui.OnQuickStackClicked);

        Button storeAllBtn = CreateTactileActionBarButton(midBarGO.transform, "Btn_StoreAll", "⬇️ Cất Hết Vào Rương", -110, 190, 30, DungeonUIAssetHelper.ButtonStyle.WoodRustic, uiLayer);
        UnityEventTools.AddPersistentListener(storeAllBtn.onClick, ui.OnStoreAllClicked);

        Button takeAllBtn = CreateTactileActionBarButton(midBarGO.transform, "Btn_TakeAll", "⬆️ Lấy Hết Về Balo", 100, 190, 30, DungeonUIAssetHelper.ButtonStyle.WoodRustic, uiLayer);
        UnityEventTools.AddPersistentListener(takeAllBtn.onClick, ui.OnTakeAllClicked);

        Button sortAllBtn = CreateTactileActionBarButton(midBarGO.transform, "Btn_SortAll", "🔀 Sắp Xếp Cả Hai", 310, 180, 30, DungeonUIAssetHelper.ButtonStyle.WoodRustic, uiLayer);
        UnityEventTools.AddPersistentListener(sortAllBtn.onClick, ui.OnSortAllClicked);

        // 7. Lower Section: Backpack Section (936 x 240, anchored at y = -158)
        GameObject bpSecGO = new GameObject("BackpackSection", typeof(RectTransform));
        bpSecGO.layer = uiLayer;
        bpSecGO.transform.SetParent(windowGO.transform, false);
        RectTransform bpSecRT = bpSecGO.GetComponent<RectTransform>();
        bpSecRT.anchorMin = new Vector2(0.5f, 0.5f);
        bpSecRT.anchorMax = new Vector2(0.5f, 0.5f);
        bpSecRT.pivot = new Vector2(0.5f, 0.5f);
        bpSecRT.anchoredPosition = new Vector2(0, -158);
        bpSecRT.sizeDelta = new Vector2(936, 240);

        // SectionHeader (Top of Backpack Section)
        GameObject bpHeaderGO = new GameObject("SectionHeader", typeof(RectTransform));
        bpHeaderGO.layer = uiLayer;
        bpHeaderGO.transform.SetParent(bpSecGO.transform, false);
        RectTransform bphRT = bpHeaderGO.GetComponent<RectTransform>();
        bphRT.anchorMin = new Vector2(0, 1);
        bphRT.anchorMax = new Vector2(1, 1);
        bphRT.pivot = new Vector2(0.5f, 1);
        bphRT.anchoredPosition = new Vector2(0, 0);
        bphRT.sizeDelta = new Vector2(0, 26);

        // Title tag
        GameObject bpTitleGO = new GameObject("TitleTag", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpTitleGO.layer = uiLayer;
        bpTitleGO.transform.SetParent(bpHeaderGO.transform, false);
        RectTransform bptRT = bpTitleGO.GetComponent<RectTransform>();
        bptRT.anchorMin = new Vector2(0, 0.5f);
        bptRT.anchorMax = new Vector2(0, 0.5f);
        bptRT.pivot = new Vector2(0, 0.5f);
        bptRT.anchoredPosition = new Vector2(6, 0);
        bptRT.sizeDelta = new Vector2(240, 24);

        TextMeshProUGUI bpTitle = bpTitleGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(bpTitle);
        bpTitle.fontSize = 13.5f;
        bpTitle.fontStyle = FontStyles.Bold;
        bpTitle.alignment = TextAlignmentOptions.MidlineLeft;
        bpTitle.text = "🎒 <color=#A5D6A7>BA LÔ CỦA BẠN</color>";

        // Capacity Text
        GameObject bpCapGO = new GameObject("CapacityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpCapGO.layer = uiLayer;
        bpCapGO.transform.SetParent(bpHeaderGO.transform, false);
        RectTransform bpcRT = bpCapGO.GetComponent<RectTransform>();
        bpcRT.anchorMin = new Vector2(0, 0.5f);
        bpcRT.anchorMax = new Vector2(0, 0.5f);
        bpcRT.pivot = new Vector2(0, 0.5f);
        bpcRT.anchoredPosition = new Vector2(220, 0);
        bpcRT.sizeDelta = new Vector2(180, 24);

        TextMeshProUGUI backpackCapacityText = bpCapGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(backpackCapacityText);
        backpackCapacityText.fontSize = 12f;
        backpackCapacityText.alignment = TextAlignmentOptions.MidlineLeft;
        backpackCapacityText.text = "Đang chứa: <color=#A5D6A7><b>0</b></color> / 33 ô";
        backpackCapacityText.color = new Color(0.85f, 0.9f, 0.95f);

        // Header Buttons: Sort Backpack & Store All
        Button sortBpBtn = CreateCompactButton(bpHeaderGO.transform, "Btn_SortBackpack", "🔀 Sắp xếp balo", new Vector2(-120, 0), new Vector2(110, 24), uiLayer, out _);
        UnityEventTools.AddPersistentListener(sortBpBtn.onClick, ui.OnSortBackpackClicked);

        Button storeAllBpBtn = CreateCompactButton(bpHeaderGO.transform, "Btn_StoreAll", "⬇️ Cất tất cả", new Vector2(-4, 0), new Vector2(100, 24), uiLayer, out _);
        UnityEventTools.AddPersistentListener(storeAllBpBtn.onClick, ui.OnStoreAllClicked);

        // Backpack Frame Box (Slate dark panel)
        GameObject bpFrameGO = new GameObject("BackpackFrame", typeof(RectTransform), typeof(Image));
        bpFrameGO.layer = uiLayer;
        bpFrameGO.transform.SetParent(bpSecGO.transform, false);
        RectTransform bpfRT = bpFrameGO.GetComponent<RectTransform>();
        bpfRT.anchorMin = new Vector2(0, 0);
        bpfRT.anchorMax = new Vector2(1, 1);
        bpfRT.offsetMin = new Vector2(0, 0);
        bpfRT.offsetMax = new Vector2(0, -28);

        Image bpfImg = bpFrameGO.GetComponent<Image>();
        bpfImg.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);
        if (frameSprite != null)
        {
            bpfImg.sprite = frameSprite;
            bpfImg.type = Image.Type.Sliced;
        }

        // Backpack Slots Grid (11 cols x 3 rows = 33 slots)
        GameObject bpSlotsGO = new GameObject("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup));
        bpSlotsGO.layer = uiLayer;
        bpSlotsGO.transform.SetParent(bpFrameGO.transform, false);
        StretchFull(bpSlotsGO.GetComponent<RectTransform>());

        GridLayoutGroup bpGrid = bpSlotsGO.GetComponent<GridLayoutGroup>();
        bpGrid.cellSize = new Vector2(60, 60);
        bpGrid.spacing = new Vector2(8, 8);
        bpGrid.padding = new RectOffset(18, 18, 6, 6);
        bpGrid.childAlignment = TextAnchor.MiddleCenter;

        // Instantiate 33 slots
        for (int i = 0; i < 33; i++)
        {
            GameObject sGO = PrefabUtility.InstantiatePrefab(slotPrefab, bpSlotsGO.transform) as GameObject;
            if (sGO == null) sGO = Object.Instantiate(slotPrefab, bpSlotsGO.transform);
            sGO.name = $"inventorySlot ({i})";
            sGO.SetActive(true);
        }

        // 8. Footer Hint Bar (936 x 28, anchored at bottom y = -338)
        GameObject footerGO = new GameObject("FooterHintBar", typeof(RectTransform), typeof(Image));
        footerGO.layer = uiLayer;
        footerGO.transform.SetParent(windowGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0.5f, 0.5f);
        footerRT.anchorMax = new Vector2(0.5f, 0.5f);
        footerRT.pivot = new Vector2(0.5f, 0.5f);
        footerRT.anchoredPosition = new Vector2(0, -338);
        footerRT.sizeDelta = new Vector2(936, 28);

        Image footerBg = footerGO.GetComponent<Image>();
        footerBg.color = new Color(0.06f, 0.07f, 0.10f, 0.85f);
        if (headerPlaqueSprite != null)
        {
            footerBg.sprite = headerPlaqueSprite;
            footerBg.type = Image.Type.Sliced;
        }

        // Footer Hint Text
        GameObject fHintGO = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fHintGO.layer = uiLayer;
        fHintGO.transform.SetParent(footerGO.transform, false);
        RectTransform fhRT = fHintGO.GetComponent<RectTransform>();
        fhRT.anchorMin = new Vector2(0, 0);
        fhRT.anchorMax = new Vector2(0.68f, 1);
        fhRT.offsetMin = new Vector2(10, 0);
        fhRT.offsetMax = new Vector2(-4, 0);

        TextMeshProUGUI hintFooterText = fHintGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(hintFooterText);
        hintFooterText.fontSize = 11f;
        hintFooterText.alignment = TextAlignmentOptions.MidlineLeft;
        hintFooterText.text = "💡 <b>Chuột phải:</b> Chuyển nhanh 2 bên  •  <b>Kéo thả:</b> Hoán đổi vị trí / gộp chồng  •  <b>Phím E / ESC:</b> Đóng";
        hintFooterText.color = new Color(0.82f, 0.88f, 0.95f);

        // Footer Item Info Text
        GameObject fInfoGO = new GameObject("ItemInfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fInfoGO.layer = uiLayer;
        fInfoGO.transform.SetParent(footerGO.transform, false);
        RectTransform fiRT = fInfoGO.GetComponent<RectTransform>();
        fiRT.anchorMin = new Vector2(0.69f, 0);
        fiRT.anchorMax = new Vector2(1, 1);
        fiRT.offsetMin = new Vector2(4, 0);
        fiRT.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI itemHoverInfoText = fInfoGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(itemHoverInfoText);
        itemHoverInfoText.fontSize = 11.5f;
        itemHoverInfoText.alignment = TextAlignmentOptions.MidlineRight;
        itemHoverInfoText.text = "";

        // 9. Auto-Bind and apply references
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("chestPanel").objectReferenceValue = overlayGO;
        so.FindProperty("windowRect").objectReferenceValue = windowRT;
        so.FindProperty("chestSlotsParent").objectReferenceValue = chestSlotsGO.transform;
        so.FindProperty("backpackSlotsParent").objectReferenceValue = bpSlotsGO.transform;
        so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;

        so.FindProperty("chestCapacityText").objectReferenceValue = chestCapacityText;
        so.FindProperty("backpackCapacityText").objectReferenceValue = backpackCapacityText;

        so.FindProperty("sortChestButton").objectReferenceValue = sortChestBtn;
        so.FindProperty("takeAllChestButton").objectReferenceValue = takeAllChestBtn;

        so.FindProperty("sortBackpackButton").objectReferenceValue = sortBpBtn;
        so.FindProperty("storeAllBackpackButton").objectReferenceValue = storeAllBpBtn;

        so.FindProperty("quickStackButton").objectReferenceValue = quickStackBtn;
        so.FindProperty("storeAllButton").objectReferenceValue = storeAllBtn;
        so.FindProperty("takeAllButton").objectReferenceValue = takeAllBtn;
        so.FindProperty("sortAllButton").objectReferenceValue = sortAllBtn;

        so.FindProperty("headerTitleText").objectReferenceValue = headerTitleText;
        so.FindProperty("headerSubtitleText").objectReferenceValue = headerSubtitleText;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.FindProperty("hintFooterText").objectReferenceValue = hintFooterText;
        so.FindProperty("itemHoverInfoText").objectReferenceValue = itemHoverInfoText;
        so.ApplyModifiedProperties();

        ui.AutoBindReferences();

        // 10. Deactivate overlay by default so screen is never covered on load
        overlayGO.SetActive(false);

        // 11. Save Prefab Backup
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGO, PREFAB_PATH, InteractionMode.AutomatedAction);
        Debug.Log($"[ChestCanvasBuilder] Saved prefab backup to {PREFAB_PATH}");

        // 12. Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ChestCanvasBuilder] Complete ChestCanvas built and saved into Base.unity successfully!");

        return canvasGO;
    }

    private static Button CreateCompactButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, int layer, out TextMeshProUGUI textComp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        Button btn = go.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(btn, img, DungeonUIAssetHelper.ButtonStyle.WoodRustic);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.layer = layer;
        txtGO.transform.SetParent(go.transform, false);
        StretchFull(txtGO.GetComponent<RectTransform>());

        textComp = txtGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(textComp);
        textComp.fontSize = 10.5f;
        textComp.fontStyle = FontStyles.Bold;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.text = label;
        textComp.color = new Color(0.95f, 0.95f, 0.9f);

        return btn;
    }

    private static Button CreateTactileActionBarButton(Transform parent, string name, string label, float posX, float width, float height, DungeonUIAssetHelper.ButtonStyle style, int layer)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(posX, 0);
        rt.sizeDelta = new Vector2(width, height);

        Image img = go.GetComponent<Image>();
        Button btn = go.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(btn, img, style);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.layer = layer;
        txtGO.transform.SetParent(go.transform, false);
        StretchFull(txtGO.GetComponent<RectTransform>());

        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(txt);
        txt.fontSize = 11.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.text = label;
        txt.color = (style == DungeonUIAssetHelper.ButtonStyle.Victory) 
            ? new Color(1f, 0.95f, 0.6f) 
            : new Color(0.95f, 0.95f, 0.9f);

        return btn;
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
