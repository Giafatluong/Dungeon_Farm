using UnityEngine;
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
    }

    private Canvas GetCanvas()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        return canvas;
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = GetCanvas();

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
            for (int i = container.maxSlots; i < existingSlots.Length; i++)
            {
                existingSlots[i].gameObject.SetActive(false);
            }
            return;
        }

        foreach (InventoryButton slot in slotList)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }
        slotList.Clear();

        if (slotPrefab == null)
        {
            Debug.LogWarning("[ChestUI] slotPrefab is not assigned and parent has no InventoryButton children!");
            return;
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, parent);
            slotObj.name = $"Slot_{i}";
            InventoryButton btn = slotObj.GetComponent<InventoryButton>();

            if (btn == null)
            {
                Debug.LogWarning("[ChestUI] slotPrefab missing InventoryButton component!");
                Destroy(slotObj);
                continue;
            }

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            slotList.Add(btn);
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

        ItemData tempItem = targetSlot.itemData;
        int tempAmount = targetSlot.amount;

        targetSlot.itemData = sourceSlot.itemData;
        targetSlot.amount = sourceSlot.amount;

        sourceSlot.itemData = tempItem;
        sourceSlot.amount = tempAmount;

        source.NotifyChange();
        target.NotifyChange();
    }
}
