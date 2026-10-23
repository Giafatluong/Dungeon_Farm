using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Kitchen Canvas directly in the game.
/// Matches the visual mockup: Sổ tay công thức, Nồi nấu bếp lửa (4 ô, progress bar, preview), and Balo nguyên liệu.
/// Saves both into Base.unity scene and as a reusable Prefab asset.
/// </summary>
public static class KitchenCanvasBuilder
{
    private const string PREFAB_PATH = "Assets/_Assets/_Prefabs/KitchenCanvas.prefab";
    private const string SCENE_PATH = "Assets/_Scenes/Base.unity";

    [MenuItem("Tools/Rebuild Kitchen Canvas")]
    public static void BuildKitchenCanvasMenu()
    {
        BuildAndSaveKitchenCanvas();
    }

    public static GameObject BuildAndSaveKitchenCanvas()
    {
        Debug.Log("[KitchenCanvasBuilder] Building Kitchen Canvas directly in Unity...");

        // Load fonts & sprites
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Sprite headerPlaqueSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Header Plaque.png");
        Sprite cardBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Món ăn  (không chọn).png");
        Sprite previewBoxSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Món ăn dự kiến.png");
        Sprite previewIconFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Ảnh món ăn dự kiến.png");
        Sprite cardIconFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Ảnh món ăn.png");
        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Assets/Frame.png");
        Sprite soupIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Ui_Food/01_tomato_soup-removebg-preview.png");
        Sprite slotPrefab = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Ui_Inventory/UI_Slot.png");

        Sprite fireSprite = null;
        Object[] campfireSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Kenmi/Cute Fantasy RPG - 16x16 top down pixel art asset pack/Sprites/Outdoor decoration/Campfire_Anim.png");
        for (int i = 0; i < campfireSprites.Length; i++)
        {
            if (campfireSprites[i] is Sprite s && s.name == "Campfire_Anim_1")
            {
                fireSprite = s;
                break;
            }
        }
        if (fireSprite == null && campfireSprites.Length > 1 && campfireSprites[1] is Sprite sFirst)
        {
            fireSprite = sFirst;
        }

        GameObject backpackSlotPrefabObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Assets/_Prefabs/inventorySlot.prefab");

        // 1. Root Canvas GameObject
        GameObject canvasGO = new GameObject("KitchenCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        KitchenUI kitchenUI = canvasGO.AddComponent<KitchenUI>();

        // 2. Fullscreen Overlay (Black 82%)
        GameObject overlayGO = CreateUIObject("KitchenOverlay", canvasGO.transform);
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        SetStretch(overlayRT);
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0.06f, 0.04f, 0.03f, 0.85f);
        overlayImg.raycastTarget = true;

        // 3. Central Main Window (1440 x 860)
        GameObject windowGO = CreateUIObject("KitchenWindow", overlayGO.transform);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(1440, 860);
        windowRT.anchoredPosition = Vector2.zero;

        Image windowImg = windowGO.AddComponent<Image>();
        windowImg.color = new Color(0.24f, 0.14f, 0.09f, 1f); // Dark rich wood #3D2417
        if (frameSprite != null)
        {
            windowImg.sprite = frameSprite;
            windowImg.type = Image.Type.Sliced;
        }

        // Inner Border line / soft panel background
        GameObject innerBorderGO = CreateUIObject("InnerBorder", windowGO.transform);
        RectTransform ibRT = innerBorderGO.GetComponent<RectTransform>();
        ibRT.anchorMin = Vector2.zero;
        ibRT.anchorMax = Vector2.one;
        ibRT.offsetMin = new Vector2(8, 8);
        ibRT.offsetMax = new Vector2(-8, -8);
        Image ibImg = innerBorderGO.AddComponent<Image>();
        ibImg.color = new Color(0.18f, 0.10f, 0.06f, 0.95f);
        ibImg.raycastTarget = false;

        // 4. Top Header Bar
        GameObject headerGO = CreateUIObject("TopHeaderBar", windowGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -14);
        headerRT.sizeDelta = new Vector2(-36, 58);

        Image headerBg = headerGO.AddComponent<Image>();
        headerBg.color = new Color(0.35f, 0.22f, 0.14f, 1f); // Cocoa Plaque #593824
        if (headerPlaqueSprite != null)
        {
            headerBg.sprite = headerPlaqueSprite;
            headerBg.type = Image.Type.Sliced;
        }

        // Header Title Text
        GameObject titleGO = CreateUIObject("HeaderTitle", headerGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(0.75f, 1);
        titleRT.offsetMin = new Vector2(24, 0);
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "🍲 BẾP NẤU GIA ĐÌNH & SỔ TAY CÔNG THỨC ẨM THỰC";
        titleTMP.fontSize = 22;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.color = new Color(1f, 0.95f, 0.88f);

        // Header Close Button
        GameObject closeBtnGO = CreateUIObject("CloseButton", headerGO.transform);
        RectTransform closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 0.5f);
        closeBtnRT.anchorMax = new Vector2(1, 0.5f);
        closeBtnRT.pivot = new Vector2(1, 0.5f);
        closeBtnRT.anchoredPosition = new Vector2(-16, 0);
        closeBtnRT.sizeDelta = new Vector2(170, 42);

        Image closeBg = closeBtnGO.AddComponent<Image>();
        closeBg.color = new Color(0.24f, 0.14f, 0.08f, 0.75f);
        Button closeBtn = closeBtnGO.AddComponent<Button>();

        GameObject closeTextGO = CreateUIObject("CloseText", closeBtnGO.transform);
        RectTransform closeTextRT = closeTextGO.GetComponent<RectTransform>();
        SetStretch(closeTextRT);
        TextMeshProUGUI closeTMP = closeTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) closeTMP.font = font;
        closeTMP.text = "✕ [ESC] Đóng Bếp";
        closeTMP.fontSize = 17;
        closeTMP.fontStyle = FontStyles.Bold;
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.color = new Color(1f, 0.72f, 0.22f); // Amber gold

        // ==========================================
        // 5. LEFT COLUMN: SỔ TAY CÔNG THỨC (RECIPES)
        // ==========================================
        GameObject leftPanelGO = CreateUIObject("LeftPanel_Recipes", windowGO.transform);
        RectTransform leftPanelRT = leftPanelGO.GetComponent<RectTransform>();
        leftPanelRT.anchorMin = new Vector2(0.016f, 0.024f);
        leftPanelRT.anchorMax = new Vector2(0.340f, 0.900f);
        leftPanelRT.offsetMin = Vector2.zero;
        leftPanelRT.offsetMax = Vector2.zero;

        Image leftBg = leftPanelGO.AddComponent<Image>();
        leftBg.color = new Color(0.98f, 0.96f, 0.93f, 1f); // Warm ivory #FAF4ED

        // Tab Bar
        GameObject tabRowGO = CreateUIObject("FilterTabBar", leftPanelGO.transform);
        RectTransform tabRowRT = tabRowGO.GetComponent<RectTransform>();
        tabRowRT.anchorMin = new Vector2(0, 1);
        tabRowRT.anchorMax = new Vector2(1, 1);
        tabRowRT.pivot = new Vector2(0.5f, 1);
        tabRowRT.anchoredPosition = new Vector2(0, -12);
        tabRowRT.sizeDelta = new Vector2(-20, 42);

        HorizontalLayoutGroup tabLayout = tabRowGO.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 6;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;

        Button tabAll = CreateTabButton("Tab_All", tabRowGO.transform, "TẤT CẢ (12)", new Color(0.36f, 0.70f, 0.16f), Color.white, font);
        Button tabRec = CreateTabButton("Tab_Recovery", tabRowGO.transform, "HỒI PHỤC", new Color(0.92f, 0.86f, 0.78f), new Color(0.36f, 0.25f, 0.20f), font);
        Button tabSpd = CreateTabButton("Tab_Speed", tabRowGO.transform, "TĂNG TỐC", new Color(0.92f, 0.86f, 0.78f), new Color(0.36f, 0.25f, 0.20f), font);
        Button tabOth = CreateTabButton("Tab_Other", tabRowGO.transform, "KHÁC", new Color(0.92f, 0.86f, 0.78f), new Color(0.36f, 0.25f, 0.20f), font);

        // Recipe Scroll View
        GameObject recipeScrollGO = CreateUIObject("RecipeScrollView", leftPanelGO.transform);
        RectTransform rScrollRT = recipeScrollGO.GetComponent<RectTransform>();
        rScrollRT.anchorMin = new Vector2(0, 0);
        rScrollRT.anchorMax = new Vector2(1, 1);
        rScrollRT.offsetMin = new Vector2(10, 12);
        rScrollRT.offsetMax = new Vector2(-10, -62);

        ScrollRect rScroll = recipeScrollGO.AddComponent<ScrollRect>();
        rScroll.horizontal = false;
        rScroll.vertical = true;
        rScroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject rViewportGO = CreateUIObject("Viewport", recipeScrollGO.transform);
        RectTransform rViewRT = rViewportGO.GetComponent<RectTransform>();
        SetStretch(rViewRT);
        rViewportGO.AddComponent<RectMask2D>();
        rScroll.viewport = rViewRT;

        GameObject rContentGO = CreateUIObject("Content", rViewportGO.transform);
        RectTransform rContentRT = rContentGO.GetComponent<RectTransform>();
        rContentRT.anchorMin = new Vector2(0, 1);
        rContentRT.anchorMax = new Vector2(1, 1);
        rContentRT.pivot = new Vector2(0.5f, 1);
        rContentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup rLayout = rContentGO.AddComponent<VerticalLayoutGroup>();
        rLayout.spacing = 8;
        rLayout.childForceExpandWidth = true;
        rLayout.childForceExpandHeight = false;
        rLayout.childControlWidth = true;
        rLayout.childControlHeight = true;

        ContentSizeFitter rFitter = rContentGO.AddComponent<ContentSizeFitter>();
        rFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rScroll.content = rContentRT;

        // Recipe Card Template
        GameObject cardTemplate = CreateRecipeCardTemplate(leftPanelGO.transform, cardBgSprite, cardIconFrameSprite, soupIconSprite, font);
        cardTemplate.SetActive(false);

        // ==========================================
        // 6. MIDDLE COLUMN: NỒI NẤU BẾP LỬA (STOVE)
        // ==========================================
        GameObject midPanelGO = CreateUIObject("MiddlePanel_Stove", windowGO.transform);
        RectTransform midPanelRT = midPanelGO.GetComponent<RectTransform>();
        midPanelRT.anchorMin = new Vector2(0.352f, 0.024f);
        midPanelRT.anchorMax = new Vector2(0.675f, 0.900f);
        midPanelRT.offsetMin = Vector2.zero;
        midPanelRT.offsetMax = Vector2.zero;

        Image midBg = midPanelGO.AddComponent<Image>();
        midBg.color = new Color(0.98f, 0.96f, 0.93f, 1f); // Warm ivory

        // Header Tag: "NỒI NẤU BẾP LỬA"
        GameObject midTagGO = CreateUIObject("HeaderTag", midPanelGO.transform);
        RectTransform midTagRT = midTagGO.GetComponent<RectTransform>();
        midTagRT.anchorMin = new Vector2(0.5f, 1f);
        midTagRT.anchorMax = new Vector2(0.5f, 1f);
        midTagRT.pivot = new Vector2(0.5f, 0.5f);
        midTagRT.anchoredPosition = new Vector2(0, 0);
        midTagRT.sizeDelta = new Vector2(230, 36);

        Image midTagBg = midTagGO.AddComponent<Image>();
        midTagBg.color = new Color(0.36f, 0.22f, 0.14f, 1f);
        if (headerPlaqueSprite != null)
        {
            midTagBg.sprite = headerPlaqueSprite;
            midTagBg.type = Image.Type.Sliced;
        }

        GameObject midTagTextGO = CreateUIObject("TagText", midTagGO.transform);
        RectTransform midTagTextRT = midTagTextGO.GetComponent<RectTransform>();
        SetStretch(midTagTextRT);
        TextMeshProUGUI midTagTMP = midTagTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) midTagTMP.font = font;
        midTagTMP.text = "NỒI NẤU BẾP LỬA";
        midTagTMP.fontSize = 15;
        midTagTMP.fontStyle = FontStyles.Bold;
        midTagTMP.alignment = TextAlignmentOptions.Center;
        midTagTMP.color = new Color(1f, 0.96f, 0.88f);

        // Cooking Stage Display (Dark chocolate box with Pot + Flame)
        GameObject stageGO = CreateUIObject("CookingStage", midPanelGO.transform);
        RectTransform stageRT = stageGO.GetComponent<RectTransform>();
        stageRT.anchorMin = new Vector2(0.5f, 1);
        stageRT.anchorMax = new Vector2(0.5f, 1);
        stageRT.pivot = new Vector2(0.5f, 1);
        stageRT.anchoredPosition = new Vector2(0, -32);
        stageRT.sizeDelta = new Vector2(430, 160);

        Image stageBg = stageGO.AddComponent<Image>();
        stageBg.color = new Color(0.24f, 0.14f, 0.09f, 1f); // Dark cocoa

        // Pot Icon
        GameObject potIconGO = CreateUIObject("PotIcon", stageGO.transform);
        RectTransform potIconRT = potIconGO.GetComponent<RectTransform>();
        potIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        potIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        potIconRT.anchoredPosition = new Vector2(-42, 14);
        potIconRT.sizeDelta = new Vector2(80, 80);
        Image potIconImg = potIconGO.AddComponent<Image>();
        if (soupIconSprite != null) potIconImg.sprite = soupIconSprite;

        // Fire Icon
        GameObject fireIconGO = CreateUIObject("FireIcon", stageGO.transform);
        RectTransform fireIconRT = fireIconGO.GetComponent<RectTransform>();
        fireIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        fireIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        fireIconRT.anchoredPosition = new Vector2(42, 14);
        fireIconRT.sizeDelta = new Vector2(68, 68);
        Image fireIconImg = fireIconGO.AddComponent<Image>();
        if (fireSprite != null) fireIconImg.sprite = fireSprite;

        // Stage Subtitle
        GameObject stageSubGO = CreateUIObject("StageSubtitle", stageGO.transform);
        RectTransform stageSubRT = stageSubGO.GetComponent<RectTransform>();
        stageSubRT.anchorMin = new Vector2(0, 0);
        stageSubRT.anchorMax = new Vector2(1, 0);
        stageSubRT.pivot = new Vector2(0.5f, 0);
        stageSubRT.anchoredPosition = new Vector2(0, 10);
        stageSubRT.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI stageSubTMP = stageSubGO.AddComponent<TextMeshProUGUI>();
        if (font != null) stageSubTMP.font = font;
        stageSubTMP.text = "Bếp Lửa Sôi Sùng Sục • Sẵn Sàng Chế Biến";
        stageSubTMP.fontSize = 15;
        stageSubTMP.fontStyle = FontStyles.Bold;
        stageSubTMP.alignment = TextAlignmentOptions.Center;
        stageSubTMP.color = new Color(1f, 0.90f, 0.55f); // Golden yellow

        // Pot Slots Section
        GameObject potSecGO = CreateUIObject("PotSlotsSection", midPanelGO.transform);
        RectTransform potSecRT = potSecGO.GetComponent<RectTransform>();
        potSecRT.anchorMin = new Vector2(0.5f, 1);
        potSecRT.anchorMax = new Vector2(0.5f, 1);
        potSecRT.pivot = new Vector2(0.5f, 1);
        potSecRT.anchoredPosition = new Vector2(0, -204);
        potSecRT.sizeDelta = new Vector2(430, 115);

        // Section Title: "BỎ NGUYÊN LIỆU VÀO 4 Ô DƯỚI ĐÂY:"
        GameObject secTitleGO = CreateUIObject("SectionTitle", potSecGO.transform);
        RectTransform secTitleRT = secTitleGO.GetComponent<RectTransform>();
        secTitleRT.anchorMin = new Vector2(0, 1);
        secTitleRT.anchorMax = new Vector2(1, 1);
        secTitleRT.pivot = new Vector2(0.5f, 1);
        secTitleRT.anchoredPosition = new Vector2(0, 0);
        secTitleRT.sizeDelta = new Vector2(0, 26);
        TextMeshProUGUI secTitleTMP = secTitleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) secTitleTMP.font = font;
        secTitleTMP.text = "BỎ NGUYÊN LIỆU VÀO 4 Ô DƯỚI ĐÂY:";
        secTitleTMP.fontSize = 15;
        secTitleTMP.fontStyle = FontStyles.Bold;
        secTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        secTitleTMP.color = new Color(0.32f, 0.20f, 0.14f);

        // Slots Row (4 slots)
        GameObject slotsRowGO = CreateUIObject("SlotsRow", potSecGO.transform);
        RectTransform slotsRowRT = slotsRowGO.GetComponent<RectTransform>();
        slotsRowRT.anchorMin = new Vector2(0, 0);
        slotsRowRT.anchorMax = new Vector2(1, 0);
        slotsRowRT.pivot = new Vector2(0.5f, 0);
        slotsRowRT.anchoredPosition = new Vector2(0, 0);
        slotsRowRT.sizeDelta = new Vector2(0, 84);

        HorizontalLayoutGroup slotsLayout = slotsRowGO.AddComponent<HorizontalLayoutGroup>();
        slotsLayout.spacing = 14;
        slotsLayout.childAlignment = TextAnchor.MiddleCenter;
        slotsLayout.childForceExpandWidth = false;
        slotsLayout.childForceExpandHeight = false;
        slotsLayout.childControlWidth = false;
        slotsLayout.childControlHeight = false;

        for (int i = 0; i < 4; i++)
        {
            CreatePotSlot($"PotSlot_{i}", slotsRowGO.transform, previewIconFrameSprite, font);
        }

        // Cooking Progress Bar Section
        GameObject progSecGO = CreateUIObject("ProgressBarSection", midPanelGO.transform);
        RectTransform progSecRT = progSecGO.GetComponent<RectTransform>();
        progSecRT.anchorMin = new Vector2(0.5f, 1);
        progSecRT.anchorMax = new Vector2(0.5f, 1);
        progSecRT.pivot = new Vector2(0.5f, 1);
        progSecRT.anchoredPosition = new Vector2(0, -328);
        progSecRT.sizeDelta = new Vector2(430, 60);

        // Progress Title: "⏱️ TIẾN TRÌNH NẤU (5 GIÂY) ⏱️"
        GameObject progTitleGO = CreateUIObject("ProgressTitle", progSecGO.transform);
        RectTransform progTitleRT = progTitleGO.GetComponent<RectTransform>();
        progTitleRT.anchorMin = new Vector2(0, 1);
        progTitleRT.anchorMax = new Vector2(1, 1);
        progTitleRT.pivot = new Vector2(0.5f, 1);
        progTitleRT.anchoredPosition = new Vector2(0, 0);
        progTitleRT.sizeDelta = new Vector2(0, 22);
        TextMeshProUGUI progTitleTMP = progTitleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) progTitleTMP.font = font;
        progTitleTMP.text = "⏱️ TIẾN TRÌNH NẤU (5 GIÂY) ⏱️";
        progTitleTMP.fontSize = 14;
        progTitleTMP.fontStyle = FontStyles.Bold;
        progTitleTMP.alignment = TextAlignmentOptions.Center;
        progTitleTMP.color = new Color(0.24f, 0.44f, 0.64f); // Crisp cyan-blue label

        // Slider Bar
        GameObject sliderGO = CreateUIObject("CookingSlider", progSecGO.transform);
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0, 0);
        sliderRT.anchorMax = new Vector2(1, 0);
        sliderRT.pivot = new Vector2(0.5f, 0);
        sliderRT.anchoredPosition = new Vector2(0, 0);
        sliderRT.sizeDelta = new Vector2(0, 32);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        // Slider Background
        Image sliderBg = sliderGO.AddComponent<Image>();
        sliderBg.color = new Color(0.25f, 0.16f, 0.10f, 1f); // Dark cocoa track

        // Slider Fill Area
        GameObject fillAreaGO = CreateUIObject("Fill Area", sliderGO.transform);
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0);
        fillAreaRT.anchorMax = new Vector2(1, 1);
        fillAreaRT.offsetMin = new Vector2(3, 3);
        fillAreaRT.offsetMax = new Vector2(-3, -3);

        GameObject fillGO = CreateUIObject("Fill", fillAreaGO.transform);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.58f, 0.06f, 1f); // Fire flame orange #FF9410
        slider.fillRect = fillRT;

        // Slider Status Text
        GameObject statusTextGO = CreateUIObject("StatusText", sliderGO.transform);
        RectTransform statusTextRT = statusTextGO.GetComponent<RectTransform>();
        SetStretch(statusTextRT);
        TextMeshProUGUI statusTMP = statusTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) statusTMP.font = font;
        statusTMP.text = "SẴN SÀNG";
        statusTMP.fontSize = 14;
        statusTMP.fontStyle = FontStyles.Bold;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = Color.white;

        // Output Preview Card (Món ăn dự kiến)
        GameObject prevCardGO = CreateUIObject("OutputPreviewCard", midPanelGO.transform);
        RectTransform prevCardRT = prevCardGO.GetComponent<RectTransform>();
        prevCardRT.anchorMin = new Vector2(0.5f, 1);
        prevCardRT.anchorMax = new Vector2(0.5f, 1);
        prevCardRT.pivot = new Vector2(0.5f, 1);
        prevCardRT.anchoredPosition = new Vector2(0, -400);
        prevCardRT.sizeDelta = new Vector2(430, 115);

        Image prevCardBg = prevCardGO.AddComponent<Image>();
        prevCardBg.color = new Color(1f, 0.98f, 0.92f, 1f);
        if (previewBoxSprite != null)
        {
            prevCardBg.sprite = previewBoxSprite;
            prevCardBg.type = Image.Type.Sliced;
        }

        // Preview Icon Frame
        GameObject prevIconFrameGO = CreateUIObject("PreviewIconFrame", prevCardGO.transform);
        RectTransform prevIconFrameRT = prevIconFrameGO.GetComponent<RectTransform>();
        prevIconFrameRT.anchorMin = new Vector2(0, 0.5f);
        prevIconFrameRT.anchorMax = new Vector2(0, 0.5f);
        prevIconFrameRT.pivot = new Vector2(0, 0.5f);
        prevIconFrameRT.anchoredPosition = new Vector2(16, 0);
        prevIconFrameRT.sizeDelta = new Vector2(80, 80);

        Image prevIconFrameImg = prevIconFrameGO.AddComponent<Image>();
        prevIconFrameImg.color = new Color(0.95f, 0.91f, 0.83f, 1f);
        if (previewIconFrameSprite != null)
        {
            prevIconFrameImg.sprite = previewIconFrameSprite;
            prevIconFrameImg.type = Image.Type.Sliced;
        }

        GameObject prevDishIconGO = CreateUIObject("DishIcon", prevIconFrameGO.transform);
        RectTransform prevDishIconRT = prevDishIconGO.GetComponent<RectTransform>();
        SetStretch(prevDishIconRT, 8);
        Image prevDishIconImg = prevDishIconGO.AddComponent<Image>();
        if (soupIconSprite != null) prevDishIconImg.sprite = soupIconSprite;

        // Preview Info Block
        GameObject prevInfoGO = CreateUIObject("InfoBlock", prevCardGO.transform);
        RectTransform prevInfoRT = prevInfoGO.GetComponent<RectTransform>();
        prevInfoRT.anchorMin = new Vector2(0, 0);
        prevInfoRT.anchorMax = new Vector2(1, 1);
        prevInfoRT.offsetMin = new Vector2(110, 10);
        prevInfoRT.offsetMax = new Vector2(-12, -10);

        GameObject prevLblGO = CreateUIObject("PreviewHeaderLabel", prevInfoGO.transform);
        RectTransform prevLblRT = prevLblGO.GetComponent<RectTransform>();
        prevLblRT.anchorMin = new Vector2(0, 1);
        prevLblRT.anchorMax = new Vector2(1, 1);
        prevLblRT.pivot = new Vector2(0, 1);
        prevLblRT.anchoredPosition = new Vector2(0, 0);
        prevLblRT.sizeDelta = new Vector2(0, 20);
        TextMeshProUGUI prevLblTMP = prevLblGO.AddComponent<TextMeshProUGUI>();
        if (font != null) prevLblTMP.font = font;
        prevLblTMP.text = "MÓN THÀNH PHẨM DỰ KIẾN:";
        prevLblTMP.fontSize = 13;
        prevLblTMP.fontStyle = FontStyles.Bold;
        prevLblTMP.color = new Color(0.48f, 0.32f, 0.22f);

        GameObject prevNameGO = CreateUIObject("PreviewDishName", prevInfoGO.transform);
        RectTransform prevNameRT = prevNameGO.GetComponent<RectTransform>();
        prevNameRT.anchorMin = new Vector2(0, 1);
        prevNameRT.anchorMax = new Vector2(1, 1);
        prevNameRT.pivot = new Vector2(0, 1);
        prevNameRT.anchoredPosition = new Vector2(0, -22);
        prevNameRT.sizeDelta = new Vector2(0, 28);
        TextMeshProUGUI prevNameTMP = prevNameGO.AddComponent<TextMeshProUGUI>();
        if (font != null) prevNameTMP.font = font;
        prevNameTMP.text = "Súp Cà Chua Hương Thảo";
        prevNameTMP.fontSize = 19;
        prevNameTMP.fontStyle = FontStyles.Bold;
        prevNameTMP.color = new Color(0.24f, 0.14f, 0.08f);

        GameObject prevStatsGO = CreateUIObject("PreviewDishStats", prevInfoGO.transform);
        RectTransform prevStatsRT = prevStatsGO.GetComponent<RectTransform>();
        prevStatsRT.anchorMin = new Vector2(0, 0);
        prevStatsRT.anchorMax = new Vector2(1, 0);
        prevStatsRT.pivot = new Vector2(0, 0);
        prevStatsRT.anchoredPosition = new Vector2(0, 4);
        prevStatsRT.sizeDelta = new Vector2(0, 24);
        TextMeshProUGUI prevStatsTMP = prevStatsGO.AddComponent<TextMeshProUGUI>();
        if (font != null) prevStatsTMP.font = font;
        prevStatsTMP.text = "❤️ +45 HP | 🍗 +60 Độ No | ⚡ +10% Tốc Độ";
        prevStatsTMP.fontSize = 13;
        prevStatsTMP.fontStyle = FontStyles.Bold;
        prevStatsTMP.color = new Color(0.35f, 0.25f, 0.20f);

        // Action Buttons Row (Cook & Cancel)
        GameObject actionRowGO = CreateUIObject("ActionButtonsRow", midPanelGO.transform);
        RectTransform actionRowRT = actionRowGO.GetComponent<RectTransform>();
        actionRowRT.anchorMin = new Vector2(0.5f, 0);
        actionRowRT.anchorMax = new Vector2(0.5f, 0);
        actionRowRT.pivot = new Vector2(0.5f, 0);
        actionRowRT.anchoredPosition = new Vector2(0, 18);
        actionRowRT.sizeDelta = new Vector2(430, 52);

        HorizontalLayoutGroup actLayout = actionRowGO.AddComponent<HorizontalLayoutGroup>();
        actLayout.spacing = 14;
        actLayout.childForceExpandWidth = true;
        actLayout.childForceExpandHeight = true;
        actLayout.childControlWidth = true;
        actLayout.childControlHeight = true;

        CreateActionButton("CookButton", actionRowGO.transform, "🔥 BẮT ĐẦU NẤU", new Color(0.32f, 0.65f, 0.16f), Color.white, font);
        CreateActionButton("CancelButton", actionRowGO.transform, "✕ HỦY NGUYÊN LIỆU", new Color(0.82f, 0.22f, 0.22f), Color.white, font);

        // ==========================================
        // 7. RIGHT COLUMN: BALO NGUYÊN LIỆU (BACKPACK)
        // ==========================================
        GameObject rightPanelGO = CreateUIObject("RightPanel_Backpack", windowGO.transform);
        RectTransform rightPanelRT = rightPanelGO.GetComponent<RectTransform>();
        rightPanelRT.anchorMin = new Vector2(0.686f, 0.024f);
        rightPanelRT.anchorMax = new Vector2(0.984f, 0.900f);
        rightPanelRT.offsetMin = Vector2.zero;
        rightPanelRT.offsetMax = Vector2.zero;

        Image rightBg = rightPanelGO.AddComponent<Image>();
        rightBg.color = new Color(0.98f, 0.96f, 0.93f, 1f); // Warm ivory

        // Header Tag: "BALO NGUYÊN LIỆU"
        GameObject rightTagGO = CreateUIObject("HeaderTag", rightPanelGO.transform);
        RectTransform rightTagRT = rightTagGO.GetComponent<RectTransform>();
        rightTagRT.anchorMin = new Vector2(0.5f, 1f);
        rightTagRT.anchorMax = new Vector2(0.5f, 1f);
        rightTagRT.pivot = new Vector2(0.5f, 0.5f);
        rightTagRT.anchoredPosition = new Vector2(0, 0);
        rightTagRT.sizeDelta = new Vector2(230, 36);

        Image rightTagBg = rightTagGO.AddComponent<Image>();
        rightTagBg.color = new Color(0.36f, 0.22f, 0.14f, 1f);
        if (headerPlaqueSprite != null)
        {
            rightTagBg.sprite = headerPlaqueSprite;
            rightTagBg.type = Image.Type.Sliced;
        }

        GameObject rightTagTextGO = CreateUIObject("TagText", rightTagGO.transform);
        RectTransform rightTagTextRT = rightTagTextGO.GetComponent<RectTransform>();
        SetStretch(rightTagTextRT);
        TextMeshProUGUI rightTagTMP = rightTagTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) rightTagTMP.font = font;
        rightTagTMP.text = "BALO NGUYÊN LIỆU";
        rightTagTMP.fontSize = 15;
        rightTagTMP.fontStyle = FontStyles.Bold;
        rightTagTMP.alignment = TextAlignmentOptions.Center;
        rightTagTMP.color = new Color(1f, 0.96f, 0.88f);

        // Subtitle: "Kéo thả nguyên liệu từ Balo vào 4 ô nấu ăn:"
        GameObject subGO = CreateUIObject("BackpackSubtitle", rightPanelGO.transform);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0.5f, 1);
        subRT.anchoredPosition = new Vector2(0, -26);
        subRT.sizeDelta = new Vector2(-24, 26);
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        if (font != null) subTMP.font = font;
        subTMP.text = "Kéo thả nguyên liệu từ Balo vào 4 ô nấu ăn:";
        subTMP.fontSize = 14;
        subTMP.fontStyle = FontStyles.Bold;
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;
        subTMP.color = new Color(0.35f, 0.22f, 0.16f);

        // Backpack Scroll View
        GameObject bpScrollGO = CreateUIObject("BackpackScrollView", rightPanelGO.transform);
        RectTransform bpScrollRT = bpScrollGO.GetComponent<RectTransform>();
        bpScrollRT.anchorMin = new Vector2(0, 0);
        bpScrollRT.anchorMax = new Vector2(1, 1);
        bpScrollRT.offsetMin = new Vector2(12, 70);
        bpScrollRT.offsetMax = new Vector2(-12, -56);

        ScrollRect bpScroll = bpScrollGO.AddComponent<ScrollRect>();
        bpScroll.horizontal = false;
        bpScroll.vertical = true;
        bpScroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject bpViewportGO = CreateUIObject("Viewport", bpScrollGO.transform);
        RectTransform bpViewRT = bpViewportGO.GetComponent<RectTransform>();
        SetStretch(bpViewRT);
        bpViewportGO.AddComponent<RectMask2D>();
        bpScroll.viewport = bpViewRT;

        GameObject bpContentGO = CreateUIObject("Content", bpViewportGO.transform);
        RectTransform bpContentRT = bpContentGO.GetComponent<RectTransform>();
        bpContentRT.anchorMin = new Vector2(0, 1);
        bpContentRT.anchorMax = new Vector2(1, 1);
        bpContentRT.pivot = new Vector2(0.5f, 1);
        bpContentRT.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup bpGrid = bpContentGO.AddComponent<GridLayoutGroup>();
        bpGrid.cellSize = new Vector2(66, 66);
        bpGrid.spacing = new Vector2(8, 8);
        bpGrid.padding = new RectOffset(6, 6, 6, 6);
        bpGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGrid.constraintCount = 5;

        ContentSizeFitter bpFitter = bpContentGO.AddComponent<ContentSizeFitter>();
        bpFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bpScroll.content = bpContentRT;

        // Backpack Action Buttons Row (Sort & Clear)
        GameObject bpActRowGO = CreateUIObject("BackpackActionButtonsRow", rightPanelGO.transform);
        RectTransform bpActRowRT = bpActRowGO.GetComponent<RectTransform>();
        bpActRowRT.anchorMin = new Vector2(0.5f, 0);
        bpActRowRT.anchorMax = new Vector2(0.5f, 0);
        bpActRowRT.pivot = new Vector2(0.5f, 0);
        bpActRowRT.anchoredPosition = new Vector2(0, 14);
        bpActRowRT.sizeDelta = new Vector2(390, 46);

        HorizontalLayoutGroup bpActLayout = bpActRowGO.AddComponent<HorizontalLayoutGroup>();
        bpActLayout.spacing = 10;
        bpActLayout.childForceExpandWidth = true;
        bpActLayout.childForceExpandHeight = true;
        bpActLayout.childControlWidth = true;
        bpActLayout.childControlHeight = true;

        CreateActionButton("SortButton", bpActRowGO.transform, "⚡ SẮP XẾP TÚI", new Color(0.68f, 0.45f, 0.16f), Color.white, font);
        CreateActionButton("ClearButton", bpActRowGO.transform, "🧹 LÀM TRỐNG Ô", new Color(0.94f, 0.90f, 0.84f), new Color(0.36f, 0.22f, 0.14f), font);

        // 8. Feedback Toast (Floating notification banner)
        GameObject toastGO = CreateUIObject("FeedbackToast", overlayGO.transform);
        RectTransform toastRT = toastGO.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0.5f, 1);
        toastRT.anchorMax = new Vector2(0.5f, 1);
        toastRT.pivot = new Vector2(0.5f, 1);
        toastRT.anchoredPosition = new Vector2(0, -25);
        toastRT.sizeDelta = new Vector2(560, 46);

        Image toastBg = toastGO.AddComponent<Image>();
        toastBg.color = new Color(0.16f, 0.10f, 0.06f, 0.94f);
        if (headerPlaqueSprite != null)
        {
            toastBg.sprite = headerPlaqueSprite;
            toastBg.type = Image.Type.Sliced;
        }

        GameObject toastTextGO = CreateUIObject("ToastText", toastGO.transform);
        RectTransform toastTextRT = toastTextGO.GetComponent<RectTransform>();
        SetStretch(toastTextRT, 10);
        TextMeshProUGUI toastTMP = toastTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) toastTMP.font = font;
        toastTMP.text = "Đã nấu thành công món ăn!";
        toastTMP.fontSize = 16;
        toastTMP.fontStyle = FontStyles.Bold;
        toastTMP.alignment = TextAlignmentOptions.Center;
        toastTMP.color = new Color(1f, 0.95f, 0.85f);
        toastGO.SetActive(false);
        overlayGO.SetActive(false);

        // 9. Auto-bind all references on KitchenUI
        kitchenUI.AutoBindReferences();

        // 10. Save as Prefab Asset
        string prefDir = Path.GetDirectoryName(PREFAB_PATH);
        if (!Directory.Exists(prefDir))
        {
            Directory.CreateDirectory(prefDir);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGO, PREFAB_PATH, InteractionMode.AutomatedAction);
        Debug.Log($"[KitchenCanvasBuilder] Successfully saved prefab at {PREFAB_PATH}");

        // 11. Also place in Base.unity scene if Base scene is open or save into it
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == SCENE_PATH)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[KitchenCanvasBuilder] Saved KitchenCanvas directly into active Base.unity scene.");
        }
        else
        {
            // Open Base scene to ensure KitchenCanvas is present in it
            var openedScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            // Check if already in scene
            var existing = GameObject.Find("KitchenCanvas");
            if (existing != null && existing != canvasGO)
            {
                Object.DestroyImmediate(existing);
            }
            PrefabUtility.InstantiatePrefab(prefab, openedScene);
            EditorSceneManager.MarkSceneDirty(openedScene);
            EditorSceneManager.SaveScene(openedScene);
            Debug.Log("[KitchenCanvasBuilder] KitchenCanvas added to Base.unity scene.");
        }

        return canvasGO;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetStretch(RectTransform rt, float margin = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    private static Button CreateTabButton(string name, Transform parent, string label, Color bgColor, Color textColor, TMP_FontAsset font)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();

        GameObject textGO = CreateUIObject("Label", go.transform);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        SetStretch(textRT);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;

        return btn;
    }

    private static Button CreateActionButton(string name, Transform parent, string label, Color bgColor, Color textColor, TMP_FontAsset font)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();

        GameObject textGO = CreateUIObject("ActionText", go.transform);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        SetStretch(textRT);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;

        return btn;
    }

    private static void CreatePotSlot(string name, Transform parent, Sprite slotFrameSprite, TMP_FontAsset font)
    {
        GameObject slotGO = CreateUIObject(name, parent);
        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(84, 84);

        Image slotBg = slotGO.AddComponent<Image>();
        slotBg.color = new Color(0.96f, 0.93f, 0.87f, 1f);
        if (slotFrameSprite != null)
        {
            slotBg.sprite = slotFrameSprite;
            slotBg.type = Image.Type.Sliced;
        }

        Button btn = slotGO.AddComponent<Button>();

        // Highlight Border
        GameObject hlGO = CreateUIObject("HighlightBorder", slotGO.transform);
        RectTransform hlRT = hlGO.GetComponent<RectTransform>();
        SetStretch(hlRT, -3);
        Image hlImg = hlGO.AddComponent<Image>();
        hlImg.color = new Color(0.24f, 0.60f, 0.96f, 0.85f); // Vibrant blue highlight border
        hlGO.SetActive(false);

        // Item Icon
        GameObject iconGO = CreateUIObject("ItemIcon", slotGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        SetStretch(iconRT, 12);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.white;
        iconGO.SetActive(false);

        // Count Text
        GameObject countGO = CreateUIObject("CountText", slotGO.transform);
        RectTransform countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0.4f, 0f);
        countRT.anchorMax = new Vector2(1f, 0.45f);
        countRT.offsetMin = Vector2.zero;
        countRT.offsetMax = new Vector2(-4, 2);
        TextMeshProUGUI countTMP = countGO.AddComponent<TextMeshProUGUI>();
        if (font != null) countTMP.font = font;
        countTMP.text = "x1";
        countTMP.fontSize = 15;
        countTMP.fontStyle = FontStyles.Bold;
        countTMP.alignment = TextAlignmentOptions.BottomRight;
        countTMP.color = Color.black;
        countGO.SetActive(false);
    }

    private static GameObject CreateRecipeCardTemplate(Transform parent, Sprite cardBg, Sprite iconFrame, Sprite defaultDish, TMP_FontAsset font)
    {
        GameObject cardGO = CreateUIObject("RecipeCardTemplate", parent);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(415, 94);

        Image bg = cardGO.AddComponent<Image>();
        bg.color = Color.white;
        if (cardBg != null)
        {
            bg.sprite = cardBg;
            bg.type = Image.Type.Sliced;
        }

        RecipeCardUI cardUI = cardGO.AddComponent<RecipeCardUI>();
        cardUI.cardBackground = bg;

        // Icon Frame
        GameObject iconFrameGO = CreateUIObject("IconFrame", cardGO.transform);
        RectTransform ifRT = iconFrameGO.GetComponent<RectTransform>();
        ifRT.anchorMin = new Vector2(0, 0.5f);
        ifRT.anchorMax = new Vector2(0, 0.5f);
        ifRT.pivot = new Vector2(0, 0.5f);
        ifRT.anchoredPosition = new Vector2(12, 0);
        ifRT.sizeDelta = new Vector2(68, 68);

        Image ifImg = iconFrameGO.AddComponent<Image>();
        ifImg.color = Color.white;
        if (iconFrame != null)
        {
            ifImg.sprite = iconFrame;
            ifImg.type = Image.Type.Sliced;
        }
        cardUI.iconFrame = ifImg;

        GameObject dishIconGO = CreateUIObject("DishIcon", iconFrameGO.transform);
        RectTransform diRT = dishIconGO.GetComponent<RectTransform>();
        SetStretch(diRT, 6);
        Image diImg = dishIconGO.AddComponent<Image>();
        if (defaultDish != null) diImg.sprite = defaultDish;
        cardUI.dishIcon = diImg;

        // Info Column
        GameObject infoGO = CreateUIObject("InfoColumn", cardGO.transform);
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 0);
        infoRT.anchorMax = new Vector2(1, 1);
        infoRT.offsetMin = new Vector2(90, 8);
        infoRT.offsetMax = new Vector2(-115, -8);

        // Title Row (Name + Stars)
        GameObject titleRowGO = CreateUIObject("TitleRow", infoGO.transform);
        RectTransform trRT = titleRowGO.GetComponent<RectTransform>();
        trRT.anchorMin = new Vector2(0, 1);
        trRT.anchorMax = new Vector2(1, 1);
        trRT.pivot = new Vector2(0, 1);
        trRT.anchoredPosition = new Vector2(0, 0);
        trRT.sizeDelta = new Vector2(0, 26);

        GameObject titleTextGO = CreateUIObject("TitleText", titleRowGO.transform);
        RectTransform ttRT = titleTextGO.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0, 0);
        ttRT.anchorMax = new Vector2(0.72f, 1);
        ttRT.offsetMin = Vector2.zero;
        ttRT.offsetMax = Vector2.zero;
        TextMeshProUGUI ttTMP = titleTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) ttTMP.font = font;
        ttTMP.text = "Súp Cà Chua Đậm Đà";
        ttTMP.fontSize = 16;
        ttTMP.fontStyle = FontStyles.Bold;
        ttTMP.color = new Color(0.24f, 0.14f, 0.08f);
        cardUI.titleText = ttTMP;

        GameObject starsTextGO = CreateUIObject("StarsText", titleRowGO.transform);
        RectTransform stRT = starsTextGO.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0.72f, 0);
        stRT.anchorMax = new Vector2(1, 1);
        stRT.offsetMin = Vector2.zero;
        stRT.offsetMax = Vector2.zero;
        TextMeshProUGUI stTMP = starsTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) stTMP.font = font;
        stTMP.text = "⭐⭐⭐";
        stTMP.fontSize = 14;
        stTMP.alignment = TextAlignmentOptions.MidlineRight;
        stTMP.color = new Color(1f, 0.78f, 0.18f); // Gold stars
        cardUI.starsText = stTMP;

        // Detail Text (HP, Hunger, Ingredients)
        GameObject detailGO = CreateUIObject("DetailText", infoGO.transform);
        RectTransform dtRT = detailGO.GetComponent<RectTransform>();
        dtRT.anchorMin = new Vector2(0, 0);
        dtRT.anchorMax = new Vector2(1, 0.65f);
        dtRT.offsetMin = Vector2.zero;
        dtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI dtTMP = detailGO.AddComponent<TextMeshProUGUI>();
        if (font != null) dtTMP.font = font;
        dtTMP.text = "Hồi 45 HP & 60 Độ No | Cần: Cà Chua x2 + Nước x1";
        dtTMP.fontSize = 13;
        dtTMP.color = new Color(0.42f, 0.30f, 0.22f);
        cardUI.detailText = dtTMP;

        // Action Button
        GameObject actBtnGO = CreateUIObject("ActionButton", cardGO.transform);
        RectTransform abRT = actBtnGO.GetComponent<RectTransform>();
        abRT.anchorMin = new Vector2(1, 0.5f);
        abRT.anchorMax = new Vector2(1, 0.5f);
        abRT.pivot = new Vector2(1, 0.5f);
        abRT.anchoredPosition = new Vector2(-12, 0);
        abRT.sizeDelta = new Vector2(100, 38);

        Image abImg = actBtnGO.AddComponent<Image>();
        abImg.color = new Color(0.48f, 0.32f, 0.22f, 1f); // Cocoa brown
        Button abBtn = actBtnGO.AddComponent<Button>();
        cardUI.actionButton = abBtn;
        cardUI.actionButtonBackground = abImg;

        GameObject actTextGO = CreateUIObject("ActionText", actBtnGO.transform);
        RectTransform atRT = actTextGO.GetComponent<RectTransform>();
        SetStretch(atRT);
        TextMeshProUGUI atTMP = actTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) atTMP.font = font;
        atTMP.text = "TỰ ĐIỀN";
        atTMP.fontSize = 14;
        atTMP.fontStyle = FontStyles.Bold;
        atTMP.alignment = TextAlignmentOptions.Center;
        atTMP.color = Color.white;
        cardUI.actionButtonText = atTMP;

        return cardGO;
    }
}
