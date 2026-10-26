using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the Wandering Merchant encounter in the Dungeon.
/// Allows players to trade rare recipes, barter seeds, and buy travel rations,
/// while viewing their real-time backpack inventory.
/// Styled using project fantasy pixel art assets (UI_Frame, UI_Slot, inventorySlot).
/// </summary>
public class MerchantUI : MonoBehaviour
{
    private static MerchantUI _instance;
    public static MerchantUI Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    public static MerchantUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        MerchantUI found = FindFirstObjectByType<MerchantUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        Canvas c = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (c != null)
        {
            GameObject go = new GameObject("MerchantUI");
            go.transform.SetParent(c.transform, false);
            _instance = go.AddComponent<MerchantUI>();
            _instance.canvas = c;
            return _instance;
        }

        return null;
    }

    [Header("Panels")]
    [SerializeField] public GameObject merchantPanel;
    [SerializeField] public Transform waresContainer;
    [SerializeField] public Transform backpackSlotsParent;

    [Header("References")]
    [SerializeField] public Canvas canvas;
    [SerializeField] public GameObject slotPrefab;

    [Header("Labels & Buttons")]
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI subtitleText;
    [SerializeField] public TextMeshProUGUI feedbackText;
    [SerializeField] public TextMeshProUGUI backpackHeaderTitle;
    [SerializeField] public Button continueButton;
    [SerializeField] public Button closeButton;

    private MerchantEvent currentMerchant;
    private ItemContainer backpackContainer;
    private System.Action onContinueCallback;

    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();

    private bool isExplicitlyOpened = false;
    public bool IsOpen => isExplicitlyOpened && merchantPanel != null && merchantPanel.activeSelf;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (merchantPanel != null)
        {
            merchantPanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(Close);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    private void Start()
    {
        if (!isExplicitlyOpened && merchantPanel != null)
        {
            merchantPanel.SetActive(false);
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
        UnsubscribeEvents();
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange += HandleInventoryChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshBackpackSlots();
        BuildWaresList(); // Refresh trade buttons state based on current inventory
        UpdateBackpackHeader();
    }

    private void UpdateBackpackHeader()
    {
        if (backpackHeaderTitle == null || backpackContainer == null) return;
        int occupied = 0;
        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            if (backpackContainer.itemSlots[i] != null && backpackContainer.itemSlots[i].itemData != null)
            {
                occupied++;
            }
        }
        backpackHeaderTitle.text = $"PLAYER BACKPACK <size=11><color=#A0D8EF>({occupied}/{backpackContainer.maxSlots} SLOTS)</color></size>";
    }

    public void Open(MerchantEvent merchant, ItemContainer backpack, System.Action onContinue)
    {
        if (merchant == null)
        {
            merchant = FindFirstObjectByType<MerchantEvent>(FindObjectsInactive.Include);
        }

        isExplicitlyOpened = true;
        currentMerchant = merchant;
        backpackContainer = backpack;
        onContinueCallback = onContinue;

        EnsureUIBuilt();
        EnforceBalancedLayout();

        if (merchantPanel != null)
        {
            merchantPanel.SetActive(true);
            merchantPanel.transform.SetAsLastSibling();
        }

        SubscribeEvents();

        BuildWaresList();
        BuildBackpackSlots(backpackContainer);

        int floorNum = ProgressionManager.Instance != null && ProgressionManager.Instance.currentFloor > 0
            ? ProgressionManager.Instance.currentFloor
            : 1;

        MerchantEvent.RecordMerchantEncounter(floorNum);
        bool isMerchantActive = MerchantEvent.IsMerchantActiveForFloor(floorNum);
        int daysLeft = MerchantEvent.GetRemainingDaysForFloor(floorNum);
        int cdLeft = MerchantEvent.GetCooldownForFloor(floorNum);

        if (isMerchantActive)
        {
            if (subtitleText != null)
            {
                subtitleText.text = $"Floor {floorNum} Traveling Merchant | Visit time remaining: {daysLeft} day(s)";
                subtitleText.color = new Color(0.78f, 0.82f, 0.92f);
            }
            SetFeedback("Welcome, brave traveler! Browse my rare recipes, barter seeds, or stock up on hot travel rations.");
        }
        else
        {
            if (subtitleText != null)
            {
                subtitleText.text = cdLeft > 0
                    ? $"<color=#FF7777>Floor {floorNum} Merchant has departed! Returns in {cdLeft} day(s).</color>"
                    : $"<color=#FF7777>Floor {floorNum} Merchant stall is temporarily closed.</color>";
            }
            SetFeedback("The merchant packed up and departed to a new land. You can rest for a moment then continue your journey.");
        }
    }

    public void Close()
    {
        isExplicitlyOpened = false;
        UnsubscribeEvents();

        if (merchantPanel != null)
        {
            merchantPanel.SetActive(false);
        }

        onContinueCallback?.Invoke();
        onContinueCallback = null;
    }

    public void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void BuildWaresList()
    {
        if (waresContainer == null || currentMerchant == null) return;

        // Immediately detach and destroy existing cards
        for (int i = waresContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = waresContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // Configure VerticalLayoutGroup for clean non-overlapping display of all items
        VerticalLayoutGroup vlg = waresContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        float currentY = 4f;

        bool isMerchantActive = currentMerchant == null || (currentMerchant.merchantActive && currentMerchant.remainingDays > 0 && currentMerchant.merchantCooldown <= 0);
        if (!isMerchantActive)
        {
            CreateSectionHeader("REST STOP - MERCHANT DEPARTED", new Color(0.6f, 0.8f, 1f), ref currentY);
            CreateDepartedRestCard(ref currentY);
        }
        else
        {
            currentMerchant?.EnsureDefaultTrades();

            // Header for 2 main columns and trade button
            CreateWaresHeaderBar(ref currentY);

            // 1. Seed Barter Section
            if (currentMerchant.seedTrades != null && currentMerchant.seedTrades.Length > 0)
            {
                CreateSectionHeader("SEED BARTER EXCHANGE", new Color(0.45f, 0.9f, 0.55f), ref currentY);
                for (int i = 0; i < currentMerchant.seedTrades.Length; i++)
                {
                    SeedTrade trade = currentMerchant.seedTrades[i];
                    if (trade != null)
                    {
                        CreateSeedTradeCard(trade, ref currentY);
                    }
                }
            }

            // 2. Survival Rations Section
            if (currentMerchant.rationTrades != null && currentMerchant.rationTrades.Length > 0)
            {
                CreateSectionHeader("TRAVEL RATIONS & SUPPLIES", new Color(0.45f, 0.8f, 1f), ref currentY);
                for (int i = 0; i < currentMerchant.rationTrades.Length; i++)
                {
                    RationTrade trade = currentMerchant.rationTrades[i];
                    if (trade != null)
                    {
                        CreateRationTradeCard(trade, ref currentY);
                    }
                }
            }
        }

        // Force layout rebuild and size setting so all rows render cleanly
        if (waresContainer is RectTransform rt)
        {
            rt.sizeDelta = new Vector2(0, currentY + 16f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            if (rt.parent is RectTransform parentRT)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
            }
            Canvas.ForceUpdateCanvases();
        }

        ScrollRect sr = waresContainer.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            sr.verticalNormalizedPosition = 1f;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
    }

    private void CreateDepartedRestCard(ref float currentY)
    {
        GameObject cardGO = new GameObject("DepartedRestCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0, 1);
        cardRT.anchorMax = new Vector2(1, 1);
        cardRT.pivot = new Vector2(0, 1);
        cardRT.anchoredPosition = new Vector2(0, -currentY);
        cardRT.sizeDelta = new Vector2(0, 110);
        currentY += 110f + 6f;

        LayoutElement le = cardGO.GetComponent<LayoutElement>();
        le.minHeight = 110f;
        le.preferredHeight = 110f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.12f, 0.16f, 0.22f, 0.98f));

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Info message
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(cardGO.transform, false);
        TextMeshProUGUI textTMP = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(textTMP);
        textTMP.fontSize = 11.5f;
        textTMP.lineSpacing = 3;
        textTMP.color = new Color(0.85f, 0.9f, 0.95f);
        textTMP.text = "The merchant caravan packed up and departed after their dungeon stay ended.\nThey left some warm coals and pure spring water for passing travelers.";

        // Rest / Continue Button
        GameObject btnGO = new GameObject("BtnRest", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(458, 36);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, new Color(0.2f, 0.45f, 0.35f, 1f));

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
            if (ps != null)
            {
                ps.currentHealth = Mathf.Min(ps.maxHealth, ps.currentHealth + 20);
                ps.currentHunger = Mathf.Min(ps.maxHunger, ps.currentHunger + 10);
            }
            Close();
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTMP);
        btnTMP.fontSize = 12.5f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.text = "[Rest & Recover 20 HP -> Continue]";
        btnTMP.color = Color.white;
        btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(458, 36);
    }

    private void CreateSectionHeader(string title, Color color, ref float currentY)
    {
        GameObject headerGO = new GameObject("SectionHeader", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        headerGO.transform.SetParent(waresContainer, false);
        RectTransform rt = headerGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(4, -currentY);
        rt.sizeDelta = new Vector2(-8, 22);
        currentY += 22f + 6f;

        LayoutElement le = headerGO.GetComponent<LayoutElement>();
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        TextMeshProUGUI tmp = headerGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 12f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.text = $"[ {title} ]";
    }


    private void EnforceBalancedLayout()
    {
        if (merchantPanel == null) return;

        // 1. Ensure Window Modal is 1040x620 centered
        Transform windowT = merchantPanel.transform.Find("Window");
        if (windowT is RectTransform windowRT)
        {
            windowRT.anchorMin = new Vector2(0.5f, 0.5f);
            windowRT.anchorMax = new Vector2(0.5f, 0.5f);
            windowRT.pivot = new Vector2(0.5f, 0.5f);
            windowRT.anchoredPosition = Vector2.zero;
            windowRT.sizeDelta = new Vector2(1040, 620);

            // Title at top: y = -14, height 30
            Transform titleT = windowT.Find("Title");
            if (titleT is RectTransform titleRT)
            {
                titleRT.anchorMin = new Vector2(0, 1);
                titleRT.anchorMax = new Vector2(1, 1);
                titleRT.pivot = new Vector2(0.5f, 1);
                titleRT.anchoredPosition = new Vector2(0, -14);
                titleRT.sizeDelta = new Vector2(-60, 30);
            }

            // Subtitle: y = -44, height 20
            Transform subT = windowT.Find("Subtitle");
            if (subT is RectTransform subRT)
            {
                subRT.anchorMin = new Vector2(0, 1);
                subRT.anchorMax = new Vector2(1, 1);
                subRT.pivot = new Vector2(0.5f, 1);
                subRT.anchoredPosition = new Vector2(0, -44);
                subRT.sizeDelta = new Vector2(-60, 20);
            }

            // Feedback: y = -66, height 22
            Transform feedT = windowT.Find("Feedback");
            if (feedT is RectTransform feedRT)
            {
                feedRT.anchorMin = new Vector2(0, 1);
                feedRT.anchorMax = new Vector2(1, 1);
                feedRT.pivot = new Vector2(0.5f, 1);
                feedRT.anchoredPosition = new Vector2(0, -66);
                feedRT.sizeDelta = new Vector2(-60, 22);
            }

            // Left Column (WaresColumn): 57.5% width, starts at y = -94, bottom at y = 68
            Transform waresColT = windowT.Find("WaresColumn");
            if (waresColT is RectTransform waresColRT)
            {
                waresColRT.anchorMin = new Vector2(0, 0);
                waresColRT.anchorMax = new Vector2(0.575f, 1);
                waresColRT.pivot = new Vector2(0, 1);
                waresColRT.offsetMin = new Vector2(20, 68);
                waresColRT.offsetMax = new Vector2(-10, -94);

                // Viewport
                Transform vpT = waresColT.Find("Viewport");
                if (vpT is RectTransform vpRT)
                {
                    vpRT.anchorMin = Vector2.zero;
                    vpRT.anchorMax = Vector2.one;
                    vpRT.offsetMin = new Vector2(6, 6);
                    vpRT.offsetMax = new Vector2(-20, -6);
                }
            }

            // Right Column (BackpackColumn): 42.5% width, starts at y = -94, bottom at y = 68
            Transform bpColT = windowT.Find("BackpackColumn");
            if (bpColT is RectTransform bpColRT)
            {
                bpColRT.anchorMin = new Vector2(0.575f, 0);
                bpColRT.anchorMax = new Vector2(1, 1);
                bpColRT.pivot = new Vector2(1, 1);
                bpColRT.offsetMin = new Vector2(10, 68);
                bpColRT.offsetMax = new Vector2(-20, -94);
            }

            // Continue Expedition Button at bottom center
            Transform contT = windowT.Find("BtnContinueExpedition");
            if (contT is RectTransform contRT)
            {
                contRT.anchorMin = new Vector2(0.5f, 0);
                contRT.anchorMax = new Vector2(0.5f, 0);
                contRT.pivot = new Vector2(0.5f, 0);
                contRT.anchoredPosition = new Vector2(0, 14);
                contRT.sizeDelta = new Vector2(250, 44);
            }
        }

        // 2. Configure waresContainer layout
        if (waresContainer is RectTransform contentRT)
        {
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = contentRT.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding = new RectOffset(6, 6, 6, 6);
                vlg.spacing = 6f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            ContentSizeFitter csf = contentRT.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }

    private void CreateWaresHeaderBar(ref float currentY)
    {
        GameObject barGO = new GameObject("WaresHeaderBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        barGO.transform.SetParent(waresContainer, false);
        RectTransform barRT = barGO.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 1);
        barRT.anchorMax = new Vector2(1, 1);
        barRT.pivot = new Vector2(0, 1);
        barRT.anchoredPosition = new Vector2(0, -currentY);
        barRT.sizeDelta = new Vector2(0, 24);
        currentY += 24f + 6f;

        LayoutElement le = barGO.GetComponent<LayoutElement>();
        le.minHeight = 24f;
        le.preferredHeight = 24f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Image barBg = barGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(barBg, new Color(0.12f, 0.14f, 0.20f, 0.95f));

        HorizontalLayoutGroup hlg = barGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 2, 2);
        hlg.spacing = 6;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateHeaderLabel(barGO, "NHẬN ĐƯỢC (REWARD)", 195, new Color(1f, 0.85f, 0.35f), TextAlignmentOptions.Left);
        CreateHeaderLabel(barGO, "⇄", 20, new Color(0.6f, 0.7f, 0.85f), TextAlignmentOptions.Center);
        CreateHeaderLabel(barGO, "DÙNG ĐỔI (COST)", 190, new Color(0.5f, 0.85f, 1f), TextAlignmentOptions.Left);
        CreateHeaderLabel(barGO, "THAO TÁC", 75, new Color(0.8f, 0.85f, 0.9f), TextAlignmentOptions.Center);
    }

    private void CreateHeaderLabel(GameObject parent, string text, float width, Color color, TextAlignmentOptions align)
    {
        GameObject lblGO = new GameObject("HeaderLbl", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGO.transform.SetParent(parent.transform, false);
        RectTransform rt = lblGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 20);

        TextMeshProUGUI tmp = lblGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 10.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.color = color;
        tmp.text = text;
    }

    private void CreateTradeRow(string tradeId, ItemData outItem, int outAmount, string outCategory, ItemData inItem, int inAmount, bool canAfford, System.Action onTradeClicked, ref float currentY)
    {
        int haveInput = inItem != null ? GetItemCount(backpackContainer, inItem) : 0;

        GameObject cardGO = new GameObject($"TradeRow_{tradeId}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0, 1);
        cardRT.anchorMax = new Vector2(1, 1);
        cardRT.pivot = new Vector2(0, 1);
        cardRT.anchoredPosition = new Vector2(0, -currentY);
        cardRT.sizeDelta = new Vector2(0, 48);
        currentY += 48f + 6f;

        LayoutElement le = cardGO.GetComponent<LayoutElement>();
        le.minHeight = 48f;
        le.preferredHeight = 48f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.13f, 0.15f, 0.22f, 0.98f));

        HorizontalLayoutGroup hlg = cardGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.spacing = 6;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // --- CỘT 1: ITEM NHẬN ĐƯỢC (REWARD) ---
        GameObject outColGO = new GameObject("OutputCol", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        outColGO.transform.SetParent(cardGO.transform, false);
        outColGO.GetComponent<RectTransform>().sizeDelta = new Vector2(195, 40);
        HorizontalLayoutGroup outHlg = outColGO.GetComponent<HorizontalLayoutGroup>();
        outHlg.spacing = 6;
        outHlg.childControlWidth = false;
        outHlg.childControlHeight = true;
        outHlg.childForceExpandWidth = false;
        outHlg.childForceExpandHeight = true;

        // Slot Box nhận
        GameObject outSlotGO = new GameObject("OutSlot", typeof(RectTransform), typeof(Image));
        outSlotGO.transform.SetParent(outColGO.transform, false);
        outSlotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(38, 38);
        Image outSlotImg = outSlotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(outSlotImg, new Color(0.24f, 0.32f, 0.44f, 1f));

        GameObject outIconGO = new GameObject("OutIcon", typeof(RectTransform), typeof(Image));
        outIconGO.transform.SetParent(outSlotGO.transform, false);
        RectTransform outIconRT = outIconGO.GetComponent<RectTransform>();
        outIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        outIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        outIconRT.anchoredPosition = Vector2.zero;
        outIconRT.sizeDelta = new Vector2(28, 28);
        Image outIcon = outIconGO.GetComponent<Image>();
        outIcon.preserveAspect = true;
        if (outItem != null) outIcon.sprite = outItem.itemIcon;

        // Label thông tin item nhận
        GameObject outDescGO = new GameObject("OutDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        outDescGO.transform.SetParent(outColGO.transform, false);
        outDescGO.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40);
        TextMeshProUGUI outDescTMP = outDescGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(outDescTMP);
        outDescTMP.fontSize = 11.5f;
        string outName = outItem != null ? outItem.itemName : "Vật phẩm";
        outDescTMP.text = $"<color=#FFD700><b>{outAmount}x {outName}</b></color>\n<size=9.5><color=#8FA0C0>{outCategory}</color></size>";

        // --- MŨI TÊN CHUYỂN ĐỔI ---
        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(cardGO.transform, false);
        arrowGO.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 40);
        TextMeshProUGUI arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(arrowTMP);
        arrowTMP.fontSize = 14f;
        arrowTMP.fontStyle = FontStyles.Bold;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.text = "⇄";
        arrowTMP.color = new Color(0.65f, 0.75f, 0.9f);

        // --- CỘT 2: ITEM DÙNG ĐỔI (PAY/COST) ---
        GameObject inColGO = new GameObject("InputCol", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        inColGO.transform.SetParent(cardGO.transform, false);
        inColGO.GetComponent<RectTransform>().sizeDelta = new Vector2(190, 40);
        HorizontalLayoutGroup inHlg = inColGO.GetComponent<HorizontalLayoutGroup>();
        inHlg.spacing = 6;
        inHlg.childControlWidth = false;
        inHlg.childControlHeight = true;
        inHlg.childForceExpandWidth = false;
        inHlg.childForceExpandHeight = true;

        // Slot Box dùng đổi
        GameObject inSlotGO = new GameObject("InSlot", typeof(RectTransform), typeof(Image));
        inSlotGO.transform.SetParent(inColGO.transform, false);
        inSlotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(38, 38);
        Image inSlotImg = inSlotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(inSlotImg, new Color(0.24f, 0.26f, 0.35f, 1f));

        GameObject inIconGO = new GameObject("InIcon", typeof(RectTransform), typeof(Image));
        inIconGO.transform.SetParent(inSlotGO.transform, false);
        RectTransform inIconRT = inIconGO.GetComponent<RectTransform>();
        inIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        inIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        inIconRT.anchoredPosition = Vector2.zero;
        inIconRT.sizeDelta = new Vector2(28, 28);
        Image inIcon = inIconGO.GetComponent<Image>();
        inIcon.preserveAspect = true;
        if (inItem != null) inIcon.sprite = inItem.itemIcon;

        // Label thông tin item dùng đổi
        GameObject inDescGO = new GameObject("InDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        inDescGO.transform.SetParent(inColGO.transform, false);
        inDescGO.GetComponent<RectTransform>().sizeDelta = new Vector2(145, 40);
        TextMeshProUGUI inDescTMP = inDescGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(inDescTMP);
        inDescTMP.fontSize = 11f;
        string inColor = haveInput >= inAmount ? "#55FF88" : "#FF6666";
        string inName = inItem != null ? inItem.itemName : "Nguyên liệu";
        inDescTMP.text = $"<b>{inAmount}x {inName}</b>\n<size=10>Cần: {inAmount} (<color={inColor}>Có: {haveInput}</color>)</size>";

        // --- NÚT TRADE ---
        GameObject btnGO = new GameObject("BtnTrade", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(75, 34);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, canAfford ? new Color(0.18f, 0.52f, 0.34f, 1f) : new Color(0.24f, 0.26f, 0.32f, 0.8f));

        Button btn = btnGO.GetComponent<Button>();
        btn.interactable = canAfford;
        btn.onClick.AddListener(() => onTradeClicked?.Invoke());

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTMP);
        btnTMP.fontSize = 11.5f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.text = "ĐỔI";
        btnTMP.color = canAfford ? Color.white : new Color(0.65f, 0.65f, 0.65f);
        btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(75, 34);
    }

    private void CreateSeedTradeCard(SeedTrade trade, ref float currentY)
    {
        bool canAfford = currentMerchant.CanTradeSeed(trade, backpackContainer);
        CreateTradeRow(
            trade.tradeName,
            trade.outputSeed,
            trade.outputAmount,
            "Hạt giống",
            trade.inputSeed,
            trade.inputAmount,
            canAfford,
            () =>
            {
                bool success = currentMerchant.TradeSeed(trade, backpackContainer);
                if (success)
                {
                    string outName = trade.outputSeed != null ? trade.outputSeed.itemName : "Hạt giống";
                    SetFeedback($"<color=#55FF88>Đổi thành công {trade.outputAmount}x {outName}!</color>");
                    backpackContainer?.NotifyChange();
                    BuildWaresList();
                }
            },
            ref currentY
        );
    }

    private void CreateRationTradeCard(RationTrade trade, ref float currentY)
    {
        bool canAfford = currentMerchant.CanTradeRation(trade, backpackContainer);
        CreateTradeRow(
            trade.tradeName,
            trade.outputFood,
            trade.outputAmount,
            "Lương thực",
            trade.inputProduce,
            trade.inputAmount,
            canAfford,
            () =>
            {
                bool success = currentMerchant.TradeRation(trade, backpackContainer);
                if (success)
                {
                    string outName = trade.outputFood != null ? trade.outputFood.itemName : "Lương thực";
                    SetFeedback($"<color=#77CCFF>Đổi thành công {trade.outputAmount}x {outName}!</color>");
                    backpackContainer?.NotifyChange();
                    BuildWaresList();
                }
            },
            ref currentY
        );
    }

    private void BuildBackpackSlots(ItemContainer container)
    {
        if (backpackSlotsParent == null || container == null) return;

        EnsureSlotPrefab();
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
            Debug.LogWarning("[MerchantUI] slotPrefab is null, cannot build backpack slots!");
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

        UpdateBackpackHeader();
    }

    private void EnsureSlotPrefab()
    {
        if (slotPrefab != null) return;
        slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
    }

    private Canvas GetCanvas()
    {
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
            }
        }
        return canvas;
    }

    private void EnsureUIBuilt()
    {
        if (merchantPanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[MerchantUI] No Canvas found in scene!");
            return;
        }

        // 1. Fullscreen Dark Overlay
        GameObject overlayGO = new GameObject("MerchantPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        merchantPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        // 2. Central Window Panel with 9-Sliced Frame
        GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(1040, 620);

        Image windowBg = windowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(windowBg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

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
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 21;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "WANDERING MERCHANT - DUNGEON OUTPOST";
        titleText.color = new Color(1f, 0.85f, 0.35f);

        GameObject subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.transform.SetParent(windowGO.transform, false);
        RectTransform subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0, 1);
        subtitleRT.anchorMax = new Vector2(1, 1);
        subtitleRT.pivot = new Vector2(0.5f, 1);
        subtitleRT.anchoredPosition = new Vector2(0, -44);
        subtitleRT.sizeDelta = new Vector2(-60, 22);

        subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 12.5f;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "A mysterious trader braving the dungeon. Barter seeds, recipes, and survival rations.";
        subtitleText.color = new Color(0.82f, 0.86f, 0.94f);

        // 4. Feedback Label
        GameObject feedbackGO = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.transform.SetParent(windowGO.transform, false);
        RectTransform feedbackRT = feedbackGO.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 1);
        feedbackRT.anchorMax = new Vector2(1, 1);
        feedbackRT.pivot = new Vector2(0.5f, 1);
        feedbackRT.anchoredPosition = new Vector2(0, -68);
        feedbackRT.sizeDelta = new Vector2(-60, 24);

        feedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(feedbackText);
        feedbackText.fontSize = 13;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.text = "Welcome traveler! What would you like to barter today?";
        feedbackText.color = new Color(0.88f, 0.94f, 1f);

        // 5. Close Button (X)
        GameObject closeGO = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(windowGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-14, -14);
        closeRT.sizeDelta = new Vector2(32, 32);

        Image closeBg = closeGO.GetComponent<Image>();
        closeButton = closeGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(closeButton, closeBg, new Color(0.6f, 0.18f, 0.18f, 1f), new Color(0.85f, 0.25f, 0.25f, 1f));
        closeButton.onClick.AddListener(Close);

        GameObject xTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI xTMP = xTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(xTMP);
        xTMP.fontSize = 16;
        xTMP.fontStyle = FontStyles.Bold;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.text = "X";
        xTMP.color = Color.white;
        xTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32);

        // 6. Left Column: Merchant Wares (Scrollable) with 9-Sliced Frame
        GameObject waresColGO = new GameObject("WaresColumn", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        waresColGO.transform.SetParent(windowGO.transform, false);
        RectTransform waresColRT = waresColGO.GetComponent<RectTransform>();
        waresColRT.anchorMin = new Vector2(0, 0);
        waresColRT.anchorMax = new Vector2(0, 1);
        waresColRT.pivot = new Vector2(0, 0.5f);
        waresColRT.anchoredPosition = new Vector2(20, -18);
        waresColRT.sizeDelta = new Vector2(585, -140);

        Image waresColBg = waresColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(waresColBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        // Viewport
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(waresColGO.transform, false);
        RectTransform vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(6, 6);
        vpRT.offsetMax = new Vector2(-22, -6);

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 4f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = waresColGO.GetComponent<ScrollRect>();
        sr.viewport = vpRT;
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        // Wares Vertical Scrollbar / Slider
        GameObject waresSbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        waresSbGO.transform.SetParent(waresColGO.transform, false);
        RectTransform waresSbRT = waresSbGO.GetComponent<RectTransform>();
        waresSbRT.anchorMin = new Vector2(1, 0);
        waresSbRT.anchorMax = new Vector2(1, 1);
        waresSbRT.pivot = new Vector2(1, 0.5f);
        waresSbRT.sizeDelta = new Vector2(12, -12);
        waresSbRT.anchoredPosition = new Vector2(-6, 0);

        Image waresSbBg = waresSbGO.GetComponent<Image>();
        waresSbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar waresSb = waresSbGO.GetComponent<Scrollbar>();
        waresSb.direction = Scrollbar.Direction.BottomToTop;

        GameObject waresSaGO = new GameObject("SlidingArea", typeof(RectTransform));
        waresSaGO.transform.SetParent(waresSbGO.transform, false);
        RectTransform waresSaRT = waresSaGO.GetComponent<RectTransform>();
        waresSaRT.anchorMin = Vector2.zero;
        waresSaRT.anchorMax = Vector2.one;
        waresSaRT.sizeDelta = Vector2.zero;

        GameObject waresHandleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        waresHandleGO.transform.SetParent(waresSaGO.transform, false);
        RectTransform waresHandleRT = waresHandleGO.GetComponent<RectTransform>();
        waresHandleRT.sizeDelta = Vector2.zero;

        Image waresHandleImg = waresHandleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(waresHandleImg, new Color(0.85f, 0.65f, 0.2f, 1f));

        waresSb.handleRect = waresHandleRT;
        waresSb.targetGraphic = waresHandleImg;
        sr.verticalScrollbar = waresSb;

        waresContainer = contentGO.transform;

        // 7. Right Column: Backpack Section with 9-Sliced Frame
        GameObject backpackColGO = new GameObject("BackpackColumn", typeof(RectTransform), typeof(Image));
        backpackColGO.transform.SetParent(windowGO.transform, false);
        RectTransform bpColRT = backpackColGO.GetComponent<RectTransform>();
        bpColRT.anchorMin = new Vector2(1, 0);
        bpColRT.anchorMax = new Vector2(1, 1);
        bpColRT.pivot = new Vector2(1, 0.5f);
        bpColRT.anchoredPosition = new Vector2(-20, -18);
        bpColRT.sizeDelta = new Vector2(395, -140);

        Image bpColBg = backpackColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bpColBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        // Backpack Column Header
        GameObject bpHeaderGO = new GameObject("BackpackHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpHeaderGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpHeaderRT = bpHeaderGO.GetComponent<RectTransform>();
        bpHeaderRT.anchorMin = new Vector2(0, 1);
        bpHeaderRT.anchorMax = new Vector2(1, 1);
        bpHeaderRT.pivot = new Vector2(0.5f, 1);
        bpHeaderRT.anchoredPosition = new Vector2(0, -8);
        bpHeaderRT.sizeDelta = new Vector2(-20, 26);

        backpackHeaderTitle = bpHeaderGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(backpackHeaderTitle);
        backpackHeaderTitle.fontSize = 13.5f;
        backpackHeaderTitle.fontStyle = FontStyles.Bold;
        backpackHeaderTitle.alignment = TextAlignmentOptions.Center;
        backpackHeaderTitle.text = "PLAYER BACKPACK";
        backpackHeaderTitle.color = new Color(0.4f, 0.85f, 1f);

        // Scroll Area for Backpack
        GameObject bpScrollGO = new GameObject("BackpackScrollArea", typeof(RectTransform), typeof(ScrollRect));
        bpScrollGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpScrollRT = bpScrollGO.GetComponent<RectTransform>();
        bpScrollRT.anchorMin = Vector2.zero;
        bpScrollRT.anchorMax = Vector2.one;
        bpScrollRT.offsetMin = new Vector2(8, 8);
        bpScrollRT.offsetMax = new Vector2(-8, -38);

        ScrollRect bpScrollRect = bpScrollGO.GetComponent<ScrollRect>();
        bpScrollRect.horizontal = false;
        bpScrollRect.vertical = true;
        bpScrollRect.movementType = ScrollRect.MovementType.Clamped;
        bpScrollRect.scrollSensitivity = 25f;

        // Viewport
        GameObject bpVpGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        bpVpGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform bpVpRT = bpVpGO.GetComponent<RectTransform>();
        bpVpRT.anchorMin = Vector2.zero;
        bpVpRT.anchorMax = Vector2.one;
        bpVpRT.offsetMin = Vector2.zero;
        bpVpRT.offsetMax = new Vector2(-16, 0); // space for scrollbar

        // Backpack Grid Container (Content)
        GameObject bpGridGO = new GameObject("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        bpGridGO.transform.SetParent(bpVpGO.transform, false);
        RectTransform bpGridRT = bpGridGO.GetComponent<RectTransform>();
        bpGridRT.anchorMin = new Vector2(0, 1);
        bpGridRT.anchorMax = new Vector2(1, 1);
        bpGridRT.pivot = new Vector2(0.5f, 1);
        bpGridRT.anchoredPosition = Vector2.zero;
        bpGridRT.sizeDelta = Vector2.zero;

        GridLayoutGroup bpGLG = bpGridGO.GetComponent<GridLayoutGroup>();
        bpGLG.cellSize = new Vector2(50, 50);
        bpGLG.spacing = new Vector2(7, 7);
        bpGLG.padding = new RectOffset(6, 6, 6, 6);
        bpGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGLG.constraintCount = 6;
        bpGLG.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter bpCsf = bpGridGO.GetComponent<ContentSizeFitter>();
        bpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bpScrollRect.viewport = bpVpRT;
        bpScrollRect.content = bpGridRT;

        // Vertical Scrollbar for Backpack
        GameObject bpSbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        bpSbGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform bpSbRT = bpSbGO.GetComponent<RectTransform>();
        bpSbRT.anchorMin = new Vector2(1, 0);
        bpSbRT.anchorMax = new Vector2(1, 1);
        bpSbRT.pivot = new Vector2(1, 0.5f);
        bpSbRT.sizeDelta = new Vector2(12, 0);
        bpSbRT.anchoredPosition = Vector2.zero;

        Image bpSbBg = bpSbGO.GetComponent<Image>();
        bpSbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar bpSb = bpSbGO.GetComponent<Scrollbar>();
        bpSb.direction = Scrollbar.Direction.BottomToTop;

        GameObject bpSaGO = new GameObject("SlidingArea", typeof(RectTransform));
        bpSaGO.transform.SetParent(bpSbGO.transform, false);
        RectTransform bpSaRT = bpSaGO.GetComponent<RectTransform>();
        bpSaRT.anchorMin = Vector2.zero;
        bpSaRT.anchorMax = Vector2.one;
        bpSaRT.sizeDelta = Vector2.zero;

        GameObject bpHandleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        bpHandleGO.transform.SetParent(bpSaGO.transform, false);
        RectTransform bpHandleRT = bpHandleGO.GetComponent<RectTransform>();
        bpHandleRT.sizeDelta = Vector2.zero;

        Image bpHandleImg = bpHandleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bpHandleImg, new Color(0.25f, 0.42f, 0.65f, 1f));

        bpSb.handleRect = bpHandleRT;
        bpSb.targetGraphic = bpHandleImg;
        bpScrollRect.verticalScrollbar = bpSb;

        backpackSlotsParent = bpGridGO.transform;

        // 8. Footer: Continue Journey Button
        GameObject continueGO = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
        continueGO.transform.SetParent(windowGO.transform, false);
        RectTransform continueRT = continueGO.GetComponent<RectTransform>();
        continueRT.anchorMin = new Vector2(0.5f, 0);
        continueRT.anchorMax = new Vector2(0.5f, 0);
        continueRT.pivot = new Vector2(0.5f, 0);
        continueRT.anchoredPosition = new Vector2(0, 12);
        continueRT.sizeDelta = new Vector2(300, 42);

        Image continueBg = continueGO.GetComponent<Image>();
        continueButton = continueGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(continueButton, continueBg, new Color(0.18f, 0.55f, 0.32f, 1f), new Color(0.24f, 0.70f, 0.40f, 1f));
        continueButton.onClick.AddListener(Close);

        GameObject continueTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        continueTextGO.transform.SetParent(continueGO.transform, false);
        TextMeshProUGUI continueTMP = continueTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(continueTMP);
        continueTMP.fontSize = 13.5f;
        continueTMP.fontStyle = FontStyles.Bold;
        continueTMP.alignment = TextAlignmentOptions.Center;
        continueTMP.text = "CONTINUE EXPEDITION >>";
        continueTMP.color = Color.white;
        continueTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 42);
    }

    private int GetItemCount(ItemContainer container, ItemData item)
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
}
