using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Combat Loot UI displayed after defeating monsters / ending combat:
/// - Balanced 50/50 two-column layout: [DROPPED LOOT] and [YOUR BACKPACK]
/// - Standard 6-column grid with padding slots for neat alignment
/// - Smooth notifications and transfer animations
/// - [LOOT ALL] button automatically disables when no items remain
/// - [CONTINUE] button highlights to guide player forward
/// </summary>
public class LootUI : MonoBehaviour
{
    public static LootUI Instance { get; private set; }

    [Header("Panel Roots")]
    [SerializeField] public GameObject lootPanel;
    [SerializeField] public Transform lootSlotsParent;
    [SerializeField] public Transform backpackSlotsParent;
    [SerializeField] public ScrollRect lootScrollRect;
    [SerializeField] public ScrollRect bpScrollRect;
    [SerializeField] public Canvas canvas;
    [SerializeField] public GameObject slotPrefab;

    [Header("Controls")]
    [SerializeField] public Button lootAllButton;
    [SerializeField] public Button continueButton;
    [SerializeField] public Image lootAllBtnImg;
    [SerializeField] public Image contBtnImg;
    [SerializeField] public TextMeshProUGUI lootAllTxt;
    [SerializeField] public TextMeshProUGUI contTxt;

    [Header("Labels & Notices")]
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI subtitleText;
    [SerializeField] public TextMeshProUGUI hintText;
    [SerializeField] public TextMeshProUGUI lootHeaderTitle;
    [SerializeField] public TextMeshProUGUI backpackHeaderTitle;
    [SerializeField] public GameObject emptyLootNoticeGO;

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

        BindSceneReferences();
        BindButtonEvents();

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

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            Close();
        }
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

    public void BindSceneReferences()
    {
        if (lootPanel == null)
        {
            Canvas parentCanvas = GetCanvas();
            if (parentCanvas != null)
            {
                Transform lp = parentCanvas.transform.Find("LootPanel");
                if (lp != null) lootPanel = lp.gameObject;
            }
            if (lootPanel == null)
            {
                lootPanel = GameObject.Find("LootPanel");
            }
        }

        if (lootPanel == null) return;

        Transform modal = lootPanel.transform.Find("LootModal");
        if (modal != null)
        {
            Transform header = modal.Find("Header");
            if (header != null)
            {
                titleText = header.Find("TitleText")?.GetComponent<TextMeshProUGUI>() ?? header.Find("TitleCol/Title")?.GetComponent<TextMeshProUGUI>();
                subtitleText = header.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>() ?? header.Find("TitleCol/Subtitle")?.GetComponent<TextMeshProUGUI>();
            }

            Transform body = modal.Find("Body");
            if (body != null)
            {
                Transform leftCol = body.Find("Left_LootColumn") ?? body.Find("Left_ChestColumn");
                if (leftCol != null)
                {
                    lootHeaderTitle = leftCol.Find("HeaderBar/Label")?.GetComponent<TextMeshProUGUI>() ?? leftCol.Find("HeaderBar/ChestTitle")?.GetComponent<TextMeshProUGUI>();
                    lootScrollRect = leftCol.Find("ScrollView")?.GetComponent<ScrollRect>();
                    lootSlotsParent = leftCol.Find("ScrollView/Viewport/Content");
                }

                Transform rightCol = body.Find("Right_BackpackColumn");
                if (rightCol != null)
                {
                    backpackHeaderTitle = rightCol.Find("HeaderBar/BackpackTitle")?.GetComponent<TextMeshProUGUI>();
                    bpScrollRect = rightCol.Find("ScrollView")?.GetComponent<ScrollRect>();
                    backpackSlotsParent = rightCol.Find("ScrollView/Viewport/Content");
                }
            }

            Transform allBtn = modal.Find("LootAllButton");
            if (allBtn != null)
            {
                lootAllButton = allBtn.GetComponent<Button>();
                lootAllBtnImg = allBtn.GetComponent<Image>();
                lootAllTxt = allBtn.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            Transform contBtn = modal.Find("ContinueButton");
            if (contBtn != null)
            {
                continueButton = contBtn.GetComponent<Button>();
                contBtnImg = contBtn.GetComponent<Image>();
                contTxt = contBtn.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            Transform hint = modal.Find("HintText");
            if (hint != null)
            {
                hintText = hint.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    public void EnsureUIBuilt()
    {
        BindSceneReferences();
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
    /// Opens the Loot UI modal with dropped items and the player's backpack
    /// </summary>
    public void Open(List<ItemSlot> incomingLoot, ItemContainer playerBackpack, System.Action onClose)
    {
        EnsureUIBuilt();

        // Hide combat victory messages in the background to avoid overlap
        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.combatLogText != null) CombatUI.Instance.combatLogText.gameObject.SetActive(false);
            if (CombatUI.Instance.victoryPanel != null) CombatUI.Instance.victoryPanel.SetActive(false);
            if (CombatUI.Instance.rewardPanel != null) CombatUI.Instance.rewardPanel.SetActive(false);
        }

        if (playerBackpack == null)
        {
            if (PlayerStats.Instance != null && PlayerStats.Instance.itemContainer != null)
            {
                playerBackpack = PlayerStats.Instance.itemContainer;
            }
            else
            {
                ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
                for (int i = 0; i < containers.Length; i++)
                {
                    if (containers[i] != null && !containers[i].name.ToLower().Contains("chest") && !containers[i].name.ToLower().Contains("loot"))
                    {
                        playerBackpack = containers[i];
                        break;
                    }
                }
            }
        }

        backpackContainer = playerBackpack;
        onLootClosed = onClose;

        // Calculate required loot slots (round up to multiple of 6 for full rows)
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
                hintText.text = "KÃ©o tháº£ váº­t pháº©m vÃ o Ba LÃ´, hoáº·c báº¥m [NHáº¶T Háº¾T] Ä‘á»ƒ thu tháº­p táº¥t cáº£.";
                hintText.color = new Color(0.9f, 0.95f, 1f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = "<color=#68D391>KhÃ´ng cÃ³ váº­t pháº©m má»›i. Báº¡n cÃ³ thá»ƒ sáº¯p xáº¿p ba lÃ´ hoáº·c báº¥m [TIáº¾P Tá»¤C].</color>";
            }
        }

        if (lootPanel != null)
        {
            lootPanel.SetActive(true);
            lootPanel.transform.SetAsLastSibling();
        }

        UIEscapeManager.NotifyModalOpened("Loot");

        if (lootScrollRect != null) lootScrollRect.verticalNormalizedPosition = 1f;
        if (bpScrollRect != null) bpScrollRect.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// Closes the Loot UI and invokes the continuation callback
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

        UIEscapeManager.NotifyModalClosed("Loot");
        CurrentItemPanel.RefreshHotbar();

        System.Action callback = onLootClosed;
        onLootClosed = null;
        callback?.Invoke();
    }

    public void OnContinueClicked()
    {
        OnLootAllClicked();
        Close();
    }

    /// <summary>
    /// Quick transfers an item between Loot and Backpack via right click
    /// </summary>
    public static void QuickTransfer(ItemContainer source, int sourceIndex)
    {
        if (Instance == null || !Instance.IsOpen) return;
        if (source == null || Instance.lootContainer == null || Instance.backpackContainer == null) return;

        ItemContainer target = (source == Instance.lootContainer) ? Instance.backpackContainer : Instance.lootContainer;
        if (sourceIndex < 0 || sourceIndex >= source.itemSlots.Length) return;

        ItemSlot sourceSlot = source.itemSlots[sourceIndex];
        if (sourceSlot == null || sourceSlot.itemData == null || sourceSlot.amount <= 0) return;

        int emptyOrStackIndex = -1;
        if (sourceSlot.itemData.isStackable)
        {
            for (int i = 0; i < target.itemSlots.Length; i++)
            {
                if (target.itemSlots[i] != null && ItemContainer.IsItemMatch(target.itemSlots[i].itemData, sourceSlot.itemData))
                {
                    emptyOrStackIndex = i;
                    break;
                }
            }
        }
        if (emptyOrStackIndex == -1)
        {
            emptyOrStackIndex = target.GetEmptySlot();
        }

        if (emptyOrStackIndex != -1)
        {
            ChestUI.TransferItem(source, sourceIndex, target, emptyOrStackIndex);
            Instance.RefreshAll();
        }
    }

    public void OnLootAllClicked()
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
                hintText.text = "Ba lÃ´ Ä‘Ã£ Ä‘áº§y! HÃ£y dá»n chá»— trá»‘ng hoáº·c kÃ©o tháº£ Ä‘á»•i vá»‹ trÃ­.";
                hintText.color = new Color(1f, 0.6f, 0.3f);
            }
        }
        else
        {
            if (hintText != null)
            {
                hintText.text = itemsLooted > 0
                    ? $"<color=#68D391>Successfully collected {itemsLooted} items into Backpack! Click [CONTINUE] to advance.</color>"
                    : "<color=#68D391>All loot collected. Click [CONTINUE] to advance!</color>";
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
        if (slotPrefab == null)
        {
            slotPrefab = DungeonUIAssetHelper.CreateProceduralSlotPrefab();
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

        // Complete final row for Backpack (33 slots + 3 padding locked slots = 36 slots, 6x6 grid)
        int neededPadding = (container == backpackContainer && container.maxSlots == 33) ? 3 : 0;
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

        // Hide surplus slots beyond capacity
        int targetTotal = container.maxSlots + neededPadding;
        for (int i = targetTotal; i < existingCount; i++)
        {
            existing[i].gameObject.SetActive(false);
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
        backpackHeaderTitle.text = $"BA LÃ” Cá»¦A Báº N ({filled}/{backpackContainer.maxSlots})";
    }

    private void UpdateLootHeader()
    {
        if (lootHeaderTitle != null)
        {
            lootHeaderTitle.text = "Váº¬T PHáº¨M RÆ I RA";
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
                lootAllTxt.text = "NHáº¶T Háº¾T";
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
                contTxt.text = "TIáº¾P Tá»¤C [->]";
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
