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
        backpackHeaderTitle.text = $"BA LÔ CỦA BẠN <size=11><color=#A0D8EF>({occupied}/{backpackContainer.maxSlots} Ô)</color></size>";
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

        UIEscapeManager.NotifyModalOpened("Merchant");

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
                subtitleText.text = $"Thương Nhân Du Mục Tầng {floorNum} | Thời gian ghé thăm còn: {daysLeft} ngày";
                subtitleText.color = new Color(0.78f, 0.82f, 0.92f);
            }
            SetFeedback("Chào mừng nhà thám hiểm dũng cảm! Hãy xem qua các công thức hiếm, đổi hạt giống hoặc mua lương khô nóng hổi mang theo.");
        }
        else
        {
            if (subtitleText != null)
            {
                subtitleText.text = cdLeft > 0
                    ? $"<color=#FF7777>Thương nhân Tầng {floorNum} đã rời đi! Sẽ quay lại sau {cdLeft} ngày.</color>"
                    : $"<color=#FF7777>Quầy hàng của Thương nhân Tầng {floorNum} tạm thời đóng cửa.</color>";
            }
            SetFeedback("Đoàn thương buôn đã dọn hàng chuyển sang vùng đất mới. Bạn có thể nghỉ chân chốc lát rồi tiếp tục hành trình.");
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

        DungeonTutorialController.Instance?.HideBanner();
        DungeonTutorialController.Instance?.HidePointer();

        UIEscapeManager.NotifyModalClosed("Merchant");
        CurrentItemPanel.RefreshHotbar();

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
            CreateSectionHeader("TRẠM DỪNG CHÂN - THƯƠNG NHÂN ĐÃ RỜI ĐI", new Color(0.6f, 0.8f, 1f), ref currentY);
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
                CreateSectionHeader("GIAO THƯƠNG HẠT GIỐNG", new Color(0.45f, 0.9f, 0.55f), ref currentY);
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
                CreateSectionHeader("LƯƠNG KHÔ & VẬT PHẨM HÀNH TRÌNH", new Color(0.45f, 0.8f, 1f), ref currentY);
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
        textTMP.text = "Đoàn thương buôn đã dọn dẹp hàng quán và rời đi sau kỳ hạn lưu trú tại hầm ngục.\nHọ đã để lại một ít than hồng ấm áp và nước suối tinh khiết cho khách bộ hành đi ngang qua.";

        // Rest / Continue Button
        GameObject btnGO = new GameObject("BtnRest", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(458, 36);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, new Color(0.2f, 0.45f, 0.35f, 1f));

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
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
        btnTMP.text = "[Nghỉ ngơi hồi phục 20 HP -> Tiếp tục]";
        btnTMP.color = Color.white;
        btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(458, 36);
    }

    private void CreateSectionHeader(string title, Color color, ref float currentY)
    {
        GameObject headerGO = new GameObject("SectionHeader", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        headerGO.transform.SetParent(waresContainer, false);
        RectTransform rt = headerGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, -currentY);
        rt.sizeDelta = new Vector2(0, 26);
        currentY += 26f + 6f;

        LayoutElement le = headerGO.GetComponent<LayoutElement>();
        le.minHeight = 26f;
        le.preferredHeight = 26f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Image bannerImg = headerGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(bannerImg, "Comp_Merchant_SectionBanner", sliced: true, Color.white))
        {
            bannerImg.color = new Color(0.14f, 0.10f, 0.08f, 0.92f);
        }

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(headerGO.transform, false);
        RectTransform trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12, 2);
        trt.offsetMax = new Vector2(-12, -2);

        TextMeshProUGUI tmp = txtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp, withOutline: true);
        tmp.fontSize = 12f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 9.5f;
        tmp.fontSizeMax = 12f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.color = color;
        tmp.text = $"[ {title} ]";
    }


    private void EnforceBalancedLayout()
    {
        if (merchantPanel == null) return;

        // 1. Ensure Window Modal is 1040x620 centered + styled with GOTY Comp_* sprites
        Transform windowT = merchantPanel.transform.Find("Window");
        if (windowT is RectTransform windowRT)
        {
            windowRT.anchorMin = new Vector2(0.5f, 0.5f);
            windowRT.anchorMax = new Vector2(0.5f, 0.5f);
            windowRT.pivot = new Vector2(0.5f, 0.5f);
            windowRT.anchoredPosition = Vector2.zero;
            windowRT.sizeDelta = new Vector2(1040, 620);

            Image winImg = windowT.GetComponent<Image>();
            if (winImg != null)
            {
                DungeonUIAssetHelper.ApplyCompSprite(winImg, "Comp_Merchant_ModalBase", sliced: true, Color.white);
            }

            // Title at top: y = -16, height 30
            Transform titleT = windowT.Find("Title");
            if (titleT is RectTransform titleRT)
            {
                titleRT.anchorMin = new Vector2(0, 1);
                titleRT.anchorMax = new Vector2(1, 1);
                titleRT.pivot = new Vector2(0.5f, 1);
                titleRT.anchoredPosition = new Vector2(0, -16);
                titleRT.sizeDelta = new Vector2(-80, 30);
            }

            // Subtitle: y = -46, height 20
            Transform subT = windowT.Find("Subtitle");
            if (subT is RectTransform subRT)
            {
                subRT.anchorMin = new Vector2(0, 1);
                subRT.anchorMax = new Vector2(1, 1);
                subRT.pivot = new Vector2(0.5f, 1);
                subRT.anchoredPosition = new Vector2(0, -46);
                subRT.sizeDelta = new Vector2(-80, 20);
            }

            // Feedback: y = -68, height 22
            Transform feedT = windowT.Find("Feedback");
            if (feedT is RectTransform feedRT)
            {
                feedRT.anchorMin = new Vector2(0, 1);
                feedRT.anchorMax = new Vector2(1, 1);
                feedRT.pivot = new Vector2(0.5f, 1);
                feedRT.anchoredPosition = new Vector2(0, -68);
                feedRT.sizeDelta = new Vector2(-80, 22);
            }

            // Left Column (WaresColumn): 57.5% width, padded away from ModalBase drop shadow
            Transform waresColT = windowT.Find("WaresColumn");
            if (waresColT is RectTransform waresColRT)
            {
                waresColRT.anchorMin = new Vector2(0, 0);
                waresColRT.anchorMax = new Vector2(0.575f, 1);
                waresColRT.pivot = new Vector2(0, 1);
                waresColRT.offsetMin = new Vector2(24, 72);
                waresColRT.offsetMax = new Vector2(-8, -94);

                Image wcImg = waresColT.GetComponent<Image>();
                if (wcImg != null)
                {
                    DungeonUIAssetHelper.ApplyCompSprite(wcImg, "Comp_Merchant_ColumnPanel", sliced: true, Color.white);
                }

                // Viewport
                Transform vpT = waresColT.Find("Viewport");
                if (vpT is RectTransform vpRT)
                {
                    vpRT.anchorMin = Vector2.zero;
                    vpRT.anchorMax = Vector2.one;
                    vpRT.offsetMin = new Vector2(8, 8);
                    vpRT.offsetMax = new Vector2(-20, -8);
                }
            }

            // Right Column (BackpackColumn): 42.5% width, padded away from ModalBase drop shadow
            Transform bpColT = windowT.Find("BackpackColumn");
            if (bpColT is RectTransform bpColRT)
            {
                bpColRT.anchorMin = new Vector2(0.575f, 0);
                bpColRT.anchorMax = new Vector2(1, 1);
                bpColRT.pivot = new Vector2(1, 1);
                bpColRT.offsetMin = new Vector2(8, 72);
                bpColRT.offsetMax = new Vector2(-24, -94);

                Image bpImg = bpColT.GetComponent<Image>();
                if (bpImg != null)
                {
                    DungeonUIAssetHelper.ApplyCompSprite(bpImg, "Comp_Merchant_ColumnPanel", sliced: true, Color.white);
                }
            }

            // Continue Expedition Button at bottom center
            Transform contT = windowT.Find("BtnContinueExpedition");
            if (contT is RectTransform contRT)
            {
                contRT.anchorMin = new Vector2(0.5f, 0);
                contRT.anchorMax = new Vector2(0.5f, 0);
                contRT.pivot = new Vector2(0.5f, 0);
                contRT.anchoredPosition = new Vector2(0, 20);
                contRT.sizeDelta = new Vector2(250, 44);
            }
        }

        if (continueButton != null)
        {
            DungeonUIAssetHelper.ApplyCompButton(continueButton, continueButton.GetComponent<Image>(), "Comp_Merchant_TradeBtn", Color.white);
            TextMeshProUGUI contTxt = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (contTxt != null)
            {
                contTxt.enableAutoSizing = true;
                contTxt.fontSizeMin = 10f;
                contTxt.fontSizeMax = 14f;
                contTxt.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }
        if (closeButton != null)
        {
            Image closeImg = closeButton.GetComponent<Image>();
            if (DungeonUIAssetHelper.ApplyCompButton(closeButton, closeImg, "Comp_Statue_BtnCloseX", Color.white) && closeImg != null)
            {
                closeImg.type = Image.Type.Simple;
            }
        }

        ConfigureAutoSizeTMP(titleText, 14f, 20f);
        ConfigureAutoSizeTMP(subtitleText, 10f, 13f);
        ConfigureAutoSizeTMP(feedbackText, 9.5f, 12.5f);
        ConfigureAutoSizeTMP(backpackHeaderTitle, 11f, 15f);

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

    private static void ConfigureAutoSizeTMP(TextMeshProUGUI tmp, float minSize, float maxSize)
    {
        if (tmp == null) return;
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
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
        barRT.sizeDelta = new Vector2(0, 26);
        currentY += 26f + 6f;

        LayoutElement le = barGO.GetComponent<LayoutElement>();
        le.minHeight = 26f;
        le.preferredHeight = 26f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Image barBg = barGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(barBg, "Comp_Merchant_SectionBanner", sliced: true, Color.white))
        {
            DungeonUIAssetHelper.StyleSlicedFrame(barBg, new Color(0.12f, 0.14f, 0.20f, 0.95f));
        }

        HorizontalLayoutGroup hlg = barGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 2, 2);
        hlg.spacing = 6;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateHeaderLabel(barGO, "VẬT PHẨM NHẬN", 140f, 1f, new Color(1f, 0.88f, 0.51f), TextAlignmentOptions.Left);
        CreateHeaderLabel(barGO, "⇄", 28f, 0f, new Color(0.84f, 0.80f, 0.78f), TextAlignmentOptions.Center);
        CreateHeaderLabel(barGO, "VẬT PHẨM ĐỔI", 135f, 1f, new Color(0.55f, 0.88f, 1f), TextAlignmentOptions.Left);
        CreateHeaderLabel(barGO, "THAO TÁC", 78f, 0f, new Color(0.90f, 0.88f, 0.85f), TextAlignmentOptions.Center);
    }

    private void CreateHeaderLabel(GameObject parent, string text, float minWidth, float flexWidth, Color color, TextAlignmentOptions align)
    {
        GameObject lblGO = new GameObject("HeaderLbl", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        lblGO.transform.SetParent(parent.transform, false);
        LayoutElement le = lblGO.GetComponent<LayoutElement>();
        le.minWidth = minWidth;
        le.preferredWidth = flexWidth > 0f ? minWidth : minWidth;
        le.flexibleWidth = flexWidth;

        TextMeshProUGUI tmp = lblGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 10.5f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 8.5f;
        tmp.fontSizeMax = 10.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
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
        cardRT.sizeDelta = new Vector2(0, 60);
        currentY += 60f + 6f;

        LayoutElement le = cardGO.GetComponent<LayoutElement>();
        le.minHeight = 60f;
        le.preferredHeight = 60f;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        Image bg = cardGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(bg, "Comp_Merchant_TradeRowBase", sliced: true, Color.white))
        {
            DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.13f, 0.15f, 0.22f, 0.98f));
        }

        HorizontalLayoutGroup hlg = cardGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 8, 8);
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // --- CỘT 1: ITEM NHẬN ĐƯỢC (REWARD) ---
        GameObject outColGO = new GameObject("OutputCol", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        outColGO.transform.SetParent(cardGO.transform, false);
        LayoutElement outColLE = outColGO.GetComponent<LayoutElement>();
        outColLE.minWidth = 140f;
        outColLE.flexibleWidth = 1f;
        outColLE.minHeight = 44f;
        outColLE.preferredHeight = 44f;

        HorizontalLayoutGroup outHlg = outColGO.GetComponent<HorizontalLayoutGroup>();
        outHlg.spacing = 6f;
        outHlg.childAlignment = TextAnchor.MiddleLeft;
        outHlg.childControlWidth = true;
        outHlg.childControlHeight = true;
        outHlg.childForceExpandWidth = false;
        outHlg.childForceExpandHeight = false;

        // Slot Box nhận
        GameObject outSlotGO = new GameObject("OutSlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        outSlotGO.transform.SetParent(outColGO.transform, false);
        LayoutElement outSlotLE = outSlotGO.GetComponent<LayoutElement>();
        outSlotLE.minWidth = 40f;
        outSlotLE.preferredWidth = 40f;
        outSlotLE.minHeight = 40f;
        outSlotLE.preferredHeight = 40f;
        Image outSlotImg = outSlotGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(outSlotImg, "Comp_Merchant_ItemSlot", sliced: true, Color.white))
        {
            DungeonUIAssetHelper.StyleSlotImage(outSlotImg, new Color(0.24f, 0.32f, 0.44f, 1f));
        }

        GameObject outIconGO = new GameObject("OutIcon", typeof(RectTransform), typeof(Image));
        outIconGO.transform.SetParent(outSlotGO.transform, false);
        RectTransform outIconRT = outIconGO.GetComponent<RectTransform>();
        outIconRT.anchorMin = Vector2.zero;
        outIconRT.anchorMax = Vector2.one;
        outIconRT.offsetMin = new Vector2(5, 5);
        outIconRT.offsetMax = new Vector2(-5, -5);
        Image outIcon = outIconGO.GetComponent<Image>();
        outIcon.preserveAspect = true;
        if (outItem != null) outIcon.sprite = outItem.itemIcon;

        // Label thông tin item nhận
        GameObject outDescGO = new GameObject("OutDesc", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        outDescGO.transform.SetParent(outColGO.transform, false);
        LayoutElement outDescLE = outDescGO.GetComponent<LayoutElement>();
        outDescLE.minWidth = 80f;
        outDescLE.flexibleWidth = 1f;
        outDescLE.minHeight = 42f;
        outDescLE.preferredHeight = 42f;
        TextMeshProUGUI outDescTMP = outDescGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(outDescTMP, withOutline: true);
        outDescTMP.fontSize = 11.5f;
        outDescTMP.enableAutoSizing = true;
        outDescTMP.fontSizeMin = 9f;
        outDescTMP.fontSizeMax = 12f;
        outDescTMP.alignment = TextAlignmentOptions.MidlineLeft;
        outDescTMP.textWrappingMode = TextWrappingModes.Normal;
        outDescTMP.overflowMode = TextOverflowModes.Ellipsis;
        string outName = outItem != null ? outItem.itemName : "Vật phẩm";
        string outSub = outItem is FoodData fdOut
            ? $"<color=#FF6B6B>+{fdOut.healthValue} HP</color> | <color=#FFD043>+{fdOut.hungerValue} No</color>"
            : $"<color=#CBD5E1>{outCategory}</color>";
        outDescTMP.text = $"<color=#FFD700><b>{outAmount}x {outName}</b></color>\n<size=9.5>{outSub}</size>";

        // --- MŨI TÊN CHUYỂN ĐỔI (ArrowBadge Image + child Text) ---
        GameObject arrowBgGO = new GameObject("ArrowBadge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        arrowBgGO.transform.SetParent(cardGO.transform, false);
        LayoutElement arrowLE = arrowBgGO.GetComponent<LayoutElement>();
        arrowLE.minWidth = 28f;
        arrowLE.preferredWidth = 28f;
        arrowLE.minHeight = 28f;
        arrowLE.preferredHeight = 28f;
        Image arrowBgImg = arrowBgGO.GetComponent<Image>();
        if (DungeonUIAssetHelper.ApplyCompSprite(arrowBgImg, "Comp_Merchant_ArrowBadge", sliced: false, Color.white))
        {
            arrowBgImg.type = Image.Type.Simple;
        }
        else
        {
            arrowBgImg.color = new Color(0.18f, 0.12f, 0.08f, 0.9f);
        }

        GameObject arrowGO = new GameObject("ArrowText", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(arrowBgGO.transform, false);
        RectTransform arrowTextRT = arrowGO.GetComponent<RectTransform>();
        arrowTextRT.anchorMin = Vector2.zero;
        arrowTextRT.anchorMax = Vector2.one;
        arrowTextRT.offsetMin = Vector2.zero;
        arrowTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(arrowTMP);
        arrowTMP.fontSize = 14f;
        arrowTMP.fontStyle = FontStyles.Bold;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.text = "⇄";
        arrowTMP.color = new Color(1f, 0.88f, 0.51f);

        // --- CỘT 2: ITEM DÙNG ĐỔI (PAY/COST) ---
        GameObject inColGO = new GameObject("InputCol", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        inColGO.transform.SetParent(cardGO.transform, false);
        LayoutElement inColLE = inColGO.GetComponent<LayoutElement>();
        inColLE.minWidth = 135f;
        inColLE.flexibleWidth = 1f;
        inColLE.minHeight = 44f;
        inColLE.preferredHeight = 44f;

        HorizontalLayoutGroup inHlg = inColGO.GetComponent<HorizontalLayoutGroup>();
        inHlg.spacing = 6f;
        inHlg.childAlignment = TextAnchor.MiddleLeft;
        inHlg.childControlWidth = true;
        inHlg.childControlHeight = true;
        inHlg.childForceExpandWidth = false;
        inHlg.childForceExpandHeight = false;

        // Slot Box dùng đổi
        GameObject inSlotGO = new GameObject("InSlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        inSlotGO.transform.SetParent(inColGO.transform, false);
        LayoutElement inSlotLE = inSlotGO.GetComponent<LayoutElement>();
        inSlotLE.minWidth = 40f;
        inSlotLE.preferredWidth = 40f;
        inSlotLE.minHeight = 40f;
        inSlotLE.preferredHeight = 40f;
        Image inSlotImg = inSlotGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(inSlotImg, "Comp_Merchant_ItemSlot", sliced: true, Color.white))
        {
            DungeonUIAssetHelper.StyleSlotImage(inSlotImg, new Color(0.24f, 0.26f, 0.35f, 1f));
        }

        GameObject inIconGO = new GameObject("InIcon", typeof(RectTransform), typeof(Image));
        inIconGO.transform.SetParent(inSlotGO.transform, false);
        RectTransform inIconRT = inIconGO.GetComponent<RectTransform>();
        inIconRT.anchorMin = Vector2.zero;
        inIconRT.anchorMax = Vector2.one;
        inIconRT.offsetMin = new Vector2(5, 5);
        inIconRT.offsetMax = new Vector2(-5, -5);
        Image inIcon = inIconGO.GetComponent<Image>();
        inIcon.preserveAspect = true;
        if (inItem != null) inIcon.sprite = inItem.itemIcon;

        // Label thông tin item dùng đổi
        GameObject inDescGO = new GameObject("InDesc", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        inDescGO.transform.SetParent(inColGO.transform, false);
        LayoutElement inDescLE = inDescGO.GetComponent<LayoutElement>();
        inDescLE.minWidth = 80f;
        inDescLE.flexibleWidth = 1f;
        inDescLE.minHeight = 42f;
        inDescLE.preferredHeight = 42f;
        TextMeshProUGUI inDescTMP = inDescGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(inDescTMP, withOutline: true);
        inDescTMP.fontSize = 11.5f;
        inDescTMP.enableAutoSizing = true;
        inDescTMP.fontSizeMin = 9f;
        inDescTMP.fontSizeMax = 11.5f;
        inDescTMP.alignment = TextAlignmentOptions.MidlineLeft;
        inDescTMP.textWrappingMode = TextWrappingModes.Normal;
        inDescTMP.overflowMode = TextOverflowModes.Ellipsis;
        string inColor = haveInput >= inAmount ? "#4ADE80" : "#FF6B6B";
        string inName = inItem != null ? inItem.itemName : "Nguyên liệu";
        inDescTMP.text = $"<b>{inAmount}x {inName}</b>\n<size=10><color=#CBD5E1>Cần:</color> {inAmount} (<color={inColor}>Có: {haveInput}</color>)</size>";

        // --- NÚT TRADE ---
        GameObject btnGO = new GameObject("BtnTrade", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnGO.transform.SetParent(cardGO.transform, false);
        LayoutElement btnLE = btnGO.GetComponent<LayoutElement>();
        btnLE.minWidth = 78f;
        btnLE.preferredWidth = 78f;
        btnLE.minHeight = 36f;
        btnLE.preferredHeight = 36f;

        Image btnBg = btnGO.GetComponent<Image>();
        Button btn = btnGO.GetComponent<Button>();
        Color btnTint = canAfford ? Color.white : new Color(0.55f, 0.55f, 0.58f, 0.85f);
        if (!DungeonUIAssetHelper.ApplyCompButton(btn, btnBg, "Comp_Merchant_TradeBtn", btnTint))
        {
            DungeonUIAssetHelper.StyleSlicedFrame(btnBg, canAfford ? new Color(0.18f, 0.52f, 0.34f, 1f) : new Color(0.24f, 0.26f, 0.32f, 0.8f));
        }

        btn.interactable = canAfford;
        btn.onClick.AddListener(() => onTradeClicked?.Invoke());

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTxtRT = btnTextGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = new Vector2(4, 2);
        btnTxtRT.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTMP, withOutline: true);
        btnTMP.fontSize = 12f;
        btnTMP.enableAutoSizing = true;
        btnTMP.fontSizeMin = 9f;
        btnTMP.fontSizeMax = 12f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.textWrappingMode = TextWrappingModes.NoWrap;
        btnTMP.text = canAfford ? "ĐỔI" : "THIẾU";
        btnTMP.color = canAfford ? Color.white : new Color(0.72f, 0.72f, 0.72f);
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

    public void EnsureUIBuilt()
    {
        // UI is authored and bound directly in the scene.
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
