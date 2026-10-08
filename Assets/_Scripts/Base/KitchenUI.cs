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
    public static KitchenUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<KitchenUI>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    Canvas c = FindFirstObjectByType<Canvas>();
                    if (c != null)
                    {
                        GameObject go = new GameObject("KitchenUI");
                        go.transform.SetParent(c.transform, false);
                        _instance = go.AddComponent<KitchenUI>();
                    }
                }
            }
            return _instance;
        }
        private set => _instance = value;
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

    // Runtime Cooking Pot Data (Max 4 slots)
    private const int MAX_POT_SLOTS = 4;
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

        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
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

    private void OnDestroy()
    {
        UnsubscribeContainerEvents();
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

        isInternalUpdating = true;
        try
        {
            // 1. Kiểm tra xem loại item này đã có trong nồi chưa
            ItemSlot existingSlot = null;
            for (int i = 0; i < potSlots.Count; i++)
            {
                if (ItemContainer.IsItemMatch(potSlots[i].itemData, item))
                {
                    existingSlot = potSlots[i];
                    break;
                }
            }

            if (existingSlot != null)
            {
                backpackContainer.RemoveItem(item, 1);
                existingSlot.amount += 1;
            }
            else
            {
                if (potSlots.Count >= MAX_POT_SLOTS)
                {
                    ShowToast($"Nồi nấu đã đầy tối đa {MAX_POT_SLOTS} loại nguyên liệu!");
                    return;
                }

                backpackContainer.RemoveItem(item, 1);
                potSlots.Add(new ItemSlot { itemData = item, amount = 1 });
            }
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
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                backpackContainer.AddItem(slot.itemData, 1);
                slot.amount -= 1;
                if (slot.amount <= 0)
                {
                    potSlots.RemoveAt(slotIndex);
                }
            }
        }
        finally
        {
            isInternalUpdating = false;
        }

        if (resultBannerGO != null) resultBannerGO.SetActive(false);
        RefreshUI();
    }

    public void ReturnAllPotItemsToBackpack()
    {
        if (backpackContainer == null || potSlots.Count == 0) return;

        isInternalUpdating = true;
        try
        {
            for (int i = 0; i < potSlots.Count; i++)
            {
                ItemSlot slot = potSlots[i];
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    backpackContainer.AddItem(slot.itemData, slot.amount);
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
            ShowToast($"Nấu thành công món: <color=#88FF88>{matchedRecipe.recipeName}</color>!");
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
        rt.sizeDelta = new Vector2(66, 66);

        Image bg = slotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(bg, slot != null ? new Color(0.22f, 0.28f, 0.38f, 1f) : new Color(0.12f, 0.14f, 0.18f, 0.8f));

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

            // Amount badge
            GameObject amtGO = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
            amtGO.transform.SetParent(slotGO.transform, false);
            RectTransform amtRT = amtGO.GetComponent<RectTransform>();
            amtRT.anchorMin = new Vector2(1, 0);
            amtRT.anchorMax = new Vector2(1, 0);
            amtRT.pivot = new Vector2(1, 0);
            amtRT.anchoredPosition = new Vector2(-2, 2);
            amtRT.sizeDelta = new Vector2(30, 20);

            TextMeshProUGUI amtTxt = amtGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(amtTxt);
            amtTxt.fontSize = 13;
            amtTxt.fontStyle = FontStyles.Bold;
            amtTxt.alignment = TextAlignmentOptions.BottomRight;
            amtTxt.color = Color.yellow;
            amtTxt.text = $"x{slot.amount}";
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
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.14f, 0.16f, 0.22f, 0.95f));

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
        infoTxt.fontSize = 12;
        infoTxt.alignment = TextAlignmentOptions.MidlineLeft;
        infoTxt.color = Color.white;
        infoTxt.text = $"<b>{item.itemName}</b>\n<size=10><color=#AAAAAA>Có sẵn: <color=#88FF88>{count}</color></color></size>";

        // Add Button Badge
        GameObject addBadge = new GameObject("AddBadge", typeof(RectTransform), typeof(Image));
        addBadge.transform.SetParent(card.transform, false);
        RectTransform badgeRT = addBadge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(1, 0.5f);
        badgeRT.anchorMax = new Vector2(1, 0.5f);
        badgeRT.anchoredPosition = new Vector2(-36, 0);
        badgeRT.sizeDelta = new Vector2(58, 28);
        Image badgeImg = addBadge.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(badgeImg, new Color(0.2f, 0.5f, 0.25f, 0.9f));

        GameObject addTxtGO = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        addTxtGO.transform.SetParent(addBadge.transform, false);
        RectTransform addTxtRT = addTxtGO.GetComponent<RectTransform>();
        addTxtRT.anchorMin = Vector2.zero;
        addTxtRT.anchorMax = Vector2.one;
        addTxtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI addTxt = addTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(addTxt);
        addTxt.fontSize = 11;
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
        infoTxt.fontSize = 11;
        infoTxt.alignment = TextAlignmentOptions.MidlineLeft;

        if (isDiscovered)
        {
            System.Text.StringBuilder ingSb = new System.Text.StringBuilder();
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Length; i++)
                {
                    ItemRequirement req = recipe.ingredients[i];
                    if (req.item != null)
                    {
                        ingSb.Append($"{req.item.itemName} x{req.amount}  ");
                    }
                }
            }
            infoTxt.text = $"<b><color=#FFD700>{recipe.recipeName}</color></b>\n<size=10><color=#88FF88>Công thức:</color> {ingSb.ToString().TrimEnd()}</size>";
        }
        else
        {
            infoTxt.text = "<b><color=#777777>??? Món ăn bí mật ???</color></b>\n<size=10><color=#555555>Chưa khám phá - Hãy thử đoán mò các nguyên liệu!</color></size>";
        }

        return card;
    }

    private void ShowCookResult(bool success, RecipeData recipe, bool isNewDiscovery)
    {
        if (resultBannerGO == null) return;
        resultBannerGO.SetActive(true);

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
            DungeonUIAssetHelper.StyleButton(tabIngredientsBtn, img, currentView == RightTabView.Ingredients ? new Color(0.2f, 0.45f, 0.7f, 1f) : new Color(0.16f, 0.2f, 0.28f, 0.8f));
        }
        if (tabNotebookBtn != null)
        {
            Image img = tabNotebookBtn.GetComponent<Image>();
            DungeonUIAssetHelper.StyleButton(tabNotebookBtn, img, currentView == RightTabView.RecipeBook ? new Color(0.2f, 0.45f, 0.7f, 1f) : new Color(0.16f, 0.2f, 0.28f, 0.8f));
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
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. Root Panel
        kitchenPanel = new GameObject("KitchenPanel", typeof(RectTransform), typeof(Image));
        kitchenPanel.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = kitchenPanel.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = new Vector2(760, 520);
        rootRT.anchoredPosition = Vector2.zero;

        Image rootBg = kitchenPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rootBg, new Color(0.08f, 0.10f, 0.14f, 0.98f));

        // 2. Header
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(kitchenPanel.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 68);
        headerRT.anchoredPosition = Vector2.zero;
        Image headerBg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerBg, new Color(0.14f, 0.18f, 0.25f, 0.95f));

        // Title
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(25, 18);
        titleRT.offsetMax = new Vector2(-60, 0);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 17;
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
        subtitleText.fontSize = 11;
        subtitleText.text = "Bỏ nguyên liệu từ Balo vào Nồi để đoán mò công thức bí mật!";

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
        DungeonUIAssetHelper.StyleButton(closeButton, closeImg, new Color(0.6f, 0.2f, 0.2f, 0.9f));
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

        // 3. Body Layout: Left Side (Pot) & Right Side (Ingredients / Notebook)
        // LEFT COLUMN: COOKING POT
        GameObject leftCol = new GameObject("LeftPotCol", typeof(RectTransform), typeof(Image));
        leftCol.transform.SetParent(kitchenPanel.transform, false);
        RectTransform leftRT = leftCol.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0);
        leftRT.anchorMax = new Vector2(0.52f, 1);
        leftRT.offsetMin = new Vector2(16, 44);
        leftRT.offsetMax = new Vector2(-8, -76);
        Image leftBg = leftCol.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(leftBg, new Color(0.11f, 0.13f, 0.18f, 0.9f));

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
        plTxt.text = "🔥 NỒI NẤU ĐANG ĐUN (4 Ô NGUYÊN LIỆU)";

        // Pot Slots Row
        GameObject potRowGO = new GameObject("PotSlotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        potRowGO.transform.SetParent(leftCol.transform, false);
        potSlotsRow = potRowGO.transform;
        RectTransform prRT = potRowGO.GetComponent<RectTransform>();
        prRT.anchorMin = new Vector2(0.5f, 1);
        prRT.anchorMax = new Vector2(0.5f, 1);
        prRT.pivot = new Vector2(0.5f, 1);
        prRT.sizeDelta = new Vector2(320, 74);
        prRT.anchoredPosition = new Vector2(0, -48);
        HorizontalLayoutGroup potHlg = potRowGO.GetComponent<HorizontalLayoutGroup>();
        potHlg.spacing = 10;
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
        DungeonUIAssetHelper.StyleButton(cookButton, cookBtnImg, new Color(0.85f, 0.45f, 0.12f, 1f));
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
        cookTxt.text = "🍳 BẬT LỬA NẤU";

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
        DungeonUIAssetHelper.StyleButton(clearPotButton, clearBtnImg, new Color(0.35f, 0.25f, 0.28f, 0.9f));
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
        DungeonUIAssetHelper.StyleSlicedFrame(rbImg, new Color(0.16f, 0.20f, 0.28f, 0.95f));

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
        rightCol.transform.SetParent(kitchenPanel.transform, false);
        RectTransform rightRT = rightCol.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.52f, 0);
        rightRT.anchorMax = new Vector2(1f, 1);
        rightRT.offsetMin = new Vector2(8, 44);
        rightRT.offsetMax = new Vector2(-16, -76);
        Image rightBg = rightCol.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(rightBg, new Color(0.11f, 0.13f, 0.18f, 0.9f));

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
        t1Txt.fontSize = 11;
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
        t2Txt.fontSize = 11;
        t2Txt.fontStyle = FontStyles.Bold;
        t2Txt.alignment = TextAlignmentOptions.Center;
        t2Txt.color = Color.white;
        t2Txt.text = "📖 SỔ TAY CÔNG THỨC";

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

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        Image vpImg = viewportGO.GetComponent<Image>();
        vpImg.color = Color.white;
        Mask mask = viewportGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

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

        sr.viewport = vpRT;
        sr.content = igcRT;

        // 4. Toast text bar at bottom
        GameObject toastGO = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastGO.transform.SetParent(kitchenPanel.transform, false);
        RectTransform toastRT = toastGO.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0, 0);
        toastRT.anchorMax = new Vector2(1, 0);
        toastRT.pivot = new Vector2(0.5f, 0);
        toastRT.sizeDelta = new Vector2(0, 38);
        toastRT.anchoredPosition = Vector2.zero;
        feedbackToastText = toastGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(feedbackToastText);
        feedbackToastText.fontSize = 12;
        feedbackToastText.alignment = TextAlignmentOptions.Center;
        feedbackToastText.color = new Color(0.9f, 0.95f, 1f);
        feedbackToastText.gameObject.SetActive(false);
    }
    #endregion
}
