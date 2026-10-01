using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LootUI : MonoBehaviour
{
    public static LootUI Instance { get; private set; }

    public static LootUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        LootUI found = FindFirstObjectByType<LootUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        // Tìm Canvas thích hợp trong scene (ưu tiên CombatCanvas hoặc ScreenSpaceOverlay)
        Canvas targetCanvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.name.Contains("Combat") || c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }
        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        GameObject host = targetCanvas != null ? targetCanvas.gameObject : new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (targetCanvas == null)
        {
            Canvas c = host.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = host.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
        }

        LootUI ui = host.GetComponent<LootUI>();
        if (ui == null) ui = host.AddComponent<LootUI>();
        Instance = ui;
        return Instance;
    }

    [Header("Panel Roots")]
    [SerializeField] private GameObject lootPanel;
    [SerializeField] private Transform lootSlotsParent;
    [SerializeField] private Transform backpackSlotsParent;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject slotPrefab;

    [Header("Controls")]
    [SerializeField] private Button lootAllButton;
    [SerializeField] private Button continueButton;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI hintText;

    private ItemContainer lootContainer;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> lootButtons = new List<InventoryButton>();
    private readonly List<InventoryButton> backpackButtons = new List<InventoryButton>();

    private System.Action onLootClosed;

    public bool IsOpen => lootPanel != null && lootPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AutoBindReferences();

        if (lootPanel != null)
        {
            lootPanel.SetActive(false);
        }
    }

    private void Start()
    {
        BindButtonEvents();
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

    public void AutoBindReferences()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        }

        if (lootPanel == null)
        {
            Transform t = transform.Find("LootPanel") ?? (canvas != null ? canvas.transform.Find("LootPanel") : null);
            if (t != null) lootPanel = t.gameObject;
        }

        if (lootPanel != null)
        {
            Transform root = lootPanel.transform;
            if (lootSlotsParent == null)
            {
                lootSlotsParent = FindChildRecursive(root, "LootSlots");
            }

            if (backpackSlotsParent == null)
            {
                backpackSlotsParent = FindChildRecursive(root, "BackpackSlots");
            }

            if (lootAllButton == null)
            {
                Transform t = FindChildRecursive(root, "BtnLootAll");
                if (t != null) lootAllButton = t.GetComponent<Button>();
            }

            if (continueButton == null)
            {
                Transform t = FindChildRecursive(root, "BtnContinue");
                if (t != null) continueButton = t.GetComponent<Button>();
            }

            if (titleText == null)
            {
                Transform t = FindChildRecursive(root, "Title");
                if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
            }

            if (hintText == null)
            {
                Transform t = FindChildRecursive(root, "HintText");
                if (t != null) hintText = t.GetComponent<TextMeshProUGUI>();
            }
        }

        // Tự động tìm Slot Prefab nếu chưa gán
        if (slotPrefab == null)
        {
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null)
            {
                InventoryButton btn = invPanel.GetComponentInChildren<InventoryButton>(true);
                if (btn != null) slotPrefab = btn.gameObject;
            }

#if UNITY_EDITOR
            if (slotPrefab == null)
            {
                slotPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Assets/_Prefabs/inventorySlot.prefab");
            }
#endif
        }
    }

    /// <summary>
    /// Mở bảng UI nhặt đồ với danh sách chiến lợi phẩm thu được
    /// </summary>
    public void Open(List<ItemSlot> incomingLoot, ItemContainer playerBackpack, System.Action onClose)
    {
        AutoBindReferences();

        if (lootPanel == null)
        {
            BuildUIHierarchy();
        }

        backpackContainer = playerBackpack;
        onLootClosed = onClose;

        // Tạo container tạm thời cho Loot (10 ô)
        lootContainer = ScriptableObject.CreateInstance<ItemContainer>();
        lootContainer.maxSlots = 10;
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

        // Đăng ký sự kiện cập nhật giao diện khi người chơi kéo thả
        lootContainer.OnInventoryChange += RefreshAll;
        if (backpackContainer != null)
        {
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
                hintText.text = "💡 Kéo thả vật phẩm vào Ba Lô, hoặc ấn [Loot Hết] để gom toàn bộ.";
                hintText.color = new Color(0.9f, 0.95f, 1f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = "Không có vật phẩm nào rơi ra đợt này. Bạn có thể sắp xếp lại ba lô hoặc ấn [Tiếp Tục].";
                hintText.color = new Color(0.7f, 0.75f, 0.85f);
            }
        }

        if (lootPanel != null)
        {
            lootPanel.SetActive(true);
            lootPanel.transform.SetAsLastSibling();
            Debug.Log($"[LootUI] Đã mở LootPanel thành công! activeSelf={lootPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[LootUI] Không thể mở bảng loot vì lootPanel là NULL!");
        }
    }

    /// <summary>
    /// Đóng bảng Loot UI và gọi callback tiếp tục
    /// </summary>
    public void Close()
    {
        if (lootContainer != null)
        {
            lootContainer.OnInventoryChange -= RefreshAll;
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

            // Kiểm tra xem ba lô có chỗ trống hoặc slot cùng loại có thể stack không
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
                hintText.text = "⚠️ Ba lô đã đầy! Hãy kéo bớt vật phẩm không cần thiết sang ô Chiến Lợi Phẩm để bỏ bớt.";
                hintText.color = new Color(1f, 0.6f, 0.3f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = itemsLooted > 0
                    ? $"✅ Đã gom thành công {itemsLooted} vật phẩm vào Ba Lô!"
                    : "Đã gom hết đồ. Hãy ấn [Tiếp Tục]!";
                hintText.color = new Color(0.4f, 1f, 0.5f);
            }
        }
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = canvas != null ? canvas : GetComponentInParent<Canvas>();

        // 1. Tái sử dụng các slot sẵn có nếu đúng số lượng
        if (slotList.Count == container.maxSlots && slotList.TrueForAll(s => s != null))
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

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            slotList.Add(btn);
        }
    }

    private GameObject CreateFallbackSlot(Transform parent, int index)
    {
        int uiLayer = parent != null ? parent.gameObject.layer : 5;
        if (uiLayer < 0) uiLayer = 5;

        GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(InventoryButton));
        slotObj.layer = uiLayer;
        slotObj.transform.SetParent(parent, false);

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);

        Image bg = slotObj.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.3f, 0.95f);

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.layer = uiLayer;
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
        GameObject textGO = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.layer = uiLayer;
        textGO.transform.SetParent(slotObj.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 0.4f);
        textRT.offsetMin = new Vector2(4, 2);
        textRT.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI txt = textGO.GetComponent<TextMeshProUGUI>();

        TMP_FontAsset font = null;
        TextMeshProUGUI sample = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (sample != null) font = sample.font;
        if (font != null) txt.font = font;

        txt.fontSize = 14;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.BottomRight;
        txt.color = Color.white;
        txt.raycastTarget = false;
        txt.gameObject.SetActive(false);

        InventoryButton btn = slotObj.GetComponent<InventoryButton>();
        if (btn == null) btn = slotObj.AddComponent<InventoryButton>();
        btn.BindComponents(iconImg, txt);

        return slotObj;
    }

    public void RefreshAll()
    {
        RefreshPanel(lootButtons, lootContainer);
        RefreshPanel(backpackButtons, backpackContainer);
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

    /// <summary>
    /// Tự động sinh hệ thống phân cấp UI hoàn chỉnh nếu chưa có sẵn trong Scene
    /// </summary>
    private void BuildUIHierarchy()
    {
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        TMP_FontAsset font = null;
        TextMeshProUGUI sample = FindFirstObjectByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        if (sample != null) font = sample.font;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        GameObject modal = new GameObject("LootPanel", typeof(RectTransform), typeof(Image));
        modal.layer = uiLayer;
        modal.transform.SetParent(canvas.transform, false);
        modal.transform.SetAsLastSibling();

        RectTransform modalRT = modal.GetComponent<RectTransform>();
        modalRT.anchorMin = Vector2.zero;
        modalRT.anchorMax = Vector2.one;
        modalRT.offsetMin = Vector2.zero;
        modalRT.offsetMax = Vector2.zero;

        Image modalBg = modal.GetComponent<Image>();
        modalBg.color = new Color(0.03f, 0.04f, 0.07f, 0.88f);

        // Center Box
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.layer = uiLayer;
        box.transform.SetParent(modal.transform, false);
        RectTransform boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(1160, 680);
        boxRT.anchoredPosition = Vector2.zero;

        Image boxBg = box.GetComponent<Image>();
        boxBg.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);

        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.layer = uiLayer;
        titleGO.transform.SetParent(box.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -32);
        titleRT.sizeDelta = new Vector2(0, 40);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        if (font != null) titleText.font = font;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "⚔️ CHIẾN LỢI PHẨM CHIẾN THẮNG";
        titleText.color = new Color(1f, 0.85f, 0.3f);

        // Subtitle / Hint
        GameObject hintGO = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.layer = uiLayer;
        hintGO.transform.SetParent(box.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 1);
        hintRT.anchorMax = new Vector2(1, 1);
        hintRT.pivot = new Vector2(0.5f, 1);
        hintRT.anchoredPosition = new Vector2(0, -70);
        hintRT.sizeDelta = new Vector2(0, 30);
        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        if (font != null) hintText.font = font;
        hintText.fontSize = 15;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.text = "💡 Kéo thả vật phẩm giữa 2 bên để sắp xếp, hoặc dùng thanh trượt để xem toàn bộ túi đồ";
        hintText.color = new Color(0.85f, 0.92f, 1f);

        // Left Frame: Loot (Scrollable with Slider)
        GameObject lootFrame = CreateFrame(box.transform, "Loot_Frame", "CHIẾN LỢI PHẨM (RƠI TỪ QUÁI)", new Vector2(-275, -15), new Vector2(520, 420), new Color(0.6f, 0.45f, 0.1f), font, uiLayer);
        lootSlotsParent = CreateScrollableSlotGrid(lootFrame.transform, "LootSlots", 5, new Color(0.85f, 0.65f, 0.2f), uiLayer);

        // Right Frame: Backpack (Scrollable with Slider)
        GameObject backpackFrame = CreateFrame(box.transform, "Backpack_Frame", "BA LÔ NGƯỜI CHƠI (TÚI ĐỒ)", new Vector2(275, -15), new Vector2(520, 420), new Color(0.15f, 0.5f, 0.7f), font, uiLayer);
        backpackSlotsParent = CreateScrollableSlotGrid(backpackFrame.transform, "BackpackSlots", 5, new Color(0.25f, 0.75f, 0.95f), uiLayer);

        // Action Button: Loot Hết
        lootAllButton = CreateStyledButton(box.transform, "BtnLootAll", "✨ LOOT HẾT", new Vector2(-160, -285), new Vector2(230, 52), new Color(0.15f, 0.65f, 0.35f), font, uiLayer);

        // Action Button: Tiếp Tục
        continueButton = CreateStyledButton(box.transform, "BtnContinue", "➡️ TIẾP TỤC", new Vector2(160, -285), new Vector2(230, 52), new Color(0.2f, 0.45f, 0.85f), font, uiLayer);

        lootPanel = modal;
    }

    private GameObject CreateFrame(Transform parent, string name, string header, Vector2 pos, Vector2 size, Color borderColor, TMP_FontAsset font, int layer)
    {
        GameObject frame = new GameObject(name, typeof(RectTransform), typeof(Image));
        frame.layer = layer;
        frame.transform.SetParent(parent, false);
        RectTransform rt = frame.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = frame.GetComponent<Image>();
        img.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        // Header Label (nằm cố định ở đầu frame, không bao giờ bị ô đồ đè lên)
        GameObject labelGO = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.layer = layer;
        labelGO.transform.SetParent(frame.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 1);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.pivot = new Vector2(0.5f, 1);
        labelRT.anchoredPosition = new Vector2(0, -16);
        labelRT.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = 16;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.text = header;
        label.color = borderColor * 1.5f;

        return frame;
    }

    private Transform CreateScrollableSlotGrid(Transform frameParent, string name, int cols, Color handleColor, int layer)
    {
        // 1. ScrollView root (bắt đầu bên dưới header label 48px)
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.layer = layer;
        scrollGO.transform.SetParent(frameParent, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(12, 12);
        scrollRT.offsetMax = new Vector2(-12, -48);

        ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // 2. Viewport có RectMask2D để cắt gọt slot, ngăn tràn ra ngoài
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.layer = layer;
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = new Vector2(-22, 0); // Chừa 22px cho thanh slider

        // 3. Content chứa Grid các slot
        GameObject contentGO = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGO.layer = layer;
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup grid = contentGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(66, 66);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(6, 6, 8, 8);

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4. Thanh trượt Slider (Scrollbar)
        GameObject scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGO.layer = layer;
        scrollbarGO.transform.SetParent(scrollGO.transform, false);
        RectTransform scrollbarRT = scrollbarGO.GetComponent<RectTransform>();
        scrollbarRT.anchorMin = new Vector2(1, 0);
        scrollbarRT.anchorMax = new Vector2(1, 1);
        scrollbarRT.pivot = new Vector2(1, 0.5f);
        scrollbarRT.anchoredPosition = Vector2.zero;
        scrollbarRT.sizeDelta = new Vector2(16, 0);

        Image scrollbarBg = scrollbarGO.GetComponent<Image>();
        scrollbarBg.color = new Color(0.08f, 0.10f, 0.15f, 0.9f);

        Scrollbar scrollbar = scrollbarGO.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Sliding Area
        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.layer = layer;
        slidingArea.transform.SetParent(scrollbarGO.transform, false);
        RectTransform slidingRT = slidingArea.GetComponent<RectTransform>();
        slidingRT.anchorMin = Vector2.zero;
        slidingRT.anchorMax = Vector2.one;
        slidingRT.offsetMin = new Vector2(2, 4);
        slidingRT.offsetMax = new Vector2(-2, -4);

        // Handle (nút kéo thanh trượt)
        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.layer = layer;
        handleGO.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        handleImg.color = handleColor;

        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        return contentGO.transform;
    }

    private Button CreateStyledButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color, TMP_FontAsset font, int layer)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.layer = layer;
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnGO.GetComponent<Image>();
        img.color = color;

        Button btn = btnGO.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.25f;
        cb.pressedColor = color * 0.8f;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = cb;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.layer = layer;
        textGO.transform.SetParent(btnGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGO.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = 17;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        label.color = Color.white;

        return btn;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
