using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Redesigned Home Chest UI (Rương Chứa Đồ Tại Nhà).
/// Displays two structured, beautifully aligned frames:
/// - Upper Panel: Rương Nhà (Storage Chest) [33 slots, 11x3 grid, Sort & Take All buttons]
/// - Middle Bar: Quick Action buttons (Quick Stack, Store All, Take All, Sort All)
/// - Lower Panel: Ba Lô Cá Nhân (Backpack) [33 slots, 11x3 grid matching Hotbar, Sort & Store All buttons]
/// - Top & Bottom Bars: Authentic pixel-art headers, capacity indicators, and item info hints.
/// Supports drag-and-drop, right-click quick transfer, and complete persistence with HomeChest.
/// If not authored in the scene, automatically constructs the complete pixel-art UI hierarchy at runtime.
/// </summary>
public class ChestUI : MonoBehaviour
{
    #region Singleton & Statics
    private static ChestUI _instance;
    public static ChestUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = EnsureInstance();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    public static ChestUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        _instance = FindFirstObjectByType<ChestUI>(FindObjectsInactive.Include);
        if (_instance != null) return _instance;

        // Try load ChestCanvas prefab from Resources
        GameObject prefab = Resources.Load<GameObject>("ChestCanvas");

#if UNITY_EDITOR
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ChestCanvas.prefab")
                  ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Assets/_Prefabs/ChestCanvas.prefab");
        }
#endif

        if (prefab != null)
        {
            GameObject go = Instantiate(prefab);
            go.name = "ChestCanvas";
            _instance = go.GetComponentInChildren<ChestUI>(true);
            if (_instance != null)
            {
                _instance.AutoBindReferences();
                return _instance;
            }
        }

        return _instance;
    }
    #endregion

    #region Serialized UI References
    [Header("Root Canvas & Panels")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject chestPanel; // Fullscreen overlay or root panel
    [SerializeField] private RectTransform windowRect;

    [Header("Chest Panel (Top)")]
    [SerializeField] private Transform chestSlotsParent;
    [SerializeField] private TextMeshProUGUI chestCapacityText;
    [SerializeField] private Button sortChestButton;
    [SerializeField] private Button takeAllChestButton;

    [Header("Backpack Panel (Bottom)")]
    [SerializeField] private Transform backpackSlotsParent;
    [SerializeField] private TextMeshProUGUI backpackCapacityText;
    [SerializeField] private Button sortBackpackButton;
    [SerializeField] private Button storeAllBackpackButton;

    [Header("Central Action Bar")]
    [SerializeField] private Button quickStackButton;
    [SerializeField] private Button storeAllButton;
    [SerializeField] private Button takeAllButton;
    [SerializeField] private Button sortAllButton;

    [Header("Header & Footer")]
    [SerializeField] private TextMeshProUGUI headerTitleText;
    [SerializeField] private TextMeshProUGUI headerSubtitleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI hintFooterText;
    [SerializeField] private TextMeshProUGUI itemHoverInfoText;

    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;
    #endregion

    #region Runtime State
    private ItemContainer chestContainer;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> chestSlots = new List<InventoryButton>();
    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();

    private bool isExplicitlyOpened = false;
    public bool IsOpen => isExplicitlyOpened && chestPanel != null && chestPanel.activeSelf;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        AutoBindReferences();
        RegisterButtonEvents();

        if (!isExplicitlyOpened && chestPanel != null)
        {
            chestPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (!isExplicitlyOpened && chestPanel != null)
        {
            chestPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            Close();
            if (HomeChest.Instance != null && HomeChest.Instance.isOpen)
            {
                HomeChest.Instance.CloseChest();
            }
        }
    }

    private void OnEnable()
    {
        InventoryButton.OnItemSelected += HandleItemSelected;
        if (IsOpen && chestContainer != null)
        {
            SubscribeEvents();
        }
    }

    private void OnDisable()
    {
        InventoryButton.OnItemSelected -= HandleItemSelected;
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        InventoryButton.OnItemSelected -= HandleItemSelected;
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region AutoBind References
    public void AutoBindReferences()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (chestPanel == null)
        {
            Transform overlayT = transform.Find("ChestOverlay");
            if (overlayT != null) chestPanel = overlayT.gameObject;
            else if (gameObject.name.Contains("Panel") || gameObject.name.Contains("Overlay")) chestPanel = gameObject;
            else if (canvas != null)
            {
                Transform t = canvas.transform.Find("ChestOverlay") ?? canvas.transform.Find("ChestPanel");
                if (t != null) chestPanel = t.gameObject;
            }
        }

        if (chestPanel == null) chestPanel = gameObject;

        Transform root = chestPanel != null ? chestPanel.transform : transform;

        // Window Rect
        if (windowRect == null && root != null)
        {
            Transform w = root.Find("ChestWindow") ?? root.Find("Window");
            if (w != null) windowRect = w.GetComponent<RectTransform>();
        }

        Transform win = windowRect != null ? windowRect.transform : root;

        // Header
        if (headerTitleText == null && win != null)
            headerTitleText = win.Find("TopHeaderBar/HeaderTitle")?.GetComponent<TextMeshProUGUI>();
        if (headerSubtitleText == null && win != null)
            headerSubtitleText = win.Find("TopHeaderBar/HeaderSubtitle")?.GetComponent<TextMeshProUGUI>();
        if (closeButton == null && win != null)
            closeButton = win.Find("TopHeaderBar/CloseButton")?.GetComponent<Button>() ?? win.Find("CloseButton")?.GetComponent<Button>();

        // Chest section
        if (chestSlotsParent == null && win != null)
        {
            Transform t = win.Find("ChestSection/ChestFrame/ChestSlots") 
                       ?? win.Find("Chest_Frame/ChestSlots") 
                       ?? win.Find("Chest_Frame") 
                       ?? win.Find("ChestSlots");
            if (t != null) chestSlotsParent = t;
        }
        if (chestCapacityText == null && win != null)
            chestCapacityText = win.Find("ChestSection/SectionHeader/CapacityText")?.GetComponent<TextMeshProUGUI>();
        if (sortChestButton == null && win != null)
            sortChestButton = win.Find("ChestSection/SectionHeader/Btn_SortChest")?.GetComponent<Button>();
        if (takeAllChestButton == null && win != null)
            takeAllChestButton = win.Find("ChestSection/SectionHeader/Btn_TakeAll")?.GetComponent<Button>();

        // Middle action bar
        if (quickStackButton == null && win != null)
            quickStackButton = win.Find("MiddleDividerBar/Btn_QuickStack")?.GetComponent<Button>();
        if (storeAllButton == null && win != null)
            storeAllButton = win.Find("MiddleDividerBar/Btn_StoreAll")?.GetComponent<Button>();
        if (takeAllButton == null && win != null)
            takeAllButton = win.Find("MiddleDividerBar/Btn_TakeAll")?.GetComponent<Button>();
        if (sortAllButton == null && win != null)
            sortAllButton = win.Find("MiddleDividerBar/Btn_SortAll")?.GetComponent<Button>();

        // Backpack section
        if (backpackSlotsParent == null && win != null)
        {
            Transform t = win.Find("BackpackSection/BackpackFrame/BackpackSlots") 
                       ?? win.Find("Backpack_Frame/BackpackSlots") 
                       ?? win.Find("Backpack_Frame") 
                       ?? win.Find("BackpackSlots");
            if (t != null) backpackSlotsParent = t;
        }
        if (backpackCapacityText == null && win != null)
            backpackCapacityText = win.Find("BackpackSection/SectionHeader/CapacityText")?.GetComponent<TextMeshProUGUI>();
        if (sortBackpackButton == null && win != null)
            sortBackpackButton = win.Find("BackpackSection/SectionHeader/Btn_SortBackpack")?.GetComponent<Button>();
        if (storeAllBackpackButton == null && win != null)
            storeAllBackpackButton = win.Find("BackpackSection/SectionHeader/Btn_StoreAll")?.GetComponent<Button>();

        // Footer
        if (hintFooterText == null && win != null)
            hintFooterText = win.Find("FooterHintBar/HintText")?.GetComponent<TextMeshProUGUI>();
        if (itemHoverInfoText == null && win != null)
            itemHoverInfoText = win.Find("FooterHintBar/ItemInfoText")?.GetComponent<TextMeshProUGUI>();

        // Prefab
        if (slotPrefab == null)
            slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
    }

    private void RegisterButtonEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (quickStackButton != null)
        {
            quickStackButton.onClick.RemoveListener(OnQuickStackClicked);
            quickStackButton.onClick.AddListener(OnQuickStackClicked);
        }

        if (storeAllButton != null)
        {
            storeAllButton.onClick.RemoveListener(OnStoreAllClicked);
            storeAllButton.onClick.AddListener(OnStoreAllClicked);
        }

        if (takeAllButton != null)
        {
            takeAllButton.onClick.RemoveListener(OnTakeAllClicked);
            takeAllButton.onClick.AddListener(OnTakeAllClicked);
        }

        if (sortAllButton != null)
        {
            sortAllButton.onClick.RemoveListener(OnSortAllClicked);
            sortAllButton.onClick.AddListener(OnSortAllClicked);
        }

        if (sortChestButton != null)
        {
            sortChestButton.onClick.RemoveListener(OnSortChestClicked);
            sortChestButton.onClick.AddListener(OnSortChestClicked);
        }

        if (takeAllChestButton != null)
        {
            takeAllChestButton.onClick.RemoveListener(OnTakeAllClicked);
            takeAllChestButton.onClick.AddListener(OnTakeAllClicked);
        }

        if (sortBackpackButton != null)
        {
            sortBackpackButton.onClick.RemoveListener(OnSortBackpackClicked);
            sortBackpackButton.onClick.AddListener(OnSortBackpackClicked);
        }

        if (storeAllBackpackButton != null)
        {
            storeAllBackpackButton.onClick.RemoveListener(OnStoreAllClicked);
            storeAllBackpackButton.onClick.AddListener(OnStoreAllClicked);
        }
    }
    #endregion

    #region Container Sync
    private void SubscribeEvents()
    {
        UnsubscribeEvents();

        if (chestContainer != null)
            chestContainer.OnInventoryChange += RefreshAll;
        if (backpackContainer != null)
            backpackContainer.OnInventoryChange += RefreshAll;
    }

    private void UnsubscribeEvents()
    {
        if (chestContainer != null)
            chestContainer.OnInventoryChange -= RefreshAll;
        if (backpackContainer != null)
            backpackContainer.OnInventoryChange -= RefreshAll;
    }
    #endregion

    #region Public Interface
    public void Open(ItemContainer chest, ItemContainer backpack)
    {
        isExplicitlyOpened = true;
        chestContainer = chest;
        backpackContainer = backpack;

        InventoryController.Instance?.CloseInventoryPanel();
        AutoBindReferences();
        RegisterButtonEvents();

        if (canvas != null && !canvas.gameObject.activeSelf)
        {
            canvas.gameObject.SetActive(true);
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (chestPanel != null)
        {
            chestPanel.SetActive(true);
            chestPanel.transform.SetAsLastSibling();
        }

        SubscribeEvents();

        BuildSlots(chestSlotsParent, chestContainer, chestSlots);
        BuildSlots(backpackSlotsParent, backpackContainer, backpackSlots);

        RefreshAll();

        if (itemHoverInfoText != null) itemHoverInfoText.text = "";

        Debug.Log($"[ChestUI] Opened Chest UI successfully. Showing {chestSlots.Count} chest slots and {backpackSlots.Count} backpack slots.");
    }

    public void Close()
    {
        isExplicitlyOpened = false;
        UnsubscribeEvents();

        if (chestPanel != null)
            chestPanel.SetActive(false);

        if (HomeChest.Instance != null && HomeChest.Instance.isOpen)
        {
            HomeChest.Instance.CloseChest();
        }

        Debug.Log("[ChestUI] Closed Chest UI.");
    }

    private Canvas GetCanvas()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null)
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        return canvas;
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = GetCanvas();

        InventoryButton[] existingSlots = parent.GetComponentsInChildren<InventoryButton>(true);
        slotList.Clear();

        int targetCount = container.maxSlots > 0 ? container.maxSlots : 33;
        int countToUse = Mathf.Min(existingSlots.Length, targetCount);

        for (int i = 0; i < countToUse; i++)
        {
            existingSlots[i].gameObject.SetActive(true);
            existingSlots[i].SetSlotData(container, i);
            existingSlots[i].SetCanvas(currentCanvas);
            slotList.Add(existingSlots[i]);
        }

        // If existing slots are fewer than needed, instantiate new ones from prefab or clone existing slot
        if (countToUse < targetCount)
        {
            GameObject template = slotPrefab;
            if (template == null && existingSlots.Length > 0) template = existingSlots[0].gameObject;
            if (template == null) template = DungeonUIAssetHelper.GetSlotPrefab();

            if (template != null)
            {
                for (int i = countToUse; i < targetCount; i++)
                {
                    GameObject newSlotGO = Instantiate(template, parent);
                    newSlotGO.name = $"inventorySlot ({i})";
                    newSlotGO.SetActive(true);
                    InventoryButton btn = newSlotGO.GetComponent<InventoryButton>();
                    if (btn != null)
                    {
                        btn.SetSlotData(container, i);
                        btn.SetCanvas(currentCanvas);
                        slotList.Add(btn);
                    }
                }
            }
        }

        // Hide any excess slots beyond targetCount
        for (int i = targetCount; i < existingSlots.Length; i++)
        {
            existingSlots[i].gameObject.SetActive(false);
        }
    }

    public void RefreshAll()
    {
        RefreshPanel(chestSlots, chestContainer);
        RefreshPanel(backpackSlots, backpackContainer);
        UpdateCapacityLabels();
    }

    private void RefreshPanel(List<InventoryButton> slotList, ItemContainer container)
    {
        if (container == null)
            return;

        for (int i = 0; i < slotList.Count; i++)
        {
            if (i < container.itemSlots.Length && container.itemSlots[i] != null && container.itemSlots[i].itemData != null)
            {
                slotList[i].SetItem(
                    container.itemSlots[i].itemData,
                    container.itemSlots[i].amount
                );
            }
            else
            {
                slotList[i].ClearItem();
            }
        }
    }

    private void UpdateCapacityLabels()
    {
        if (chestCapacityText != null && chestContainer != null)
        {
            int chestOccupied = 0;
            int total = chestContainer.itemSlots != null ? chestContainer.itemSlots.Length : chestContainer.maxSlots;
            for (int i = 0; i < total; i++)
            {
                if (i < chestContainer.itemSlots.Length && chestContainer.itemSlots[i] != null && chestContainer.itemSlots[i].itemData != null && chestContainer.itemSlots[i].amount > 0)
                    chestOccupied++;
            }
            chestCapacityText.text = $"Sức chứa: <color=#FFE082><b>{chestOccupied}</b></color> / {total} ô";
        }

        if (backpackCapacityText != null && backpackContainer != null)
        {
            int bpOccupied = 0;
            int total = backpackContainer.itemSlots != null ? backpackContainer.itemSlots.Length : backpackContainer.maxSlots;
            for (int i = 0; i < total; i++)
            {
                if (i < backpackContainer.itemSlots.Length && backpackContainer.itemSlots[i] != null && backpackContainer.itemSlots[i].itemData != null && backpackContainer.itemSlots[i].amount > 0)
                    bpOccupied++;
            }
            backpackCapacityText.text = $"Đang chứa: <color=#A5D6A7><b>{bpOccupied}</b></color> / {total} ô";
        }
    }

    private void HandleItemSelected(ItemData item)
    {
        if (itemHoverInfoText == null) return;
        if (item != null)
        {
            string name = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
            itemHoverInfoText.text = $"<color=#FFE082>Đang chọn:</color> <b>{name}</b> ({GetItemTypeLabel(item.itemType)})";
        }
        else
        {
            itemHoverInfoText.text = "";
        }
    }

    private static string GetItemTypeLabel(ItemData.ItemType type)
    {
        return type switch
        {
            ItemData.ItemType.Seed => "Hạt giống",
            ItemData.ItemType.Produce => "Nông sản",
            ItemData.ItemType.Food => "Thức ăn",
            _ => "Vật phẩm"
        };
    }
    #endregion

    #region Action Button Handlers
    public void OnQuickStackClicked()
    {
        if (chestContainer == null || backpackContainer == null) return;
        QuickStackContainers(backpackContainer, chestContainer);
    }

    public void OnStoreAllClicked()
    {
        if (chestContainer == null || backpackContainer == null) return;
        TransferAllContainers(backpackContainer, chestContainer);
    }

    public void OnTakeAllClicked()
    {
        if (chestContainer == null || backpackContainer == null) return;
        TransferAllContainers(chestContainer, backpackContainer);
    }

    public void OnSortChestClicked()
    {
        if (chestContainer != null) SortContainer(chestContainer);
    }

    public void OnSortBackpackClicked()
    {
        if (backpackContainer != null) SortContainer(backpackContainer);
    }

    public void OnSortAllClicked()
    {
        if (chestContainer != null) SortContainer(chestContainer);
        if (backpackContainer != null) SortContainer(backpackContainer);
    }

    public static void QuickStackContainers(ItemContainer source, ItemContainer target)
    {
        if (source == null || target == null || source.itemSlots == null || target.itemSlots == null) return;

        bool anyMoved = false;
        for (int s = 0; s < source.itemSlots.Length; s++)
        {
            ItemSlot sSlot = source.itemSlots[s];
            if (sSlot == null || sSlot.itemData == null || sSlot.amount <= 0) continue;
            if (!sSlot.itemData.isStackable) continue;

            for (int t = 0; t < target.itemSlots.Length; t++)
            {
                ItemSlot tSlot = target.itemSlots[t];
                if (tSlot != null && ItemContainer.IsItemMatch(tSlot.itemData, sSlot.itemData))
                {
                    tSlot.amount += sSlot.amount;
                    sSlot.itemData = null;
                    sSlot.amount = 0;
                    anyMoved = true;
                    break;
                }
            }
        }

        if (anyMoved)
        {
            source.NotifyChange();
            target.NotifyChange();
            HomeChest.Instance?.SaveChestData();
        }
    }

    public static void TransferAllContainers(ItemContainer source, ItemContainer target)
    {
        if (source == null || target == null || source.itemSlots == null || target.itemSlots == null) return;

        bool anyMoved = false;

        // 1st pass: merge stackable items into existing stacks
        for (int s = 0; s < source.itemSlots.Length; s++)
        {
            ItemSlot sSlot = source.itemSlots[s];
            if (sSlot == null || sSlot.itemData == null || sSlot.amount <= 0) continue;
            if (!sSlot.itemData.isStackable) continue;

            for (int t = 0; t < target.itemSlots.Length; t++)
            {
                ItemSlot tSlot = target.itemSlots[t];
                if (tSlot != null && ItemContainer.IsItemMatch(tSlot.itemData, sSlot.itemData))
                {
                    tSlot.amount += sSlot.amount;
                    sSlot.itemData = null;
                    sSlot.amount = 0;
                    anyMoved = true;
                    break;
                }
            }
        }

        // 2nd pass: transfer remaining items into first empty slot in target
        for (int s = 0; s < source.itemSlots.Length; s++)
        {
            ItemSlot sSlot = source.itemSlots[s];
            if (sSlot == null || sSlot.itemData == null || sSlot.amount <= 0) continue;

            int emptySlot = target.GetEmptySlot();
            if (emptySlot == -1) break; // target is full

            if (target.itemSlots[emptySlot] == null) target.itemSlots[emptySlot] = new ItemSlot();
            target.itemSlots[emptySlot].itemData = sSlot.itemData;
            target.itemSlots[emptySlot].amount = sSlot.amount;

            sSlot.itemData = null;
            sSlot.amount = 0;
            anyMoved = true;
        }

        if (anyMoved)
        {
            source.NotifyChange();
            target.NotifyChange();
            HomeChest.Instance?.SaveChestData();
        }
    }

    public static void SortContainer(ItemContainer container)
    {
        if (container == null || container.itemSlots == null) return;

        // 1. Merge duplicate stackable items
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            ItemSlot slotA = container.itemSlots[i];
            if (slotA == null || slotA.itemData == null || slotA.amount <= 0) continue;
            if (!slotA.itemData.isStackable) continue;

            for (int j = i + 1; j < container.itemSlots.Length; j++)
            {
                ItemSlot slotB = container.itemSlots[j];
                if (slotB != null && ItemContainer.IsItemMatch(slotB.itemData, slotA.itemData))
                {
                    slotA.amount += slotB.amount;
                    slotB.itemData = null;
                    slotB.amount = 0;
                }
            }
        }

        // 2. Extract valid items
        var items = new List<ItemSlot>();
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            ItemSlot slot = container.itemSlots[i];
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                items.Add(new ItemSlot { itemData = slot.itemData, amount = slot.amount });
            }
        }

        // 3. Sort by ItemType order, then by Name, then by amount descending
        items.Sort((a, b) =>
        {
            int typeCompare = a.itemData.itemType.CompareTo(b.itemData.itemType);
            if (typeCompare != 0) return typeCompare;

            string nameA = !string.IsNullOrEmpty(a.itemData.itemName) ? a.itemData.itemName : a.itemData.name;
            string nameB = !string.IsNullOrEmpty(b.itemData.itemName) ? b.itemData.itemName : b.itemData.name;
            int nameCompare = string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0) return nameCompare;

            return b.amount.CompareTo(a.amount);
        });

        // 4. Fill container back
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i] == null) container.itemSlots[i] = new ItemSlot();

            if (i < items.Count)
            {
                container.itemSlots[i].itemData = items[i].itemData;
                container.itemSlots[i].amount = items[i].amount;
            }
            else
            {
                container.itemSlots[i].itemData = null;
                container.itemSlots[i].amount = 0;
            }
        }

        container.NotifyChange();
        HomeChest.Instance?.SaveChestData();
    }
    #endregion

    #region Drag & Drop and Quick Transfer
    public static void TransferItem(ItemContainer source, int sourceIndex,
                                     ItemContainer target, int targetIndex)
    {
        if (source == null || target == null)
            return;

        if (sourceIndex < 0 || sourceIndex >= source.itemSlots.Length)
            return;

        if (targetIndex < 0 || targetIndex >= target.itemSlots.Length)
            return;

        ItemSlot sourceSlot = source.itemSlots[sourceIndex];
        ItemSlot targetSlot = target.itemSlots[targetIndex];

        if (sourceSlot == null || sourceSlot.itemData == null)
            return;

        if (targetSlot == null)
        {
            targetSlot = new ItemSlot();
            target.itemSlots[targetIndex] = targetSlot;
        }

        if (source == target)
        {
            source.SwapSlots(sourceIndex, targetIndex);
            HomeChest.Instance?.SaveChestData();
            return;
        }

        if (targetSlot.itemData != null &&
            ItemContainer.IsItemMatch(targetSlot.itemData, sourceSlot.itemData) &&
            sourceSlot.itemData.isStackable)
        {
            targetSlot.amount += sourceSlot.amount;
            sourceSlot.itemData = null;
            sourceSlot.amount = 0;

            source.NotifyChange();
            target.NotifyChange();
            HomeChest.Instance?.SaveChestData();
            return;
        }

        ItemData tempItem = targetSlot.itemData;
        int tempAmount = targetSlot.amount;

        targetSlot.itemData = sourceSlot.itemData;
        targetSlot.amount = sourceSlot.amount;

        sourceSlot.itemData = tempItem;
        sourceSlot.amount = tempAmount;

        source.NotifyChange();
        target.NotifyChange();
        HomeChest.Instance?.SaveChestData();
    }

    /// <summary>
    /// Quick transfer items between Chest (HomeChest) and Backpack when right-clicked.
    /// </summary>
    public static void QuickTransfer(ItemContainer source, int sourceIndex)
    {
        if (Instance == null || !Instance.IsOpen) return;
        if (source == null || source.itemSlots == null) return;
        if (sourceIndex < 0 || sourceIndex >= source.itemSlots.Length) return;

        ItemSlot sourceSlot = source.itemSlots[sourceIndex];
        if (sourceSlot == null || sourceSlot.itemData == null || sourceSlot.amount <= 0) return;

        ItemContainer target = (source == Instance.chestContainer) ? Instance.backpackContainer : Instance.chestContainer;
        if (target == null || target.itemSlots == null) return;

        // 1. If stackable, stack into matching slot in target container
        if (sourceSlot.itemData.isStackable)
        {
            for (int i = 0; i < target.itemSlots.Length; i++)
            {
                if (target.itemSlots[i] != null && ItemContainer.IsItemMatch(target.itemSlots[i].itemData, sourceSlot.itemData))
                {
                    target.itemSlots[i].amount += sourceSlot.amount;
                    sourceSlot.itemData = null;
                    sourceSlot.amount = 0;
                    source.NotifyChange();
                    target.NotifyChange();
                    HomeChest.Instance?.SaveChestData();
                    return;
                }
            }
        }

        // 2. Transfer into first empty slot in target container
        int emptySlot = target.GetEmptySlot();
        if (emptySlot != -1)
        {
            if (target.itemSlots[emptySlot] == null) target.itemSlots[emptySlot] = new ItemSlot();
            target.itemSlots[emptySlot].itemData = sourceSlot.itemData;
            target.itemSlots[emptySlot].amount = sourceSlot.amount;

            sourceSlot.itemData = null;
            sourceSlot.amount = 0;

            source.NotifyChange();
            target.NotifyChange();
            HomeChest.Instance?.SaveChestData();
            return;
        }

        // Notify if target container is full
        if (source == Instance.backpackContainer)
        {
            Debug.LogWarning("[ChestUI] Chest is full, cannot transfer more items!");
        }
        else
        {
            Debug.LogWarning("[ChestUI] Backpack is full, cannot transfer more items!");
        }
    }
    #endregion
}
