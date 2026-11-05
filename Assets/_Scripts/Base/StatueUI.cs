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
    private GameObject blessingModalGO;

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
        AutoBindReferences();
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
                DamagePopupManager.ShowText(ps.transform.position + Vector3.up * 1.5f, $"+{offering.rewardAmount} {offering.rewardStat} PERMANENT!", new Color(1f, 0.85f, 0.2f), 4.5f);
            }

            if (currentStatue != null)
            {
                currentStatue.NotifyOfferingSuccess(offering);
            }

            SetFeedback($"<color=#FFD700>[BLESSING] DIVINE BLESSING! Completed {GetOfferingDisplayName(offering)}! (+{offering.rewardAmount} {offering.rewardStat} permanently)</color>");
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
            int deposit = 0;

            if (available > 0)
            {
                int backpackDeposit = Mathf.Min(available, needed);
                backpackContainer.RemoveItem(req.item, backpackDeposit);
                deposit += backpackDeposit;
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
        int hp = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentMaxHP : 0;

        PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
        int totalHp = ps != null ? ps.maxHealth : (35 + hp);
        int totalAtk = ps != null ? ps.GetCurrentATK() : (7 + atk);
        int totalDef = ps != null ? ps.GetCurrentDEF() : (1 + def);
        int totalSpd = ps != null ? ps.GetCurrentSpeed() : (14 + spd);

        permanentStatsText.text = $"<b>Blessed Permanent Stats:</b>   <color=#55FF88>HP: {totalHp} (+{hp})</color>   |   <color=#FF7777>ATK: {totalAtk} (+{atk})</color>   |   <color=#77AAFF>DEF: {totalDef} (+{def})</color>   |   <color=#FFFF77>Speed: {totalSpd} (+{spd})</color>";
    }

    #region Run Blessings UI (Divine Blessings & Build Paths)
    public void UpdateBlessingDisplay()
    {
        if (blessingStatusText == null) return;

        bool isStarter = ProgressionManager.Instance != null && ProgressionManager.Instance.IsStarterProtectionActive();
        BlessingType currentType = ProgressionManager.Instance != null ? ProgressionManager.Instance.activeBlessing : BlessingType.None;

        if (isStarter)
        {
            blessingStatusText.text = "<b><color=#55CCFF>[STARTER] [PROTECTION] Divine Protection:</color></b> Automatically protecting your inventory (items won't drop upon defeat on Floors 1-2)!";
            if (blessingButtonText != null) blessingButtonText.text = "Change Blessing";
        }
        else if (currentType != BlessingType.None)
        {
            BlessingInfo info = BlessingDatabase.GetBlessingInfo(currentType);
            string hex = info != null ? ColorUtility.ToHtmlStringRGB(info.color) : "55CCFF";
            string title = info != null ? info.title : currentType.ToString();
            string icon = info != null ? info.iconSymbol : "[BUFF]";
            string desc = info != null ? info.description : "";
            blessingStatusText.text = $"<b><color=#{hex}>[ACTIVE] {icon} {title}:</color></b> {desc} <i><color=#AAAAAA>(Expires at day end, defeat, or run clear)</color></i>";
            if (blessingButtonText != null) blessingButtonText.text = "Change Blessing";
        }
        else
        {
            blessingStatusText.text = "<b><color=#FFCC44>[NO BLESSING CHOSEN]</color></b> Choose a blessing to empower your run before entering the Dungeon!";
            if (blessingButtonText != null) blessingButtonText.text = "Choose Blessing";
        }
    }

    public void ShowBlessingSelectionModal()
    {
        CloseBlessingSelectionModal();

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null) return;

        // Modal Overlay Container
        blessingModalGO = new GameObject("BlessingModal", typeof(RectTransform), typeof(Image));
        blessingModalGO.transform.SetParent(parentCanvas.transform, false);
        blessingModalGO.transform.SetAsLastSibling();

        RectTransform modalRT = blessingModalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = Vector2.zero;
        modalRT.anchorMax = Vector2.one;
        modalRT.sizeDelta = Vector2.zero;

        Image modalOverlay = blessingModalGO.GetComponent<Image>();
        modalOverlay.color = new Color(0.02f, 0.03f, 0.05f, 0.85f);

        // Center Dialog Box
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
        mTitleTMP.text = "DIVINE BLESSINGS (CHOOSE BUILD PATH)";
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
        mSubTMP.text = "Freely choose unlocked build paths to counter enemies on higher floors. Unlock more by clearing Dungeon floors or offering at the Statue!";
        mSubTMP.color = new Color(0.75f, 0.85f, 0.95f);

        // Close Modal Button
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

        // Cards Container Grid (2 rows x 4 columns)
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

        // Display all 8 blessings (7 build paths + protection)
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
        iconTMP.fontSize = 13;
        iconTMP.fontStyle = FontStyles.Bold;
        iconTMP.alignment = TextAlignmentOptions.Center;
        iconTMP.text = isUnlocked ? info.iconSymbol : "[LOCKED]";

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
            tTMP.text = $"<color=#888888>{info.title}</color> <color=#FF6666>[LOCKED]</color>";
            tTMP.color = new Color(0.6f, 0.62f, 0.68f);
        }
        else
        {
            tTMP.text = isCurrentActive ? $"{info.title} <color=#FFDD55>[SELECTED]</color>" : info.title;
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
        descTMP.enableAutoSizing = true;
        descTMP.fontSizeMin = 8f;
        descTMP.fontSizeMax = 9.5f;
        descTMP.lineSpacing = 1.0f;
        descTMP.alignment = TextAlignmentOptions.TopLeft;

        if (!isUnlocked)
        {
            descTMP.text = $"{info.description}\n\n<color=#FFB833><b>[!] Unlock Requirement:</b>\n{info.unlockRequirement}</color>";
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
                SetFeedback($"Activated build path: {info.title}!");
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
            btnTxtTMP.text = "[LOCKED] NOT UNLOCKED";
            btnTxtTMP.color = new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            btnTxtTMP.text = isCurrentActive ? "CURRENTLY ACTIVE" : "CHOOSE THIS BUILD";
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

        // 1. Navigation Category Tabs (ATK, DEF, SPD, HP)
        BuildCategoryTabs(offeringsContainer);

        // 2. Render Offering Cards for the selected Category
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

        if (resetScroll)
        {
            ScrollRect sr = offeringsContainer.GetComponentInParent<ScrollRect>();
            if (sr != null)
            {
                sr.verticalNormalizedPosition = 1f;
            }
        }
    }

    private void BuildCategoryTabs(Transform parent)
    {
        GameObject tabsGO = new GameObject("CategoryTabsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        tabsGO.transform.SetParent(parent, false);

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

        CreateTabButton("Tấn Công (ATK)", OfferingViewCategory.Might, tabsGO.transform);
        CreateTabButton("Phòng Thủ (DEF)", OfferingViewCategory.Protection, tabsGO.transform);
        CreateTabButton("Tốc Độ (SPD)", OfferingViewCategory.Swiftness, tabsGO.transform);
        CreateTabButton("Sinh Mệnh (HP)", OfferingViewCategory.Vitality, tabsGO.transform);
    }

    private void CreateTabButton(string label, OfferingViewCategory category, Transform parent)
    {
        GameObject btnGO = new GameObject($"Tab_{category}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        bool isSelected = currentViewCategory == category;
        Image img = btnGO.GetComponent<Image>();
        if (isSelected)
        {
            DungeonUIAssetHelper.StyleSlicedFrame(img, new Color(0.85f, 0.7f, 0.2f, 0.95f));
        }
        else
        {
            DungeonUIAssetHelper.StyleSlicedPanel(img, DungeonUIAssetHelper.PanelStyle.Slate);
            img.color = new Color(0.18f, 0.2f, 0.26f, 0.95f);
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
        txtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 10;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = label;
        tmp.color = isSelected ? new Color(1f, 0.9f, 0.4f) : new Color(0.8f, 0.85f, 0.9f);
    }

    private void CreateOfferingCard(Offering off, Transform parent)
    {
        bool isDone = off.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(off.offeringKey));
        bool isUnlocked = off.IsUnlocked();

        // Card Root
        GameObject cardGO = new GameObject($"Card_{off.offeringKey}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        cardGO.transform.SetParent(parent, false);

        LayoutElement cardLE = cardGO.GetComponent<LayoutElement>();
        cardLE.minHeight = 92f;
        cardLE.preferredHeight = 92f;
        cardLE.flexibleHeight = 0;

        Image cardBg = cardGO.GetComponent<Image>();
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

        Outline outline = cardGO.AddComponent<Outline>();
        if (isDone)
        {
            outline.effectColor = new Color(0.25f, 0.85f, 0.45f, 0.7f);
        }
        else if (!isUnlocked)
        {
            outline.effectColor = new Color(0.35f, 0.35f, 0.4f, 0.4f);
        }
        else if (off.tier == 5)
        {
            outline.effectColor = new Color(1f, 0.78f, 0.2f, 0.95f);
        }
        else if (off.tier == 10)
        {
            outline.effectColor = new Color(0.9f, 0.45f, 1f, 0.95f);
        }
        else
        {
            outline.effectColor = new Color(0.65f, 0.55f, 0.25f, 0.6f);
        }
        outline.effectDistance = new Vector2(1, -1);

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 6, 6);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header Row (Name + Reward Badge + Completed Badge + Quick Offer Button)
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(cardGO.transform, false);

        LayoutElement headerLE = headerGO.GetComponent<LayoutElement>();
        headerLE.minHeight = 22f;
        headerLE.preferredHeight = 22f;

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
        DungeonUIAssetHelper.ApplyFont(titleTMP);
        titleTMP.fontSize = 11.5f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
        titleTMP.overflowMode = TextOverflowModes.Ellipsis;
        titleTMP.text = GetOfferingDisplayName(off);
        if (isDone)
            titleTMP.color = new Color(0.4f, 1f, 0.6f);
        else if (!isUnlocked)
            titleTMP.color = new Color(0.65f, 0.65f, 0.7f);
        else
            titleTMP.color = new Color(1f, 0.88f, 0.4f);
        nameGO.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 22);

        // Reward Badge
        GameObject badgeGO = new GameObject("RewardBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        badgeGO.transform.SetParent(headerGO.transform, false);
        Image badgeImg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(badgeImg, GetStatBadgeColor(off.rewardStat));
        badgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 20);

        HorizontalLayoutGroup badgeHLG = badgeGO.GetComponent<HorizontalLayoutGroup>();
        badgeHLG.childControlWidth = true;
        badgeHLG.childControlHeight = true;
        badgeHLG.childForceExpandWidth = true;
        badgeHLG.childForceExpandHeight = true;

        GameObject badgeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTextGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTMP = badgeTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(badgeTMP);
        badgeTMP.fontSize = 10;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.fontStyle = FontStyles.Bold;
        string statLabel = off.rewardStat == Offering.RewardStat.MaxHP ? "HP" : off.rewardStat.ToString();
        badgeTMP.text = $"+{off.rewardAmount} {statLabel}";
        badgeTMP.color = Color.white;

        // Status or Quick Offer Button
        if (isDone)
        {
            GameObject doneGO = new GameObject("DoneBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            doneGO.transform.SetParent(headerGO.transform, false);
            TextMeshProUGUI doneTMP = doneGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(doneTMP);
            doneTMP.fontSize = 11;
            doneTMP.fontStyle = FontStyles.Bold;
            doneTMP.text = "[ĐÃ HOÀN THÀNH]";
            doneTMP.color = new Color(0.35f, 1f, 0.5f);
            doneGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 22);
        }
        else if (!isUnlocked)
        {
            GameObject lockBadgeGO = new GameObject("LockBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            lockBadgeGO.transform.SetParent(headerGO.transform, false);
            Image lockBg = lockBadgeGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(lockBg, new Color(0.35f, 0.15f, 0.15f, 0.95f));
            lockBadgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 22);

            HorizontalLayoutGroup lockHLG = lockBadgeGO.GetComponent<HorizontalLayoutGroup>();
            lockHLG.childControlWidth = true;
            lockHLG.childControlHeight = true;
            lockHLG.childForceExpandWidth = true;
            lockHLG.childForceExpandHeight = true;

            GameObject lockTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            lockTxtGO.transform.SetParent(lockBadgeGO.transform, false);
            TextMeshProUGUI lockTMP = lockTxtGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(lockTMP);
            lockTMP.fontSize = 9;
            lockTMP.fontStyle = FontStyles.Bold;
            lockTMP.alignment = TextAlignmentOptions.Center;
            lockTMP.text = off.tier == 5 ? "[CẦN BOSS 1]" : (off.tier == 10 ? "[CẦN BOSS TỐI THƯỢNG]" : "[CHƯA MỞ KHÓA]");
            lockTMP.color = new Color(1f, 0.65f, 0.65f);
        }
        else
        {
            GameObject btnGO = new GameObject("BtnQuickOffer", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
            btnGO.transform.SetParent(headerGO.transform, false);
            Image btnBg = btnGO.GetComponent<Image>();
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 22);

            HorizontalLayoutGroup btnHLG = btnGO.GetComponent<HorizontalLayoutGroup>();
            btnHLG.childControlWidth = true;
            btnHLG.childControlHeight = true;
            btnHLG.childForceExpandWidth = true;
            btnHLG.childForceExpandHeight = true;

            Button btn = btnGO.GetComponent<Button>();
            DungeonUIAssetHelper.StyleTactileButton(btn, btnBg, DungeonUIAssetHelper.ButtonStyle.Victory);
            Offering offRef = off;
            btn.onClick.AddListener(() => QuickOffer(offRef));

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(btnTMP);
            btnTMP.fontSize = 10;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.text = "Quick Offer";
            btnTMP.color = Color.white;
        }

        // Requirements Row
        GameObject reqsRowGO = new GameObject("RequirementsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        reqsRowGO.transform.SetParent(cardGO.transform, false);

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
        slotRT.sizeDelta = new Vector2(48, 48);

        LayoutElement slotLE = slotGO.GetComponent<LayoutElement>();
        slotLE.minWidth = 48f;
        slotLE.preferredWidth = 48f;
        slotLE.minHeight = 48f;
        slotLE.preferredHeight = 48f;

        Image slotBg = slotGO.GetComponent<Image>();
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

        Outline outline = slotGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.45f, 0.3f, 0.6f);
        outline.effectDistance = new Vector2(1, -1);

        // Item Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0, 5);
        iconRT.sizeDelta = new Vector2(28, 28);

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
        countRT.anchoredPosition = new Vector2(0, 1);
        countRT.sizeDelta = new Vector2(0, 14);

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
            Offering.RewardStat.ATK => $"Lễ Vật Chiến Binh {tierRoman} (+{off.rewardAmount} ATK)",
            Offering.RewardStat.DEF => $"Lễ Vật Hộ Vệ {tierRoman} (+{off.rewardAmount} DEF)",
            Offering.RewardStat.Speed => $"Lễ Vật Nhanh Nhẹn {tierRoman} (+{off.rewardAmount} Speed)",
            Offering.RewardStat.MaxHP => $"Lễ Vật Sinh Mệnh {tierRoman} (+{off.rewardAmount} HP)",
            _ => $"Lễ Vật {tierRoman} (+{off.rewardAmount})"
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
