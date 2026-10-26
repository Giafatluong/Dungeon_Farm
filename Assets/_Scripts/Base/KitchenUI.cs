using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Redesigned Kitchen UI (Bếp Nấu Gia Đình & Sổ Tay Công Thức Ẩm Thực).
/// Uses serialized Canvas UI hierarchy in the scene/prefab instead of procedural runtime generation.
/// Divided into 3 columns:
/// 1. Sổ Tay Công Thức (Recipe Notebook & Filter Tabs)
/// 2. Nồi Nấu Bếp Lửa (Cooking Pot 4 slots, Cooking Progress Bar, Dish Output Preview, Cook/Cancel actions)
/// 3. Balo Nguyên Liệu (Backpack Inventory Grid & Quick Sort/Clear)
/// </summary>
public class KitchenUI : MonoBehaviour
{
    #region Singleton & Statics
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

        // Try find under Canvas in scene
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null) continue;
            KitchenUI found = canvases[i].GetComponentInChildren<KitchenUI>(true);
            if (found != null)
            {
                _instance = found;
                return _instance;
            }
        }

        return null;
    }
    #endregion

    #region Serialized UI References
    [Header("Root Canvas & Panels")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject kitchenPanel; // Fullscreen root panel or overlay
    [SerializeField] private RectTransform windowRect;

    [Header("Top Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    [Header("Left Column: Sổ Tay Công Thức (Recipes)")]
    [SerializeField] private Button tabAllBtn;
    [SerializeField] private Button tabRecoveryBtn;
    [SerializeField] private Button tabSpeedBtn;
    [SerializeField] private Button tabOtherBtn;
    [SerializeField] private TextMeshProUGUI tabAllText;
    [SerializeField] private TextMeshProUGUI tabRecoveryText;
    [SerializeField] private TextMeshProUGUI tabSpeedText;
    [SerializeField] private TextMeshProUGUI tabOtherText;
    [SerializeField] private ScrollRect recipeScrollRect;
    [SerializeField] private Transform recipeContent;
    [SerializeField] private GameObject recipeCardTemplate;

    [Header("Middle Column: Nồi Nấu Bếp Lửa (Stove)")]
    [SerializeField] private TextMeshProUGUI stoveHeaderTag;
    [SerializeField] private Image stovePotIcon;
    [SerializeField] private Image stoveFireIcon;
    [SerializeField] private TextMeshProUGUI stoveSubtitleText;

    [Header("Stove: 4 Pot Slots")]
    [SerializeField] private PotSlotUI[] potSlotUIs = new PotSlotUI[4];

    [Header("Stove: Cooking Progress")]
    [SerializeField] private TextMeshProUGUI progressLabelText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TextMeshProUGUI progressStatusText;

    [Header("Stove: Output Preview Card")]
    [SerializeField] private GameObject previewCardGO;
    [SerializeField] private Image previewFoodIcon;
    [SerializeField] private TextMeshProUGUI previewHeaderLabel;
    [SerializeField] private TextMeshProUGUI previewFoodTitle;
    [SerializeField] private TextMeshProUGUI previewFoodStats;

    [Header("Stove: Action Buttons")]
    [SerializeField] private Button cookButton;
    [SerializeField] private Button cancelPotButton;
    [SerializeField] private TextMeshProUGUI cookButtonText;

    [Header("Right Column: Balo Nguyên Liệu (Backpack)")]
    [SerializeField] private TextMeshProUGUI backpackHeaderTag;
    [SerializeField] private TextMeshProUGUI backpackSubtitleText;
    [SerializeField] private ScrollRect backpackScrollRect;
    [SerializeField] private Transform backpackGridContent;
    [SerializeField] private GameObject backpackSlotPrefab;
    [SerializeField] private Button sortBackpackButton;
    [SerializeField] private Button clearSlotsButton;

    [Header("Feedback Toast")]
    [SerializeField] private GameObject toastObject;
    [SerializeField] private TextMeshProUGUI feedbackToastText;
    #endregion

    #region Nested Classes & Enums
    [System.Serializable]
    public class PotSlotUI
    {
        public GameObject rootObject;
        public Button slotButton;
        public Image slotBackground;
        public Image itemIcon;
        public TextMeshProUGUI countText;
        public GameObject highlightBorder;
    }

    public enum RecipeFilter
    {
        All,
        Recovery,
        Speed,
        Other
    }
    #endregion

    #region Runtime Data & State
    public const int MAX_POT_SLOTS = 4;
    private readonly List<ItemSlot> potSlots = new List<ItemSlot>();

    private ItemContainer backpackContainer;
    private ItemContainer chestContainer;
    private KitchenStation currentStation;

    private RecipeFilter currentFilter = RecipeFilter.All;
    private readonly List<GameObject> activeRecipeCards = new List<GameObject>();
    private readonly List<GameObject> activeBackpackSlots = new List<GameObject>();

    private float toastTimer = 0f;
    private bool isExplicitlyOpened = false;
    private bool isInternalUpdating = false;
    private bool isCooking = false;
    private Coroutine cookCoroutine;

    public bool IsOpen => isExplicitlyOpened && kitchenPanel != null && kitchenPanel.activeSelf;
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

        if (!isExplicitlyOpened && kitchenPanel != null)
        {
            kitchenPanel.SetActive(false);
        }
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
            if (toastTimer <= 0f)
            {
                if (toastObject != null) toastObject.SetActive(false);
                else if (feedbackToastText != null) feedbackToastText.gameObject.SetActive(false);
            }
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            Close();
        }
    }

    private void OnDisable()
    {
        if (isCooking)
        {
            StopCookingCoroutine();
        }
        ReturnAllPotItemsToBackpack();
        UnsubscribeContainerEvents();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Public Interface
    public void Open(KitchenStation station, ItemContainer backpack, ItemContainer chest)
    {
        isExplicitlyOpened = true;
        currentStation = station;
        backpackContainer = backpack;
        chestContainer = chest;

        InventoryController.Instance?.CloseInventoryPanel();

        if (kitchenPanel == null)
        {
            AutoBindReferences();
        }

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }

        if (kitchenPanel != null)
        {
            kitchenPanel.SetActive(true);
            kitchenPanel.transform.SetAsLastSibling();
        }

        SubscribeContainerEvents();

        isCooking = false;
        if (cookCoroutine != null) StopCoroutine(cookCoroutine);

        if (progressSlider != null) progressSlider.value = 0f;
        if (progressStatusText != null) progressStatusText.text = "SẴN SÀNG";
        if (stoveSubtitleText != null) stoveSubtitleText.text = "Bếp Lửa Sôi Sùng Sục • Sẵn Sàng Chế Biến";

        currentFilter = RecipeFilter.All;
        RefreshUI();

        if (toastObject != null) toastObject.SetActive(false);
        else if (feedbackToastText != null) feedbackToastText.gameObject.SetActive(false);
    }

    public void Close()
    {
        if (isCooking)
        {
            StopCookingCoroutine();
        }

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

    public void ShowToast(string message)
    {
        if (feedbackToastText == null && toastObject == null) return;

        if (feedbackToastText != null)
        {
            feedbackToastText.text = message;
            feedbackToastText.gameObject.SetActive(true);
        }
        if (toastObject != null)
        {
            toastObject.SetActive(true);
        }
        toastTimer = 3.2f;
    }
    #endregion

    #region Container Sync
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
        RefreshBackpackGrid();
        RefreshRecipeNotebook();
    }
    #endregion

    #region Pot Management
    public void AddToPot(ItemData item)
    {
        if (item == null || backpackContainer == null || isCooking) return;

        if (backpackContainer.GetItemCount(item) <= 0)
        {
            ShowToast($"Hết <color=#FFD700>{item.itemName}</color> trong Balo!");
            return;
        }

        // Check if item already exists in one of the pot slots (stack into same slot)
        ItemSlot existingSlot = null;
        for (int i = 0; i < potSlots.Count; i++)
        {
            if (potSlots[i] != null && ItemContainer.IsItemMatch(potSlots[i].itemData, item))
            {
                existingSlot = potSlots[i];
                break;
            }
        }

        if (existingSlot != null)
        {
            isInternalUpdating = true;
            try
            {
                backpackContainer.RemoveItem(item, 1);
                existingSlot.amount += 1;
            }
            finally
            {
                isInternalUpdating = false;
            }
        }
        else
        {
            if (potSlots.Count >= MAX_POT_SLOTS)
            {
                ShowToast($"Nồi nấu đã đầy ({MAX_POT_SLOTS}/{MAX_POT_SLOTS} ô)! Nhấp vào ô nấu để lấy bớt ra.");
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
        }

        RefreshUI();
    }

    public void RemoveFromPot(int slotIndex)
    {
        if (isCooking || slotIndex < 0 || slotIndex >= potSlots.Count || backpackContainer == null) return;

        isInternalUpdating = true;
        try
        {
            ItemSlot slot = potSlots[slotIndex];
            if (slot != null && slot.itemData != null)
            {
                // Return 1 count of item back to backpack
                ReturnItemSafely(slot.itemData, 1);
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

        RefreshUI();
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
            ShowToast($"Balo đầy! Đã chuyển <color=#FFD700>{item.itemName} x{amount}</color> vào Rương.");
            return;
        }

        // Drop on ground
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
            ShowToast($"Balo đầy! <color=#FF8888>{item.itemName}</color> đã rơi xuống đất.");
        }
        else if (backpackContainer != null)
        {
            backpackContainer.AddItem(item, amount);
        }
    }
    #endregion

    #region Auto-Fill Recipe
    public void AutoFillRecipe(RecipeData recipe)
    {
        if (recipe == null || backpackContainer == null || isCooking) return;

        if (!CookingManager.Instance.CanCook(recipe, backpackContainer))
        {
            ShowToast($"Không đủ nguyên liệu để nấu <color=#FFAA00>{recipe.DisplayName}</color>!");
            return;
        }

        // 1. Return current pot items to backpack
        ReturnAllPotItemsToBackpack();

        // 2. Transfer required ingredients from backpack to pot
        isInternalUpdating = true;
        try
        {
            for (int i = 0; i < recipe.ingredients.Length; i++)
            {
                ItemRequirement req = recipe.ingredients[i];
                if (req == null || req.item == null || req.amount <= 0) continue;

                backpackContainer.RemoveItem(req.item, req.amount);
                potSlots.Add(new ItemSlot { itemData = req.item, amount = req.amount });
            }
        }
        finally
        {
            isInternalUpdating = false;
        }

        ShowToast($"Đã tự điền nguyên liệu cho: <color=#88FF88>{recipe.DisplayName}</color>!");
        RefreshUI();
    }
    #endregion

    #region Cooking Execution & Progress
    private void OnCookButtonClicked()
    {
        if (isCooking) return;

        if (potSlots.Count == 0)
        {
            ShowToast("<color=#FFCC00>Nồi nấu đang trống! Vui lòng chọn nguyên liệu từ Balo hoặc Sổ Công Thức.</color>");
            return;
        }

        if (CookingManager.Instance == null || backpackContainer == null) return;

        cookCoroutine = StartCoroutine(CookProcessRoutine());
    }

    private void OnCancelButtonClicked()
    {
        if (isCooking)
        {
            StopCookingCoroutine();
            ReturnAllPotItemsToBackpack();
            ShowToast("<color=#FF8888>Đã hủy tiến trình nấu ăn và thu hồi nguyên liệu.</color>");
            RefreshUI();
        }
        else
        {
            ReturnAllPotItemsToBackpack();
            ShowToast("Đã dọn sạch nồi và trả nguyên liệu về Balo.");
        }
    }

    private void StopCookingCoroutine()
    {
        if (cookCoroutine != null)
        {
            StopCoroutine(cookCoroutine);
            cookCoroutine = null;
        }
        isCooking = false;
        if (progressSlider != null) progressSlider.value = 0f;
        if (progressStatusText != null) progressStatusText.text = "ĐÃ HỦY";
        if (stoveSubtitleText != null) stoveSubtitleText.text = "Bếp Lửa Sôi Sùng Sục • Sẵn Sàng Chế Biến";
        if (cookButtonText != null) cookButtonText.text = "🔥 BẮT ĐẦU NẤU";
    }

    private IEnumerator CookProcessRoutine()
    {
        isCooking = true;
        float cookDuration = 4.0f; // 4 seconds cook time
        float elapsed = 0f;

        if (cookButtonText != null) cookButtonText.text = "🔥 ĐANG NẤU...";
        if (stoveSubtitleText != null) stoveSubtitleText.text = "🔥 Bếp Đang Nấu Sôi Sùng Sục • Chờ Một Chút...";

        while (elapsed < cookDuration)
        {
            elapsed += Time.deltaTime;
            float pct = Mathf.Clamp01(elapsed / cookDuration);

            if (progressSlider != null)
            {
                progressSlider.value = pct;
            }

            if (progressStatusText != null)
            {
                progressStatusText.text = $"🔥 ĐANG NẤU... {Mathf.RoundToInt(pct * 100)}% ({elapsed:0.0}s/{cookDuration:0.0}s)";
            }

            yield return null;
        }

        // Cook finish
        bool success = CookingManager.Instance.TryCookPot(potSlots, backpackContainer, out RecipeData matchedRecipe, out bool isNewDiscovery, out string failReason);

        if (success && matchedRecipe != null)
        {
            potSlots.Clear();
            ShowToast($"Nấu thành công món: <color=#88FF88>{matchedRecipe.DisplayName}</color>!");

            Vector3 kitchenPos = currentStation != null ? currentStation.transform.position : (PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : Vector3.zero);
            VFXManager.Instance?.PlayCookingSparkles(kitchenPos);
        }
        else
        {
            if (failReason != null && failReason.ToLower().Contains("full"))
            {
                ShowToast($"<color=#FFAA00>{failReason}</color>");
                StopCookingCoroutine();
                yield break;
            }

            potSlots.Clear();
            ShowToast("<color=#FF6666>Nấu thất bại! Các nguyên liệu bị cháy khét thành tro.</color>");
        }

        isCooking = false;
        cookCoroutine = null;

        if (progressSlider != null) progressSlider.value = 0f;
        if (progressStatusText != null) progressStatusText.text = success ? "HOÀN THÀNH!" : "THẤT BẠI!";
        if (stoveSubtitleText != null) stoveSubtitleText.text = "Bếp Lửa Sôi Sùng Sục • Sẵn Sàng Chế Biến";
        if (cookButtonText != null) cookButtonText.text = "🔥 BẮT ĐẦU NẤU";

        RefreshUI();
    }
    #endregion

    #region UI Rendering & Refresh
    public void RefreshUI()
    {
        RefreshPotSlots();
        RefreshOutputPreview();
        RefreshRecipeNotebook();
        RefreshBackpackGrid();
        UpdateTabButtonsState();
    }

    private void RefreshPotSlots()
    {
        if (potSlotUIs == null) return;

        for (int i = 0; i < potSlotUIs.Length; i++)
        {
            var slotUI = potSlotUIs[i];
            if (slotUI == null) continue;

            int slotIdx = i;
            if (slotUI.slotButton != null)
            {
                slotUI.slotButton.onClick.RemoveAllListeners();
                slotUI.slotButton.onClick.AddListener(() => RemoveFromPot(slotIdx));
            }

            if (i < potSlots.Count && potSlots[i] != null && potSlots[i].itemData != null)
            {
                var itemSlot = potSlots[i];
                if (slotUI.itemIcon != null)
                {
                    slotUI.itemIcon.gameObject.SetActive(true);
                    slotUI.itemIcon.sprite = itemSlot.itemData.itemIcon;
                }
                if (slotUI.countText != null)
                {
                    slotUI.countText.gameObject.SetActive(true);
                    slotUI.countText.text = itemSlot.amount > 1 ? $"x{itemSlot.amount}" : "x1";
                }
                if (slotUI.highlightBorder != null)
                {
                    slotUI.highlightBorder.SetActive(true);
                }
            }
            else
            {
                if (slotUI.itemIcon != null)
                {
                    slotUI.itemIcon.gameObject.SetActive(false);
                    slotUI.itemIcon.sprite = null;
                }
                if (slotUI.countText != null)
                {
                    slotUI.countText.gameObject.SetActive(false);
                    slotUI.countText.text = "";
                }
                if (slotUI.highlightBorder != null)
                {
                    slotUI.highlightBorder.SetActive(false);
                }
            }
        }
    }

    private void RefreshOutputPreview()
    {
        if (previewCardGO == null) return;

        RecipeData matchedRecipe = CookingManager.Instance != null ? CookingManager.Instance.MatchRecipe(potSlots) : null;

        if (matchedRecipe != null && matchedRecipe.resultFood != null)
        {
            previewCardGO.SetActive(true);
            if (previewFoodIcon != null)
            {
                previewFoodIcon.gameObject.SetActive(true);
                previewFoodIcon.sprite = matchedRecipe.resultFood.itemIcon;
            }
            if (previewHeaderLabel != null) previewHeaderLabel.text = "MÓN THÀNH PHẨM DỰ KIẾN:";
            if (previewFoodTitle != null) previewFoodTitle.text = matchedRecipe.DisplayName;
            if (previewFoodStats != null) previewFoodStats.text = GetFoodStatsDisplay(matchedRecipe.resultFood);
        }
        else if (potSlots.Count > 0)
        {
            previewCardGO.SetActive(true);
            if (previewFoodIcon != null)
            {
                previewFoodIcon.gameObject.SetActive(false);
            }
            if (previewHeaderLabel != null) previewHeaderLabel.text = "MÓN THÀNH PHẨM DỰ KIẾN:";
            if (previewFoodTitle != null) previewFoodTitle.text = "Món Thử Nghiệm (Ẩn số)";
            if (previewFoodStats != null) previewFoodStats.text = "<color=#FFAA00>??? Thử nấu để khám phá công thức bí mật!</color>";
        }
        else
        {
            previewCardGO.SetActive(true);
            if (previewFoodIcon != null) previewFoodIcon.gameObject.SetActive(false);
            if (previewHeaderLabel != null) previewHeaderLabel.text = "MÓN THÀNH PHẨM DỰ KIẾN:";
            if (previewFoodTitle != null) previewFoodTitle.text = "(Chưa có nguyên liệu)";
            if (previewFoodStats != null) previewFoodStats.text = "<color=#8D6E63>Bỏ nguyên liệu từ Balo hoặc chọn Sổ Công Thức để xem trước.</color>";
        }
    }

    private void RefreshRecipeNotebook()
    {
        if (recipeContent == null || recipeCardTemplate == null) return;

        // Clear existing
        for (int i = 0; i < activeRecipeCards.Count; i++)
        {
            if (activeRecipeCards[i] != null) Destroy(activeRecipeCards[i]);
        }
        activeRecipeCards.Clear();

        List<RecipeData> allRecipes = CookingManager.Instance != null
            ? CookingManager.Instance.GetAllAvailableRecipes()
            : new List<RecipeData>();

        // Filter recipes
        List<RecipeData> filtered = new List<RecipeData>();
        for (int i = 0; i < allRecipes.Count; i++)
        {
            RecipeData r = allRecipes[i];
            if (r == null) continue;

            bool matches = currentFilter switch
            {
                RecipeFilter.All => true,
                RecipeFilter.Recovery => r.resultFood != null && (r.resultFood.healthValue > 0 || r.resultFood.hungerValue > 0),
                RecipeFilter.Speed => r.resultFood != null && r.resultFood.foodBuff != null && r.resultFood.foodBuff.Length > 0,
                RecipeFilter.Other => r.resultFood == null || (r.resultFood.healthValue == 0 && r.resultFood.hungerValue == 0 && (r.resultFood.foodBuff == null || r.resultFood.foodBuff.Length == 0)),
                _ => true
            };

            if (matches) filtered.Add(r);
        }

        // Check if currently selected recipe matches pot slots
        RecipeData currentPotRecipe = CookingManager.Instance != null ? CookingManager.Instance.MatchRecipe(potSlots) : null;

        for (int i = 0; i < filtered.Count; i++)
        {
            RecipeData recipe = filtered[i];
            if (recipe == null) continue;

            GameObject cardGO = Instantiate(recipeCardTemplate, recipeContent);
            cardGO.SetActive(true);
            activeRecipeCards.Add(cardGO);

            // Populate card
            RecipeCardUI cardUI = cardGO.GetComponent<RecipeCardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(recipe, backpackContainer, currentPotRecipe == recipe, () => AutoFillRecipe(recipe));
            }
            else
            {
                // Fallback manual binding
                SetupRecipeCardManual(cardGO, recipe, currentPotRecipe == recipe);
            }
        }
    }

    private void SetupRecipeCardManual(GameObject cardGO, RecipeData recipe, bool isCurrentlySelected)
    {
        Image dishIcon = cardGO.transform.Find("IconFrame/DishIcon")?.GetComponent<Image>();
        TextMeshProUGUI title = cardGO.transform.Find("InfoColumn/TitleRow/TitleText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI stars = cardGO.transform.Find("InfoColumn/TitleRow/StarsText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI detail = cardGO.transform.Find("InfoColumn/DetailText")?.GetComponent<TextMeshProUGUI>();
        Button actionBtn = cardGO.transform.Find("ActionButton")?.GetComponent<Button>();
        Image actionBg = actionBtn != null ? actionBtn.GetComponent<Image>() : null;
        TextMeshProUGUI actionText = cardGO.transform.Find("ActionButton/ActionText")?.GetComponent<TextMeshProUGUI>();

        if (dishIcon != null && recipe.resultFood != null)
        {
            dishIcon.sprite = recipe.resultFood.itemIcon;
            dishIcon.gameObject.SetActive(dishIcon.sprite != null);
        }

        if (title != null) title.text = recipe.DisplayName;
        if (stars != null) stars.text = "⭐⭐⭐";

        if (detail != null)
        {
            StringBuilder sb = new StringBuilder();
            if (recipe.resultFood != null)
            {
                if (recipe.resultFood.healthValue > 0) sb.Append($"Hồi {recipe.resultFood.healthValue} HP");
                if (recipe.resultFood.hungerValue > 0)
                {
                    if (sb.Length > 0) sb.Append(" & ");
                    sb.Append($"{recipe.resultFood.hungerValue} Độ No");
                }
            }
            sb.Append(" | Cần: ");
            if (recipe.ingredients != null)
            {
                for (int ing = 0; ing < recipe.ingredients.Length; ing++)
                {
                    var req = recipe.ingredients[ing];
                    if (req != null && req.item != null)
                    {
                        if (ing > 0) sb.Append(" + ");
                        sb.Append($"{req.item.itemName}x{req.amount}");
                    }
                }
            }
            detail.text = sb.ToString();
        }

        bool canCook = CookingManager.Instance != null && CookingManager.Instance.CanCook(recipe, backpackContainer);

        if (actionBtn != null)
        {
            actionBtn.onClick.RemoveAllListeners();
            if (isCurrentlySelected)
            {
                if (actionText != null) actionText.text = "ĐANG CHỌN";
                if (actionBg != null) actionBg.color = new Color(1f, 0.6f, 0f, 1f); // Orange
                actionBtn.interactable = false;
            }
            else if (canCook)
            {
                if (actionText != null) actionText.text = "TỰ ĐIỀN";
                if (actionBg != null) actionBg.color = new Color(0.48f, 0.32f, 0.22f, 1f); // Cocoa brown
                actionBtn.interactable = true;
                actionBtn.onClick.AddListener(() => AutoFillRecipe(recipe));
            }
            else
            {
                if (actionText != null) actionText.text = "THIẾU NL";
                if (actionBg != null) actionBg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f); // Grey
                actionBtn.interactable = false;
            }
        }
    }

    private void RefreshBackpackGrid()
    {
        if (backpackGridContent == null) return;

        for (int i = 0; i < activeBackpackSlots.Count; i++)
        {
            if (activeBackpackSlots[i] != null) Destroy(activeBackpackSlots[i]);
        }
        activeBackpackSlots.Clear();

        if (backpackContainer == null || backpackContainer.itemSlots == null) return;

        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            ItemSlot slot = backpackContainer.itemSlots[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

            GameObject slotGO = null;
            if (backpackSlotPrefab != null)
            {
                slotGO = Instantiate(backpackSlotPrefab, backpackGridContent);
            }
            else
            {
                slotGO = CreateBackpackSlotFallback(backpackGridContent);
            }

            if (slotGO == null) continue;
            slotGO.SetActive(true);
            activeBackpackSlots.Add(slotGO);

            // Bind slot visuals
            Image icon = slotGO.transform.Find("ItemIcon")?.GetComponent<Image>();
            if (icon == null) icon = slotGO.GetComponentInChildren<Image>();
            TextMeshProUGUI amountText = slotGO.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (amountText == null) amountText = slotGO.GetComponentInChildren<TextMeshProUGUI>();

            if (icon != null)
            {
                icon.sprite = slot.itemData.itemIcon;
                icon.color = Color.white;
            }

            if (amountText != null)
            {
                amountText.text = slot.amount > 1 ? $"x{slot.amount}" : "";
            }

            // Click listener
            Button btn = slotGO.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                ItemData itemToAdd = slot.itemData;
                btn.onClick.AddListener(() => AddToPot(itemToAdd));
            }
        }
    }

    private GameObject CreateBackpackSlotFallback(Transform parent)
    {
        GameObject go = new GameObject("BackpackSlot", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.92f, 0.88f, 0.80f, 1f);

        GameObject iconGO = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0f);
        textRT.anchorMax = new Vector2(1f, 0.4f);
        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 13f;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.color = Color.black;

        return go;
    }

    private void SortBackpack()
    {
        if (backpackContainer == null || backpackContainer.itemSlots == null) return;

        // Collect non-empty slots
        List<ItemSlot> validSlots = new List<ItemSlot>();
        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            var s = backpackContainer.itemSlots[i];
            if (s != null && s.itemData != null && s.amount > 0)
            {
                validSlots.Add(s);
            }
        }

        // Sort: Food/Crops first, then by name
        validSlots.Sort((a, b) =>
        {
            bool aIsFood = a.itemData is FoodData;
            bool bIsFood = b.itemData is FoodData;
            if (aIsFood && !bIsFood) return -1;
            if (!aIsFood && bIsFood) return 1;
            return string.Compare(a.itemData.itemName, b.itemData.itemName, System.StringComparison.OrdinalIgnoreCase);
        });

        // Reassign
        isInternalUpdating = true;
        try
        {
            for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
            {
                if (i < validSlots.Count) backpackContainer.itemSlots[i] = validSlots[i];
                else backpackContainer.itemSlots[i] = new ItemSlot();
            }
        }
        finally
        {
            isInternalUpdating = false;
        }

        ShowToast("⚡ Đã sắp xếp lại Balo nguyên liệu gọn gàng!");
        RefreshBackpackGrid();
    }
    #endregion

    #region Tab Controls & Styling
    private void UpdateTabButtonsState()
    {
        SetTabStyle(tabAllBtn, tabAllText, currentFilter == RecipeFilter.All);
        SetTabStyle(tabRecoveryBtn, tabRecoveryText, currentFilter == RecipeFilter.Recovery);
        SetTabStyle(tabSpeedBtn, tabSpeedText, currentFilter == RecipeFilter.Speed);
        SetTabStyle(tabOtherBtn, tabOtherText, currentFilter == RecipeFilter.Other);
    }

    private void SetTabStyle(Button btn, TextMeshProUGUI txt, bool isActive)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = isActive
                ? new Color(0.36f, 0.70f, 0.16f, 1f) // Green active (#5CAE28)
                : new Color(0.92f, 0.86f, 0.78f, 1f); // Cream inactive
        }
        if (txt != null)
        {
            txt.color = isActive ? Color.white : new Color(0.36f, 0.25f, 0.20f, 1f);
        }
    }

    private string GetFoodStatsDisplay(FoodData food)
    {
        if (food == null) return "";
        List<string> parts = new List<string>();
        if (food.healthValue > 0) parts.Add($"<color=#E53935>❤️ +{food.healthValue} HP</color>");
        if (food.hungerValue > 0) parts.Add($"<color=#FB8C00>🍗 +{food.hungerValue} Độ No</color>");
        if (food.foodBuff != null)
        {
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                var fb = food.foodBuff[i];
                if (fb != null && fb.buffs != null)
                {
                    string stat = CombatUI.GetBuffStatLabel(fb.buffs);
                    parts.Add($"<color=#00ACC1>⚡ +{fb.buffValue} {stat}</color>");
                }
            }
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "Món ăn ngon miệng";
    }
    #endregion

    #region AutoBind References
    public void AutoBindReferences()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>(true);
        if (kitchenPanel == null)
        {
            Transform p = transform.Find("KitchenOverlay");
            if (p != null) kitchenPanel = p.gameObject;
            else kitchenPanel = gameObject;
        }

        Transform root = transform;

        // Top Header
        if (titleText == null) titleText = root.Find("KitchenOverlay/KitchenWindow/TopHeaderBar/HeaderTitle")?.GetComponent<TextMeshProUGUI>();
        if (closeButton == null) closeButton = root.Find("KitchenOverlay/KitchenWindow/TopHeaderBar/CloseButton")?.GetComponent<Button>();

        // Left Panel
        Transform left = root.Find("KitchenOverlay/KitchenWindow/LeftPanel_Recipes");
        if (left != null)
        {
            if (tabAllBtn == null) tabAllBtn = left.Find("FilterTabBar/Tab_All")?.GetComponent<Button>();
            if (tabRecoveryBtn == null) tabRecoveryBtn = left.Find("FilterTabBar/Tab_Recovery")?.GetComponent<Button>();
            if (tabSpeedBtn == null) tabSpeedBtn = left.Find("FilterTabBar/Tab_Speed")?.GetComponent<Button>();
            if (tabOtherBtn == null) tabOtherBtn = left.Find("FilterTabBar/Tab_Other")?.GetComponent<Button>();

            if (tabAllText == null) tabAllText = tabAllBtn?.GetComponentInChildren<TextMeshProUGUI>();
            if (tabRecoveryText == null) tabRecoveryText = tabRecoveryBtn?.GetComponentInChildren<TextMeshProUGUI>();
            if (tabSpeedText == null) tabSpeedText = tabSpeedBtn?.GetComponentInChildren<TextMeshProUGUI>();
            if (tabOtherText == null) tabOtherText = tabOtherBtn?.GetComponentInChildren<TextMeshProUGUI>();

            if (recipeScrollRect == null) recipeScrollRect = left.Find("RecipeScrollView")?.GetComponent<ScrollRect>();
            if (recipeContent == null) recipeContent = left.Find("RecipeScrollView/Viewport/Content");
            if (recipeCardTemplate == null)
            {
                Transform t = left.Find("RecipeCardTemplate") ?? left.Find("RecipeScrollView/Viewport/Content/RecipeCardTemplate");
                if (t != null) recipeCardTemplate = t.gameObject;
            }
        }

        // Middle Panel
        Transform mid = root.Find("KitchenOverlay/KitchenWindow/MiddlePanel_Stove");
        if (mid != null)
        {
            if (stoveHeaderTag == null) stoveHeaderTag = mid.Find("HeaderTag/TagText")?.GetComponent<TextMeshProUGUI>();
            if (stovePotIcon == null) stovePotIcon = mid.Find("CookingStage/PotIcon")?.GetComponent<Image>();
            if (stoveFireIcon == null) stoveFireIcon = mid.Find("CookingStage/FireIcon")?.GetComponent<Image>();
            if (stoveSubtitleText == null) stoveSubtitleText = mid.Find("CookingStage/StageSubtitle")?.GetComponent<TextMeshProUGUI>();

            // 4 Pot Slots
            Transform slotsRow = mid.Find("PotSlotsSection/SlotsRow");
            if (slotsRow != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Transform sTrans = slotsRow.Find($"PotSlot_{i}");
                    if (sTrans != null)
                    {
                        if (potSlotUIs[i] == null) potSlotUIs[i] = new PotSlotUI();
                        potSlotUIs[i].rootObject = sTrans.gameObject;
                        potSlotUIs[i].slotButton = sTrans.GetComponent<Button>();
                        potSlotUIs[i].slotBackground = sTrans.GetComponent<Image>();
                        potSlotUIs[i].itemIcon = sTrans.Find("ItemIcon")?.GetComponent<Image>();
                        potSlotUIs[i].countText = sTrans.Find("CountText")?.GetComponent<TextMeshProUGUI>();
                        potSlotUIs[i].highlightBorder = sTrans.Find("HighlightBorder")?.gameObject;
                    }
                }
            }

            // Progress Bar
            if (progressLabelText == null) progressLabelText = mid.Find("ProgressBarSection/ProgressTitle")?.GetComponent<TextMeshProUGUI>();
            if (progressSlider == null) progressSlider = mid.Find("ProgressBarSection/CookingSlider")?.GetComponent<Slider>();
            if (progressFillImage == null) progressFillImage = mid.Find("ProgressBarSection/CookingSlider/Fill Area/Fill")?.GetComponent<Image>();
            if (progressStatusText == null) progressStatusText = mid.Find("ProgressBarSection/CookingSlider/StatusText")?.GetComponent<TextMeshProUGUI>();

            // Output Preview
            Transform prev = mid.Find("OutputPreviewCard");
            if (prev != null)
            {
                previewCardGO = prev.gameObject;
                if (previewFoodIcon == null) previewFoodIcon = prev.Find("PreviewIconFrame/DishIcon")?.GetComponent<Image>();
                if (previewHeaderLabel == null) previewHeaderLabel = prev.Find("InfoBlock/PreviewHeaderLabel")?.GetComponent<TextMeshProUGUI>();
                if (previewFoodTitle == null) previewFoodTitle = prev.Find("InfoBlock/PreviewDishName")?.GetComponent<TextMeshProUGUI>();
                if (previewFoodStats == null) previewFoodStats = prev.Find("InfoBlock/PreviewDishStats")?.GetComponent<TextMeshProUGUI>();
            }

            // Action Buttons
            if (cookButton == null) cookButton = mid.Find("ActionButtonsRow/CookButton")?.GetComponent<Button>();
            if (cancelPotButton == null) cancelPotButton = mid.Find("ActionButtonsRow/CancelButton")?.GetComponent<Button>();
            if (cookButtonText == null && cookButton != null) cookButtonText = cookButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Right Panel
        Transform right = root.Find("KitchenOverlay/KitchenWindow/RightPanel_Backpack");
        if (right != null)
        {
            if (backpackHeaderTag == null) backpackHeaderTag = right.Find("HeaderTag/TagText")?.GetComponent<TextMeshProUGUI>();
            if (backpackSubtitleText == null) backpackSubtitleText = right.Find("BackpackSubtitle")?.GetComponent<TextMeshProUGUI>();
            if (backpackScrollRect == null) backpackScrollRect = right.Find("BackpackScrollView")?.GetComponent<ScrollRect>();
            if (backpackGridContent == null) backpackGridContent = right.Find("BackpackScrollView/Viewport/Content");
            if (sortBackpackButton == null) sortBackpackButton = right.Find("BackpackActionButtonsRow/SortButton")?.GetComponent<Button>();
            if (clearSlotsButton == null) clearSlotsButton = right.Find("BackpackActionButtonsRow/ClearButton")?.GetComponent<Button>();
        }

        // Toast
        if (toastObject == null) toastObject = root.Find("KitchenOverlay/FeedbackToast")?.gameObject;
        if (feedbackToastText == null && toastObject != null) feedbackToastText = toastObject.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void RegisterButtonEvents()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (tabAllBtn != null)
        {
            tabAllBtn.onClick.RemoveAllListeners();
            tabAllBtn.onClick.AddListener(() => { currentFilter = RecipeFilter.All; RefreshUI(); });
        }
        if (tabRecoveryBtn != null)
        {
            tabRecoveryBtn.onClick.RemoveAllListeners();
            tabRecoveryBtn.onClick.AddListener(() => { currentFilter = RecipeFilter.Recovery; RefreshUI(); });
        }
        if (tabSpeedBtn != null)
        {
            tabSpeedBtn.onClick.RemoveAllListeners();
            tabSpeedBtn.onClick.AddListener(() => { currentFilter = RecipeFilter.Speed; RefreshUI(); });
        }
        if (tabOtherBtn != null)
        {
            tabOtherBtn.onClick.RemoveAllListeners();
            tabOtherBtn.onClick.AddListener(() => { currentFilter = RecipeFilter.Other; RefreshUI(); });
        }

        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(OnCookButtonClicked);
        }

        if (cancelPotButton != null)
        {
            cancelPotButton.onClick.RemoveAllListeners();
            cancelPotButton.onClick.AddListener(OnCancelButtonClicked);
        }

        if (sortBackpackButton != null)
        {
            sortBackpackButton.onClick.RemoveAllListeners();
            sortBackpackButton.onClick.AddListener(SortBackpack);
        }

        if (clearSlotsButton != null)
        {
            clearSlotsButton.onClick.RemoveAllListeners();
            clearSlotsButton.onClick.AddListener(ReturnAllPotItemsToBackpack);
        }
    }
    #endregion
}
