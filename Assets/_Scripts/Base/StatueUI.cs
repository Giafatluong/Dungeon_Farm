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
            SetFeedback("<color=#FF6666>Cá»™t má»‘c nÃ y Ä‘ang bá»‹ khÃ³a! Cáº§n hoÃ n thÃ nh cá»™t má»‘c trÆ°á»›c Ä‘Ã³.</color>");
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
        if (blessingModalPanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null) return;

        // 1. Root Modal Overlay
        blessingModalPanel = new GameObject("BlessingModal", typeof(RectTransform), typeof(Image));
        blessingModalPanel.transform.SetParent(parentCanvas.transform, false);
        blessingModalPanel.transform.SetAsLastSibling();

        RectTransform modalRT = blessingModalPanel.GetComponent<RectTransform>();
        modalRT.anchorMin = Vector2.zero;
        modalRT.anchorMax = Vector2.one;
        modalRT.sizeDelta = Vector2.zero;

        Image modalOverlay = blessingModalPanel.GetComponent<Image>();
        modalOverlay.color = new Color(0.02f, 0.03f, 0.05f, 0.85f);

        // 2. DialogBox Window
        GameObject boxGO = new GameObject("DialogBox", typeof(RectTransform), typeof(Image));
        boxGO.transform.SetParent(blessingModalPanel.transform, false);
        RectTransform boxRT = boxGO.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(1180f, 860f);

        Image boxBg = boxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(boxBg, new Color(0.09f, 0.11f, 0.15f, 0.98f));

        Outline boxOutline = boxGO.AddComponent<Outline>();
        boxOutline.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.95f);
        boxOutline.effectDistance = new Vector2(2, -2);

        // Header Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(boxGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(-60, 32);

        TextMeshProUGUI titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleTMP);
        titleTMP.fontSize = 22;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.text = "DIVINE BLESSINGS (CHOOSE BUILD PATH)";
        titleTMP.color = new Color(1f, 0.88f, 0.45f);

        // Subtitle
        GameObject subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(boxGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0.5f, 1);
        subRT.anchoredPosition = new Vector2(0, -48);
        subRT.sizeDelta = new Vector2(-60, 24);

        TextMeshProUGUI subTMP = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subTMP);
        subTMP.fontSize = 11;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.text = "RÃª chuá»™t Ä‘á»ƒ phÃ³ng to & xem chi tiáº¿t â€¢ Click vÃ o tháº» bÃ i Ä‘á»ƒ kÃ­ch hoáº¡t hoáº·c chuyá»ƒn Ä‘á»•i PhÆ°á»›c LÃ nh";
        subTMP.color = new Color(0.72f, 0.78f, 0.88f);

        // Close Button
        GameObject closeBtnGO = new GameObject("BtnCloseModal", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(boxGO.transform, false);
        RectTransform closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 1);
        closeBtnRT.anchorMax = new Vector2(1, 1);
        closeBtnRT.pivot = new Vector2(1, 1);
        closeBtnRT.anchoredPosition = new Vector2(-18, -16);
        closeBtnRT.sizeDelta = new Vector2(36, 36);

        Image closeBtnBg = closeBtnGO.GetComponent<Image>();
        modalCloseButton = closeBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(modalCloseButton, closeBtnBg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        closeBtnBg.color = new Color(0.65f, 0.20f, 0.20f, 0.95f);
        modalCloseButton.onClick.AddListener(CloseBlessingSelectionModal);

        GameObject closeTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtGO.transform.SetParent(closeBtnGO.transform, false);
        TextMeshProUGUI closeTMP = closeTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(closeTMP);
        closeTMP.fontSize = 18;
        closeTMP.fontStyle = FontStyles.Bold;
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.text = "X";
        closeTMP.color = Color.white;
        closeTxtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 36);

        // 3. CardsContainer Grid
        GameObject containerGO = new GameObject("CardsContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        containerGO.transform.SetParent(boxGO.transform, false);
        RectTransform cRT = containerGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.anchoredPosition = new Vector2(0, -32f);
        cRT.sizeDelta = new Vector2(-48f, -96f);

        GridLayoutGroup glg = containerGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(246f, 366f);
        glg.spacing = new Vector2(16f, 14f);
        glg.padding = new RectOffset(16, 16, 8, 8);
        glg.childAlignment = TextAnchor.MiddleCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        // 4. Instantiate 8 Cards with BlessingCardUI
        blessingCards.Clear();
        BlessingType[] types = new BlessingType[]
        {
            BlessingType.KeepLootOnDefeat,
            BlessingType.RawVitality,
            BlessingType.SwiftMomentum,
            BlessingType.BastionStrike,
            BlessingType.BitterDecay,
            BlessingType.StarvingFury,
            BlessingType.GluttonousAegis,
            BlessingType.EndlessFeast
        };

        for (int i = 0; i < types.Length; i++)
        {
            GameObject cardGO = new GameObject($"Card_{types[i]}", typeof(RectTransform), typeof(Image), typeof(BlessingCardUI));
            cardGO.transform.SetParent(containerGO.transform, false);
            BlessingCardUI cardUI = cardGO.GetComponent<BlessingCardUI>();
            cardUI.blessingType = types[i];
            cardUI.EnsureFigmaCardStructure();
            blessingCards.Add(cardUI);
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
        SetFeedback($"<color=#55FF88>[PHÆ¯á»šC LÃ€NH ÄÃƒ Äá»”I] KÃ­ch hoáº¡t nhÃ¡nh: {info?.title ?? type.ToString()}!</color>");
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

        // 1. Navigation Category Tabs (Táº¥n CÃ´ng (ATK), PhÃ²ng Thá»§ (DEF), Tá»‘c Äá»™ (SPD), Sinh Má»‡nh (HP))
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

        CreateTabButton("Táº¥n CÃ´ng (ATK)", OfferingViewCategory.Might, tabsGO.transform);
        CreateTabButton("PhÃ²ng Thá»§ (DEF)", OfferingViewCategory.Protection, tabsGO.transform);
        CreateTabButton("Tá»‘c Äá»™ (SPD)", OfferingViewCategory.Swiftness, tabsGO.transform);
        CreateTabButton("Sinh Má»‡nh (HP)", OfferingViewCategory.Vitality, tabsGO.transform);
    }

    private void CreateTabButton(string label, OfferingViewCategory category, Transform parent)
    {
        GameObject btnGO = new GameObject($"Tab_{category}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 28f);

        bool isSelected = currentViewCategory == category;
        Image img = btnGO.GetComponent<Image>();
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
        DungeonUIAssetHelper.ApplyFont(tmp, withOutline: isSelected);
        tmp.fontSize = 10.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = label;
        tmp.color = isSelected ? Color.white : new Color(0.82f, 0.86f, 0.92f);
    }

    private void CreateOfferingCard(Offering off, Transform parent)
    {
        bool isDone = off.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(off.offeringKey));
        bool isUnlocked = off.IsUnlocked();

        // Card Root
        GameObject cardGO = new GameObject($"Card_{off.offeringKey}", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardGO.transform.SetParent(parent, false);

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(0, 86);

        LayoutElement cardLE = cardGO.AddComponent<LayoutElement>();
        cardLE.minHeight = 86f;
        cardLE.preferredHeight = 86f;
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
        cardBg.pixelsPerUnitMultiplier = 1.6f;

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
        vlg.padding = new RectOffset(12, 12, 8, 6);
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header Row (Name + Reward Badge + Completed Badge + Quick Offer Button)
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(cardGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.sizeDelta = new Vector2(0, 20);

        LayoutElement headerLE = headerGO.GetComponent<LayoutElement>();
        headerLE.minHeight = 20f;
        headerLE.preferredHeight = 20f;

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
        titleTMP.text = GetOfferingDisplayName(off);
        if (isDone)
            titleTMP.color = new Color(0.4f, 1f, 0.6f);
        else if (!isUnlocked)
            titleTMP.color = new Color(0.65f, 0.65f, 0.7f);
        else
            titleTMP.color = new Color(1f, 0.88f, 0.4f);
        nameGO.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 20);

        // Reward Badge
        GameObject badgeGO = new GameObject("RewardBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        badgeGO.transform.SetParent(headerGO.transform, false);
        Image badgeImg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(badgeImg, GetStatBadgeColor(off.rewardStat));
        badgeImg.pixelsPerUnitMultiplier = 2.6f;
        badgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 20);

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
        badgeTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 20);

        // Status or Quick Offer Button
        if (isDone)
        {
            GameObject doneGO = new GameObject("DoneBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            doneGO.transform.SetParent(headerGO.transform, false);
            TextMeshProUGUI doneTMP = doneGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(doneTMP);
            doneTMP.fontSize = 11;
            doneTMP.fontStyle = FontStyles.Bold;
            doneTMP.text = "[ÄÃƒ HOÃ€N THÃ€NH]";
            doneTMP.color = new Color(0.35f, 1f, 0.5f);
            doneGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 20);
        }
        else if (!isUnlocked)
        {
            GameObject lockBadgeGO = new GameObject("LockBadge", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            lockBadgeGO.transform.SetParent(headerGO.transform, false);
            Image lockBg = lockBadgeGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(lockBg, new Color(0.35f, 0.15f, 0.15f, 0.95f));
            lockBg.pixelsPerUnitMultiplier = 2.6f;
            lockBadgeGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 20);

            GameObject lockTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            lockTxtGO.transform.SetParent(lockBadgeGO.transform, false);
            TextMeshProUGUI lockTMP = lockTxtGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(lockTMP);
            lockTMP.fontSize = 9;
            lockTMP.fontStyle = FontStyles.Bold;
            lockTMP.alignment = TextAlignmentOptions.Center;
            lockTMP.text = off.tier == 5 ? "[Cáº¦N BOSS 1]" : (off.tier == 10 ? "[Cáº¦N BOSS Tá»I THÆ¯á»¢NG]" : "[CHÆ¯A Má»ž KHÃ“A]");
            lockTMP.color = new Color(1f, 0.65f, 0.65f);
            lockTxtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 20);
        }
        else
        {
            GameObject btnGO = new GameObject("BtnQuickOffer", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(headerGO.transform, false);
            Image btnBg = btnGO.GetComponent<Image>();
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 20);

            Button btn = btnGO.GetComponent<Button>();
            DungeonUIAssetHelper.StyleTactileButton(btn, btnBg, DungeonUIAssetHelper.ButtonStyle.Victory);
            btnBg.pixelsPerUnitMultiplier = 2.6f;
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
            btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(96, 20);
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
            Offering.RewardStat.ATK => $"Lá»… Váº­t Chiáº¿n Binh {tierRoman} (+{off.rewardAmount} ATK)",
            Offering.RewardStat.DEF => $"Lá»… Váº­t Há»™ Vá»‡ {tierRoman} (+{off.rewardAmount} DEF)",
            Offering.RewardStat.Speed => $"Lá»… Váº­t Nhanh Nháº¹n {tierRoman} (+{off.rewardAmount} Speed)",
            Offering.RewardStat.MaxHP => $"Lá»… Váº­t Sinh Má»‡nh {tierRoman} (+{off.rewardAmount} HP)",
            _ => $"Lá»… Váº­t {tierRoman} (+{off.rewardAmount})"
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
