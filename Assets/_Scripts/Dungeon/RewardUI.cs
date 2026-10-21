using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI for the Treasure Vault / Reward Chest room in the Dungeon.
/// Displays unlocked procedural loot, allows individual or batch claiming,
/// and displays real-time backpack inventory status.
/// Styled using project fantasy pixel art assets (UI_Frame, UI_Slot, Golden Chest).
/// </summary>
public class RewardUI : MonoBehaviour
{
    #region Singleton & References
    private static RewardUI _instance;
    public static RewardUI Instance => _instance;

    public static RewardUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        RewardUI found = FindFirstObjectByType<RewardUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (canvas != null)
        {
            GameObject go = new("RewardUI");
            go.transform.SetParent(canvas.transform, false);
            _instance = go.AddComponent<RewardUI>();
            _instance.canvas = canvas;
            return _instance;
        }

        return null;
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private RectTransform chestItemsContainer;
    [SerializeField] private RectTransform backpackSlotsParent;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject slotPrefab;

    [Header("Header Graphics & Labels")]
    [SerializeField] private Image chestHeaderIcon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI leftHeaderTitle;
    [SerializeField] private TextMeshProUGUI backpackHeaderTitle;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Action Buttons")]
    [SerializeField] private Button lootAllButton;
    [SerializeField] private Button continueButton;

    private RewardChest currentChest;
    private ItemContainer backpackContainer;
    private System.Action onCompleteCallback;

    private readonly List<GameObject> activeRewardRows = new();
    private readonly List<InventoryButton> backpackSlots = new();
    private GameObject emptyNoticeGO;

    public bool IsOpen => rewardPanel != null && rewardPanel.activeSelf;
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

        EnsureUIBuilt();

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
        }
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Open & Close
    public void Open(RewardChest chest, List<ItemSlot> loot, ItemContainer backpack, System.Action onComplete, string customTitle = null, string customSubtitle = null)
    {
        EnsureUIBuilt();

        if (titleText != null)
        {
            titleText.text = !string.IsNullOrEmpty(customTitle) ? customTitle : "TREASURE VAULT - STAGE REWARD";
        }
        if (subtitleText != null)
        {
            subtitleText.text = !string.IsNullOrEmpty(customSubtitle) ? customSubtitle : "An ancient treasure cache has unlocked! Claim seeds, rare crops, and provisions.";
        }
        if (leftHeaderTitle != null)
        {
            leftHeaderTitle.text = !string.IsNullOrEmpty(customTitle) ? customTitle.ToUpper() : "CHEST CACHE CONTENTS";
        }

        currentChest = chest;
        if (currentChest == null)
        {
            currentChest = GetComponent<RewardChest>();
            if (currentChest == null) currentChest = gameObject.AddComponent<RewardChest>();
        }

        if (loot != null && loot.Count > 0)
        {
            currentChest.currentChestLoot = new List<ItemSlot>(loot);
        }
        else if (currentChest.currentChestLoot == null)
        {
            currentChest.currentChestLoot = new List<ItemSlot>();
        }

        backpackContainer = backpack;
        onCompleteCallback = onComplete;

        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
            backpackContainer.OnInventoryChange += RefreshBackpackSlots;
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            rewardPanel.transform.SetAsLastSibling();
        }

        RefreshAll();

        SetHint("Click <b>[Take]</b> to pick up an item or <b>[CLAIM ALL]</b> to store all into backpack.");
        Debug.Log("[RewardUI] Reward/Loot room opened successfully.");
    }

    public void Close()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
    }
    #endregion

    #region UI Refresh & Rendering
    public void RefreshAll()
    {
        RefreshChestItems();
        BuildBackpackSlots(backpackContainer);
        UpdateBackpackHeader();
        UpdateControlsState();
    }

    private void UpdateBackpackHeader()
    {
        if (backpackHeaderTitle == null || backpackContainer == null || backpackContainer.itemSlots == null) return;
        int occupied = 0;
        int maxSlots = backpackContainer.maxSlots;
        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            if (backpackContainer.itemSlots[i] != null && backpackContainer.itemSlots[i].itemData != null)
            {
                occupied++;
            }
        }
        backpackHeaderTitle.text = $"YOUR BACKPACK <size=11><color=#A0D8EF>({occupied}/{maxSlots} SLOTS)</color></size>";
    }

    private void RefreshChestItems()
    {
        if (currentChest == null || currentChest.currentChestLoot == null || currentChest.currentChestLoot.Count == 0)
        {
            for (int i = 0; i < activeRewardRows.Count; i++)
            {
                if (activeRewardRows[i] != null) activeRewardRows[i].SetActive(false);
            }
            ShowEmptyChestNotice();
            return;
        }

        if (emptyNoticeGO != null)
        {
            emptyNoticeGO.SetActive(false);
        }

        int validIndex = 0;
        for (int i = 0; i < currentChest.currentChestLoot.Count; i++)
        {
            ItemSlot slot = currentChest.currentChestLoot[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

            if (validIndex < activeRewardRows.Count && activeRewardRows[validIndex] != null)
            {
                GameObject row = activeRewardRows[validIndex];
                row.SetActive(true);
                UpdateRewardRow(row, slot, validIndex);
            }
            else
            {
                GameObject newRow = CreateRewardRow(slot, validIndex);
                if (newRow != null && chestItemsContainer != null)
                {
                    newRow.transform.SetParent(chestItemsContainer, false);
                    activeRewardRows.Add(newRow);
                }
            }
            validIndex++;
        }

        for (int i = validIndex; i < activeRewardRows.Count; i++)
        {
            if (activeRewardRows[i] != null)
            {
                activeRewardRows[i].SetActive(false);
            }
        }

        if (validIndex == 0)
        {
            ShowEmptyChestNotice();
        }
    }

    private void ShowEmptyChestNotice()
    {
        if (chestItemsContainer == null) return;

        if (emptyNoticeGO == null)
        {
            emptyNoticeGO = new("EmptyNotice", typeof(RectTransform), typeof(Image));
            emptyNoticeGO.transform.SetParent(chestItemsContainer, false);
            RectTransform rt = emptyNoticeGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(370, 75);

            Image bg = emptyNoticeGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.1f, 0.12f, 0.16f, 0.9f));

            GameObject textGO = new("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(emptyNoticeGO.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 6);
            textRT.offsetMax = new Vector2(-10, -6);

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textGO.AddComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(tmp);
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.9f, 0.95f, 0.95f);
            tmp.text = "<b>No more loot remaining.</b>\n<size=11><color=#88FF88>All items claimed!</color> Click [CONTINUE] to proceed.</size>";
        }

        emptyNoticeGO.SetActive(true);
        emptyNoticeGO.transform.SetAsLastSibling();
    }

    private void UpdateRewardRow(GameObject row, ItemSlot slot, int slotIndex)
    {
        if (row == null || slot == null || slot.itemData == null) return;

        row.name = $"RewardItem_{slotIndex}";
        ItemData item = slot.itemData;
        int amount = slot.amount;

        Image iconImg = row.transform.Find("SlotBox/Icon")?.GetComponent<Image>();
        if (iconImg != null)
        {
            iconImg.preserveAspect = true;
            iconImg.sprite = item.itemIcon;
            iconImg.enabled = item.itemIcon != null;
        }

        TextMeshProUGUI label = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            string tag = GetItemTag(item);
            label.text = $"<b>{item.itemName}</b> <color=#FFD700>x{amount}</color> {tag}\n<size=11.5><color=#C8D4E2>{GetItemDescription(item)}</color></size>";
        }

        Button btn = row.transform.Find("TakeBtn")?.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                btn.interactable = false;
                OnTakeItemClicked(slot);
            });
        }
    }

    private GameObject CreateRewardRow(ItemSlot slot, int slotIndex)
    {
        GameObject row = new($"RewardItem_{slotIndex}", typeof(RectTransform), typeof(Image));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 60);

        Image bg = row.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.13f, 0.16f, 0.22f, 0.98f));

        GameObject slotBoxGO = new("SlotBox", typeof(RectTransform), typeof(Image));
        slotBoxGO.transform.SetParent(row.transform, false);
        RectTransform slotBoxRT = slotBoxGO.GetComponent<RectTransform>();
        slotBoxRT.anchorMin = new Vector2(0, 0.5f);
        slotBoxRT.anchorMax = new Vector2(0, 0.5f);
        slotBoxRT.anchoredPosition = new Vector2(30, 0);
        slotBoxRT.sizeDelta = new Vector2(44, 44);

        Image slotBoxImg = slotBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(slotBoxImg, new Color(0.2f, 0.24f, 0.32f, 1f));

        GameObject iconGO = new("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotBoxGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = Vector2.zero;
        iconRT.sizeDelta = new Vector2(34, 34);

        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;

        GameObject labelGO = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(62, 4);
        labelRT.offsetMax = new Vector2(-84, -4);

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(label);
        label.fontSize = 13;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject btnGO = new("TakeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-46, 0);
        btnRT.sizeDelta = new Vector2(74, 36);

        Image btnImg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnImg, new Color(0.18f, 0.62f, 0.32f, 1f));

        btnGO.GetComponent<Button>();

        GameObject btnTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnText);
        btnText.fontSize = 12;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.text = "Take";

        UpdateRewardRow(row, slot, slotIndex);
        return row;
    }

    private string GetItemTag(ItemData item)
    {
        if (item == null) return "";
        string nameLower = item.name.ToLower();
        if (nameLower.Contains("seed") || item.itemName.ToLower().Contains("seed"))
        {
            return "<color=#88FF88>[SEED]</color>";
        }
        if (item is FoodData)
        {
            return "<color=#FFAA44>[FOOD]</color>";
        }
        return "<color=#88CCFF>[CROP]</color>";
    }

    private string GetItemDescription(ItemData item)
    {
        if (item is FoodData food)
        {
            return $"+{food.healthValue} HP, +{food.hungerValue} Fullness";
        }
        if (item.name.ToLower().Contains("seed") || item.itemName.ToLower().Contains("seed"))
        {
            return "Cultivate at Farm for crops & buffs";
        }
        return "Valuable resource";
    }

    private void UpdateControlsState()
    {
        bool hasItems = currentChest != null && currentChest.currentChestLoot != null && currentChest.currentChestLoot.Count > 0;
        if (lootAllButton != null)
        {
            lootAllButton.interactable = hasItems;
        }
    }
    #endregion

    #region Action Handlers
    private void OnTakeItemClicked(ItemSlot targetSlot)
    {
        if (currentChest == null || backpackContainer == null || targetSlot == null) return;

        // Find the item in the current loot list by reference (safe after RemoveAt shifts indices)
        int foundIndex = -1;
        for (int i = 0; i < currentChest.currentChestLoot.Count; i++)
        {
            if (currentChest.currentChestLoot[i] == targetSlot)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex < 0)
        {
            // Already claimed (double-click race), just refresh
            RefreshAll();
            return;
        }

        bool success = currentChest.ClaimSingleItem(foundIndex, backpackContainer, out string message);
        if (success)
        {
            SetHint($"<color=#88FF88>Claimed:</color> {message}");
        }
        else
        {
            SetHint($"<color=#FF7777>{message}</color>");
        }
        RefreshAll();
    }

    private void OnLootAllClicked()
    {
        if (currentChest == null || backpackContainer == null) return;

        int claimed = currentChest.ClaimAllLoot(backpackContainer, out bool isFull);
        if (claimed > 0)
        {
            string msg = $"<color=#88FF88>Collected {claimed} item stacks into backpack!</color>";
            if (isFull)
            {
                msg += " <color=#FFCC00>(Backpack is now full!)</color>";
            }
            SetHint(msg);
            RefreshAll();
        }
        else if (isFull)
        {
            SetHint("<color=#FF7777>Backpack is full! Free up inventory slots first.</color>");
        }
    }

    private void OnContinueClicked()
    {
        Close();
    }

    private void SetHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
        }
        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"[Treasure] {message}");
        }
    }
    #endregion

    #region Backpack UI Integration
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

        foreach (var slot in backpackSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        backpackSlots.Clear();

        if (slotPrefab == null)
        {
            Debug.LogWarning("[RewardUI] slotPrefab is null, cannot build backpack slots!");
            return;
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, backpackSlotsParent);
            slotObj.name = $"BackpackSlot_{i}";
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
    #endregion

    #region Procedural UI Building
    private void EnsureUIBuilt()
    {
        if (rewardPanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[RewardUI] No Canvas found in scene!");
            return;
        }

        // 1. Fullscreen Dark Overlay
        GameObject overlayGO = new("RewardPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        rewardPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        // 2. Main Window Modal with 9-Sliced Frame
        GameObject modalGO = new("RewardModal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(overlayGO.transform, false);

        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(920, 580);

        Image modalImg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Header Banner with Chest Graphic
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(modalGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 76);

        Image headerImg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.24f, 0.18f, 0.08f, 1f));

        // Chest Icon in Header
        GameObject chestIconGO = new("ChestIcon", typeof(RectTransform), typeof(Image));
        chestIconGO.transform.SetParent(headerGO.transform, false);
        RectTransform chestIconRT = chestIconGO.GetComponent<RectTransform>();
        chestIconRT.anchorMin = new Vector2(0, 0.5f);
        chestIconRT.anchorMax = new Vector2(0, 0.5f);
        chestIconRT.anchoredPosition = new Vector2(42, 0);
        chestIconRT.sizeDelta = new Vector2(50, 50);

        chestHeaderIcon = chestIconGO.GetComponent<Image>();
        chestHeaderIcon.preserveAspect = true;
        chestHeaderIcon.sprite = DungeonUIAssetHelper.GetChestSprite();

        // Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(80, 24);
        titleRT.offsetMax = new Vector2(-20, -6);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 21;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.88f, 0.38f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.text = "TREASURE VAULT - STAGE REWARD";

        // Subtitle Text
        GameObject subGO = new("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(headerGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.offsetMin = new Vector2(80, 6);
        subRT.offsetMax = new Vector2(-20, -36);

        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 12.5f;
        subtitleText.color = new Color(0.92f, 0.88f, 0.78f);
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleText.text = "An ancient treasure cache has unlocked! Claim seeds, rare crops, and provisions.";

        // 4. Center Columns: Left = Chest Items, Right = Backpack
        GameObject bodyGO = new("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(modalGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(16, 64);
        bodyRT.offsetMax = new Vector2(-16, -84);

        // Left Panel: Chest Items (Sliced Frame)
        GameObject leftPanel = new("LeftChestPanel", typeof(RectTransform), typeof(Image));
        leftPanel.transform.SetParent(bodyGO.transform, false);
        RectTransform leftRT = leftPanel.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0.53f, 1);
        leftRT.offsetMin = Vector2.zero;
        leftRT.offsetMax = new Vector2(-6, 0);

        Image leftImg = leftPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftImg, new Color(0.08f, 0.10f, 0.14f, 0.95f));

        // Left Header
        GameObject leftHeaderGO = new("LeftHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        leftHeaderGO.transform.SetParent(leftPanel.transform, false);
        RectTransform lhRT = leftHeaderGO.GetComponent<RectTransform>();
        lhRT.anchorMin = new Vector2(0, 1);
        lhRT.anchorMax = new Vector2(1, 1);
        lhRT.pivot = new Vector2(0.5f, 1);
        lhRT.sizeDelta = new Vector2(0, 32);

        TextMeshProUGUI lhTMP = leftHeaderGO.GetComponent<TextMeshProUGUI>();
        leftHeaderTitle = lhTMP;
        DungeonUIAssetHelper.ApplyFont(lhTMP);
        lhTMP.fontSize = 13.5f;
        lhTMP.fontStyle = FontStyles.Bold;
        lhTMP.color = new Color(1f, 0.85f, 0.3f);
        lhTMP.alignment = TextAlignmentOptions.Center;
        lhTMP.text = "CHEST CACHE CONTENTS";

        // Scroll Area for Chest Items
        GameObject chestScrollGO = new("ChestScrollArea", typeof(RectTransform), typeof(ScrollRect));
        chestScrollGO.transform.SetParent(leftPanel.transform, false);
        RectTransform chestScrollRT = chestScrollGO.GetComponent<RectTransform>();
        chestScrollRT.anchorMin = Vector2.zero;
        chestScrollRT.anchorMax = Vector2.one;
        chestScrollRT.offsetMin = new Vector2(10, 10);
        chestScrollRT.offsetMax = new Vector2(-10, -36);

        ScrollRect chestScrollRect = chestScrollGO.GetComponent<ScrollRect>();
        chestScrollRect.horizontal = false;
        chestScrollRect.vertical = true;
        chestScrollRect.movementType = ScrollRect.MovementType.Clamped;
        chestScrollRect.scrollSensitivity = 25f;

        // Chest Viewport
        GameObject chestVpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        chestVpGO.transform.SetParent(chestScrollGO.transform, false);
        RectTransform chestVpRT = chestVpGO.GetComponent<RectTransform>();
        chestVpRT.anchorMin = Vector2.zero;
        chestVpRT.anchorMax = Vector2.one;
        chestVpRT.offsetMin = Vector2.zero;
        chestVpRT.offsetMax = new Vector2(-16, 0);

        // Chest Items Container (Content)
        GameObject chestItemsGO = new("ChestItemsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        chestItemsGO.transform.SetParent(chestVpGO.transform, false);
        chestItemsContainer = chestItemsGO.GetComponent<RectTransform>();
        chestItemsContainer.anchorMin = new Vector2(0, 1);
        chestItemsContainer.anchorMax = new Vector2(1, 1);
        chestItemsContainer.pivot = new Vector2(0.5f, 1);
        chestItemsContainer.anchoredPosition = Vector2.zero;
        chestItemsContainer.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = chestItemsGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter chestCsf = chestItemsGO.GetComponent<ContentSizeFitter>();
        chestCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        chestScrollRect.viewport = chestVpRT;
        chestScrollRect.content = chestItemsContainer;

        // Chest Vertical Scrollbar
        GameObject chestSbGO = new("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        chestSbGO.transform.SetParent(chestScrollGO.transform, false);
        RectTransform chestSbRT = chestSbGO.GetComponent<RectTransform>();
        chestSbRT.anchorMin = new Vector2(1, 0);
        chestSbRT.anchorMax = new Vector2(1, 1);
        chestSbRT.pivot = new Vector2(1, 0.5f);
        chestSbRT.sizeDelta = new Vector2(12, 0);
        chestSbRT.anchoredPosition = Vector2.zero;

        Image chestSbBg = chestSbGO.GetComponent<Image>();
        chestSbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar chestSb = chestSbGO.GetComponent<Scrollbar>();
        chestSb.direction = Scrollbar.Direction.BottomToTop;

        GameObject chestSaGO = new("SlidingArea", typeof(RectTransform));
        chestSaGO.transform.SetParent(chestSbGO.transform, false);
        RectTransform chestSaRT = chestSaGO.GetComponent<RectTransform>();
        chestSaRT.anchorMin = Vector2.zero;
        chestSaRT.anchorMax = Vector2.one;
        chestSaRT.sizeDelta = Vector2.zero;

        GameObject chestHandleGO = new("Handle", typeof(RectTransform), typeof(Image));
        chestHandleGO.transform.SetParent(chestSaGO.transform, false);
        RectTransform chestHandleRT = chestHandleGO.GetComponent<RectTransform>();
        chestHandleRT.sizeDelta = Vector2.zero;

        Image chestHandleImg = chestHandleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(chestHandleImg, new Color(0.85f, 0.65f, 0.2f, 1f));

        chestSb.handleRect = chestHandleRT;
        chestSb.targetGraphic = chestHandleImg;
        chestScrollRect.verticalScrollbar = chestSb;

        // Right Panel: Player Backpack (Sliced Frame)
        GameObject rightPanel = new("RightBackpackPanel", typeof(RectTransform), typeof(Image));
        rightPanel.transform.SetParent(bodyGO.transform, false);
        RectTransform rightRT = rightPanel.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.53f, 0);
        rightRT.anchorMax = new Vector2(1, 1);
        rightRT.offsetMin = new Vector2(6, 0);
        rightRT.offsetMax = Vector2.zero;

        Image rightImg = rightPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightImg, new Color(0.08f, 0.10f, 0.14f, 0.95f));

        // Right Header
        GameObject rightHeaderGO = new("RightHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        rightHeaderGO.transform.SetParent(rightPanel.transform, false);
        RectTransform rhRT = rightHeaderGO.GetComponent<RectTransform>();
        rhRT.anchorMin = new Vector2(0, 1);
        rhRT.anchorMax = new Vector2(1, 1);
        rhRT.pivot = new Vector2(0.5f, 1);
        rhRT.sizeDelta = new Vector2(0, 32);

        backpackHeaderTitle = rightHeaderGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(backpackHeaderTitle);
        backpackHeaderTitle.fontSize = 13.5f;
        backpackHeaderTitle.fontStyle = FontStyles.Bold;
        backpackHeaderTitle.color = new Color(0.4f, 0.85f, 1f);
        backpackHeaderTitle.alignment = TextAlignmentOptions.Center;
        backpackHeaderTitle.text = "YOUR BACKPACK";

        // Scroll Area for Backpack
        GameObject bpScrollGO = new("BackpackScrollArea", typeof(RectTransform), typeof(ScrollRect));
        bpScrollGO.transform.SetParent(rightPanel.transform, false);
        RectTransform bpScrollRT = bpScrollGO.GetComponent<RectTransform>();
        bpScrollRT.anchorMin = Vector2.zero;
        bpScrollRT.anchorMax = Vector2.one;
        bpScrollRT.offsetMin = new Vector2(10, 10);
        bpScrollRT.offsetMax = new Vector2(-10, -36);

        ScrollRect bpScrollRect = bpScrollGO.GetComponent<ScrollRect>();
        bpScrollRect.horizontal = false;
        bpScrollRect.vertical = true;
        bpScrollRect.movementType = ScrollRect.MovementType.Clamped;
        bpScrollRect.scrollSensitivity = 25f;

        // Viewport
        GameObject bpViewportGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        bpViewportGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform bpVpRT = bpViewportGO.GetComponent<RectTransform>();
        bpVpRT.anchorMin = Vector2.zero;
        bpVpRT.anchorMax = Vector2.one;
        bpVpRT.offsetMin = Vector2.zero;
        bpVpRT.offsetMax = new Vector2(-16, 0); // leave space for scrollbar on the right

        // Backpack Grid Container (Content)
        GameObject bpSlotsGO = new("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        bpSlotsGO.transform.SetParent(bpViewportGO.transform, false);
        backpackSlotsParent = bpSlotsGO.GetComponent<RectTransform>();
        backpackSlotsParent.anchorMin = new Vector2(0, 1);
        backpackSlotsParent.anchorMax = new Vector2(1, 1);
        backpackSlotsParent.pivot = new Vector2(0.5f, 1);
        backpackSlotsParent.anchoredPosition = Vector2.zero;
        backpackSlotsParent.sizeDelta = new Vector2(0, 0);

        GridLayoutGroup glg = bpSlotsGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(52, 52);
        glg.spacing = new Vector2(6, 6);
        glg.padding = new RectOffset(4, 4, 4, 4);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;

        ContentSizeFitter csf = bpSlotsGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bpScrollRect.viewport = bpVpRT;
        bpScrollRect.content = backpackSlotsParent;

        // Vertical Scrollbar / Slider
        GameObject sbGO = new("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(bpScrollGO.transform, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(12, 0);
        sbRT.anchoredPosition = Vector2.zero;

        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        Scrollbar sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject slideAreaGO = new("SlidingArea", typeof(RectTransform));
        slideAreaGO.transform.SetParent(sbGO.transform, false);
        RectTransform saRT = slideAreaGO.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.sizeDelta = Vector2.zero;

        GameObject handleGO = new("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(slideAreaGO.transform, false);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(0, 0);

        Image handleImg = handleGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(handleImg, new Color(0.25f, 0.42f, 0.65f, 1f));

        sb.handleRect = handleRT;
        sb.targetGraphic = handleImg;
        bpScrollRect.verticalScrollbar = sb;

        // 5. Bottom Action Bar
        GameObject footerGO = new("Footer", typeof(RectTransform));
        footerGO.transform.SetParent(modalGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.sizeDelta = new Vector2(0, 60);

        // Hint Text
        GameObject hintGO = new("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(footerGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0.52f, 1);
        hintRT.offsetMin = new Vector2(18, 4);
        hintRT.offsetMax = new Vector2(-10, -4);

        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(hintText);
        hintText.fontSize = 12;
        hintText.color = new Color(0.85f, 0.9f, 0.96f);
        hintText.alignment = TextAlignmentOptions.MidlineLeft;
        hintText.text = "Items collected will be safely stored in your backpack.";

        // Button: Loot All
        GameObject lootAllGO = new("BtnLootAll", typeof(RectTransform), typeof(Image), typeof(Button));
        lootAllGO.transform.SetParent(footerGO.transform, false);
        RectTransform laRT = lootAllGO.GetComponent<RectTransform>();
        laRT.anchorMin = new Vector2(0.54f, 0.5f);
        laRT.anchorMax = new Vector2(0.54f, 0.5f);
        laRT.pivot = new Vector2(0, 0.5f);
        laRT.sizeDelta = new Vector2(180, 42);
        laRT.anchoredPosition = Vector2.zero;

        Image laImg = lootAllGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(laImg, new Color(0.18f, 0.65f, 0.32f, 1f));

        lootAllButton = lootAllGO.GetComponent<Button>();
        lootAllButton.onClick.AddListener(OnLootAllClicked);

        GameObject laTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        laTextGO.transform.SetParent(lootAllGO.transform, false);
        RectTransform laTextRT = laTextGO.GetComponent<RectTransform>();
        laTextRT.anchorMin = Vector2.zero;
        laTextRT.anchorMax = Vector2.one;
        laTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI laText = laTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(laText);
        laText.fontSize = 13;
        laText.fontStyle = FontStyles.Bold;
        laText.alignment = TextAlignmentOptions.Center;
        laText.color = Color.white;
        laText.text = "CLAIM ALL";

        // Button: Continue
        GameObject contGO = new("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
        contGO.transform.SetParent(footerGO.transform, false);
        RectTransform contRT = contGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(1, 0.5f);
        contRT.anchorMax = new Vector2(1, 0.5f);
        contRT.pivot = new Vector2(1, 0.5f);
        contRT.sizeDelta = new Vector2(180, 42);
        contRT.anchoredPosition = new Vector2(-18, 0);

        Image contImg = contGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(contImg, new Color(0.2f, 0.45f, 0.8f, 1f));

        continueButton = contGO.GetComponent<Button>();
        continueButton.onClick.AddListener(OnContinueClicked);

        GameObject contTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        contTextGO.transform.SetParent(contGO.transform, false);
        RectTransform contTextRT = contTextGO.GetComponent<RectTransform>();
        contTextRT.anchorMin = Vector2.zero;
        contTextRT.anchorMax = Vector2.one;
        contTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI contText = contTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(contText);
        contText.fontSize = 13;
        contText.fontStyle = FontStyles.Bold;
        contText.alignment = TextAlignmentOptions.Center;
        contText.color = Color.white;
        contText.text = "CONTINUE >>";
    }
    #endregion
}
