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

        return null;
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

        if (slotPrefab == null)
        {
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null)
            {
                InventoryButton btn = invPanel.GetComponentInChildren<InventoryButton>(true);
                if (btn != null) slotPrefab = btn.gameObject;
            }
        }
    }

    public void Open(List<ItemSlot> incomingLoot, ItemContainer playerBackpack, System.Action onClose)
    {
        AutoBindReferences();

        if (lootPanel == null)
        {
            Debug.LogWarning("[LootUI] lootPanel is not assigned!");
            return;
        }

        backpackContainer = playerBackpack;
        onLootClosed = onClose;

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
                hintText.text = "Drag items to your backpack or click [Loot All] to collect.";
                hintText.color = new Color(0.9f, 0.95f, 1f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = "No items dropped this wave. Manage your inventory or click [Continue].";
                hintText.color = new Color(0.7f, 0.75f, 0.85f);
            }
        }

        if (lootPanel != null)
        {
            lootPanel.SetActive(true);
            lootPanel.transform.SetAsLastSibling();
            Debug.Log($"[LootUI] Opened LootPanel successfully! activeSelf={lootPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[LootUI] Cannot open loot panel: lootPanel is NULL!");
        }
    }

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
                hintText.text = "Backpack full! Move unneeded items to the loot slots to discard.";
                hintText.color = new Color(1f, 0.6f, 0.3f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = itemsLooted > 0
                    ? $"Collected {itemsLooted} items into your backpack!"
                    : "All loot collected. Click [Continue]!";
                hintText.color = new Color(0.4f, 1f, 0.5f);
            }
        }
    }

    private void BuildSlots(Transform parent, ItemContainer container, List<InventoryButton> slotList)
    {
        if (parent == null || container == null)
            return;

        Canvas currentCanvas = canvas != null ? canvas : GetComponentInParent<Canvas>();

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

        foreach (var s in slotList)
        {
            if (s != null) Destroy(s.gameObject);
        }
        slotList.Clear();

        for (int i = 0; i < container.maxSlots; i++)
        {
            if (slotPrefab == null)
            {
                Debug.LogWarning("[LootUI] slotPrefab is not assigned!");
                break;
            }

            GameObject slotObj = Instantiate(slotPrefab, parent);
            slotObj.name = $"Slot_{i}";
            InventoryButton btn = slotObj.GetComponent<InventoryButton>();
            if (btn == null) btn = slotObj.AddComponent<InventoryButton>();

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            slotList.Add(btn);
        }
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

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
