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
    public static StatueUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<StatueUI>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    Canvas c = FindFirstObjectByType<Canvas>();
                    if (c != null)
                    {
                        GameObject go = new GameObject("StatueUI");
                        go.transform.SetParent(c.transform, false);
                        _instance = go.AddComponent<StatueUI>();
                    }
                }
            }
            return _instance;
        }
        private set => _instance = value;
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

    [Header("Blessing UI (Phước Lành & Hướng Build)")]
    [SerializeField] private TextMeshProUGUI blessingStatusText;
    [SerializeField] private Button blessingSelectButton;
    [SerializeField] private TextMeshProUGUI blessingButtonText;
    private GameObject blessingModalGO;

    private Statue currentStatue;
    private List<Offering> currentOfferings;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();
    private readonly List<OfferingSlot> activeOfferingSlots = new List<OfferingSlot>();

    private bool isExplicitlyOpened = false;
    public bool IsOpen => isExplicitlyOpened && statuePanel != null && statuePanel.activeSelf;
    public ItemContainer PlayerBackpack => backpackContainer;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        // Only hide on start if not already opened by player interaction
        if (!isExplicitlyOpened && statuePanel != null)
        {
            statuePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDestroy()
    {
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
        backpackContainer = backpack;

        EnsureUIBuilt();

        if (statuePanel != null)
        {
            statuePanel.SetActive(true);
            statuePanel.transform.SetAsLastSibling();
        }

        SubscribeEvents();

        BuildOfferingsList();
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

            SetFeedback($"<color=#FFD700>[BLESSING] DIVINE BLESSING! Completed {GetOfferingDisplayName(offering)}! (+{offering.rewardAmount} {offering.rewardStat} permanently)</color>");
            Debug.Log($"[StatueUI] Offering completed: {offering.offeringName}! Rewarded permanent +{offering.rewardAmount} {offering.rewardStat}");

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

        bool isDone = offering.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(offering.offeringKey));
        if (isDone)
        {
            SetFeedback("<color=#88FF88>This offering is already complete!</color>");
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
            if (available <= 0) continue;

            int deposit = Mathf.Min(available, needed);
            backpackContainer.RemoveItem(req.item, deposit);

            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.AddOfferingProgress(offering.offeringKey, i, deposit);
            }

            totalDeposited += deposit;
        }

        if (totalDeposited > 0)
        {
            backpackContainer.NotifyChange();
            SetFeedback($"<color=#FFD700>Quick-offered {totalDeposited} items to {GetOfferingDisplayName(offering)}!</color>");
            CheckOfferingCompletion(offering);
        }
        else
        {
            SetFeedback("<color=#FFAA55>No matching items in backpack for this offering.</color>");
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

        permanentStatsText.text = $"<b>Permanent Blessings:</b>   <color=#FF7777>ATK +{atk}</color>   |   <color=#77AAFF>DEF +{def}</color>   |   <color=#FFFF77>Speed +{spd}</color>";
    }

    #region Run Blessings UI (Phước Lành Thần Linh & Hướng Build)
    public void UpdateBlessingDisplay()
    {
        if (blessingStatusText == null) return;

        bool isStarter = ProgressionManager.Instance != null && ProgressionManager.Instance.IsStarterProtectionActive();
        BlessingType currentType = ProgressionManager.Instance != null ? ProgressionManager.Instance.activeBlessing : BlessingType.None;

        if (isStarter)
        {
            blessingStatusText.text = "<b><color=#55CCFF>[TÂN THỦ] 🛡️ Bảo Hộ Thần Linh:</color></b> Đang tự động bảo vệ hành trang (không rơi đồ khi thua tại Tầng 1-2)!";
            if (blessingButtonText != null) blessingButtonText.text = "✨ Đổi Phước Lành";
        }
        else if (currentType != BlessingType.None)
        {
            BlessingInfo info = BlessingDatabase.GetBlessingInfo(currentType);
            string hex = info != null ? ColorUtility.ToHtmlStringRGB(info.color) : "55CCFF";
            string title = info != null ? info.title : currentType.ToString();
            string icon = info != null ? info.iconSymbol : "✨";
            string desc = info != null ? info.description : "";
            blessingStatusText.text = $"<b><color=#{hex}>[HIỆN TẠI] {icon} {title}:</color></b> {desc} <i><color=#AAAAAA>(Mất khi qua ngày, thua, hoặc xong run)</color></i>";
            if (blessingButtonText != null) blessingButtonText.text = "✨ Đổi Phước Lành";
        }
        else
        {
            blessingStatusText.text = "<b><color=#FFCC44>[CHƯA NHẬN PHƯỚC LÀNH]</color></b> Hãy chọn 1 phước lành trợ chiến trước khi bước vào Dungeon!";
            if (blessingButtonText != null) blessingButtonText.text = "✨ Thỉnh Phước Lành";
        }
    }

    public void ShowBlessingSelectionModal()
    {
        CloseBlessingSelectionModal();

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null) return;

        // Container phủ modal
        blessingModalGO = new GameObject("BlessingModal", typeof(RectTransform), typeof(Image));
        blessingModalGO.transform.SetParent(parentCanvas.transform, false);
        blessingModalGO.transform.SetAsLastSibling();

        RectTransform modalRT = blessingModalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = Vector2.zero;
        modalRT.anchorMax = Vector2.one;
        modalRT.sizeDelta = Vector2.zero;

        Image modalOverlay = blessingModalGO.GetComponent<Image>();
        modalOverlay.color = new Color(0.02f, 0.03f, 0.05f, 0.85f);

        // Hộp thoại trung tâm
        GameObject boxGO = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        boxGO.transform.SetParent(blessingModalGO.transform, false);
        RectTransform boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(960, 560);

        Image boxBg = boxGO.GetComponent<Image>();
        boxBg.color = new Color(0.09f, 0.11f, 0.15f, 0.98f);

        Outline boxOutline = boxGO.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.95f);
        boxOutline.effectDistance = new Vector2(2, -2);

        // Header
        GameObject mTitleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        mTitleGO.transform.SetParent(boxGO.transform, false);
        RectTransform mTitleRT = mTitleGO.GetComponent<RectTransform>();
        mTitleRT.anchorMin = new Vector2(0, 1);
        mTitleRT.anchorMax = new Vector2(1, 1);
        mTitleRT.pivot = new Vector2(0.5f, 1);
        mTitleRT.anchoredPosition = new Vector2(0, -16);
        mTitleRT.sizeDelta = new Vector2(-60, 32);

        TextMeshProUGUI mTitleTMP = mTitleGO.GetComponent<TextMeshProUGUI>();
        mTitleTMP.fontSize = 21;
        mTitleTMP.fontStyle = FontStyles.Bold;
        mTitleTMP.alignment = TextAlignmentOptions.Center;
        mTitleTMP.text = "PHƯỚC LÀNH THẦN LINH (CHỌN HƯỚNG BUILD CHIẾN THUẬT)";
        mTitleTMP.color = new Color(1f, 0.88f, 0.45f);

        GameObject mSubGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        mSubGO.transform.SetParent(boxGO.transform, false);
        RectTransform mSubRT = mSubGO.GetComponent<RectTransform>();
        mSubRT.anchorMin = new Vector2(0, 1);
        mSubRT.anchorMax = new Vector2(1, 1);
        mSubRT.pivot = new Vector2(0.5f, 1);
        mSubRT.anchoredPosition = new Vector2(0, -48);
        mSubRT.sizeDelta = new Vector2(-60, 32);

        TextMeshProUGUI mSubTMP = mSubGO.GetComponent<TextMeshProUGUI>();
        mSubTMP.fontSize = 11;
        mSubTMP.alignment = TextAlignmentOptions.Center;
        mSubTMP.text = "Tự do chọn lối chơi đã mở khóa để khắc chế quái vật tầng cao. Mở khóa thêm bằng cách Vượt Tầng Dungeon hoặc Cúng Dường tại Tượng Thần!";
        mSubTMP.color = new Color(0.75f, 0.85f, 0.95f);

        // Nút Đóng Modal
        GameObject mCloseBtnGO = new GameObject("BtnCloseModal", typeof(RectTransform), typeof(Image), typeof(Button));
        mCloseBtnGO.transform.SetParent(boxGO.transform, false);
        RectTransform mCloseRT = mCloseBtnGO.GetComponent<RectTransform>();
        mCloseRT.anchorMin = new Vector2(1, 1);
        mCloseRT.anchorMax = new Vector2(1, 1);
        mCloseRT.pivot = new Vector2(1, 1);
        mCloseRT.anchoredPosition = new Vector2(-14, -14);
        mCloseRT.sizeDelta = new Vector2(30, 30);

        Image mCloseBg = mCloseBtnGO.GetComponent<Image>();
        mCloseBg.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);

        Button mCloseBtn = mCloseBtnGO.GetComponent<Button>();
        mCloseBtn.onClick.AddListener(CloseBlessingSelectionModal);

        GameObject mCloseTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        mCloseTextGO.transform.SetParent(mCloseBtnGO.transform, false);
        TextMeshProUGUI mCloseTMP = mCloseTextGO.GetComponent<TextMeshProUGUI>();
        mCloseTMP.fontSize = 16;
        mCloseTMP.fontStyle = FontStyles.Bold;
        mCloseTMP.alignment = TextAlignmentOptions.Center;
        mCloseTMP.text = "X";
        mCloseTMP.color = Color.white;
        mCloseTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);

        // Khung chứa 8 Card dạng Grid 2 hàng x 4 cột
        GameObject cardsContainer = new GameObject("CardsContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        cardsContainer.transform.SetParent(boxGO.transform, false);
        RectTransform cRT = cardsContainer.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.anchoredPosition = new Vector2(0, -28);
        cRT.sizeDelta = new Vector2(-40, -96);

        GridLayoutGroup glg = cardsContainer.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(215, 210);
        glg.spacing = new Vector2(14, 12);
        glg.padding = new RectOffset(16, 16, 6, 6);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        glg.childAlignment = TextAnchor.UpperCenter;

        // Hiển thị toàn bộ danh sách 8 phước lành (7 hướng build + Bảo hộ không rơi đồ)
        List<BlessingInfo> allBlessings = BlessingDatabase.GetAllBlessings();
        for (int i = 0; i < allBlessings.Count; i++)
        {
            BlessingInfo info = allBlessings[i];
            CreateBlessingCard(info, cardsContainer.transform);
        }
    }

    private void CreateBlessingCard(BlessingInfo info, Transform parent)
    {
        if (info == null) return;

        bool isUnlocked = ProgressionManager.Instance != null ? ProgressionManager.Instance.IsBlessingUnlocked(info.type) : true;
        bool isCurrentActive = isUnlocked && ProgressionManager.Instance != null && ProgressionManager.Instance.activeBlessing == info.type;

        GameObject cardGO = new GameObject($"Card_{info.type}", typeof(RectTransform), typeof(Image));
        cardGO.transform.SetParent(parent, false);

        Image cardBg = cardGO.GetComponent<Image>();
        if (!isUnlocked)
        {
            cardBg.color = new Color(0.07f, 0.08f, 0.11f, 0.88f);
        }
        else
        {
            cardBg.color = isCurrentActive ? new Color(0.16f, 0.22f, 0.32f, 0.98f) : new Color(0.11f, 0.13f, 0.18f, 0.95f);
        }

        Outline cardBorder = cardGO.AddComponent<Outline>();
        if (!isUnlocked)
        {
            cardBorder.effectColor = new Color(0.35f, 0.38f, 0.45f, 0.5f);
            cardBorder.effectDistance = new Vector2(1.5f, -1.5f);
        }
        else
        {
            cardBorder.effectColor = isCurrentActive ? new Color(1f, 0.85f, 0.3f, 1f) : info.color;
            cardBorder.effectDistance = isCurrentActive ? new Vector2(3, -3) : new Vector2(1.5f, -1.5f);
        }

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(TextMeshProUGUI));
        iconGO.transform.SetParent(cardGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 1);
        iconRT.anchorMax = new Vector2(0.5f, 1);
        iconRT.pivot = new Vector2(0.5f, 1);
        iconRT.anchoredPosition = new Vector2(0, -10);
        iconRT.sizeDelta = new Vector2(50, 36);

        TextMeshProUGUI iconTMP = iconGO.GetComponent<TextMeshProUGUI>();
        iconTMP.fontSize = 28;
        iconTMP.alignment = TextAlignmentOptions.Center;
        iconTMP.text = isUnlocked ? info.iconSymbol : "🔒";

        // Title
        GameObject tGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        tGO.transform.SetParent(cardGO.transform, false);
        RectTransform tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 1);
        tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1);
        tRT.anchoredPosition = new Vector2(0, -48);
        tRT.sizeDelta = new Vector2(-12, 26);

        TextMeshProUGUI tTMP = tGO.GetComponent<TextMeshProUGUI>();
        tTMP.fontSize = 12.5f;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.alignment = TextAlignmentOptions.Center;
        if (!isUnlocked)
        {
            tTMP.text = $"<color=#888888>{info.title}</color> <color=#FF6666>[KHÓA]</color>";
            tTMP.color = new Color(0.6f, 0.62f, 0.68f);
        }
        else
        {
            tTMP.text = isCurrentActive ? $"{info.title} <color=#FFDD55>[ĐANG CHỌN]</color>" : info.title;
            tTMP.color = isCurrentActive ? new Color(1f, 0.85f, 0.4f) : info.color;
        }

        // Description
        GameObject descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(cardGO.transform, false);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.anchoredPosition = new Vector2(0, -4);
        descRT.sizeDelta = new Vector2(-16, -120);

        TextMeshProUGUI descTMP = descGO.GetComponent<TextMeshProUGUI>();
        descTMP.fontSize = 9.5f;
        descTMP.lineSpacing = 1.5f;
        descTMP.alignment = TextAlignmentOptions.TopLeft;

        if (!isUnlocked)
        {
            descTMP.text = $"{info.description}\n\n<color=#FFB833><b>🔓 Điều kiện mở khóa:</b>\n{info.unlockRequirement}</color>";
            descTMP.color = new Color(0.7f, 0.72f, 0.78f);
        }
        else
        {
            descTMP.text = info.description;
            descTMP.color = new Color(0.85f, 0.9f, 0.98f);
        }

        // Select Button
        GameObject btnGO = new GameObject("BtnSelect", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 0);
        btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(0.5f, 0);
        btnRT.anchoredPosition = new Vector2(0, 8);
        btnRT.sizeDelta = new Vector2(-18, 30);

        Image btnBg = btnGO.GetComponent<Image>();
        Button btn = btnGO.GetComponent<Button>();

        if (!isUnlocked)
        {
            btnBg.color = new Color(0.2f, 0.22f, 0.26f, 0.7f);
            btn.interactable = false;
        }
        else
        {
            btnBg.color = isCurrentActive ? new Color(0.2f, 0.5f, 0.7f, 0.95f) : new Color(0.22f, 0.55f, 0.35f, 0.95f);
            btn.interactable = !isCurrentActive;
            btn.onClick.AddListener(() =>
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.SetBlessing(info.type);
                }
                CloseBlessingSelectionModal();
                UpdateBlessingDisplay();
                SetFeedback($"Đã kích hoạt lối chơi: {info.title}!");
            });
        }

        GameObject btnTxtGO = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTxtTMP = btnTxtGO.GetComponent<TextMeshProUGUI>();
        btnTxtTMP.fontSize = 11;
        btnTxtTMP.fontStyle = FontStyles.Bold;
        btnTxtTMP.alignment = TextAlignmentOptions.Center;

        if (!isUnlocked)
        {
            btnTxtTMP.text = "🔒 CHƯA MỞ KHÓA";
            btnTxtTMP.color = new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            btnTxtTMP.text = isCurrentActive ? "ĐANG SỬ DỤNG" : "CHỌN LỐI CHƠI NÀY";
            btnTxtTMP.color = Color.white;
        }
    }

    public void CloseBlessingSelectionModal()
    {
        if (blessingModalGO != null)
        {
            Destroy(blessingModalGO);
            blessingModalGO = null;
        }
    }
    #endregion

    private void BuildOfferingsList()
    {
        if (offeringsContainer == null || currentOfferings == null) return;

        activeOfferingSlots.Clear();

        foreach (Transform child in offeringsContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentOfferings.Count; i++)
        {
            Offering off = currentOfferings[i];
            if (off == null) continue;

            CreateOfferingCard(off, offeringsContainer);
        }
    }

    private void CreateOfferingCard(Offering off, Transform parent)
    {
        bool isDone = off.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(off.offeringKey));

        // Card Root
        GameObject cardGO = new GameObject($"Card_{off.offeringKey}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardGO.transform.SetParent(parent, false);

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(490, 115);

        Image cardBg = cardGO.GetComponent<Image>();
        cardBg.color = isDone ? new Color(0.12f, 0.22f, 0.16f, 0.95f) : new Color(0.14f, 0.16f, 0.22f, 0.95f);

        Outline outline = cardGO.AddComponent<Outline>();
        outline.effectColor = isDone ? new Color(0.25f, 0.85f, 0.45f, 0.7f) : new Color(0.45f, 0.4f, 0.3f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header Row (Name + Reward Badge + Completed Badge + Quick Offer Button)
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        headerGO.transform.SetParent(cardGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.sizeDelta = new Vector2(0, 26);

        HorizontalLayoutGroup hlg = headerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Title
        GameObject nameGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(headerGO.transform, false);
        TextMeshProUGUI titleTMP = nameGO.GetComponent<TextMeshProUGUI>();
        titleTMP.fontSize = 13;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.text = GetOfferingDisplayName(off);
        titleTMP.color = isDone ? new Color(0.4f, 1f, 0.6f) : new Color(1f, 0.88f, 0.4f);
        nameGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 26);

        // Reward Badge
        GameObject badgeGO = new GameObject("RewardBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        badgeGO.transform.SetParent(headerGO.transform, false);
        badgeGO.GetComponent<Image>().color = GetStatBadgeColor(off.rewardStat);
        badgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 22);

        GameObject badgeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTextGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTMP = badgeTextGO.GetComponent<TextMeshProUGUI>();
        badgeTMP.fontSize = 11;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.text = $"+{off.rewardAmount} {off.rewardStat}";
        badgeTMP.color = Color.white;
        badgeTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 22);

        // Status or Quick Offer Button
        if (isDone)
        {
            GameObject doneGO = new GameObject("DoneBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            doneGO.transform.SetParent(headerGO.transform, false);
            TextMeshProUGUI doneTMP = doneGO.GetComponent<TextMeshProUGUI>();
            doneTMP.fontSize = 12;
            doneTMP.fontStyle = FontStyles.Bold;
            doneTMP.text = "[COMPLETED]";
            doneTMP.color = new Color(0.35f, 1f, 0.5f);
            doneGO.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 26);
        }
        else
        {
            GameObject btnGO = new GameObject("BtnQuickOffer", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(headerGO.transform, false);
            btnGO.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.35f, 0.95f);
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 24);

            Button btn = btnGO.GetComponent<Button>();
            Offering offRef = off;
            btn.onClick.AddListener(() => QuickOffer(offRef));

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
            btnTMP.fontSize = 11;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.text = "Quick Offer";
            btnTMP.color = Color.white;
            btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 24);
        }

        // Requirements Row
        GameObject reqsRowGO = new GameObject("RequirementsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        reqsRowGO.transform.SetParent(cardGO.transform, false);
        RectTransform reqsRowRT = reqsRowGO.GetComponent<RectTransform>();
        reqsRowRT.sizeDelta = new Vector2(0, 68);

        HorizontalLayoutGroup reqsHLG = reqsRowGO.GetComponent<HorizontalLayoutGroup>();
        reqsHLG.spacing = 14;
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
        GameObject slotGO = new GameObject($"ReqSlot_{reqIndex}", typeof(RectTransform), typeof(Image), typeof(OfferingSlot));
        slotGO.transform.SetParent(parent, false);

        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(62, 62);

        Image slotBg = slotGO.GetComponent<Image>();
        slotBg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

        Outline outline = slotGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.45f, 0.3f, 0.6f);
        outline.effectDistance = new Vector2(1, -1);

        // Item Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0, 4);
        iconRT.sizeDelta = new Vector2(40, 40);

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
        countRT.anchoredPosition = new Vector2(0, 2);
        countRT.sizeDelta = new Vector2(0, 16);

        TextMeshProUGUI countTMP = countGO.GetComponent<TextMeshProUGUI>();
        countTMP.fontSize = 11;
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
        checkRT.sizeDelta = new Vector2(18, 18);

        TextMeshProUGUI checkTMP = checkGO.GetComponent<TextMeshProUGUI>();
        checkTMP.fontSize = 14;
        checkTMP.fontStyle = FontStyles.Bold;
        checkTMP.alignment = TextAlignmentOptions.Center;
        checkTMP.text = "<color=#55FF88>OK</color>";
        checkTMP.raycastTarget = false;
        checkGO.SetActive(false);

        // Bind references to OfferingSlot component via reflection/fields
        OfferingSlot slotComp = slotGO.GetComponent<OfferingSlot>();
        SetPrivateField(slotComp, "slotBackground", slotBg);
        SetPrivateField(slotComp, "itemIcon", iconImg);
        SetPrivateField(slotComp, "countText", countTMP);
        SetPrivateField(slotComp, "checkmarkObj", checkGO);
        SetPrivateField(slotComp, "outline", outline);

        slotComp.Setup(off, reqIndex, req);
        activeOfferingSlots.Add(slotComp);
    }

    private void BuildBackpackSlots(ItemContainer container)
    {
        if (backpackSlotsParent == null || container == null) return;

        EnsureSlotPrefab();

        Canvas currentCanvas = GetCanvas();

        if (backpackSlots.Count == container.maxSlots && backpackSlots.TrueForAll(s => s != null))
        {
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                backpackSlots[i].gameObject.SetActive(true);
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

        foreach (InventoryButton slot in backpackSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        backpackSlots.Clear();

        if (slotPrefab == null)
        {
            Debug.LogWarning("[StatueUI] slotPrefab is null, cannot build backpack slots!");
            return;
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, backpackSlotsParent);
            slotObj.name = $"Slot_{i}";
            InventoryButton btn = slotObj.GetComponent<InventoryButton>();

            if (btn == null)
            {
                Destroy(slotObj);
                continue;
            }

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

        InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (invPanel != null)
        {
            InventoryButton btn = invPanel.GetComponentInChildren<InventoryButton>(true);
            if (btn != null)
            {
                slotPrefab = btn.gameObject;
            }
        }
    }

    private Canvas GetCanvas()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        return canvas;
    }

    private void EnsureUIBuilt()
    {
        if (statuePanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[StatueUI] No Canvas found in scene!");
            return;
        }

        // 1. Fullscreen Dark Overlay (blocks raycasts to world)
        GameObject overlayGO = new GameObject("StatuePanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        statuePanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.8f);

        // 2. Central Window Panel
        GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(960, 580);

        Image windowBg = windowGO.GetComponent<Image>();
        windowBg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

        Outline windowBorder = windowGO.AddComponent<Outline>();
        windowBorder.effectColor = new Color(0.75f, 0.62f, 0.28f, 0.9f);
        windowBorder.effectDistance = new Vector2(2, -2);

        // 3. Header Section
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(windowGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -14);
        titleRT.sizeDelta = new Vector2(-60, 32);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "STATUE OF THE ANCIENTS";
        titleText.color = new Color(1f, 0.85f, 0.35f);

        GameObject subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.transform.SetParent(windowGO.transform, false);
        RectTransform subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0, 1);
        subtitleRT.anchorMax = new Vector2(1, 1);
        subtitleRT.pivot = new Vector2(0.5f, 1);
        subtitleRT.anchoredPosition = new Vector2(0, -42);
        subtitleRT.sizeDelta = new Vector2(-60, 20);

        subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 11;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "Offer sacred items to the statue to gain permanent divine blessings.";
        subtitleText.color = new Color(0.75f, 0.8f, 0.9f);

        // 4. Feedback Label
        GameObject feedbackGO = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.transform.SetParent(windowGO.transform, false);
        RectTransform feedbackRT = feedbackGO.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 1);
        feedbackRT.anchorMax = new Vector2(1, 1);
        feedbackRT.pivot = new Vector2(0.5f, 1);
        feedbackRT.anchoredPosition = new Vector2(0, -62);
        feedbackRT.sizeDelta = new Vector2(-60, 22);

        feedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        feedbackText.fontSize = 12;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.text = "Drag items from backpack or click a requirement slot to offer.";
        feedbackText.color = new Color(0.85f, 0.92f, 1f);

        // 5. Close Button (X)
        GameObject closeGO = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(windowGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-12, -12);
        closeRT.sizeDelta = new Vector2(28, 28);

        Image closeBg = closeGO.GetComponent<Image>();
        closeBg.color = new Color(0.5f, 0.15f, 0.15f, 0.9f);

        closeButton = closeGO.GetComponent<Button>();
        closeButton.onClick.AddListener(Close);

        GameObject xTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI xTMP = xTextGO.GetComponent<TextMeshProUGUI>();
        xTMP.fontSize = 16;
        xTMP.fontStyle = FontStyles.Bold;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.text = "X";
        xTMP.color = Color.white;
        xTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);

        // 5b. Blessing Banner (Khu vực Phước Lành Thần Linh)
        GameObject blessingBannerGO = new GameObject("BlessingBanner", typeof(RectTransform), typeof(Image));
        blessingBannerGO.transform.SetParent(windowGO.transform, false);
        RectTransform bannerRT = blessingBannerGO.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0, 1);
        bannerRT.anchorMax = new Vector2(1, 1);
        bannerRT.pivot = new Vector2(0.5f, 1);
        bannerRT.anchoredPosition = new Vector2(0, -88);
        bannerRT.sizeDelta = new Vector2(-40, 32);

        Image bannerBg = blessingBannerGO.GetComponent<Image>();
        bannerBg.color = new Color(0.06f, 0.1f, 0.15f, 0.95f);

        Outline bannerOutline = blessingBannerGO.AddComponent<Outline>();
        bannerOutline.effectColor = new Color(0.3f, 0.65f, 0.95f, 0.7f);
        bannerOutline.effectDistance = new Vector2(1, -1);

        // Status Text bên trái
        GameObject bStatusGO = new GameObject("BlessingStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bStatusGO.transform.SetParent(blessingBannerGO.transform, false);
        RectTransform bStatusRT = bStatusGO.GetComponent<RectTransform>();
        bStatusRT.anchorMin = new Vector2(0, 0);
        bStatusRT.anchorMax = new Vector2(0.76f, 1);
        bStatusRT.offsetMin = new Vector2(12, 0);
        bStatusRT.offsetMax = new Vector2(-6, 0);

        blessingStatusText = bStatusGO.GetComponent<TextMeshProUGUI>();
        blessingStatusText.fontSize = 11;
        blessingStatusText.alignment = TextAlignmentOptions.MidlineLeft;
        blessingStatusText.color = new Color(0.9f, 0.95f, 1f);

        // Nút Thỉnh Phước Lành bên phải
        GameObject bBtnGO = new GameObject("BtnSelectBlessing", typeof(RectTransform), typeof(Image), typeof(Button));
        bBtnGO.transform.SetParent(blessingBannerGO.transform, false);
        RectTransform bBtnRT = bBtnGO.GetComponent<RectTransform>();
        bBtnRT.anchorMin = new Vector2(0.77f, 0);
        bBtnRT.anchorMax = new Vector2(1, 1);
        bBtnRT.offsetMin = new Vector2(4, 4);
        bBtnRT.offsetMax = new Vector2(-6, -4);

        Image bBtnBg = bBtnGO.GetComponent<Image>();
        bBtnBg.color = new Color(0.22f, 0.45f, 0.65f, 0.95f);

        blessingSelectButton = bBtnGO.GetComponent<Button>();
        blessingSelectButton.onClick.AddListener(ShowBlessingSelectionModal);

        GameObject bBtnTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        bBtnTxtGO.transform.SetParent(bBtnGO.transform, false);
        RectTransform bBtnTxtRT = bBtnTxtGO.GetComponent<RectTransform>();
        bBtnTxtRT.anchorMin = Vector2.zero;
        bBtnTxtRT.anchorMax = Vector2.one;
        bBtnTxtRT.sizeDelta = Vector2.zero;

        blessingButtonText = bBtnTxtGO.GetComponent<TextMeshProUGUI>();
        blessingButtonText.fontSize = 12;
        blessingButtonText.fontStyle = FontStyles.Bold;
        blessingButtonText.alignment = TextAlignmentOptions.Center;
        blessingButtonText.text = "✨ Thỉnh Phước Lành";
        blessingButtonText.color = Color.white;

        // 6. Left Column: Offerings Container
        GameObject offeringsColGO = new GameObject("OfferingsColumn", typeof(RectTransform), typeof(Image));
        offeringsColGO.transform.SetParent(windowGO.transform, false);
        RectTransform offColRT = offeringsColGO.GetComponent<RectTransform>();
        offColRT.anchorMin = new Vector2(0, 0);
        offColRT.anchorMax = new Vector2(0, 1);
        offColRT.pivot = new Vector2(0, 0.5f);
        offColRT.anchoredPosition = new Vector2(20, -28);
        offColRT.sizeDelta = new Vector2(520, -155);

        Image offColBg = offeringsColGO.GetComponent<Image>();
        offColBg.color = new Color(0.08f, 0.09f, 0.13f, 0.8f);

        GameObject offContentGO = new GameObject("OfferingsContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        offContentGO.transform.SetParent(offeringsColGO.transform, false);
        RectTransform offContentRT = offContentGO.GetComponent<RectTransform>();
        offContentRT.anchorMin = Vector2.zero;
        offContentRT.anchorMax = Vector2.one;
        offContentRT.sizeDelta = new Vector2(-16, -16);
        offContentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup offVLG = offContentGO.GetComponent<VerticalLayoutGroup>();
        offVLG.padding = new RectOffset(6, 6, 6, 6);
        offVLG.spacing = 10;
        offVLG.childControlWidth = true;
        offVLG.childControlHeight = false;
        offVLG.childForceExpandWidth = true;
        offVLG.childForceExpandHeight = false;

        offeringsContainer = offContentGO.transform;

        // 7. Right Column: Backpack Section
        GameObject backpackColGO = new GameObject("BackpackColumn", typeof(RectTransform), typeof(Image));
        backpackColGO.transform.SetParent(windowGO.transform, false);
        RectTransform bpColRT = backpackColGO.GetComponent<RectTransform>();
        bpColRT.anchorMin = new Vector2(1, 0);
        bpColRT.anchorMax = new Vector2(1, 1);
        bpColRT.pivot = new Vector2(1, 0.5f);
        bpColRT.anchoredPosition = new Vector2(-20, -28);
        bpColRT.sizeDelta = new Vector2(380, -155);

        Image bpColBg = backpackColGO.GetComponent<Image>();
        bpColBg.color = new Color(0.08f, 0.09f, 0.13f, 0.8f);

        // Backpack Column Header
        GameObject bpHeaderGO = new GameObject("BackpackHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpHeaderGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpHeaderRT = bpHeaderGO.GetComponent<RectTransform>();
        bpHeaderRT.anchorMin = new Vector2(0, 1);
        bpHeaderRT.anchorMax = new Vector2(1, 1);
        bpHeaderRT.pivot = new Vector2(0.5f, 1);
        bpHeaderRT.anchoredPosition = new Vector2(0, -6);
        bpHeaderRT.sizeDelta = new Vector2(-20, 22);

        TextMeshProUGUI bpHeaderTMP = bpHeaderGO.GetComponent<TextMeshProUGUI>();
        bpHeaderTMP.fontSize = 13;
        bpHeaderTMP.fontStyle = FontStyles.Bold;
        bpHeaderTMP.alignment = TextAlignmentOptions.Center;
        bpHeaderTMP.text = "PLAYER BACKPACK";
        bpHeaderTMP.color = new Color(0.9f, 0.92f, 0.98f);

        // Backpack Grid Parent
        GameObject bpGridGO = new GameObject("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup));
        bpGridGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpGridRT = bpGridGO.GetComponent<RectTransform>();
        bpGridRT.anchorMin = new Vector2(0, 0);
        bpGridRT.anchorMax = new Vector2(1, 1);
        bpGridRT.sizeDelta = new Vector2(-20, -42);
        bpGridRT.anchoredPosition = new Vector2(0, -12);

        GridLayoutGroup bpGLG = bpGridGO.GetComponent<GridLayoutGroup>();
        bpGLG.cellSize = new Vector2(50, 50);
        bpGLG.spacing = new Vector2(8, 8);
        bpGLG.padding = new RectOffset(8, 8, 8, 8);
        bpGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGLG.constraintCount = 6;
        bpGLG.childAlignment = TextAnchor.UpperCenter;

        backpackSlotsParent = bpGridGO.transform;

        // 8. Footer: Permanent Stats Summary
        GameObject footerGO = new GameObject("Footer", typeof(RectTransform), typeof(Image));
        footerGO.transform.SetParent(windowGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 10);
        footerRT.sizeDelta = new Vector2(-40, 32);

        Image footerBg = footerGO.GetComponent<Image>();
        footerBg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);

        GameObject statsTextGO = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statsTextGO.transform.SetParent(footerGO.transform, false);
        RectTransform statsTextRT = statsTextGO.GetComponent<RectTransform>();
        statsTextRT.anchorMin = Vector2.zero;
        statsTextRT.anchorMax = Vector2.one;
        statsTextRT.sizeDelta = Vector2.zero;

        permanentStatsText = statsTextGO.GetComponent<TextMeshProUGUI>();
        permanentStatsText.fontSize = 12;
        permanentStatsText.alignment = TextAlignmentOptions.Center;
        permanentStatsText.color = new Color(0.9f, 0.92f, 0.98f);
    }

    private static string GetOfferingDisplayName(Offering off)
    {
        if (off == null) return "Offering";
        switch (off.rewardStat)
        {
            case Offering.RewardStat.ATK:
                return $"Offering of Might (+{off.rewardAmount} ATK)";
            case Offering.RewardStat.DEF:
                return $"Offering of Protection (+{off.rewardAmount} DEF)";
            case Offering.RewardStat.Speed:
                return $"Offering of Swiftness (+{off.rewardAmount} Speed)";
            default:
                return string.IsNullOrEmpty(off.offeringName) ? "Statue Offering" : off.offeringName;
        }
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
            if (container.itemSlots[i] != null && container.itemSlots[i].itemData == item)
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
