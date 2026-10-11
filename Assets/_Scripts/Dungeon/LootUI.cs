using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng UI Chiến Lợi Phẩm sau khi tiêu diệt quái / kết thúc combat:
/// - Bên trái: Danh sách vật phẩm quái rơi (Loot Container)
/// - Bên phải: Toàn bộ ba lô của người chơi (Backpack Container)
/// - Kéo thả vật phẩm tự do giữa 2 bên để nhặt đồ hoặc sắp xếp ba lô (sử dụng InventoryButton)
/// - Nút "Loot Hết": tự động gom toàn bộ vật phẩm còn chỗ vào ba lô
/// - Nút "Tiếp Tục": đóng UI và tiếp tục chuyến thám hiểm
/// Tự động xây dựng giao diện chất lượng cao với DungeonUIAssetHelper nếu chưa có sẵn trong Scene.
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

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI backpackHeaderTitle;

    private ItemContainer lootContainer;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> lootButtons = new();
    private readonly List<InventoryButton> backpackButtons = new();

    private System.Action onLootClosed;

    public bool IsOpen => lootPanel != null && lootPanel.activeSelf;

    public static LootUI EnsureInstance()
    {
        if (Instance != null) return Instance;

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

        backpackContainer = playerBackpack;
        onLootClosed = onClose;

        // Container cho Loot (15 ô, 3 hàng x 5 cột)
        int lootCapacity = 15;
        if (incomingLoot != null && incomingLoot.Count > lootCapacity)
        {
            lootCapacity = Mathf.CeilToInt(incomingLoot.Count / 5f) * 5;
        }

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
                hintText.text = "💡 Kéo thả vật phẩm vào Ba Lô, hoặc ấn <b>[LOOT HẾT]</b> để gom nhanh toàn bộ đồ.";
                hintText.color = new Color(0.9f, 0.95f, 1f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = "Không có vật phẩm mới. Bạn có thể sắp xếp lại ba lô hoặc bấm [TIẾP TỤC].";
                hintText.color = new Color(0.7f, 0.75f, 0.85f);
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
                hintText.text = "⚠️ Ba lô đã đầy! Hãy dọn dẹp hoặc kéo thả bớt vật phẩm qua lại.";
                hintText.color = new Color(1f, 0.6f, 0.3f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = itemsLooted > 0
                    ? $"✅ Đã gom thành công {itemsLooted} vật phẩm vào Ba Lô! Ấn [TIẾP TỤC] để tiến bước."
                    : "Đã gom hết đồ. Hãy ấn [TIẾP TỤC]!";
                hintText.color = new Color(0.4f, 1f, 0.5f);
            }
        }
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = GetCanvas();

        // 1. Tái sử dụng các slot sẵn có nếu đúng số lượng
        bool allSlotsValid = slotList.Count == container.maxSlots;
        if (allSlotsValid)
        {
            for (int i = 0; i < slotList.Count; i++)
            {
                if (slotList[i] == null)
                {
                    allSlotsValid = false;
                    break;
                }
            }
        }

        if (allSlotsValid)
        {
            for (int i = 0; i < slotList.Count; i++)
            {
                slotList[i].gameObject.SetActive(true);
                slotList[i].SetSlotData(container, i);
                slotList[i].SetCanvas(currentCanvas);
            }
            return;
        }

        // 2. Tận dụng slot con có sẵn trong parent
        InventoryButton[] existing = parent.GetComponentsInChildren<InventoryButton>(true);
        if (existing != null && existing.Length >= container.maxSlots)
        {
            slotList.Clear();
            for (int i = 0; i < container.maxSlots; i++)
            {
                existing[i].gameObject.SetActive(true);
                existing[i].SetSlotData(container, i);
                existing[i].SetCanvas(currentCanvas);
                slotList.Add(existing[i]);
            }
            for (int i = container.maxSlots; i < existing.Length; i++)
            {
                existing[i].gameObject.SetActive(false);
            }
            return;
        }

        // 3. Tạo mới nếu chưa có
        foreach (var s in slotList)
        {
            if (s != null) Destroy(s.gameObject);
        }
        slotList.Clear();

        if (slotPrefab == null)
        {
            slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj;
            if (slotPrefab != null)
            {
                slotObj = Instantiate(slotPrefab, parent);
            }
            else
            {
                slotObj = CreateFallbackSlot(parent, i);
            }

            slotObj.name = $"Slot_{i}";
            InventoryButton btn = slotObj.GetComponent<InventoryButton>();
            if (btn == null) btn = slotObj.AddComponent<InventoryButton>();

            // Ensure background image has raycastTarget enabled for drag & drop
            Image bg = slotObj.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            slotList.Add(btn);
        }
    }

    private GameObject CreateFallbackSlot(Transform parent, int index)
    {
        GameObject slotObj = new($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(InventoryButton));
        slotObj.transform.SetParent(parent, false);

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(62, 62);

        Image bg = slotObj.GetComponent<Image>();
        bg.raycastTarget = true;
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

        // Icon
        GameObject iconGO = new("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotObj.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.gameObject.SetActive(false);

        // Amount Text
        GameObject textGO = new("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(slotObj.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 0.45f);
        textRT.offsetMin = new Vector2(4, 2);
        textRT.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI txt = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(txt);
        txt.fontSize = 13;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.BottomRight;
        txt.color = Color.white;
        txt.raycastTarget = false;
        txt.gameObject.SetActive(false);

        InventoryButton btn = slotObj.GetComponent<InventoryButton>();
        btn.BindComponents(iconImg, txt);

        return slotObj;
    }

    public void RefreshAll()
    {
        RefreshPanel(lootButtons, lootContainer);
        RefreshPanel(backpackButtons, backpackContainer);
        UpdateBackpackTitle();
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
        backpackHeaderTitle.text = $"🎒 BA LÔ CỦA BẠN ({filled}/{backpackContainer.maxSlots})";
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

    #region Procedural UI Building
    public void EnsureUIBuilt()
    {
        if (lootPanel != null && lootSlotsParent != null && backpackSlotsParent != null && continueButton != null)
        {
            return;
        }

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[LootUI] No Canvas found in scene!");
            return;
        }

        // Clean up incomplete / legacy LootPanel if exists
        Transform existingPanel = parentCanvas.transform.Find("LootPanel");
        if (existingPanel != null && (lootSlotsParent == null || continueButton == null))
        {
            existingPanel.gameObject.SetActive(false);
            Destroy(existingPanel.gameObject);
            lootPanel = null;
        }

        // 1. Fullscreen Dark Overlay
        GameObject overlayGO = new("LootPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        lootPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.05f, 0.92f);

        // 2. Main Dialog Frame (900 x 540)
        GameObject frameGO = new("MainFrame", typeof(RectTransform), typeof(Image));
        frameGO.transform.SetParent(overlayGO.transform, false);
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0.5f, 0.5f);
        frameRT.anchorMax = new Vector2(0.5f, 0.5f);
        frameRT.pivot = new Vector2(0.5f, 0.5f);
        frameRT.sizeDelta = new Vector2(900, 540);

        Image frameImg = frameGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(frameImg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Header Bar (Height 70px)
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(frameGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -6);
        headerRT.sizeDelta = new Vector2(-16, 68);

        Image headerBg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerBg, new Color(0.20f, 0.15f, 0.08f, 0.95f));

        // Header Chest Icon
        GameObject iconGO = new("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(headerGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(16, 0);
        iconRT.sizeDelta = new Vector2(48, 48);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = DungeonUIAssetHelper.GetChestSprite();
        iconImg.preserveAspect = true;

        // Header Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(76, -8);
        titleRT.sizeDelta = new Vector2(-90, 28);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 19f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.88f, 0.35f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.text = "⚔️ CHIẾN LỢI PHẨM QUÁI VẬT (MONSTER LOOT)";

        // Header Subtitle Text
        GameObject subGO = new("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(headerGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0, 1);
        subRT.anchoredPosition = new Vector2(76, -38);
        subRT.sizeDelta = new Vector2(-90, 22);
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 12f;
        subtitleText.color = new Color(0.9f, 0.92f, 0.96f);
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleText.text = "Kéo thả vật phẩm vào Ba Lô của bạn, hoặc bấm [LOOT HẾT] để gom toàn bộ.";

        // 4. Body Area (2 Columns)
        GameObject bodyGO = new("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(frameGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(16, 58);
        bodyRT.offsetMax = new Vector2(-16, -78);

        // LEFT COLUMN: Monster Loot Slots
        GameObject leftColGO = new("LeftColumn_Loot", typeof(RectTransform), typeof(Image));
        leftColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.492f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = Vector2.zero;
        Image leftColBg = leftColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftColBg, new Color(0.14f, 0.11f, 0.08f, 0.92f));

        // Left Title Bar
        GameObject leftTitleGO = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        leftTitleGO.transform.SetParent(leftColGO.transform, false);
        RectTransform leftTitleRT = leftTitleGO.GetComponent<RectTransform>();
        leftTitleRT.anchorMin = new Vector2(0, 1);
        leftTitleRT.anchorMax = new Vector2(1, 1);
        leftTitleRT.pivot = new Vector2(0.5f, 1);
        leftTitleRT.anchoredPosition = new Vector2(0, -6);
        leftTitleRT.sizeDelta = new Vector2(-20, 26);
        TextMeshProUGUI leftTitleTmp = leftTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(leftTitleTmp);
        leftTitleTmp.fontSize = 13f;
        leftTitleTmp.fontStyle = FontStyles.Bold;
        leftTitleTmp.color = new Color(1f, 0.85f, 0.35f);
        leftTitleTmp.text = "⚔️ VẬT PHẨM RƠI RA (KÉO THẢ VÀO BALO)";

        // Left Scroll View for Monster Loot
        GameObject lootScrollGO = new("LootScrollView", typeof(RectTransform), typeof(ScrollRect));
        lootScrollGO.transform.SetParent(leftColGO.transform, false);
        RectTransform lootScrollRT = lootScrollGO.GetComponent<RectTransform>();
        lootScrollRT.anchorMin = Vector2.zero;
        lootScrollRT.anchorMax = Vector2.one;
        lootScrollRT.offsetMin = new Vector2(8, 8);
        lootScrollRT.offsetMax = new Vector2(-8, -34);

        lootScrollRect = lootScrollGO.GetComponent<ScrollRect>();
        lootScrollRect.horizontal = false;
        lootScrollRect.vertical = true;
        lootScrollRect.scrollSensitivity = 25f;

        // Viewport with RectMask2D (leaves 14px on right for slider)
        GameObject lootVpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        lootVpGO.transform.SetParent(lootScrollGO.transform, false);
        RectTransform lootVpRT = lootVpGO.GetComponent<RectTransform>();
        lootVpRT.anchorMin = Vector2.zero;
        lootVpRT.anchorMax = Vector2.one;
        lootVpRT.offsetMin = Vector2.zero;
        lootVpRT.offsetMax = new Vector2(-14, 0);

        // Content
        GameObject lootSlotsGO = new("LootSlots", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        lootSlotsGO.transform.SetParent(lootVpGO.transform, false);
        lootSlotsParent = lootSlotsGO.transform;
        RectTransform lootSlotsRT = lootSlotsGO.GetComponent<RectTransform>();
        lootSlotsRT.anchorMin = new Vector2(0, 1);
        lootSlotsRT.anchorMax = new Vector2(1, 1);
        lootSlotsRT.pivot = new Vector2(0.5f, 1);
        lootSlotsRT.sizeDelta = Vector2.zero;

        GridLayoutGroup lootGLG = lootSlotsGO.GetComponent<GridLayoutGroup>();
        lootGLG.cellSize = new Vector2(62, 62);
        lootGLG.spacing = new Vector2(8, 8);
        lootGLG.padding = new RectOffset(6, 6, 6, 6);
        lootGLG.startCorner = GridLayoutGroup.Corner.UpperLeft;
        lootGLG.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter lootCsf = lootSlotsGO.GetComponent<ContentSizeFitter>();
        lootCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        lootScrollRect.viewport = lootVpRT;
        lootScrollRect.content = lootSlotsRT;

        // Vertical Scrollbar Slider for Loot
        DungeonUIAssetHelper.CreateVerticalScrollbar(lootScrollGO.transform, lootScrollRect, new Color(0.85f, 0.65f, 0.2f, 1f));

        // RIGHT COLUMN: Player Backpack Slots
        GameObject rightColGO = new("RightColumn_Backpack", typeof(RectTransform), typeof(Image));
        rightColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.508f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = Vector2.zero;
        rightColRT.offsetMax = Vector2.zero;
        Image rightColBg = rightColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightColBg, new Color(0.08f, 0.12f, 0.17f, 0.92f));

        // Right Title Bar
        GameObject rightTitleGO = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        rightTitleGO.transform.SetParent(rightColGO.transform, false);
        RectTransform rightTitleRT = rightTitleGO.GetComponent<RectTransform>();
        rightTitleRT.anchorMin = new Vector2(0, 1);
        rightTitleRT.anchorMax = new Vector2(1, 1);
        rightTitleRT.pivot = new Vector2(0.5f, 1);
        rightTitleRT.anchoredPosition = new Vector2(0, -6);
        rightTitleRT.sizeDelta = new Vector2(-20, 26);
        backpackHeaderTitle = rightTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(backpackHeaderTitle);
        backpackHeaderTitle.fontSize = 13f;
        backpackHeaderTitle.fontStyle = FontStyles.Bold;
        backpackHeaderTitle.color = new Color(0.55f, 0.85f, 1f);
        backpackHeaderTitle.text = "🎒 BA LÔ CỦA BẠN";

        // Right Scroll View for Player Backpack
        GameObject bpScrollGO = new("BackpackScrollView", typeof(RectTransform), typeof(ScrollRect));
        bpScrollGO.transform.SetParent(rightColGO.transform, false);
        RectTransform bpScrollRT = bpScrollGO.GetComponent<RectTransform>();
        bpScrollRT.anchorMin = Vector2.zero;
        bpScrollRT.anchorMax = Vector2.one;
        bpScrollRT.offsetMin = new Vector2(8, 8);
        bpScrollRT.offsetMax = new Vector2(-8, -34);

        bpScrollRect = bpScrollGO.GetComponent<ScrollRect>();
        bpScrollRect.horizontal = false;
        bpScrollRect.vertical = true;
        bpScrollRect.scrollSensitivity = 25f;

        // Viewport with RectMask2D (leaves 14px on right for slider)
        GameObject bpVpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        bpVpGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform bpVpRT = bpVpGO.GetComponent<RectTransform>();
        bpVpRT.anchorMin = Vector2.zero;
        bpVpRT.anchorMax = Vector2.one;
        bpVpRT.offsetMin = Vector2.zero;
        bpVpRT.offsetMax = new Vector2(-14, 0);

        // Content
        GameObject bpSlotsGO = new("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        bpSlotsGO.transform.SetParent(bpVpGO.transform, false);
        backpackSlotsParent = bpSlotsGO.transform;
        RectTransform bpSlotsRT = bpSlotsGO.GetComponent<RectTransform>();
        bpSlotsRT.anchorMin = new Vector2(0, 1);
        bpSlotsRT.anchorMax = new Vector2(1, 1);
        bpSlotsRT.pivot = new Vector2(0.5f, 1);
        bpSlotsRT.sizeDelta = Vector2.zero;

        GridLayoutGroup bpGLG = bpSlotsGO.GetComponent<GridLayoutGroup>();
        bpGLG.cellSize = new Vector2(62, 62);
        bpGLG.spacing = new Vector2(8, 8);
        bpGLG.padding = new RectOffset(6, 6, 6, 6);
        bpGLG.startCorner = GridLayoutGroup.Corner.UpperLeft;
        bpGLG.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter bpCsf = bpSlotsGO.GetComponent<ContentSizeFitter>();
        bpCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bpScrollRect.viewport = bpVpRT;
        bpScrollRect.content = bpSlotsRT;

        // Vertical Scrollbar Slider for Backpack
        DungeonUIAssetHelper.CreateVerticalScrollbar(bpScrollGO.transform, bpScrollRect, new Color(0.25f, 0.55f, 0.85f, 1f));

        // 5. Footer Bar (Height 54px)
        GameObject footerGO = new("Footer", typeof(RectTransform), typeof(Image));
        footerGO.transform.SetParent(frameGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 6);
        footerRT.sizeDelta = new Vector2(-16, 50);

        Image footerBg = footerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(footerBg, new Color(0.10f, 0.10f, 0.12f, 0.95f));

        // Hint Text (Left half of Footer)
        GameObject hintGO = new("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(footerGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0, 1);
        hintRT.pivot = new Vector2(0, 0.5f);
        hintRT.anchoredPosition = new Vector2(16, 0);
        hintRT.sizeDelta = new Vector2(520, 42);
        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(hintText);
        hintText.fontSize = 11.5f;
        hintText.alignment = TextAlignmentOptions.MidlineLeft;
        hintText.color = new Color(0.85f, 0.92f, 1f);

        // Buttons Row (Right half of Footer)
        GameObject btnRowGO = new("ButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRowGO.transform.SetParent(footerGO.transform, false);
        RectTransform btnRowRT = btnRowGO.GetComponent<RectTransform>();
        btnRowRT.anchorMin = new Vector2(1, 0.5f);
        btnRowRT.anchorMax = new Vector2(1, 0.5f);
        btnRowRT.pivot = new Vector2(1, 0.5f);
        btnRowRT.anchoredPosition = new Vector2(-16, 0);
        btnRowRT.sizeDelta = new Vector2(310, 38);

        HorizontalLayoutGroup btnHLG = btnRowGO.GetComponent<HorizontalLayoutGroup>();
        btnHLG.spacing = 12;
        btnHLG.childAlignment = TextAnchor.MiddleRight;
        btnHLG.childForceExpandWidth = false;
        btnHLG.childForceExpandHeight = false;
        btnHLG.childControlWidth = false;
        btnHLG.childControlHeight = false;

        // Button 1: Loot All
        lootAllButton = CreateStyledButton(btnRowGO.transform, "BtnLootAll", "🎁 LOOT HẾT", new Color(0.85f, 0.65f, 0.15f), new Vector2(140, 36));

        // Button 2: Continue
        continueButton = CreateStyledButton(btnRowGO.transform, "BtnContinue", "TIẾP TỤC >>", new Color(0.18f, 0.55f, 0.32f), new Vector2(140, 36));

        BindButtonEvents();
    }

    private Button CreateStyledButton(Transform parent, string name, string label, Color color, Vector2 size)
    {
        GameObject btnGO = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(img, color);

        Button btn = btnGO.GetComponent<Button>();

        GameObject textGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 12f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;

        return btn;
    }
    #endregion
}
