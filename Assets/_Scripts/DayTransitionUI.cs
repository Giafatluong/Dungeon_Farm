using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Day Transition & Morning Report UI (Báo Cáo Ban Mai) at Base.
/// Displays a smooth fade-to-black sequence, advances the farm day,
/// and presents a stylish morning report modal summarizing farm readiness and dungeon/merchant news.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
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
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        EnsureUIConstructed();
        ResetToHiddenState();
    }

    private void Update()
    {
        // Support pressing Space, Enter, E, or ESC to confirm morning report and start day
        if (isWaitingForPlayerConfirm)
        {
            if (Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.Return) || 
                Input.GetKeyDown(KeyCode.KeypadEnter) || 
                Input.GetKeyDown(KeyCode.E) || 
                Input.GetKeyDown(KeyCode.Escape))
            {
                OnStartNewDayClicked();
            }
        }
    }

    public void ResetToHiddenState()
    {
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

        // 1. Fade to Black (Màn hình tối dần)
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
            modalCanvasGroup.blocksRaycasts = true;
        }
        if (morningReportModal != null) morningReportModal.transform.localScale = Vector3.one;

        isWaitingForPlayerConfirm = true;
    }

    private void PopulateReportData(int newDay)
    {
        // Title
        if (headerTitleText != null)
        {
            headerTitleText.text = $"☀️ BÌNH MINH • NGÀY {newDay:D2}";
        }
        if (headerSubtitleText != null)
        {
            headerSubtitleText.text = "Một đêm yên bình đã qua. Nông trại và hầm ngục đã có những chuyển biến mới.";
        }

        // 1. Nông Trại
        if (farmSectionTitleText != null)
        {
            farmSectionTitleText.text = "🌾 NÔNG TRẠI HÔM NAY";
        }

        FarmPlot[] plots = FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int readyCount = 0;
        int plantedCount = 0;
        int emptyCount = 0;
        Dictionary<string, int> readyCropsSummary = new Dictionary<string, int>();

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] == null) continue;

            if (plots[i].currentState == FarmPlot.State.Ready)
            {
                readyCount++;
                string cropName = (plots[i].currentCrop != null && !string.IsNullOrEmpty(plots[i].currentCrop.cropName))
                    ? plots[i].currentCrop.cropName
                    : "Nông sản";

                if (readyCropsSummary.ContainsKey(cropName))
                    readyCropsSummary[cropName]++;
                else
                    readyCropsSummary[cropName] = 1;
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
            farmSb.AppendLine($"• <color=#81C784><b>{readyCount} ô nông sản</b> đã chín rộ sẵn sàng thu hoạch!</color> ({detailStr})");
        }
        else
        {
            farmSb.AppendLine("• <color=#B0BEC5>Chưa có nông sản nào chín trong sáng nay.</color>");
        }

        if (plantedCount > 0)
        {
            farmSb.AppendLine($"• <color=#FFF59D>{plantedCount} ô cây trồng</color> đang tiếp tục lớn khỏe mạnh.");
        }

        if (emptyCount > 0)
        {
            farmSb.AppendLine($"• Còn <color=#FFE082>{emptyCount} ô đất trống</color>, hãy gieo thêm hạt giống mới để mở rộng vụ mùa.");
        }

        if (plots.Length == 0)
        {
            farmSb.AppendLine("• Không tìm thấy ô đất nào trong khu vực.");
        }

        if (farmContentText != null)
        {
            farmContentText.text = farmSb.ToString().TrimEnd();
        }

        // 2. Thế Giới & Hầm Ngục
        if (worldSectionTitleText != null)
        {
            worldSectionTitleText.text = "🗺️ TIN TỨC HẦM NGỤC & THẾ GIỚI";
        }

        System.Text.StringBuilder worldSb = new System.Text.StringBuilder();

        // Kiểm tra Boss các tầng
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
                        worldSb.AppendLine($"• <color=#FF8A65><b>⚔️ Boss Tầng {f}</b> đã hồi sinh! Cánh cổng thử thách đã mở lại.</color>");
                        foundBossNews = true;
                    }
                    else
                    {
                        int daysLeft = ProgressionManager.Instance.GetDaysUntilBossReset(f);
                        worldSb.AppendLine($"• <color=#90A4AE>⏳ Boss Tầng {f} đang phục hồi sinh lực (còn <b>{daysLeft} ngày</b> nữa).</color>");
                        foundBossNews = true;
                    }
                }
            }

            if (!foundBossNews)
            {
                worldSb.AppendLine("• <color=#80CBC4>🏰 Tầng 1 Hầm ngục Dungeon đang mở cửa đón chờ nhà thám hiểm.</color>");
            }
        }
        else
        {
            worldSb.AppendLine("• <color=#80CBC4>🏰 Hầm ngục Dungeon đã sẵn sàng cho chuyến thám hiểm tiếp theo.</color>");
        }

        // Kiểm tra Merchant theo từng tầng (Chỉ thông báo nếu đã từng gặp thương nhân ở tầng đó)
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
                    worldSb.AppendLine($"• <color=#4DD0E1>🛒 Thương nhân đang ở <b>Tầng {m.floorNumber}</b> (Còn <b>{m.remainingDays} ngày</b> ghé thăm).</color>");
                }
                else if (m.cooldownDays > 0)
                {
                    worldSb.AppendLine($"• <color=#B0BEC5>🛒 Thương nhân ở <b>Tầng {m.floorNumber}</b> đang đi nhập hàng mới (Trở lại sau <b>{m.cooldownDays} ngày</b>).</color>");
                }
            }
        }
        // Nếu chưa gặp thương nhân ở bất kỳ tầng nào: KHÔNG hiển thị thông báo thương nhân!

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

        // 2. Fade in screen (Màn hình sáng dần trở lại)
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
        Debug.Log("[DayTransitionUI] New day started successfully!");
    }

    #region Procedural UI Construction
    public void EnsureUIConstructed()
    {
        if (morningReportModal != null && fadeCanvasGroup != null) return;

        // Find or create Canvas
        if (targetCanvas == null)
        {
            targetCanvas = DungeonUIAssetHelper.GetMainScreenCanvas();
            if (targetCanvas == null)
            {
                GameObject canvasGO = new GameObject("DayTransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                targetCanvas = canvasGO.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 999; // Always on top

                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        // 1. Black Fade Overlay
        if (fadeOverlayImage == null)
        {
            GameObject overlayGO = new GameObject("DayTransition_FadeOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            overlayGO.transform.SetParent(targetCanvas.transform, false);
            overlayGO.transform.SetAsLastSibling();

            RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;
            overlayRT.anchoredPosition = Vector2.zero;

            fadeOverlayImage = overlayGO.GetComponent<Image>();
            fadeOverlayImage.color = new Color(0.02f, 0.03f, 0.05f, 1f);

            fadeCanvasGroup = overlayGO.GetComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // 2. Morning Report Modal Dialog
        if (morningReportModal == null)
        {
            morningReportModal = new GameObject("MorningReportModal", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            morningReportModal.transform.SetParent(targetCanvas.transform, false);
            morningReportModal.transform.SetAsLastSibling();

            RectTransform modalRT = morningReportModal.GetComponent<RectTransform>();
            modalRT.anchorMin = new Vector2(0.5f, 0.5f);
            modalRT.anchorMax = new Vector2(0.5f, 0.5f);
            modalRT.pivot = new Vector2(0.5f, 0.5f);
            modalRT.sizeDelta = new Vector2(740, 560);
            modalRT.anchoredPosition = Vector2.zero;

            Image modalBg = morningReportModal.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(modalBg, new Color(0.11f, 0.13f, 0.18f, 0.98f));

            modalCanvasGroup = morningReportModal.GetComponent<CanvasGroup>();
            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.blocksRaycasts = false;

            // Header Container
            GameObject headerGO = new GameObject("HeaderPanel", typeof(RectTransform));
            headerGO.transform.SetParent(morningReportModal.transform, false);
            RectTransform headerRT = headerGO.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.sizeDelta = new Vector2(-40, 80);
            headerRT.anchoredPosition = new Vector2(0, -20);

            // Title
            GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(headerRT, false);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.5f);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.sizeDelta = Vector2.zero;
            titleRT.anchoredPosition = new Vector2(0, 5);

            headerTitleText = titleGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(headerTitleText);
            headerTitleText.fontSize = 28;
            headerTitleText.fontStyle = FontStyles.Bold;
            headerTitleText.alignment = TextAlignmentOptions.Center;
            headerTitleText.color = new Color(1f, 0.84f, 0.31f); // Warm Morning Gold
            headerTitleText.text = "☀️ BÌNH MINH • NGÀY 01";

            // Subtitle
            GameObject subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(headerRT, false);
            RectTransform subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0, 0);
            subRT.anchorMax = new Vector2(1, 0.5f);
            subRT.sizeDelta = Vector2.zero;
            subRT.anchoredPosition = Vector2.zero;

            headerSubtitleText = subGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(headerSubtitleText);
            headerSubtitleText.fontSize = 15;
            headerSubtitleText.alignment = TextAlignmentOptions.Center;
            headerSubtitleText.color = new Color(0.7f, 0.76f, 0.84f);
            headerSubtitleText.text = "Một đêm yên bình đã qua. Nông trại và thế giới bên ngoài đã có nhiều thay đổi.";

            // Divider Line
            GameObject divGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divGO.transform.SetParent(morningReportModal.transform, false);
            RectTransform divRT = divGO.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0.08f, 1);
            divRT.anchorMax = new Vector2(0.92f, 1);
            divRT.sizeDelta = new Vector2(0, 2);
            divRT.anchoredPosition = new Vector2(0, -106);
            Image divImg = divGO.GetComponent<Image>();
            divImg.color = new Color(0.7f, 0.55f, 0.22f, 0.6f);

            // 3. Content Sections Container
            GameObject contentGO = new GameObject("ContentContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGO.transform.SetParent(morningReportModal.transform, false);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0.06f, 0.16f);
            contentRT.anchorMax = new Vector2(0.94f, 0.79f);
            contentRT.sizeDelta = Vector2.zero;
            contentRT.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup contentVLG = contentGO.GetComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 18f;
            contentVLG.childControlHeight = true;
            contentVLG.childControlWidth = true;
            contentVLG.childForceExpandHeight = false;
            contentVLG.childForceExpandWidth = true;

            // Section 1: Farm Box
            GameObject farmBox = CreateSectionCard(contentGO.transform, "FarmSectionBox", out farmSectionTitleText, out farmContentText);

            // Section 2: World / Dungeon News Box
            GameObject worldBox = CreateSectionCard(contentGO.transform, "WorldSectionBox", out worldSectionTitleText, out worldContentText);

            // 4. Start New Day Button
            GameObject btnGO = new GameObject("StartNewDayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(morningReportModal.transform, false);
            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0);
            btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.sizeDelta = new Vector2(340, 52);
            btnRT.anchoredPosition = new Vector2(0, 24);

            Image btnImg = btnGO.GetComponent<Image>();
            startNewDayButton = btnGO.GetComponent<Button>();
            DungeonUIAssetHelper.StyleButton(startNewDayButton, btnImg, new Color(0.85f, 0.45f, 0.12f, 1f), new Color(1f, 0.60f, 0.25f, 1f));
            startNewDayButton.onClick.AddListener(OnStartNewDayClicked);

            GameObject btnTextGO = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            startNewDayButtonText = btnTextGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(startNewDayButtonText);
            startNewDayButtonText.fontSize = 19;
            startNewDayButtonText.fontStyle = FontStyles.Bold;
            startNewDayButtonText.alignment = TextAlignmentOptions.Center;
            startNewDayButtonText.color = Color.white;
            startNewDayButtonText.text = "☀️ BẮT ĐẦU NGÀY MỚI  (Space)";

            morningReportModal.SetActive(false);
        }
    }

    private GameObject CreateSectionCard(Transform parent, string cardName, out TextMeshProUGUI titleText, out TextMeshProUGUI bodyText)
    {
        GameObject cardGO = new GameObject(cardName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardGO.transform.SetParent(parent, false);

        Image cardBg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(cardBg, new Color(0.07f, 0.08f, 0.12f, 0.90f));

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;

        // Title
        GameObject tGO = new GameObject("SectionTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        tGO.transform.SetParent(cardGO.transform, false);
        titleText = tGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.96f, 0.77f, 0.35f);

        // Body
        GameObject bGO = new GameObject("SectionBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        bGO.transform.SetParent(cardGO.transform, false);
        bodyText = bGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(bodyText);
        bodyText.fontSize = 14;
        bodyText.lineSpacing = 6f;
        bodyText.color = new Color(0.9f, 0.92f, 0.96f);

        return cardGO;
    }
    #endregion
}
