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
    public GameObject overlayCanvasGO;
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
    private float nextWaveManagerSearchTime = 0f;
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
            if (Time.time < nextWaveManagerSearchTime) return;
            nextWaveManagerSearchTime = Time.time + 0.5f;

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
            keyPrompt: "Bấm [TIẾP TỤC] (Phím D hoặc Space)"
        );

        DungeonEntranceArrowUI arrowUI = DungeonEntranceArrowUI.Instance != null ? DungeonEntranceArrowUI.Instance : FindFirstObjectByType<DungeonEntranceArrowUI>();
        if (arrowUI != null && arrowUI.arrowButton != null)
        {
            PointToTarget(arrowUI.arrowButton.GetComponent<RectTransform>(), "BẤM VÀO ĐÂY");
        }
    }

    private IEnumerator ShowRewardStageGuideDelayed()
    {
        yield return new WaitForSeconds(0.4f);

        int stageNum = waveManager != null ? waveManager.GetCurrentWaveIndex() + 1 : 2;
        int totalStages = waveManager != null ? waveManager.GetTotalStages() : 5;

        ShowBanner(
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: RƯƠNG KHO BÁU" : "ẢI KHO BÁU",
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
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: SỰ KIỆN HẦM NGỤC" : "SỰ KIỆN HẦM NGỤC",
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
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: THƯƠNG NHÂN DU MỤC" : "THƯƠNG NHÂN DU MỤC",
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
            stageBadge: isTutorialFloor ? $"ẢI {stageNum}/{totalStages}: LỬA TRẠI NGHỈ CHÂN" : "TRẠI NGHỈ CHÂN",
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
    public bool BindSceneReferences()
    {
        if (overlayCanvasGO == null)
        {
            overlayCanvasGO = GameObject.Find("[DungeonTutorialCanvas]");
            if (overlayCanvasGO == null)
            {
                Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == "[DungeonTutorialCanvas]")
                    {
                        overlayCanvasGO = all[i].gameObject;
                        break;
                    }
                }
            }
        }
        if (overlayCanvasGO == null) return false;

        overlayCanvas = overlayCanvasGO.GetComponent<Canvas>();
        Transform root = overlayCanvasGO.transform;
        bannerGO = root.Find("DungeonTutorialBanner")?.gameObject;
        if (bannerGO != null)
        {
            bannerRT = bannerGO.GetComponent<RectTransform>();
            Transform plate = bannerGO.transform.Find("DarkPlate");
            Transform searchRoot = plate != null ? plate : bannerGO.transform;
            stageBadgeText = searchRoot.Find("Badge")?.GetComponent<TextMeshProUGUI>();
            titleText = searchRoot.Find("Title")?.GetComponent<TextMeshProUGUI>();
            instructionText = searchRoot.Find("Instruction")?.GetComponent<TextMeshProUGUI>();
            keyPromptText = searchRoot.Find("KeyPrompt")?.GetComponent<TextMeshProUGUI>();
            Button cBtn = searchRoot.Find("CloseButton")?.GetComponent<Button>();
            if (cBtn != null)
            {
                cBtn.onClick.RemoveListener(HideBanner);
                cBtn.onClick.AddListener(HideBanner);
            }
        }
        pointerGO = root.Find("TutorialPointer")?.gameObject;
        if (pointerGO != null)
        {
            pointerRT = pointerGO.GetComponent<RectTransform>();
            pointerBadgeText = pointerGO.transform.Find("Plate/BadgeText")?.GetComponent<TextMeshProUGUI>()
                               ?? pointerGO.transform.Find("BadgeText")?.GetComponent<TextMeshProUGUI>();
        }
        return true;
    }

    public void ConstructUI()
    {
        if (overlayCanvasGO != null || BindSceneReferences()) return;

        Debug.LogWarning("[DungeonTutorialController] [DungeonTutorialCanvas] not found in scene!");
        return;
    }

    private void LegacyConstructUI()
    {
        Debug.LogWarning("[DungeonTutorialController] [DungeonTutorialCanvas] not found in scene!");
        return;
    }

    #endregion
}
