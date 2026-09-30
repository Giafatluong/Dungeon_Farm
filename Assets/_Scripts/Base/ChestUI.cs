using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI Rương tại Base: hiển thị 2 panel song song (Chest | Backpack)
/// Hỗ trợ kéo thả vật phẩm giữa 2 container (hoán đổi hoặc gộp stack).
/// </summary>
public class ChestUI : MonoBehaviour
{
    private static ChestUI _instance;
    public static ChestUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ChestUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Chest Panel (bên trái)")]
    [SerializeField] private Transform chestSlotsParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Backpack Panel (bên phải)")]
    [SerializeField] private Transform backpackSlotsParent;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject chestPanel; // Root panel chứa toàn bộ UI Rương

    private ItemContainer chestContainer;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> chestSlots = new List<InventoryButton>();
    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();

    public bool IsOpen => chestPanel != null && chestPanel.activeSelf;

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
        if (canvas == null)
            canvas = GetCanvas();

        if (chestPanel == null)
        {
            if (gameObject.name == "ChestPanel")
            {
                chestPanel = gameObject;
            }
            else if (canvas != null)
            {
                Transform t = canvas.transform.Find("ChestPanel");
                if (t != null) chestPanel = t.gameObject;
            }
        }

        Transform root = chestPanel != null ? chestPanel.transform : transform;

        if (chestSlotsParent == null && root != null)
        {
            Transform t = root.Find("Chest_Frame") ?? root.Find("Chest_Frame/ChestSlots") ?? root.Find("ChestSlots");
            if (t != null) chestSlotsParent = t;
        }

        if (backpackSlotsParent == null && root != null)
        {
            Transform t = root.Find("Backpack_Frame") ?? root.Find("Backpack_Frame/BackpackSlots") ?? root.Find("BackpackSlots");
            if (t != null) backpackSlotsParent = t;
        }

        if (slotPrefab == null)
        {
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
    }

    private void Start()
    {
        // Đảm bảo panel ẩn khi mới vào game
        if (chestPanel != null)
        {
            chestPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Nhấn Escape để đóng Rương nhanh
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            if (HomeChest.Instance != null && HomeChest.Instance.isOpen)
            {
                HomeChest.Instance.CloseChest();
            }
        }
    }

    private void OnEnable()
    {
        if (IsOpen && chestContainer != null)
        {
            SubscribeEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents(); // Đảm bảo không bị lặp delegate

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

    /// <summary>
    /// Được gọi bởi HomeChest khi mở rương. Truyền 2 container vào.
    /// </summary>
    public void Open(ItemContainer chest, ItemContainer backpack)
    {
        chestContainer = chest;
        backpackContainer = backpack;

        AutoBindReferences();

        if (chestPanel != null)
            chestPanel.SetActive(true);

        SubscribeEvents();

        BuildSlots(chestSlotsParent, chestContainer, chestSlots);
        BuildSlots(backpackSlotsParent, backpackContainer, backpackSlots);

        RefreshAll();
    }

    public void Close()
    {
        UnsubscribeEvents();

        if (chestPanel != null)
            chestPanel.SetActive(false);
    }

    private Canvas GetCanvas()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        return canvas;
    }

    /// <summary>
    /// Tạo hoặc tái sử dụng các slot button cho một container (tránh Destroy/Instantiate liên tục)
    /// </summary>
    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = GetCanvas();

        // 1. Nếu danh sách đã có đủ slot và các đối tượng còn tồn tại -> tái sử dụng
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

        // 2. Nếu trong parent đã có sẵn các slot (được tạo trước trong Scene) -> bind trực tiếp
        InventoryButton[] existingSlots = parent.GetComponentsInChildren<InventoryButton>(true);
        if (existingSlots != null && existingSlots.Length >= container.maxSlots)
        {
            slotList.Clear();
            for (int i = 0; i < container.maxSlots; i++)
            {
                existingSlots[i].gameObject.SetActive(true);
                existingSlots[i].SetSlotData(container, i);
                existingSlots[i].SetCanvas(currentCanvas);
                slotList.Add(existingSlots[i]);
            }
            // Ẩn slot thừa nếu có
            for (int i = container.maxSlots; i < existingSlots.Length; i++)
            {
                existingSlots[i].gameObject.SetActive(false);
            }
            return;
        }

        // 3. Nếu chưa đủ thì tạo từ prefab
        // Dọn dẹp slot cũ nếu có
        foreach (InventoryButton slot in slotList)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slotList.Clear();

        if (slotPrefab == null)
        {
            Debug.LogWarning("[ChestUI] slotPrefab chưa được gán và parent chưa có sẵn InventoryButton!");
            return;
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, parent);
            slotObj.name = $"Slot_{i}";
            InventoryButton btn = slotObj.GetComponent<InventoryButton>();

            if (btn == null)
            {
                Debug.LogWarning("[ChestUI] slotPrefab không có component InventoryButton!");
                Destroy(slotObj);
                continue;
            }

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            slotList.Add(btn);
        }
    }

    /// <summary>
    /// Refresh hiển thị cả 2 panel
    /// </summary>
    private void RefreshAll()
    {
        RefreshPanel(chestSlots, chestContainer);
        RefreshPanel(backpackSlots, backpackContainer);
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

    /// <summary>
    /// Chuyển vật phẩm từ container nguồn sang container đích (khi drop cross-container)
    /// Hỗ trợ gộp stack nếu cùng loại item, hoặc hoán đổi nếu khác item / slot trống.
    /// </summary>
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

        // Nếu cùng container → swap/stack bình thường
        if (source == target)
        {
            source.SwapSlots(sourceIndex, targetIndex);
            return;
        }

        // Cross-container:
        // Trường hợp 1: Nếu cùng loại item và item đó cho phép stack -> Gộp stack
        if (targetSlot.itemData != null &&
            targetSlot.itemData == sourceSlot.itemData &&
            sourceSlot.itemData.isStackable)
        {
            targetSlot.amount += sourceSlot.amount;
            sourceSlot.itemData = null;
            sourceSlot.amount = 0;

            source.NotifyChange();
            target.NotifyChange();
            return;
        }

        // Trường hợp 2: Khác item hoặc ô đích trống -> Hoán đổi dữ liệu giữa 2 slot
        ItemData tempItem = targetSlot.itemData;
        int tempAmount = targetSlot.amount;

        targetSlot.itemData = sourceSlot.itemData;
        targetSlot.amount = sourceSlot.amount;

        sourceSlot.itemData = tempItem;
        sourceSlot.amount = tempAmount;

        // Thông báo cả 2 container đã thay đổi
        source.NotifyChange();
        target.NotifyChange();
    }
}
