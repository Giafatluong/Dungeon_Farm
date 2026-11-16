using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the God Statue: displays active offerings, required items, drag-and-drop slots,
/// and the player's backpack for seamless offering contributions.
/// </summary>
public class StatueUI : MonoBehaviour
{
    private static StatueUI _instance;
    public static StatueUI Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    public static StatueUI EnsureInstance()
    {
        if (_instance != null) return _instance;
        _instance = FindFirstObjectByType<StatueUI>(FindObjectsInactive.Include);
        return _instance;
    }

    [Header("Panels")]
    [SerializeField] private GameObject statuePanel;
    [SerializeField] private Transform offeringsContainer;
    [SerializeField] private Transform backpackSlotsParent;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject slotPrefab;

    [Header("Labels & Buttons")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI permanentStatsText;
    [SerializeField] private Button closeButton;

    [Header("Blessing UI (Blessings & Build Paths)")]
    [SerializeField] private TextMeshProUGUI blessingStatusText;
    [SerializeField] private Button blessingSelectButton;
    [SerializeField] private TextMeshProUGUI blessingButtonText;

    [Header("Blessing Modal (Scene Objects)")]
    [SerializeField] public GameObject blessingModalPanel;
    [SerializeField] public Button modalCloseButton;
    [SerializeField] public List<BlessingCardUI> blessingCards = new List<BlessingCardUI>();

    public enum OfferingViewCategory
    {
        Might,       // ATK
        Protection,  // DEF
        Swiftness,   // Speed
        Vitality     // Max HP
    }

    private OfferingViewCategory currentViewCategory = OfferingViewCategory.Might;

    private Statue currentStatue;
    private List<Offering> currentOfferings;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();
    private readonly List<OfferingSlot> activeOfferingSlots = new List<OfferingSlot>();

    private bool isExplicitlyOpened = false;
    public bool IsOpen => (isExplicitlyOpened && statuePanel != null && statuePanel.activeSelf)
                       || (blessingModalPanel != null && blessingModalPanel.activeSelf);
    public ItemContainer PlayerBackpack => backpackContainer;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        AutoBindReferences();

        isExplicitlyOpened = false;
        if (statuePanel != null) statuePanel.SetActive(false);
        if (blessingModalPanel != null) blessingModalPanel.SetActive(false);
    }

    private void AutoBindReferences()
    {
        if (statuePanel == null)
        {
            Transform t = transform.Find("StatuePanel");
            if (t != null) statuePanel = t.gameObject;
        }
        if (offeringsContainer == null && statuePanel != null)
        {
            Transform t = statuePanel.transform.Find("Window/OfferingsColumn/ScrollView/Viewport/OfferingsContent");
            if (t != null) offeringsContainer = t;
        }
        if (backpackSlotsParent == null && statuePanel != null)
        {
            Transform t = statuePanel.transform.Find("Window/BackpackColumn/ScrollView/Viewport/BackpackSlots");
            if (t != null) backpackSlotsParent = t;
        }
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponent<Canvas>();
        }
        if (blessingModalPanel == null)
        {
            Transform t = transform.Find("BlessingModal");
            if (t == null && statuePanel != null && statuePanel.transform.parent != null)
            {
                t = statuePanel.transform.parent.Find("BlessingModal");
            }
            if (t != null) blessingModalPanel = t.gameObject;
        }
        if (modalCloseButton == null && blessingModalPanel != null)
        {
            Transform t = blessingModalPanel.transform.Find("DialogBox/BtnCloseModal");
            if (t != null) modalCloseButton = t.GetComponent<Button>();
        }
        if ((blessingCards == null || blessingCards.Count == 0) && blessingModalPanel != null)
        {
            blessingCards = new List<BlessingCardUI>(blessingModalPanel.GetComponentsInChildren<BlessingCardUI>(true));
        }

        ApplyStatueSceneGOTYStyling();
    }

    private void ApplyStatueSceneGOTYStyling()
    {
        if (statuePanel == null) return;

        Transform windowT = statuePanel.transform.Find("Window");
        if (windowT != null)
        {
            Image winImg = windowT.GetComponent<Image>();
            if (winImg != null)
            {
                DungeonUIAssetHelper.ApplyCompSprite(winImg, "Comp_Statue_ModalBase", sliced: true, Color.white);
            }

            Transform bannerT = windowT.Find("BlessingBanner");
            if (bannerT != null)
            {
                if (bannerT is RectTransform bRT && bRT.sizeDelta.y < 40f)
                {
                    bRT.sizeDelta = new Vector2(bRT.sizeDelta.x, 40f);
                }
                Image bImg = bannerT.GetComponent<Image>();
                if (bImg != null)
                {
                    DungeonUIAssetHelper.ApplyCompSprite(bImg, "Comp_Statue_BlessingBanner", sliced: true, Color.white);
                }
            }

            Transform offCol = windowT.Find("OfferingsColumn");
            if (offCol != null)
            {
                Image offImg = offCol.GetComponent<Image>();
                if (offImg != null)
                    DungeonUIAssetHelper.ApplyCompSprite(offImg, "Comp_Statue_ColumnPanel", sliced: true, Color.white);
            }

            Transform bpCol = windowT.Find("BackpackColumn");
            if (bpCol != null)
            {
                Image bpImg = bpCol.GetComponent<Image>();
                if (bpImg != null)
                    DungeonUIAssetHelper.ApplyCompSprite(bpImg, "Comp_Statue_ColumnPanel", sliced: true, Color.white);
            }

            Transform footerT = windowT.Find("Footer");
            if (footerT != null)
            {
                Image fImg = footerT.GetComponent<Image>();
                if (fImg != null)
                    DungeonUIAssetHelper.ApplyCompSprite(fImg, "Comp_Statue_HintBar", sliced: true, Color.white);
            }

            if (titleText == null)
                titleText = windowT.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (feedbackText == null)
                feedbackText = windowT.Find("Feedback")?.GetComponent<TextMeshProUGUI>();
            if (permanentStatsText == null)
                permanentStatsText = windowT.Find("Footer/StatsText")?.GetComponent<TextMeshProUGUI>();
            if (closeButton == null)
                closeButton = windowT.Find("BtnClose")?.GetComponent<Button>();
            if (blessingSelectButton == null && bannerT != null)
                blessingSelectButton = bannerT.Find("BtnSelectBlessing")?.GetComponent<Button>();
            if (blessingStatusText == null && bannerT != null)
                blessingStatusText = bannerT.Find("BlessingStatusText")?.GetComponent<TextMeshProUGUI>();
            if (blessingButtonText == null && blessingSelectButton != null)
                blessingButtonText = blessingSelectButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (blessingSelectButton != null)
        {
            DungeonUIAssetHelper.ApplyCompButton(blessingSelectButton, blessingSelectButton.GetComponent<Image>(), "Comp_Statue_ActionButton", Color.white);
        }
        if (closeButton != null)
        {
            Image closeImg = closeButton.GetComponent<Image>();
            if (DungeonUIAssetHelper.ApplyCompButton(closeButton, closeImg, "Comp_Statue_BtnCloseX", Color.white) && closeImg != null)
            {
                closeImg.type = Image.Type.Simple;
            }
        }

        ConfigureStatueTextAutoSize(titleText, 14f, 20f, wrap: false);
        ConfigureStatueTextAutoSize(blessingStatusText, 9f, 11.5f, wrap: true);
        ConfigureStatueTextAutoSize(feedbackText, 9.5f, 12f, wrap: false);
        ConfigureStatueTextAutoSize(permanentStatsText, 9.5f, 12.5f, wrap: false);
        ConfigureStatueTextAutoSize(blessingButtonText, 9.5f, 12.5f, wrap: false);
    }

    private static void ConfigureStatueTextAutoSize(TextMeshProUGUI tmp, float minSize, float maxSize, bool wrap)
    {
        if (tmp == null) return;
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void Start()
    {
        // Only hide on start if not already opened by player interaction
        if (!isExplicitlyOpened && statuePanel != null)
        {
            statuePanel.SetActive(false);
        }
        if (modalCloseButton != null)
        {
            modalCloseButton.onClick.RemoveAllListeners();
            modalCloseButton.onClick.AddListener(CloseBlessingSelectionModal);
        }
    }

    private void Update()
    {
        if (blessingModalPanel != null && blessingModalPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            CloseBlessingSelectionModal();
            return;
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            Close();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange += RefreshBackpackSlots;
        }
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnBlessingChanged += OnBlessingStateChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
        }
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnBlessingChanged -= OnBlessingStateChanged;
        }
    }

    private void OnBlessingStateChanged(BlessingType newBlessing)
    {
        UpdateBlessingDisplay();
    }

    public void Open(Statue statue, List<Offering> offerings, ItemContainer backpack)
    {
        isExplicitlyOpened = true;
        currentStatue = statue;
        currentOfferings = offerings;
        if (currentOfferings == null || currentOfferings.Count < 40)
        {
            Offering[] allOfferings = GameAssetHelper.LoadAll<Offering>();
            if (allOfferings != null && allOfferings.Length > 0)
            {
                currentOfferings = new List<Offering>(allOfferings);
            }
        }
        backpackContainer = backpack;
        if (backpackContainer == null)
        {
            InventoryPanel invPanel = InventoryPanel.Instance != null ? InventoryPanel.Instance : FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null && invPanel.ItemContainer != null)
            {
                backpackContainer = invPanel.ItemContainer;
            }
        }
        if (backpackContainer == null)
        {
            PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
            if (ps != null && ps.itemContainer != null)
            {
                backpackContainer = ps.itemContainer;
            }
        }
        if (backpackContainer == null)
        {
            ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null && !containers[i].name.ToLower().Contains("chest") && !containers[i].name.ToLower().Contains("loot"))
                {
                    backpackContainer = containers[i];
                    break;
                }
            }
        }

        UIEscapeManager.NotifyModalOpened("Statue");
        ApplyStatueSceneGOTYStyling();

        if (statuePanel != null)
        {
            statuePanel.SetActive(true);
            statuePanel.transform.SetAsLastSibling();
        }

        SubscribeEvents();

        BuildOfferingsList(true);
        BuildBackpackSlots(backpackContainer);
        UpdatePermanentStatsDisplay();
        UpdateBlessingDisplay();
        SetFeedback("Drag items from backpack or click a requirement slot to make an offering.");
    }

    public void Close()
    {
        isExplicitlyOpened = false;
        UnsubscribeEvents();
        CloseBlessingSelectionModal();

        if (statuePanel != null)
        {
            statuePanel.SetActive(false);
        }

        UIEscapeManager.NotifyModalClosed("Statue");
        CurrentItemPanel.RefreshHotbar();
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.permanentATK > 0)
        {
            TutorialManager.Instance?.NotifyStatueOfferingDone();
        }
    }

    public void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    public void CheckOfferingCompletion(Offering offering)
    {
        if (offering == null) return;

        bool alreadyDone = offering.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(offering.offeringKey));

        if (alreadyDone)
        {
            RefreshAllOfferingSlots();
            return;
        }

        if (offering.requiredItems == null || offering.requiredItems.Length == 0) return;

        bool allFulfilled = true;
        for (int i = 0; i < offering.requiredItems.Length; i++)
        {
            ItemRequirement req = offering.requiredItems[i];
            if (req.item == null || req.amount <= 0) continue;

            int cont = ProgressionManager.Instance != null ?
                ProgressionManager.Instance.GetOfferingProgress(offering.offeringKey, i) : 0;

            if (cont < req.amount)
            {
                allFulfilled = false;
                break;
            }
        }

        if (allFulfilled)
        {
            offering.completed = true;

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.CompleteOffering(offering.offeringKey);
                ProgressionManager.Instance.AddPermanentStat(offering.rewardStat, offering.rewardAmount);
            }

            // Sync with PlayerStats and show celebratory popup
            PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
            if (ps != null)
            {
                ps.RefreshStatsFromProgression();
                DamagePopupManager.ShowText(ps.transform.position + Vector3.up * 1.5f, $"+{offering.rewardAmount} {GetStatDisplayName(offering.rewardStat)} VĨNH VIỄN!", new Color(1f, 0.85f, 0.2f), 4.5f);
            }

            if (currentStatue != null)
            {
                currentStatue.NotifyOfferingSuccess(offering);
            }

            SetFeedback($"<color=#FFD700>PHƯỚC LÀNH CỔ XƯA! Đã hoàn thành {GetOfferingDisplayName(offering)}! (+{offering.rewardAmount} {GetStatDisplayName(offering.rewardStat)} vĩnh viễn)</color>");
            Debug.Log($"[StatueUI] Offering completed: {offering.offeringName}! Rewarded permanent +{offering.rewardAmount} {offering.rewardStat}");
            TutorialManager.Instance?.NotifyStatueOfferingDone();

            RefreshAllOfferingSlots();
            UpdatePermanentStatsDisplay();
            BuildOfferingsList(); // Rebuild cards to show completed state
        }
        else
        {
            RefreshAllOfferingSlots();
        }
    }

    public void QuickOffer(Offering offering)
    {
        if (offering == null || backpackContainer == null) return;

        if (!offering.IsUnlocked())
        {
            SetFeedback("<color=#FF6666>Cột mốc này đang bị khóa! Cần hoàn thành cột mốc trước đó.</color>");
            return;
        }

        bool isDone = offering.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(offering.offeringKey));
        if (isDone)
        {
            SetFeedback("<color=#88FF88>Cột mốc dâng lễ này đã hoàn thành!</color>");
            return;
        }

        int totalDeposited = 0;

        for (int i = 0; i < offering.requiredItems.Length; i++)
        {
            ItemRequirement req = offering.requiredItems[i];
            if (req.item == null || req.amount <= 0) continue;

            int cont = ProgressionManager.Instance != null ?
                ProgressionManager.Instance.GetOfferingProgress(offering.offeringKey, i) : 0;
            int needed = req.amount - cont;
            if (needed <= 0) continue;

            int available = GetTotalItemCount(backpackContainer, req.item);
            int deposit = 0;

            if (available > 0)
            {
                int backpackDeposit = Mathf.Min(available, needed);
                backpackContainer.RemoveItem(req.item, backpackDeposit);
                deposit += backpackDeposit;
            }

            if (deposit < needed && HomeChest.Instance != null && HomeChest.Instance.chestContainer != null)
            {
                int chestAvail = GetTotalItemCount(HomeChest.Instance.chestContainer, req.item);
                if (chestAvail > 0)
                {
                    int fromChest = Mathf.Min(chestAvail, needed - deposit);
                    HomeChest.Instance.chestContainer.RemoveItem(req.item, fromChest);
                    HomeChest.Instance.chestContainer.NotifyChange();
                    HomeChest.Instance.SaveChestData();
                    deposit += fromChest;
                }
            }

            if (deposit <= 0) continue;

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.AddOfferingProgress(offering.offeringKey, i, deposit);
            }

            totalDeposited += deposit;
        }

        if (totalDeposited > 0)
        {
            backpackContainer.NotifyChange();
            CurrentItemPanel.RefreshHotbar();
            SetFeedback($"<color=#FFD700>Đã dâng nhanh {totalDeposited} vật phẩm cho {GetOfferingDisplayName(offering)}!</color>");
            CheckOfferingCompletion(offering);
            RefreshAllOfferingSlots();
        }
        else
        {
            SetFeedback("<color=#FFAA55>Không có vật phẩm phù hợp trong ba lô hoặc Rương Nhà để dâng lễ.</color>");
        }
    }

    private void RefreshAllOfferingSlots()
    {
        for (int i = 0; i < activeOfferingSlots.Count; i++)
        {
            if (activeOfferingSlots[i] != null)
            {
                activeOfferingSlots[i].UpdateVisuals();
            }
        }
    }

    private void UpdatePermanentStatsDisplay()
    {
        if (permanentStatsText == null) return;

        int atk = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentATK : 0;
        int def = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentDEF : 0;
        int spd = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentSpeed : 0;
        int hp = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentMaxHP : 0;

        PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
        int totalHp = ps != null ? ps.maxHealth : (35 + hp);
        int totalAtk = ps != null ? ps.GetCurrentATK() : (7 + atk);
        int totalDef = ps != null ? ps.GetCurrentDEF() : (1 + def);
        int totalSpd = ps != null ? ps.GetCurrentSpeed() : (14 + spd);

        permanentStatsText.text = $"<b>Chỉ Số Vĩnh Viễn:</b>   <color=#55FF88>HP: {totalHp} (+{hp})</color>   |   <color=#FF7777>ATK: {totalAtk} (+{atk})</color>   |   <color=#77AAFF>DEF: {totalDef} (+{def})</color>   |   <color=#FFFF77>Tốc độ: {totalSpd} (+{spd})</color>";
    }

    #region Run Blessings UI (Divine Blessings & Build Paths)
    public void UpdateBlessingDisplay()
    {
        if (blessingStatusText == null) return;

        bool isStarter = ProgressionManager.Instance != null && ProgressionManager.Instance.IsStarterProtectionActive();
        BlessingType currentType = ProgressionManager.Instance != null ? ProgressionManager.Instance.activeBlessing : BlessingType.None;

        if (isStarter)
        {
            blessingStatusText.text = "<b><color=#55CCFF>[BẢO HỘ TÂN THỦ] Thần Linh Che Chở:</color></b> Tự động bảo vệ ba lô của bạn (không bị rơi vật phẩm khi gục ngã ở Tầng 1-2)!";
            if (blessingButtonText != null) blessingButtonText.text = "Đổi Phước Lành";
        }
        else if (currentType != BlessingType.None)
        {
            BlessingInfo info = BlessingDatabase.GetBlessingInfo(currentType);
            string hex = info != null ? ColorUtility.ToHtmlStringRGB(info.color) : "55CCFF";
            string title = info != null ? info.title : currentType.ToString();
            string icon = info != null ? info.iconSymbol : "[BUFF]";
            string desc = info != null ? info.description : "";
            blessingStatusText.text = $"<b><color=#{hex}>[HIỆU LỰC] {icon} {title}:</color></b> {desc} <i><color=#AAAAAA>(Hết hạn vào cuối ngày, khi gục ngã hoặc hoàn thành hầm ngục)</color></i>";
            if (blessingButtonText != null) blessingButtonText.text = "Đổi Phước Lành";
        }
        else
        {
            blessingStatusText.text = "<b><color=#FFCC44>[CHƯA CHỌN PHƯỚC LÀNH]</color></b> Hãy chọn một phước lành để gia tăng sức mạnh trước khi tiến vào Hầm Ngục!";
            if (blessingButtonText != null) blessingButtonText.text = "Chọn Phước Lành";
        }
    }

    public void ShowBlessingSelectionModal()
    {
        if (blessingModalPanel == null) AutoBindReferences();
        if (blessingModalPanel == null)
        {
            EnsureProceduralBlessingModal();
        }

        if (blessingModalPanel != null)
        {
            EnsureBlessingModalLayout();
            blessingModalPanel.SetActive(true);
            blessingModalPanel.transform.SetAsLastSibling();
            RefreshBlessingCards();
        }
    }

    private void EnsureBlessingModalLayout()
    {
        if (blessingModalPanel == null) return;

        // Ensure DialogBox panel size is large and tall for 1080p
        Transform dialogBox = blessingModalPanel.transform.Find("DialogBox");
        if (dialogBox == null && blessingModalPanel.transform.childCount > 0)
        {
            dialogBox = blessingModalPanel.transform.GetChild(0);
        }
        if (dialogBox != null)
        {
            RectTransform dRT = dialogBox.GetComponent<RectTransform>();
            if (dRT != null)
            {
                dRT.sizeDelta = new Vector2(1180f, 860f);
            }
        }

        // Ensure CardsContainer has proper grid dimensions for tall 246 x 366 Figma GOTY cards
        Transform containerTr = dialogBox != null ? dialogBox.Find("CardsContainer") : blessingModalPanel.transform.Find("CardsContainer");
        if (containerTr != null)
        {
            RectTransform cRT = containerTr.GetComponent<RectTransform>();
            if (cRT != null)
            {
                cRT.anchoredPosition = new Vector2(0, -32f);
                cRT.sizeDelta = new Vector2(-48f, -96f);
            }
            GridLayoutGroup glg = containerTr.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                glg.cellSize = new Vector2(246f, 366f);
                glg.spacing = new Vector2(16f, 14f);
                glg.padding = new RectOffset(16, 16, 8, 8);
                glg.childAlignment = TextAnchor.MiddleCenter;
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 4;
            }
        }
    }

    public void EnsureProceduralBlessingModal()
    {
        if (blessingModalPanel == null)
        {
            AutoBindReferences();
        }
    }

    public void CloseBlessingSelectionModal()
    {
        if (blessingModalPanel != null)
        {
            blessingModalPanel.SetActive(false);
        }
    }

    public void RefreshBlessingCards()
    {
        if (blessingCards == null || blessingCards.Count == 0)
        {
            if (blessingModalPanel != null)
            {
                blessingCards = new List<BlessingCardUI>(blessingModalPanel.GetComponentsInChildren<BlessingCardUI>(true));
            }
        }

        if (blessingCards != null)
        {
            for (int i = 0; i < blessingCards.Count; i++)
            {
                if (blessingCards[i] != null)
                {
                    blessingCards[i].UpdateVisuals(OnBlessingCardSelected);
                }
            }
        }
    }

    private void OnBlessingCardSelected(BlessingType type)
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.SetBlessing(type);
        }
        UpdateBlessingDisplay();
        RefreshBlessingCards(); // Instantly update active card gold border, keep modal open for free switching
        BlessingInfo info = BlessingDatabase.GetBlessingInfo(type);
        SetFeedback($"<color=#55FF88>[PHƯỚC LÀNH ĐÃ ĐỔI] Kích hoạt nhánh: {info?.title ?? type.ToString()}!</color>");
    }
    #endregion

    private void BuildOfferingsList(bool resetScroll = false)
    {
        if (offeringsContainer == null || currentOfferings == null) return;

        activeOfferingSlots.Clear();

        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in offeringsContainer) toDestroy.Add(child.gameObject);
        for (int i = 0; i < toDestroy.Count; i++) { toDestroy[i].transform.SetParent(null); Destroy(toDestroy[i]); }

        // Sort by Category then Milestone Tier
        currentOfferings.Sort((a, b) =>
        {
            if (a.rewardStat != b.rewardStat) return a.rewardStat.CompareTo(b.rewardStat);
            return a.tier.CompareTo(b.tier);
        });

        // 1. Navigation Category Tabs (Tấn Công (ATK), Phòng Thủ (DEF), Tốc Độ (SPD), Sinh Mệnh (HP))
        // Place sticky category tabs in OfferingsColumn (outside ScrollRect content) so they stay fixed when scrolling
        ScrollRect sr = offeringsContainer.GetComponentInParent<ScrollRect>();
        Transform columnTransform = sr != null && sr.transform.parent != null ? sr.transform.parent : offeringsContainer.parent;
        EnsureStickyHeaderAndScrollLayout(columnTransform, sr);
        BuildCategoryTabs(columnTransform);

        // 2. Render View filtered by selected category tab
        Offering.RewardStat filterStat = currentViewCategory switch
        {
            OfferingViewCategory.Might => Offering.RewardStat.ATK,
            OfferingViewCategory.Protection => Offering.RewardStat.DEF,
            OfferingViewCategory.Swiftness => Offering.RewardStat.Speed,
            OfferingViewCategory.Vitality => Offering.RewardStat.MaxHP,
            _ => Offering.RewardStat.ATK
        };

        var filtered = currentOfferings.FindAll(o => o.rewardStat == filterStat);
        for (int i = 0; i < filtered.Count; i++)
        {
            CreateOfferingCard(filtered[i], offeringsContainer);
        }

        if (resetScroll && sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 1f;
        }
    }

    private void EnsureStickyHeaderAndScrollLayout(Transform columnTransform, ScrollRect sr)
    {
        if (columnTransform == null) return;

        Transform headerTrans = columnTransform.Find("Header");
        if (headerTrans is RectTransform headerRT)
        {
            headerRT.anchorMin = new Vector2(0f, 1f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = new Vector2(0f, -6f);
            headerRT.sizeDelta = new Vector2(-20f, 22f);
        }

        if (sr != null && sr.transform is RectTransform srRT)
        {
            srRT.anchorMin = new Vector2(0f, 0f);
            srRT.anchorMax = new Vector2(1f, 1f);
            srRT.pivot = new Vector2(0.5f, 0.5f);
            srRT.offsetMin = new Vector2(8f, 8f);
            srRT.offsetMax = new Vector2(-8f, -64f);

            if (sr.viewport != null)
            {
                sr.viewport.anchorMin = new Vector2(0.015f, 0.01f);
                sr.viewport.anchorMax = new Vector2(0.955f, 1f);
                sr.viewport.offsetMin = Vector2.zero;
                sr.viewport.offsetMax = Vector2.zero;
            }
        }
    }

    private void BuildCategoryTabs(Transform columnParent)
    {
        if (columnParent == null) return;

        Transform existingTabs = columnParent.Find("CategoryTabsRow");
        if (existingTabs != null)
        {
            existingTabs.SetParent(null);
            Destroy(existingTabs.gameObject);
        }

        GameObject tabsGO = new GameObject("CategoryTabsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        tabsGO.transform.SetParent(columnParent, false);

        RectTransform tabsRT = tabsGO.GetComponent<RectTransform>();
        tabsRT.anchorMin = new Vector2(0f, 1f);
        tabsRT.anchorMax = new Vector2(1f, 1f);
        tabsRT.pivot = new Vector2(0.5f, 1f);
        tabsRT.anchoredPosition = new Vector2(0f, -30f);
        tabsRT.sizeDelta = new Vector2(-24f, 28f);

        LayoutElement le = tabsGO.GetComponent<LayoutElement>();
        le.minHeight = 28f;
        le.preferredHeight = 28f;
        le.flexibleHeight = 0;

        HorizontalLayoutGroup hlg = tabsGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        CreateTabButton("Tấn Công", OfferingViewCategory.Might, tabsGO.transform);
        CreateTabButton("Phòng Thủ", OfferingViewCategory.Protection, tabsGO.transform);
        CreateTabButton("Tốc Độ", OfferingViewCategory.Swiftness, tabsGO.transform);
        CreateTabButton("Sinh Mệnh", OfferingViewCategory.Vitality, tabsGO.transform);
    }

    private void CreateTabButton(string label, OfferingViewCategory category, Transform parent)
    {
        GameObject btnGO = new GameObject($"Tab_{category}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 28f);

        bool isSelected = currentViewCategory == category;
        Image img = btnGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(img, "Comp_Statue_Tab", sliced: true, isSelected ? Color.white : new Color(0.76f, 0.72f, 0.70f, 0.95f)))
        {
            if (isSelected)
            {
                DungeonUIAssetHelper.StyleSlicedFrame(img, new Color(0.85f, 0.65f, 0.16f, 1f));
            }
            else
            {
                DungeonUIAssetHelper.StyleSlicedPanel(img, DungeonUIAssetHelper.PanelStyle.Slate);
                img.color = new Color(0.18f, 0.2f, 0.26f, 0.95f);
            }
            img.pixelsPerUnitMultiplier = 2.6f;
        }

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            currentViewCategory = category;
            BuildOfferingsList(true);
        });

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(4, 2);
        txtRT.offsetMax = new Vector2(-4, -2);

        TextMeshProUGUI tmp = txtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp, withOutline: isSelected);
        tmp.fontSize = 11f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 8.5f;
        tmp.fontSizeMax = 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = label;
        tmp.color = isSelected ? new Color(1f, 0.92f, 0.55f) : new Color(0.84f, 0.80f, 0.78f);
    }

    private void CreateOfferingCard(Offering off, Transform parent)
    {
        bool isDone = off.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(off.offeringKey));
        bool isUnlocked = off.IsUnlocked();

        // Card Root (112px accommodates GOTY OfferingCardBase shadow + non-overlapping header & req slots)
        GameObject cardGO = new GameObject($"Card_{off.offeringKey}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardGO.transform.SetParent(parent, false);

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0, 112);

        LayoutElement cardLE = cardGO.AddComponent<LayoutElement>();
        cardLE.minHeight = 112f;
        cardLE.preferredHeight = 112f;
        cardLE.flexibleHeight = 0;

        Image cardBg = cardGO.GetComponent<Image>();
        Color cardTint = isDone
            ? new Color(0.92f, 1f, 0.92f, 1f)
            : (!isUnlocked ? new Color(0.62f, 0.60f, 0.62f, 0.92f) : Color.white);
        bool usesGotyCard = DungeonUIAssetHelper.ApplyCompSprite(cardBg, "Comp_Statue_OfferingCardBase", sliced: true, cardTint);
        if (!usesGotyCard)
        {
            if (isDone)
            {
                DungeonUIAssetHelper.StyleSlicedPanel(cardBg, DungeonUIAssetHelper.PanelStyle.Wood);
            }
            else if (!isUnlocked)
            {
                DungeonUIAssetHelper.StyleSlicedPanel(cardBg, DungeonUIAssetHelper.PanelStyle.Slate);
                cardBg.color = new Color(0.12f, 0.13f, 0.17f, 0.92f);
            }
            else
            {
                DungeonUIAssetHelper.StyleSlicedPanel(cardBg, DungeonUIAssetHelper.PanelStyle.Slate);
            }
            cardBg.pixelsPerUnitMultiplier = 1.6f;

            Outline outline = cardGO.AddComponent<Outline>();
            if (isDone)
                outline.effectColor = new Color(0.25f, 0.85f, 0.45f, 0.7f);
            else if (!isUnlocked)
                outline.effectColor = new Color(0.35f, 0.35f, 0.4f, 0.4f);
            else if (off.tier == 5)
                outline.effectColor = new Color(1f, 0.78f, 0.2f, 0.95f);
            else if (off.tier == 10)
                outline.effectColor = new Color(0.9f, 0.45f, 1f, 0.95f);
            else
                outline.effectColor = new Color(0.65f, 0.55f, 0.25f, 0.6f);
            outline.effectDistance = new Vector2(1, -1);
        }

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 12, 16);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header Row (Flexible Title + Reward Badge + Completed/Lock/Quick Offer Button)
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(cardGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.sizeDelta = new Vector2(0, 26);

        LayoutElement headerLE = headerGO.GetComponent<LayoutElement>();
        headerLE.minHeight = 26f;
        headerLE.preferredHeight = 26f;

        HorizontalLayoutGroup hlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Title (flexible width + autosize prevents colliding into RewardBadge)
        GameObject nameGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        nameGO.transform.SetParent(headerGO.transform, false);
        LayoutElement nameLE = nameGO.GetComponent<LayoutElement>();
        nameLE.minWidth = 100f;
        nameLE.flexibleWidth = 1f;
        TextMeshProUGUI titleTMP = nameGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleTMP, withOutline: true);
        titleTMP.fontSize = 12.5f;
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMin = 9.5f;
        titleTMP.fontSizeMax = 12.5f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
        titleTMP.overflowMode = TextOverflowModes.Ellipsis;
        titleTMP.text = GetOfferingDisplayName(off);
        if (isDone)
            titleTMP.color = new Color(0.50f, 1f, 0.65f);
        else if (!isUnlocked)
            titleTMP.color = new Color(0.68f, 0.66f, 0.70f);
        else
            titleTMP.color = new Color(1f, 0.88f, 0.51f);

        // Reward Badge
        GameObject badgeGO = new GameObject("RewardBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        badgeGO.transform.SetParent(headerGO.transform, false);
        LayoutElement badgeLE = badgeGO.GetComponent<LayoutElement>();
        badgeLE.minWidth = 88f;
        badgeLE.preferredWidth = 92f;
        Image badgeImg = badgeGO.GetComponent<Image>();
        string badgeCompName = off.rewardStat switch
        {
            Offering.RewardStat.ATK => "Comp_Combat_ActionBtn",
            Offering.RewardStat.DEF => "Comp_EnterDungeon_StatusBadge",
            Offering.RewardStat.MaxHP => "Comp_Statue_ActionButton",
            _ => "Comp_Statue_RewardBadge"
        };
        if (!DungeonUIAssetHelper.ApplyCompSprite(badgeImg, badgeCompName, sliced: true, Color.white)
            && !DungeonUIAssetHelper.ApplyCompSprite(badgeImg, "Comp_Statue_RewardBadge", sliced: true, Color.white))
        {
            DungeonUIAssetHelper.StyleSlicedFrame(badgeImg, GetStatBadgeColor(off.rewardStat));
            badgeImg.pixelsPerUnitMultiplier = 2.6f;
        }
        HorizontalLayoutGroup bHlg = badgeGO.GetComponent<HorizontalLayoutGroup>();
        bHlg.padding = new RectOffset(4, 4, 1, 1);
        bHlg.childControlWidth = true;
        bHlg.childControlHeight = true;

        GameObject badgeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTextGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTMP = badgeTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(badgeTMP, withOutline: true);
        badgeTMP.fontSize = 10.5f;
        badgeTMP.enableAutoSizing = true;
        badgeTMP.fontSizeMin = 8.5f;
        badgeTMP.fontSizeMax = 10.5f;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.textWrappingMode = TextWrappingModes.NoWrap;
        badgeTMP.fontStyle = FontStyles.Bold;
        string statLabel = GetStatDisplayName(off.rewardStat);
        badgeTMP.text = $"+{off.rewardAmount} {statLabel}";
        badgeTMP.color = Color.white;

        // Status or Quick Offer Button
        if (isDone)
        {
            GameObject doneGO = new GameObject("DoneBadge", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            doneGO.transform.SetParent(headerGO.transform, false);
            LayoutElement doneLE = doneGO.GetComponent<LayoutElement>();
            doneLE.minWidth = 108f;
            doneLE.preferredWidth = 116f;
            TextMeshProUGUI doneTMP = doneGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(doneTMP, withOutline: true);
            doneTMP.fontSize = 10.5f;
            doneTMP.enableAutoSizing = true;
            doneTMP.fontSizeMin = 8.5f;
            doneTMP.fontSizeMax = 10.5f;
            doneTMP.alignment = TextAlignmentOptions.Right;
            doneTMP.textWrappingMode = TextWrappingModes.NoWrap;
            doneTMP.fontStyle = FontStyles.Bold;
            doneTMP.text = "[ĐÃ HOÀN THÀNH]";
            doneTMP.color = new Color(0.45f, 1f, 0.58f);
        }
        else if (!isUnlocked)
        {
            GameObject lockBadgeGO = new GameObject("LockBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            lockBadgeGO.transform.SetParent(headerGO.transform, false);
            LayoutElement lockLE = lockBadgeGO.GetComponent<LayoutElement>();
            lockLE.minWidth = 112f;
            lockLE.preferredWidth = 122f;
            Image lockBg = lockBadgeGO.GetComponent<Image>();
            if (!DungeonUIAssetHelper.ApplyCompSprite(lockBg, "Comp_EnterDungeon_HintBar", sliced: true, Color.white))
            {
                DungeonUIAssetHelper.StyleSlicedFrame(lockBg, new Color(0.35f, 0.15f, 0.15f, 0.95f));
                lockBg.pixelsPerUnitMultiplier = 2.6f;
            }
            HorizontalLayoutGroup lHlg = lockBadgeGO.GetComponent<HorizontalLayoutGroup>();
            lHlg.padding = new RectOffset(4, 4, 1, 1);
            lHlg.childControlWidth = true;
            lHlg.childControlHeight = true;

            GameObject lockTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            lockTxtGO.transform.SetParent(lockBadgeGO.transform, false);
            TextMeshProUGUI lockTMP = lockTxtGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(lockTMP);
            lockTMP.fontSize = 9.5f;
            lockTMP.enableAutoSizing = true;
            lockTMP.fontSizeMin = 7.5f;
            lockTMP.fontSizeMax = 9.5f;
            lockTMP.fontStyle = FontStyles.Bold;
            lockTMP.alignment = TextAlignmentOptions.Center;
            lockTMP.textWrappingMode = TextWrappingModes.NoWrap;
            lockTMP.text = off.tier == 5 ? "[CẦN BOSS 1]" : (off.tier == 10 ? "[CẦN BOSS TỐI THƯỢNG]" : "[CHƯA MỞ KHÓA]");
            lockTMP.color = new Color(1f, 0.65f, 0.65f);
        }
        else
        {
            GameObject btnGO = new GameObject("BtnQuickOffer", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGO.transform.SetParent(headerGO.transform, false);
            LayoutElement btnLE = btnGO.GetComponent<LayoutElement>();
            btnLE.minWidth = 98f;
            btnLE.preferredWidth = 104f;
            Image btnBg = btnGO.GetComponent<Image>();

            Button btn = btnGO.GetComponent<Button>();
            if (!DungeonUIAssetHelper.ApplyCompButton(btn, btnBg, "Comp_Statue_ActionButton", Color.white))
            {
                DungeonUIAssetHelper.StyleTactileButton(btn, btnBg, DungeonUIAssetHelper.ButtonStyle.Victory);
                btnBg.pixelsPerUnitMultiplier = 2.6f;
            }
            Offering offRef = off;
            btn.onClick.AddListener(() => QuickOffer(offRef));

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            RectTransform btRT = btnTextGO.GetComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero;
            btRT.anchorMax = Vector2.one;
            btRT.offsetMin = new Vector2(4, 2);
            btRT.offsetMax = new Vector2(-4, -2);
            TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(btnTMP, withOutline: true);
            btnTMP.fontSize = 10.5f;
            btnTMP.enableAutoSizing = true;
            btnTMP.fontSizeMin = 8.5f;
            btnTMP.fontSizeMax = 10.5f;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.textWrappingMode = TextWrappingModes.NoWrap;
            btnTMP.text = "Dâng Nhanh";
            btnTMP.color = Color.white;
        }

        // Requirements Row
        GameObject reqsRowGO = new GameObject("RequirementsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        reqsRowGO.transform.SetParent(cardGO.transform, false);
        RectTransform reqsRowRT = reqsRowGO.GetComponent<RectTransform>();
        reqsRowRT.sizeDelta = new Vector2(0, 52);

        LayoutElement reqsLE = reqsRowGO.GetComponent<LayoutElement>();
        reqsLE.minHeight = 52f;
        reqsLE.preferredHeight = 52f;

        HorizontalLayoutGroup reqsHLG = reqsRowGO.GetComponent<HorizontalLayoutGroup>();
        reqsHLG.spacing = 8;
        reqsHLG.childControlWidth = false;
        reqsHLG.childControlHeight = false;
        reqsHLG.childForceExpandWidth = false;
        reqsHLG.childForceExpandHeight = false;

        if (off.requiredItems != null)
        {
            for (int r = 0; r < off.requiredItems.Length; r++)
            {
                ItemRequirement req = off.requiredItems[r];
                if (req.item == null) continue;

                CreateOfferingSlot(off, r, req, reqsRowGO.transform);
            }
        }
    }

    private void CreateOfferingSlot(Offering off, int reqIndex, ItemRequirement req, Transform parent)
    {
        // Slot Box Root
        GameObject slotGO = new GameObject($"ReqSlot_{reqIndex}", typeof(RectTransform), typeof(Image), typeof(OfferingSlot), typeof(LayoutElement));
        slotGO.transform.SetParent(parent, false);

        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(48, 52);

        LayoutElement slotLE = slotGO.GetComponent<LayoutElement>();
        slotLE.minWidth = 48f;
        slotLE.preferredWidth = 48f;
        slotLE.minHeight = 52f;
        slotLE.preferredHeight = 52f;

        Image slotBg = slotGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(slotBg, "Comp_Statue_ReqSlot", sliced: true, Color.white))
        {
            Sprite slotSp = DungeonUIAssetHelper.GetSlotSprite();
            if (slotSp != null)
            {
                slotBg.sprite = slotSp;
                slotBg.type = Image.Type.Sliced;
                slotBg.color = Color.white;
            }
            else
            {
                slotBg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
            }
        }

        Outline outline = slotGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.45f, 0.3f, 0.6f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Item Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0, 7);
        iconRT.sizeDelta = new Vector2(26, 26);

        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Amount Progress Text
        GameObject countGO = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGO.transform.SetParent(slotGO.transform, false);
        RectTransform countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0, 0);
        countRT.anchorMax = new Vector2(1, 0);
        countRT.pivot = new Vector2(0.5f, 0);
        countRT.anchoredPosition = new Vector2(0, 3);
        countRT.sizeDelta = new Vector2(-4, 15);

        TextMeshProUGUI countTMP = countGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(countTMP);
        countTMP.fontSize = 10;
        countTMP.fontStyle = FontStyles.Bold;
        countTMP.alignment = TextAlignmentOptions.Center;
        countTMP.raycastTarget = false;

        // Checkmark indicator
        GameObject checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(TextMeshProUGUI));
        checkGO.transform.SetParent(slotGO.transform, false);
        RectTransform checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(1, 1);
        checkRT.anchorMax = new Vector2(1, 1);
        checkRT.pivot = new Vector2(1, 1);
        checkRT.anchoredPosition = new Vector2(-2, -2);
        checkRT.sizeDelta = new Vector2(16, 16);

        TextMeshProUGUI checkTMP = checkGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(checkTMP);
        checkTMP.fontSize = 10;
        checkTMP.fontStyle = FontStyles.Bold;
        checkTMP.alignment = TextAlignmentOptions.Center;
        checkTMP.text = "<color=#55FF88>[V]</color>";
        checkTMP.raycastTarget = false;
        checkGO.SetActive(false);

        // Bind references directly to OfferingSlot component (zero reflection overhead)
        OfferingSlot slotComp = slotGO.GetComponent<OfferingSlot>();
        slotComp.BindReferences(slotBg, iconImg, countTMP, checkGO, outline);

        slotComp.Setup(off, reqIndex, req);
        activeOfferingSlots.Add(slotComp);
    }

    private void BuildBackpackSlots(ItemContainer container)
    {
        if (container == null)
        {
            if (backpackContainer != null)
            {
                container = backpackContainer;
            }
            else
            {
                InventoryPanel invPanel = InventoryPanel.Instance != null ? InventoryPanel.Instance : FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
                if (invPanel != null && invPanel.ItemContainer != null) container = invPanel.ItemContainer;
            }
            if (container == null)
            {
                PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
                if (ps != null && ps.itemContainer != null) container = ps.itemContainer;
            }
            if (container != null) backpackContainer = container;
        }

        if (backpackSlotsParent == null || container == null) return;

        EnsureSlotPrefab();
        if (slotPrefab == null)
        {
            slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
        }

        Canvas currentCanvas = GetCanvas();

        bool allSlotsValid = backpackSlots.Count == container.maxSlots;
        if (allSlotsValid)
        {
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                if (backpackSlots[i] == null)
                {
                    allSlotsValid = false;
                    break;
                }
            }
        }

        if (allSlotsValid)
        {
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                backpackSlots[i].gameObject.SetActive(true);
                backpackSlots[i].transform.localScale = Vector3.one;
                backpackSlots[i].SetSlotData(container, i);
                backpackSlots[i].SetCanvas(currentCanvas);
            }
            RefreshBackpackSlots();
            return;
        }

        InventoryButton[] existing = backpackSlotsParent.GetComponentsInChildren<InventoryButton>(true);
        if (existing != null && existing.Length >= container.maxSlots)
        {
            backpackSlots.Clear();
            for (int i = 0; i < container.maxSlots; i++)
            {
                existing[i].gameObject.SetActive(true);
                existing[i].transform.localScale = Vector3.one;
                existing[i].SetSlotData(container, i);
                existing[i].SetCanvas(currentCanvas);
                backpackSlots.Add(existing[i]);
            }
            for (int i = container.maxSlots; i < existing.Length; i++)
            {
                existing[i].gameObject.SetActive(false);
            }
            RefreshBackpackSlots();
            return;
        }

        for (int i = 0; i < backpackSlots.Count; i++) { if (backpackSlots[i] != null) { backpackSlots[i].transform.SetParent(null); Destroy(backpackSlots[i].gameObject); } }
        backpackSlots.Clear();

        for (int i = 0; i < container.maxSlots; i++)
        {
            if (slotPrefab == null) break;
            GameObject slotObj = Instantiate(slotPrefab, backpackSlotsParent, false);

            slotObj.name = $"Slot_{i}";
            slotObj.SetActive(true);
            slotObj.transform.localScale = Vector3.one;

            InventoryButton btn = slotObj.GetComponent<InventoryButton>();
            if (btn == null) btn = slotObj.AddComponent<InventoryButton>();

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            backpackSlots.Add(btn);
        }

        RefreshBackpackSlots();
    }

    private void RefreshBackpackSlots()
    {
        if (backpackContainer == null) return;

        for (int i = 0; i < backpackSlots.Count; i++)
        {
            if (i < backpackContainer.itemSlots.Length &&
                backpackContainer.itemSlots[i] != null &&
                backpackContainer.itemSlots[i].itemData != null)
            {
                backpackSlots[i].SetItem(
                    backpackContainer.itemSlots[i].itemData,
                    backpackContainer.itemSlots[i].amount
                );
            }
            else
            {
                backpackSlots[i].ClearItem();
            }
        }
    }

    private void EnsureSlotPrefab()
    {
        if (slotPrefab != null) return;

        InventoryPanel invPanel = InventoryPanel.Instance != null ? InventoryPanel.Instance : FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (invPanel != null)
        {
            InventoryButton btn = invPanel.GetComponentInChildren<InventoryButton>(true);
            if (btn != null)
            {
                slotPrefab = btn.gameObject;
                return;
            }
        }

        slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
    }

    private Canvas GetCanvas()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        return canvas;
    }

    private static string GetStatDisplayName(Offering.RewardStat stat)
    {
        return stat switch
        {
            Offering.RewardStat.ATK => "Công",
            Offering.RewardStat.DEF => "Thủ",
            Offering.RewardStat.Speed => "Tốc Độ",
            Offering.RewardStat.MaxHP => "Máu",
            _ => "Chỉ Số"
        };
    }

    private static string GetOfferingDisplayName(Offering off)
    {
        if (off == null) return "Offering";
        if (!string.IsNullOrEmpty(off.offeringName))
        {
            return off.offeringName;
        }

        string tierRoman = off.tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            7 => "VII",
            8 => "VIII",
            9 => "IX",
            10 => "X",
            _ => off.tier.ToString()
        };

        return off.rewardStat switch
        {
            Offering.RewardStat.ATK => $"Lễ Vật Chiến Binh {tierRoman}",
            Offering.RewardStat.DEF => $"Lễ Vật Hộ Vệ {tierRoman}",
            Offering.RewardStat.Speed => $"Lễ Vật Nhanh Nhẹn {tierRoman}",
            Offering.RewardStat.MaxHP => $"Lễ Vật Sinh Mệnh {tierRoman}",
            _ => $"Lễ Vật {tierRoman}"
        };
    }

    private static Color GetStatBadgeColor(Offering.RewardStat stat)
    {
        switch (stat)
        {
            case Offering.RewardStat.ATK:
                return new Color(0.65f, 0.2f, 0.2f, 0.95f);
            case Offering.RewardStat.DEF:
                return new Color(0.2f, 0.35f, 0.65f, 0.95f);
            case Offering.RewardStat.Speed:
                return new Color(0.65f, 0.55f, 0.15f, 0.95f);
            case Offering.RewardStat.MaxHP:
                return new Color(0.18f, 0.62f, 0.32f, 0.95f);
            default:
                return new Color(0.3f, 0.3f, 0.35f, 0.95f);
        }
    }

    private int GetTotalItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null || container.itemSlots == null) return 0;
        int count = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i] != null && ItemContainer.IsItemMatch(container.itemSlots[i].itemData, item))
            {
                count += container.itemSlots[i].amount;
            }
        }
        return count;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
