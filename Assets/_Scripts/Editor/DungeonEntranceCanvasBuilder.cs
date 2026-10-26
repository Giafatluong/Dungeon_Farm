using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that constructs and saves the complete, styled Dungeon Entrance Canvas
/// directly inside Base.unity as a scene-authored Canvas.
/// Matches 100% the layout, sizes, colors, fonts, hierarchy, and visuals of DungeonEntranceUI.
/// All button events are wired as persistent listeners.
/// </summary>
public static class DungeonEntranceCanvasBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Base.unity";
    private const string CANVAS_NAME = "DungeonEntranceCanvas";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build Dungeon Entrance Canvas")]
    public static void BuildDungeonEntranceCanvasMenu()
    {
        BuildDungeonEntranceCanvas();
    }

    public static GameObject BuildDungeonEntranceCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Clean up any legacy or duplicate DungeonEntranceUI objects in the scene
        DungeonEntranceUI[] existingUIs = Object.FindObjectsByType<DungeonEntranceUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existingUIs.Length; i++)
        {
            if (existingUIs[i] != null)
            {
                Undo.DestroyObjectImmediate(existingUIs[i].gameObject);
            }
        }

        // Also clean up any orphan DungeonEnterUI object
        GameObject orphan = GameObject.Find("DungeonEnterUI");
        if (orphan != null)
        {
            Undo.DestroyObjectImmediate(orphan);
        }

        // Load font
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) font = DungeonUIAssetHelper.GetFontAsset();

        // 1. Root Canvas
        GameObject canvasGO = new GameObject(CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Dungeon Entrance Canvas");
        if (canvasGO.scene != scene)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasGO, scene);
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25; // Sits above HUD (0) and Kitchen (20), below MainMenu (100)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        DungeonEntranceUI ui = canvasGO.AddComponent<DungeonEntranceUI>();

        // 2. Fullscreen Dark Overlay (EnterDungeonPanel)
        GameObject enterDungeonPanelGO = new GameObject("EnterDungeonPanel", typeof(RectTransform), typeof(Image));
        enterDungeonPanelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform overlayRT = enterDungeonPanelGO.GetComponent<RectTransform>();
        StretchFull(overlayRT);

        Image overlayImg = enterDungeonPanelGO.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.65f);
        overlayImg.raycastTarget = true;

        // 3. Central Modal Window (FloorSelectionModal - 560 x 560)
        GameObject modalGO = new GameObject("FloorSelectionModal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(enterDungeonPanelGO.transform, false);
        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(560, 560);
        modalRT.anchoredPosition = Vector2.zero;

        Image modalBg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalBg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // 4. Header Title
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(modalGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -16);
        titleRT.sizeDelta = new Vector2(500, 26);

        TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "SELECT DUNGEON FLOOR";
        titleText.color = new Color(1f, 0.85f, 0.35f);

        // 5. Subtitle
        GameObject subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(modalGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 1);
        subRT.anchorMax = new Vector2(0.5f, 1);
        subRT.pivot = new Vector2(0.5f, 1);
        subRT.anchoredPosition = new Vector2(0, -42);
        subRT.sizeDelta = new Vector2(500, 18);

        TextMeshProUGUI subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(subtitleText);
        subtitleText.fontSize = 11.5f;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "Defeat the previous floor Boss to unlock the next floor";
        subtitleText.color = new Color(0.70f, 0.75f, 0.85f);

        // 6. Decorative Divider
        GameObject divGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divGO.transform.SetParent(modalGO.transform, false);
        RectTransform divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 1);
        divRT.anchorMax = new Vector2(0.5f, 1);
        divRT.pivot = new Vector2(0.5f, 1);
        divRT.anchoredPosition = new Vector2(0, -64);
        divRT.sizeDelta = new Vector2(500, 2);
        divGO.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.25f, 0.45f);

        // 7. Scroll View for Tower Floors
        GameObject scrollGO = new GameObject("FloorScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(modalGO.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(24, 78);
        scrollRT.offsetMax = new Vector2(-24, -70);

        ScrollRect sRect = scrollGO.GetComponent<ScrollRect>();
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.scrollSensitivity = 28f;
        sRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport with RectMask2D
        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-16, 0);

        // Content Container
        GameObject contentGO = new GameObject("TowerFloorsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Vertical Scrollbar
        GameObject sbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(scrollGO.transform, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(12, 0);

        Image sbBg = sbGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(sbBg, new Color(0.10f, 0.12f, 0.18f, 0.95f));

        Scrollbar sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject saGO = new GameObject("SlidingArea", typeof(RectTransform));
        saGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = saGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.offsetMin = new Vector2(0, 4);
        saRT.offsetMax = new Vector2(0, -4);

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(saGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(handleImg, new Color(1f, 0.78f, 0.25f, 1f));

        sb.handleRect = handleRT;
        sb.targetGraphic = handleImg;

        sRect.viewport = vpRT;
        sRect.content = contentRT;
        sRect.verticalScrollbar = sb;

        // Pre-create cards for floors 5 down to 1
        CreateFloorCard(contentGO.transform, 5, "FLOOR 5", "Lord of Darkness", ui);
        CreateFloorCard(contentGO.transform, 4, "FLOOR 4", "Abyssal Fire Dragon", ui);
        CreateFloorCard(contentGO.transform, 3, "FLOOR 3: CITADEL OF DEMONS", "Dark Flying Rider", ui);
        CreateFloorCard(contentGO.transform, 2, "FLOOR 2: MONSTER SWAMPLANDS", "King Slime", ui);
        CreateFloorCard(contentGO.transform, 1, "FLOOR 1: CATACOMBS OF THE UNDEAD", "Skeleton King", ui);

        // 8. Hint / Warning Text
        GameObject hintGO = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(modalGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.5f, 0);
        hintRT.anchorMax = new Vector2(0.5f, 0);
        hintRT.pivot = new Vector2(0.5f, 0);
        hintRT.anchoredPosition = new Vector2(0, 56);
        hintRT.sizeDelta = new Vector2(500, 20);

        TextMeshProUGUI detailsText = hintGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(detailsText);
        detailsText.fontSize = 11.5f;
        detailsText.alignment = TextAlignmentOptions.Center;
        detailsText.textWrappingMode = TextWrappingModes.Normal;
        detailsText.text = "";

        // 9. Footer Action Buttons
        GameObject footerGO = new GameObject("FooterButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footerGO.transform.SetParent(modalGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0.5f, 0);
        footerRT.anchorMax = new Vector2(0.5f, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 12);
        footerRT.sizeDelta = new Vector2(500, 42);

        HorizontalLayoutGroup hlg = footerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Enter Dungeon Button
        GameObject enterBtnGO = new GameObject("BtnEnterDungeon", typeof(RectTransform), typeof(Image), typeof(Button));
        enterBtnGO.transform.SetParent(footerGO.transform, false);
        Image enterImg = enterBtnGO.GetComponent<Image>();
        Button enterButton = enterBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(enterButton, enterImg, new Color(0.18f, 0.50f, 0.22f, 1f), new Color(0.24f, 0.65f, 0.30f, 1f));
        UnityEventTools.AddPersistentListener(enterButton.onClick, ui.EnterSelectedFloor);

        GameObject enterTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        enterTextGO.transform.SetParent(enterBtnGO.transform, false);
        StretchFull(enterTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI enterButtonText = enterTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(enterButtonText);
        enterButtonText.fontSize = 13.5f;
        enterButtonText.fontStyle = FontStyles.Bold;
        enterButtonText.alignment = TextAlignmentOptions.Center;
        enterButtonText.text = "ENTER FLOOR 1 (ENTER)";
        enterButtonText.color = Color.white;

        // Cancel / Back Button
        GameObject closeBtnGO = new GameObject("BtnCancel", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(footerGO.transform, false);
        Image closeImg = closeBtnGO.GetComponent<Image>();
        Button closeButton = closeBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(closeButton, closeImg, new Color(0.24f, 0.28f, 0.35f, 1f), new Color(0.32f, 0.38f, 0.48f, 1f));
        UnityEventTools.AddPersistentListener(closeButton.onClick, ui.Close);

        GameObject closeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        StretchFull(closeTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI closeText = closeTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(closeText);
        closeText.fontSize = 13;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.text = "BACK";
        closeText.color = new Color(0.9f, 0.9f, 0.9f);

        // 10. Top-Right Close 'X' Button
        GameObject xBtnGO = new GameObject("BtnCloseX", typeof(RectTransform), typeof(Image), typeof(Button));
        xBtnGO.transform.SetParent(modalGO.transform, false);
        RectTransform xRT = xBtnGO.GetComponent<RectTransform>();
        xRT.anchorMin = new Vector2(1, 1);
        xRT.anchorMax = new Vector2(1, 1);
        xRT.pivot = new Vector2(1, 1);
        xRT.anchoredPosition = new Vector2(-12, -12);
        xRT.sizeDelta = new Vector2(28, 28);
        Image xImg = xBtnGO.GetComponent<Image>();
        Button xBtn = xBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(xBtn, xImg, new Color(0.25f, 0.28f, 0.35f, 0.9f), new Color(0.85f, 0.25f, 0.25f, 1f));
        UnityEventTools.AddPersistentListener(xBtn.onClick, ui.Close);

        GameObject xTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.transform.SetParent(xBtnGO.transform, false);
        StretchFull(xTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI xt = xTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(xt);
        xt.fontSize = 14;
        xt.fontStyle = FontStyles.Bold;
        xt.alignment = TextAlignmentOptions.Center;
        xt.text = "X";
        xt.color = Color.white;

        // 11. Deactivate EnterDungeonPanel by default
        enterDungeonPanelGO.SetActive(false);

        // 12. Bind Serialized Properties on DungeonEntranceUI
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("enterDungeonPanel").objectReferenceValue = enterDungeonPanelGO;
        so.FindProperty("dialogPanel").objectReferenceValue = modalGO;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.FindProperty("subtitleText").objectReferenceValue = subtitleText;
        so.FindProperty("detailsText").objectReferenceValue = detailsText;
        so.FindProperty("enterButton").objectReferenceValue = enterButton;
        so.FindProperty("enterButtonText").objectReferenceValue = enterButtonText;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.FindProperty("totalFloors").intValue = 5;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 13. Auto-connect DungeonEntrance in the scene if present
        DungeonEntrance entranceStation = Object.FindFirstObjectByType<DungeonEntrance>(FindObjectsInactive.Include);
        if (entranceStation != null)
        {
            SerializedObject soEntrance = new SerializedObject(entranceStation);
            soEntrance.FindProperty("dungeonEntranceUI").objectReferenceValue = ui;
            soEntrance.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvasGO;
        Debug.Log("[DungeonEntranceCanvasBuilder] DungeonEntranceCanvas built and saved into Base.unity successfully!");
        return canvasGO;
    }

    private static void CreateFloorCard(Transform parent, int floorNum, string floorName, string bossName, DungeonEntranceUI ui)
    {
        GameObject cardGO = new GameObject($"FloorCard_{floorNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardGO.transform.SetParent(parent, false);

        LayoutElement le = cardGO.GetComponent<LayoutElement>();
        le.preferredHeight = 58;
        le.minHeight = 58;

        Image cardBg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(cardBg, new Color(0.12f, 0.16f, 0.23f, 0.95f));

        Button cardBtn = cardGO.GetComponent<Button>();
        cardBtn.targetGraphic = cardBg;
        ColorBlock cb = cardBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.selectedColor = Color.white;
        cardBtn.colors = cb;

        UnityEventTools.AddIntPersistentListener(cardBtn.onClick, ui.SelectFloor, floorNum);

        // 1. Left Badge
        GameObject badgeGO = new GameObject("BadgeBox", typeof(RectTransform), typeof(Image));
        badgeGO.transform.SetParent(cardGO.transform, false);
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0, 0.5f);
        bRT.anchorMax = new Vector2(0, 0.5f);
        bRT.pivot = new Vector2(0, 0.5f);
        bRT.anchoredPosition = new Vector2(8, 0);
        bRT.sizeDelta = new Vector2(46, 42);
        Image badgeBg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(badgeBg, new Color(0.18f, 0.22f, 0.32f, 1f));

        GameObject bTextGO = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bTextGO.transform.SetParent(badgeGO.transform, false);
        StretchFull(bTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI bText = bTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(bText);
        bText.fontSize = 13.5f;
        bText.fontStyle = FontStyles.Bold;
        bText.alignment = TextAlignmentOptions.Center;
        bText.text = $"T{floorNum}";
        bText.color = new Color(0.7f, 0.85f, 1f);

        // 2. Middle Content Box
        GameObject contentGO = new GameObject("ContentBox", typeof(RectTransform));
        contentGO.transform.SetParent(cardGO.transform, false);
        RectTransform cRT = contentGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0, 0.5f);
        cRT.offsetMin = new Vector2(62, 3);
        cRT.offsetMax = new Vector2(-130, -3);

        // Floor Name
        GameObject nameGO = new GameObject("FloorName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(contentGO.transform, false);
        RectTransform nRT = nameGO.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0.5f);
        nRT.anchorMax = new Vector2(1, 1);
        nRT.pivot = new Vector2(0, 0.5f);
        nRT.anchoredPosition = new Vector2(0, 2);
        nRT.sizeDelta = new Vector2(0, 24);
        TextMeshProUGUI nameText = nameGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(nameText);
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.text = floorName;
        nameText.color = Color.white;

        // Boss Info
        GameObject bossGO = new GameObject("BossInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        bossGO.transform.SetParent(contentGO.transform, false);
        RectTransform bossRT = bossGO.GetComponent<RectTransform>();
        bossRT.anchorMin = new Vector2(0, 0);
        bossRT.anchorMax = new Vector2(1, 0.5f);
        bossRT.pivot = new Vector2(0, 0.5f);
        bossRT.anchoredPosition = new Vector2(0, -2);
        bossRT.sizeDelta = new Vector2(0, 18);
        TextMeshProUGUI bossText = bossGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(bossText);
        bossText.fontSize = 10.5f;
        bossText.alignment = TextAlignmentOptions.Left;
        bossText.text = $"Boss: {bossName}";
        bossText.color = new Color(0.6f, 0.72f, 0.82f);

        // 3. Right Status Badge
        GameObject statusBoxGO = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        statusBoxGO.transform.SetParent(cardGO.transform, false);
        RectTransform sRT = statusBoxGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(1, 0.5f);
        sRT.anchorMax = new Vector2(1, 0.5f);
        sRT.pivot = new Vector2(1, 0.5f);
        sRT.anchoredPosition = new Vector2(-10, 0);
        sRT.sizeDelta = new Vector2(116, 32);

        Image statusBg = statusBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(statusBg, new Color(0.15f, 0.25f, 0.4f, 0.85f));

        GameObject sTextGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        sTextGO.transform.SetParent(statusBoxGO.transform, false);
        StretchFull(sTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI sText = sTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(sText);
        sText.fontSize = 11;
        sText.fontStyle = FontStyles.Bold;
        sText.alignment = TextAlignmentOptions.Center;
        sText.text = "READY";
        sText.color = Color.white;
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
