using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu and Pause Menu interface at Base.
/// Uses the scene-authored MainMenuCanvas hierarchy in Base.unity.
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
            Instance.AutoBindReferences();
            return Instance;
        }

        return null;
    }

    #region Inspector & State
    [Header("Menu State")]
    private bool isMenuOpen = false;
    private bool isBaseScene = false;
    public bool IsOpen => isMenuOpen;

    [Header("UI Hierarchy References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject menuRootGO;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Sub-panels")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmNewGamePanel;

    [Header("Text labels")]
    [SerializeField] private TextMeshProUGUI saveInfoText;
    [SerializeField] private TextMeshProUGUI fullscreenBtnText;
    [SerializeField] private TextMeshProUGUI soundBtnText;

    [Header("HUD")]
    [SerializeField] private GameObject hudMenuButtonGO;

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
        isBaseScene = SceneManager.GetActiveScene().name.Equals("Base", System.StringComparison.OrdinalIgnoreCase);
        AutoBindReferences();

        if (!isMenuOpen && menuRootGO != null)
        {
            menuRootGO.SetActive(false);
        }
    }

    private void OnEnable()
    {
        isBaseScene = SceneManager.GetActiveScene().name.Equals("Base", System.StringComparison.OrdinalIgnoreCase);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isBaseScene = scene.name.Equals("Base", System.StringComparison.OrdinalIgnoreCase);
        if (isBaseScene)
        {
            AutoBindReferences();
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
        // HUD corner button only displayed when in Base and menu is closed
        if (hudMenuButtonGO != null)
        {
            bool shouldShowHudBtn = isBaseScene && !isMenuOpen;
            if (hudMenuButtonGO.activeSelf != shouldShowHudBtn)
            {
                hudMenuButtonGO.SetActive(shouldShowHudBtn);
            }
        }

        if (!isBaseScene) return;

        // Press ESC to open / close Main Menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
                UIEscapeManager.ConsumeEscape();
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
                // Only open if no other blocking modal is open and escape was not consumed this frame
                if (!IsOtherBlockingUIOpen())
                {
                    Open();
                }
            }
        }
    }

    private bool IsOtherBlockingUIOpen()
    {
        if (UIEscapeManager.IsEscapeConsumedThisFrame()) return true;
        if (UIEscapeManager.IsAnyOtherUIOpen()) return true;
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
        AutoBindReferences();
        EnsureBackground();
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

        // Resume/Start tutorial flow if tutorial is active
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnMainMenuClosed();
        }
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
            GameObject hbCanvas = GameObject.Find("HealthBarCanvas");
            if (hbCanvas == null) hbCanvas = GameObject.Find("HealthBarcanvas");
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
        // If the player already finished the tutorial in this save or has advanced days/floors, keep tutorial dismissed
        bool isTutorialAlreadyDone = PlayerPrefs.GetInt(TutorialManager.TutorialDoneKey, 0) == 1
                                  || PlayerPrefs.GetInt("Farm_CurrentDay", 1) > 1
                                  || PlayerPrefs.GetInt("Dungeon_HighestUnlockedFloor", 1) > 1;

        if (isTutorialAlreadyDone)
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SkipOrCompleteTutorial();
            }
            if (IntroCutsceneController.Instance != null && IntroCutsceneController.Instance.IsPlaying)
            {
                IntroCutsceneController.Instance.SkipCutscene();
            }
            if (TutorialDialogueUI.Instance != null)
            {
                TutorialDialogueUI.Instance.HideDialogue();
            }
            if (CombatTutorialGuide.Instance != null)
            {
                CombatTutorialGuide.Instance.SkipTutorial();
            }
        }

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

        // Reset Tutorial progress and prepare tutorial crops and seeds immediately after clearing containers
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetTutorial();
            TutorialManager.Instance.EnsureTutorialCropsReady();
        }

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
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene("Base");
        }
        else
        {
            SceneManager.LoadScene("Base");
        }
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

    #region AutoBind References & Save Summary
    public void AutoBindReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
            if (targetCanvas == null && menuRootGO != null)
                targetCanvas = menuRootGO.GetComponentInParent<Canvas>();
        }

        if (menuRootGO == null)
        {
            Transform rootT = transform.Find("MainMenu_Root");
            if (rootT != null) menuRootGO = rootT.gameObject;
        }

        if (menuRootGO != null)
        {
            Image rootImg = menuRootGO.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = new Color(0.04f, 0.07f, 0.11f, 1f); // Opaque atmospheric base
            }

            EnsureBackground();

            if (menuCanvasGroup == null)
                menuCanvasGroup = menuRootGO.GetComponent<CanvasGroup>();

            Transform cm = menuRootGO.transform.Find("CenterModal");
            if (cm != null)
            {
                Transform titleTextT = cm.Find("TitlePlaque/TitleText");
                if (titleTextT != null)
                {
                    TextMeshProUGUI tTMP = titleTextT.GetComponent<TextMeshProUGUI>();
                    if (tTMP != null && tTMP.text != "HARVEST & HOLLOW")
                    {
                        tTMP.text = "HARVEST & HOLLOW";
                        tTMP.fontStyle = FontStyles.Bold;
                        DungeonUIAssetHelper.ApplyFont(tTMP, withOutline: true);
                    }
                }

                if (saveInfoText == null)
                {
                    Transform saveT = cm.Find("SaveInfoBox/SaveText");
                    if (saveT != null) saveInfoText = saveT.GetComponent<TextMeshProUGUI>();
                }

                Transform contentArea = cm.Find("ContentArea");
                Transform panelParent = contentArea != null ? contentArea : cm;

                if (mainButtonsPanel == null)
                {
                    Transform mb = panelParent.Find("MainButtonsPanel");
                    if (mb != null) mainButtonsPanel = mb.gameObject;
                }
                if (howToPlayPanel == null)
                {
                    Transform htp = panelParent.Find("HowToPlayPanel");
                    if (htp != null) howToPlayPanel = htp.gameObject;
                }
                if (settingsPanel == null)
                {
                    Transform sp = panelParent.Find("SettingsPanel");
                    if (sp != null) settingsPanel = sp.gameObject;
                }
                if (confirmNewGamePanel == null)
                {
                    Transform cng = panelParent.Find("ConfirmNewGamePanel");
                    if (cng != null) confirmNewGamePanel = cng.gameObject;
                }

                if (settingsPanel != null)
                {
                    if (fullscreenBtnText == null)
                    {
                        Transform fs = settingsPanel.transform.Find("Btn_Display Mode");
                        if (fs != null) fullscreenBtnText = fs.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                    if (soundBtnText == null)
                    {
                        Transform snd = settingsPanel.transform.Find("Btn_Sound");
                        if (snd != null) soundBtnText = snd.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                }
            }
        }

        if (hudMenuButtonGO == null)
        {
            Transform hudT = transform.Find("HUD_MenuButton");
            if (hudT != null) hudMenuButtonGO = hudT.gameObject;
        }
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

    #region Atmospheric Main Menu Background
    private GameObject backgroundRootGO;
    private static Sprite _cachedGradientSprite;

    private void EnsureBackground()
    {
        if (menuRootGO == null) return;

        Transform existingBg = menuRootGO.transform.Find("MainMenu_Background");
        if (existingBg != null)
        {
            backgroundRootGO = existingBg.gameObject;
            backgroundRootGO.transform.SetSiblingIndex(0);
            return;
        }

        backgroundRootGO = new GameObject("MainMenu_Background", typeof(RectTransform));
        backgroundRootGO.transform.SetParent(menuRootGO.transform, false);
        backgroundRootGO.transform.SetSiblingIndex(0); // Place behind CenterModal

        RectTransform bgRT = backgroundRootGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // 1. Fullscreen Atmospheric Deep Sky Gradient (Midnight Navy -> Twilight Teal -> Pine Forest Earth)
        GameObject skyGO = new GameObject("SkyGradient", typeof(RectTransform), typeof(Image));
        skyGO.transform.SetParent(backgroundRootGO.transform, false);
        RectTransform skyRT = skyGO.GetComponent<RectTransform>();
        skyRT.anchorMin = Vector2.zero;
        skyRT.anchorMax = Vector2.one;
        skyRT.offsetMin = Vector2.zero;
        skyRT.offsetMax = Vector2.zero;
        Image skyImg = skyGO.GetComponent<Image>();
        skyImg.sprite = GetOrCreateGradientSprite();
        skyImg.color = Color.white;
        skyImg.raycastTarget = false;

        // 2. Stars & Fireflies Cluster
        GameObject starsGO = new GameObject("StarsCluster", typeof(RectTransform));
        starsGO.transform.SetParent(backgroundRootGO.transform, false);
        RectTransform starsRT = starsGO.GetComponent<RectTransform>();
        starsRT.anchorMin = Vector2.zero;
        starsRT.anchorMax = Vector2.one;
        starsRT.offsetMin = Vector2.zero;
        starsRT.offsetMax = Vector2.zero;

        System.Random rnd = new System.Random(42);
        for (int i = 0; i < 40; i++)
        {
            GameObject star = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            star.transform.SetParent(starsGO.transform, false);
            RectTransform sRT = star.GetComponent<RectTransform>();
            float rx = (float)rnd.NextDouble();
            float ry = 0.32f + (float)rnd.NextDouble() * 0.65f; // Upper 68% of screen
            sRT.anchorMin = new Vector2(rx, ry);
            sRT.anchorMax = new Vector2(rx, ry);
            float sz = 2.5f + (float)rnd.NextDouble() * 3.5f;
            sRT.sizeDelta = new Vector2(sz, sz);

            Image sImg = star.GetComponent<Image>();
            float a = 0.35f + (float)rnd.NextDouble() * 0.55f;
            sImg.color = (i % 3 == 0)
                ? new Color(0.65f, 0.95f, 1f, a)  // Ice cyan star
                : (i % 3 == 1)
                    ? new Color(1f, 0.95f, 0.65f, a) // Warm gold star
                    : new Color(0.7f, 1f, 0.85f, a);  // Emerald glow
            sImg.raycastTarget = false;
        }

        // 3. Silhouette Horizon Layer (Bottom 36%)
        GameObject horizonGO = new GameObject("HorizonLandscape", typeof(RectTransform));
        horizonGO.transform.SetParent(backgroundRootGO.transform, false);
        RectTransform hzRT = horizonGO.GetComponent<RectTransform>();
        hzRT.anchorMin = new Vector2(0f, 0f);
        hzRT.anchorMax = new Vector2(1f, 0.36f);
        hzRT.offsetMin = Vector2.zero;
        hzRT.offsetMax = Vector2.zero;

        // Dark Forest Ground Silhouette
        GameObject groundGO = new GameObject("GroundSilhouette", typeof(RectTransform), typeof(Image));
        groundGO.transform.SetParent(horizonGO.transform, false);
        RectTransform groundRT = groundGO.GetComponent<RectTransform>();
        groundRT.anchorMin = Vector2.zero;
        groundRT.anchorMax = new Vector2(1f, 0.65f);
        groundRT.offsetMin = Vector2.zero;
        groundRT.offsetMax = Vector2.zero;
        Image groundImg = groundGO.GetComponent<Image>();
        groundImg.color = new Color(0.03f, 0.07f, 0.06f, 0.96f); // Deep dark forest floor
        groundImg.raycastTarget = false;

        // Distant Trees & Nature Elements
        Sprite oakSprite = DungeonUIAssetHelper.FindSprite("Oak_Tree") ?? DungeonUIAssetHelper.FindSprite("Spruce_tree");
        Sprite birchSprite = DungeonUIAssetHelper.FindSprite("Birch_Tree") ?? DungeonUIAssetHelper.FindSprite("Spruce_Tree_Small");
        Sprite tentSprite = DungeonUIAssetHelper.GetTentSprite();
        Sprite chestSprite = DungeonUIAssetHelper.GetChestSprite();

        // Left Camp: Tent & Campfire
        if (tentSprite != null)
        {
            GameObject tentGO = new GameObject("CampTent", typeof(RectTransform), typeof(Image));
            tentGO.transform.SetParent(horizonGO.transform, false);
            RectTransform tRT = tentGO.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.11f, 0.35f);
            tRT.anchorMax = new Vector2(0.11f, 0.35f);
            tRT.sizeDelta = new Vector2(96, 84);
            Image tImg = tentGO.GetComponent<Image>();
            tImg.sprite = tentSprite;
            tImg.color = new Color(0.18f, 0.28f, 0.22f, 0.90f);
            tImg.preserveAspect = true;
            tImg.raycastTarget = false;
        }

        // Warm Campfire Glow
        GameObject fireGlowGO = new GameObject("CampfireGlow", typeof(RectTransform), typeof(Image));
        fireGlowGO.transform.SetParent(horizonGO.transform, false);
        RectTransform fgRT = fireGlowGO.GetComponent<RectTransform>();
        fgRT.anchorMin = new Vector2(0.18f, 0.22f);
        fgRT.anchorMax = new Vector2(0.18f, 0.22f);
        fgRT.sizeDelta = new Vector2(90, 90);
        Image fgImg = fireGlowGO.GetComponent<Image>();
        fgImg.color = new Color(1f, 0.55f, 0.15f, 0.25f); // Warm campfire ambiance
        fgImg.raycastTarget = false;

        // Tree clusters on left & right
        if (oakSprite != null)
        {
            CreateSilhouettedTree(horizonGO.transform, oakSprite, new Vector2(0.04f, 0.42f), new Vector2(110, 140));
            CreateSilhouettedTree(horizonGO.transform, birchSprite ?? oakSprite, new Vector2(0.24f, 0.38f), new Vector2(85, 110));

            CreateSilhouettedTree(horizonGO.transform, oakSprite, new Vector2(0.82f, 0.40f), new Vector2(120, 150));
            CreateSilhouettedTree(horizonGO.transform, birchSprite ?? oakSprite, new Vector2(0.92f, 0.44f), new Vector2(95, 120));
            CreateSilhouettedTree(horizonGO.transform, oakSprite, new Vector2(0.74f, 0.34f), new Vector2(80, 100));
        }

        // Dungeon Chest on right horizon
        if (chestSprite != null)
        {
            GameObject chestGO = new GameObject("DungeonChest", typeof(RectTransform), typeof(Image));
            chestGO.transform.SetParent(horizonGO.transform, false);
            RectTransform cRT = chestGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.88f, 0.20f);
            cRT.anchorMax = new Vector2(0.88f, 0.20f);
            cRT.sizeDelta = new Vector2(46, 46);
            Image cImg = chestGO.GetComponent<Image>();
            cImg.sprite = chestSprite;
            cImg.color = new Color(0.90f, 0.72f, 0.28f, 0.80f);
            cImg.preserveAspect = true;
            cImg.raycastTarget = false;
        }

        // 4. Cinematic Dark Edge Vignette
        GameObject vignetteGO = new GameObject("VignetteShadow", typeof(RectTransform), typeof(Image));
        vignetteGO.transform.SetParent(backgroundRootGO.transform, false);
        RectTransform vRT = vignetteGO.GetComponent<RectTransform>();
        vRT.anchorMin = Vector2.zero;
        vRT.anchorMax = Vector2.one;
        vRT.offsetMin = Vector2.zero;
        vRT.offsetMax = Vector2.zero;
        Image vImg = vignetteGO.GetComponent<Image>();
        vImg.color = new Color(0.02f, 0.03f, 0.05f, 0.45f);
        vImg.raycastTarget = false;

        // 5. Grand Atmospheric Title Header (Positioned above CenterModal)
        GameObject headerGO = new GameObject("TitleHeaderBar", typeof(RectTransform));
        headerGO.transform.SetParent(backgroundRootGO.transform, false);
        RectTransform hdrRT = headerGO.GetComponent<RectTransform>();
        hdrRT.anchorMin = new Vector2(0.5f, 1f);
        hdrRT.anchorMax = new Vector2(0.5f, 1f);
        hdrRT.pivot = new Vector2(0.5f, 1f);
        hdrRT.anchoredPosition = new Vector2(0, -32);
        hdrRT.sizeDelta = new Vector2(850, 96);

        GameObject titleTextGO = new GameObject("MainTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleTextGO.transform.SetParent(headerGO.transform, false);
        RectTransform ttRT = titleTextGO.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0f, 0.35f);
        ttRT.anchorMax = new Vector2(1f, 1f);
        ttRT.offsetMin = Vector2.zero;
        ttRT.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleTextGO.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "HARVEST & HOLLOW";
        titleTMP.fontSize = 38;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 4f;
        titleTMP.color = new Color(1f, 0.88f, 0.42f, 1f); // Radiant sunlight gold
        DungeonUIAssetHelper.ApplyFont(titleTMP, withOutline: true);

        GameObject subTextGO = new GameObject("SubTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subTextGO.transform.SetParent(headerGO.transform, false);
        RectTransform stRT = subTextGO.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0f);
        stRT.anchorMax = new Vector2(1f, 0.38f);
        stRT.offsetMin = Vector2.zero;
        stRT.offsetMax = Vector2.zero;
        TextMeshProUGUI subTMP = subTextGO.GetComponent<TextMeshProUGUI>();
        subTMP.text = "✦  NÔNG TRẠI THANH BÌNH  •  THÁM HIỂM HẦM NGỤC  ✦";
        subTMP.fontSize = 13.5f;
        subTMP.fontStyle = FontStyles.Bold;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.characterSpacing = 2.5f;
        subTMP.color = new Color(0.55f, 0.85f, 0.80f, 0.95f); // Soft tranquil mint
        DungeonUIAssetHelper.ApplyFont(subTMP, withOutline: true);

        // 6. Bottom Navigation Hint
        GameObject hintGO = new GameObject("BottomHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(backgroundRootGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0, 16);
        hintRT.sizeDelta = new Vector2(-40, 24);
        TextMeshProUGUI hintTMP = hintGO.GetComponent<TextMeshProUGUI>();
        hintTMP.text = "<color=#607D8B>Harvest & Hollow v1.0</color>   |   <color=#90A4AE>Nhấn [Tiếp Tục] hoặc [Chơi Mới] để bắt đầu hành trình!</color>";
        hintTMP.fontSize = 12.5f;
        hintTMP.alignment = TextAlignmentOptions.Center;
        DungeonUIAssetHelper.ApplyFont(hintTMP, withOutline: true);
    }

    private static Sprite GetOrCreateGradientSprite()
    {
        if (_cachedGradientSprite != null) return _cachedGradientSprite;

        int width = 16;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color topColor = new Color(0.04f, 0.07f, 0.12f, 1f);     // Midnight Starfield
        Color midColor = new Color(0.08f, 0.14f, 0.18f, 1f);     // Twilight Teal Dusk
        Color bottomColor = new Color(0.05f, 0.10f, 0.08f, 1f);  // Pine Forest Gloom

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            Color rowColor = t < 0.5f
                ? Color.Lerp(bottomColor, midColor, t * 2f)
                : Color.Lerp(midColor, topColor, (t - 0.5f) * 2f);

            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = rowColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        _cachedGradientSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        return _cachedGradientSprite;
    }

    private void CreateSilhouettedTree(Transform parent, Sprite treeSprite, Vector2 anchorPos, Vector2 size)
    {
        if (treeSprite == null || parent == null) return;
        GameObject treeGO = new GameObject("TreeSilhouette", typeof(RectTransform), typeof(Image));
        treeGO.transform.SetParent(parent, false);
        RectTransform tRT = treeGO.GetComponent<RectTransform>();
        tRT.anchorMin = anchorPos;
        tRT.anchorMax = anchorPos;
        tRT.sizeDelta = size;
        Image tImg = treeGO.GetComponent<Image>();
        tImg.sprite = treeSprite;
        tImg.color = new Color(0.06f, 0.12f, 0.09f, 0.85f); // Deep forest green silhouette
        tImg.preserveAspect = true;
        tImg.raycastTarget = false;
    }
    #endregion
}
