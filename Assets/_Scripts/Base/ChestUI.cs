using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Chest UI at Base: displays two panels side by side (Chest | Backpack).
/// Supports drag-and-drop between containers (swap or stack).
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

    [Header("Chest Panel (Left)")]
    [SerializeField] private Transform chestSlotsParent;
    [SerializeField] private GameObject slotPrefab;

    [Header("Backpack Panel (Right)")]
    [SerializeField] private Transform backpackSlotsParent;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject chestPanel;

    private ItemContainer chestContainer;
    private ItemContainer backpackContainer;

    private readonly List<InventoryButton> chestSlots = new List<InventoryButton>();
    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();

    private bool isExplicitlyOpened = false;
    public bool IsOpen => isExplicitlyOpened && chestPanel != null && chestPanel.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

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
            Transform t = root.Find("Chest_Frame");
            if (t == null) t = root.Find("Chest_Frame/ChestSlots");
            if (t == null) t = root.Find("ChestSlots");
            if (t != null) chestSlotsParent = t;
        }

        if (backpackSlotsParent == null && root != null)
        {
            Transform t = root.Find("Backpack_Frame");
            if (t == null) t = root.Find("Backpack_Frame/BackpackSlots");
            if (t == null) t = root.Find("BackpackSlots");
            if (t != null) backpackSlotsParent = t;
        }

        if (root != null)
        {
            Image rootImg = root.GetComponent<Image>();
            if (rootImg != null) DungeonUIAssetHelper.StyleSlicedPanel(rootImg, DungeonUIAssetHelper.PanelStyle.Wood);

            Transform chestFrameT = root.Find("Chest_Frame");
            if (chestFrameT != null)
            {
                Image cfImg = chestFrameT.GetComponent<Image>();
                if (cfImg != null) DungeonUIAssetHelper.StyleSlicedPanel(cfImg, DungeonUIAssetHelper.PanelStyle.Slate);
            }

            Transform backpackFrameT = root.Find("Backpack_Frame");
            if (backpackFrameT != null)
            {
                Image bfImg = backpackFrameT.GetComponent<Image>();
                if (bfImg != null) DungeonUIAssetHelper.StyleSlicedPanel(bfImg, DungeonUIAssetHelper.PanelStyle.Slate);
            }
        }

        Button closeBtn = null;
        Transform closeBtnT = root != null ? root.Find("MiddleBar/CloseButton") : null;
        if (closeBtnT != null) closeBtn = closeBtnT.GetComponent<Button>();
        if (closeBtn == null && root != null) closeBtn = root.GetComponentInChildren<Button>(true);

        if (closeBtn != null)
        {
            Image cImg = closeBtn.GetComponent<Image>();
            DungeonUIAssetHelper.StyleTactileButton(closeBtn, cImg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
            closeBtn.onClick.RemoveListener(Close);
            closeBtn.onClick.AddListener(Close);
        }
    }

    private void Start()
    {
        // Only hide on start if not already opened by player interaction.
        // If Open() was called on an inactive GameObject, Unity invokes Start() afterward; do not close it!
        if (!isExplicitlyOpened && chestPanel != null)
        {
            chestPanel.SetActive(false);
        }
    }

    private void Update()
    {
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
        if (_instance == this)
        {
            _instance = null;
        }
    }

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

    public void Open(ItemContainer chest, ItemContainer backpack)
    {
        isExplicitlyOpened = true;
        chestContainer = chest;
        backpackContainer = backpack;

        InventoryController.Instance?.CloseInventoryPanel();
        AutoBindReferences();

        if (chestPanel != null)
        {
            chestPanel.SetActive(true);
            chestPanel.transform.SetAsLastSibling();
        }

        SubscribeEvents();

        BuildSlots(chestSlotsParent, chestContainer, chestSlots);
        BuildSlots(backpackSlotsParent, backpackContainer, backpackSlots);

        RefreshAll();
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
    }

    private Canvas GetCanvas()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        return canvas;
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = GetCanvas();

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

        InventoryButton[] existingSlots = parent.GetComponentsInChildren<InventoryButton>(true);
        if (existingSlots != null && existingSlots.Length > 0)
        {
            slotList.Clear();
            int countToUse = Mathf.Min(existingSlots.Length, container.maxSlots);
            for (int i = 0; i < countToUse; i++)
            {
                existingSlots[i].gameObject.SetActive(true);
                existingSlots[i].SetSlotData(container, i);
                existingSlots[i].SetCanvas(currentCanvas);
                slotList.Add(existingSlots[i]);
            }
            for (int i = countToUse; i < existingSlots.Length; i++)
            {
                existingSlots[i].gameObject.SetActive(false);
            }
            return;
        }
    }

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
}
