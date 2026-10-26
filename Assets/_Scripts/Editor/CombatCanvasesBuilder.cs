using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Combat and Reward Canvases:
/// 1. LootCanvas (Combat loot drop modal: Dropped Loot vs Backpack 50/50 layout)
/// 2. RewardCanvas (Treasure Vault / Cache room: Chest contents vs Backpack)
/// 3. FloorVictoryCanvas (Floor cleared modal: Stats enhancement, Ancient blessings, Floor cache)
/// 4. DungeonSummaryCanvas (Expedition completion / defeat recap)
/// directly inside Dungeon.unity as scene-authored Canvases.
/// Matches 100% the layout, sizes, colors, fonts, hierarchy, and visuals of existing UI builders.
/// All button events are wired as persistent listeners.
/// </summary>
public static class CombatCanvasesBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Dungeon.unity";
    private const string LOOT_CANVAS_NAME = "LootCanvas";
    private const string REWARD_CANVAS_NAME = "RewardCanvas";
    private const string VICTORY_CANVAS_NAME = "FloorVictoryCanvas";
    private const string SUMMARY_CANVAS_NAME = "DungeonSummaryCanvas";

    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SLOT_PREFAB_PATH = "Assets/_Assets/_Prefabs/inventorySlot.prefab";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build All Combat Canvases")]
    public static void BuildAllCombatCanvases()
    {
        EnsureSceneLoaded();
        CleanupCombatCanvas();
        BuildLootCanvas();
        BuildRewardCanvas();
        BuildFloorVictoryCanvas();
        BuildDungeonSummaryCanvas();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[CombatCanvasesBuilder] SUCCESS: Built all 4 combat canvases in Dungeon.unity!");
    }

    [MenuItem("Tools/Build Loot Canvas")]
    public static GameObject BuildLootCanvasMenu()
    {
        EnsureSceneLoaded();
        var go = BuildLootCanvas();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return go;
    }

    [MenuItem("Tools/Build Reward Canvas")]
    public static GameObject BuildRewardCanvasMenu()
    {
        EnsureSceneLoaded();
        var go = BuildRewardCanvas();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return go;
    }

    [MenuItem("Tools/Build Floor Victory Canvas")]
    public static GameObject BuildFloorVictoryCanvasMenu()
    {
        EnsureSceneLoaded();
        var go = BuildFloorVictoryCanvas();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return go;
    }

    [MenuItem("Tools/Build Dungeon Summary Canvas")]
    public static GameObject BuildDungeonSummaryCanvasMenu()
    {
        EnsureSceneLoaded();
        var go = BuildDungeonSummaryCanvas();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        return go;
    }

    private static void EnsureSceneLoaded()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }
        EnsureFontLoaded();
    }

    private static void EnsureFontLoaded()
    {
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (font == null) font = DungeonUIAssetHelper.GetFontAsset();
        }
    }

    private static void CleanupCombatCanvas()
    {
        GameObject combatCanvasGO = GameObject.Find("CombatCanvas");
        if (combatCanvasGO != null)
        {
            // Remove any dangling RewardUI from CombatCanvas
            RewardUI[] rUIs = combatCanvasGO.GetComponents<RewardUI>();
            for (int i = 0; i < rUIs.Length; i++)
            {
                if (rUIs[i] != null) Undo.DestroyObjectImmediate(rUIs[i]);
            }

            // Hook targetReticle if unassigned
            CombatUI combatUI = combatCanvasGO.GetComponent<CombatUI>();
            if (combatUI != null && combatUI.targetReticle == null)
            {
                GameObject reticleGO = GameObject.Find("TargetReticle");
                if (reticleGO != null)
                {
                    combatUI.targetReticle = reticleGO.transform;
                    EditorUtility.SetDirty(combatUI);
                }
            }
        }
    }

    public static GameObject BuildLootCanvas()
    {
        GameObject existing = GameObject.Find(LOOT_CANVAS_NAME);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject canvasGO = CreateRootCanvas(LOOT_CANVAS_NAME, 110);
        LootUI ui = canvasGO.AddComponent<LootUI>();
        Canvas c = canvasGO.GetComponent<Canvas>();
        ui.canvas = c;

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
        ui.slotPrefab = slotPrefab;

        // 1. Overlay
        GameObject overlayGO = CreateUIElement("LootPanel", canvasGO.transform);
        StretchFull(overlayGO.GetComponent<RectTransform>());
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
        ui.lootPanel = overlayGO;

        // 2. Modal Frame
        GameObject modalGO = CreateUIElement("LootModal", overlayGO.transform);
        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.sizeDelta = new Vector2(940, 580);
        Image modalImg = modalGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Header
        GameObject headerGO = CreateUIElement("Header", modalGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 74);
        Image headerImg = headerGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.22f, 0.16f, 0.06f, 1f));

        GameObject iconGO = CreateUIElement("ChestIcon", headerGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(38, 0);
        iconRT.sizeDelta = new Vector2(48, 48);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = DungeonUIAssetHelper.GetChestSprite();

        GameObject titleGO = CreateUIElement("TitleText", headerGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(76, 24);
        titleRT.offsetMax = new Vector2(-20, -6);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(titleTMP);
        titleTMP.text = "ENEMY LOOT DROPS";
        titleTMP.fontSize = 20f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.85f, 0.25f);
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ui.titleText = titleTMP;

        GameObject subGO = CreateUIElement("SubtitleText", headerGO.transform);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = Vector2.one;
        subRT.offsetMin = new Vector2(76, 4);
        subRT.offsetMax = new Vector2(-20, -32);
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(subTMP);
        subTMP.text = "Drag and drop items into your Backpack, or click [LOOT ALL] to collect everything.";
        subTMP.fontSize = 12f;
        subTMP.color = new Color(0.85f, 0.88f, 0.95f);
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ui.subtitleText = subTMP;

        // 4. Body
        GameObject bodyGO = CreateUIElement("Body", modalGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(20, 68);
        bodyRT.offsetMax = new Vector2(-20, -82);

        // Left Column (Loot)
        GameObject leftColGO = CreateUIElement("Left_LootColumn", bodyGO.transform);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.488f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = Vector2.zero;
        Image leftBg = leftColGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftBg, new Color(0.14f, 0.16f, 0.22f, 0.95f));

        GameObject leftHeaderGO = CreateUIElement("HeaderBar", leftColGO.transform);
        RectTransform leftHeaderRT = leftHeaderGO.GetComponent<RectTransform>();
        leftHeaderRT.anchorMin = new Vector2(0, 1);
        leftHeaderRT.anchorMax = new Vector2(1, 1);
        leftHeaderRT.pivot = new Vector2(0.5f, 1);
        leftHeaderRT.sizeDelta = new Vector2(-12, 32);
        leftHeaderRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StyleSlicedFrame(leftHeaderGO.AddComponent<Image>(), new Color(0.19f, 0.22f, 0.30f, 1f));

        GameObject leftLabelGO = CreateUIElement("Label", leftHeaderGO.transform);
        StretchFull(leftLabelGO.GetComponent<RectTransform>());
        RectTransform llRT = leftLabelGO.GetComponent<RectTransform>();
        llRT.offsetMin = new Vector2(10, 0);
        llRT.offsetMax = new Vector2(-10, 0);
        TextMeshProUGUI leftLabelTMP = leftLabelGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(leftLabelTMP);
        leftLabelTMP.text = "DROPPED LOOT";
        leftLabelTMP.fontSize = 12.5f;
        leftLabelTMP.fontStyle = FontStyles.Bold;
        leftLabelTMP.color = new Color(1f, 0.85f, 0.35f);
        leftLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ui.lootHeaderTitle = leftLabelTMP;

        GameObject leftScrollGO = CreateUIElement("ScrollView", leftColGO.transform);
        RectTransform leftScrollRT = leftScrollGO.GetComponent<RectTransform>();
        leftScrollRT.anchorMin = Vector2.zero;
        leftScrollRT.anchorMax = Vector2.one;
        leftScrollRT.offsetMin = new Vector2(8, 8);
        leftScrollRT.offsetMax = new Vector2(-8, -42);
        Image leftScrollImg = leftScrollGO.AddComponent<Image>();
        leftScrollImg.color = new Color(0, 0, 0, 0.2f);
        ScrollRect leftSR = leftScrollGO.AddComponent<ScrollRect>();
        leftSR.horizontal = false;
        leftSR.vertical = true;
        leftSR.scrollSensitivity = 25f;
        leftSR.movementType = ScrollRect.MovementType.Clamped;
        ui.lootScrollRect = leftSR;

        GameObject leftViewGO = CreateUIElement("Viewport", leftScrollGO.transform);
        RectTransform leftViewRT = leftViewGO.GetComponent<RectTransform>();
        leftViewRT.anchorMin = Vector2.zero;
        leftViewRT.anchorMax = Vector2.one;
        leftViewRT.offsetMin = Vector2.zero;
        leftViewRT.offsetMax = new Vector2(-14, 0);
        leftViewGO.AddComponent<RectMask2D>();
        leftSR.viewport = leftViewRT;

        GameObject leftContentGO = CreateUIElement("Content", leftViewGO.transform);
        RectTransform leftContentRT = leftContentGO.GetComponent<RectTransform>();
        leftContentRT.anchorMin = new Vector2(0, 1);
        leftContentRT.anchorMax = new Vector2(1, 1);
        leftContentRT.pivot = new Vector2(0.5f, 1);
        leftContentRT.sizeDelta = new Vector2(0, 300);
        GridLayoutGroup leftGrid = leftContentGO.AddComponent<GridLayoutGroup>();
        leftGrid.cellSize = new Vector2(54, 54);
        leftGrid.spacing = new Vector2(6, 6);
        leftGrid.padding = new RectOffset(8, 8, 8, 8);
        leftGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        leftGrid.constraintCount = 6;
        ContentSizeFitter leftCsf = leftContentGO.AddComponent<ContentSizeFitter>();
        leftCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        leftSR.content = leftContentRT;
        ui.lootSlotsParent = leftContentGO.transform;

        DungeonUIAssetHelper.CreateVerticalScrollbar(leftScrollGO.transform, leftSR, new Color(0.75f, 0.60f, 0.30f, 0.95f));

        // Right Column (Backpack)
        GameObject rightColGO = CreateUIElement("Right_BackpackColumn", bodyGO.transform);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.512f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = Vector2.zero;
        rightColRT.offsetMax = Vector2.zero;
        Image rightBg = rightColGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightBg, new Color(0.14f, 0.16f, 0.22f, 0.95f));

        GameObject rightHeaderGO = CreateUIElement("HeaderBar", rightColGO.transform);
        RectTransform rightHeaderRT = rightHeaderGO.GetComponent<RectTransform>();
        rightHeaderRT.anchorMin = new Vector2(0, 1);
        rightHeaderRT.anchorMax = new Vector2(1, 1);
        rightHeaderRT.pivot = new Vector2(0.5f, 1);
        rightHeaderRT.sizeDelta = new Vector2(-12, 32);
        rightHeaderRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StyleSlicedFrame(rightHeaderGO.AddComponent<Image>(), new Color(0.19f, 0.22f, 0.30f, 1f));

        GameObject bpTitleGO = CreateUIElement("BackpackTitle", rightHeaderGO.transform);
        StretchFull(bpTitleGO.GetComponent<RectTransform>());
        RectTransform bpRT = bpTitleGO.GetComponent<RectTransform>();
        bpRT.offsetMin = new Vector2(10, 0);
        bpRT.offsetMax = new Vector2(-10, 0);
        TextMeshProUGUI bpTMP = bpTitleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(bpTMP);
        bpTMP.text = "YOUR BACKPACK";
        bpTMP.fontSize = 12.5f;
        bpTMP.fontStyle = FontStyles.Bold;
        bpTMP.color = new Color(0.55f, 0.95f, 1f);
        bpTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ui.backpackHeaderTitle = bpTMP;

        GameObject rightScrollGO = CreateUIElement("ScrollView", rightColGO.transform);
        RectTransform rightScrollRT = rightScrollGO.GetComponent<RectTransform>();
        rightScrollRT.anchorMin = Vector2.zero;
        rightScrollRT.anchorMax = Vector2.one;
        rightScrollRT.offsetMin = new Vector2(8, 8);
        rightScrollRT.offsetMax = new Vector2(-8, -42);
        Image rightScrollImg = rightScrollGO.AddComponent<Image>();
        rightScrollImg.color = new Color(0, 0, 0, 0.2f);
        ScrollRect rightSR = rightScrollGO.AddComponent<ScrollRect>();
        rightSR.horizontal = false;
        rightSR.vertical = true;
        rightSR.scrollSensitivity = 25f;
        rightSR.movementType = ScrollRect.MovementType.Clamped;
        ui.bpScrollRect = rightSR;

        GameObject rightViewGO = CreateUIElement("Viewport", rightScrollGO.transform);
        RectTransform rightViewRT = rightViewGO.GetComponent<RectTransform>();
        rightViewRT.anchorMin = Vector2.zero;
        rightViewRT.anchorMax = Vector2.one;
        rightViewRT.offsetMin = Vector2.zero;
        rightViewRT.offsetMax = new Vector2(-14, 0);
        rightViewGO.AddComponent<RectMask2D>();
        rightSR.viewport = rightViewRT;

        GameObject rightContentGO = CreateUIElement("Content", rightViewGO.transform);
        RectTransform rightContentRT = rightContentGO.GetComponent<RectTransform>();
        rightContentRT.anchorMin = new Vector2(0, 1);
        rightContentRT.anchorMax = new Vector2(1, 1);
        rightContentRT.pivot = new Vector2(0.5f, 1);
        rightContentRT.sizeDelta = new Vector2(0, 300);
        GridLayoutGroup rightGrid = rightContentGO.AddComponent<GridLayoutGroup>();
        rightGrid.cellSize = new Vector2(54, 54);
        rightGrid.spacing = new Vector2(6, 6);
        rightGrid.padding = new RectOffset(8, 8, 8, 8);
        rightGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rightGrid.constraintCount = 6;
        ContentSizeFitter rightCsf = rightContentGO.AddComponent<ContentSizeFitter>();
        rightCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rightSR.content = rightContentRT;
        ui.backpackSlotsParent = rightContentGO.transform;

        DungeonUIAssetHelper.CreateVerticalScrollbar(rightScrollGO.transform, rightSR, new Color(0.75f, 0.60f, 0.30f, 0.95f));

        // 5. Controls
        GameObject hintGO = CreateUIElement("HintText", modalGO.transform);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0.58f, 0);
        hintRT.offsetMin = new Vector2(24, 14);
        hintRT.offsetMax = new Vector2(0, 50);
        TextMeshProUGUI hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(hintTMP);
        hintTMP.fontSize = 12f;
        hintTMP.alignment = TextAlignmentOptions.MidlineLeft;
        hintTMP.text = "Drag and drop items into your Backpack, or click [LOOT ALL] to collect everything.";
        ui.hintText = hintTMP;

        // Button Loot All
        GameObject lootAllBtnGO = CreateUIElement("LootAllButton", modalGO.transform);
        RectTransform laRT = lootAllBtnGO.GetComponent<RectTransform>();
        laRT.anchorMin = new Vector2(1, 0);
        laRT.anchorMax = new Vector2(1, 0);
        laRT.pivot = new Vector2(1, 0);
        laRT.anchoredPosition = new Vector2(-180, 14);
        laRT.sizeDelta = new Vector2(145, 38);
        Button laBtn = lootAllBtnGO.AddComponent<Button>();
        Image laImg = lootAllBtnGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleButton(laBtn, laImg, new Color(0.85f, 0.65f, 0.15f));
        UnityEventTools.AddPersistentListener(laBtn.onClick, ui.OnLootAllClicked);
        ui.lootAllButton = laBtn;
        ui.lootAllBtnImg = laImg;

        GameObject laTxtGO = CreateUIElement("Text", lootAllBtnGO.transform);
        StretchFull(laTxtGO.GetComponent<RectTransform>());
        TextMeshProUGUI laTxtTMP = laTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(laTxtTMP);
        laTxtTMP.text = "LOOT ALL";
        laTxtTMP.fontSize = 13f;
        laTxtTMP.fontStyle = FontStyles.Bold;
        laTxtTMP.alignment = TextAlignmentOptions.Center;
        laTxtTMP.color = Color.white;
        ui.lootAllTxt = laTxtTMP;

        // Button Continue
        GameObject contBtnGO = CreateUIElement("ContinueButton", modalGO.transform);
        RectTransform contRT = contBtnGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(1, 0);
        contRT.anchorMax = new Vector2(1, 0);
        contRT.pivot = new Vector2(1, 0);
        contRT.anchoredPosition = new Vector2(-20, 14);
        contRT.sizeDelta = new Vector2(145, 38);
        Button contBtn = contBtnGO.AddComponent<Button>();
        Image contImg = contBtnGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleButton(contBtn, contImg, new Color(0.18f, 0.55f, 0.28f));
        UnityEventTools.AddPersistentListener(contBtn.onClick, ui.OnContinueClicked);
        ui.continueButton = contBtn;
        ui.contBtnImg = contImg;

        GameObject contTxtGO = CreateUIElement("Text", contBtnGO.transform);
        StretchFull(contTxtGO.GetComponent<RectTransform>());
        TextMeshProUGUI contTxtTMP = contTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(contTxtTMP);
        contTxtTMP.text = "CONTINUE [->]";
        contTxtTMP.fontSize = 13f;
        contTxtTMP.fontStyle = FontStyles.Bold;
        contTxtTMP.alignment = TextAlignmentOptions.Center;
        contTxtTMP.color = Color.white;
        ui.contTxt = contTxtTMP;

        overlayGO.SetActive(false);
        EditorUtility.SetDirty(ui);
        return canvasGO;
    }

    public static GameObject BuildRewardCanvas()
    {
        GameObject existing = GameObject.Find(REWARD_CANVAS_NAME);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject canvasGO = CreateRootCanvas(REWARD_CANVAS_NAME, 112);
        RewardUI ui = canvasGO.AddComponent<RewardUI>();
        Canvas c = canvasGO.GetComponent<Canvas>();
        ui.canvas = c;

        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
        ui.slotPrefab = slotPrefab;

        // 1. Overlay
        GameObject overlayGO = CreateUIElement("RewardPanel", canvasGO.transform);
        StretchFull(overlayGO.GetComponent<RectTransform>());
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        ui.rewardPanel = overlayGO;

        // 2. Modal
        GameObject modalGO = CreateUIElement("RewardModal", overlayGO.transform);
        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.sizeDelta = new Vector2(920, 580);
        Image modalImg = modalGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Header
        GameObject headerGO = CreateUIElement("Header", modalGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 76);
        Image headerImg = headerGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.24f, 0.18f, 0.08f, 1f));

        GameObject iconGO = CreateUIElement("ChestIcon", headerGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(42, 0);
        iconRT.sizeDelta = new Vector2(50, 50);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = DungeonUIAssetHelper.GetChestSprite();
        ui.chestHeaderIcon = iconImg;

        GameObject titleGO = CreateUIElement("TitleText", headerGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(80, 24);
        titleRT.offsetMax = new Vector2(-20, -6);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(titleTMP);
        titleTMP.text = "TREASURE VAULT - STAGE REWARD";
        titleTMP.fontSize = 21f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.88f, 0.38f);
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ui.titleText = titleTMP;

        GameObject subGO = CreateUIElement("SubtitleText", headerGO.transform);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = Vector2.one;
        subRT.offsetMin = new Vector2(80, 6);
        subRT.offsetMax = new Vector2(-20, -36);
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(subTMP);
        subTMP.text = "An ancient treasure cache has unlocked! Claim seeds, rare crops, and provisions.";
        subTMP.fontSize = 12.5f;
        subTMP.color = new Color(0.92f, 0.88f, 0.78f);
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;
        ui.subtitleText = subTMP;

        // 4. Body
        GameObject bodyGO = CreateUIElement("Body", modalGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(16, 64);
        bodyRT.offsetMax = new Vector2(-16, -84);

        // Left Panel (Chest items)
        GameObject leftPanelGO = CreateUIElement("LeftChestPanel", bodyGO.transform);
        RectTransform leftPanelRT = leftPanelGO.GetComponent<RectTransform>();
        leftPanelRT.anchorMin = new Vector2(0, 0);
        leftPanelRT.anchorMax = new Vector2(0.50f, 1);
        leftPanelRT.pivot = new Vector2(0.5f, 0.5f);
        leftPanelRT.offsetMin = Vector2.zero;
        leftPanelRT.offsetMax = new Vector2(-6, 0);
        Image leftImg = leftPanelGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftImg, new Color(0.12f, 0.14f, 0.20f, 0.95f));

        GameObject lhGO = CreateUIElement("LeftHeader", leftPanelGO.transform);
        RectTransform lhRT = lhGO.GetComponent<RectTransform>();
        lhRT.anchorMin = new Vector2(0, 1);
        lhRT.anchorMax = new Vector2(1, 1);
        lhRT.pivot = new Vector2(0.5f, 1);
        lhRT.anchoredPosition = new Vector2(0, -6);
        lhRT.sizeDelta = new Vector2(-16, 32);
        TextMeshProUGUI lhTMP = lhGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(lhTMP);
        lhTMP.fontSize = 13.5f;
        lhTMP.fontStyle = FontStyles.Bold;
        lhTMP.color = new Color(1f, 0.85f, 0.3f);
        lhTMP.alignment = TextAlignmentOptions.Center;
        lhTMP.text = "CHEST CACHE CONTENTS";
        ui.leftHeaderTitle = lhTMP;

        GameObject chestScrollGO = CreateUIElement("ChestScrollArea", leftPanelGO.transform);
        RectTransform csRT = chestScrollGO.GetComponent<RectTransform>();
        csRT.anchorMin = Vector2.zero;
        csRT.anchorMax = Vector2.one;
        csRT.pivot = new Vector2(0.5f, 0.5f);
        csRT.offsetMin = new Vector2(8, 8);
        csRT.offsetMax = new Vector2(-8, -42);
        ScrollRect csSR = chestScrollGO.AddComponent<ScrollRect>();
        csSR.horizontal = false;
        csSR.vertical = true;
        csSR.movementType = ScrollRect.MovementType.Clamped;
        csSR.scrollSensitivity = 25f;

        GameObject csVpGO = CreateUIElement("Viewport", chestScrollGO.transform);
        RectTransform csVpRT = csVpGO.GetComponent<RectTransform>();
        csVpRT.anchorMin = Vector2.zero;
        csVpRT.anchorMax = Vector2.one;
        csVpRT.pivot = new Vector2(0.5f, 0.5f);
        csVpRT.offsetMin = Vector2.zero;
        csVpRT.offsetMax = new Vector2(-16, 0);
        csVpGO.AddComponent<RectMask2D>();
        csSR.viewport = csVpRT;

        GameObject csItemsGO = CreateUIElement("ChestItemsContainer", csVpGO.transform);
        RectTransform csItemsRT = csItemsGO.GetComponent<RectTransform>();
        csItemsRT.anchorMin = new Vector2(0, 1);
        csItemsRT.anchorMax = new Vector2(1, 1);
        csItemsRT.pivot = new Vector2(0.5f, 1);
        csItemsRT.anchoredPosition = Vector2.zero;
        csItemsRT.sizeDelta = Vector2.zero;
        VerticalLayoutGroup csVLG = csItemsGO.AddComponent<VerticalLayoutGroup>();
        csVLG.spacing = 8;
        csVLG.padding = new RectOffset(4, 4, 4, 4);
        csVLG.childControlHeight = true;
        csVLG.childControlWidth = true;
        csVLG.childForceExpandHeight = false;
        csVLG.childForceExpandWidth = true;
        ContentSizeFitter csCsf = csItemsGO.AddComponent<ContentSizeFitter>();
        csCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csSR.content = csItemsRT;
        ui.chestItemsContainer = csItemsRT;

        DungeonUIAssetHelper.CreateVerticalScrollbar(chestScrollGO.transform, csSR, new Color(0.85f, 0.65f, 0.2f, 1f));

        // Right Panel (Backpack)
        GameObject rightPanelGO = CreateUIElement("RightBackpackPanel", bodyGO.transform);
        RectTransform rightPanelRT = rightPanelGO.GetComponent<RectTransform>();
        rightPanelRT.anchorMin = new Vector2(0.50f, 0);
        rightPanelRT.anchorMax = new Vector2(1, 1);
        rightPanelRT.pivot = new Vector2(0.5f, 0.5f);
        rightPanelRT.offsetMin = new Vector2(6, 0);
        rightPanelRT.offsetMax = Vector2.zero;
        Image rightImg = rightPanelGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightImg, new Color(0.12f, 0.14f, 0.20f, 0.95f));

        GameObject rhGO = CreateUIElement("RightHeader", rightPanelGO.transform);
        RectTransform rhRT = rhGO.GetComponent<RectTransform>();
        rhRT.anchorMin = new Vector2(0, 1);
        rhRT.anchorMax = new Vector2(1, 1);
        rhRT.pivot = new Vector2(0.5f, 1);
        rhRT.anchoredPosition = new Vector2(0, -6);
        rhRT.sizeDelta = new Vector2(-16, 32);
        TextMeshProUGUI rhTMP = rhGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(rhTMP);
        rhTMP.fontSize = 13.5f;
        rhTMP.fontStyle = FontStyles.Bold;
        rhTMP.color = new Color(0.4f, 0.85f, 1f);
        rhTMP.alignment = TextAlignmentOptions.Center;
        rhTMP.text = "YOUR BACKPACK";
        ui.backpackHeaderTitle = rhTMP;

        GameObject bpScrollGO = CreateUIElement("BackpackScrollArea", rightPanelGO.transform);
        RectTransform bpSRT = bpScrollGO.GetComponent<RectTransform>();
        bpSRT.anchorMin = Vector2.zero;
        bpSRT.anchorMax = Vector2.one;
        bpSRT.pivot = new Vector2(0.5f, 0.5f);
        bpSRT.offsetMin = new Vector2(8, 8);
        bpSRT.offsetMax = new Vector2(-8, -42);
        ScrollRect bpSR = bpScrollGO.AddComponent<ScrollRect>();
        bpSR.horizontal = false;
        bpSR.vertical = true;
        bpSR.movementType = ScrollRect.MovementType.Clamped;
        bpSR.scrollSensitivity = 25f;

        GameObject bpVpGO = CreateUIElement("Viewport", bpScrollGO.transform);
        RectTransform bpVpRT = bpVpGO.GetComponent<RectTransform>();
        bpVpRT.anchorMin = Vector2.zero;
        bpVpRT.anchorMax = Vector2.one;
        bpVpRT.pivot = new Vector2(0.5f, 0.5f);
        bpVpRT.offsetMin = Vector2.zero;
        bpVpRT.offsetMax = new Vector2(-16, 0);
        bpVpGO.AddComponent<RectMask2D>();
        bpSR.viewport = bpVpRT;

        GameObject bpSlotsGO = CreateUIElement("BackpackSlots", bpVpGO.transform);
        RectTransform bpSlotsRT = bpSlotsGO.GetComponent<RectTransform>();
        bpSlotsRT.anchorMin = new Vector2(0, 1);
        bpSlotsRT.anchorMax = new Vector2(1, 1);
        bpSlotsRT.pivot = new Vector2(0.5f, 1);
        bpSlotsRT.anchoredPosition = Vector2.zero;
        bpSlotsRT.sizeDelta = Vector2.zero;
        GridLayoutGroup bpGrid = bpSlotsGO.AddComponent<GridLayoutGroup>();
        bpGrid.cellSize = new Vector2(52, 52);
        bpGrid.spacing = new Vector2(6, 6);
        bpGrid.padding = new RectOffset(6, 6, 6, 6);
        bpGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGrid.constraintCount = 6;
        bpGrid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter bpCsf = bpSlotsGO.AddComponent<ContentSizeFitter>();
        bpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bpSR.content = bpSlotsRT;
        ui.backpackSlotsParent = bpSlotsRT;

        DungeonUIAssetHelper.CreateVerticalScrollbar(bpScrollGO.transform, bpSR, new Color(0.25f, 0.42f, 0.65f, 1f));

        // 5. Footer
        GameObject footerGO = CreateUIElement("Footer", modalGO.transform);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.sizeDelta = new Vector2(0, 60);

        GameObject hintGO = CreateUIElement("HintText", footerGO.transform);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0.52f, 1);
        hintRT.offsetMin = new Vector2(18, 4);
        hintRT.offsetMax = new Vector2(-10, -4);
        TextMeshProUGUI hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(hintTMP);
        hintTMP.fontSize = 12f;
        hintTMP.color = new Color(0.85f, 0.9f, 0.96f);
        hintTMP.alignment = TextAlignmentOptions.MidlineLeft;
        hintTMP.text = "Items collected will be safely stored in your backpack.";
        ui.hintText = hintTMP;

        // Button Claim All
        GameObject laBtnGO = CreateUIElement("BtnLootAll", footerGO.transform);
        RectTransform laRT = laBtnGO.GetComponent<RectTransform>();
        laRT.anchorMin = new Vector2(0.54f, 0.5f);
        laRT.anchorMax = new Vector2(0.54f, 0.5f);
        laRT.pivot = new Vector2(0, 0.5f);
        laRT.sizeDelta = new Vector2(180, 42);
        laRT.anchoredPosition = Vector2.zero;
        Button laBtn = laBtnGO.AddComponent<Button>();
        Image laImg = laBtnGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(laImg, new Color(0.18f, 0.65f, 0.32f, 1f));
        UnityEventTools.AddPersistentListener(laBtn.onClick, ui.OnLootAllClicked);
        ui.lootAllButton = laBtn;

        GameObject laTxtGO = CreateUIElement("Text", laBtnGO.transform);
        StretchFull(laTxtGO.GetComponent<RectTransform>());
        TextMeshProUGUI laTxtTMP = laTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(laTxtTMP);
        laTxtTMP.fontSize = 13f;
        laTxtTMP.fontStyle = FontStyles.Bold;
        laTxtTMP.alignment = TextAlignmentOptions.Center;
        laTxtTMP.color = Color.white;
        laTxtTMP.text = "CLAIM ALL";

        // Button Continue
        GameObject contBtnGO = CreateUIElement("BtnContinue", footerGO.transform);
        RectTransform contRT = contBtnGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(1, 0.5f);
        contRT.anchorMax = new Vector2(1, 0.5f);
        contRT.pivot = new Vector2(1, 0.5f);
        contRT.sizeDelta = new Vector2(180, 42);
        contRT.anchoredPosition = new Vector2(-18, 0);
        Button contBtn = contBtnGO.AddComponent<Button>();
        Image contImg = contBtnGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(contImg, new Color(0.2f, 0.45f, 0.8f, 1f));
        UnityEventTools.AddPersistentListener(contBtn.onClick, ui.OnContinueClicked);
        ui.continueButton = contBtn;

        GameObject contTxtGO = CreateUIElement("Text", contBtnGO.transform);
        StretchFull(contTxtGO.GetComponent<RectTransform>());
        TextMeshProUGUI contTxtTMP = contTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(contTxtTMP);
        contTxtTMP.fontSize = 13f;
        contTxtTMP.fontStyle = FontStyles.Bold;
        contTxtTMP.alignment = TextAlignmentOptions.Center;
        contTxtTMP.color = Color.white;
        contTxtTMP.text = "CONTINUE >>";

        overlayGO.SetActive(false);
        EditorUtility.SetDirty(ui);
        return canvasGO;
    }

    public static GameObject BuildFloorVictoryCanvas()
    {
        GameObject existing = GameObject.Find(VICTORY_CANVAS_NAME);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject canvasGO = CreateRootCanvas(VICTORY_CANVAS_NAME, 135);
        FloorVictoryUI ui = canvasGO.AddComponent<FloorVictoryUI>();
        Canvas c = canvasGO.GetComponent<Canvas>();
        ui.canvas = c;

        // 1. Root Overlay
        GameObject overlayGO = CreateUIElement("VictoryPanel", canvasGO.transform);
        StretchFull(overlayGO.GetComponent<RectTransform>());
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
        ui.victoryPanel = overlayGO;

        // 2. Main Frame
        GameObject frameGO = CreateUIElement("MainFrame", overlayGO.transform);
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.sizeDelta = new Vector2(960, 580);
        Image frameImg = frameGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(frameImg, DungeonUIAssetHelper.PanelStyle.Wood);

        // 3. Header
        GameObject headerGO = CreateUIElement("Header", frameGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -6);
        headerRT.sizeDelta = new Vector2(-16, 72);
        Image headerBg = headerGO.AddComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(headerBg, DungeonUIAssetHelper.PlaqueStyle.Victory);

        GameObject iconGO = CreateUIElement("ChestIcon", headerGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(16, 0);
        iconRT.sizeDelta = new Vector2(50, 50);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = DungeonUIAssetHelper.GetChestSprite();
        iconImg.preserveAspect = true;

        GameObject titleGO = CreateUIElement("TitleText", headerGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(78, -10);
        titleRT.sizeDelta = new Vector2(-96, 28);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(titleTMP);
        titleTMP.fontSize = 19f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.color = new Color(1f, 0.88f, 0.25f);
        ui.titleText = titleTMP;

        GameObject subGO = CreateUIElement("SubtitleText", headerGO.transform);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0, 1);
        subRT.anchoredPosition = new Vector2(78, -40);
        subRT.sizeDelta = new Vector2(-96, 24);
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(subTMP);
        subTMP.fontSize = 12f;
        subTMP.fontStyle = FontStyles.Bold;
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;
        subTMP.color = new Color(0.95f, 0.90f, 0.75f);
        ui.subtitleText = subTMP;

        // 4. Body
        GameObject bodyGO = CreateUIElement("Body", frameGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(16, 68);
        bodyRT.offsetMax = new Vector2(-16, -84);

        // Left Column (Overview & Stats / Blessings)
        GameObject leftColGO = CreateUIElement("LeftColumn_Container", bodyGO.transform);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.485f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = Vector2.zero;
        Image leftColBg = leftColGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftColBg, new Color(0.10f, 0.11f, 0.15f, 0.95f));

        GameObject leftScrollGO = CreateUIElement("LeftScrollView", leftColGO.transform);
        RectTransform leftScrollRT = leftScrollGO.GetComponent<RectTransform>();
        leftScrollRT.anchorMin = Vector2.zero;
        leftScrollRT.anchorMax = Vector2.one;
        leftScrollRT.offsetMin = new Vector2(6, 6);
        leftScrollRT.offsetMax = new Vector2(-6, -6);
        ScrollRect leftSR = leftScrollGO.AddComponent<ScrollRect>();
        leftSR.horizontal = false;
        leftSR.vertical = true;
        leftSR.scrollSensitivity = 25f;
        ui.leftOverviewScrollRect = leftSR;

        GameObject leftVpGO = CreateUIElement("Viewport", leftScrollGO.transform);
        RectTransform leftVpRT = leftVpGO.GetComponent<RectTransform>();
        leftVpRT.anchorMin = Vector2.zero;
        leftVpRT.anchorMax = Vector2.one;
        leftVpRT.offsetMin = Vector2.zero;
        leftVpRT.offsetMax = new Vector2(-14, 0);
        leftVpGO.AddComponent<RectMask2D>();
        leftSR.viewport = leftVpRT;

        GameObject leftContentGO = CreateUIElement("Content", leftVpGO.transform);
        RectTransform leftContentRT = leftContentGO.GetComponent<RectTransform>();
        leftContentRT.anchorMin = new Vector2(0, 1);
        leftContentRT.anchorMax = new Vector2(1, 1);
        leftContentRT.pivot = new Vector2(0.5f, 1);
        leftContentRT.sizeDelta = Vector2.zero;
        VerticalLayoutGroup leftVLG = leftContentGO.AddComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 10;
        leftVLG.padding = new RectOffset(4, 4, 4, 4);
        leftVLG.childControlHeight = true;
        leftVLG.childControlWidth = true;
        leftVLG.childForceExpandHeight = false;
        leftVLG.childForceExpandWidth = true;
        ContentSizeFitter leftCsf = leftContentGO.AddComponent<ContentSizeFitter>();
        leftCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        leftSR.content = leftContentRT;

        DungeonUIAssetHelper.CreateVerticalScrollbar(leftScrollGO.transform, leftSR, new Color(0.85f, 0.65f, 0.2f, 1f));

        // Card Stats
        GameObject statsCardGO = CreateUIElement("Card_Stats", leftContentGO.transform);
        Image statsBg = statsCardGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(statsBg, new Color(0.16f, 0.13f, 0.08f, 0.95f));
        VerticalLayoutGroup statsVLG = statsCardGO.AddComponent<VerticalLayoutGroup>();
        statsVLG.padding = new RectOffset(12, 12, 10, 10);
        statsVLG.spacing = 6;
        statsVLG.childControlHeight = true;
        statsVLG.childControlWidth = true;
        statsVLG.childForceExpandHeight = false;
        statsVLG.childForceExpandWidth = true;
        ContentSizeFitter statsCsf = statsCardGO.AddComponent<ContentSizeFitter>();
        statsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ui.statsSectionGO = statsCardGO;

        GameObject statsTitleGO = CreateUIElement("Title", statsCardGO.transform);
        TextMeshProUGUI statsTitleTMP = statsTitleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(statsTitleTMP);
        statsTitleTMP.fontSize = 13f;
        statsTitleTMP.fontStyle = FontStyles.Bold;
        statsTitleTMP.color = new Color(1f, 0.85f, 0.3f);
        statsTitleTMP.text = "PERMANENT STAT ENHANCEMENT";

        GameObject statsBadgesGO = CreateUIElement("BadgesText", statsCardGO.transform);
        TextMeshProUGUI statsBadgesTMP = statsBadgesGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(statsBadgesTMP);
        statsBadgesTMP.fontSize = 11.5f;
        statsBadgesTMP.color = Color.white;
        ui.statsBadgesText = statsBadgesTMP;

        // Card Blessings
        GameObject blessCardGO = CreateUIElement("Card_Blessings", leftContentGO.transform);
        Image blessBg = blessCardGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(blessBg, new Color(0.11f, 0.11f, 0.19f, 0.95f));
        VerticalLayoutGroup blessVLG = blessCardGO.AddComponent<VerticalLayoutGroup>();
        blessVLG.padding = new RectOffset(12, 12, 10, 10);
        blessVLG.spacing = 6;
        blessVLG.childControlHeight = true;
        blessVLG.childControlWidth = true;
        blessVLG.childForceExpandHeight = false;
        blessVLG.childForceExpandWidth = true;
        ContentSizeFitter blessCsf = blessCardGO.AddComponent<ContentSizeFitter>();
        blessCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ui.blessingSectionGO = blessCardGO;

        GameObject blessTitleGO = CreateUIElement("Title", blessCardGO.transform);
        TextMeshProUGUI blessTitleTMP = blessTitleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(blessTitleTMP);
        blessTitleTMP.fontSize = 13f;
        blessTitleTMP.fontStyle = FontStyles.Bold;
        blessTitleTMP.color = new Color(0.75f, 0.85f, 1f);
        blessTitleTMP.text = "ANCIENT BLESSINGS UNLOCKED";

        GameObject blessDetailsGO = CreateUIElement("DetailsText", blessCardGO.transform);
        TextMeshProUGUI blessDetailsTMP = blessDetailsGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(blessDetailsTMP);
        blessDetailsTMP.fontSize = 11.5f;
        blessDetailsTMP.color = Color.white;
        ui.blessingDetailsText = blessDetailsTMP;

        // Right Column (Floor Clear Rewards)
        GameObject rightColGO = CreateUIElement("RightColumn_Container", bodyGO.transform);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.505f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = Vector2.zero;
        rightColRT.offsetMax = Vector2.zero;
        Image rightColBg = rightColGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightColBg, new Color(0.08f, 0.09f, 0.13f, 0.95f));

        GameObject lootTitleGO = CreateUIElement("Title", rightColGO.transform);
        RectTransform ltRT = lootTitleGO.GetComponent<RectTransform>();
        ltRT.anchorMin = new Vector2(0, 1);
        ltRT.anchorMax = new Vector2(1, 1);
        ltRT.pivot = new Vector2(0.5f, 1);
        ltRT.sizeDelta = new Vector2(-20, 26);
        ltRT.anchoredPosition = new Vector2(0, -6);
        TextMeshProUGUI ltTMP = lootTitleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(ltTMP);
        ltTMP.fontSize = 12.5f;
        ltTMP.fontStyle = FontStyles.Bold;
        ltTMP.color = new Color(1f, 0.85f, 0.3f);
        ltTMP.text = "CHIẾN LỢI PHẨM TẦNG";

        GameObject rightScrollGO = CreateUIElement("ScrollArea", rightColGO.transform);
        RectTransform rightScrollRT = rightScrollGO.GetComponent<RectTransform>();
        rightScrollRT.anchorMin = Vector2.zero;
        rightScrollRT.anchorMax = Vector2.one;
        rightScrollRT.offsetMin = new Vector2(8, 8);
        rightScrollRT.offsetMax = new Vector2(-8, -42);
        ScrollRect rightSR = rightScrollGO.AddComponent<ScrollRect>();
        rightSR.horizontal = false;
        rightSR.vertical = true;
        rightSR.scrollSensitivity = 25f;
        ui.rightRewardScrollRect = rightSR;

        GameObject rightVpGO = CreateUIElement("Viewport", rightScrollGO.transform);
        RectTransform rightVpRT = rightVpGO.GetComponent<RectTransform>();
        rightVpRT.anchorMin = Vector2.zero;
        rightVpRT.anchorMax = Vector2.one;
        rightVpRT.offsetMin = Vector2.zero;
        rightVpRT.offsetMax = new Vector2(-14, 0);
        rightVpGO.AddComponent<RectMask2D>();
        rightSR.viewport = rightVpRT;

        GameObject rightContentGO = CreateUIElement("Content", rightVpGO.transform);
        RectTransform rightContentRT = rightContentGO.GetComponent<RectTransform>();
        rightContentRT.anchorMin = new Vector2(0, 1);
        rightContentRT.anchorMax = new Vector2(1, 1);
        rightContentRT.pivot = new Vector2(0.5f, 1);
        rightContentRT.sizeDelta = Vector2.zero;
        VerticalLayoutGroup rightVLG = rightContentGO.AddComponent<VerticalLayoutGroup>();
        rightVLG.padding = new RectOffset(4, 4, 4, 4);
        rightVLG.spacing = 6;
        rightVLG.childForceExpandWidth = true;
        rightVLG.childForceExpandHeight = false;
        rightVLG.childControlWidth = true;
        rightVLG.childControlHeight = true;
        ContentSizeFitter rightCsf = rightContentGO.AddComponent<ContentSizeFitter>();
        rightCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rightSR.content = rightContentRT;
        ui.rewardItemsContainer = rightContentRT;

        DungeonUIAssetHelper.CreateVerticalScrollbar(rightScrollGO.transform, rightSR, new Color(0.85f, 0.65f, 0.15f, 1f));

        // 5. Footer
        GameObject footerGO = CreateUIElement("Footer", frameGO.transform);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 6);
        footerRT.sizeDelta = new Vector2(-16, 56);
        Image footerBg = footerGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(footerBg, new Color(0.10f, 0.10f, 0.12f, 0.95f));

        GameObject hintGO = CreateUIElement("HintText", footerGO.transform);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0, 1);
        hintRT.pivot = new Vector2(0, 0.5f);
        hintRT.anchoredPosition = new Vector2(16, 0);
        hintRT.sizeDelta = new Vector2(430, 48);
        TextMeshProUGUI hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(hintTMP);
        hintTMP.fontSize = 11.5f;
        hintTMP.alignment = TextAlignmentOptions.MidlineLeft;
        hintTMP.color = new Color(0.85f, 0.92f, 1f);
        ui.statusHintText = hintTMP;

        GameObject btnRowGO = CreateUIElement("ButtonsRow", footerGO.transform);
        RectTransform btnRowRT = btnRowGO.GetComponent<RectTransform>();
        btnRowRT.anchorMin = new Vector2(1, 0.5f);
        btnRowRT.anchorMax = new Vector2(1, 0.5f);
        btnRowRT.pivot = new Vector2(1, 0.5f);
        btnRowRT.anchoredPosition = new Vector2(-16, 0);
        btnRowRT.sizeDelta = new Vector2(520, 42);
        HorizontalLayoutGroup btnHLG = btnRowGO.AddComponent<HorizontalLayoutGroup>();
        btnHLG.enabled = false;

        // Button Claim All
        Button laBtn = CreateFooterButton(btnRowGO.transform, "BtnLootAll", "CLAIM ALL", new Color(0.85f, 0.65f, 0.15f), new Vector2(130, 38));
        UnityEventTools.AddPersistentListener(laBtn.onClick, ui.OnLootAllClicked);
        ui.lootAllButton = laBtn;

        // Button Return
        Button retBtn = CreateFooterButton(btnRowGO.transform, "BtnReturn", "RETURN TO FARM", new Color(0.18f, 0.55f, 0.28f), new Vector2(150, 38));
        UnityEventTools.AddPersistentListener(retBtn.onClick, ui.OnReturnToBaseClicked);
        ui.returnToBaseButton = retBtn;

        // Button Next Floor
        Button nextBtn = CreateFooterButton(btnRowGO.transform, "BtnNextFloor", "NEXT FLOOR [->]", new Color(0.25f, 0.45f, 0.85f), new Vector2(165, 38));
        UnityEventTools.AddPersistentListener(nextBtn.onClick, ui.OnNextFloorClicked);
        ui.nextFloorButton = nextBtn;

        overlayGO.SetActive(false);
        EditorUtility.SetDirty(ui);
        return canvasGO;
    }

    public static GameObject BuildDungeonSummaryCanvas()
    {
        GameObject existing = GameObject.Find(SUMMARY_CANVAS_NAME);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject canvasGO = CreateRootCanvas(SUMMARY_CANVAS_NAME, 140);
        DungeonSummaryUI ui = canvasGO.AddComponent<DungeonSummaryUI>();
        Canvas c = canvasGO.GetComponent<Canvas>();
        ui.canvas = c;

        // 1. Overlay
        GameObject overlayGO = CreateUIElement("DungeonSummaryPanel", canvasGO.transform);
        StretchFull(overlayGO.GetComponent<RectTransform>());
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.06f, 0.94f);
        ui.summaryPanel = overlayGO;

        // 2. Modal Frame
        GameObject modalGO = CreateUIElement("SummaryModal", overlayGO.transform);
        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.sizeDelta = new Vector2(900, 580);
        Image modalImg = modalGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(modalImg, DungeonUIAssetHelper.PanelStyle.Wood);

        // 3. Header
        GameObject headerGO = CreateUIElement("Header", modalGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -6);
        headerRT.sizeDelta = new Vector2(-12, 74);
        Image headerImg = headerGO.AddComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(headerImg, DungeonUIAssetHelper.PlaqueStyle.Gold);
        ui.headerBannerImg = headerImg;

        GameObject iconGO = CreateUIElement("HeaderIcon", headerGO.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(24, 0);
        iconRT.sizeDelta = new Vector2(50, 50);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        ui.headerIcon = iconImg;

        GameObject titleGO = CreateUIElement("TitleText", headerGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(86, 24);
        titleRT.offsetMax = new Vector2(-20, -6);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(titleTMP);
        titleTMP.fontSize = 20f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.text = "DUNGEON EXPEDITION SUMMARY";
        ui.titleText = titleTMP;

        GameObject subGO = CreateUIElement("SubtitleText", headerGO.transform);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = Vector2.one;
        subRT.offsetMin = new Vector2(86, 6);
        subRT.offsetMax = new Vector2(-20, -36);
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(subTMP);
        subTMP.fontSize = 12f;
        subTMP.color = new Color(0.85f, 0.88f, 0.95f);
        subTMP.alignment = TextAlignmentOptions.MidlineLeft;
        subTMP.text = "Overview of progress, stages cleared, and all secured expedition loot.";
        ui.subtitleText = subTMP;

        // 4. Body
        GameObject bodyGO = CreateUIElement("Body", modalGO.transform);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(12, 64);
        bodyRT.offsetMax = new Vector2(-12, -88);

        // Left Column (Stats)
        GameObject leftColGO = CreateUIElement("LeftColumn_Stats", bodyGO.transform);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.42f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = new Vector2(-6, 0);
        Image leftColBg = leftColGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(leftColBg, DungeonUIAssetHelper.PanelStyle.Slate);

        GameObject leftHeaderGO = CreateUIElement("LeftHeader", leftColGO.transform);
        RectTransform lhRT = leftHeaderGO.GetComponent<RectTransform>();
        lhRT.anchorMin = new Vector2(0, 1);
        lhRT.anchorMax = new Vector2(1, 1);
        lhRT.pivot = new Vector2(0.5f, 1);
        lhRT.sizeDelta = new Vector2(-12, 30);
        lhRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StylePlaque(leftHeaderGO.AddComponent<Image>(), DungeonUIAssetHelper.PlaqueStyle.Wood);

        GameObject lhTxtGO = CreateUIElement("Text", leftHeaderGO.transform);
        StretchFull(lhTxtGO.GetComponent<RectTransform>());
        RectTransform lhTxtRT = lhTxtGO.GetComponent<RectTransform>();
        lhTxtRT.offsetMin = new Vector2(8, 0);
        lhTxtRT.offsetMax = new Vector2(-8, 0);
        TextMeshProUGUI lhTmp = lhTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(lhTmp);
        lhTmp.fontSize = 12.5f;
        lhTmp.fontStyle = FontStyles.Bold;
        lhTmp.color = new Color(1f, 0.85f, 0.35f);
        lhTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lhTmp.text = "EXPEDITION STATS";

        GameObject statsContentGO = CreateUIElement("StatsList", leftColGO.transform);
        RectTransform scRT = statsContentGO.GetComponent<RectTransform>();
        scRT.anchorMin = Vector2.zero;
        scRT.anchorMax = Vector2.one;
        scRT.offsetMin = new Vector2(10, 10);
        scRT.offsetMax = new Vector2(-10, -42);
        VerticalLayoutGroup scVLG = statsContentGO.AddComponent<VerticalLayoutGroup>();
        scVLG.spacing = 8;
        scVLG.childControlWidth = true;
        scVLG.childControlHeight = false;
        scVLG.childForceExpandWidth = true;
        scVLG.childForceExpandHeight = false;

        ui.statFloorText = CreateSummaryStatLine(statsContentGO.transform, "StatFloor", "[Floor] 1");
        ui.statStagesText = CreateSummaryStatLine(statsContentGO.transform, "StatStages", "[Stages] 0/5");
        ui.statBossText = CreateSummaryStatLine(statsContentGO.transform, "StatBoss", "[Boss] Unmet");
        ui.statHealthText = CreateSummaryStatLine(statsContentGO.transform, "StatHealth", "[Vitality] 100/100 HP");
        ui.statBlessingText = CreateSummaryStatLine(statsContentGO.transform, "StatBlessing", "[Blessing] None");
        ui.statDayTransitionText = CreateSummaryStatLine(statsContentGO.transform, "StatDay", "[Time] New Day (+1 Day)");

        GameObject noticeBoxGO = CreateUIElement("NoticeBox", statsContentGO.transform);
        RectTransform nbRT = noticeBoxGO.GetComponent<RectTransform>();
        nbRT.sizeDelta = new Vector2(0, 110);
        Image nbImg = noticeBoxGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(nbImg, DungeonUIAssetHelper.PanelStyle.Wood);

        GameObject noticeTxtGO = CreateUIElement("NoticeText", noticeBoxGO.transform);
        StretchFull(noticeTxtGO.GetComponent<RectTransform>());
        RectTransform ntRT = noticeTxtGO.GetComponent<RectTransform>();
        ntRT.offsetMin = new Vector2(8, 6);
        ntRT.offsetMax = new Vector2(-8, -6);
        TextMeshProUGUI ntTMP = noticeTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(ntTMP);
        ntTMP.fontSize = 11.5f;
        ntTMP.color = Color.white;
        ntTMP.alignment = TextAlignmentOptions.TopLeft;
        ui.specialNoticeText = ntTMP;

        // Right Column (Secured Loot)
        GameObject rightColGO = CreateUIElement("RightColumn_Loot", bodyGO.transform);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.42f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = new Vector2(6, 0);
        rightColRT.offsetMax = Vector2.zero;
        Image rightColBg = rightColGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(rightColBg, DungeonUIAssetHelper.PanelStyle.Slate);

        GameObject rightHeaderGO = CreateUIElement("RightHeader", rightColGO.transform);
        RectTransform rhRT = rightHeaderGO.GetComponent<RectTransform>();
        rhRT.anchorMin = new Vector2(0, 1);
        rhRT.anchorMax = new Vector2(1, 1);
        rhRT.pivot = new Vector2(0.5f, 1);
        rhRT.sizeDelta = new Vector2(-12, 30);
        rhRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StyleSlicedFrame(rightHeaderGO.AddComponent<Image>(), new Color(0.18f, 0.22f, 0.30f, 1f));

        GameObject rhTxtGO = CreateUIElement("Text", rightHeaderGO.transform);
        StretchFull(rhTxtGO.GetComponent<RectTransform>());
        RectTransform rhTxtRT = rhTxtGO.GetComponent<RectTransform>();
        rhTxtRT.offsetMin = new Vector2(8, 0);
        rhTxtRT.offsetMax = new Vector2(-8, 0);
        TextMeshProUGUI rhTMP = rhTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(rhTMP);
        rhTMP.fontSize = 12.5f;
        rhTMP.fontStyle = FontStyles.Bold;
        rhTMP.color = new Color(0.4f, 0.85f, 1f);
        rhTMP.alignment = TextAlignmentOptions.MidlineLeft;
        rhTMP.text = "SAFELY SECURED LOOT";
        ui.lootHeaderTitle = rhTMP;

        GameObject scrollGO = CreateUIElement("LootScrollArea", rightColGO.transform);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(8, 8);
        scrollRT.offsetMax = new Vector2(-8, -42);
        ScrollRect itemsSR = scrollGO.AddComponent<ScrollRect>();
        itemsSR.horizontal = false;
        itemsSR.vertical = true;
        itemsSR.scrollSensitivity = 25f;
        itemsSR.movementType = ScrollRect.MovementType.Clamped;
        ui.itemsScrollRect = itemsSR;

        GameObject vpGO = CreateUIElement("Viewport", scrollGO.transform);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-16, 0);
        vpGO.AddComponent<RectMask2D>();
        itemsSR.viewport = vpRT;

        GameObject contentGO = CreateUIElement("Content", vpGO.transform);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = Vector2.zero;
        GridLayoutGroup glg = contentGO.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(230, 52);
        glg.spacing = new Vector2(8, 6);
        glg.padding = new RectOffset(6, 6, 6, 6);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        itemsSR.content = contentRT;
        ui.itemsGridContainer = contentRT;

        DungeonUIAssetHelper.CreateVerticalScrollbar(scrollGO.transform, itemsSR, new Color(0.25f, 0.75f, 0.85f, 1f));

        // 5. Footer
        GameObject footerGO = CreateUIElement("Footer", modalGO.transform);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 12);
        footerRT.sizeDelta = new Vector2(-24, 46);

        GameObject btnGO = CreateUIElement("BtnReturnToBase", footerGO.transform);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.pivot = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
        btnRT.sizeDelta = new Vector2(260, 44);
        Button btn = btnGO.AddComponent<Button>();
        Image btnImg = btnGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleTactileButton(btn, btnImg, DungeonUIAssetHelper.ButtonStyle.Victory);
        UnityEventTools.AddPersistentListener(btn.onClick, ui.OnReturnClicked);
        ui.returnToBaseButton = btn;

        GameObject btnTxtGO = CreateUIElement("Text", btnGO.transform);
        StretchFull(btnTxtGO.GetComponent<RectTransform>());
        TextMeshProUGUI btnTxtTMP = btnTxtGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(btnTxtTMP);
        btnTxtTMP.fontSize = 14f;
        btnTxtTMP.fontStyle = FontStyles.Bold;
        btnTxtTMP.alignment = TextAlignmentOptions.Center;
        btnTxtTMP.text = "RETURN TO FARM  [ -> ]";
        btnTxtTMP.color = Color.white;
        ui.returnButtonText = btnTxtTMP;

        overlayGO.SetActive(false);
        EditorUtility.SetDirty(ui);
        return canvasGO;
    }

    private static GameObject CreateRootCanvas(string name, int sortingOrder)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;

        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        return go;
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        EnsureFontLoaded();
        if (font != null) tmp.font = font;
    }

    private static Button CreateFooterButton(Transform parent, string name, string label, Color color, Vector2 size)
    {
        GameObject btnGO = CreateUIElement(name, parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        LayoutElement le = btnGO.AddComponent<LayoutElement>();
        le.minWidth = size.x;
        le.preferredWidth = size.x;
        le.minHeight = size.y;
        le.preferredHeight = size.y;
        Button btn = btnGO.AddComponent<Button>();
        Image img = btnGO.AddComponent<Image>();
        DungeonUIAssetHelper.StyleButton(btn, img, color);

        GameObject textGO = CreateUIElement("Text", btnGO.transform);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.text = label;
        tmp.fontSize = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return btn;
    }

    private static TextMeshProUGUI CreateSummaryStatLine(Transform parent, string name, string defaultText)
    {
        GameObject go = CreateUIElement(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 32);
        Image bg = go.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.10f, 0.13f, 0.19f, 0.95f));

        GameObject textGO = CreateUIElement("Text", go.transform);
        StretchFull(textGO.GetComponent<RectTransform>());
        RectTransform txtRT = textGO.GetComponent<RectTransform>();
        txtRT.offsetMin = new Vector2(8, 0);
        txtRT.offsetMax = new Vector2(-8, 0);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp);
        tmp.fontSize = 11.5f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text = defaultText;
        return tmp;
    }
}
