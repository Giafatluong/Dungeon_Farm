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
        if (_instance != null) return _instance;

        Canvas c = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (c != null)
        {
            GameObject go = new GameObject("StatueUI");
            go.transform.SetParent(c.transform, false);
            _instance = go.AddComponent<StatueUI>();
            _instance.canvas = c;
            return _instance;
        }

        return null;
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
        if (currentOfferings == null || currentOfferings.Count == 0)
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
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null && invPanel.ItemContainer != null)
            {
                backpackContainer = invPanel.ItemContainer;
            }
        }
        if (backpackContainer == null)
        {
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
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

        InventoryController.Instance?.CloseInventoryPanel();
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

            // Sync with PlayerStats and show celebratory popup
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
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

        PlayerStats ps = FindFirstObjectByType<PlayerStats>();
        int totalAtk = ps != null ? ps.GetCurrentATK() : (10 + atk);
        int totalDef = ps != null ? ps.GetCurrentDEF() : def;
        int totalSpd = ps != null ? ps.GetCurrentSpeed() : (10 + spd);

        permanentStatsText.text = $"<b>Blessed Permanent Stats:</b>   <color=#FF7777>ATK: {totalAtk} (+{atk})</color>   |   <color=#77AAFF>DEF: {totalDef} (+{def})</color>   |   <color=#FFFF77>Speed: {totalSpd} (+{spd})</color>";
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
        descTMP.lineSpacing = 1.5f;
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

    private void BuildOfferingsList()
    {
        if (offeringsContainer == null || currentOfferings == null) return;

        activeOfferingSlots.Clear();

        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in offeringsContainer) toDestroy.Add(child.gameObject);
        for (int i = 0; i < toDestroy.Count; i++) { toDestroy[i].transform.SetParent(null); Destroy(toDestroy[i]); }

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
        cardRT.sizeDelta = new Vector2(0, 116);

        Image cardBg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(cardBg, isDone ? DungeonUIAssetHelper.PanelStyle.Wood : DungeonUIAssetHelper.PanelStyle.Slate);

        Outline outline = cardGO.AddComponent<Outline>();
        outline.effectColor = isDone ? new Color(0.25f, 0.85f, 0.45f, 0.7f) : new Color(0.65f, 0.55f, 0.25f, 0.6f);
        outline.effectDistance = new Vector2(1, -1);

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 8;
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
        DungeonUIAssetHelper.ApplyFont(titleTMP);
        titleTMP.fontSize = 13;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.text = GetOfferingDisplayName(off);
        titleTMP.color = isDone ? new Color(0.4f, 1f, 0.6f) : new Color(1f, 0.88f, 0.4f);
        nameGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 26);

        // Reward Badge
        GameObject badgeGO = new GameObject("RewardBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        badgeGO.transform.SetParent(headerGO.transform, false);
        Image badgeImg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(badgeImg, GetStatBadgeColor(off.rewardStat));
        badgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 22);

        GameObject badgeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTextGO.transform.SetParent(badgeGO.transform, false);
        TextMeshProUGUI badgeTMP = badgeTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(badgeTMP);
        badgeTMP.fontSize = 11;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.text = $"+{off.rewardAmount} {off.rewardStat}";
        badgeTMP.color = Color.white;
        badgeTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 22);

        // Status or Quick Offer Button
        if (isDone)
        {
            GameObject doneGO = new GameObject("DoneBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            doneGO.transform.SetParent(headerGO.transform, false);
            TextMeshProUGUI doneTMP = doneGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(doneTMP);
            doneTMP.fontSize = 12;
            doneTMP.fontStyle = FontStyles.Bold;
            doneTMP.text = "[COMPLETED]";
            doneTMP.color = new Color(0.35f, 1f, 0.5f);
            doneGO.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 26);
        }
        else
        {
            GameObject btnGO = new GameObject("BtnQuickOffer", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(headerGO.transform, false);
            Image btnBg = btnGO.GetComponent<Image>();
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 24);

            Button btn = btnGO.GetComponent<Button>();
            DungeonUIAssetHelper.StyleTactileButton(btn, btnBg, DungeonUIAssetHelper.ButtonStyle.Victory);
            Offering offRef = off;
            btn.onClick.AddListener(() => QuickOffer(offRef));

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(btnTMP);
            btnTMP.fontSize = 11;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.text = "Quick Offer";
            btnTMP.color = Color.white;
            btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 24);
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
        iconRT.sizeDelta = new Vector2(38, 38);

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
        DungeonUIAssetHelper.ApplyFont(countTMP);
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
        DungeonUIAssetHelper.ApplyFont(checkTMP);
        checkTMP.fontSize = 12;
        checkTMP.fontStyle = FontStyles.Bold;
        checkTMP.alignment = TextAlignmentOptions.Center;
        checkTMP.text = "<color=#55FF88>[V]</color>";
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
        if (container == null)
        {
            if (backpackContainer != null)
            {
                container = backpackContainer;
            }
            else
            {
                InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
                if (invPanel != null && invPanel.ItemContainer != null) container = invPanel.ItemContainer;
            }
            if (container == null)
            {
                PlayerStats ps = FindFirstObjectByType<PlayerStats>();
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
            GameObject slotObj;
            if (slotPrefab != null)
            {
                slotObj = Instantiate(slotPrefab, backpackSlotsParent, false);
            }
            else
            {
                slotObj = CreateFallbackBackpackSlot(backpackSlotsParent, i);
            }

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

    private GameObject CreateFallbackBackpackSlot(Transform parent, int index)
    {
        GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(InventoryButton));
        slotObj.transform.SetParent(parent, false);

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(66, 66);

        Image bg = slotObj.GetComponent<Image>();
        Sprite slotSp = DungeonUIAssetHelper.GetSlotSprite();
        if (slotSp != null)
        {
            bg.sprite = slotSp;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.18f, 0.22f, 0.3f, 0.95f);
        }

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotObj.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.gameObject.SetActive(false);

        GameObject textGO = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(slotObj.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 0.45f);
        textRT.offsetMin = new Vector2(4, 2);
        textRT.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI txt = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(txt);
        txt.fontSize = 14;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.BottomRight;
        txt.color = Color.white;
        txt.gameObject.SetActive(false);

        InventoryButton btn = slotObj.GetComponent<InventoryButton>();
        btn.BindComponents(iconImg, txt);

        return slotObj;
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

    private void EnsureUIBuilt()
    {
        if (statuePanel != null)
        {
            if (offeringsContainer != null && offeringsContainer.GetComponentInParent<ScrollRect>() != null)
            {
                return;
            }
            Destroy(statuePanel);
            statuePanel = null;
        }

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[StatueUI] No Canvas found in scene!");
            return;
        }

        TMP_FontAsset font = null;
        TextMeshProUGUI sample = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (sample != null) font = sample.font;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Fullscreen Dark Overlay (blocks raycasts to world)
        GameObject overlayGO = new GameObject("StatuePanel", typeof(RectTransform), typeof(Image));
        overlayGO.layer = uiLayer;
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        statuePanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        // 2. Central Window Panel (1100 x 660)
        GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.layer = uiLayer;
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(1100, 660);
        windowRT.anchoredPosition = Vector2.zero;

        Image windowBg = windowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(windowBg, DungeonUIAssetHelper.PanelStyle.Wood);

        // 3. Header Section
        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(windowGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(-100, 32);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) titleText.font = font;
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "ANCIENT SHRINE (DIVINE BLESSING STATUE)";
        titleText.color = new Color(1f, 0.86f, 0.35f);

        // Subtitle
        GameObject subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.layer = uiLayer;
        subtitleGO.transform.SetParent(windowGO.transform, false);
        RectTransform subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0, 1);
        subtitleRT.anchorMax = new Vector2(1, 1);
        subtitleRT.pivot = new Vector2(0.5f, 1);
        subtitleRT.anchoredPosition = new Vector2(0, -48);
        subtitleRT.sizeDelta = new Vector2(-100, 20);

        subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) subtitleText.font = font;
        subtitleText.fontSize = 12;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "Make sacred offerings to gain permanent stats and unlock tactical build paths.";
        subtitleText.color = new Color(0.75f, 0.82f, 0.92f);

        // Sub-Header Bar (Blessing Banner)
        GameObject blessingBannerGO = new GameObject("BlessingBanner", typeof(RectTransform), typeof(Image));
        blessingBannerGO.layer = uiLayer;
        blessingBannerGO.transform.SetParent(windowGO.transform, false);
        RectTransform bannerRT = blessingBannerGO.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0, 1);
        bannerRT.anchorMax = new Vector2(1, 1);
        bannerRT.pivot = new Vector2(0.5f, 1);
        bannerRT.anchoredPosition = new Vector2(0, -74);
        bannerRT.sizeDelta = new Vector2(-48, 34);

        Image bannerBg = blessingBannerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(bannerBg, DungeonUIAssetHelper.PlaqueStyle.Gold);

        // Blessing Status Text (left side of bar)
        GameObject bStatusGO = new GameObject("BlessingStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bStatusGO.layer = uiLayer;
        bStatusGO.transform.SetParent(blessingBannerGO.transform, false);
        RectTransform bStatusRT = bStatusGO.GetComponent<RectTransform>();
        bStatusRT.anchorMin = new Vector2(0, 0);
        bStatusRT.anchorMax = new Vector2(0.79f, 1);
        bStatusRT.offsetMin = new Vector2(14, 0);
        bStatusRT.offsetMax = new Vector2(-6, 0);

        blessingStatusText = bStatusGO.GetComponent<TextMeshProUGUI>();
        if (font != null) blessingStatusText.font = font;
        blessingStatusText.fontSize = 11.5f;
        blessingStatusText.alignment = TextAlignmentOptions.MidlineLeft;
        blessingStatusText.color = new Color(0.9f, 0.95f, 1f);

        // Blessing Selection Button (right side of bar)
        GameObject bBtnGO = new GameObject("BtnSelectBlessing", typeof(RectTransform), typeof(Image), typeof(Button));
        bBtnGO.layer = uiLayer;
        bBtnGO.transform.SetParent(blessingBannerGO.transform, false);
        RectTransform bBtnRT = bBtnGO.GetComponent<RectTransform>();
        bBtnRT.anchorMin = new Vector2(0.80f, 0);
        bBtnRT.anchorMax = new Vector2(1, 1);
        bBtnRT.offsetMin = new Vector2(4, 3);
        bBtnRT.offsetMax = new Vector2(-6, -3);

        Image bBtnBg = bBtnGO.GetComponent<Image>();
        blessingSelectButton = bBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(blessingSelectButton, bBtnBg, DungeonUIAssetHelper.ButtonStyle.Victory);
        blessingSelectButton.onClick.RemoveAllListeners();
        blessingSelectButton.onClick.AddListener(ShowBlessingSelectionModal);

        GameObject bBtnTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        bBtnTxtGO.layer = uiLayer;
        bBtnTxtGO.transform.SetParent(bBtnGO.transform, false);
        RectTransform bBtnTxtRT = bBtnTxtGO.GetComponent<RectTransform>();
        bBtnTxtRT.anchorMin = Vector2.zero;
        bBtnTxtRT.anchorMax = Vector2.one;
        bBtnTxtRT.sizeDelta = Vector2.zero;

        blessingButtonText = bBtnTxtGO.GetComponent<TextMeshProUGUI>();
        if (font != null) blessingButtonText.font = font;
        blessingButtonText.fontSize = 12;
        blessingButtonText.fontStyle = FontStyles.Bold;
        blessingButtonText.alignment = TextAlignmentOptions.Center;
        blessingButtonText.text = "Change Build";
        blessingButtonText.color = Color.white;

        // Feedback / Hint Strip
        GameObject feedbackGO = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.layer = uiLayer;
        feedbackGO.transform.SetParent(windowGO.transform, false);
        RectTransform feedbackRT = feedbackGO.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 1);
        feedbackRT.anchorMax = new Vector2(1, 1);
        feedbackRT.pivot = new Vector2(0.5f, 1);
        feedbackRT.anchoredPosition = new Vector2(0, -112);
        feedbackRT.sizeDelta = new Vector2(-48, 20);

        feedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        if (font != null) feedbackText.font = font;
        feedbackText.fontSize = 12;
        feedbackText.fontStyle = FontStyles.Italic;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.text = "Drag and drop items from your backpack or click requirement slots to offer.";
        feedbackText.color = new Color(0.85f, 0.92f, 1f);

        // Close Button (X)
        GameObject closeGO = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.layer = uiLayer;
        closeGO.transform.SetParent(windowGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-14, -14);
        closeRT.sizeDelta = new Vector2(30, 30);

        Image closeBg = closeGO.GetComponent<Image>();
        closeButton = closeGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(closeButton, closeBg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        GameObject xTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.layer = uiLayer;
        xTextGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI xTMP = xTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) xTMP.font = font;
        xTMP.fontSize = 16;
        xTMP.fontStyle = FontStyles.Bold;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.text = "X";
        xTMP.color = Color.white;
        xTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);

        // 4. Columns:
        // Left Column: Offerings Container (610 wide, 465 high)
        GameObject offColGO = CreateColumnFrame(windowGO.transform, "OfferingsColumn", "DIVINE OFFERINGS LIST",
            new Vector2(24, -136), new Vector2(610, 465), new Vector2(0, 1), new Color(0.85f, 0.7f, 0.25f), font, uiLayer);
        offeringsContainer = CreateScrollableList(offColGO.transform, "OfferingsContent", new Color(0.85f, 0.65f, 0.2f), uiLayer);

        // Right Column: Backpack Section (420 wide, 465 high)
        GameObject bpColGO = CreateColumnFrame(windowGO.transform, "BackpackColumn", "PLAYER INVENTORY (BACKPACK)",
            new Vector2(-24, -136), new Vector2(420, 465), new Vector2(1, 1), new Color(0.2f, 0.7f, 0.95f), font, uiLayer);
        backpackSlotsParent = CreateScrollableGrid(bpColGO.transform, "BackpackSlots", 5, new Color(0.25f, 0.75f, 0.95f), uiLayer);

        // 5. Footer: Permanent Stats Summary (Docked at bottom)
        GameObject footerGO = new GameObject("Footer", typeof(RectTransform), typeof(Image));
        footerGO.layer = uiLayer;
        footerGO.transform.SetParent(windowGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 12);
        footerRT.sizeDelta = new Vector2(-48, 36);

        Image footerBg = footerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(footerBg, DungeonUIAssetHelper.PlaqueStyle.Wood);

        GameObject statsTextGO = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statsTextGO.layer = uiLayer;
        statsTextGO.transform.SetParent(footerGO.transform, false);
        RectTransform statsTextRT = statsTextGO.GetComponent<RectTransform>();
        statsTextRT.anchorMin = Vector2.zero;
        statsTextRT.anchorMax = Vector2.one;
        statsTextRT.sizeDelta = Vector2.zero;

        permanentStatsText = statsTextGO.GetComponent<TextMeshProUGUI>();
        if (font != null) permanentStatsText.font = font;
        permanentStatsText.fontSize = 13;
        permanentStatsText.fontStyle = FontStyles.Bold;
        permanentStatsText.alignment = TextAlignmentOptions.Center;
        permanentStatsText.color = new Color(0.9f, 0.95f, 1f);
    }

    private GameObject CreateColumnFrame(Transform parent, string name, string headerText, Vector2 pos, Vector2 size, Vector2 pivotAndAnchorX, Color headerColor, TMP_FontAsset font, int layer)
    {
        GameObject colGO = new GameObject(name, typeof(RectTransform), typeof(Image));
        colGO.layer = layer;
        colGO.transform.SetParent(parent, false);
        RectTransform rt = colGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(pivotAndAnchorX.x, 1);
        rt.anchorMax = new Vector2(pivotAndAnchorX.x, 1);
        rt.pivot = pivotAndAnchorX;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = colGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(img, DungeonUIAssetHelper.PanelStyle.Slate);

        // Header Text at top of column
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerGO.layer = layer;
        headerGO.transform.SetParent(colGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -10);
        headerRT.sizeDelta = new Vector2(-20, 26);

        TextMeshProUGUI tmp = headerGO.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = headerText;
        tmp.color = headerColor;

        return colGO;
    }

    private Transform CreateScrollableList(Transform parent, string name, Color handleColor, int layer)
    {
        // 1. ScrollView
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = layer;
        scrollGO.transform.SetParent(parent, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -40); // Starts below header

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // 2. Viewport + RectMask2D
        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = layer;
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-20, 0); // Leave space for scrollbar

        // 3. Content
        GameObject contentGO = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = layer;
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 8, 8);
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. Scrollbar
        CreateScrollbar(scrollGO.transform, scrollRect, vpRT, contentRT, handleColor, layer);

        return contentGO.transform;
    }

    private Transform CreateScrollableGrid(Transform parent, string name, int cols, Color handleColor, int layer)
    {
        // 1. ScrollView
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = layer;
        scrollGO.transform.SetParent(parent, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -40);

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // 2. Viewport + RectMask2D
        GameObject vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.layer = layer;
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-20, 0);

        // 3. Content Grid
        GameObject contentGO = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = layer;
        contentGO.transform.SetParent(vpGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        GridLayoutGroup glg = contentGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(66, 66);
        glg.spacing = new Vector2(8, 8);
        glg.padding = new RectOffset(6, 6, 8, 8);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = cols;
        glg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. Scrollbar
        CreateScrollbar(scrollGO.transform, scrollRect, vpRT, contentRT, handleColor, layer);

        return contentGO.transform;
    }

    private void CreateScrollbar(Transform parent, ScrollRect scrollRect, RectTransform viewport, RectTransform content, Color handleColor, int layer)
    {
        GameObject sbGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.layer = layer;
        sbGO.transform.SetParent(parent, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.anchoredPosition = Vector2.zero;
        sbRT.sizeDelta = new Vector2(14, 0);

        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

        Scrollbar scrollbar = sbGO.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject saGO = new GameObject("Sliding Area", typeof(RectTransform));
        saGO.layer = layer;
        saGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = saGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.offsetMin = new Vector2(2, 4);
        saRT.offsetMax = new Vector2(-2, -4);

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.layer = layer;
        handleGO.transform.SetParent(saGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        handleImg.color = handleColor;

        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
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
