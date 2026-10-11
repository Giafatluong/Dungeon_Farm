using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Grand Floor Victory UI modal.
/// Displayed when the player clears all waves of a Floor and slays the Floor Boss.
/// - Chỉ số vĩnh viễn (Permanent Stats) CHỈ ĐƯỢC CỘNG 1 LẦN DUY NHẤT ở lần đầu vượt tầng, vượt lại không được cộng nữa.
/// - Cả 2 cột (Cột Trái: Chỉ số & Phước lành; Cột Phải: Kho báu phần thưởng) đều được tích hợp ScrollRect và thanh cuộn slider dọc đẹp mắt,
///   loại bỏ triệt để tình trạng tràn chữ hay che khuất phần tử.
/// </summary>
public class FloorVictoryUI : MonoBehaviour
{
    #region Singleton & References
    private static FloorVictoryUI _instance;
    public static FloorVictoryUI Instance => _instance;

    public static FloorVictoryUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        FloorVictoryUI found = FindFirstObjectByType<FloorVictoryUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (canvas != null)
        {
            GameObject go = new("FloorVictoryUI");
            go.transform.SetParent(canvas.transform, false);
            _instance = go.AddComponent<FloorVictoryUI>();
            _instance.canvas = canvas;
            return _instance;
        }

        return null;
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform rewardItemsContainer;
    [SerializeField] private ScrollRect rightRewardScrollRect;
    [SerializeField] private ScrollRect leftOverviewScrollRect;

    [Header("Header Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Permanent Stats Section")]
    [SerializeField] private GameObject statsSectionGO;
    [SerializeField] private TextMeshProUGUI statsBadgesText;

    [Header("Blessing Section")]
    [SerializeField] private GameObject blessingSectionGO;
    [SerializeField] private TextMeshProUGUI blessingDetailsText;

    [Header("Action Buttons")]
    [SerializeField] private Button lootAllButton;
    [SerializeField] private Button returnToBaseButton;
    [SerializeField] private Button nextFloorButton;
    [SerializeField] private TextMeshProUGUI statusHintText;

    private FloorData currentFloor;
    private int currentFloorNum;
    private ItemContainer backpackContainer;
    private ItemContainer homeChestContainer;
    private bool isFirstClearOfThisFloor = false;
    private System.Action onReturnCallback;
    private System.Action onNextFloorCallback;

    private readonly List<ItemSlot> currentRewardLoot = new();
    private readonly List<GameObject> activeRewardRows = new();
    private GameObject emptyNoticeGO;

    public bool IsOpen => victoryPanel != null && victoryPanel.activeSelf;
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

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Open & Flow
    public void Open(FloorData floorData, int floorNumber, ItemContainer backpack, System.Action onReturnToBase, System.Action onNextFloor)
    {
        bool isFirst = ProgressionManager.Instance != null && !ProgressionManager.Instance.HasClaimedFirstClearStats(floorNumber > 0 ? floorNumber : 1);
        Open(floorData, floorNumber, backpack, isFirst, onReturnToBase, onNextFloor);
    }

    public void Open(FloorData floorData, int floorNumber, ItemContainer backpack, bool isFirstClear, System.Action onReturnToBase, System.Action onNextFloor)
    {
        CloseConflictingPanels();
        EnsureUIBuilt();

        currentFloor = floorData;
        currentFloorNum = floorNumber > 0 ? floorNumber : (floorData != null ? floorData.floorNumber : 1);
        backpackContainer = backpack;
        isFirstClearOfThisFloor = isFirstClear;
        onReturnCallback = onReturnToBase;
        onNextFloorCallback = onNextFloor;

        LoadHomeChestContainer();

        string floorName = !string.IsNullOrEmpty(currentFloor?.floorName) ? currentFloor.floorName : $"Tầng {currentFloorNum}";
        string bossName = !string.IsNullOrEmpty(currentFloor?.bossName) ? currentFloor.bossName : "Thủ Lĩnh Hầm Ngục";

        if (titleText != null)
        {
            titleText.text = $"🏆 CHIẾN THẮNG TẦNG {currentFloorNum}: {floorName.ToUpper()}! 🏆";
        }
        if (subtitleText != null)
        {
            subtitleText.text = $"👑 TRÙM CUỐI [{bossName.ToUpper()}] ĐÃ BỊ HẠ GỤC HOÀN TOÀN!";
        }

        // Cấu hình chỉ số & phước lành
        SetupPermanentStats();
        SetupBlessingUnlockDisplay();
        SetupFloorLoot();
        SetupButtonsState();

        if (statusHintText != null)
        {
            statusHintText.text = "Bấm <b>[NHẬN TẤT CẢ]</b> để cất đồ vào balo (đồ dư tự chuyển về Rương Nhà Kho).";
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            victoryPanel.transform.SetAsLastSibling();
        }

        // Reset scroll vị trí lên đỉnh
        if (leftOverviewScrollRect != null) leftOverviewScrollRect.verticalNormalizedPosition = 1f;
        if (rightRewardScrollRect != null) rightRewardScrollRect.verticalNormalizedPosition = 1f;

        RefreshRewardItems();
        Debug.Log($"[FloorVictoryUI] Floor {currentFloorNum} victory opened cleanly (FirstClear: {isFirstClearOfThisFloor}). Rewards: {currentRewardLoot.Count}");
    }

    private void CloseConflictingPanels()
    {
        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.victoryPanel != null) CombatUI.Instance.victoryPanel.SetActive(false);
            if (CombatUI.Instance.rewardPanel != null) CombatUI.Instance.rewardPanel.SetActive(false);
            if (CombatUI.Instance.defeatPanel != null) CombatUI.Instance.defeatPanel.SetActive(false);
        }
        if (LootUI.Instance != null && LootUI.Instance.IsOpen)
        {
            LootUI.Instance.Close();
        }
        if (RewardUI.Instance != null && RewardUI.Instance.IsOpen)
        {
            RewardUI.Instance.Close();
        }
    }

    public void Close()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    private void LoadHomeChestContainer()
    {
        if (homeChestContainer != null) return;
        ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
        for (int i = 0; i < containers.Length; i++)
        {
            if (containers[i] != null && containers[i].name.ToLower().Contains("homechest"))
            {
                homeChestContainer = containers[i];
                break;
            }
        }
    }
    #endregion

    #region Stats & Blessings Configuration
    private void SetupPermanentStats()
    {
        if (statsSectionGO == null || statsBadgesText == null) return;

        int bonusATK = currentFloor != null ? currentFloor.bonusPermanentATK : 0;
        int bonusDEF = currentFloor != null ? currentFloor.bonusPermanentDEF : 0;
        int bonusHP = currentFloor != null ? currentFloor.bonusPermanentMaxHP : 0;
        int bonusSpeed = currentFloor != null ? currentFloor.bonusPermanentSpeed : 0;

        if (bonusATK == 0 && bonusDEF == 0 && bonusHP == 0 && bonusSpeed == 0)
        {
            switch (currentFloorNum)
            {
                case 1:
                    bonusATK = 1;
                    bonusHP = 5;
                    break;
                case 2:
                    bonusDEF = 1;
                    bonusHP = 10;
                    break;
                case 3:
                    bonusATK = 2;
                    bonusSpeed = 2;
                    bonusHP = 15;
                    break;
                default:
                    bonusATK = 1;
                    bonusDEF = 1;
                    bonusHP = 10;
                    break;
            }
        }

        // KIỂM TRA ĐIỀU KIỆN LẦN ĐẦU VƯỢT TẦNG:
        // Chỉ cộng trực tiếp khi LẦN ĐẦU VƯỢT TẦNG, vượt lại tuyệt đối KHÔNG ĐƯỢC CỘNG NỮA!
        bool hasClaimedBefore = ProgressionManager.Instance != null && ProgressionManager.Instance.HasClaimedFirstClearStats(currentFloorNum);
        bool shouldGrantFirstClearBonus = isFirstClearOfThisFloor && !hasClaimedBefore;

        if (shouldGrantFirstClearBonus)
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.ApplyPermanentStatBonus(bonusATK, bonusDEF, bonusHP, bonusSpeed);
                ProgressionManager.Instance.MarkFirstClearStatsClaimed(currentFloorNum);
            }

            List<string> badges = new();
            if (bonusATK > 0) badges.Add($"<color=#FF6B6B>⚔️ +{bonusATK} Sát Thương (ATK)</color>");
            if (bonusDEF > 0) badges.Add($"<color=#54A0FF>🛡️ +{bonusDEF} Phòng Thủ (DEF)</color>");
            if (bonusHP > 0) badges.Add($"<color=#1DD1A1>❤️ +{bonusHP} Máu Tối Đa (HP)</color>");
            if (bonusSpeed > 0) badges.Add($"<color=#FECA57>⚡ +{bonusSpeed} Tốc Độ Đi Lượt</color>");

            statsBadgesText.text = $"<b>⭐ PHẦN THƯỞNG LẦN ĐẦU VƯỢT TẦNG:</b>\n{string.Join("\n", badges)}\n\n<size=11><color=#88FF88>✓ Chỉ số đã được cộng vĩnh viễn vào nhân vật tại Trang Trại!</color></size>";
        }
        else
        {
            // Đã vượt tầng này trước đó hoặc đã nhận thưởng lần đầu -> KHÔNG CỘNG NỮA
            statsBadgesText.text = $"<b>⭐ CHỈ SỐ VĨNH VIỄN LẦN ĐẦU:</b> <color=#55FF88>[ĐÃ NHẬN]</color>\n\n<color=#D0D4DC>Bạn đã nhận trọn vẹn điểm chỉ số vĩnh viễn ở lần đầu chinh phục Tầng {currentFloorNum}.</color>\n<size=11><color=#FFD070>⚠️ Vượt lại tầng này sẽ không cộng dồn thêm chỉ số vĩnh viễn.</color></size>";
        }

        statsSectionGO.SetActive(true);
    }

    private void SetupBlessingUnlockDisplay()
    {
        if (blessingSectionGO == null || blessingDetailsText == null) return;

        string blessingLine = "";
        switch (currentFloorNum)
        {
            case 1:
                blessingLine = "• <color=#FFD700>⚔️ Lấy Thủ Làm Công</color>: Chuyển +DEF vào đòn đánh, Defend hồi +1 AP.\n• <color=#55FF88>⚡ Động Lực Tốc Độ</color>: +4 SPD, tăng sát thương khi đi trước.";
                break;
            case 2:
                blessingLine = "• <color=#FFD700>🍲 No Căng Phản Đòn</color>: +8 DEF khi no bụng & phản sát thương cận chiến.\n• <color=#D070FF>🧪 Món Đắng Bào Mòn</color>: Găm độc sâu và giảm 2 DEF của quái vật.";
                break;
            case 3:
                blessingLine = "• <color=#FFD700>🍗 Đại Tiệc Bất Tận</color>: Buff thức ăn kéo dài x2 lượt; món đầu tiên trong trận miễn phí AP.";
                break;
            default:
                blessingLine = "• <color=#FFD700>✨ Thần Lực Hầm Ngục</color>: Toàn bộ phước lành cổ xưa tiếp tục được kích hoạt tối đa.";
                break;
        }

        blessingDetailsText.text = $"{blessingLine}\n\n<size=11><color=#FFE090>Kích hoạt tại Tượng Thần ở Trang Trại trước mỗi chuyến đi.</color></size>";
        blessingSectionGO.SetActive(true);
    }

    private void SetupFloorLoot()
    {
        currentRewardLoot.Clear();

        if (currentFloor != null && currentFloor.clearRewardItems != null && currentFloor.clearRewardItems.Length > 0)
        {
            for (int i = 0; i < currentFloor.clearRewardItems.Length; i++)
            {
                ItemSlot s = currentFloor.clearRewardItems[i];
                if (s != null && s.itemData != null && s.amount > 0)
                {
                    currentRewardLoot.Add(new ItemSlot { itemData = s.itemData, amount = s.amount });
                }
            }
        }

        if (currentRewardLoot.Count == 0)
        {
            ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
            switch (currentFloorNum)
            {
                case 1:
                    AddItemIfExists(allItems, "WheatSeed", 5);
                    AddItemIfExists(allItems, "CarrotSeed", 5);
                    AddItemIfExists(allItems, "Carrot soup", 2);
                    break;
                case 2:
                    AddItemIfExists(allItems, "TomatoSeed", 6);
                    AddItemIfExists(allItems, "CabbageSeed", 6);
                    AddItemIfExists(allItems, "CornSeed", 4);
                    AddItemIfExists(allItems, "Tomato Soup", 2);
                    AddItemIfExists(allItems, "Cabbage Salad", 2);
                    break;
                case 3:
                    AddItemIfExists(allItems, "ChilliSeed", 8);
                    AddItemIfExists(allItems, "CucumberSeed", 8);
                    AddItemIfExists(allItems, "Spicy tomato cabbage carrot", 3);
                    break;
                default:
                    AddItemIfExists(allItems, "CarrotSeed", 6);
                    AddItemIfExists(allItems, "TomatoSeed", 6);
                    break;
            }
        }
    }

    private void AddItemIfExists(ItemData[] allItems, string partialName, int amount)
    {
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null && allItems[i].name.Equals(partialName, System.StringComparison.OrdinalIgnoreCase))
            {
                currentRewardLoot.Add(new ItemSlot { itemData = allItems[i], amount = amount });
                return;
            }
        }

        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null && allItems[i].name.ToLower().Contains(partialName.ToLower()))
            {
                currentRewardLoot.Add(new ItemSlot { itemData = allItems[i], amount = amount });
                return;
            }
        }
    }

    private void SetupButtonsState()
    {
        if (nextFloorButton != null)
        {
            int totalFloors = 3;
            bool hasNextFloor = currentFloorNum < totalFloors;
            nextFloorButton.gameObject.SetActive(hasNextFloor);

            TextMeshProUGUI nextBtnText = nextFloorButton.GetComponentInChildren<TextMeshProUGUI>();
            if (nextBtnText != null)
            {
                nextBtnText.text = $"TIẾN VÀO TẦNG {currentFloorNum + 1} >>";
            }
        }
    }
    #endregion

    #region Loot Rendering & Claiming
    private void RefreshRewardItems()
    {
        if (rewardItemsContainer == null) return;

        if (currentRewardLoot.Count == 0)
        {
            for (int i = 0; i < activeRewardRows.Count; i++)
            {
                if (activeRewardRows[i] != null) activeRewardRows[i].SetActive(false);
            }
            ShowEmptyLootNotice();
            return;
        }

        if (emptyNoticeGO != null)
        {
            emptyNoticeGO.SetActive(false);
        }

        int validIndex = 0;
        for (int i = 0; i < currentRewardLoot.Count; i++)
        {
            ItemSlot slot = currentRewardLoot[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

            if (validIndex < activeRewardRows.Count && activeRewardRows[validIndex] != null)
            {
                activeRewardRows[validIndex].SetActive(true);
                UpdateRewardRow(activeRewardRows[validIndex], slot, i);
            }
            else
            {
                GameObject newRow = CreateRewardRow();
                if (newRow != null)
                {
                    newRow.transform.SetParent(rewardItemsContainer, false);
                    activeRewardRows.Add(newRow);
                    UpdateRewardRow(newRow, slot, i);
                }
            }
            validIndex++;
        }

        for (int i = validIndex; i < activeRewardRows.Count; i++)
        {
            if (activeRewardRows[i] != null) activeRewardRows[i].SetActive(false);
        }

        if (validIndex == 0)
        {
            ShowEmptyLootNotice();
        }
    }

    private void ShowEmptyLootNotice()
    {
        if (rewardItemsContainer == null) return;

        if (emptyNoticeGO == null)
        {
            emptyNoticeGO = new("EmptyNotice", typeof(RectTransform), typeof(Image));
            emptyNoticeGO.transform.SetParent(rewardItemsContainer, false);
            RectTransform rt = emptyNoticeGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440, 56);

            Image bg = emptyNoticeGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.1f, 0.14f, 0.18f, 0.95f));

            GameObject textGO = new("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(emptyNoticeGO.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 4);
            textRT.offsetMax = new Vector2(-10, -4);

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(tmp);
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.95f, 0.85f);
            tmp.text = "<b>ĐÃ THU NHẬN TOÀN BỘ PHẦN THƯỞNG!</b>\n<size=10.5><color=#88FF88>Vật phẩm đã được cất an toàn vào túi đồ / kho lưu trữ.</color></size>";
        }

        emptyNoticeGO.SetActive(true);
        emptyNoticeGO.transform.SetAsLastSibling();
    }

    private void UpdateRewardRow(GameObject row, ItemSlot slot, int slotIndex)
    {
        if (row == null || slot == null || slot.itemData == null) return;

        row.name = $"LootItem_{slotIndex}";
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
            string category = item.itemType == ItemData.ItemType.Seed ? "<color=#A0FFA0>[HẠT GIỐNG]</color>" : "<color=#FFA0A0>[MÓN ĂN / VẬT PHẨM]</color>";
            label.text = $"<b>{item.itemName}</b> <color=#FFD700>x{amount}</color> {category}\n<size=10><color=#C8D4E2>{GetItemDescription(item)}</color></size>";
        }

        Button btn = row.transform.Find("TakeBtn")?.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            int capturedIndex = slotIndex;
            btn.onClick.AddListener(() => OnTakeSingleItem(capturedIndex));
        }
    }

    private string GetItemDescription(ItemData item)
    {
        if (item == null) return "";
        if (item.itemType == ItemData.ItemType.Seed) return "Hạt giống quý giá dùng để gieo trồng tại Trang Trại.";
        if (item.itemType == ItemData.ItemType.Food) return "Món ăn giàu dinh dưỡng hồi phục HP và thể lực.";
        if (item.itemType == ItemData.ItemType.Produce) return "Nông sản tươi ngon thu hoạch từ đất mẹ.";
        return "Chiến lợi phẩm quý giá thu được từ hầm ngục.";
    }

    private void OnTakeSingleItem(int index)
    {
        if (index < 0 || index >= currentRewardLoot.Count) return;
        ItemSlot slot = currentRewardLoot[index];
        if (slot == null || slot.itemData == null || slot.amount <= 0) return;

        bool added = false;
        bool storedToHomeChest = false;

        // 1. Try add to backpack
        if (backpackContainer != null && backpackContainer.CanAddItem(slot.itemData))
        {
            backpackContainer.AddItem(slot.itemData, slot.amount);
            added = true;
        }

        // 2. If backpack full, route to HomeChest
        if (!added && homeChestContainer != null)
        {
            homeChestContainer.AddItem(slot.itemData, slot.amount);
            added = true;
            storedToHomeChest = true;
        }

        if (added)
        {
            currentRewardLoot.RemoveAt(index);
            RefreshRewardItems();

            if (storedToHomeChest)
            {
                SetHint($"<color=#FFD700>Balo đã đầy! {slot.itemData.itemName} đã được chuyển an toàn về Rương Nhà Kho tại Trang Trại!</color>");
            }
            else
            {
                SetHint($"Đã nhận thành công: <b>{slot.amount}x {slot.itemData.itemName}</b> vào túi đồ.");
            }
        }
        else
        {
            SetHint("<color=#FF6666>Không thể nhận vật phẩm: Túi đồ và Rương đều đã đầy!</color>");
        }
    }

    public void OnLootAllClicked()
    {
        if (currentRewardLoot.Count == 0) return;

        int itemsClaimed = 0;
        int homeChestOverflow = 0;

        for (int i = currentRewardLoot.Count - 1; i >= 0; i--)
        {
            ItemSlot slot = currentRewardLoot[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

            bool claimed = false;
            if (backpackContainer != null && backpackContainer.CanAddItem(slot.itemData))
            {
                backpackContainer.AddItem(slot.itemData, slot.amount);
                claimed = true;
                itemsClaimed++;
            }

            if (!claimed && homeChestContainer != null)
            {
                homeChestContainer.AddItem(slot.itemData, slot.amount);
                claimed = true;
                homeChestOverflow++;
                itemsClaimed++;
            }

            if (claimed)
            {
                currentRewardLoot.RemoveAt(i);
            }
        }

        RefreshRewardItems();

        if (homeChestOverflow > 0)
        {
            SetHint($"<color=#55FF88>✓ Đã thu nhận tất cả!</color> <color=#FFD700>({homeChestOverflow} món đầy balo đã tự chuyển về Rương Trang Trại an toàn)</color>");
        }
        else
        {
            SetHint("<color=#55FF88>✓ Đã nhận toàn bộ phần thưởng đặc biệt vào balo thành công!</color>");
        }
    }

    private void SetHint(string msg)
    {
        if (statusHintText != null)
        {
            statusHintText.text = msg;
        }
    }
    #endregion

    #region Action Buttons Click Handlers
    public void OnReturnToBaseClicked()
    {
        OnLootAllClicked();
        Close();
        onReturnCallback?.Invoke();
    }

    public void OnNextFloorClicked()
    {
        OnLootAllClicked();
        Close();
        onNextFloorCallback?.Invoke();
    }
    #endregion

    #region Procedural UI Builder With Visible Vertical Scrollbar Sliders
    private void EnsureUIBuilt()
    {
        if (victoryPanel != null) return;

        if (canvas == null)
        {
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        }

        // 1. Root Modal Overlay
        victoryPanel = new GameObject("VictoryPanel", typeof(RectTransform), typeof(Image));
        victoryPanel.transform.SetParent(transform, false);
        RectTransform rootRT = victoryPanel.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        Image overlayImg = victoryPanel.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);

        // 2. Main Dialog Frame (960 x 580)
        GameObject frameGO = new("MainFrame", typeof(RectTransform), typeof(Image));
        frameGO.transform.SetParent(victoryPanel.transform, false);
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0.5f, 0.5f);
        frameRT.anchorMax = new Vector2(0.5f, 0.5f);
        frameRT.pivot = new Vector2(0.5f, 0.5f);
        frameRT.sizeDelta = new Vector2(960, 580);

        Image frameImg = frameGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(frameImg, new Color(0.12f, 0.10f, 0.07f, 0.98f));

        // -------------------------------------------------------------
        // SECTION 1: HEADER (Anchored top, fixed 72px height)
        // -------------------------------------------------------------
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(frameGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -6);
        headerRT.sizeDelta = new Vector2(-16, 72);

        Image headerBg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerBg, new Color(0.22f, 0.16f, 0.06f, 0.95f));

        // Trophy/Chest Icon
        GameObject iconGO = new("ChestIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(headerGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(16, 0);
        iconRT.sizeDelta = new Vector2(50, 50);

        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = DungeonUIAssetHelper.GetChestSprite();
        iconImg.preserveAspect = true;

        // Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(78, -10);
        titleRT.sizeDelta = new Vector2(-96, 28);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 19f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(1f, 0.88f, 0.25f);

        // Subtitle Text
        GameObject subGO = new("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(headerGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0, 1);
        subRT.anchoredPosition = new Vector2(78, -40);
        subRT.sizeDelta = new Vector2(-96, 24);

        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 12f;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleText.color = new Color(0.95f, 0.90f, 0.75f);

        // -------------------------------------------------------------
        // SECTION 2: BODY (Between Top -84 and Bottom 68)
        // -------------------------------------------------------------
        GameObject bodyGO = new("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(frameGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(16, 68);
        bodyRT.offsetMax = new Vector2(-16, -84);

        // =============================================================
        // LEFT COLUMN: Overview & Stats / Blessings (With Vertical Scrollbar Slider)
        // =============================================================
        GameObject leftColGO = new("LeftColumn_Container", typeof(RectTransform), typeof(Image));
        leftColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.485f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = Vector2.zero;

        Image leftColBg = leftColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftColBg, new Color(0.10f, 0.11f, 0.15f, 0.95f));

        // Left Column ScrollRect
        GameObject leftScrollGO = new("LeftScrollView", typeof(RectTransform), typeof(ScrollRect));
        leftScrollGO.transform.SetParent(leftColGO.transform, false);
        RectTransform leftScrollRT = leftScrollGO.GetComponent<RectTransform>();
        leftScrollRT.anchorMin = Vector2.zero;
        leftScrollRT.anchorMax = Vector2.one;
        leftScrollRT.offsetMin = new Vector2(6, 6);
        leftScrollRT.offsetMax = new Vector2(-6, -6);

        leftOverviewScrollRect = leftScrollGO.GetComponent<ScrollRect>();
        leftOverviewScrollRect.horizontal = false;
        leftOverviewScrollRect.vertical = true;
        leftOverviewScrollRect.scrollSensitivity = 25f;

        // Viewport with RectMask2D (leave 14px on right for slider)
        GameObject leftVpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        leftVpGO.transform.SetParent(leftScrollGO.transform, false);
        RectTransform leftVpRT = leftVpGO.GetComponent<RectTransform>();
        leftVpRT.anchorMin = Vector2.zero;
        leftVpRT.anchorMax = Vector2.one;
        leftVpRT.offsetMin = Vector2.zero;
        leftVpRT.offsetMax = new Vector2(-14, 0);

        // Content
        GameObject leftContentGO = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        leftContentGO.transform.SetParent(leftVpGO.transform, false);
        RectTransform leftContentRT = leftContentGO.GetComponent<RectTransform>();
        leftContentRT.anchorMin = new Vector2(0, 1);
        leftContentRT.anchorMax = new Vector2(1, 1);
        leftContentRT.pivot = new Vector2(0.5f, 1);
        leftContentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup leftVLG = leftContentGO.GetComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 10;
        leftVLG.padding = new RectOffset(4, 4, 4, 4);
        leftVLG.childControlHeight = true;
        leftVLG.childControlWidth = true;
        leftVLG.childForceExpandHeight = false;
        leftVLG.childForceExpandWidth = true;

        ContentSizeFitter leftCsf = leftContentGO.GetComponent<ContentSizeFitter>();
        leftCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        leftOverviewScrollRect.viewport = leftVpRT;
        leftOverviewScrollRect.content = leftContentRT;

        // Thanh slider dọc cho Cột Trái
        DungeonUIAssetHelper.CreateVerticalScrollbar(leftScrollGO.transform, leftOverviewScrollRect, new Color(0.85f, 0.65f, 0.2f, 1f));

        // CARD 1: CƯỜNG HÓA THỂ CHẤT
        statsSectionGO = new("Card_Stats", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        statsSectionGO.transform.SetParent(leftContentGO.transform, false);
        Image statsBg = statsSectionGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(statsBg, new Color(0.16f, 0.13f, 0.08f, 0.95f));

        VerticalLayoutGroup statsVLG = statsSectionGO.GetComponent<VerticalLayoutGroup>();
        statsVLG.padding = new RectOffset(12, 12, 10, 10);
        statsVLG.spacing = 6;
        statsVLG.childControlHeight = true;
        statsVLG.childControlWidth = true;
        statsVLG.childForceExpandHeight = false;
        statsVLG.childForceExpandWidth = true;

        ContentSizeFitter statsCsf = statsSectionGO.GetComponent<ContentSizeFitter>();
        statsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject statsTitleGO = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        statsTitleGO.transform.SetParent(statsSectionGO.transform, false);
        TextMeshProUGUI statsTitleTMP = statsTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statsTitleTMP);
        statsTitleTMP.fontSize = 13f;
        statsTitleTMP.fontStyle = FontStyles.Bold;
        statsTitleTMP.color = new Color(1f, 0.85f, 0.3f);
        statsTitleTMP.text = "⭐ CHỈ SỐ CƯỜNG HÓA VĨNH VIỄN";

        GameObject statsTextGO = new("BadgesText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        statsTextGO.transform.SetParent(statsSectionGO.transform, false);
        statsBadgesText = statsTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statsBadgesText);
        statsBadgesText.fontSize = 11.5f;
        statsBadgesText.color = Color.white;

        // CARD 2: PHƯỚC LÀNH CỔ XƯA
        blessingSectionGO = new("Card_Blessings", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        blessingSectionGO.transform.SetParent(leftContentGO.transform, false);
        Image blessBg = blessingSectionGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(blessBg, new Color(0.11f, 0.11f, 0.19f, 0.95f));

        VerticalLayoutGroup blessVLG = blessingSectionGO.GetComponent<VerticalLayoutGroup>();
        blessVLG.padding = new RectOffset(12, 12, 10, 10);
        blessVLG.spacing = 6;
        blessVLG.childControlHeight = true;
        blessVLG.childControlWidth = true;
        blessVLG.childForceExpandHeight = false;
        blessVLG.childForceExpandWidth = true;

        ContentSizeFitter blessCsf = blessingSectionGO.GetComponent<ContentSizeFitter>();
        blessCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject blessTitleGO = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        blessTitleGO.transform.SetParent(blessingSectionGO.transform, false);
        TextMeshProUGUI blessTitleTMP = blessTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(blessTitleTMP);
        blessTitleTMP.fontSize = 13f;
        blessTitleTMP.fontStyle = FontStyles.Bold;
        blessTitleTMP.color = new Color(0.75f, 0.85f, 1f);
        blessTitleTMP.text = "🔮 PHƯỚC LÀNH CỔ XƯA MỞ KHÓA";

        GameObject blessTextGO = new("DetailsText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        blessTextGO.transform.SetParent(blessingSectionGO.transform, false);
        blessingDetailsText = blessTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(blessingDetailsText);
        blessingDetailsText.fontSize = 11.5f;
        blessingDetailsText.color = Color.white;

        // =============================================================
        // RIGHT COLUMN: Floor Clear Cache (With Vertical Scrollbar Slider)
        // =============================================================
        GameObject rightColGO = new("RightColumn_Container", typeof(RectTransform), typeof(Image));
        rightColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.505f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = Vector2.zero;
        rightColRT.offsetMax = Vector2.zero;

        Image rightColBg = rightColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightColBg, new Color(0.08f, 0.09f, 0.13f, 0.95f));

        // Right Title Bar
        GameObject lootTitleGO = new("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        lootTitleGO.transform.SetParent(rightColGO.transform, false);
        RectTransform ltRT = lootTitleGO.GetComponent<RectTransform>();
        ltRT.anchorMin = new Vector2(0, 1);
        ltRT.anchorMax = new Vector2(1, 1);
        ltRT.pivot = new Vector2(0.5f, 1);
        ltRT.sizeDelta = new Vector2(-20, 26);
        ltRT.anchoredPosition = new Vector2(0, -6);

        TextMeshProUGUI ltTMP = lootTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(ltTMP);
        ltTMP.fontSize = 12.5f;
        ltTMP.fontStyle = FontStyles.Bold;
        ltTMP.color = new Color(1f, 0.85f, 0.3f);
        ltTMP.text = "🎁 KHO BÁU CHIẾN LỢI PHẨM HOÀNG KIM";

        // Scroll Area for Floor Rewards
        GameObject rightScrollGO = new("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        rightScrollGO.transform.SetParent(rightColGO.transform, false);
        RectTransform rightScrollRT = rightScrollGO.GetComponent<RectTransform>();
        rightScrollRT.anchorMin = Vector2.zero;
        rightScrollRT.anchorMax = Vector2.one;
        rightScrollRT.offsetMin = new Vector2(8, 8);
        rightScrollRT.offsetMax = new Vector2(-8, -36);

        rightRewardScrollRect = rightScrollGO.GetComponent<ScrollRect>();
        rightRewardScrollRect.horizontal = false;
        rightRewardScrollRect.vertical = true;
        rightRewardScrollRect.scrollSensitivity = 25f;

        // Viewport with RectMask2D (leaves 14px on right for slider)
        GameObject rightVpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        rightVpGO.transform.SetParent(rightScrollGO.transform, false);
        RectTransform rightVpRT = rightVpGO.GetComponent<RectTransform>();
        rightVpRT.anchorMin = Vector2.zero;
        rightVpRT.anchorMax = Vector2.one;
        rightVpRT.offsetMin = Vector2.zero;
        rightVpRT.offsetMax = new Vector2(-14, 0);

        // Content
        GameObject contentGO = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(rightVpGO.transform, false);
        rewardItemsContainer = contentGO.GetComponent<RectTransform>();
        rewardItemsContainer.anchorMin = new Vector2(0, 1);
        rewardItemsContainer.anchorMax = new Vector2(1, 1);
        rewardItemsContainer.pivot = new Vector2(0.5f, 1);
        rewardItemsContainer.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 6;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rightRewardScrollRect.viewport = rightVpRT;
        rightRewardScrollRect.content = rewardItemsContainer;

        // Thanh slider dọc cho Cột Phải
        DungeonUIAssetHelper.CreateVerticalScrollbar(rightScrollGO.transform, rightRewardScrollRect, new Color(0.85f, 0.65f, 0.15f, 1f));

        // -------------------------------------------------------------
        // SECTION 3: FOOTER (Anchored bottom, height 56px)
        // -------------------------------------------------------------
        GameObject footerGO = new("Footer", typeof(RectTransform), typeof(Image));
        footerGO.transform.SetParent(frameGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 6);
        footerRT.sizeDelta = new Vector2(-16, 56);

        Image footerBg = footerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(footerBg, new Color(0.10f, 0.10f, 0.12f, 0.95f));

        // Status Hint Text (Left half of Footer)
        GameObject hintGO = new("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(footerGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(0, 1);
        hintRT.pivot = new Vector2(0, 0.5f);
        hintRT.anchoredPosition = new Vector2(16, 0);
        hintRT.sizeDelta = new Vector2(430, 48);

        statusHintText = hintGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statusHintText);
        statusHintText.fontSize = 11.5f;
        statusHintText.alignment = TextAlignmentOptions.MidlineLeft;
        statusHintText.color = new Color(0.85f, 0.92f, 1f);

        // Buttons Row (Right half of Footer)
        GameObject btnRowGO = new("ButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRowGO.transform.SetParent(footerGO.transform, false);
        RectTransform btnRowRT = btnRowGO.GetComponent<RectTransform>();
        btnRowRT.anchorMin = new Vector2(1, 0.5f);
        btnRowRT.anchorMax = new Vector2(1, 0.5f);
        btnRowRT.pivot = new Vector2(1, 0.5f);
        btnRowRT.anchoredPosition = new Vector2(-16, 0);
        btnRowRT.sizeDelta = new Vector2(480, 40);

        HorizontalLayoutGroup btnHLG = btnRowGO.GetComponent<HorizontalLayoutGroup>();
        btnHLG.spacing = 10;
        btnHLG.childAlignment = TextAnchor.MiddleRight;
        btnHLG.childForceExpandWidth = false;
        btnHLG.childForceExpandHeight = false;
        btnHLG.childControlWidth = false;
        btnHLG.childControlHeight = false;

        // 1. Loot All Button (Gold)
        lootAllButton = CreateStyledButton(btnRowGO.transform, "BtnLootAll", "🎁 NHẬN TẤT CẢ", new Color(0.85f, 0.65f, 0.15f), new Vector2(145, 38));
        lootAllButton.onClick.AddListener(OnLootAllClicked);

        // 2. Return To Base Button (Emerald Green)
        returnToBaseButton = CreateStyledButton(btnRowGO.transform, "BtnReturn", "🏡 VỀ TRANG TRẠI", new Color(0.18f, 0.55f, 0.28f), new Vector2(145, 38));
        returnToBaseButton.onClick.AddListener(OnReturnToBaseClicked);

        // 3. Next Floor Button (Royal Blue)
        nextFloorButton = CreateStyledButton(btnRowGO.transform, "BtnNextFloor", "TIẾN VÀO TẦNG TIẾP >>", new Color(0.25f, 0.45f, 0.85f), new Vector2(165, 38));
        nextFloorButton.onClick.AddListener(OnNextFloorClicked);
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
        tmp.fontSize = 11.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;

        return btn;
    }

    private GameObject CreateRewardRow()
    {
        GameObject row = new("RewardRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(440, 52);

        Image rowBg = row.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rowBg, new Color(0.12f, 0.14f, 0.18f, 0.90f));

        LayoutElement le = row.GetComponent<LayoutElement>();
        le.minHeight = 52;
        le.preferredHeight = 52;
        le.flexibleWidth = 1;

        // Slot icon box
        GameObject slotBox = new("SlotBox", typeof(RectTransform), typeof(Image));
        slotBox.transform.SetParent(row.transform, false);
        RectTransform sbRT = slotBox.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0, 0.5f);
        sbRT.anchorMax = new Vector2(0, 0.5f);
        sbRT.pivot = new Vector2(0, 0.5f);
        sbRT.sizeDelta = new Vector2(38, 38);
        sbRT.anchoredPosition = new Vector2(8, 0);

        Image sbImg = slotBox.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(sbImg, new Color(0.2f, 0.24f, 0.32f, 1f));

        GameObject iconObj = new("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(slotBox.transform, false);
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(3, 3);
        iconRT.offsetMax = new Vector2(-3, -3);

        // Label
        GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(row.transform, false);
        RectTransform lRT = labelObj.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0, 0);
        lRT.anchorMax = new Vector2(1, 1);
        lRT.offsetMin = new Vector2(54, 2);
        lRT.offsetMax = new Vector2(-88, -2);

        TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(labelTmp);
        labelTmp.fontSize = 11.5f;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = Color.white;

        // Take button
        GameObject takeBtnObj = new("TakeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        takeBtnObj.transform.SetParent(row.transform, false);
        RectTransform tbRT = takeBtnObj.GetComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(1, 0.5f);
        tbRT.anchorMax = new Vector2(1, 0.5f);
        tbRT.pivot = new Vector2(1, 0.5f);
        tbRT.sizeDelta = new Vector2(74, 32);
        tbRT.anchoredPosition = new Vector2(-8, 0);

        Image tbImg = takeBtnObj.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(tbImg, new Color(0.2f, 0.55f, 0.35f, 1f));

        GameObject tbTextObj = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        tbTextObj.transform.SetParent(takeBtnObj.transform, false);
        RectTransform tbtRT = tbTextObj.GetComponent<RectTransform>();
        tbtRT.anchorMin = Vector2.zero;
        tbtRT.anchorMax = Vector2.one;
        tbtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tbtTmp = tbTextObj.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tbtTmp);
        tbtTmp.fontSize = 11.5f;
        tbtTmp.fontStyle = FontStyles.Bold;
        tbtTmp.alignment = TextAlignmentOptions.Center;
        tbtTmp.color = Color.white;
        tbtTmp.text = "NHẬN";

        return row;
    }
    #endregion
}
