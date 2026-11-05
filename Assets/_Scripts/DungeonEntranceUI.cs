using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Dungeon Entrance UI at Base.
/// Displays available dungeon floors in a vertical tower layout (Floor 1 at the bottom, higher floors above).
/// Players must defeat the boss of the lower floor before advancing to the next floor.
/// </summary>
public class DungeonEntranceUI : MonoBehaviour
{
    [SerializeField] private GameObject enterDungeonPanel;

    [Header("Floors Configuration")]
    [SerializeField] private int totalFloors = 10;
    [SerializeField] private FloorDisplayData[] customFloors;

    [System.Serializable]
    public class FloorDisplayData
    {
        public int floorNumber;
        public string floorName;
        public string bossName;
        public string subTitle;
        [TextArea(2, 3)]
        public string description;
    }

    private readonly List<FloorDisplayData> floorList = new List<FloorDisplayData>();
    private int selectedFloor = 1;

    // Scene & Runtime UI references
    [Header("Scene UI References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private Button enterButton;
    [SerializeField] private TextMeshProUGUI enterButtonText;
    [SerializeField] private Button closeButton;
    private readonly Dictionary<int, FloorCardUI> floorCards = new Dictionary<int, FloorCardUI>();
    private Coroutine feedbackFlashCoroutine;
    private GameObject blessingWarningModalGO;

    private class FloorCardUI
    {
        public GameObject rootGO;
        public Button button;
        public Image backgroundImage;
        public TextMeshProUGUI floorBadgeText;
        public TextMeshProUGUI floorNameText;
        public TextMeshProUGUI bossInfoText;
        public TextMeshProUGUI statusBadgeText;
        public Image statusBadgeBg;
        public int floorNumber;
    }

    public static DungeonEntranceUI Instance { get; private set; }
    public bool IsOpen => enterDungeonPanel != null && enterDungeonPanel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeFloorData();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        EnsureUIConstructed();
    }

    private void Update()
    {
        if (blessingWarningModalGO != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIEscapeManager.ConsumeEscape();
                CloseBlessingWarningModal();
            }
            return;
        }

        if (enterDungeonPanel != null && enterDungeonPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIEscapeManager.ConsumeEscape();
                Close();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                EnterSelectedFloor();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                ProgressionManager.Instance?.UnlockAllFloors();
                RefreshUI();
            }
#endif
        }
    }

    private void InitializeFloorData()
    {
        floorList.Clear();

        if (customFloors != null && customFloors.Length > 0)
        {
            floorList.AddRange(customFloors);
            totalFloors = Mathf.Max(totalFloors, floorList.Count);
            return;
        }

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 1,
            floorName = "TẦNG 1: HẦM NGỤC CỔ XƯA",
            bossName = "Vua Xương",
            subTitle = "8 Phòng Thử Thách — Nông Sản Khởi Đầu (Cà Rốt & Lúa Mì)",
            description = "Mục đích: Khởi đầu làm quen chiến đấu theo lượt và thu thập giống cây cơ bản (Cà Rốt, Lúa Mì). Quái vật đánh nhanh, ít giáp. Hãy nấu Súp Cà Rốt để tăng Tốc độ và hồi phục sinh lực!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 2,
            floorName = "TẦNG 2: ĐẦM LẦY QUÁI VẬT",
            bossName = "Slime Chúa Khổng Lồ",
            subTitle = "16 Phòng Bền Bỉ — Nông Sản Phòng Thủ (Ngô, Bắp Cải, Cà Chua, Dưa Chuột)",
            description = "Mục đích: Thử thách sức bền với 16 phòng. Quái vật bọc giáp (2 DEF) và tấn công mạnh (10-14 ATK). Bắt buộc phải chuẩn bị món ăn tăng Giáp (Ngô Nướng Bơ, Salad Bắp Cải) hoặc món tăng Công (Súp Cà Chua) mới có thể sống sót!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 3,
            floorName = "TẦNG 3: THÀNH CỔ QUỶ DỮ",
            bossName = "Kỵ Sĩ Bầu Trời Hắc Ám",
            subTitle = "18 Phòng Tối Thượng — Sát Thương Bùng Nổ & Ớt Hiểm (Chilli)",
            description = "Mục đích: Tử địa khắc nghiệt trước đây. Quái vật thiết giáp hạng nặng cực trâu (4 DEF, 14-17 ATK). Chỉ có thể vượt qua khi chuẩn bị các món ăn đại tiệc cao cấp (Ngô Cay, Canh Thập Cẩm Cay) và kết hợp phước lành thần thánh!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 4,
            floorName = "TẦNG 4: HANG ĐỘNG NHAM THẠCH",
            bossName = "Hỏa Dơi Ma Vương",
            subTitle = "12 Phòng Rực Lửa — Sát Thương Tốc Độ & Nham Thạch",
            description = "Mục đích: Hang động dung nham ngầm rực lửa. Đàn dơi lửa sở hữu Tốc độ vượt trội, luôn chiếm thế thượng phong ra đòn trước. Hãy ăn Súp Cà Rốt hoặc kích hoạt Chúc Phúc Tốc Độ để không bị dồn sát thương!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 5,
            floorName = "TẦNG 5: HẦM MỘ BĂNG GIÁ",
            bossName = "Tướng Băng Xương",
            subTitle = "14 Phòng Hàn Băng — Giáp Băng Kiên Cố & Cung Thủ Bắn Tỉa",
            description = "Mục đích: Khí lạnh thấu xương làm giảm nhuệ khí. Quân đoàn hộ vệ mang khiên băng cực dày kết hợp cùng Cung thủ bóng đêm rỉa máu từ xa. Cần chuẩn bị Canh Thập Cẩm Cay hoặc Súp Cà Chua để phá tan lớp giáp băng!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 6,
            floorName = "TẦNG 6: RỪNG NẤM ĐỘC",
            bossName = "Nấm Ma Độc Cổ Thụ",
            subTitle = "14 Phòng U Ám — Bào Tử Độc Tố & Nổ Cảm Tử",
            description = "Mục đích: Khu rừng nấm ngầm ngập tràn khí độc. Quái vật nấm có khả năng phát nổ cảm tử gây sát thương diện rộng. Hãy phân bổ 3 AP khéo léo để tiêu diệt chúng chớp nhoáng trước khi chúng phát nổ!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 7,
            floorName = "TẦNG 7: CẤM ĐỊA HUYẾT QUÁI",
            bossName = "Huyết Thử Vương",
            subTitle = "16 Phòng Đẫm Máu — Bầy Đàn Cắn Xé & Cấu Rỉa Liên Hoàn",
            description = "Mục đích: Ổ chuột quỷ biến dị khổng lồ với nanh vuốt tẩm độc. Chúng tấn công theo bầy đàn đông đảo và liên tục. Hãy chuẩn bị các món gia cố Giáp (Ngô Nướng Bơ, Salad Bắp Cải) để phòng thủ vững vàng!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 8,
            floorName = "TẦNG 8: TÀN TÍCH HƯ VÔ",
            bossName = "Thiện Xạ Hư Vô Cổ Đại",
            subTitle = "16 Phòng Cổ Đại — Bắn Tỉa Chí Mạng & Sát Thủ Bóng Đêm",
            description = "Mục đích: Đền thờ hư vô nơi các xạ thủ cổ đại canh gác. Những mũi tên bóng tối có tỉ lệ chí mạng cực cao. Người chơi cần kết hợp Giáp phòng ngự và Chúc phúc Thần Thánh để chống chịu những đòn đánh tử thần!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 9,
            floorName = "TẦNG 9: VỰC SÂU HẮC ÁM",
            bossName = "Tà Ma Hắc Thần",
            subTitle = "18 Phòng Vực Thẳm — Thiết Giáp Hủy Diệt & Tiêu Hao Cơn Đói",
            description = "Mục đích: Vực thẳm sâu nhất trước thềm Ngai Vàng. Quái vật mang sức mạnh hắc ám hung hãn, lượng máu khổng lồ. Hành trình dài 18 phòng tiêu tốn rất nhiều Độ No, bắt buộc phải mang theo nhiều lương thực dự trữ!"
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 10,
            floorName = "TẦNG 10: NGAI VÀNG VĨNH CỬU",
            bossName = "Chúa Tể Bóng Tối",
            subTitle = "20 Phòng Quyết Đấu — Đỉnh Cao Hầm Ngục & Hắc Huyết Tinh Thể",
            description = "Mục đích: Đại thử thách tối thượng của Hầm Ngục. Quân đoàn tinh nhuệ thiện chiến nhất bảo vệ Ngai Vàng. Đánh bại Chúa Tể Bóng Tối để đoạt lấy Hắc Huyết Tinh Thể, hoàn thành đại nghiệp và nhận vinh quang vĩnh cửu!"
        });
    }

    public void Open()
    {
        // In tutorial: bypass floor selection UI and directly enter Floor 0 ONLY if tutorial is actively at Dungeon Gate
        bool isTutorialActive = (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialCompleted && TutorialManager.Instance.CurrentStep == TutorialManager.TutorialStep.DungeonGate)
                             && PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 0;
        if (isTutorialActive)
        {
            EnterFloorDirectly(0);
            return;
        }

        UIEscapeManager.NotifyModalOpened("DungeonEntrance");
        EnsureUIConstructed();

        if (enterDungeonPanel != null)
        {
            enterDungeonPanel.SetActive(true);
        }

        // Default to highest unlocked floor or current floor
        if (ProgressionManager.Instance != null)
        {
            int candidate = ProgressionManager.Instance.currentFloor;
            if (candidate > 0 && candidate <= totalFloors && IsFloorUnlocked(candidate))
            {
                selectedFloor = candidate;
            }
            else
            {
                selectedFloor = Mathf.Clamp(ProgressionManager.Instance.highestUnlockedFloor, 1, totalFloors);
            }
        }
        else
        {
            selectedFloor = 1;
        }

        RefreshUI();
    }

    public void Close()
    {
        CloseBlessingWarningModal();

        if (enterDungeonPanel != null)
        {
            enterDungeonPanel.SetActive(false);
        }

        UIEscapeManager.NotifyModalClosed("DungeonEntrance");
    }

    /// <summary>
    /// Backward-compatible method called by existing Unity UI events.
    /// Enters dungeon at the currently selected floor.
    /// </summary>
    public void EnterDungeon()
    {
        EnterSelectedFloor();
    }

    // Cached Color Constants for zero-allocation UI refreshing
    private static readonly Color ColorCardSelected = new Color(1.0f, 0.78f, 0.20f, 1f);
    private static readonly Color ColorCardUnlocked = new Color(0.28f, 0.38f, 0.52f, 0.90f);
    private static readonly Color ColorCardLocked = new Color(0.20f, 0.22f, 0.26f, 0.55f);

    private static readonly Color ColorBadgeSelected = new Color(1.0f, 0.88f, 0.35f);
    private static readonly Color ColorBadgeUnlocked = new Color(0.70f, 0.85f, 1.0f);
    private static readonly Color ColorBadgeLocked = new Color(0.45f, 0.48f, 0.55f);

    private static readonly Color ColorNameUnlocked = new Color(0.92f, 0.94f, 0.98f);
    private static readonly Color ColorNameLocked = new Color(0.55f, 0.58f, 0.65f);

    private static readonly Color ColorBossSelected = new Color(1.0f, 0.85f, 0.50f);
    private static readonly Color ColorBossUnlocked = new Color(0.60f, 0.72f, 0.82f);
    private static readonly Color ColorBossLocked = new Color(0.40f, 0.42f, 0.48f);

    private static readonly Color ColorStatusDefeatedText = new Color(0.40f, 1.0f, 0.45f);
    private static readonly Color ColorStatusDefeatedBg = new Color(0.10f, 0.35f, 0.15f, 0.85f);
    private static readonly Color ColorStatusSelectedText = new Color(1.0f, 0.90f, 0.30f);
    private static readonly Color ColorStatusSelectedBg = new Color(0.40f, 0.28f, 0.05f, 0.95f);
    private static readonly Color ColorStatusUnlockedText = new Color(0.55f, 0.85f, 1.0f);
    private static readonly Color ColorStatusUnlockedBg = new Color(0.12f, 0.25f, 0.42f, 0.85f);
    private static readonly Color ColorStatusLockedText = new Color(0.65f, 0.65f, 0.65f);
    private static readonly Color ColorStatusLockedBg = new Color(0.18f, 0.18f, 0.20f, 0.85f);

    public void EnterFloorDirectly(int floor)
    {
        Close();
        TutorialManager.Instance?.NotifyDungeonEntered();

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentFloor = floor;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.Log("Scene Transition Manager is NULL, fallback loading Dungeon directly");
            ProgressionManager.Instance?.StartRun(floor);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon");
            return;
        }

        SceneTransitionManager.Instance.LoadDungeon(floor);
    }

    public void EnterSelectedFloor()
    {
        // First-time dungeon check: If player has not completed Dungeon Tutorial, force Tutorial Floor (Floor 0)
        bool isDungeonTutorialCompleted = PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 1;
        int floorToEnter = selectedFloor;

        if (!isDungeonTutorialCompleted)
        {
            floorToEnter = 0; // Route directly into Tutorial Floor
            Debug.Log("[DungeonEntranceUI] First time entering Dungeon: routing to Tutorial Floor (Floor 0).");
        }
        else if (!IsFloorUnlocked(selectedFloor))
        {
            ShowWarning($"Floor {selectedFloor} is locked! Defeat Floor {selectedFloor - 1} Boss first.");
            return;
        }

        // Warning when player enters Floor >= 2 (or after clearing Floor 1) without a chosen blessing
        ProgressionManager pm = ProgressionManager.Instance;
        bool isAfterFloor1 = floorToEnter >= 2 || (pm != null && pm.IsBossDefeated(1));
        bool noBlessing = pm == null || pm.activeBlessing == BlessingType.None;

        if (isAfterFloor1 && noBlessing)
        {
            ShowBlessingWarningModal(floorToEnter);
            return;
        }

        EnterFloorDirectly(floorToEnter);
    }

    private void ShowBlessingWarningModal(int floorToEnter)
    {
        CloseBlessingWarningModal();

        Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        Transform parentTransform = enterDungeonPanel != null ? enterDungeonPanel.transform : (canvas != null ? canvas.transform : transform);

        blessingWarningModalGO = new GameObject("BlessingWarningModal", typeof(RectTransform), typeof(Image));
        blessingWarningModalGO.transform.SetParent(parentTransform, false);
        blessingWarningModalGO.transform.SetAsLastSibling();

        RectTransform modalRT = blessingWarningModalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = Vector2.zero;
        modalRT.anchorMax = Vector2.one;
        modalRT.sizeDelta = Vector2.zero;

        Image modalBg = blessingWarningModalGO.GetComponent<Image>();
        modalBg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

        // Center Box
        GameObject boxGO = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        boxGO.transform.SetParent(blessingWarningModalGO.transform, false);
        RectTransform boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(530, 310);

        Image boxBg = boxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(boxBg, new Color(0.14f, 0.09f, 0.10f, 0.98f));

        // Header Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(boxGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(490, 32);

        TextMeshProUGUI titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleTMP);
        titleTMP.fontSize = 17;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.text = "<color=#FF5555>[!] CẢNH BÁO: CHƯA CÓ PHƯỚC LÀNH</color>";

        // Message
        GameObject msgGO = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        msgGO.transform.SetParent(boxGO.transform, false);
        RectTransform msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.5f, 0.5f);
        msgRT.anchorMax = new Vector2(0.5f, 0.5f);
        msgRT.pivot = new Vector2(0.5f, 0.5f);
        msgRT.anchoredPosition = new Vector2(0, 15);
        msgRT.sizeDelta = new Vector2(470, 160);

        TextMeshProUGUI msgTMP = msgGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(msgTMP);
        msgTMP.fontSize = 13;
        msgTMP.alignment = TextAlignmentOptions.Center;
        msgTMP.text = "Bạn chưa chọn bất kỳ <b>Phước Lành</b> nào tại <b>Tượng Nữ Thần</b>!\n\n"
                    + "<color=#FF8888>• Từ Tầng 2 trở đi, <b>Bảo Hộ Tân Thủ đã hết hạn</b>.</color>\n"
                    + "<color=#FF5555>• Nếu gục ngã, bạn sẽ <b>MẤT TOÀN BỘ 100% VẬT PHẨM</b> trong ba lô!</color>\n\n"
                    + "Hãy đến Tượng Nữ Thần để nhận Phước Lành (như <i>Bảo Hộ Thần Thánh</i>) trước khi vào hầm ngục.";

        // Buttons Container
        GameObject btnContainer = new GameObject("Buttons", typeof(RectTransform));
        btnContainer.transform.SetParent(boxGO.transform, false);
        RectTransform btnRT = btnContainer.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0);
        btnRT.anchorMax = new Vector2(0.5f, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 18);
        btnRT.sizeDelta = new Vector2(490, 44);

        // 1. Button "ĐẾN TƯỢNG NỮ THẦN"
        GameObject chooseBtnGO = new GameObject("BtnChooseBlessing", typeof(RectTransform), typeof(Image), typeof(Button));
        chooseBtnGO.transform.SetParent(btnContainer.transform, false);
        RectTransform cbRT = chooseBtnGO.GetComponent<RectTransform>();
        cbRT.anchorMin = new Vector2(0, 0.5f);
        cbRT.anchorMax = new Vector2(0, 0.5f);
        cbRT.pivot = new Vector2(0, 0.5f);
        cbRT.anchoredPosition = new Vector2(10, 0);
        cbRT.sizeDelta = new Vector2(225, 40);

        Image cbImg = chooseBtnGO.GetComponent<Image>();
        Button cbBtn = chooseBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(cbBtn, cbImg, new Color(0.20f, 0.45f, 0.70f, 1f), new Color(0.28f, 0.55f, 0.85f, 1f));
        cbBtn.onClick.AddListener(() =>
        {
            CloseBlessingWarningModal();
            Close();

            StatueUI statueUI = StatueUI.Instance != null ? StatueUI.Instance : FindFirstObjectByType<StatueUI>(FindObjectsInactive.Include);
            if (statueUI != null)
            {
                statueUI.ShowBlessingSelectionModal();
            }
            DamagePopupManager.ShowText(Vector3.zero, "Hãy chọn Phước Lành tại Tượng Nữ Thần!", Color.yellow, 2.5f);
        });

        GameObject cbTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        cbTextGO.transform.SetParent(chooseBtnGO.transform, false);
        RectTransform cbtRT = cbTextGO.GetComponent<RectTransform>();
        cbtRT.anchorMin = Vector2.zero;
        cbtRT.anchorMax = Vector2.one;
        cbtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI cbTMP = cbTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(cbTMP);
        cbTMP.fontSize = 12.5f;
        cbTMP.fontStyle = FontStyles.Bold;
        cbTMP.alignment = TextAlignmentOptions.Center;
        cbTMP.text = "ĐẾN TƯỢNG NỮ THẦN";
        cbTMP.color = Color.white;

        // 2. Button "VẪN TIẾN VÀO (RỦI RO)"
        GameObject enterAnywayGO = new GameObject("BtnEnterAnyway", typeof(RectTransform), typeof(Image), typeof(Button));
        enterAnywayGO.transform.SetParent(btnContainer.transform, false);
        RectTransform eaRT = enterAnywayGO.GetComponent<RectTransform>();
        eaRT.anchorMin = new Vector2(1, 0.5f);
        eaRT.anchorMax = new Vector2(1, 0.5f);
        eaRT.pivot = new Vector2(1, 0.5f);
        eaRT.anchoredPosition = new Vector2(-10, 0);
        eaRT.sizeDelta = new Vector2(225, 40);

        Image eaImg = enterAnywayGO.GetComponent<Image>();
        Button eaBtn = enterAnywayGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(eaBtn, eaImg, new Color(0.38f, 0.18f, 0.18f, 1f), new Color(0.50f, 0.24f, 0.24f, 1f));
        eaBtn.onClick.AddListener(() =>
        {
            CloseBlessingWarningModal();
            EnterFloorDirectly(floorToEnter);
        });

        GameObject eaTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        eaTextGO.transform.SetParent(enterAnywayGO.transform, false);
        RectTransform eatRT = eaTextGO.GetComponent<RectTransform>();
        eatRT.anchorMin = Vector2.zero;
        eatRT.anchorMax = Vector2.one;
        eatRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI eaTMP = eaTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(eaTMP);
        eaTMP.fontSize = 12.5f;
        eaTMP.fontStyle = FontStyles.Bold;
        eaTMP.alignment = TextAlignmentOptions.Center;
        eaTMP.text = "VẪN TIẾN VÀO (RỦI RO)";
        eaTMP.color = new Color(1f, 0.85f, 0.85f);
    }

    public void CloseBlessingWarningModal()
    {
        if (blessingWarningModalGO != null)
        {
            Destroy(blessingWarningModalGO);
            blessingWarningModalGO = null;
        }
    }

    public void SelectFloor(int floor)
    {
        if (!IsFloorUnlocked(floor))
        {
            ShowWarning($"Floor {floor} is locked! Defeat Floor {floor - 1} Boss first.");
            return;
        }

        selectedFloor = floor;
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentFloor = selectedFloor;
        }

        RefreshUI();
    }

    public bool IsFloorUnlocked(int floor)
    {
        if (floor <= 1) return true;
        if (ProgressionManager.Instance != null)
        {
            return ProgressionManager.Instance.IsFloorUnlocked(floor);
        }
        return false;
    }

    public bool IsBossDefeated(int floor)
    {
        if (ProgressionManager.Instance != null)
        {
            return ProgressionManager.Instance.IsBossDefeated(floor);
        }
        return false;
    }

    private FloorDisplayData GetFloorData(int floor)
    {
        for (int i = 0; i < floorList.Count; i++)
        {
            if (floorList[i].floorNumber == floor)
                return floorList[i];
        }

        return new FloorDisplayData
        {
            floorNumber = floor,
            floorName = $"FLOOR {floor}",
            bossName = $"Floor {floor} Boss",
            subTitle = $"Floor {floor} of the dungeon",
            description = $"Explore floor {floor} and defeat the boss to proceed."
        };
    }

    private void RefreshUI()
    {
        if (dialogPanel == null) return;

        // Refresh all cards without heap allocations
        foreach (var kvp in floorCards)
        {
            int floorNum = kvp.Key;
            FloorCardUI card = kvp.Value;
            bool isUnlocked = IsFloorUnlocked(floorNum);
            bool isDefeated = IsBossDefeated(floorNum);
            bool isSelected = (floorNum == selectedFloor);

            // Card background border
            card.backgroundImage.color = isSelected ? ColorCardSelected : (isUnlocked ? ColorCardUnlocked : ColorCardLocked);

            // Text colors
            card.floorBadgeText.color = isSelected ? ColorBadgeSelected : (isUnlocked ? ColorBadgeUnlocked : ColorBadgeLocked);
            card.floorNameText.color = isSelected ? Color.white : (isUnlocked ? ColorNameUnlocked : ColorNameLocked);
            card.bossInfoText.color = isSelected ? ColorBossSelected : (isUnlocked ? ColorBossUnlocked : ColorBossLocked);

            // Status Badge
            if (isDefeated)
            {
                bool bossAvailable = ProgressionManager.Instance == null || ProgressionManager.Instance.IsBossAvailable(floorNum);
                if (bossAvailable)
                {
                    card.statusBadgeText.text = "BOSS READY";
                    card.statusBadgeText.color = new Color(1f, 0.88f, 0.35f);
                    card.statusBadgeBg.color = new Color(0.35f, 0.25f, 0.08f, 0.9f);
                }
                else
                {
                    int cd = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetDaysUntilBossReset(floorNum) : 0;
                    card.statusBadgeText.text = $"DA VUOT ({cd}N)";
                    card.statusBadgeText.color = ColorStatusDefeatedText;
                    card.statusBadgeBg.color = ColorStatusDefeatedBg;
                }
            }
            else if (isSelected)
            {
                card.statusBadgeText.text = "> CHON";
                card.statusBadgeText.color = ColorStatusSelectedText;
                card.statusBadgeBg.color = ColorStatusSelectedBg;
            }
            else if (isUnlocked)
            {
                card.statusBadgeText.text = "SAN SANG";
                card.statusBadgeText.color = ColorStatusUnlockedText;
                card.statusBadgeBg.color = ColorStatusUnlockedBg;
            }
            else
            {
                card.statusBadgeText.text = "KHOA";
                card.statusBadgeText.color = ColorStatusLockedText;
                card.statusBadgeBg.color = ColorStatusLockedBg;
            }
        }

        // Details / Hint Text & Enter button text
        bool selUnlocked = IsFloorUnlocked(selectedFloor);
        bool selDefeated = IsBossDefeated(selectedFloor);

        if (detailsText != null && feedbackFlashCoroutine == null)
        {
            if (!selUnlocked)
            {
                detailsText.text = $"<color=#FF5252>Đang khóa: Hạ gục Boss Tầng {selectedFloor - 1} để mở khóa</color>";
            }
            else if (selDefeated)
            {
                bool bAvailable = ProgressionManager.Instance == null || ProgressionManager.Instance.IsBossAvailable(selectedFloor);
                if (bAvailable)
                {
                    detailsText.text = $"<color=#FFE082>Boss Tầng {selectedFloor} đã hồi sinh! Sẵn sàng khiêu chiến lại nhận thưởng.</color>";
                }
                else
                {
                    int daysLeft = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetDaysUntilBossReset(selectedFloor) : 0;
                    detailsText.text = $"<color=#69F0AE>Đã vượt tầng (Boss hồi sinh sau {daysLeft} ngày). Phòng Boss trống, có thể đi qua tầng kế.</color>";
                }
            }
            else
            {
                detailsText.text = $"<color=#FFE082>Tầng đã sẵn sàng. Nhấn [TIẾN VÀO] hoặc bấm Enter để xuất phát</color>";
            }
        }

        if (enterButtonText != null)
        {
            enterButtonText.text = selUnlocked ? $"TIẾN VÀO TẦNG {selectedFloor} (ENTER)" : $"TẦNG {selectedFloor} (ĐANG KHÓA)";
        }

        if (enterButton != null)
        {
            enterButton.interactable = selUnlocked;
        }
    }

    private void ShowWarning(string warning)
    {
        if (detailsText == null) return;

        if (feedbackFlashCoroutine != null)
        {
            StopCoroutine(feedbackFlashCoroutine);
        }

        feedbackFlashCoroutine = StartCoroutine(FlashWarningRoutine(warning));
    }

    private System.Collections.IEnumerator FlashWarningRoutine(string warning)
    {
        detailsText.text = $"<b><color=#FF5252>[!] {warning}</color></b>";

        yield return new WaitForSeconds(2.2f);

        feedbackFlashCoroutine = null;
        RefreshUI();
    }

    #region Scene Binding & Procedural UI Builder
    private void EnsureUIConstructed()
    {
        if (dialogPanel != null && floorCards.Count > 0) return;

        // If authored directly in the scene (dialogPanel assigned or exists under enterDungeonPanel)
        if (dialogPanel != null || (enterDungeonPanel != null && enterDungeonPanel.transform.Find("FloorSelectionModal") != null))
        {
            if (dialogPanel == null && enterDungeonPanel != null)
            {
                dialogPanel = enterDungeonPanel.transform.Find("FloorSelectionModal").gameObject;
            }
            BindSceneReferences();
            BindFloorCards();
            return;
        }

        if (enterDungeonPanel == null)
        {
            return;
        }

        Transform existingModal = enterDungeonPanel.transform.Find("FloorSelectionModal");
        if (existingModal != null)
        {
            Destroy(existingModal.gameObject);
            dialogPanel = null;
        }

        // Hide legacy unstyled Yes/No buttons in the scene so they don't overlap
        for (int i = 0; i < enterDungeonPanel.transform.childCount; i++)
        {
            Transform child = enterDungeonPanel.transform.GetChild(i);
            if (child.name != "FloorSelectionModal")
            {
                child.gameObject.SetActive(false);
            }
        }

        BuildModalUI();
    }

    public void BindSceneReferences()
    {
        if (dialogPanel == null) return;

        if (titleText == null) titleText = dialogPanel.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (subtitleText == null) subtitleText = dialogPanel.transform.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
        if (detailsText == null) detailsText = dialogPanel.transform.Find("HintText")?.GetComponent<TextMeshProUGUI>();
        if (enterButton == null) enterButton = dialogPanel.transform.Find("FooterButtons/BtnEnterDungeon")?.GetComponent<Button>();
        if (enterButtonText == null) enterButtonText = dialogPanel.transform.Find("FooterButtons/BtnEnterDungeon/Text")?.GetComponent<TextMeshProUGUI>();
        if (closeButton == null) closeButton = dialogPanel.transform.Find("FooterButtons/BtnCancel")?.GetComponent<Button>();

        Button xBtn = dialogPanel.transform.Find("BtnCloseX")?.GetComponent<Button>();
        if (xBtn != null && xBtn.onClick.GetPersistentEventCount() == 0)
        {
            xBtn.onClick.RemoveListener(Close);
            xBtn.onClick.AddListener(Close);
        }

        if (enterButton != null && enterButton.onClick.GetPersistentEventCount() == 0)
        {
            enterButton.onClick.RemoveListener(EnterSelectedFloor);
            enterButton.onClick.AddListener(EnterSelectedFloor);
        }

        if (closeButton != null && closeButton.onClick.GetPersistentEventCount() == 0)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    public void BindFloorCards()
    {
        if (floorCards.Count > 0) return;
        if (dialogPanel == null) return;

        if (floorList.Count == 0)
        {
            InitializeFloorData();
        }

        int numFloors = Mathf.Max(totalFloors, floorList.Count);
        for (int f = 1; f <= numFloors; f++)
        {
            Transform cardT = dialogPanel.transform.Find($"FloorScrollView/Viewport/TowerFloorsContainer/FloorCard_{f}");
            if (cardT == null) continue;

            Button btn = cardT.GetComponent<Button>();
            int capturedFloor = f;
            if (btn != null && btn.onClick.GetPersistentEventCount() == 0)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectFloor(capturedFloor));
            }

            FloorCardUI cardUI = new FloorCardUI
            {
                rootGO = cardT.gameObject,
                button = btn,
                backgroundImage = cardT.GetComponent<Image>(),
                floorBadgeText = cardT.Find("BadgeBox/BadgeText")?.GetComponent<TextMeshProUGUI>(),
                floorNameText = cardT.Find("ContentBox/FloorName")?.GetComponent<TextMeshProUGUI>(),
                bossInfoText = cardT.Find("ContentBox/BossInfo")?.GetComponent<TextMeshProUGUI>(),
                statusBadgeText = cardT.Find("StatusBadge/StatusText")?.GetComponent<TextMeshProUGUI>(),
                statusBadgeBg = cardT.Find("StatusBadge")?.GetComponent<Image>(),
                floorNumber = f
            };

            floorCards[f] = cardUI;
        }
    }

    private void BuildModalUI()
    {
        if (floorList.Count == 0)
        {
            InitializeFloorData();
        }

        // 1. Modal Dialog Window
        dialogPanel = new GameObject("FloorSelectionModal", typeof(RectTransform), typeof(Image));
        dialogPanel.transform.SetParent(enterDungeonPanel.transform, false);
        RectTransform dialogRT = dialogPanel.GetComponent<RectTransform>();
        dialogRT.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRT.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRT.pivot = new Vector2(0.5f, 0.5f);
        dialogRT.sizeDelta = new Vector2(560, 560);
        dialogRT.anchoredPosition = Vector2.zero;

        Image dialogBg = dialogPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(dialogBg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // 2. Header Title
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -16);
        titleRT.sizeDelta = new Vector2(500, 24);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 16f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "CHỌN TẦNG HẦM NGỤC";
        titleText.color = new Color(1f, 0.85f, 0.35f);

        // 3. Subtitle / Prompt
        GameObject subtitleGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform subRT = subtitleGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 1);
        subRT.anchorMax = new Vector2(0.5f, 1);
        subRT.pivot = new Vector2(0.5f, 1);
        subRT.anchoredPosition = new Vector2(0, -42);
        subRT.sizeDelta = new Vector2(500, 16);

        subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 10.5f;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "Đánh bại Boss tầng trước để mở khóa tầng tiếp theo";
        subtitleText.color = new Color(0.70f, 0.75f, 0.85f);

        // 4. Decorative divider
        GameObject divGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 1);
        divRT.anchorMax = new Vector2(0.5f, 1);
        divRT.pivot = new Vector2(0.5f, 1);
        divRT.anchoredPosition = new Vector2(0, -64);
        divRT.sizeDelta = new Vector2(500, 2);
        divGO.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.25f, 0.45f);

        // 5. Scroll View Container for Tower Floors
        GameObject scrollGO = new GameObject("FloorScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(24, 82); // Reserved space for Hint and Footer at bottom
        scrollRT.offsetMax = new Vector2(-24, -70); // Reserved space for Header at top

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
        vpRT.offsetMax = new Vector2(-16, 0); // Leave space for slider scrollbar on the right

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

        // Vertical Slider / Scrollbar
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

        floorCards.Clear();

        // Add floors in descending order: 5 at top down to 1 at bottom
        int numFloors = Mathf.Max(totalFloors, floorList.Count);
        for (int f = numFloors; f >= 1; f--)
        {
            CreateFloorCard(contentGO.transform, f);
        }

        // 6. Hint / Warning Status Line (Outside ScrollView, safely above Footer)
        GameObject detailsTextGO = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        detailsTextGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform dtRT = detailsTextGO.GetComponent<RectTransform>();
        dtRT.anchorMin = new Vector2(0.5f, 0);
        dtRT.anchorMax = new Vector2(0.5f, 0);
        dtRT.pivot = new Vector2(0.5f, 0);
        dtRT.anchoredPosition = new Vector2(0, 54);
        dtRT.sizeDelta = new Vector2(520, 30);

        detailsText = detailsTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(detailsText);
        detailsText.fontSize = 10.5f;
        detailsText.enableAutoSizing = true;
        detailsText.fontSizeMin = 8.5f;
        detailsText.fontSizeMax = 10.5f;
        detailsText.alignment = TextAlignmentOptions.Center;
        detailsText.textWrappingMode = TextWrappingModes.Normal;
        detailsText.text = "";

        // 7. Footer Action Buttons (Fixed at bottom outside ScrollView)
        GameObject footerGO = new GameObject("FooterButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footerGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0.5f, 0);
        footerRT.anchorMax = new Vector2(0.5f, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 12);
        footerRT.sizeDelta = new Vector2(500, 38);

        HorizontalLayoutGroup hlg = footerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 14;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // "Enter Dungeon" Button
        GameObject enterBtnGO = new GameObject("BtnEnterDungeon", typeof(RectTransform), typeof(Image), typeof(Button));
        enterBtnGO.transform.SetParent(footerGO.transform, false);
        Image enterImg = enterBtnGO.GetComponent<Image>();
        enterButton = enterBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(enterButton, enterImg, new Color(0.18f, 0.50f, 0.22f, 1f), new Color(0.24f, 0.65f, 0.30f, 1f));
        enterButton.onClick.AddListener(EnterSelectedFloor);

        GameObject enterTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        enterTextGO.transform.SetParent(enterBtnGO.transform, false);
        RectTransform etRT = enterTextGO.GetComponent<RectTransform>();
        etRT.anchorMin = Vector2.zero;
        etRT.anchorMax = Vector2.one;
        etRT.sizeDelta = Vector2.zero;
        enterButtonText = enterTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(enterButtonText);
        enterButtonText.fontSize = 12f;
        enterButtonText.enableAutoSizing = true;
        enterButtonText.fontSizeMin = 9.5f;
        enterButtonText.fontSizeMax = 12f;
        enterButtonText.fontStyle = FontStyles.Bold;
        enterButtonText.alignment = TextAlignmentOptions.Center;
        enterButtonText.text = "TIẾN VÀO TẦNG 1 (ENTER)";
        enterButtonText.color = Color.white;

        // "Back" Button
        GameObject closeBtnGO = new GameObject("BtnCancel", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(footerGO.transform, false);
        Image closeImg = closeBtnGO.GetComponent<Image>();
        closeButton = closeBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(closeButton, closeImg, new Color(0.24f, 0.28f, 0.35f, 1f), new Color(0.32f, 0.38f, 0.48f, 1f));
        closeButton.onClick.AddListener(Close);

        GameObject closeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        RectTransform ctRT = closeTextGO.GetComponent<RectTransform>();
        ctRT.anchorMin = Vector2.zero;
        ctRT.anchorMax = Vector2.one;
        ctRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeText = closeTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(closeText);
        closeText.fontSize = 12f;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.text = "QUAY LẠI";
        closeText.color = new Color(0.9f, 0.9f, 0.9f);

        // Top-right 'X' close button
        GameObject xBtnGO = new GameObject("BtnCloseX", typeof(RectTransform), typeof(Image), typeof(Button));
        xBtnGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform xRT = xBtnGO.GetComponent<RectTransform>();
        xRT.anchorMin = new Vector2(1, 1);
        xRT.anchorMax = new Vector2(1, 1);
        xRT.pivot = new Vector2(1, 1);
        xRT.anchoredPosition = new Vector2(-12, -12);
        xRT.sizeDelta = new Vector2(26, 26);
        Image xImg = xBtnGO.GetComponent<Image>();
        Button xBtn = xBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(xBtn, xImg, new Color(0.25f, 0.28f, 0.35f, 0.9f), new Color(0.85f, 0.25f, 0.25f, 1f));
        xBtn.onClick.AddListener(Close);

        GameObject xTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.transform.SetParent(xBtnGO.transform, false);
        RectTransform xtRT = xTextGO.GetComponent<RectTransform>();
        xtRT.anchorMin = Vector2.zero;
        xtRT.anchorMax = Vector2.one;
        xtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI xt = xTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(xt);
        xt.fontSize = 12;
        xt.fontStyle = FontStyles.Bold;
        xt.alignment = TextAlignmentOptions.Center;
        xt.text = "X";
        xt.color = Color.white;
    }

    private void CreateFloorCard(Transform parent, int floorNum)
    {
        FloorDisplayData data = GetFloorData(floorNum);

        GameObject cardGO = new GameObject($"FloorCard_{floorNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardGO.transform.SetParent(parent, false);

        LayoutElement le = cardGO.GetComponent<LayoutElement>();
        le.preferredHeight = 52;
        le.minHeight = 52;

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

        int capturedFloor = floorNum;
        cardBtn.onClick.AddListener(() => SelectFloor(capturedFloor));

        // 1. Left Badge (e.g. "T1", "T2")
        GameObject badgeGO = new GameObject("BadgeBox", typeof(RectTransform), typeof(Image));
        badgeGO.transform.SetParent(cardGO.transform, false);
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0, 0.5f);
        bRT.anchorMax = new Vector2(0, 0.5f);
        bRT.pivot = new Vector2(0, 0.5f);
        bRT.anchoredPosition = new Vector2(8, 0);
        bRT.sizeDelta = new Vector2(38, 36);
        Image badgeBg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(badgeBg, new Color(0.18f, 0.22f, 0.32f, 1f));

        GameObject bTextGO = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bTextGO.transform.SetParent(badgeGO.transform, false);
        RectTransform btRT = bTextGO.GetComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI bText = bTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(bText);
        bText.fontSize = 11.5f;
        bText.fontStyle = FontStyles.Bold;
        bText.alignment = TextAlignmentOptions.Center;
        bText.text = $"T{floorNum}";
        bText.color = new Color(0.7f, 0.85f, 1f);

        // 2. Middle Content (Floor Name & Boss)
        GameObject contentGO = new GameObject("ContentBox", typeof(RectTransform));
        contentGO.transform.SetParent(cardGO.transform, false);
        RectTransform cRT = contentGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0, 0.5f);
        cRT.offsetMin = new Vector2(52, 2);
        cRT.offsetMax = new Vector2(-108, -2);

        // Floor Name
        GameObject nameGO = new GameObject("FloorName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(contentGO.transform, false);
        RectTransform nRT = nameGO.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0.5f);
        nRT.anchorMax = new Vector2(1, 1);
        nRT.pivot = new Vector2(0, 0.5f);
        nRT.anchoredPosition = new Vector2(0, 5);
        nRT.sizeDelta = new Vector2(0, 18);
        TextMeshProUGUI nameText = nameGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(nameText);
        nameText.fontSize = 11.5f;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 9f;
        nameText.fontSizeMax = 11.5f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.text = data.floorName;
        nameText.color = Color.white;

        // Boss Info
        GameObject bossGO = new GameObject("BossInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        bossGO.transform.SetParent(contentGO.transform, false);
        RectTransform bossRT = bossGO.GetComponent<RectTransform>();
        bossRT.anchorMin = new Vector2(0, 0);
        bossRT.anchorMax = new Vector2(1, 0.5f);
        bossRT.pivot = new Vector2(0, 0.5f);
        bossRT.anchoredPosition = new Vector2(0, -5);
        bossRT.sizeDelta = new Vector2(0, 16);
        TextMeshProUGUI bossText = bossGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(bossText);
        bossText.fontSize = 9.5f;
        bossText.enableAutoSizing = true;
        bossText.fontSizeMin = 7.5f;
        bossText.fontSizeMax = 9.5f;
        bossText.alignment = TextAlignmentOptions.Left;
        bossText.textWrappingMode = TextWrappingModes.NoWrap;
        bossText.overflowMode = TextOverflowModes.Ellipsis;
        bossText.text = $"Boss: {data.bossName}";
        bossText.color = new Color(0.6f, 0.72f, 0.82f);

        // 3. Right Status Badge
        GameObject statusBoxGO = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        statusBoxGO.transform.SetParent(cardGO.transform, false);
        RectTransform sRT = statusBoxGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(1, 0.5f);
        sRT.anchorMax = new Vector2(1, 0.5f);
        sRT.pivot = new Vector2(1, 0.5f);
        sRT.anchoredPosition = new Vector2(-8, 0);
        sRT.sizeDelta = new Vector2(96, 26);

        Image statusBg = statusBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(statusBg, new Color(0.15f, 0.25f, 0.4f, 0.85f));

        GameObject sTextGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        sTextGO.transform.SetParent(statusBoxGO.transform, false);
        RectTransform stRT = sTextGO.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero;
        stRT.anchorMax = Vector2.one;
        stRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI sText = sTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(sText);
        sText.fontSize = 9.5f;
        sText.enableAutoSizing = true;
        sText.fontSizeMin = 7.5f;
        sText.fontSizeMax = 9.5f;
        sText.fontStyle = FontStyles.Bold;
        sText.alignment = TextAlignmentOptions.Center;
        sText.text = "SAN SANG";
        sText.color = Color.white;

        FloorCardUI cardUI = new FloorCardUI
        {
            rootGO = cardGO,
            button = cardBtn,
            backgroundImage = cardBg,
            floorBadgeText = bText,
            floorNameText = nameText,
            bossInfoText = bossText,
            statusBadgeText = sText,
            statusBadgeBg = statusBg,
            floorNumber = floorNum
        };

        floorCards[floorNum] = cardUI;
    }
    #endregion
}