using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool that builds the Main Menu as a real Canvas directly inside Base.unity.
/// The hierarchy, sizes, sprites, colors, fonts and texts are a 1:1 copy of the
/// procedural UI previously generated at runtime by MainMenuUI.EnsureUIConstructed().
/// All button events are wired as persistent listeners (visible in the Inspector).
/// </summary>
public static class MainMenuCanvasBuilder
{
    private const string SCENE_PATH = "Assets/_Scenes/Base.unity";
    private const string CANVAS_NAME = "MainMenuCanvas";
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static TMP_FontAsset font;

    [MenuItem("Tools/Build Main Menu Canvas")]
    public static void BuildMainMenuCanvasMenu()
    {
        BuildMainMenuCanvas();
    }

    public static GameObject BuildMainMenuCanvas()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        // Remove previous Main Menu objects to avoid duplicates
        MainMenuUI[] existing = Object.FindObjectsByType<MainMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null) Undo.DestroyObjectImmediate(existing[i].gameObject);
        }

        // Assets (same lookup as runtime DungeonUIAssetHelper)
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null) font = DungeonUIAssetHelper.GetFontAsset();
        Sprite frameSprite = DungeonUIAssetHelper.GetFrameSprite();
        Sprite toolbarSprite = DungeonUIAssetHelper.GetToolbarSprite();

        // ===== Canvas =====
        GameObject canvasGO = new GameObject(CANVAS_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Main Menu Canvas");
        SceneManagerMoveToScene(canvasGO, scene);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above HUD (0) and Kitchen (20)
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
        CopyScalerFromHUD(canvasGO.GetComponent<CanvasScaler>(), canvas);

        MainMenuUI menu = canvasGO.AddComponent<MainMenuUI>();

        // ===== 1. Root Overlay =====
        GameObject menuRootGO = new GameObject("MainMenu_Root", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        menuRootGO.transform.SetParent(canvasGO.transform, false);
        CanvasGroup menuCanvasGroup = menuRootGO.GetComponent<CanvasGroup>();

        RectTransform rootRT = menuRootGO.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        Image bgImg = menuRootGO.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.20f);

        // ===== 2. Center Modal Frame =====
        GameObject centerModalGO = new GameObject("CenterModal", typeof(RectTransform), typeof(Image));
        centerModalGO.transform.SetParent(menuRootGO.transform, false);
        RectTransform cmRT = centerModalGO.GetComponent<RectTransform>();
        cmRT.anchorMin = new Vector2(0.5f, 0.5f);
        cmRT.anchorMax = new Vector2(0.5f, 0.5f);
        cmRT.pivot = new Vector2(0.5f, 0.5f);
        cmRT.sizeDelta = new Vector2(580, 640);

        Image cmImg = centerModalGO.GetComponent<Image>();
        if (frameSprite != null)
        {
            cmImg.sprite = frameSprite;
            cmImg.type = Image.Type.Sliced;
            cmImg.color = Color.white;
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedFrame(cmImg, Color.white);
        }

        // ===== 3. Title Banner =====
        GameObject titlePlaqueGO = new GameObject("TitlePlaque", typeof(RectTransform), typeof(Image));
        titlePlaqueGO.transform.SetParent(centerModalGO.transform, false);
        RectTransform tpRT = titlePlaqueGO.GetComponent<RectTransform>();
        tpRT.anchorMin = new Vector2(0.5f, 1f);
        tpRT.anchorMax = new Vector2(0.5f, 1f);
        tpRT.pivot = new Vector2(0.5f, 1f);
        tpRT.sizeDelta = new Vector2(512, 84);
        tpRT.anchoredPosition = new Vector2(0, -18);

        Image tpImg = titlePlaqueGO.GetComponent<Image>();
        if (toolbarSprite != null)
        {
            tpImg.sprite = toolbarSprite;
            tpImg.type = Image.Type.Simple;
            tpImg.preserveAspect = true;
            tpImg.color = Color.white;
        }
        else
        {
            DungeonUIAssetHelper.StylePlaque(tpImg, DungeonUIAssetHelper.PlaqueStyle.Gold);
        }

        GameObject titleTextGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleTextGO.transform.SetParent(titlePlaqueGO.transform, false);
        RectTransform ttRT = titleTextGO.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = new Vector2(10, 20);
        ttRT.offsetMax = new Vector2(-10, -4);
        TextMeshProUGUI tt = titleTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(tt);
        tt.fontSize = 32;
        tt.fontStyle = FontStyles.Bold;
        tt.alignment = TextAlignmentOptions.Center;
        tt.color = new Color(1f, 0.88f, 0.40f);
        tt.text = "DUNGEON FARM";

        GameObject subTextGO = new GameObject("SubTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subTextGO.transform.SetParent(titlePlaqueGO.transform, false);
        RectTransform stRT = subTextGO.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero;
        stRT.anchorMax = Vector2.one;
        stRT.offsetMin = new Vector2(10, 4);
        stRT.offsetMax = new Vector2(-10, -44);
        TextMeshProUGUI st = subTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(st);
        st.fontSize = 12.5f;
        st.alignment = TextAlignmentOptions.Center;
        st.color = new Color(0.85f, 0.95f, 0.90f);
        st.text = "Farming By Day - Dungeon Crawling By Night";

        // ===== 4. Save Status Box =====
        GameObject saveInfoGO = new GameObject("SaveInfoBox", typeof(RectTransform), typeof(Image));
        saveInfoGO.transform.SetParent(centerModalGO.transform, false);
        RectTransform siRT = saveInfoGO.GetComponent<RectTransform>();
        siRT.anchorMin = new Vector2(0.5f, 1f);
        siRT.anchorMax = new Vector2(0.5f, 1f);
        siRT.pivot = new Vector2(0.5f, 1f);
        siRT.sizeDelta = new Vector2(500, 44);
        siRT.anchoredPosition = new Vector2(0, -112);

        Image siImg = saveInfoGO.GetComponent<Image>();
        if (frameSprite != null)
        {
            siImg.sprite = frameSprite;
            siImg.type = Image.Type.Sliced;
            siImg.color = new Color(0.75f, 0.75f, 0.80f, 0.95f);
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedPanel(siImg, DungeonUIAssetHelper.PanelStyle.Slate);
        }

        GameObject siTextGO = new GameObject("SaveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        siTextGO.transform.SetParent(saveInfoGO.transform, false);
        RectTransform sitRT = siTextGO.GetComponent<RectTransform>();
        sitRT.anchorMin = Vector2.zero;
        sitRT.anchorMax = Vector2.one;
        sitRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI saveInfoText = siTextGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(saveInfoText);
        saveInfoText.fontSize = 13.5f;
        saveInfoText.alignment = TextAlignmentOptions.Center;
        saveInfoText.color = new Color(0.95f, 0.95f, 1f);
        saveInfoText.text = "Current Progress: <color=#FFE066><b>Day 1</b></color>  |  Dungeon: <color=#80CBC4><b>Floor 1</b></color>";

        // ===== 5. Content Area & Sub-Panels =====
        GameObject contentAreaGO = new GameObject("ContentArea", typeof(RectTransform));
        contentAreaGO.transform.SetParent(centerModalGO.transform, false);
        RectTransform caRT = contentAreaGO.GetComponent<RectTransform>();
        caRT.anchorMin = new Vector2(0.5f, 0);
        caRT.anchorMax = new Vector2(0.5f, 1);
        caRT.pivot = new Vector2(0.5f, 0.5f);
        caRT.sizeDelta = new Vector2(500, 0);
        caRT.offsetMin = new Vector2(-250, 24);
        caRT.offsetMax = new Vector2(250, -170);

        // --- Main Buttons Panel ---
        GameObject mainButtonsPanel = new GameObject("MainButtonsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        mainButtonsPanel.transform.SetParent(caRT, false);
        StretchFull(mainButtonsPanel.GetComponent<RectTransform>());
        VerticalLayoutGroup mbVlg = mainButtonsPanel.GetComponent<VerticalLayoutGroup>();
        mbVlg.spacing = 12;
        mbVlg.childAlignment = TextAnchor.MiddleCenter;
        mbVlg.childControlWidth = true;
        mbVlg.childControlHeight = false;

        CreateFigmaButton(mainButtonsPanel.transform, frameSprite, "CONTINUE", new Color(1f, 0.95f, 0.70f), menu.OnContinueClicked);
        CreateFigmaButton(mainButtonsPanel.transform, frameSprite, "NEW GAME", new Color(0.70f, 0.90f, 1f), menu.OnNewGameClicked);
        CreateFigmaButton(mainButtonsPanel.transform, frameSprite, "HOW TO PLAY & CONTROLS", new Color(0.80f, 1f, 0.80f), menu.OnHowToPlayClicked);
        CreateFigmaButton(mainButtonsPanel.transform, frameSprite, "SETTINGS", new Color(0.90f, 0.90f, 1f), menu.OnSettingsClicked);
        CreateFigmaButton(mainButtonsPanel.transform, frameSprite, "QUIT GAME", new Color(1f, 0.70f, 0.70f), menu.OnQuitClicked);

        // --- How To Play Panel ---
        GameObject howToPlayPanel = CreateSubPanel("HowToPlayPanel", caRT, frameSprite, new Color(0.85f, 0.85f, 0.90f, 0.98f), false, new RectOffset(20, 20, 16, 16), 8);
        CreatePanelTitle(howToPlayPanel.transform, "[ HOW TO PLAY & CONTROLS ]", new Color(1f, 0.85f, 0.4f));

        GameObject cGO = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
        cGO.transform.SetParent(howToPlayPanel.transform, false);
        TextMeshProUGUI content = cGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(content);
        content.fontSize = 13.5f;
        content.color = new Color(0.95f, 0.95f, 0.98f);
        content.text =
            "- <b>[W][A][S][D] / Arrow Keys:</b> Move character\n" +
            "- <b>[Left Click]:</b> Plant seeds (when holding seed) / Harvest crops\n" +
            "- <b>[Key E]:</b> Interact with Kitchen Pot, Storage Chest, Statue, Dungeon Portal\n" +
            "- <b>[Key I]:</b> Open / Close Backpack Inventory\n" +
            "- <b>[Sleep]:</b> Approach Bed and press <b>[E]</b> to advance day and crop cycles\n" +
            "- <b>[Key ESC]:</b> Open / Close this Menu at any time in Base";

        CreateFigmaButton(howToPlayPanel.transform, frameSprite, "BACK TO MENU", Color.white, menu.OnBackToMenuClicked, 46);
        howToPlayPanel.SetActive(false);

        // --- Settings Panel ---
        GameObject settingsPanel = CreateSubPanel("SettingsPanel", caRT, frameSprite, new Color(0.85f, 0.85f, 0.90f, 0.98f), false, new RectOffset(20, 20, 20, 20), 14);
        CreatePanelTitle(settingsPanel.transform, "[ SETTINGS ]", new Color(1f, 0.85f, 0.4f));

        GameObject fsBtn = CreateFigmaButton(settingsPanel.transform, frameSprite, "Display Mode", Color.white, menu.OnToggleFullscreen, 48);
        TextMeshProUGUI fullscreenBtnText = fsBtn.GetComponentInChildren<TextMeshProUGUI>();
        fullscreenBtnText.text = "Mode: [ WINDOWED ]";

        GameObject sndBtn = CreateFigmaButton(settingsPanel.transform, frameSprite, "Sound", Color.white, menu.OnToggleSound, 48);
        TextMeshProUGUI soundBtnText = sndBtn.GetComponentInChildren<TextMeshProUGUI>();
        soundBtnText.text = "Sound: [ ON 100% ]";

        CreateFigmaButton(settingsPanel.transform, frameSprite, "BACK TO MENU", Color.white, menu.OnBackToMenuClicked, 46);
        settingsPanel.SetActive(false);

        // --- Confirm New Game Panel ---
        GameObject confirmNewGamePanel = CreateSubPanel("ConfirmNewGamePanel", caRT, frameSprite, new Color(0.95f, 0.80f, 0.80f, 0.98f), true, new RectOffset(20, 20, 20, 20), 14);
        CreatePanelTitle(confirmNewGamePanel.transform, "[ CONFIRM NEW GAME ]", new Color(1f, 0.4f, 0.4f));

        GameObject wGO = new GameObject("WarnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        wGO.transform.SetParent(confirmNewGamePanel.transform, false);
        TextMeshProUGUI warn = wGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(warn);
        warn.fontSize = 14;
        warn.color = Color.white;
        warn.alignment = TextAlignmentOptions.Center;
        warn.text = "Are you sure you want to <b>start over from scratch</b>?\n\nAll farm day progress, crops, stored chest items, and dungeon progress will be wiped and reset to <b>Day 1</b>!";

        CreateFigmaButton(confirmNewGamePanel.transform, frameSprite, "CONFIRM - RESET & START OVER", new Color(1f, 0.6f, 0.6f), menu.OnConfirmNewGameYes, 48);
        CreateFigmaButton(confirmNewGamePanel.transform, frameSprite, "CANCEL - RETURN", Color.white, menu.OnConfirmNewGameNo, 46);
        confirmNewGamePanel.SetActive(false);

        // ===== 6. Floating HUD Menu Button =====
        GameObject hudMenuButtonGO = BuildFloatingHUDButton(canvasGO.transform, frameSprite, menu);

        // ===== Hidden by default (same as runtime Close()) =====
        menuCanvasGroup.alpha = 0f;
        menuCanvasGroup.blocksRaycasts = false;
        menuCanvasGroup.interactable = false;
        menuRootGO.SetActive(false);

        // ===== Bind references on MainMenuUI =====
        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("targetCanvas").objectReferenceValue = canvas;
        so.FindProperty("menuRootGO").objectReferenceValue = menuRootGO;
        so.FindProperty("menuCanvasGroup").objectReferenceValue = menuCanvasGroup;
        so.FindProperty("mainButtonsPanel").objectReferenceValue = mainButtonsPanel;
        so.FindProperty("howToPlayPanel").objectReferenceValue = howToPlayPanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("confirmNewGamePanel").objectReferenceValue = confirmNewGamePanel;
        so.FindProperty("saveInfoText").objectReferenceValue = saveInfoText;
        so.FindProperty("fullscreenBtnText").objectReferenceValue = fullscreenBtnText;
        so.FindProperty("soundBtnText").objectReferenceValue = soundBtnText;
        so.FindProperty("hudMenuButtonGO").objectReferenceValue = hudMenuButtonGO;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvasGO;
        Debug.Log("[MainMenuCanvasBuilder] MainMenuCanvas built and saved into Base.unity.");
        return canvasGO;
    }

    #region Helpers
    private static void SceneManagerMoveToScene(GameObject go, UnityEngine.SceneManagement.Scene scene)
    {
        if (go.scene != scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    }

    /// <summary>Use the same scaling as the HUD canvas the menu used to live in.</summary>
    private static void CopyScalerFromHUD(CanvasScaler scaler, Canvas canvas)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject hudGO = GameObject.Find("Canvas");
        if (hudGO == null) return;
        CanvasScaler hudScaler = hudGO.GetComponent<CanvasScaler>();
        if (hudScaler != null)
        {
            scaler.uiScaleMode = hudScaler.uiScaleMode;
            scaler.referenceResolution = hudScaler.referenceResolution;
            scaler.screenMatchMode = hudScaler.screenMatchMode;
            scaler.matchWidthOrHeight = hudScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = hudScaler.referencePixelsPerUnit;
            scaler.scaleFactor = hudScaler.scaleFactor;
        }
        Canvas hudCanvas = hudGO.GetComponent<Canvas>();
        if (hudCanvas != null) canvas.pixelPerfect = hudCanvas.pixelPerfect;
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp != null && font != null) tmp.font = font;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    private static GameObject CreateSubPanel(string name, Transform parent, Sprite frameSprite, Color frameColor, bool redFallback, RectOffset padding, float spacing)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);
        StretchFull(panel.GetComponent<RectTransform>());

        Image bg = panel.GetComponent<Image>();
        if (frameSprite != null)
        {
            bg.sprite = frameSprite;
            bg.type = Image.Type.Sliced;
            bg.color = frameColor;
        }
        else if (redFallback)
        {
            DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.20f, 0.08f, 0.08f, 0.98f));
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedPanel(bg, DungeonUIAssetHelper.PanelStyle.Slate);
        }

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = padding;
        vlg.spacing = spacing;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        return panel;
    }

    private static void CreatePanelTitle(Transform parent, string text, Color color)
    {
        GameObject tGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        tGO.transform.SetParent(parent, false);
        TextMeshProUGUI title = tGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(title);
        title.fontSize = 19;
        title.fontStyle = FontStyles.Bold;
        title.color = color;
        title.text = text;
        title.alignment = TextAlignmentOptions.Center;
    }

    private static GameObject CreateFigmaButton(Transform parent, Sprite frameSprite, string label, Color textColor, UnityAction action, float height = 50)
    {
        GameObject btnGO = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        Image img = btnGO.GetComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedFrame(img, Color.white);
        }

        Button btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        UnityEventTools.AddPersistentListener(btn.onClick, action);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(btnGO.transform, false);
        StretchFull(txtGO.GetComponent<RectTransform>());

        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(txt);
        txt.fontSize = 16.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = textColor;
        txt.text = label;

        return btnGO;
    }

    private static GameObject BuildFloatingHUDButton(Transform canvasT, Sprite frameSprite, MainMenuUI menu)
    {
        GameObject hudBtnGO = new GameObject("HUD_MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        hudBtnGO.transform.SetParent(canvasT, false);

        // Keep the corner button at HUD layer (it used to live inside the HUD canvas),
        // so other popups (Kitchen etc.) still draw above it.
        Canvas hudLayer = hudBtnGO.AddComponent<Canvas>();
        hudLayer.overrideSorting = true;
        hudLayer.sortingOrder = 1;
        hudLayer.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
        hudBtnGO.AddComponent<GraphicRaycaster>();

        RectTransform rt = hudBtnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(140, 42);
        rt.anchoredPosition = new Vector2(-16, -16);

        Image img = hudBtnGO.GetComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedFrame(img, Color.white);
        }

        Button btn = hudBtnGO.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.05f);
        btn.colors = colors;
        UnityEventTools.AddPersistentListener(btn.onClick, menu.Open);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(hudBtnGO.transform, false);
        StretchFull(txtGO.GetComponent<RectTransform>());

        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        ApplyFont(txt);
        txt.fontSize = 13.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(1f, 0.9f, 0.5f);
        txt.text = "[ MENU (ESC) ]";

        return hudBtnGO;
    }
    #endregion
}
