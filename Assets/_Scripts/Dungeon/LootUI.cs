using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng UI Chiến Lợi Phẩm sau khi tiêu diệt quái / kết thúc combat:
/// - Thiết kế cân đối đối xứng 50/50 giữa 2 cột: [VẬT PHẨM RƠI] và [BA LÔ CỦA BẠN]
/// - Cả 2 bên đều dùng lưới chuẩn 6 cột, tự động hoàn thiện hàng cuối (padding 3 slot khóa để đủ 6x6)
/// - Khi đã nhặt hết: Ẩn các ô trống, hiện bảng thông báo 'ĐÃ THU NHẬP TẤT CẢ!' trang nhã
/// - Nút [LOOT HẾT] tự động vô hiệu hóa & mờ đi khi không còn đồ để loot
/// - Nút [TIẾP TỤC] tự động sáng xanh ngọc nổi bật khi đã gom hết đồ, hướng dẫn người chơi đi tiếp
/// - Khắc phục triệt để lỗi chữ 'Victory! All enemies defeated.' từ CombatUI xuyên qua màn hình
/// </summary>
public class LootUI : MonoBehaviour
{
    public static LootUI Instance { get; private set; }

    [Header("Panel Roots")]
    [SerializeField] private GameObject lootPanel;
    [SerializeField] private Transform lootSlotsParent;
    [SerializeField] private Transform backpackSlotsParent;
    [SerializeField] private ScrollRect lootScrollRect;
    [SerializeField] private ScrollRect bpScrollRect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject slotPrefab;

    [Header("Controls")]
    [SerializeField] private Button lootAllButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Image lootAllBtnImg;
    [SerializeField] private Image contBtnImg;
    [SerializeField] private TextMeshProUGUI lootAllTxt;
    [SerializeField] private TextMeshProUGUI contTxt;

    [Header("Labels & Notices")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI lootHeaderTitle;
    [SerializeField] private TextMeshProUGUI backpackHeaderTitle;
    [SerializeField] private GameObject emptyLootNoticeGO;

    private ItemContainer lootContainer;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> lootButtons = new();
    private readonly List<InventoryButton> backpackButtons = new();
    private readonly List<GameObject> paddingSlots = new();

    private System.Action onLootClosed;

    public bool IsOpen => lootPanel != null && lootPanel.activeSelf;

    public static LootUI EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureUIBuilt();
            return Instance;
        }

        LootUI existing = FindFirstObjectByType<LootUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            Instance.EnsureUIBuilt();
            return Instance;
        }

        Canvas c = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (c != null)
        {
            GameObject go = new("LootUI_Manager", typeof(LootUI));
            go.transform.SetParent(c.transform, false);
            Instance = go.GetComponent<LootUI>();
            Instance.canvas = c;
            Instance.EnsureUIBuilt();
            return Instance;
        }

        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureUIBuilt();

        if (lootPanel != null)
        {
            lootPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (lootContainer != null)
        {
            lootContainer.OnInventoryChange -= RefreshAll;
            Destroy(lootContainer);
            lootContainer = null;
        }
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshAll;
        }
        if (Instance == this) Instance = null;
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
        if (lootPanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[LootUI] No Canvas found in scene!");
            return;
        }

        // 1. Fullscreen Dark Overlay (ngăn chặn triệt để bleed-through từ combat UI phía sau)
        GameObject overlayGO = new("LootPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        lootPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);

        // 2. Main Window Modal with 9-Sliced Frame
        GameObject modalGO = new("LootModal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(overlayGO.transform, false);

        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(940, 580);

        Image modalImg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Header Banner
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(modalGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 74);

        Image headerImg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.22f, 0.16f, 0.06f, 1f));

        // Chest Icon
        GameObject iconGO = new("ChestIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(headerGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(38, 0);
        iconRT.sizeDelta = new Vector2(48, 48);

        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = DungeonUIAssetHelper.GetChestSprite();

        // Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(76, 24);
        titleRT.offsetMax = new Vector2(-20, -6);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.text = "CHIẾN LỢI PHẨM QUÁI RƠI";
        titleText.fontSize = 20f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.25f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Subtitle Text
        GameObject subGO = new("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(headerGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.offsetMin = new Vector2(76, 4);
        subRT.offsetMax = new Vector2(-20, -32);

        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.text = "Kéo thả vật phẩm vào Ba Lô, hoặc ấn [LOOT HẾT] để gom nhanh toàn bộ đồ.";
        subtitleText.fontSize = 12f;
        subtitleText.color = new Color(0.85f, 0.88f, 0.95f);
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;

        // 4. Body (Cân đối 50/50: Cột Trái 48.8% | Khoảng cách giữa 2.4% | Cột Phải 48.8%)
        GameObject bodyGO = new("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(modalGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(20, 68);
        bodyRT.offsetMax = new Vector2(-20, -82);

        // --- Left Column: Loot (48.8% width) ---
        GameObject leftColGO = new("Left_LootColumn", typeof(RectTransform), typeof(Image));
        leftColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.488f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = Vector2.zero;

        Image leftBg = leftColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftBg, new Color(0.14f, 0.16f, 0.22f, 0.95f));

        GameObject leftHeaderGO = new("HeaderBar", typeof(RectTransform), typeof(Image));
        leftHeaderGO.transform.SetParent(leftColGO.transform, false);
        RectTransform leftHeaderRT = leftHeaderGO.GetComponent<RectTransform>();
        leftHeaderRT.anchorMin = new Vector2(0, 1);
        leftHeaderRT.anchorMax = new Vector2(1, 1);
        leftHeaderRT.pivot = new Vector2(0.5f, 1);
        leftHeaderRT.sizeDelta = new Vector2(-12, 32);
        leftHeaderRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StyleSlicedFrame(leftHeaderGO.GetComponent<Image>(), new Color(0.19f, 0.22f, 0.30f, 1f));

        GameObject leftLabelGO = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        leftLabelGO.transform.SetParent(leftHeaderGO.transform, false);
        RectTransform leftLabelRT = leftLabelGO.GetComponent<RectTransform>();
        leftLabelRT.anchorMin = Vector2.zero;
        leftLabelRT.anchorMax = Vector2.one;
        leftLabelRT.offsetMin = new Vector2(10, 0);
        leftLabelRT.offsetMax = new Vector2(-10, 0);
        lootHeaderTitle = leftLabelGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(lootHeaderTitle);
        lootHeaderTitle.text = "VẬT PHẨM RƠI";
        lootHeaderTitle.fontSize = 12.5f;
        lootHeaderTitle.fontStyle = FontStyles.Bold;
        lootHeaderTitle.color = new Color(1f, 0.85f, 0.35f);
        lootHeaderTitle.alignment = TextAlignmentOptions.MidlineLeft;

        // Left ScrollView
        GameObject leftScrollGO = new("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        leftScrollGO.transform.SetParent(leftColGO.transform, false);
        lootScrollRect = leftScrollGO.GetComponent<ScrollRect>();
        RectTransform leftScrollRT = leftScrollGO.GetComponent<RectTransform>();
        leftScrollRT.anchorMin = Vector2.zero;
        leftScrollRT.anchorMax = Vector2.one;
        leftScrollRT.offsetMin = new Vector2(8, 8);
        leftScrollRT.offsetMax = new Vector2(-8, -42);
        Image leftScrollImg = leftScrollGO.GetComponent<Image>();
        leftScrollImg.color = new Color(0, 0, 0, 0.2f);

        GameObject leftViewGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        leftViewGO.transform.SetParent(leftScrollGO.transform, false);
        RectTransform leftViewRT = leftViewGO.GetComponent<RectTransform>();
        leftViewRT.anchorMin = Vector2.zero;
        leftViewRT.anchorMax = Vector2.one;
        leftViewRT.offsetMin = Vector2.zero;
        leftViewRT.offsetMax = new Vector2(-14, 0);

        GameObject leftContentGO = new("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        leftContentGO.transform.SetParent(leftViewGO.transform, false);
        lootSlotsParent = leftContentGO.transform;
        RectTransform leftContentRT = leftContentGO.GetComponent<RectTransform>();
        leftContentRT.anchorMin = new Vector2(0, 1);
        leftContentRT.anchorMax = new Vector2(1, 1);
        leftContentRT.pivot = new Vector2(0.5f, 1);
        leftContentRT.sizeDelta = new Vector2(0, 300);

        GridLayoutGroup leftGrid = leftContentGO.GetComponent<GridLayoutGroup>();
        leftGrid.cellSize = new Vector2(54, 54);
        leftGrid.spacing = new Vector2(6, 6);
        leftGrid.padding = new RectOffset(8, 8, 8, 8);
        leftGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        leftGrid.constraintCount = 6;

        ContentSizeFitter leftCsf = leftContentGO.GetComponent<ContentSizeFitter>();
        leftCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        lootScrollRect.content = leftContentRT;
        lootScrollRect.viewport = leftViewRT;
        lootScrollRect.horizontal = false;
        lootScrollRect.vertical = true;
        lootScrollRect.scrollSensitivity = 25f;
        lootScrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Scrollbar thanh lịch đồng bộ màu vàng đồng cổ
        Color themeScrollColor = new(0.75f, 0.60f, 0.30f, 0.95f);
        DungeonUIAssetHelper.CreateVerticalScrollbar(leftScrollGO.transform, lootScrollRect, themeScrollColor);

        // --- Right Column: Backpack (48.8% width) ---
        GameObject rightColGO = new("Right_BackpackColumn", typeof(RectTransform), typeof(Image));
        rightColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.512f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = Vector2.zero;
        rightColRT.offsetMax = Vector2.zero;

        Image rightBg = rightColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightBg, new Color(0.14f, 0.16f, 0.22f, 0.95f));

        GameObject rightHeaderGO = new("HeaderBar", typeof(RectTransform), typeof(Image));
        rightHeaderGO.transform.SetParent(rightColGO.transform, false);
        RectTransform rightHeaderRT = rightHeaderGO.GetComponent<RectTransform>();
        rightHeaderRT.anchorMin = new Vector2(0, 1);
        rightHeaderRT.anchorMax = new Vector2(1, 1);
        rightHeaderRT.pivot = new Vector2(0.5f, 1);
        rightHeaderRT.sizeDelta = new Vector2(-12, 32);
        rightHeaderRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StyleSlicedFrame(rightHeaderGO.GetComponent<Image>(), new Color(0.19f, 0.22f, 0.30f, 1f));

        GameObject bpTitleGO = new("BackpackTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpTitleGO.transform.SetParent(rightHeaderGO.transform, false);
        RectTransform bpTitleRT = bpTitleGO.GetComponent<RectTransform>();
        bpTitleRT.anchorMin = Vector2.zero;
        bpTitleRT.anchorMax = Vector2.one;
        bpTitleRT.offsetMin = new Vector2(10, 0);
        bpTitleRT.offsetMax = new Vector2(-10, 0);
        backpackHeaderTitle = bpTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(backpackHeaderTitle);
        backpackHeaderTitle.text = "BA LÔ CỦA BẠN";
        backpackHeaderTitle.fontSize = 12.5f;
        backpackHeaderTitle.fontStyle = FontStyles.Bold;
        backpackHeaderTitle.color = new Color(0.55f, 0.95f, 1f);
        backpackHeaderTitle.alignment = TextAlignmentOptions.MidlineLeft;

        // Right ScrollView
        GameObject rightScrollGO = new("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        rightScrollGO.transform.SetParent(rightColGO.transform, false);
        bpScrollRect = rightScrollGO.GetComponent<ScrollRect>();
        RectTransform rightScrollRT = rightScrollGO.GetComponent<RectTransform>();
        rightScrollRT.anchorMin = Vector2.zero;
        rightScrollRT.anchorMax = Vector2.one;
        rightScrollRT.offsetMin = new Vector2(8, 8);
        rightScrollRT.offsetMax = new Vector2(-8, -42);
        Image rightScrollImg = rightScrollGO.GetComponent<Image>();
        rightScrollImg.color = new Color(0, 0, 0, 0.2f);

        GameObject rightViewGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        rightViewGO.transform.SetParent(rightScrollGO.transform, false);
        RectTransform rightViewRT = rightViewGO.GetComponent<RectTransform>();
        rightViewRT.anchorMin = Vector2.zero;
        rightViewRT.anchorMax = Vector2.one;
        rightViewRT.offsetMin = Vector2.zero;
        rightViewRT.offsetMax = new Vector2(-14, 0);

        GameObject rightContentGO = new("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        rightContentGO.transform.SetParent(rightViewGO.transform, false);
        backpackSlotsParent = rightContentGO.transform;
        RectTransform rightContentRT = rightContentGO.GetComponent<RectTransform>();
        rightContentRT.anchorMin = new Vector2(0, 1);
        rightContentRT.anchorMax = new Vector2(1, 1);
        rightContentRT.pivot = new Vector2(0.5f, 1);
        rightContentRT.sizeDelta = new Vector2(0, 300);

        GridLayoutGroup rightGrid = rightContentGO.GetComponent<GridLayoutGroup>();
        rightGrid.cellSize = new Vector2(54, 54);
        rightGrid.spacing = new Vector2(6, 6);
        rightGrid.padding = new RectOffset(8, 8, 8, 8);
        rightGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        rightGrid.constraintCount = 6;

        ContentSizeFitter rightCsf = rightContentGO.GetComponent<ContentSizeFitter>();
        rightCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bpScrollRect.content = rightContentRT;
        bpScrollRect.viewport = rightViewRT;
        bpScrollRect.horizontal = false;
        bpScrollRect.vertical = true;
        bpScrollRect.scrollSensitivity = 25f;
        bpScrollRect.movementType = ScrollRect.MovementType.Clamped;

        DungeonUIAssetHelper.CreateVerticalScrollbar(rightScrollGO.transform, bpScrollRect, themeScrollColor);

        // 5. Footer Controls
        GameObject hintGO = new("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(modalGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0.58f, 0);
        hintRT.offsetMin = new Vector2(24, 14);
        hintRT.offsetMax = new Vector2(0, 50);

        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(hintText);
        hintText.fontSize = 12f;
        hintText.alignment = TextAlignmentOptions.MidlineLeft;

        // Button [LOOT HẾT]
        GameObject lootAllBtnGO = new("LootAllButton", typeof(RectTransform), typeof(Image), typeof(Button));
        lootAllBtnGO.transform.SetParent(modalGO.transform, false);
        RectTransform lootAllRT = lootAllBtnGO.GetComponent<RectTransform>();
        lootAllRT.anchorMin = new Vector2(1, 0);
        lootAllRT.anchorMax = new Vector2(1, 0);
        lootAllRT.pivot = new Vector2(1, 0);
        lootAllRT.anchoredPosition = new Vector2(-180, 14);
        lootAllRT.sizeDelta = new Vector2(145, 38);

        lootAllButton = lootAllBtnGO.GetComponent<Button>();
        lootAllBtnImg = lootAllBtnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleButton(lootAllButton, lootAllBtnImg, new Color(0.85f, 0.65f, 0.15f));

        GameObject lootAllTxtGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        lootAllTxtGO.transform.SetParent(lootAllBtnGO.transform, false);
        RectTransform lootAllTxtRT = lootAllTxtGO.GetComponent<RectTransform>();
        lootAllTxtRT.anchorMin = Vector2.zero;
        lootAllTxtRT.anchorMax = Vector2.one;
        lootAllTxtRT.sizeDelta = Vector2.zero;
        lootAllTxt = lootAllTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(lootAllTxt);
        lootAllTxt.text = "LOOT HẾT";
        lootAllTxt.fontSize = 13f;
        lootAllTxt.fontStyle = FontStyles.Bold;
        lootAllTxt.alignment = TextAlignmentOptions.Center;
        lootAllTxt.color = Color.white;

        // Button [TIẾP TỤC]
        GameObject contBtnGO = new("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        contBtnGO.transform.SetParent(modalGO.transform, false);
        RectTransform contRT = contBtnGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(1, 0);
        contRT.anchorMax = new Vector2(1, 0);
        contRT.pivot = new Vector2(1, 0);
        contRT.anchoredPosition = new Vector2(-20, 14);
        contRT.sizeDelta = new Vector2(145, 38);

        continueButton = contBtnGO.GetComponent<Button>();
        contBtnImg = contBtnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleButton(continueButton, contBtnImg, new Color(0.18f, 0.55f, 0.28f));

        GameObject contTxtGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        contTxtGO.transform.SetParent(contBtnGO.transform, false);
        RectTransform contTxtRT = contTxtGO.GetComponent<RectTransform>();
        contTxtRT.anchorMin = Vector2.zero;
        contTxtRT.anchorMax = Vector2.one;
        contTxtRT.sizeDelta = Vector2.zero;
        contTxt = contTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(contTxt);
        contTxt.text = "TIẾP TỤC [->]";
        contTxt.fontSize = 13f;
        contTxt.fontStyle = FontStyles.Bold;
        contTxt.alignment = TextAlignmentOptions.Center;
        contTxt.color = Color.white;
    }

    private void BindButtonEvents()
    {
        if (lootAllButton != null)
        {
            lootAllButton.onClick.RemoveAllListeners();
            lootAllButton.onClick.AddListener(OnLootAllClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    /// <summary>
    /// Mở bảng UI nhặt đồ với danh sách chiến lợi phẩm thu được và ba lô người chơi
    /// </summary>
    public void Open(List<ItemSlot> incomingLoot, ItemContainer playerBackpack, System.Action onClose)
    {
        EnsureUIBuilt();

        // Ẩn chữ và panel thông báo chiến thắng ở nền sau nếu có để tránh che khuất
        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.combatLogText != null) CombatUI.Instance.combatLogText.gameObject.SetActive(false);
            if (CombatUI.Instance.victoryPanel != null) CombatUI.Instance.victoryPanel.SetActive(false);
            if (CombatUI.Instance.rewardPanel != null) CombatUI.Instance.rewardPanel.SetActive(false);
        }

        backpackContainer = playerBackpack;
        onLootClosed = onClose;

        // Tính toán số lượng slot Loot vừa đủ (làm tròn lên bội số của 6 để hàng luôn đầy đủ)
        int rawLootCount = incomingLoot != null ? incomingLoot.Count : 0;
        int lootCapacity = Mathf.Max(6, Mathf.CeilToInt(Mathf.Max(1, rawLootCount) / 6f) * 6);

        if (lootContainer != null)
        {
            lootContainer.OnInventoryChange -= RefreshAll;
            Destroy(lootContainer);
            lootContainer = null;
        }

        lootContainer = ScriptableObject.CreateInstance<ItemContainer>();
        lootContainer.maxSlots = lootCapacity;
        lootContainer.itemSlots = new ItemSlot[lootContainer.maxSlots];
        for (int i = 0; i < lootContainer.maxSlots; i++)
        {
            lootContainer.itemSlots[i] = new ItemSlot();
        }

        int totalItemsAdded = 0;
        if (incomingLoot != null)
        {
            for (int i = 0; i < incomingLoot.Count; i++)
            {
                var slot = incomingLoot[i];
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    lootContainer.AddItem(slot.itemData, slot.amount);
                    totalItemsAdded += slot.amount;
                }
            }
        }

        lootContainer.OnInventoryChange -= RefreshAll;
        lootContainer.OnInventoryChange += RefreshAll;
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshAll;
            backpackContainer.OnInventoryChange += RefreshAll;
        }

        BuildSlots(lootSlotsParent, lootContainer, lootButtons);

        if (backpackContainer != null && backpackSlotsParent != null)
        {
            BuildSlots(backpackSlotsParent, backpackContainer, backpackButtons);
        }

        BindButtonEvents();
        RefreshAll();

        if (totalItemsAdded > 0)
        {
            if (hintText != null)
            {
                hintText.text = "Kéo thả vật phẩm vào Ba Lô, hoặc ấn [LOOT HẾT] để gom nhanh toàn bộ đồ.";
                hintText.color = new Color(0.9f, 0.95f, 1f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = "<color=#68D391>Không có vật phẩm mới. Bạn có thể sắp xếp lại ba lô hoặc bấm [TIẾP TỤC].</color>";
            }
        }

        if (lootPanel != null)
        {
            lootPanel.SetActive(true);
            lootPanel.transform.SetAsLastSibling();
        }

        if (lootScrollRect != null) lootScrollRect.verticalNormalizedPosition = 1f;
        if (bpScrollRect != null) bpScrollRect.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// Đóng bảng Loot UI và gọi callback tiếp tục
    /// </summary>
    public void Close()
    {
        if (CombatUI.Instance != null && CombatUI.Instance.combatLogText != null)
        {
            CombatUI.Instance.combatLogText.gameObject.SetActive(true);
        }

        if (lootContainer != null)
        {
            lootContainer.OnInventoryChange -= RefreshAll;
            Destroy(lootContainer);
            lootContainer = null;
        }

        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshAll;
        }

        if (lootPanel != null)
        {
            lootPanel.SetActive(false);
        }

        System.Action callback = onLootClosed;
        onLootClosed = null;
        callback?.Invoke();
    }

    private void OnContinueClicked()
    {
        Close();
    }

    private void OnLootAllClicked()
    {
        if (lootContainer == null || backpackContainer == null)
            return;

        bool hasLootLeft = false;
        int itemsLooted = 0;

        for (int i = 0; i < lootContainer.itemSlots.Length; i++)
        {
            ItemSlot slot = lootContainer.itemSlots[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0)
                continue;

            bool canTake = false;
            if (slot.itemData.isStackable && backpackContainer.HasItem(slot.itemData))
            {
                canTake = true;
            }
            else if (backpackContainer.GetEmptySlot() != -1)
            {
                canTake = true;
            }

            if (canTake)
            {
                backpackContainer.AddItem(slot.itemData, slot.amount);
                itemsLooted += slot.amount;
                slot.itemData = null;
                slot.amount = 0;
            }
            else
            {
                hasLootLeft = true;
            }
        }

        lootContainer.NotifyChange();
        backpackContainer.NotifyChange();
        RefreshAll();

        if (hasLootLeft)
        {
            if (hintText != null)
            {
                hintText.text = "Ba lô đã đầy! Hãy dọn dẹp hoặc kéo thả bớt vật phẩm qua lại.";
                hintText.color = new Color(1f, 0.6f, 0.3f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = itemsLooted > 0
                    ? $"<color=#68D391>Đã gom thành công {itemsLooted} vật phẩm vào Ba Lô! Ấn [TIẾP TỤC] để tiến bước.</color>"
                    : "<color=#68D391>Đã gom hết đồ. Hãy ấn [TIẾP TỤC] để tiến bước!</color>";
            }
        }
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = GetCanvas();

        if (slotPrefab == null)
        {
            slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
        }

        InventoryButton[] existing = parent.GetComponentsInChildren<InventoryButton>(true);
        slotList.Clear();

        int existingCount = existing != null ? existing.Length : 0;
        for (int i = 0; i < container.maxSlots; i++)
        {
            InventoryButton btn = null;
            if (i < existingCount)
            {
                btn = existing[i];
            }
            else if (slotPrefab != null)
            {
                GameObject slotObj = Instantiate(slotPrefab, parent);
                slotObj.name = $"Slot_{i}";
                btn = slotObj.GetComponent<InventoryButton>();
            }

            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                btn.SetSlotData(container, i);
                btn.SetCanvas(currentCanvas);
                slotList.Add(btn);
            }
        }

        // Hoàn thiện hàng cuối cho Ba Lô (33 slot + 3 slot khóa padding = 36 slot, thành lưới 6x6 vuông vức)
        if (container == backpackContainer && container.maxSlots == 33)
        {
            int neededPadding = 3;
            for (int p = 0; p < neededPadding; p++)
            {
                int padIndex = container.maxSlots + p;
                InventoryButton padBtn = null;
                if (padIndex < existingCount)
                {
                    padBtn = existing[padIndex];
                }
                else if (slotPrefab != null)
                {
                    GameObject padObj = Instantiate(slotPrefab, parent);
                    padObj.name = $"PadSlot_{padIndex}";
                    padBtn = padObj.GetComponent<InventoryButton>();
                }

                if (padBtn != null)
                {
                    padBtn.gameObject.SetActive(true);
                    padBtn.ClearItem();

                    Image padImg = padBtn.GetComponent<Image>();
                    if (padImg != null)
                    {
                        padImg.color = new Color(0.12f, 0.15f, 0.22f, 0.45f);
                    }
                    Button b = padBtn.GetComponent<Button>();
                    if (b != null) b.interactable = false;
                }
            }
        }
        else
        {
            // Ẩn các slot dư thừa
            for (int i = container.maxSlots; i < existingCount; i++)
            {
                existing[i].gameObject.SetActive(false);
            }
        }
    }

    public void RefreshAll()
    {
        RefreshPanel(lootButtons, lootContainer);
        RefreshPanel(backpackButtons, backpackContainer);
        UpdateBackpackTitle();
        UpdateLootHeader();
        UpdateControlsState();
    }

    private void UpdateBackpackTitle()
    {
        if (backpackHeaderTitle == null || backpackContainer == null) return;
        int filled = 0;
        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            if (backpackContainer.itemSlots[i] != null && backpackContainer.itemSlots[i].itemData != null && backpackContainer.itemSlots[i].amount > 0)
            {
                filled++;
            }
        }
        backpackHeaderTitle.text = $"BA LÔ CỦA BẠN ({filled}/{backpackContainer.maxSlots})";
    }

    private void UpdateLootHeader()
    {
        if (lootHeaderTitle != null)
        {
            lootHeaderTitle.text = "VẬT PHẨM RƠI";
        }

        if (lootSlotsParent != null)
        {
            lootSlotsParent.gameObject.SetActive(true);
        }
    }

    private void UpdateControlsState()
    {
        int remainingCount = 0;
        if (lootContainer != null)
        {
            for (int i = 0; i < lootContainer.itemSlots.Length; i++)
            {
                var s = lootContainer.itemSlots[i];
                if (s != null && s.itemData != null && s.amount > 0)
                    remainingCount++;
            }
        }

        bool hasLoot = remainingCount > 0;
        if (lootAllButton != null)
        {
            lootAllButton.interactable = hasLoot;
            if (lootAllBtnImg != null)
            {
                lootAllBtnImg.color = hasLoot
                    ? new Color(0.85f, 0.65f, 0.15f, 1f)
                    : new Color(0.40f, 0.32f, 0.18f, 0.65f);
            }
            if (lootAllTxt != null)
            {
                lootAllTxt.text = "LOOT HẾT";
                lootAllTxt.color = hasLoot ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        if (continueButton != null)
        {
            if (contBtnImg != null)
            {
                contBtnImg.color = new Color(0.18f, 0.48f, 0.28f, 1f);
            }
            if (contTxt != null)
            {
                contTxt.text = "TIẾP TỤC [->]";
            }
        }
    }

    private void RefreshPanel(List<InventoryButton> buttons, ItemContainer container)
    {
        if (container == null) return;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;

            if (i < container.itemSlots.Length && container.itemSlots[i] != null && container.itemSlots[i].itemData != null)
            {
                buttons[i].SetItem(container.itemSlots[i].itemData, container.itemSlots[i].amount);
            }
            else
            {
                buttons[i].ClearItem();
            }
        }
    }
}
