using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giao diện Nấu Ăn Đoán Mò (Cooking Pot Experimentation) tại Home Base.
/// Người chơi tự do bỏ các nông sản từ Balo vào Nồi Nấu (tối đa 4 ô).
/// Khi nguyên liệu và số lượng khớp công thức bí mật, sẽ nấu ra đúng món và ghi vào Sổ Tay!
/// </summary>
public class KitchenUI : MonoBehaviour
{
    private static KitchenUI _instance;
    public static KitchenUI Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    public static KitchenUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        _instance = FindFirstObjectByType<KitchenUI>(FindObjectsInactive.Include);
        if (_instance != null) return _instance;

        Canvas c = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (c != null)
        {
            GameObject go = new GameObject("KitchenUI");
            go.transform.SetParent(c.transform, false);
            _instance = go.AddComponent<KitchenUI>();
            _instance.canvas = c;
            return _instance;
        }

        return null;
    }

    [Header("Panels")]
    [SerializeField] private GameObject kitchenPanel;
    [SerializeField] private Canvas canvas;

    [Header("Labels & Feedback")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI feedbackToastText;
    [SerializeField] private Button closeButton;

    // View Tabs
    private enum RightTabView { Ingredients, RecipeBook }
    private RightTabView currentView = RightTabView.Ingredients;

    // UI Dynamic Containers
    private Transform potSlotsRow;
    private Transform inventoryGridContainer;
    private Transform notebookContainer;
    private GameObject resultBannerGO;
    private Image resultFoodIcon;
    private TextMeshProUGUI resultFoodTitle;
    private TextMeshProUGUI resultFoodDesc;
    private Button cookButton;
    private Button clearPotButton;
    private Button tabIngredientsBtn;
    private Button tabNotebookBtn;
    private ScrollRect rightScrollRect;

    // Runtime Cooking Pot Data (Max 5 slots, strictly 1 item per slot)
    private const int MAX_POT_SLOTS = 5;
    private readonly List<ItemSlot> potSlots = new List<ItemSlot>();

    private ItemContainer backpackContainer;
    private ItemContainer chestContainer;
    private KitchenStation currentStation;

    private readonly List<GameObject> activePotVisuals = new List<GameObject>();
    private readonly List<GameObject> activeIngredientVisuals = new List<GameObject>();
    private readonly List<GameObject> activeNotebookVisuals = new List<GameObject>();

    private float toastTimer = 0f;
    private bool isExplicitlyOpened = false;
    private bool isInternalUpdating = false;
    public bool IsOpen => isExplicitlyOpened && kitchenPanel != null && kitchenPanel.activeSelf;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        if (!isExplicitlyOpened && kitchenPanel != null)
        {
            kitchenPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f && feedbackToastText != null)
            {
                feedbackToastText.gameObject.SetActive(false);
            }
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open(KitchenStation station, ItemContainer backpack, ItemContainer chest)
    {
        isExplicitlyOpened = true;
        currentStation = station;
        backpackContainer = backpack;
        chestContainer = chest;

        InventoryController.Instance?.CloseInventoryPanel();

        if (canvas == null)
        {
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        }

        if (kitchenPanel == null)
        {
            BuildUI();
        }

        SubscribeContainerEvents();

        kitchenPanel.SetActive(true);
        kitchenPanel.transform.SetAsLastSibling();

        if (resultBannerGO != null) resultBannerGO.SetActive(false);
        if (feedbackToastText != null) feedbackToastText.gameObject.SetActive(false);

        currentView = RightTabView.Ingredients;
        RefreshUI();
    }

    public void Close()
    {
        isExplicitlyOpened = false;
        ReturnAllPotItemsToBackpack();
        UnsubscribeContainerEvents();

        if (kitchenPanel != null)
        {
            kitchenPanel.SetActive(false);
        }
        if (currentStation != null)
        {
            currentStation.OnUIClosed();
            currentStation = null;
        }
    }

    private void SubscribeContainerEvents()
    {
        UnsubscribeContainerEvents();
        if (backpackContainer != null) backpackContainer.OnInventoryChange += OnBackpackInventoryChanged;
    }

    private void UnsubscribeContainerEvents()
    {
        if (backpackContainer != null) backpackContainer.OnInventoryChange -= OnBackpackInventoryChanged;
    }

    private void OnBackpackInventoryChanged()
    {
        if (isInternalUpdating) return;
        RefreshUI();
    }

    private void OnDisable()
    {
        ReturnAllPotItemsToBackpack();
        UnsubscribeContainerEvents();
    }

    private void OnDestroy()
    {
        ReturnAllPotItemsToBackpack();
        UnsubscribeContainerEvents();
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #region Pot Management & Interactions
    public void AddToPot(ItemData item)
    {
        if (item == null || backpackContainer == null) return;
        if (backpackContainer.GetItemCount(item) <= 0)
        {
            ShowToast($"Không còn {item.itemName} trong Balo!");
            return;
        }

        // Quy tắc: Mỗi ô trong nồi chỉ chứa tối đa đúng 1 nguyên liệu (amount = 1, không cộng dồn)
        if (potSlots.Count >= MAX_POT_SLOTS)
        {
            ShowToast($"Nồi nấu đã đầy ({MAX_POT_SLOTS}/{MAX_POT_SLOTS} ô, mỗi ô 1 nguyên liệu)! Bấm vào ô trong nồi để bỏ ra.");
            return;
        }

        isInternalUpdating = true;
        try
        {
            backpackContainer.RemoveItem(item, 1);
            potSlots.Add(new ItemSlot { itemData = item, amount = 1 });
        }
        finally
        {
            isInternalUpdating = false;
        }

        if (resultBannerGO != null) resultBannerGO.SetActive(false);
        RefreshUI();
    }

    public void RemoveFromPot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= potSlots.Count || backpackContainer == null) return;

        isInternalUpdating = true;
        try
        {
            ItemSlot slot = potSlots[slotIndex];
            if (slot != null && slot.itemData != null)
            {
                ReturnItemSafely(slot.itemData, slot.amount > 0 ? slot.amount : 1);
                potSlots.RemoveAt(slotIndex);
            }
        }
        finally
        {
            isInternalUpdating = false;
        }

        if (resultBannerGO != null) resultBannerGO.SetActive(false);
        RefreshUI();
    }

    /// <summary>
    /// Trả nguyên liệu an toàn về Balo -> Rương Nhà -> rơi trên đất. Cam kết tuyệt đối không mất đồ.
    /// </summary>
    private void ReturnItemSafely(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;

        if (backpackContainer != null && backpackContainer.CanAddItem(item))
        {
            backpackContainer.AddItem(item, amount);
            return;
        }

        if (chestContainer != null && chestContainer.CanAddItem(item))
        {
            chestContainer.AddItem(item, amount);
            ShowToast($"Balo đầy! Đã chuyển {item.itemName} x{amount} vào Rương.");
            return;
        }

        // Rơi xuống đất nếu cả balo và rương đều đầy
        Vector3 dropPos = currentStation != null ? currentStation.transform.position : transform.position;
        if (PlayerStats.Instance != null) dropPos = PlayerStats.Instance.transform.position;

        if (item.dropPrefab != null)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector3 offset = (Vector3)(Random.insideUnitCircle * 0.4f);
                GameObject dropObj = Instantiate(item.dropPrefab, dropPos + offset, Quaternion.identity);
                ItemDrop drop = dropObj.GetComponent<ItemDrop>();
                if (drop != null)
                {
                    drop.itemData = item;
                    drop.amount = 1;
                    drop.itemContainer = backpackContainer;
                    drop.PlayDropEffect();
                }
            }
            ShowToast($"Balo đầy! {item.itemName} đã rơi xuống đất.");
        }
        else if (backpackContainer != null)
        {
            backpackContainer.AddItem(item, amount);
        }
    }

    public void ReturnAllPotItemsToBackpack()
    {
        if (potSlots.Count == 0) return;

        isInternalUpdating = true;
        try
        {
            for (int i = 0; i < potSlots.Count; i++)
            {
                ItemSlot slot = potSlots[i];
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    ReturnItemSafely(slot.itemData, slot.amount);
                }
            }
            potSlots.Clear();
        }
        finally
        {
            isInternalUpdating = false;
        }

        RefreshUI();
    }

    private void ExecuteCook()
    {
        if (potSlots.Count == 0)
        {
            ShowToast("<color=#FFCC00>Nồi đang trống! Hãy chọn nguyên liệu từ Balo bỏ vào nồi trước khi nấu.</color>");
            return;
        }

        if (CookingManager.Instance == null || backpackContainer == null) return;

        bool success = CookingManager.Instance.TryCookPot(potSlots, backpackContainer, out RecipeData matchedRecipe, out bool isNewDiscovery, out string failReason);

        if (success && matchedRecipe != null)
        {
            potSlots.Clear();
            ShowCookResult(true, matchedRecipe, isNewDiscovery);
            ShowToast($"Nấu thành công món: <color=#88FF88>{matchedRecipe.DisplayName}</color>!");
        }
        else
        {
            if (failReason.Contains("đầy"))
            {
                ShowToast($"<color=#FFAA00>{failReason}</color>");
                return;
            }

            potSlots.Clear();
            ShowCookResult(false, null, false);
            ShowToast("<color=#FF6666>Kết hợp sai nguyên liệu! Món ăn đã bị cháy khét.</color>");
        }

        RefreshUI();
    }
    #endregion

    #region UI Rendering
    public void RefreshUI()
    {
        RefreshPotVisuals();

        if (currentView == RightTabView.Ingredients)
        {
            if (inventoryGridContainer != null) inventoryGridContainer.gameObject.SetActive(true);
            if (notebookContainer != null) notebookContainer.gameObject.SetActive(false);
            if (rightScrollRect != null && inventoryGridContainer != null)
            {
                rightScrollRect.content = inventoryGridContainer.GetComponent<RectTransform>();
            }
            RefreshIngredientsGrid();
        }
        else
        {
            if (inventoryGridContainer != null) inventoryGridContainer.gameObject.SetActive(false);
            if (notebookContainer != null) notebookContainer.gameObject.SetActive(true);
            if (rightScrollRect != null && notebookContainer != null)
            {
                rightScrollRect.content = notebookContainer.GetComponent<RectTransform>();
            }
            RefreshNotebookList();
        }

        UpdateTabButtonsState();
    }

    private void RefreshPotVisuals()
    {
        for (int i = 0; i < activePotVisuals.Count; i++)
        {
            if (activePotVisuals[i] != null) Destroy(activePotVisuals[i]);
        }
        activePotVisuals.Clear();

        if (potSlotsRow == null) return;

        int totalCount = 0;
        for (int i = 0; i < MAX_POT_SLOTS; i++)
        {
            ItemSlot slotData = i < potSlots.Count ? potSlots[i] : null;
            if (slotData != null) totalCount += slotData.amount;

            int index = i;
            GameObject slotGO = CreatePotSlotUI(slotData, () => RemoveFromPot(index));
            slotGO.transform.SetParent(potSlotsRow, false);
            activePotVisuals.Add(slotGO);
        }

        if (cookButton != null)
        {
            cookButton.interactable = totalCount > 0;
        }
        if (clearPotButton != null)
        {
            clearPotButton.interactable = totalCount > 0;
        }
    }

    private GameObject CreatePotSlotUI(ItemSlot slot, System.Action onClick)
    {
        GameObject slotGO = new GameObject("PotSlot", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = slotGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 60);

        Image bg = slotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(bg, slot != null);

        Button btn = slotGO.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        if (slot != null && slot.itemData != null)
        {
            // Icon
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(6, 6);
            iconRT.offsetMax = new Vector2(-6, -6);
            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = slot.itemData.itemIcon;
            iconImg.color = Color.white;

            // Single item badge (chứa đúng 1 nguyên liệu)
            GameObject amtGO = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
            amtGO.transform.SetParent(slotGO.transform, false);
            RectTransform amtRT = amtGO.GetComponent<RectTransform>();
            amtRT.anchorMin = new Vector2(1, 0);
            amtRT.anchorMax = new Vector2(1, 0);
            amtRT.pivot = new Vector2(1, 0);
            amtRT.anchoredPosition = new Vector2(-3, 2);
            amtRT.sizeDelta = new Vector2(25, 16);

            TextMeshProUGUI amtTxt = amtGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(amtTxt);
            amtTxt.fontSize = 11;
            amtTxt.fontStyle = FontStyles.Bold;
            amtTxt.alignment = TextAlignmentOptions.BottomRight;
            amtTxt.color = new Color(0.85f, 1f, 0.85f);
            amtTxt.text = "1";
        }
        else
        {
            // Empty placeholder text '+'
            GameObject plusGO = new GameObject("Plus", typeof(RectTransform), typeof(TextMeshProUGUI));
            plusGO.transform.SetParent(slotGO.transform, false);
            RectTransform plusRT = plusGO.GetComponent<RectTransform>();
            plusRT.anchorMin = Vector2.zero;
            plusRT.anchorMax = Vector2.one;
            plusRT.sizeDelta = Vector2.zero;
            TextMeshProUGUI plusTxt = plusGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(plusTxt);
            plusTxt.fontSize = 20;
            plusTxt.alignment = TextAlignmentOptions.Center;
            plusTxt.color = new Color(0.4f, 0.45f, 0.55f, 0.5f);
            plusTxt.text = "+";
        }

        return slotGO;
    }

    private void RefreshIngredientsGrid()
    {
        for (int i = 0; i < activeIngredientVisuals.Count; i++)
        {
            if (activeIngredientVisuals[i] != null) Destroy(activeIngredientVisuals[i]);
        }
        activeIngredientVisuals.Clear();

        if (inventoryGridContainer == null || backpackContainer == null) return;

        // Lọc tất cả nguyên liệu có trong Balo (rau, củ, quả, ngũ cốc)
        Dictionary<ItemData, int> availableItems = new Dictionary<ItemData, int>();
        if (backpackContainer.itemSlots != null)
        {
            for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
            {
                ItemSlot s = backpackContainer.itemSlots[i];
                if (s != null && s.itemData != null && s.amount > 0)
                {
                    // Chỉ cho phép bỏ vào nồi nếu là Produce hoặc Food (nguyên liệu ăn được)
                    if (s.itemData.itemType == ItemData.ItemType.Produce || s.itemData.itemType == ItemData.ItemType.Food || s.itemData is FoodData)
                    {
                        ItemData key = null;
                        foreach (var kvp in availableItems)
                        {
                            if (ItemContainer.IsItemMatch(kvp.Key, s.itemData))
                            {
                                key = kvp.Key;
                                break;
                            }
                        }

                        if (key != null) availableItems[key] += s.amount;
                        else availableItems[s.itemData] = s.amount;
                    }
                }
            }
        }

        if (availableItems.Count == 0)
        {
            GameObject emptyGO = new GameObject("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyGO.transform.SetParent(inventoryGridContainer, false);
            TextMeshProUGUI txt = emptyGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(txt);
            txt.fontSize = 12;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.7f, 0.7f, 0.7f);
            txt.text = "Balo của bạn không có nông sản hoặc nguyên liệu nào!\nHãy thu hoạch cây trồng ở trang trại.";
            activeIngredientVisuals.Add(emptyGO);
            return;
        }

        foreach (var kvp in availableItems)
        {
            ItemData item = kvp.Key;
            int count = kvp.Value;

            GameObject card = CreateIngredientCard(item, count, () => AddToPot(item));
            card.transform.SetParent(inventoryGridContainer, false);
            activeIngredientVisuals.Add(card);
        }
    }

    private GameObject CreateIngredientCard(ItemData item, int count, System.Action onAdd)
    {
        GameObject card = new GameObject($"Ing_{item.name}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320, 52);

        Image bg = card.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(bg, DungeonUIAssetHelper.PanelStyle.Wood);

        Button btn = card.GetComponent<Button>();
        btn.onClick.AddListener(() => onAdd?.Invoke());

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(card.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(28, 0);
        iconRT.sizeDelta = new Vector2(38, 38);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = item.itemIcon;
        iconImg.color = Color.white;

        // Name & Count
        GameObject infoGO = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoGO.transform.SetParent(card.transform, false);
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 0);
        infoRT.anchorMax = new Vector2(1, 1);
        infoRT.offsetMin = new Vector2(58, 4);
        infoRT.offsetMax = new Vector2(-75, -4);
        TextMeshProUGUI infoTxt = infoGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(infoTxt);
        infoTxt.fontSize = 13;
        infoTxt.alignment = TextAlignmentOptions.MidlineLeft;
        infoTxt.color = Color.white;
        infoTxt.text = $"<b>{item.itemName}</b>\n<size=11.5><color=#AAAAAA>Có sẵn: <color=#88FF88>{count}</color></color></size>";

        // Add Button Badge
        GameObject addBadge = new GameObject("AddBadge", typeof(RectTransform), typeof(Image));
        addBadge.transform.SetParent(card.transform, false);
        RectTransform badgeRT = addBadge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(1, 0.5f);
        badgeRT.anchorMax = new Vector2(1, 0.5f);
        badgeRT.anchoredPosition = new Vector2(-36, 0);
        badgeRT.sizeDelta = new Vector2(62, 30);
        Image badgeImg = addBadge.GetComponent<Image>();
        DungeonUIAssetHelper.StyleTactileButton(null, badgeImg, DungeonUIAssetHelper.ButtonStyle.Victory);

        GameObject addTxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        addTxtGO.transform.SetParent(addBadge.transform, false);
        RectTransform addTxtRT = addTxtGO.GetComponent<RectTransform>();
        addTxtRT.anchorMin = Vector2.zero;
        addTxtRT.anchorMax = Vector2.one;
        addTxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI addTxt = addTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(addTxt);
        addTxt.fontSize = 12;
        addTxt.fontStyle = FontStyles.Bold;
        addTxt.alignment = TextAlignmentOptions.Center;
        addTxt.color = Color.white;
        addTxt.text = "+ THÊM";

        return card;
    }

    private void RefreshNotebookList()
    {
        for (int i = 0; i < activeNotebookVisuals.Count; i++)
        {
            if (activeNotebookVisuals[i] != null) Destroy(activeNotebookVisuals[i]);
        }
        activeNotebookVisuals.Clear();

        if (notebookContainer == null) return;

        List<RecipeData> allRecipes = CookingManager.Instance != null
            ? CookingManager.Instance.GetAllAvailableRecipes()
            : new List<RecipeData>();

        int discoveredCount = 0;
        for (int i = 0; i < allRecipes.Count; i++)
        {
            RecipeData r = allRecipes[i];
            if (r == null) continue;

            bool isDiscovered = ProgressionManager.Instance != null && ProgressionManager.Instance.IsRecipeDiscovered(r);
            if (isDiscovered) discoveredCount++;

            GameObject card = CreateNotebookCard(r, isDiscovered);
            card.transform.SetParent(notebookContainer, false);
            activeNotebookVisuals.Add(card);
        }

        if (allRecipes.Count == 0)
        {
            GameObject emptyGO = new GameObject("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyGO.transform.SetParent(notebookContainer, false);
            TextMeshProUGUI txt = emptyGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(txt);
            txt.fontSize = 12;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.7f, 0.7f, 0.7f);
            txt.text = "Chưa có công thức nào trong hệ thống!";
            activeNotebookVisuals.Add(emptyGO);
        }
    }

    private GameObject CreateNotebookCard(RecipeData recipe, bool isDiscovered)
    {
        GameObject card = new GameObject($"Recipe_{recipe.recipeName}", typeof(RectTransform), typeof(Image));
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320, 60);

        Image bg = card.GetComponent<Image>();
        Color cardColor = isDiscovered ? new Color(0.14f, 0.18f, 0.24f, 0.95f) : new Color(0.10f, 0.11f, 0.14f, 0.7f);
        DungeonUIAssetHelper.StyleSlicedFrame(bg, cardColor);

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(card.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(30, 0);
        iconRT.sizeDelta = new Vector2(40, 40);
        Image iconImg = iconGO.GetComponent<Image>();

        if (isDiscovered && recipe.resultFood != null && recipe.resultFood.itemIcon != null)
        {
            iconImg.sprite = recipe.resultFood.itemIcon;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        }

        // Info Text
        GameObject infoGO = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoGO.transform.SetParent(card.transform, false);
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 0);
        infoRT.anchorMax = new Vector2(1, 1);
        infoRT.offsetMin = new Vector2(60, 4);
        infoRT.offsetMax = new Vector2(-10, -4);
        TextMeshProUGUI infoTxt = infoGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(infoTxt);
        infoTxt.fontSize = 12.5f;
        infoTxt.alignment = TextAlignmentOptions.MidlineLeft;

        if (isDiscovered)
        {
            infoRT.offsetMax = new Vector2(-78, -4);

            System.Text.StringBuilder ingSb = new System.Text.StringBuilder();
            bool hasAll = true;
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Length; i++)
                {
                    ItemRequirement req = recipe.ingredients[i];
                    if (req.item != null)
                    {
                        int has = backpackContainer != null ? backpackContainer.GetItemCount(req.item) : 0;
                        for (int p = 0; p < potSlots.Count; p++)
                        {
                            if (potSlots[p] != null && ItemContainer.IsItemMatch(potSlots[p].itemData, req.item))
                            {
                                has += potSlots[p].amount;
                            }
                        }
                        if (has < req.amount) hasAll = false;
                        ingSb.Append($"{req.item.itemName} x{req.amount}  ");
                    }
                }
            }
            infoTxt.text = $"<b><color=#FFD700>{recipe.DisplayName}</color></b>\n<size=11><color=#88FF88>Công thức:</color> {ingSb.ToString().TrimEnd()}</size>";

            // Quick Fill Button: Tự động xếp nguyên liệu vào từng ô trong nồi
            GameObject fillBtnGO = new GameObject("FillBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            fillBtnGO.transform.SetParent(card.transform, false);
            RectTransform fillRT = fillBtnGO.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(1, 0.5f);
            fillRT.anchorMax = new Vector2(1, 0.5f);
            fillRT.pivot = new Vector2(1, 0.5f);
            fillRT.anchoredPosition = new Vector2(-8, 0);
            fillRT.sizeDelta = new Vector2(66, 32);
            Image fillImg = fillBtnGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleTactileButton(null, fillImg, hasAll ? DungeonUIAssetHelper.ButtonStyle.PrimaryCook : DungeonUIAssetHelper.ButtonStyle.WoodRustic);

            Button fillBtn = fillBtnGO.GetComponent<Button>();
            fillBtn.interactable = hasAll;
            fillBtn.onClick.AddListener(() => AutoFillRecipeToPot(recipe));

            GameObject fillTxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            fillTxtGO.transform.SetParent(fillBtnGO.transform, false);
            RectTransform fillTxtRT = fillTxtGO.GetComponent<RectTransform>();
            fillTxtRT.anchorMin = Vector2.zero;
            fillTxtRT.anchorMax = Vector2.one;
            fillTxtRT.sizeDelta = Vector2.zero;
            TextMeshProUGUI fillTxt = fillTxtGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(fillTxt);
            fillTxt.fontSize = 11;
            fillTxt.fontStyle = FontStyles.Bold;
            fillTxt.alignment = TextAlignmentOptions.Center;
            fillTxt.color = hasAll ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            fillTxt.text = "XẾP VÀO";
        }
        else
        {
            infoRT.offsetMax = new Vector2(-10, -4);
            infoTxt.text = "<b><color=#777777>??? Món ăn bí mật ???</color></b>\n<size=11><color=#555555>Chưa khám phá - Hãy thử đoán mò các nguyên liệu!</color></size>";
        }

        return card;
    }

    private void AutoFillRecipeToPot(RecipeData recipe)
    {
        if (recipe == null || backpackContainer == null) return;

        int totalNeeded = 0;
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            var req = recipe.ingredients[i];
            if (req != null && req.item != null && req.amount > 0)
            {
                totalNeeded += req.amount;
                int has = backpackContainer.GetItemCount(req.item);
                for (int p = 0; p < potSlots.Count; p++)
                {
                    if (potSlots[p] != null && ItemContainer.IsItemMatch(potSlots[p].itemData, req.item))
                    {
                        has += potSlots[p].amount;
                    }
                }

                if (has < req.amount)
                {
                    ShowToast($"Không đủ {req.item.itemName} (cần {req.amount}, hiện có {has})!");
                    return;
                }
            }
        }

        if (totalNeeded > MAX_POT_SLOTS)
        {
            ShowToast($"Món này cần {totalNeeded} nguyên liệu, vượt quá {MAX_POT_SLOTS} ô trong nồi!");
            return;
        }

        ReturnAllPotItemsToBackpack();

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            var req = recipe.ingredients[i];
            if (req != null && req.item != null && req.amount > 0)
            {
                for (int a = 0; a < req.amount; a++)
                {
                    AddToPot(req.item);
                }
            }
        }

        ShowToast($"Đã xếp nguyên liệu món <color=#88FF88>{recipe.DisplayName}</color> vào từng ô trong nồi!");
    }

    private void ShowCookResult(bool success, RecipeData recipe, bool isNewDiscovery)
    {
        if (resultBannerGO == null) return;
        resultBannerGO.SetActive(true);
        Image rbImg = resultBannerGO.GetComponent<Image>();
        if (rbImg != null)
        {
            DungeonUIAssetHelper.StylePlaque(rbImg, success ? DungeonUIAssetHelper.PlaqueStyle.Victory : DungeonUIAssetHelper.PlaqueStyle.Defeat);
        }

        if (success && recipe != null && recipe.resultFood != null)
        {
            if (resultFoodIcon != null)
            {
                resultFoodIcon.gameObject.SetActive(true);
                resultFoodIcon.sprite = recipe.resultFood.itemIcon;
                resultFoodIcon.color = Color.white;
            }

            string discoveryTag = isNewDiscovery ? "<color=#FFD700>★ MÓN MỚI KHÁM PHÁ! ĐÃ LƯU VÀO SỔ TAY!</color>\n" : "";
            if (resultFoodTitle != null)
            {
                resultFoodTitle.text = $"{discoveryTag}<b><color=#88FF88>{recipe.resultFood.itemName}</color></b>";
            }
            if (resultFoodDesc != null)
            {
                string buffInfo = GetFoodBuffSummary(recipe.resultFood);
                resultFoodDesc.text = !string.IsNullOrEmpty(buffInfo) ? buffInfo : "Thơm ngon bổ dưỡng!";
            }
        }
        else
        {
            if (resultFoodIcon != null)
            {
                resultFoodIcon.gameObject.SetActive(false);
            }
            if (resultFoodTitle != null)
            {
                resultFoodTitle.text = "<b><color=#FF6666>NẤU THẤT BÀI (CHÁY KHÉT)</color></b>";
            }
            if (resultFoodDesc != null)
            {
                resultFoodDesc.text = "<color=#CCCCCC>Nguyên liệu không tạo thành món ăn nào. Hãy thử kết hợp lại!</color>";
            }
        }
    }

    private void UpdateTabButtonsState()
    {
        if (tabIngredientsBtn != null)
        {
            Image img = tabIngredientsBtn.GetComponent<Image>();
            DungeonUIAssetHelper.StyleTactileButton(tabIngredientsBtn, img, currentView == RightTabView.Ingredients ? DungeonUIAssetHelper.ButtonStyle.Defend : DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        }
        if (tabNotebookBtn != null)
        {
            Image img = tabNotebookBtn.GetComponent<Image>();
            DungeonUIAssetHelper.StyleTactileButton(tabNotebookBtn, img, currentView == RightTabView.RecipeBook ? DungeonUIAssetHelper.ButtonStyle.Defend : DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        }
    }

    private void ShowToast(string message)
    {
        if (feedbackToastText == null) return;
        feedbackToastText.text = message;
        feedbackToastText.gameObject.SetActive(true);
        toastTimer = 3.5f;
    }

    private string GetFoodBuffSummary(FoodData food)
    {
        if (food == null) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (food.healthValue > 0) sb.Append($"<color=#66FF88>+{food.healthValue} HP</color> ");
        if (food.hungerValue > 0) sb.Append($"<color=#FFCC66>+{food.hungerValue} Độ no</color> ");

        if (food.foodBuff != null && food.foodBuff.Length > 0)
        {
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                var fb = food.foodBuff[i];
                if (fb != null && fb.buffs != null)
                {
                    string bName = fb.buffs.buffName;
                    if (string.IsNullOrEmpty(bName)) bName = fb.buffs.buffType.ToString();
                    string sign = fb.buffValue > 0 ? "+" : "";
                    sb.Append($"<color=#66CCFF>{sign}{fb.buffValue} {bName}</color> ");
                }
            }
        }
        return sb.ToString().Trim();
    }
    #endregion

    #region Build UI Structure
    private void BuildUI()
    {
        if (canvas == null) canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (canvas == null) return;

        // 1. Root Modal Backdrop (Blocks all raycasts to world & hotbars)
        kitchenPanel = new GameObject("KitchenPanel", typeof(RectTransform), typeof(Image));
        kitchenPanel.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = kitchenPanel.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;

        Image overlayBg = kitchenPanel.GetComponent<Image>();
        overlayBg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        // 2. Central Window Panel
        GameObject windowGO = new GameObject("KitchenWindow", typeof(RectTransform), typeof(Image));
        windowGO.transform.SetParent(kitchenPanel.transform, false);
        RectTransform rootRT = windowGO.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = new Vector2(820, 540);
        rootRT.anchoredPosition = Vector2.zero;

        Image rootBg = windowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(rootBg, DungeonUIAssetHelper.PanelStyle.Wood);

        // 3. Header
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(windowGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 72);
        headerRT.anchoredPosition = Vector2.zero;
        Image headerBg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(headerBg, DungeonUIAssetHelper.PlaqueStyle.Gold);

        // Title
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(25, 20);
        titleRT.offsetMax = new Vector2(-60, 0);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.35f);
        titleText.text = "BẾP NẤU THỬ NGHIỆM (COOKING POT)";

        // Subtitle
        GameObject subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(headerGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.offsetMin = new Vector2(25, 4);
        subRT.offsetMax = new Vector2(-60, -26);
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 12.5f;
        subtitleText.text = "Tối đa 4 loại nguyên liệu (tự động cộng dồn)! Bỏ nguyên liệu vào nồi để đoán công thức bí mật.";

        // Close Button
        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(headerGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f);
        closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-28, 0);
        closeRT.sizeDelta = new Vector2(36, 36);
        Image closeImg = closeGO.GetComponent<Image>();
        closeButton = closeGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(closeButton, closeImg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        closeButton.onClick.AddListener(Close);

        GameObject closeTxtGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtGO.transform.SetParent(closeGO.transform, false);
        RectTransform closeTxtRT = closeTxtGO.GetComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero;
        closeTxtRT.anchorMax = Vector2.one;
        closeTxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeTxt = closeTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(closeTxt);
        closeTxt.fontSize = 14;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.color = Color.white;
        closeTxt.text = "X";

        // 4. Body Layout: Left Side (Pot) & Right Side (Ingredients / Notebook)
        // LEFT COLUMN: COOKING POT
        GameObject leftCol = new GameObject("LeftPotCol", typeof(RectTransform), typeof(Image));
        leftCol.transform.SetParent(windowGO.transform, false);
        RectTransform leftRT = leftCol.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0.52f, 1);
        leftRT.offsetMin = new Vector2(16, 44);
        leftRT.offsetMax = new Vector2(-8, -76);
        Image leftBg = leftCol.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(leftBg, DungeonUIAssetHelper.PanelStyle.Slate);

        // Pot Label
        GameObject potLabelGO = new GameObject("PotLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        potLabelGO.transform.SetParent(leftCol.transform, false);
        RectTransform plRT = potLabelGO.GetComponent<RectTransform>();
        plRT.anchorMin = new Vector2(0, 1);
        plRT.anchorMax = new Vector2(1, 1);
        plRT.pivot = new Vector2(0.5f, 1);
        plRT.sizeDelta = new Vector2(0, 32);
        plRT.anchoredPosition = new Vector2(0, -10);
        TextMeshProUGUI plTxt = potLabelGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(plTxt);
        plTxt.fontSize = 14;
        plTxt.alignment = TextAlignmentOptions.Center;
        plTxt.color = new Color(1f, 0.8f, 0.4f);
        plTxt.text = "🔥 NỒI NẤU (MỖI Ô CHỨA TỐI ĐA 1 NGUYÊN LIỆU)";

        // Pot Slots Row (Supports 5 slots, 1 ingredient per slot)
        GameObject potRowGO = new GameObject("PotSlotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        potRowGO.transform.SetParent(leftCol.transform, false);
        potSlotsRow = potRowGO.transform;
        RectTransform prRT = potRowGO.GetComponent<RectTransform>();
        prRT.anchorMin = new Vector2(0.5f, 1);
        prRT.anchorMax = new Vector2(0.5f, 1);
        prRT.pivot = new Vector2(0.5f, 1);
        prRT.sizeDelta = new Vector2(360, 74);
        prRT.anchoredPosition = new Vector2(0, -48);
        HorizontalLayoutGroup potHlg = potRowGO.GetComponent<HorizontalLayoutGroup>();
        potHlg.spacing = 8;
        potHlg.childAlignment = TextAnchor.MiddleCenter;
        potHlg.childControlWidth = false;
        potHlg.childControlHeight = false;

        // Cook Button (Big, Golden-Orange)
        GameObject cookBtnGO = new GameObject("CookButton", typeof(RectTransform), typeof(Image), typeof(Button));
        cookBtnGO.transform.SetParent(leftCol.transform, false);
        RectTransform cookBtnRT = cookBtnGO.GetComponent<RectTransform>();
        cookBtnRT.anchorMin = new Vector2(0.5f, 1);
        cookBtnRT.anchorMax = new Vector2(0.5f, 1);
        cookBtnRT.pivot = new Vector2(0.5f, 1);
        cookBtnRT.anchoredPosition = new Vector2(-60, -135);
        cookBtnRT.sizeDelta = new Vector2(180, 48);

        Image cookBtnImg = cookBtnGO.GetComponent<Image>();
        cookButton = cookBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(cookButton, cookBtnImg, DungeonUIAssetHelper.ButtonStyle.PrimaryCook);
        cookButton.onClick.AddListener(ExecuteCook);

        GameObject cookTxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        cookTxtGO.transform.SetParent(cookBtnGO.transform, false);
        RectTransform cookTxtRT = cookTxtGO.GetComponent<RectTransform>();
        cookTxtRT.anchorMin = Vector2.zero;
        cookTxtRT.anchorMax = Vector2.one;
        cookTxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI cookTxt = cookTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(cookTxt);
        cookTxt.fontSize = 15;
        cookTxt.fontStyle = FontStyles.Bold;
        cookTxt.alignment = TextAlignmentOptions.Center;
        cookTxt.color = Color.white;
        cookTxt.text = "BẬT LỬA NẤU";

        // Clear Pot Button
        GameObject clearBtnGO = new GameObject("ClearButton", typeof(RectTransform), typeof(Image), typeof(Button));
        clearBtnGO.transform.SetParent(leftCol.transform, false);
        RectTransform clearBtnRT = clearBtnGO.GetComponent<RectTransform>();
        clearBtnRT.anchorMin = new Vector2(0.5f, 1);
        clearBtnRT.anchorMax = new Vector2(0.5f, 1);
        clearBtnRT.pivot = new Vector2(0.5f, 1);
        clearBtnRT.anchoredPosition = new Vector2(90, -135);
        clearBtnRT.sizeDelta = new Vector2(100, 48);

        Image clearBtnImg = clearBtnGO.GetComponent<Image>();
        clearPotButton = clearBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(clearPotButton, clearBtnImg, DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        clearPotButton.onClick.AddListener(ReturnAllPotItemsToBackpack);

        GameObject clearTxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        clearTxtGO.transform.SetParent(clearBtnGO.transform, false);
        RectTransform clearTxtRT = clearTxtGO.GetComponent<RectTransform>();
        clearTxtRT.anchorMin = Vector2.zero;
        clearTxtRT.anchorMax = Vector2.one;
        clearTxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI clearTxt = clearTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(clearTxt);
        clearTxt.fontSize = 12;
        clearTxt.alignment = TextAlignmentOptions.Center;
        clearTxt.color = new Color(0.9f, 0.9f, 0.9f);
        clearTxt.text = "THU HỒI\n(TRẢ LẠI)";

        // Result Banner Area
        resultBannerGO = new GameObject("ResultBanner", typeof(RectTransform), typeof(Image));
        resultBannerGO.transform.SetParent(leftCol.transform, false);
        RectTransform rbRT = resultBannerGO.GetComponent<RectTransform>();
        rbRT.anchorMin = new Vector2(0, 0);
        rbRT.anchorMax = new Vector2(1, 0);
        rbRT.pivot = new Vector2(0.5f, 0);
        rbRT.sizeDelta = new Vector2(0, 160);
        rbRT.anchoredPosition = new Vector2(0, 12);
        Image rbImg = resultBannerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(rbImg, DungeonUIAssetHelper.PlaqueStyle.Victory);

        // Result Icon
        GameObject rIconGO = new GameObject("FoodIcon", typeof(RectTransform), typeof(Image));
        rIconGO.transform.SetParent(resultBannerGO.transform, false);
        RectTransform rIconRT = rIconGO.GetComponent<RectTransform>();
        rIconRT.anchorMin = new Vector2(0.5f, 1);
        rIconRT.anchorMax = new Vector2(0.5f, 1);
        rIconRT.anchoredPosition = new Vector2(0, -36);
        rIconRT.sizeDelta = new Vector2(52, 52);
        resultFoodIcon = rIconGO.GetComponent<Image>();

        // Result Title
        GameObject rTitleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        rTitleGO.transform.SetParent(resultBannerGO.transform, false);
        RectTransform rTitleRT = rTitleGO.GetComponent<RectTransform>();
        rTitleRT.anchorMin = new Vector2(0, 0);
        rTitleRT.anchorMax = new Vector2(1, 0);
        rTitleRT.sizeDelta = new Vector2(0, 36);
        rTitleRT.anchoredPosition = new Vector2(0, 48);
        resultFoodTitle = rTitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(resultFoodTitle);
        resultFoodTitle.fontSize = 13;
        resultFoodTitle.alignment = TextAlignmentOptions.Center;

        // Result Desc / Buffs
        GameObject rDescGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        rDescGO.transform.SetParent(resultBannerGO.transform, false);
        RectTransform rDescRT = rDescGO.GetComponent<RectTransform>();
        rDescRT.anchorMin = new Vector2(0, 0);
        rDescRT.anchorMax = new Vector2(1, 0);
        rDescRT.sizeDelta = new Vector2(0, 32);
        rDescRT.anchoredPosition = new Vector2(0, 16);
        resultFoodDesc = rDescGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(resultFoodDesc);
        resultFoodDesc.fontSize = 11;
        resultFoodDesc.alignment = TextAlignmentOptions.Center;

        resultBannerGO.SetActive(false);

        // RIGHT COLUMN: INGREDIENTS & RECIPE NOTEBOOK
        GameObject rightCol = new GameObject("RightCol", typeof(RectTransform), typeof(Image));
        rightCol.transform.SetParent(windowGO.transform, false);
        RectTransform rightRT = rightCol.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.52f, 0);
        rightRT.anchorMax = new Vector2(1f, 1);
        rightRT.offsetMin = new Vector2(8, 44);
        rightRT.offsetMax = new Vector2(-16, -76);
        Image rightBg = rightCol.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(rightBg, DungeonUIAssetHelper.PanelStyle.Slate);

        // Tab Switch Buttons
        GameObject tabRowGO = new GameObject("TabRow", typeof(RectTransform));
        tabRowGO.transform.SetParent(rightCol.transform, false);
        RectTransform tabRowRT = tabRowGO.GetComponent<RectTransform>();
        tabRowRT.anchorMin = new Vector2(0, 1);
        tabRowRT.anchorMax = new Vector2(1, 1);
        tabRowRT.pivot = new Vector2(0.5f, 1);
        tabRowRT.sizeDelta = new Vector2(0, 36);
        tabRowRT.anchoredPosition = new Vector2(0, -6);

        // Tab 1: Nguyên liệu
        GameObject tab1GO = new GameObject("TabIngredients", typeof(RectTransform), typeof(Image), typeof(Button));
        tab1GO.transform.SetParent(tabRowGO.transform, false);
        RectTransform tab1RT = tab1GO.GetComponent<RectTransform>();
        tab1RT.anchorMin = new Vector2(0, 0);
        tab1RT.anchorMax = new Vector2(0.5f, 1);
        tab1RT.offsetMin = new Vector2(6, 2);
        tab1RT.offsetMax = new Vector2(-4, -2);
        Image tab1Img = tab1GO.GetComponent<Image>();
        tabIngredientsBtn = tab1GO.GetComponent<Button>();
        tabIngredientsBtn.onClick.AddListener(() =>
        {
            currentView = RightTabView.Ingredients;
            if (rightScrollRect != null) rightScrollRect.verticalNormalizedPosition = 1f;
            RefreshUI();
        });

        GameObject tab1TxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        tab1TxtGO.transform.SetParent(tab1GO.transform, false);
        RectTransform tab1TxtRT = tab1TxtGO.GetComponent<RectTransform>();
        tab1TxtRT.anchorMin = Vector2.zero;
        tab1TxtRT.anchorMax = Vector2.one;
        tab1TxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI t1Txt = tab1TxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(t1Txt);
        t1Txt.fontSize = 12;
        t1Txt.fontStyle = FontStyles.Bold;
        t1Txt.alignment = TextAlignmentOptions.Center;
        t1Txt.color = Color.white;
        t1Txt.text = "NGUYÊN LIỆU (BALO)";

        // Tab 2: Sổ tay
        GameObject tab2GO = new GameObject("TabNotebook", typeof(RectTransform), typeof(Image), typeof(Button));
        tab2GO.transform.SetParent(tabRowGO.transform, false);
        RectTransform tab2RT = tab2GO.GetComponent<RectTransform>();
        tab2RT.anchorMin = new Vector2(0.5f, 0);
        tab2RT.anchorMax = new Vector2(1f, 1);
        tab2RT.offsetMin = new Vector2(4, 2);
        tab2RT.offsetMax = new Vector2(-6, -2);
        Image tab2Img = tab2GO.GetComponent<Image>();
        tabNotebookBtn = tab2GO.GetComponent<Button>();
        tabNotebookBtn.onClick.AddListener(() =>
        {
            currentView = RightTabView.RecipeBook;
            if (rightScrollRect != null) rightScrollRect.verticalNormalizedPosition = 1f;
            RefreshUI();
        });

        GameObject tab2TxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        tab2TxtGO.transform.SetParent(tab2GO.transform, false);
        RectTransform tab2TxtRT = tab2TxtGO.GetComponent<RectTransform>();
        tab2TxtRT.anchorMin = Vector2.zero;
        tab2TxtRT.anchorMax = Vector2.one;
        tab2TxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI t2Txt = tab2TxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(t2Txt);
        t2Txt.fontSize = 12;
        t2Txt.fontStyle = FontStyles.Bold;
        t2Txt.alignment = TextAlignmentOptions.Center;
        t2Txt.color = Color.white;
        t2Txt.text = "SỔ TAY CÔNG THỨC";

        // Scroll Area for Right Column
        GameObject scrollGO = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGO.transform.SetParent(rightCol.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(8, 8);
        scrollRT.offsetMax = new Vector2(-8, -48);
        Image scrollBg = scrollGO.GetComponent<Image>();
        scrollBg.color = new Color(0.06f, 0.08f, 0.11f, 0.5f);
        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        rightScrollRect = sr;

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(4, 4);
        vpRT.offsetMax = new Vector2(-16, -4);

        // Content: Ingredients
        GameObject ingContentGO = new GameObject("IngredientsContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        ingContentGO.transform.SetParent(viewportGO.transform, false);
        inventoryGridContainer = ingContentGO.transform;
        RectTransform igcRT = ingContentGO.GetComponent<RectTransform>();
        igcRT.anchorMin = new Vector2(0, 1);
        igcRT.anchorMax = new Vector2(1, 1);
        igcRT.pivot = new Vector2(0.5f, 1);
        igcRT.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup vlg1 = ingContentGO.GetComponent<VerticalLayoutGroup>();
        vlg1.spacing = 6;
        vlg1.padding = new RectOffset(6, 6, 6, 6);
        vlg1.childControlWidth = true;
        vlg1.childControlHeight = false;
        vlg1.childForceExpandWidth = true;
        ContentSizeFitter csf1 = ingContentGO.GetComponent<ContentSizeFitter>();
        csf1.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Content: Notebook
        GameObject nbContentGO = new GameObject("NotebookContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        nbContentGO.transform.SetParent(viewportGO.transform, false);
        notebookContainer = nbContentGO.transform;
        RectTransform nbcRT = nbContentGO.GetComponent<RectTransform>();
        nbcRT.anchorMin = new Vector2(0, 1);
        nbcRT.anchorMax = new Vector2(1, 1);
        nbcRT.pivot = new Vector2(0.5f, 1);
        nbcRT.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup vlg2 = nbContentGO.GetComponent<VerticalLayoutGroup>();
        vlg2.spacing = 6;
        vlg2.padding = new RectOffset(6, 6, 6, 6);
        vlg2.childControlWidth = true;
        vlg2.childControlHeight = false;
        vlg2.childForceExpandWidth = true;
        ContentSizeFitter csf2 = nbContentGO.GetComponent<ContentSizeFitter>();
        csf2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Vertical Scrollbar / Slider
        GameObject sbGO = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGO.transform.SetParent(scrollGO.transform, false);
        RectTransform sbRT = sbGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(10, -8);
        sbRT.anchoredPosition = new Vector2(-4, 0);
        Image sbBg = sbGO.GetComponent<Image>();
        sbBg.color = new Color(0.04f, 0.06f, 0.08f, 0.7f);

        Scrollbar sb = sbGO.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        GameObject handleArea = new GameObject("Sliding Area", typeof(RectTransform));
        handleArea.transform.SetParent(sbGO.transform, false);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRT = handle.GetComponent<RectTransform>();
        hRT.sizeDelta = Vector2.zero;
        Image handleImg = handle.GetComponent<Image>();
        handleImg.color = new Color(0.95f, 0.75f, 0.25f, 0.9f);

        sb.targetGraphic = handleImg;
        sb.handleRect = hRT;

        sr.viewport = vpRT;
        sr.content = igcRT;
        sr.verticalScrollbar = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // 5. Toast text bar at bottom
        GameObject toastGO = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastGO.transform.SetParent(windowGO.transform, false);
        RectTransform toastRT = toastGO.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0, 0);
        toastRT.anchorMax = new Vector2(1, 0);
        toastRT.pivot = new Vector2(0.5f, 0);
        toastRT.sizeDelta = new Vector2(0, 38);
        toastRT.anchoredPosition = Vector2.zero;
        feedbackToastText = toastGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(feedbackToastText);
        feedbackToastText.fontSize = 13;
        feedbackToastText.fontStyle = FontStyles.Bold;
        feedbackToastText.alignment = TextAlignmentOptions.Center;
        feedbackToastText.color = new Color(0.9f, 0.95f, 1f);
        feedbackToastText.gameObject.SetActive(false);
    }
    #endregion
}
