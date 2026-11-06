using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Stage-by-Stage Guide Controller for Dungeon Encounters.
/// Handles comprehensive guidance in Floor 0 (Tutorial Floor) as well as
/// first-time encounter introductions (Events, Merchants, Camp, Rewards) across dungeon runs.
/// </summary>
public class DungeonTutorialController : MonoBehaviour
{
    public static DungeonTutorialController Instance { get; private set; }

    public const string EventIntroSeenKey = "Tut_Event_Intro_Seen";
    public const string MerchantIntroSeenKey = "Tut_Merchant_Intro_Seen";
    public const string CampIntroSeenKey = "Tut_Camp_Intro_Seen";
    public const string RewardIntroSeenKey = "Tut_Reward_Intro_Seen";

    [Header("UI Overlay Elements")]
    private GameObject overlayCanvasGO;
    private Canvas overlayCanvas;
    private GameObject bannerGO;
    private RectTransform bannerRT;
    private TextMeshProUGUI stageBadgeText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI keyPromptText;

    private GameObject pointerGO;
    private RectTransform pointerRT;
    private TextMeshProUGUI pointerBadgeText;
    private RectTransform currentTargetRT;

    private WaveManager waveManager;
    private int lastObservedWave = -999;
    private bool isTutorialFloor = false;
    private bool hasShownEntranceGuide = false;
    private Coroutine activeStageGuideRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitSceneLoadHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Dungeon")
        {
            EnsureInstance();
        }
    }

    public static DungeonTutorialController EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("[DungeonTutorialController]");
        Instance = go.AddComponent<DungeonTutorialController>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        waveManager = WaveManager.Instance != null ? WaveManager.Instance : FindFirstObjectByType<WaveManager>();

        RefreshTutorialFloorState();
        ConstructUI();

        // Listen to WaveManager entrance state
        if (waveManager != null && waveManager.isAtEntrance)
        {
            lastObservedWave = -1;
            if (isTutorialFloor && PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 0)
            {
                ShowEntranceRoomGuide();
            }
        }
    }

    private void RefreshTutorialFloorState()
    {
        int currentFloor = ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1;
        FloorData activeFloor = waveManager != null ? waveManager.GetActiveFloor() : null;
        int activeFloorNum = activeFloor != null ? activeFloor.floorNumber : currentFloor;

        // Tutorial floor is strictly Floor 0
        isTutorialFloor = (currentFloor == 0 || activeFloorNum == 0);
    }

    private void Update()
    {
        if (waveManager == null)
        {
            waveManager = WaveManager.Instance != null ? WaveManager.Instance : FindFirstObjectByType<WaveManager>();
            if (waveManager == null) return;
            RefreshTutorialFloorState();
        }

        // 1. If currently in stage walking transition, immediately hide any lingering banner/pointer
        if (waveManager.IsTransitioning)
        {
            if (bannerGO != null && bannerGO.activeSelf)
            {
                HideBanner();
                HidePointer();
            }
            return;
        }

        // 2. Safe entrance guide auto-trigger & tracking
        if (waveManager.isAtEntrance)
        {
            if (!hasShownEntranceGuide && isTutorialFloor && PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 0)
            {
                ShowEntranceRoomGuide();
            }
        }
        else
        {
            hasShownEntranceGuide = false;
        }

        // 3. Track wave changes
        int currentWaveIdx = waveManager.GetCurrentWaveIndex();
        if (currentWaveIdx != lastObservedWave)
        {
            lastObservedWave = currentWaveIdx;
            OnWaveChanged(currentWaveIdx);
        }

        // 4. Update bouncing pointer if any target is set
        UpdatePointerPosition();
    }

    private void OnWaveChanged(int waveIndex)
    {
        RefreshTutorialFloorState();
        HideBanner();
        HidePointer();

        if (waveIndex < 0)
        {
            if (waveManager != null && waveManager.isAtEntrance && isTutorialFloor && PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 0)
            {
                ShowEntranceRoomGuide();
            }
            return;
        }

        WaveData wave = waveManager != null ? waveManager.CurrentWave : null;
        if (wave == null) return;

        switch (wave.waveType)
        {
            case WaveData.WaveType.Combat:
            case WaveData.WaveType.Boss:
                // Combat guidance is handled by CombatTutorialGuide exclusively
                HideBanner();
                HidePointer();
                break;

            case WaveData.WaveType.Reward:
                if (isTutorialFloor || PlayerPrefs.GetInt(RewardIntroSeenKey, 0) == 0)
                {
                    activeStageGuideRoutine = StartCoroutine(ShowRewardStageGuideDelayed());
                }
                break;

            case WaveData.WaveType.Event:
                if (isTutorialFloor || PlayerPrefs.GetInt(EventIntroSeenKey, 0) == 0)
                {
                    activeStageGuideRoutine = StartCoroutine(ShowEventStageGuideDelayed());
                }
                break;

            case WaveData.WaveType.Merchant:
                if (isTutorialFloor || PlayerPrefs.GetInt(MerchantIntroSeenKey, 0) == 0)
                {
                    activeStageGuideRoutine = StartCoroutine(ShowMerchantStageGuideDelayed());
                }
                break;

            case WaveData.WaveType.Camp:
                if (isTutorialFloor || PlayerPrefs.GetInt(CampIntroSeenKey, 0) == 0)
                {
                    activeStageGuideRoutine = StartCoroutine(ShowCampStageGuideDelayed());
                }
                break;

            default:
                HideBanner();
                HidePointer();
                break;
        }
    }

    #region Guidance Displays
    public void ShowEntranceRoomGuide()
    {
        RefreshTutorialFloorState();
        if (!isTutorialFloor || PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 1)
        {
            return;
        }

        hasShownEntranceGuide = true;

        // Ensure CombatTutorialGuide is completely hidden while at Entrance
        if (CombatTutorialGuide.Instance != null && CombatTutorialGuide.Instance.IsTutorialActive)
        {
            return;
        }

        int totalStages = waveManager != null ? waveManager.GetTotalStages() : 5;
        ShowBanner(
            stageBadge: isTutorialFloor ? $"ẢI 0/{totalStages}: PHÒNG CHỜ AN TOÀN" : "PHÒNG CHỜ AN TOÀN",
            title: "CHUẨN BỊ THUỘC TÍNH TRƯỚC KHI CHIẾN ĐẤU",
            instruction: "Chào mừng đến Hầm Ngục! Đây là phòng an toàn: Hãy mở túi đồ <color=#FFEA30><b>[B]</b></color> kiểm tra 6 thuộc tính (HP, ATK, DEF, Speed, 3 AP, Độ No). Mỗi khi qua ải mới sẽ tốn 12 Độ No! Bấm nút mũi tên hoặc [Space] để bắt đầu chiến đấu.",
            keyPrompt: "Bấm [▶ TIẾP TỤC] (Phím D hoặc Space)"
        );

        DungeonEntranceArrowUI arrowUI = DungeonEntranceArrowUI.Instance != null ? DungeonEntranceArrowUI.Instance : FindFirstObjectByType<DungeonEntranceArrowUI>();
        if (arrowUI != null && arrowUI.arrowButton != null)
        {
            PointToTarget(arrowUI.arrowButton.GetComponent<RectTransform>(), "TIẾP TỤC [▶]");
        }
    }

    private IEnumerator ShowRewardStageGuideDelayed()
    {
        yield return new WaitForSeconds(0.4f);

        int stageNum = waveManager != null ? waveManager.GetCurrentWaveIndex() + 1 : 2;
        int totalStages = waveManager != null ? waveManager.GetTotalStages() : 5;

        ShowBanner(
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: RƯƠNG KHO BÁU (REWARD ROOM)" : "ẢI KHO BÁU (REWARD)",
            title: "THU NHẶT CHIẾN LỢI PHẨM VÀ HẠT GIỐNG MỚI",
            instruction: "Sau khi diệt quái, bạn sẽ gặp Rương Kho Báu chứa hạt giống quý và chiến lợi phẩm: Bấm nút <color=#FFEA30><b>[LẤY HẾT]</b></color> để gom toàn bộ đồ vào Ba lô, sau đó bấm <color=#4EFA8B><b>[TIẾP TỤC]</b></color> để sang ải tiếp theo.",
            keyPrompt: "Bấm [LẤY HẾT] -> Bấm [TIẾP TỤC]"
        );

        if (!isTutorialFloor)
        {
            PlayerPrefs.SetInt(RewardIntroSeenKey, 1);
            PlayerPrefs.Save();
        }

        RewardUI rewardUI = RewardUI.Instance != null ? RewardUI.Instance : FindFirstObjectByType<RewardUI>();
        if (rewardUI != null && rewardUI.lootAllButton != null)
        {
            PointToTarget(rewardUI.lootAllButton.GetComponent<RectTransform>(), "BẤM [LẤY HẾT]");
        }

        StartCoroutine(TrackRewardLootProgress(rewardUI));
    }

    private IEnumerator TrackRewardLootProgress(RewardUI rewardUI)
    {
        while (rewardUI != null && rewardUI.gameObject.activeInHierarchy)
        {
            if (rewardUI.lootAllButton != null && !rewardUI.lootAllButton.interactable)
            {
                if (rewardUI.continueButton != null)
                {
                    PointToTarget(rewardUI.continueButton.GetComponent<RectTransform>(), "BẤM [TIẾP TỤC]");
                }
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator ShowEventStageGuideDelayed()
    {
        yield return new WaitForSeconds(0.4f);

        int stageNum = waveManager != null ? waveManager.GetCurrentWaveIndex() + 1 : 3;
        int totalStages = waveManager != null ? waveManager.GetTotalStages() : 5;

        ShowBanner(
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: SỰ KIỆN HẦM NGỤC (EVENT ROOM)" : "SỰ KIỆN HẦM NGỤC (EVENT)",
            title: "SỰ KIỆN NGẪU NHIÊN: ĐÁNH ĐỔI RỦI RO & PHẦN THƯỞNG",
            instruction: "Sự kiện hầm ngục mang đến cơ hội đánh đổi: Dùng máu, độ no hoặc nông sản để đổi lấy buff chỉ số mạnh mẽ, hạt giống quý hoặc hồi phục. Hãy đọc kỹ chi phí / phần thưởng và nhấp chọn một phương án phù hợp!",
            keyPrompt: "Đọc chi phí & Nhấp chọn một phương án"
        );

        if (!isTutorialFloor)
        {
            PlayerPrefs.SetInt(EventIntroSeenKey, 1);
            PlayerPrefs.Save();
        }

        DungeonEventUI eventUI = DungeonEventUI.Instance != null ? DungeonEventUI.Instance : FindFirstObjectByType<DungeonEventUI>();
        if (eventUI != null && eventUI.choicesContainer != null)
        {
            PointToTarget(eventUI.choicesContainer, "CHỌN PHƯƠNG ÁN ĐÁNH ĐỔI");
        }

        StartCoroutine(TrackEventProgress(eventUI));
    }

    private IEnumerator TrackEventProgress(DungeonEventUI eventUI)
    {
        while (eventUI != null && eventUI.gameObject.activeInHierarchy)
        {
            if (eventUI.continueButton != null && eventUI.continueButton.gameObject.activeInHierarchy && eventUI.continueButton.interactable)
            {
                PointToTarget(eventUI.continueButton.GetComponent<RectTransform>(), "BẤM [TIẾP TỤC]");
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator ShowMerchantStageGuideDelayed()
    {
        yield return new WaitForSeconds(0.4f);

        int stageNum = waveManager != null ? waveManager.GetCurrentWaveIndex() + 1 : 4;
        int totalStages = waveManager != null ? waveManager.GetTotalStages() : 5;

        ShowBanner(
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: THƯƠNG NHÂN DU MỤC (MERCHANT ROOM)" : "THƯƠNG NHÂN DU MỤC (MERCHANT)",
            title: "TRAO ĐỔI HÀNG HÓA VỚI THƯƠNG NHÂN DU MỤC",
            instruction: "Thương nhân du mục không dùng tiền vàng: Bạn có thể đổi hạt giống cấp thấp lấy hạt giống cấp cao, đổi nông sản lấy thức ăn chế biến sẵn hoặc buff chiến đấu! Hãy bấm nút <color=#FFEA30><b>[ĐỔI]</b></color> món cần hoặc bấm <color=#4EFA8B><b>[TIẾP TỤC]</b></color> khi đã hoàn tất.",
            keyPrompt: "Bấm [ĐỔI] món đồ cần thiết hoặc [TIẾP TỤC]"
        );

        if (!isTutorialFloor)
        {
            PlayerPrefs.SetInt(MerchantIntroSeenKey, 1);
            PlayerPrefs.Save();
        }

        MerchantUI merchantUI = MerchantUI.Instance != null ? MerchantUI.Instance : FindFirstObjectByType<MerchantUI>();
        if (merchantUI != null && merchantUI.continueButton != null)
        {
            PointToTarget(merchantUI.continueButton.GetComponent<RectTransform>(), "BẤM [TIẾP TỤC]");
        }
    }

    private IEnumerator ShowCampStageGuideDelayed()
    {
        yield return new WaitForSeconds(0.4f);

        int stageNum = waveManager != null ? waveManager.GetCurrentWaveIndex() + 1 : 5;
        int totalStages = waveManager != null ? waveManager.GetTotalStages() : 5;

        ShowBanner(
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: LỬA TRẠI NGHỈ CHÂN (CAMP ROOM)" : "TRẠI NGHỈ CHÂN (CAMP)",
            title: "TRẠI NGHỈ CHÂN: ĂN UỐNG, HỒI MÁU & RÚT LUI",
            instruction: "Tại Trại: Ăn uống, Nấu ăn dã chiến, Tập luyện và Nghỉ ngơi (+25 HP). Cẩn thận: Mỗi hành động tăng Nguy cơ Phục kích!\n" +
                         "<color=#FFD700><b>Rút Lui An Toàn:</b></color> Bấm <color=#FFEA30><b>[RÚT LUI AN TOÀN]</b></color> để mang 100% quà về Căn Cứ. Thời gian ở Nông Trại sẽ trôi tương ứng để cây trồng lớn lên và chín rộ!",
            keyPrompt: "Nghỉ ngơi hồi máu / Bấm [RÚT LUI AN TOÀN] mang 100% quà về"
        );

        if (!isTutorialFloor)
        {
            PlayerPrefs.SetInt(CampIntroSeenKey, 1);
            PlayerPrefs.Save();
        }

        CampUI campUI = CampUI.Instance != null ? CampUI.Instance : FindFirstObjectByType<CampUI>();
        if (campUI != null)
        {
            if (campUI.returnHomeButton != null)
            {
                PointToTarget(campUI.returnHomeButton.GetComponent<RectTransform>(), "RÚT LUI AN TOÀN [100% QUÀ]");
            }
            else if (campUI.restButton != null)
            {
                PointToTarget(campUI.restButton.GetComponent<RectTransform>(), "HỒI MÁU [NGHỈ NGƠI]");
            }
        }
    }

    public void OnTutorialFloorCompleted()
    {
        PlayerPrefs.SetInt(TutorialManager.DungeonTutorialDoneKey, 1);
        PlayerPrefs.SetInt(TutorialManager.TutorialDoneKey, 1);
        PlayerPrefs.SetInt(RewardIntroSeenKey, 1);
        PlayerPrefs.SetInt(EventIntroSeenKey, 1);
        PlayerPrefs.SetInt(MerchantIntroSeenKey, 1);
        PlayerPrefs.SetInt(CampIntroSeenKey, 1);
        PlayerPrefs.Save();

        // Unlock Floor 1 permanently (and ensure Floor 2 is NOT unlocked by tutorial completion)
        if (ProgressionManager.Instance != null)
        {
            if (!ProgressionManager.Instance.HasClaimedFirstClearStats(1))
            {
                ProgressionManager.Instance.defeatedBossFloors?.Remove(1);
                ProgressionManager.Instance.highestUnlockedFloor = 1;
            }
            ProgressionManager.Instance.UnlockFloor(1);
            ProgressionManager.Instance.SaveFloorProgression();
        }

        HideBanner();
        HidePointer();
    }
    #endregion

    #region Pointer & Positioning
    public void PointToTarget(RectTransform target, string label)
    {
        currentTargetRT = target;
        if (pointerGO == null) return;

        if (target != null)
        {
            pointerGO.SetActive(true);
            if (pointerBadgeText != null) pointerBadgeText.text = label;
        }
        else
        {
            pointerGO.SetActive(false);
        }
    }

    public void HidePointer()
    {
        currentTargetRT = null;
        if (pointerGO != null) pointerGO.SetActive(false);
    }

    private void UpdatePointerPosition()
    {
        if (pointerGO == null || !pointerGO.activeSelf) return;

        if (currentTargetRT != null && currentTargetRT.gameObject.activeInHierarchy)
        {
            Vector3[] corners = new Vector3[4];
            currentTargetRT.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            float bounce = Mathf.Sin(Time.unscaledTime * 5f) * 6f;
            center.y += (currentTargetRT.rect.height * 0.5f) + 38f + bounce;

            pointerRT.position = center;
        }
        else
        {
            pointerGO.SetActive(false);
        }
    }
    #endregion

    #region Banner Controls
    public void ShowBanner(string stageBadge, string title, string instruction, string keyPrompt)
    {
        ConstructUI();
        if (bannerGO == null) return;

        bannerGO.SetActive(true);

        if (stageBadgeText != null) stageBadgeText.text = stageBadge;
        if (titleText != null) titleText.text = title;
        if (instructionText != null) instructionText.text = instruction;
        if (keyPromptText != null) keyPromptText.text = keyPrompt;
    }

    public void HideBanner()
    {
        if (activeStageGuideRoutine != null)
        {
            StopCoroutine(activeStageGuideRoutine);
            activeStageGuideRoutine = null;
        }
        if (bannerGO != null) bannerGO.SetActive(false);
    }
    #endregion

    #region Procedural UI Construction
    private void ConstructUI()
    {
        if (overlayCanvasGO != null) return;

        overlayCanvasGO = new GameObject("[DungeonTutorialCanvas]");
        overlayCanvas = overlayCanvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 24; // Above combat/camp/reward background, below modals

        CanvasScaler cs = overlayCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        overlayCanvasGO.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();

        // 1. Top Instruction Banner Card
        bannerGO = new GameObject("DungeonTutorialBanner", typeof(RectTransform), typeof(Image));
        bannerGO.transform.SetParent(overlayCanvasGO.transform, false);
        bannerRT = bannerGO.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0.5f, 1f);
        bannerRT.anchorMax = new Vector2(0.5f, 1f);
        bannerRT.pivot = new Vector2(0.5f, 1f);
        bannerRT.anchoredPosition = new Vector2(0, -85);
        bannerRT.sizeDelta = new Vector2(920, 185);

        Image bannerFrame = bannerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bannerFrame, new Color(0.20f, 0.70f, 0.50f, 1f)); // Vivid emerald frame

        // Inner solid dark plate for high contrast
        GameObject plate = new GameObject("DarkPlate", typeof(RectTransform), typeof(Image));
        plate.transform.SetParent(bannerGO.transform, false);
        RectTransform plRT = plate.GetComponent<RectTransform>();
        plRT.anchorMin = Vector2.zero;
        plRT.anchorMax = Vector2.one;
        plRT.offsetMin = new Vector2(5, 5);
        plRT.offsetMax = new Vector2(-5, -5);
        Image plImg = plate.GetComponent<Image>();
        plImg.color = new Color(0.05f, 0.07f, 0.09f, 0.98f); // Solid pitch dark

        // Close Button (Top-Right)
        GameObject closeBtnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(plate.transform, false);
        RectTransform cRT = closeBtnGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(-8, -6);
        cRT.sizeDelta = new Vector2(76, 24);
        Image cImg = closeBtnGO.GetComponent<Image>();
        cImg.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);
        Button cBtn = closeBtnGO.GetComponent<Button>();
        cBtn.onClick.AddListener(HideBanner);

        GameObject cTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        cTxtGO.transform.SetParent(closeBtnGO.transform, false);
        RectTransform ctRT = cTxtGO.GetComponent<RectTransform>();
        ctRT.anchorMin = Vector2.zero;
        ctRT.anchorMax = Vector2.one;
        ctRT.offsetMin = Vector2.zero;
        ctRT.offsetMax = Vector2.zero;
        TextMeshProUGUI cTmp = cTxtGO.GetComponent<TextMeshProUGUI>();
        cTmp.text = "[ Đóng ]";
        cTmp.fontSize = 12;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = new Color(0.95f, 0.95f, 0.95f);
        DungeonUIAssetHelper.ApplyFont(cTmp, withOutline: true);

        // Stage Badge
        GameObject badgeGO = new GameObject("Badge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeGO.transform.SetParent(plate.transform, false);
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 1f);
        bRT.anchorMax = new Vector2(0.5f, 1f);
        bRT.pivot = new Vector2(0.5f, 1f);
        bRT.anchoredPosition = new Vector2(0, -8);
        bRT.sizeDelta = new Vector2(860, 22);
        stageBadgeText = badgeGO.GetComponent<TextMeshProUGUI>();
        stageBadgeText.fontSize = 13.5f;
        stageBadgeText.fontStyle = FontStyles.Bold;
        stageBadgeText.alignment = TextAlignmentOptions.Center;
        stageBadgeText.color = new Color(0.35f, 1f, 0.65f); // Bright neon emerald
        DungeonUIAssetHelper.ApplyFont(stageBadgeText);

        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(plate.transform, false);
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.5f, 1f);
        tRT.anchorMax = new Vector2(0.5f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0, -30);
        tRT.sizeDelta = new Vector2(860, 26);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 16.5f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.92f, 0.30f); // Bright sunlight gold
        DungeonUIAssetHelper.ApplyFont(titleText);

        // Instruction Body (Auto-sizing enabled so multiple paragraphs never bleed outside)
        GameObject instGO = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
        instGO.transform.SetParent(plate.transform, false);
        RectTransform iRT = instGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.5f, 1f);
        iRT.anchorMax = new Vector2(0.5f, 1f);
        iRT.pivot = new Vector2(0.5f, 1f);
        iRT.anchoredPosition = new Vector2(0, -56);
        iRT.sizeDelta = new Vector2(860, 88);
        instructionText = instGO.GetComponent<TextMeshProUGUI>();
        instructionText.fontSize = 13.5f;
        instructionText.enableAutoSizing = true;
        instructionText.fontSizeMin = 11f;
        instructionText.fontSizeMax = 13.5f;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.color = Color.white; // Pure white for crystal clarity
        instructionText.lineSpacing = 0;
        DungeonUIAssetHelper.ApplyFont(instructionText);

        // Key Prompt Footer
        GameObject keyGO = new GameObject("KeyPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        keyGO.transform.SetParent(plate.transform, false);
        RectTransform kRT = keyGO.GetComponent<RectTransform>();
        kRT.anchorMin = new Vector2(0.5f, 0f);
        kRT.anchorMax = new Vector2(0.5f, 0f);
        kRT.pivot = new Vector2(0.5f, 0f);
        kRT.anchoredPosition = new Vector2(0, 8);
        kRT.sizeDelta = new Vector2(860, 20);
        keyPromptText = keyGO.GetComponent<TextMeshProUGUI>();
        keyPromptText.fontSize = 13f;
        keyPromptText.fontStyle = FontStyles.Bold;
        keyPromptText.alignment = TextAlignmentOptions.Center;
        keyPromptText.color = new Color(0.35f, 0.90f, 1f); // Bright electric cyan
        DungeonUIAssetHelper.ApplyFont(keyPromptText);

        bannerGO.SetActive(false);

        // 2. Bouncing Pointer Indicator
        pointerGO = new GameObject("TutorialPointer", typeof(RectTransform), typeof(Image));
        pointerGO.transform.SetParent(overlayCanvasGO.transform, false);
        pointerRT = pointerGO.GetComponent<RectTransform>();
        pointerRT.sizeDelta = new Vector2(250, 50);

        Image pFrame = pointerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(pFrame, new Color(1f, 0.82f, 0.20f, 1f)); // Bright golden frame

        GameObject pPlate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        pPlate.transform.SetParent(pointerGO.transform, false);
        RectTransform pplRT = pPlate.GetComponent<RectTransform>();
        pplRT.anchorMin = Vector2.zero;
        pplRT.anchorMax = Vector2.one;
        pplRT.offsetMin = new Vector2(3, 3);
        pplRT.offsetMax = new Vector2(-3, -3);
        Image pplImg = pPlate.GetComponent<Image>();
        pplImg.color = new Color(0.05f, 0.07f, 0.09f, 0.98f);

        GameObject arrowGO = new GameObject("ArrowIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(pointerGO.transform, false);
        RectTransform aRT = arrowGO.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.5f, 0f);
        aRT.anchorMax = new Vector2(0.5f, 0f);
        aRT.pivot = new Vector2(0.5f, 0f);
        aRT.anchoredPosition = new Vector2(0, -6);
        aRT.sizeDelta = new Vector2(30, 24);
        TextMeshProUGUI aTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        aTMP.fontSize = 18;
        aTMP.alignment = TextAlignmentOptions.Center;
        aTMP.text = "v";
        aTMP.color = new Color(1f, 0.90f, 0.20f);
        DungeonUIAssetHelper.ApplyFont(aTMP, withOutline: true);

        GameObject badgeTextGO = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTextGO.transform.SetParent(pPlate.transform, false);
        RectTransform btRT = badgeTextGO.GetComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.offsetMin = new Vector2(6, 4);
        btRT.offsetMax = new Vector2(-6, -4);
        pointerBadgeText = badgeTextGO.GetComponent<TextMeshProUGUI>();
        pointerBadgeText.fontSize = 13.5f;
        pointerBadgeText.fontStyle = FontStyles.Bold;
        pointerBadgeText.alignment = TextAlignmentOptions.Center;
        pointerBadgeText.color = new Color(1f, 0.95f, 0.30f);
        DungeonUIAssetHelper.ApplyFont(pointerBadgeText);

        pointerGO.SetActive(false);
    }
    #endregion
}
