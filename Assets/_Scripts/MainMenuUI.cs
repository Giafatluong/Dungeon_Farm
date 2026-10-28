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
        AutoBindReferences();

        if (!isMenuOpen && menuRootGO != null)
        {
            menuRootGO.SetActive(false);
        }
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
            if (menuCanvasGroup == null)
                menuCanvasGroup = menuRootGO.GetComponent<CanvasGroup>();

            Transform cm = menuRootGO.transform.Find("CenterModal");
            if (cm != null)
            {
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
}
