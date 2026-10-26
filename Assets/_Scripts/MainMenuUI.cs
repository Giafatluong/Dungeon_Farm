using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu and Pause Menu interface at Base.
/// Uses 100% Figma Asset GOTY components (UI_Frame, UI_Toolbar, UI_Slot) with authentic pixel art styling.
/// When opened: Automatically hides player and ingame HUD, displaying only the peaceful nature background behind!
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    public static MainMenuUI Instance { get; private set; }

    private static bool _hasShownInitialMenu = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        _hasShownInitialMenu = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitializeOnStart()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        // Automatically open Main Menu on initial entry into Base Scene
        if (!_hasShownInitialMenu && (sceneName == "Base" || sceneName.Contains("Base")))
        {
            EnsureInstance();
            if (Instance != null)
            {
                Instance.Open();
                _hasShownInitialMenu = true;
            }
        }
    }

    public static MainMenuUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        MainMenuUI found = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        GameObject go = new GameObject("MainMenuUI");
        Instance = go.AddComponent<MainMenuUI>();
        return Instance;
    }

    #region Inspector & State
    [Header("Menu State")]
    private bool isMenuOpen = false;
    public bool IsOpen => isMenuOpen;

    // UI References
    private Canvas targetCanvas;
    private GameObject menuRootGO;
    private CanvasGroup menuCanvasGroup;

    // Sub-panels
    private GameObject mainButtonsPanel;
    private GameObject howToPlayPanel;
    private GameObject settingsPanel;
    private GameObject confirmNewGamePanel;

    // Text labels
    private TextMeshProUGUI saveInfoText;
    private TextMeshProUGUI fullscreenBtnText;
    private TextMeshProUGUI soundBtnText;

    // Floating HUD button to open menu anytime
    private GameObject hudMenuButtonGO;

    // Track hidden gameplay elements
    private readonly List<Renderer> hiddenPlayerRenderers = new();
    private readonly List<GameObject> hiddenHUDObjects = new();
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUIConstructed();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Equals("Base", System.StringComparison.OrdinalIgnoreCase))
        {
            EnsureUIConstructed();
            RefreshSaveSummary();
        }
        else
        {
            if (isMenuOpen) Close();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isBase = currentScene.Equals("Base", System.StringComparison.OrdinalIgnoreCase);

        // HUD corner button only displayed when in Base and menu is closed
        if (hudMenuButtonGO != null)
        {
            hudMenuButtonGO.SetActive(isBase && !isMenuOpen);
        }

        if (!isBase) return;

        // Press ESC to open / close Main Menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
                // If in a subpanel (Settings, HowToPlay, Confirm), return to main menu
                if (howToPlayPanel != null && howToPlayPanel.activeSelf)
                {
                    ShowSubPanel(mainButtonsPanel);
                }
                else if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    ShowSubPanel(mainButtonsPanel);
                }
                else if (confirmNewGamePanel != null && confirmNewGamePanel.activeSelf)
                {
                    ShowSubPanel(mainButtonsPanel);
                }
                else
                {
                    Close();
                }
            }
            else
            {
                // Only open if no other blocking modal is open
                if (!IsOtherBlockingUIOpen())
                {
                    Open();
                }
            }
        }
    }

    private bool IsOtherBlockingUIOpen()
    {
        if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen) return true;
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return true;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return true;
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return true;
        if (DungeonEntranceUI.Instance != null && DungeonEntranceUI.Instance.IsOpen) return true;
        return false;
    }

    #region Open & Close Controls
    public void Open()
    {
        EnsureUIConstructed();
        RefreshSaveSummary();
        ShowSubPanel(mainButtonsPanel);

        // 1. Hide Player and ingame HUD, leaving the background game environment visible
        SetGameplayEntitiesVisible(false);

        if (menuRootGO != null)
        {
            menuRootGO.SetActive(true);
        }
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.blocksRaycasts = true;
            menuCanvasGroup.interactable = true;
        }

        isMenuOpen = true;
        Time.timeScale = 0f; // Pause game action when in Main Menu
    }

    public void Close()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.blocksRaycasts = false;
            menuCanvasGroup.interactable = false;
        }
        if (menuRootGO != null)
        {
            menuRootGO.SetActive(false);
        }

        // 2. Restore Player and HUD elements for gameplay
        SetGameplayEntitiesVisible(true);

        isMenuOpen = false;
        Time.timeScale = 1f; // Restore game time
    }

    private void ShowSubPanel(GameObject activePanel)
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(mainButtonsPanel == activePanel);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(howToPlayPanel == activePanel);
        if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == activePanel);
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(confirmNewGamePanel == activePanel);
    }

    /// <summary>
    /// Shows or hides Player character and ingame HUD (Hotbar, health bar, inventory...)
    /// Allows the Main Menu to see through cleanly to the tranquil game environment!
    /// </summary>
    private void SetGameplayEntitiesVisible(bool visible)
    {
        if (!visible)
        {
            hiddenPlayerRenderers.Clear();
            hiddenHUDObjects.Clear();

            // 1. Temporarily hide Player SpriteRenderer (so character doesn't block background)
            PlayerMovement pm = PlayerMovement.Instance != null ? PlayerMovement.Instance : FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                SpriteRenderer[] srs = pm.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++)
                {
                    if (srs[i] != null && srs[i].enabled)
                    {
                        hiddenPlayerRenderers.Add(srs[i]);
                        srs[i].enabled = false;
                    }
                }
            }

            // 2. Temporarily hide Hotbar and bottom toolbars
            CurrentItemPanel cip = FindFirstObjectByType<CurrentItemPanel>(FindObjectsInactive.Include);
            if (cip != null && cip.gameObject.activeSelf)
            {
                hiddenHUDObjects.Add(cip.gameObject);
                cip.gameObject.SetActive(false);
            }

            // 3. Temporarily hide HealthBarCanvas if present
            GameObject hbCanvas = GameObject.Find("HealthBarcanvas");
            if (hbCanvas != null && hbCanvas.activeSelf)
            {
                hiddenHUDObjects.Add(hbCanvas);
                hbCanvas.SetActive(false);
            }

            // 4. Temporarily hide MiddleBar if present
            GameObject middleBar = GameObject.Find("MiddleBar");
            if (middleBar != null && middleBar.activeSelf)
            {
                hiddenHUDObjects.Add(middleBar);
                middleBar.SetActive(false);
            }
        }
        else
        {
            // Restore Player renderers
            for (int i = 0; i < hiddenPlayerRenderers.Count; i++)
            {
                if (hiddenPlayerRenderers[i] != null)
                {
                    hiddenPlayerRenderers[i].enabled = true;
                }
            }
            hiddenPlayerRenderers.Clear();

            // Restore HUD elements
            for (int i = 0; i < hiddenHUDObjects.Count; i++)
            {
                if (hiddenHUDObjects[i] != null)
                {
                    hiddenHUDObjects[i].SetActive(true);
                }
            }
            hiddenHUDObjects.Clear();
        }
    }
    #endregion

    #region Button Actions
    public void OnContinueClicked()
    {
        Close();
    }

    public void OnNewGameClicked()
    {
        ShowSubPanel(confirmNewGamePanel);
    }

    public void OnConfirmNewGameYes()
    {
        Debug.Log("[MainMenuUI] Resetting ALL game data for a completely fresh New Game...");

        // 1. Delete all PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("Farm_CurrentDay", 1);
        PlayerPrefs.SetInt("Dungeon_HighestUnlockedFloor", 1);
        PlayerPrefs.Save();

        // 2. Reset all ProgressionManager progression (Permanent stats, Dungeon floors, Bosses, Blessings, Recipes)
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ResetAllProgression();
        }

        // 3. Reset DayManager to Day 1
        if (DayManager.Instance != null)
        {
            DayManager.Instance.currentDay = 1;
        }

        // 4. Clear all data in all ItemContainers (Backpack & Storage Chest)
        try
        {
            ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
            if (containers != null)
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    if (containers[i] != null)
                    {
                        containers[i].Clear();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MainMenuUI] Error clearing ItemContainers: {ex.Message}");
        }

        InventoryButton.selectedItem = null;

        // 5. Reset all Statue Offerings
        try
        {
            Offering[] offerings = GameAssetHelper.LoadAll<Offering>();
            if (offerings != null)
            {
                for (int i = 0; i < offerings.Length; i++)
                {
                    if (offerings[i] != null)
                    {
                        offerings[i].completed = false;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MainMenuUI] Error resetting Offerings: {ex.Message}");
        }

        // 6. Reset all FarmPlots in current scene
        try
        {
            FarmPlot[] plots = FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (plots != null)
            {
                for (int i = 0; i < plots.Length; i++)
                {
                    if (plots[i] != null)
                    {
                        plots[i].SetDefault();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MainMenuUI] Error resetting FarmPlots: {ex.Message}");
        }

        // 7. Reset player stats in PlayerStats
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ClearTemporaryBuffs();
            PlayerStats.Instance.RefreshStatsFromProgression();
            PlayerStats.Instance.currentHealth = PlayerStats.Instance.maxHealth;
            PlayerStats.Instance.currentHunger = PlayerStats.Instance.maxHunger;
            PlayerStats.Instance.currentAP = PlayerStats.Instance.maxAP;
            PlayerStats.Instance.defendCount = 0;
        }

        // 8. Close menu, restore timeScale, and reload Base scene
        Close();
        ShowSubPanel(mainButtonsPanel);
        Time.timeScale = 1f;
        _hasShownInitialMenu = true;
        SceneManager.LoadScene("Base");
    }

    public void OnConfirmNewGameNo()
    {
        ShowSubPanel(mainButtonsPanel);
    }

    public void OnHowToPlayClicked()
    {
        ShowSubPanel(howToPlayPanel);
    }

    public void OnSettingsClicked()
    {
        UpdateSettingsButtonsText();
        ShowSubPanel(settingsPanel);
    }

    public void OnToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        StartCoroutine(UpdateFullscreenTextNextFrame());
    }

    private IEnumerator UpdateFullscreenTextNextFrame()
    {
        yield return null;
        UpdateSettingsButtonsText();
    }

    public void OnToggleSound()
    {
        AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
        UpdateSettingsButtonsText();
    }

    private void UpdateSettingsButtonsText()
    {
        if (fullscreenBtnText != null)
        {
            fullscreenBtnText.text = Screen.fullScreen ? "Mode: [ FULLSCREEN ]" : "Mode: [ WINDOWED ]";
        }
        if (soundBtnText != null)
        {
            soundBtnText.text = AudioListener.volume > 0f ? "Sound: [ ON 100% ]" : "Sound: [ MUTED ]";
        }
    }

    public void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quitting Game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnBackToMenuClicked()
    {
        ShowSubPanel(mainButtonsPanel);
    }
    #endregion

    #region Procedural UI Builder with Figma GOTY Assets
    public void EnsureUIConstructed()
    {
        if (menuRootGO != null && targetCanvas != null && hudMenuButtonGO != null) return;

        if (menuRootGO != null && targetCanvas == null)
        {
            Destroy(menuRootGO);
            menuRootGO = null;
        }
        if (hudMenuButtonGO != null && targetCanvas == null)
        {
            Destroy(hudMenuButtonGO);
            hudMenuButtonGO = null;
        }

        targetCanvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (targetCanvas == null)
        {
            Canvas found = FindFirstObjectByType<Canvas>();
            if (found != null) targetCanvas = found;
            else
            {
                GameObject canvasGO = new GameObject("MainMenu_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                targetCanvas = canvasGO.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 999; // Topmost sorting order
                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }
        }

        // Standard Sprites from Figma Asset GOTY (UI_Frame, UI_Toolbar, UI_Slot)
        Sprite frameSprite = DungeonUIAssetHelper.GetFrameSprite();
        Sprite toolbarSprite = DungeonUIAssetHelper.GetToolbarSprite();
        Sprite slotSprite = DungeonUIAssetHelper.GetSlotSprite();

        // 1. Root Overlay: Subtle transparency (15% tint to highlight modal while showing nature background)
        menuRootGO = new GameObject("MainMenu_Root", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        menuRootGO.transform.SetParent(targetCanvas.transform, false);
        menuCanvasGroup = menuRootGO.GetComponent<CanvasGroup>();

        RectTransform rootRT = menuRootGO.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        Image bgImg = menuRootGO.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.20f); // Clear tint, fully reveals game environment

        // 2. Center Modal Frame: UI_Frame from Figma Asset GOTY with original Color.white
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
            cmImg.color = Color.white; // Original Figma Asset GOTY color
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedFrame(cmImg, Color.white);
        }

        // 3. Title Banner: UI_Toolbar from Figma Asset GOTY
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
            tpImg.color = Color.white; // Original Figma Toolbar color
        }
        else
        {
            DungeonUIAssetHelper.StylePlaque(tpImg, DungeonUIAssetHelper.PlaqueStyle.Gold);
        }

        // Game Title: DUNGEON FARM
        GameObject titleTextGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleTextGO.transform.SetParent(titlePlaqueGO.transform, false);
        RectTransform ttRT = titleTextGO.GetComponent<RectTransform>();
        ttRT.anchorMin = Vector2.zero;
        ttRT.anchorMax = Vector2.one;
        ttRT.offsetMin = new Vector2(10, 20);
        ttRT.offsetMax = new Vector2(-10, -4);
        TextMeshProUGUI tt = titleTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tt);
        tt.fontSize = 32;
        tt.fontStyle = FontStyles.Bold;
        tt.alignment = TextAlignmentOptions.Center;
        tt.color = new Color(1f, 0.88f, 0.40f);
        tt.text = "DUNGEON FARM";

        // Subtitle
        GameObject subTextGO = new GameObject("SubTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subTextGO.transform.SetParent(titlePlaqueGO.transform, false);
        RectTransform stRT = subTextGO.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero;
        stRT.anchorMax = Vector2.one;
        stRT.offsetMin = new Vector2(10, 4);
        stRT.offsetMax = new Vector2(-10, -44);
        TextMeshProUGUI st = subTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(st);
        st.fontSize = 12.5f;
        st.alignment = TextAlignmentOptions.Center;
        st.color = new Color(0.85f, 0.95f, 0.90f);
        st.text = "Farming By Day - Dungeon Crawling By Night";

        // 4. Save Status Box: UI_Frame 9-slice small
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
        saveInfoText = siTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(saveInfoText);
        saveInfoText.fontSize = 13.5f;
        saveInfoText.alignment = TextAlignmentOptions.Center;
        saveInfoText.color = new Color(0.95f, 0.95f, 1f);
        RefreshSaveSummary();

        // 5. Build Sub-Panels
        RectTransform contentArea = CreateContentArea(centerModalGO.transform);
        BuildMainButtonsPanel(contentArea, frameSprite);
        BuildHowToPlayPanel(contentArea, frameSprite);
        BuildSettingsPanel(contentArea, frameSprite);
        BuildConfirmNewGamePanel(contentArea, frameSprite);

        // 6. Floating HUD Menu Button in screen corner (using UI_Frame)
        BuildFloatingHUDButton(frameSprite);

        // Hidden by default
        Close();
    }

    private RectTransform CreateContentArea(Transform parent)
    {
        GameObject contentAreaGO = new GameObject("ContentArea", typeof(RectTransform));
        contentAreaGO.transform.SetParent(parent, false);
        RectTransform rt = contentAreaGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 0);
        rt.offsetMin = new Vector2(-250, 24);
        rt.offsetMax = new Vector2(250, -170);
        return rt;
    }

    private void BuildMainButtonsPanel(Transform parent, Sprite buttonFrameSprite)
    {
        mainButtonsPanel = new GameObject("MainButtonsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        mainButtonsPanel.transform.SetParent(parent, false);
        RectTransform rt = mainButtonsPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = mainButtonsPanel.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        CreateFigmaButton(mainButtonsPanel.transform, buttonFrameSprite, "CONTINUE", new Color(1f, 0.95f, 0.70f), OnContinueClicked);
        CreateFigmaButton(mainButtonsPanel.transform, buttonFrameSprite, "NEW GAME", new Color(0.70f, 0.90f, 1f), OnNewGameClicked);
        CreateFigmaButton(mainButtonsPanel.transform, buttonFrameSprite, "HOW TO PLAY & CONTROLS", new Color(0.80f, 1f, 0.80f), OnHowToPlayClicked);
        CreateFigmaButton(mainButtonsPanel.transform, buttonFrameSprite, "SETTINGS", new Color(0.90f, 0.90f, 1f), OnSettingsClicked);
        CreateFigmaButton(mainButtonsPanel.transform, buttonFrameSprite, "QUIT GAME", new Color(1f, 0.70f, 0.70f), OnQuitClicked);
    }

    private void BuildHowToPlayPanel(Transform parent, Sprite frameSprite)
    {
        howToPlayPanel = new GameObject("HowToPlayPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        howToPlayPanel.transform.SetParent(parent, false);
        RectTransform rt = howToPlayPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = howToPlayPanel.GetComponent<Image>();
        if (frameSprite != null)
        {
            bg.sprite = frameSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.85f, 0.85f, 0.90f, 0.98f);
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedPanel(bg, DungeonUIAssetHelper.PanelStyle.Slate);
        }

        VerticalLayoutGroup vlg = howToPlayPanel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Title
        GameObject tGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        tGO.transform.SetParent(howToPlayPanel.transform, false);
        TextMeshProUGUI title = tGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(title);
        title.fontSize = 19;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.85f, 0.4f);
        title.text = "[ HOW TO PLAY & CONTROLS ]";
        title.alignment = TextAlignmentOptions.Center;

        // Content
        GameObject cGO = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
        cGO.transform.SetParent(howToPlayPanel.transform, false);
        TextMeshProUGUI content = cGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(content);
        content.fontSize = 13.5f;
        content.color = new Color(0.95f, 0.95f, 0.98f);
        content.text =
            "- <b>[W][A][S][D] / Arrow Keys:</b> Move character\n" +
            "- <b>[Left Click]:</b> Plant seeds (when holding seed) / Harvest crops\n" +
            "- <b>[Key E]:</b> Interact with Kitchen Pot, Storage Chest, Statue, Dungeon Portal\n" +
            "- <b>[Key I]:</b> Open / Close Backpack Inventory\n" +
            "- <b>[Sleep]:</b> Approach Bed and press <b>[E]</b> to advance day and crop cycles\n" +
            "- <b>[Key ESC]:</b> Open / Close this Menu at any time in Base";

        // Back button
        CreateFigmaButton(howToPlayPanel.transform, frameSprite, "BACK TO MENU", Color.white, () => ShowSubPanel(mainButtonsPanel), 46);
        howToPlayPanel.SetActive(false);
    }

    private void BuildSettingsPanel(Transform parent, Sprite frameSprite)
    {
        settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        settingsPanel.transform.SetParent(parent, false);
        RectTransform rt = settingsPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = settingsPanel.GetComponent<Image>();
        if (frameSprite != null)
        {
            bg.sprite = frameSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.85f, 0.85f, 0.90f, 0.98f);
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedPanel(bg, DungeonUIAssetHelper.PanelStyle.Slate);
        }

        VerticalLayoutGroup vlg = settingsPanel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 14;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Title
        GameObject tGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        tGO.transform.SetParent(settingsPanel.transform, false);
        TextMeshProUGUI title = tGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(title);
        title.fontSize = 19;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.85f, 0.4f);
        title.text = "[ SETTINGS ]";
        title.alignment = TextAlignmentOptions.Center;

        // Fullscreen Button
        GameObject fsBtn = CreateFigmaButton(settingsPanel.transform, frameSprite, "Display Mode", Color.white, OnToggleFullscreen, 48);
        fullscreenBtnText = fsBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Sound Toggle Button
        GameObject sndBtn = CreateFigmaButton(settingsPanel.transform, frameSprite, "Sound", Color.white, OnToggleSound, 48);
        soundBtnText = sndBtn.GetComponentInChildren<TextMeshProUGUI>();

        UpdateSettingsButtonsText();

        // Back button
        CreateFigmaButton(settingsPanel.transform, frameSprite, "BACK TO MENU", Color.white, () => ShowSubPanel(mainButtonsPanel), 46);
        settingsPanel.SetActive(false);
    }

    private void BuildConfirmNewGamePanel(Transform parent, Sprite frameSprite)
    {
        confirmNewGamePanel = new GameObject("ConfirmNewGamePanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        confirmNewGamePanel.transform.SetParent(parent, false);
        RectTransform rt = confirmNewGamePanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = confirmNewGamePanel.GetComponent<Image>();
        if (frameSprite != null)
        {
            bg.sprite = frameSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.95f, 0.80f, 0.80f, 0.98f);
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.20f, 0.08f, 0.08f, 0.98f));
        }

        VerticalLayoutGroup vlg = confirmNewGamePanel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 14;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Title
        GameObject tGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        tGO.transform.SetParent(confirmNewGamePanel.transform, false);
        TextMeshProUGUI title = tGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(title);
        title.fontSize = 19;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.4f, 0.4f);
        title.text = "[ CONFIRM NEW GAME ]";
        title.alignment = TextAlignmentOptions.Center;

        // Warning Text
        GameObject wGO = new GameObject("WarnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        wGO.transform.SetParent(confirmNewGamePanel.transform, false);
        TextMeshProUGUI warn = wGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(warn);
        warn.fontSize = 14;
        warn.color = Color.white;
        warn.alignment = TextAlignmentOptions.Center;
        warn.text = "Are you sure you want to <b>start over from scratch</b>?\n\nAll farm day progress, crops, stored chest items, and dungeon progress will be wiped and reset to <b>Day 1</b>!";

        // Yes button
        CreateFigmaButton(confirmNewGamePanel.transform, frameSprite, "CONFIRM - RESET & START OVER", new Color(1f, 0.6f, 0.6f), OnConfirmNewGameYes, 48);

        // No button
        CreateFigmaButton(confirmNewGamePanel.transform, frameSprite, "CANCEL - RETURN", Color.white, OnConfirmNewGameNo, 46);

        confirmNewGamePanel.SetActive(false);
    }

    /// <summary>
    /// Creates a button using the UI_Frame 9-slice Sprite from Figma Asset GOTY
    /// Preserves pixel-art borders and original metal/wood textures.
    /// </summary>
    private GameObject CreateFigmaButton(Transform parent, Sprite frameSprite, string label, Color textColor, UnityEngine.Events.UnityAction action, float height = 50)
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
            img.color = Color.white; // Original Figma color
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
        btn.onClick.AddListener(action);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(txt);
        txt.fontSize = 16.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = textColor;
        txt.text = label;

        return btnGO;
    }

    private void BuildFloatingHUDButton(Sprite frameSprite)
    {
        if (hudMenuButtonGO != null || targetCanvas == null) return;

        hudMenuButtonGO = new GameObject("HUD_MenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        hudMenuButtonGO.transform.SetParent(targetCanvas.transform, false);

        RectTransform rt = hudMenuButtonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(140, 42);
        rt.anchoredPosition = new Vector2(-16, -16);

        Image img = hudMenuButtonGO.GetComponent<Image>();
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

        Button btn = hudMenuButtonGO.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.05f);
        btn.colors = colors;
        btn.onClick.AddListener(Open);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(hudMenuButtonGO.transform, false);
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(txt);
        txt.fontSize = 13.5f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(1f, 0.9f, 0.5f);
        txt.text = "[ MENU (ESC) ]";
    }

    private void RefreshSaveSummary()
    {
        if (saveInfoText == null) return;
        int currentDay = PlayerPrefs.GetInt("Farm_CurrentDay", 1);
        int unlockedFloor = 1;
        if (ProgressionManager.Instance != null)
        {
            unlockedFloor = Mathf.Max(1, ProgressionManager.Instance.highestUnlockedFloor);
        }
        else
        {
            unlockedFloor = PlayerPrefs.GetInt("Progression_HighestFloor", 1);
        }

        saveInfoText.text = $"Current Progress: <color=#FFE066><b>Day {currentDay}</b></color>  |  Dungeon: <color=#80CBC4><b>Floor {unlockedFloor}</b></color>";
    }
    #endregion
}
