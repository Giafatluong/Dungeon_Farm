using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Day Transition & Morning Report UI at Base.
/// Displays a smooth fade-to-black sequence, advances the farm day,
/// and presents a stylish morning report modal summarizing farm readiness and dungeon/merchant news.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
    public static DayTransitionUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        DayTransitionUI found = FindFirstObjectByType<DayTransitionUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        return null;
    }
    public static DayTransitionUI Instance { get; private set; }

    [Header("UI References (Optional - Auto constructed if null)")]
    public Canvas targetCanvas;
    public CanvasGroup fadeCanvasGroup;
    public Image fadeOverlayImage;
    public GameObject morningReportModal;
    public CanvasGroup modalCanvasGroup;

    [Header("Report Text Elements")]
    public TextMeshProUGUI headerTitleText;
    public TextMeshProUGUI headerSubtitleText;
    public TextMeshProUGUI farmSectionTitleText;
    public TextMeshProUGUI farmContentText;
    public TextMeshProUGUI worldSectionTitleText;
    public TextMeshProUGUI worldContentText;
    public Button startNewDayButton;
    public TextMeshProUGUI startNewDayButtonText;
    private RectTransform _cachedStartNewDayBtnRT;

    [Header("Transition Settings")]
    public float fadeOutDuration = 0.45f;
    public float fadeInDuration = 0.5f;

    private bool isTransitionActive = false;
    private bool isWaitingForPlayerConfirm = false;
    private Coroutine activeTransitionRoutine;

    public bool IsOpen => isTransitionActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUIConstructed();
        ResetToHiddenState();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        if (!isTransitionActive)
        {
            ResetToHiddenState();
        }
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ResetToHiddenState();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        EnsureUIConstructed();
        if (!isTransitionActive)
        {
            ResetToHiddenState();
        }
    }

    private void Update()
    {
        // Support pressing Space, Enter, E, ESC, or clicking the button to confirm morning report and start day
        if (isWaitingForPlayerConfirm)
        {
            if (Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.Return) || 
                Input.GetKeyDown(KeyCode.KeypadEnter) || 
                Input.GetKeyDown(KeyCode.E) || 
                Input.GetKeyDown(KeyCode.Escape))
            {
                UIEscapeManager.ConsumeEscape();
                OnStartNewDayClicked();
                return;
            }

            // Fallback direct mouse click detection on the Start New Day Button
            if (Input.GetMouseButtonDown(0))
            {
                if (startNewDayButton != null)
                {
                    if (_cachedStartNewDayBtnRT == null) _cachedStartNewDayBtnRT = startNewDayButton.GetComponent<RectTransform>();
                    if (_cachedStartNewDayBtnRT != null && RectTransformUtility.RectangleContainsScreenPoint(_cachedStartNewDayBtnRT, Input.mousePosition, null))
                    {
                        OnStartNewDayClicked();
                        return;
                    }
                }
            }
        }
    }

    public void ResetToHiddenState()
    {
        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
            activeTransitionRoutine = null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        if (morningReportModal != null)
        {
            morningReportModal.SetActive(false);
        }
        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.blocksRaycasts = false;
        }
        isTransitionActive = false;
        isWaitingForPlayerConfirm = false;
    }

    /// <summary>
    /// Initiates the Day Transition sequence.
    /// Fades out the screen, executes the day advance callback, gathers farm & world reports,
    /// and displays the Morning Report modal for player confirmation.
    /// </summary>
    public void PlayTransition(int oldDay, System.Action onDayAdvancedCallback = null)
    {
        if (isTransitionActive) return;

        UIEscapeManager.CloseAllModals(includeInventory: true);
        UIEscapeManager.NotifyModalOpened("DayTransition");

        EnsureUIConstructed();

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
        }

        activeTransitionRoutine = StartCoroutine(TransitionSequenceRoutine(oldDay, onDayAdvancedCallback));
    }

    private IEnumerator TransitionSequenceRoutine(int oldDay, System.Action onDayAdvancedCallback)
    {
        isTransitionActive = true;
        isWaitingForPlayerConfirm = false;

        if (morningReportModal != null) morningReportModal.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.blocksRaycasts = true;

        // 1. Fade to Black (screen gradually darkens)
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            }
            yield return null;
        }
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;

        // 2. Advance the Day & Update All Systems (FarmPlots, Merchant, DayManager)
        yield return new WaitForSecondsRealtime(0.15f);
        onDayAdvancedCallback?.Invoke();
        yield return new WaitForSecondsRealtime(0.1f);

        // 3. Compile Morning Briefing Data
        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : (oldDay + 1);
        PopulateReportData(currentDay);

        // 4. Open Morning Report Modal with smooth fade in
        if (morningReportModal != null)
        {
            morningReportModal.SetActive(true);
            morningReportModal.transform.localScale = Vector3.one * 0.95f;
            morningReportModal.transform.SetAsLastSibling();
        }

        elapsed = 0f;
        float modalFadeDuration = 0.3f;
        while (elapsed < modalFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / modalFadeDuration);
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = t;
            }
            if (morningReportModal != null)
            {
                morningReportModal.transform.localScale = Vector3.Lerp(Vector3.one * 0.95f, Vector3.one, Mathf.SmoothStep(0f, 1f, t));
            }
            yield return null;
        }

        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 1f;
            modalCanvasGroup.interactable = true;
            modalCanvasGroup.blocksRaycasts = true;
        }
        if (morningReportModal != null)
        {
            morningReportModal.transform.localScale = Vector3.one;
            morningReportModal.transform.SetAsLastSibling();
        }
        if (startNewDayButton != null)
        {
            startNewDayButton.interactable = true;
            startNewDayButton.transform.SetAsLastSibling();
        }

        isWaitingForPlayerConfirm = true;
    }

    private void PopulateReportData(int newDay)
    {
        // Title
        if (headerTitleText != null)
        {
            headerTitleText.text = $"[ DAWN - DAY {newDay:D2} ]";
        }
        if (headerSubtitleText != null)
        {
            headerSubtitleText.text = "A peaceful night has passed. New changes have arrived across the farm and dungeon.";
        }

        // 1. Farm
        if (farmSectionTitleText != null)
        {
            farmSectionTitleText.text = "[ TODAY'S FARM REPORT ]";
        }

        FarmPlot[] plots = FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int readyCount = 0;
        int plantedCount = 0;
        int emptyCount = 0;
        Dictionary<string, int> readyCropsSummary = new Dictionary<string, int>();
        List<string> plantedDetails = new List<string>();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null) continue;

            if (plots[i].currentState == FarmPlot.State.Ready)
            {
                readyCount++;
                string cropName = (plots[i].currentCrop != null && !string.IsNullOrEmpty(plots[i].currentCrop.cropName))
                    ? plots[i].currentCrop.cropName
                    : "Produce";

                if (readyCropsSummary.ContainsKey(cropName))
                    readyCropsSummary[cropName]++;
                else
                    readyCropsSummary[cropName] = 1;
            }
            else if (plots[i].currentState == FarmPlot.State.Planted && plots[i].currentCrop != null)
            {
                plantedCount++;
                string cName = !string.IsNullOrEmpty(plots[i].currentCrop.cropName) ? plots[i].currentCrop.cropName : "Crop";
                int totalStages = (plots[i].currentCrop.growthStages != null && plots[i].currentCrop.growthStages.Length > 0)
                    ? plots[i].currentCrop.growthStages.Length
                    : 1;
                int stageNum = plots[i].currentStage + 1;
                int daysLeftInStage = Mathf.Max(1, plots[i].currentCrop.growthTime) - plots[i].daysInCurrentStage;
                plantedDetails.Add($"{cName} (Stage {stageNum}/{totalStages}, ~{daysLeftInStage} days remaining)");
            }
            else if (plots[i].currentState == FarmPlot.State.Planted)
            {
                plantedCount++;
            }
            else
            {
                emptyCount++;
            }
        }

        System.Text.StringBuilder farmSb = new System.Text.StringBuilder();
        if (readyCount > 0)
        {
            List<string> cropDetails = new List<string>();
            foreach (var kvp in readyCropsSummary)
            {
                cropDetails.Add($"{kvp.Key} x{kvp.Value}");
            }
            string detailStr = string.Join(", ", cropDetails);
            farmSb.AppendLine($"* <color=#1E6B3A><b>{readyCount} crop plots</b> are fully ripe and ready to harvest!</color> ({detailStr})");
        }
        else
        {
            farmSb.AppendLine("* <color=#6D5B4F>No crops have ripened this morning.</color>");
        }

        if (plantedCount > 0)
        {
            if (plantedDetails.Count > 0 && plantedDetails.Count <= 3)
            {
                farmSb.AppendLine($"* <color=#7A4E00><b>{plantedCount} planted plots</b></color> growing: {string.Join(", ", plantedDetails)}.");
            }
            else
            {
                farmSb.AppendLine($"* <color=#7A4E00><b>{plantedCount} planted plots</b></color> are steadily progressing through their stages.");
            }
        }

        if (emptyCount > 0)
        {
            farmSb.AppendLine($"* <color=#5C4A3D><b>{emptyCount} empty tilled plots</b></color> remain. Sow new seeds to expand your harvest.");
        }

        if (plots.Length == 0)
        {
            farmSb.AppendLine("* <color=#5C4A3D>No farm plots detected in this area.</color>");
        }

        if (farmContentText != null)
        {
            farmContentText.text = farmSb.ToString().TrimEnd();
        }

        // 2. World & Dungeon News
        if (worldSectionTitleText != null)
        {
            worldSectionTitleText.text = "[ DUNGEON & WORLD NEWS ]";
        }

        System.Text.StringBuilder worldSb = new System.Text.StringBuilder();

        // Check Bosses on each floor
        if (ProgressionManager.Instance != null)
        {
            int highestUnlocked = Mathf.Max(1, ProgressionManager.Instance.highestUnlockedFloor);
            bool foundBossNews = false;

            for (int f = 1; f <= highestUnlocked; f++)
            {
                if (ProgressionManager.Instance.IsBossDefeated(f))
                {
                    if (ProgressionManager.Instance.IsBossAvailable(f))
                    {
                        worldSb.AppendLine($"* <color=#B71C1C><b>[FLOOR {f} BOSS]</b> has respawned! The challenge portal is reopened.</color>");
                        foundBossNews = true;
                    }
                    else
                    {
                        int daysLeft = ProgressionManager.Instance.GetDaysUntilBossReset(f);
                        worldSb.AppendLine($"* <color=#546E7A><b>[FLOOR {f} BOSS]</b> is regenerating (<b>{daysLeft} days</b> remaining).</color>");
                        foundBossNews = true;
                    }
                }
            }

            if (!foundBossNews)
            {
                worldSb.AppendLine("* <color=#00695C>Floor 1 of the Dungeon is open and awaits daring adventurers.</color>");
            }
        }
        else
        {
            worldSb.AppendLine("* <color=#00695C>The Dungeon is fully prepared for your next expedition.</color>");
        }

        // Check Merchant on each floor (Only notify if merchant was previously encountered on that floor)
        MerchantEvent.AdvanceCalendarStatus(newDay);
        List<MerchantEvent.FloorMerchantStatus> discoveredMerchants = MerchantEvent.GetAllDiscoveredMerchants();

        if (discoveredMerchants != null && discoveredMerchants.Count > 0)
        {
            discoveredMerchants.Sort((a, b) => a.floorNumber.CompareTo(b.floorNumber));
            for (int i = 0; i < discoveredMerchants.Count; i++)
            {
                var m = discoveredMerchants[i];
                if (m.remainingDays > 0)
                {
                    worldSb.AppendLine($"* <color=#006064><b>[WANDERING MERCHANT]</b> is on <b>Floor {m.floorNumber}</b> (<b>{m.remainingDays} days</b> left on visit).</color>");
                }
                else if (m.cooldownDays > 0)
                {
                    worldSb.AppendLine($"* <color=#6D5B4F><b>[WANDERING MERCHANT]</b> on <b>Floor {m.floorNumber}</b> is restocking goods (Returns in <b>{m.cooldownDays} days</b>).</color>");
                }
            }
        }
        // If merchant has never been discovered on any floor: DO NOT show merchant notification!

        if (worldContentText != null)
        {
            worldContentText.text = worldSb.ToString().TrimEnd();
        }
    }

    public void OnStartNewDayClicked()
    {
        if (!isWaitingForPlayerConfirm) return;
        StartCoroutine(DismissReportAndFadeInRoutine());
    }

    private IEnumerator DismissReportAndFadeInRoutine()
    {
        isWaitingForPlayerConfirm = false;

        // 1. Hide Report Modal with quick fade
        float elapsed = 0f;
        float modalFadeDuration = 0.2f;
        while (elapsed < modalFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / modalFadeDuration);
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 1f - t;
            }
            yield return null;
        }

        if (morningReportModal != null) morningReportModal.SetActive(false);
        if (modalCanvasGroup != null) modalCanvasGroup.alpha = 0f;

        // 2. Fade in screen (screen gradually brightens)
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            }
            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        isTransitionActive = false;
        UIEscapeManager.NotifyModalClosed("DayTransition");
        Debug.Log("[DayTransitionUI] New day started successfully!");
    }

    #region Scene UI Binding
    public void EnsureUIConstructed()
    {
        AutoBindSceneReferences();
    }

    private void AutoBindSceneReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] != null && canvases[i].name == "DayTransitionCanvas")
                    {
                        targetCanvas = canvases[i];
                        break;
                    }
                }
            }
        }

        if (targetCanvas != null)
        {
            if (fadeCanvasGroup == null || fadeOverlayImage == null)
            {
                Transform fadeT = targetCanvas.transform.Find("DayTransition_FadeOverlay");
                if (fadeT != null)
                {
                    fadeCanvasGroup = fadeT.GetComponent<CanvasGroup>();
                    fadeOverlayImage = fadeT.GetComponent<Image>();
                }
            }

            if (morningReportModal == null)
            {
                Transform modalT = targetCanvas.transform.Find("MorningReportModal");
                if (modalT != null)
                {
                    morningReportModal = modalT.gameObject;
                }
            }
        }

        if (morningReportModal != null)
        {
            Transform modalT = morningReportModal.transform;
            if (modalCanvasGroup == null) modalCanvasGroup = morningReportModal.GetComponent<CanvasGroup>();
            if (headerTitleText == null) headerTitleText = modalT.Find("HeaderPanel/TitleText")?.GetComponent<TextMeshProUGUI>();
            if (headerSubtitleText == null) headerSubtitleText = modalT.Find("HeaderPanel/SubtitleText")?.GetComponent<TextMeshProUGUI>();
            if (farmSectionTitleText == null) farmSectionTitleText = modalT.Find("ContentContainer/FarmSectionBox/SectionTitle")?.GetComponent<TextMeshProUGUI>();
            if (farmContentText == null) farmContentText = modalT.Find("ContentContainer/FarmSectionBox/SectionBody")?.GetComponent<TextMeshProUGUI>();
            if (worldSectionTitleText == null) worldSectionTitleText = modalT.Find("ContentContainer/WorldSectionBox/SectionTitle")?.GetComponent<TextMeshProUGUI>();
            if (worldContentText == null) worldContentText = modalT.Find("ContentContainer/WorldSectionBox/SectionBody")?.GetComponent<TextMeshProUGUI>();

            if (startNewDayButton == null)
            {
                Transform btnT = modalT.Find("StartNewDayButton");
                if (btnT != null)
                {
                    startNewDayButton = btnT.GetComponent<Button>();
                    startNewDayButtonText = btnT.Find("BtnText")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        if (startNewDayButton != null)
        {
            startNewDayButton.onClick.RemoveListener(OnStartNewDayClicked);
            startNewDayButton.onClick.AddListener(OnStartNewDayClicked);
        }
    }
    #endregion
}
