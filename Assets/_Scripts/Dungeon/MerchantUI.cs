using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the Wandering Merchant encounter in the Dungeon.
/// Allows players to trade rare recipes, barter seeds, and buy travel rations,
/// while viewing their real-time backpack inventory.
/// Styled using project fantasy pixel art assets (UI_Frame, UI_Slot, inventorySlot).
/// </summary>
public class MerchantUI : MonoBehaviour
{
    private static MerchantUI _instance;
    public static MerchantUI Instance => _instance;

    public static MerchantUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        MerchantUI found = FindFirstObjectByType<MerchantUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        Canvas c = FindFirstObjectByType<Canvas>();
        if (c != null)
        {
            GameObject go = new GameObject("MerchantUI");
            go.transform.SetParent(c.transform, false);
            _instance = go.AddComponent<MerchantUI>();
            return _instance;
        }

        return null;
    }

    [Header("Panels")]
    [SerializeField] private GameObject merchantPanel;
    [SerializeField] private Transform waresContainer;
    [SerializeField] private Transform backpackSlotsParent;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject slotPrefab;

    [Header("Labels & Buttons")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI backpackHeaderTitle;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;

    private MerchantEvent currentMerchant;
    private ItemContainer backpackContainer;
    private System.Action onContinueCallback;

    private readonly List<InventoryButton> backpackSlots = new List<InventoryButton>();

    public bool IsOpen => merchantPanel != null && merchantPanel.activeSelf;

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
        if (merchantPanel != null)
        {
            merchantPanel.SetActive(false);
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
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange += HandleInventoryChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshBackpackSlots();
        BuildWaresList(); // Refresh trade buttons state based on current inventory
        UpdateBackpackHeader();
    }

    private void UpdateBackpackHeader()
    {
        if (backpackHeaderTitle == null || backpackContainer == null) return;
        int occupied = 0;
        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            if (backpackContainer.itemSlots[i] != null && backpackContainer.itemSlots[i].itemData != null)
            {
                occupied++;
            }
        }
        backpackHeaderTitle.text = $"PLAYER BACKPACK <size=11><color=#A0D8EF>({occupied}/{backpackContainer.maxSlots} SLOTS)</color></size>";
    }

    public void Open(MerchantEvent merchant, ItemContainer backpack, System.Action onContinue)
    {
        if (merchant == null)
        {
            merchant = FindFirstObjectByType<MerchantEvent>(FindObjectsInactive.Include);
        }

        currentMerchant = merchant;
        backpackContainer = backpack;
        onContinueCallback = onContinue;

        EnsureUIBuilt();

        if (merchantPanel != null)
        {
            merchantPanel.SetActive(true);
            merchantPanel.transform.SetAsLastSibling();
        }

        SubscribeEvents();

        BuildWaresList();
        BuildBackpackSlots(backpackContainer);
        bool isMerchantActive = currentMerchant == null || (currentMerchant.merchantActive && currentMerchant.remainingDays > 0);
        if (isMerchantActive)
        {
            int daysLeft = currentMerchant != null ? currentMerchant.remainingDays : 5;
            if (subtitleText != null)
            {
                subtitleText.text = $"Traveling Merchant Camp | Remaining stay: {daysLeft} days";
                subtitleText.color = new Color(0.78f, 0.82f, 0.92f);
            }
            SetFeedback("Welcome, brave traveler! Browse my rare recipes, barter seeds, or stock up on hot travel rations.");
        }
        else
        {
            if (subtitleText != null)
            {
                subtitleText.text = "<color=#FF7777>Merchant has departed! Trade counter is temporarily closed.</color>";
            }
            SetFeedback("The merchant packed up and departed to a new land. You can rest for a moment then continue your journey.");
        }
    }

    public void Close()
    {
        UnsubscribeEvents();

        if (merchantPanel != null)
        {
            merchantPanel.SetActive(false);
        }

        onContinueCallback?.Invoke();
        onContinueCallback = null;
    }

    public void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void BuildWaresList()
    {
        if (waresContainer == null || currentMerchant == null) return;

        foreach (Transform child in waresContainer)
        {
            Destroy(child.gameObject);
        }

        bool isMerchantActive = currentMerchant == null || (currentMerchant.merchantActive && currentMerchant.remainingDays > 0);
        if (!isMerchantActive)
        {
            CreateSectionHeader("REST STOP - MERCHANT DEPARTED", new Color(0.6f, 0.8f, 1f));
            CreateDepartedRestCard();
            return;
        }

        // 1. Rare Cooking Recipe Section
        CreateSectionHeader("EXOTIC COOKING RECIPE", new Color(1f, 0.82f, 0.35f));
        if (currentMerchant.rewardRecipe != null)
        {
            CreateRecipeTradeCard(currentMerchant.rewardRecipe, currentMerchant.requiredItems);
        }
        else
        {
            CreateAllRecipesUnlockedCard();
        }

        // 2. Seed Barter Section
        if (currentMerchant.seedTrades != null && currentMerchant.seedTrades.Length > 0)
        {
            CreateSectionHeader("SEED BARTER EXCHANGE", new Color(0.45f, 0.9f, 0.55f));
            for (int i = 0; i < currentMerchant.seedTrades.Length; i++)
            {
                SeedTrade trade = currentMerchant.seedTrades[i];
                if (trade != null)
                {
                    CreateSeedTradeCard(trade);
                }
            }
        }

        // 3. Survival Rations Section
        if (currentMerchant.rationTrades != null && currentMerchant.rationTrades.Length > 0)
        {
            CreateSectionHeader("TRAVEL RATIONS & SUPPLIES", new Color(0.45f, 0.8f, 1f));
            for (int i = 0; i < currentMerchant.rationTrades.Length; i++)
            {
                RationTrade trade = currentMerchant.rationTrades[i];
                if (trade != null)
                {
                    CreateRationTradeCard(trade);
                }
            }
        }
    }

    private void CreateDepartedRestCard()
    {
        GameObject cardGO = new GameObject("DepartedRestCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(490, 140);

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.12f, 0.16f, 0.22f, 0.98f));

        VerticalLayoutGroup vlg = cardGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Info message
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(cardGO.transform, false);
        TextMeshProUGUI textTMP = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(textTMP);
        textTMP.fontSize = 12;
        textTMP.lineSpacing = 4;
        textTMP.color = new Color(0.85f, 0.9f, 0.95f);
        textTMP.text = "The merchant caravan packed up and departed after their dungeon stay ended.\nThey left some warm coals and pure spring water for passing travelers.";

        // Rest / Continue Button
        GameObject btnGO = new GameObject("BtnRest", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(458, 40);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, new Color(0.2f, 0.45f, 0.35f, 1f));

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
            if (ps != null)
            {
                ps.currentHealth = Mathf.Min(ps.maxHealth, ps.currentHealth + 20);
                ps.currentHunger = Mathf.Min(ps.maxHunger, ps.currentHunger + 10);
            }
            Close();
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTMP);
        btnTMP.fontSize = 13;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.text = "[Rest & Recover 20 HP -> Continue]";
        btnTMP.color = Color.white;
        btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(458, 40);
    }

    private void CreateSectionHeader(string title, Color color)
    {
        GameObject headerGO = new GameObject("SectionHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerGO.transform.SetParent(waresContainer, false);
        RectTransform rt = headerGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(490, 24);

        TextMeshProUGUI tmp = headerGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 12;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.text = $"[ {title} ]";
    }

    private void CreateAllRecipesUnlockedCard()
    {
        GameObject cardGO = new GameObject("AllRecipesUnlockedCard", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(490, 50);

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.12f, 0.18f, 0.15f, 0.95f));

        HorizontalLayoutGroup hlg = cardGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 14, 8, 8);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(cardGO.transform, false);
        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.45f, 0.95f, 0.65f);
        tmp.text = "<b>ALL RECIPES UNLOCKED!</b> (Mastered all cooking techniques)";
    }

    private void CreateRecipeTradeCard(RecipeData recipe, ItemRequirement[] requiredItems)
    {
        bool isAlreadyUnlocked = ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedRecipes.Contains(recipe);
        bool canAfford = currentMerchant.CanTrade(backpackContainer);

        GameObject cardGO = new GameObject("RecipeCard", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(490, 72);

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, isAlreadyUnlocked ? new Color(0.12f, 0.20f, 0.16f, 0.98f) : new Color(0.14f, 0.16f, 0.22f, 0.98f));

        HorizontalLayoutGroup hlg = cardGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 8, 8);
        hlg.spacing = 10;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Result Food Slot Box
        GameObject slotBoxGO = new GameObject("SlotBox", typeof(RectTransform), typeof(Image));
        slotBoxGO.transform.SetParent(cardGO.transform, false);
        slotBoxGO.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 54);
        Image slotBoxImg = slotBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(slotBoxImg, new Color(0.25f, 0.28f, 0.38f, 1f));

        // Result Food Icon inside Slot Box
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotBoxGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = Vector2.zero;
        iconRT.sizeDelta = new Vector2(40, 40);

        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        if (recipe.resultFood != null && recipe.resultFood.itemIcon != null)
        {
            iconImg.sprite = recipe.resultFood.itemIcon;
        }

        // Recipe Info & Required Items
        GameObject infoGO = new GameObject("Info", typeof(RectTransform), typeof(VerticalLayoutGroup));
        infoGO.transform.SetParent(cardGO.transform, false);
        infoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(265, 54);
        VerticalLayoutGroup vlg = infoGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(infoGO.transform, false);
        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleTMP);
        titleTMP.fontSize = 13;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.text = recipe.recipeName;
        titleTMP.color = new Color(1f, 0.88f, 0.4f);

        // Required text
        System.Text.StringBuilder reqSb = new System.Text.StringBuilder("Cost: ");
        if (requiredItems != null)
        {
            for (int i = 0; i < requiredItems.Length; i++)
            {
                ItemRequirement req = requiredItems[i];
                if (req.item == null) continue;
                int have = GetItemCount(backpackContainer, req.item);
                string color = have >= req.amount ? "#55FF88" : "#FF7777";
                reqSb.Append($"{req.amount}x {req.item.itemName} (<color={color}>{have}/{req.amount}</color>)  ");
            }
        }

        GameObject reqObj = new GameObject("Cost", typeof(RectTransform), typeof(TextMeshProUGUI));
        reqObj.transform.SetParent(infoGO.transform, false);
        TextMeshProUGUI reqTMP = reqObj.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(reqTMP);
        reqTMP.fontSize = 11;
        reqTMP.text = reqSb.ToString();
        reqTMP.color = new Color(0.85f, 0.88f, 0.95f);

        // Trade Button / Unlocked Badge
        if (isAlreadyUnlocked)
        {
            GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeObj.transform.SetParent(cardGO.transform, false);
            badgeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 36);
            TextMeshProUGUI badgeTMP = badgeObj.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(badgeTMP);
            badgeTMP.fontSize = 12;
            badgeTMP.fontStyle = FontStyles.Bold;
            badgeTMP.alignment = TextAlignmentOptions.Center;
            badgeTMP.text = "[UNLOCKED]";
            badgeTMP.color = new Color(0.35f, 1f, 0.55f);
        }
        else
        {
            GameObject btnGO = new GameObject("BtnTrade", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(cardGO.transform, false);
            btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 36);

            Image btnBg = btnGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(btnBg, canAfford ? new Color(0.2f, 0.6f, 0.3f, 1f) : new Color(0.26f, 0.28f, 0.35f, 0.8f));

            Button btn = btnGO.GetComponent<Button>();
            btn.interactable = canAfford;
            btn.onClick.AddListener(() =>
            {
                bool success = currentMerchant.Trade(backpackContainer);
                if (success)
                {
                    SetFeedback($"<color=#55FF88>[UNLOCKED] Learned new recipe: {recipe.recipeName}!</color>");
                    backpackContainer?.NotifyChange();
                }
                else
                {
                    SetFeedback("<color=#FF7777>Cannot complete trade. Check required ingredients!</color>");
                }
            });

            GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(btnTMP);
            btnTMP.fontSize = 12;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.text = "Learn Recipe";
            btnTMP.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 36);
        }
    }

    private void CreateSeedTradeCard(SeedTrade trade)
    {
        bool canAfford = currentMerchant.CanTradeSeed(trade, backpackContainer);
        int haveInput = GetItemCount(backpackContainer, trade.inputSeed);

        GameObject cardGO = new GameObject($"SeedCard_{trade.tradeName}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(490, 62);

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.14f, 0.16f, 0.22f, 0.98f));

        HorizontalLayoutGroup hlg = cardGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 8;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Input Seed Slot Box
        GameObject inSlotGO = new GameObject("InSlot", typeof(RectTransform), typeof(Image));
        inSlotGO.transform.SetParent(cardGO.transform, false);
        inSlotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
        Image inSlotImg = inSlotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(inSlotImg, new Color(0.24f, 0.26f, 0.35f, 1f));

        GameObject inIconGO = new GameObject("InIcon", typeof(RectTransform), typeof(Image));
        inIconGO.transform.SetParent(inSlotGO.transform, false);
        RectTransform inIconRT = inIconGO.GetComponent<RectTransform>();
        inIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        inIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        inIconRT.anchoredPosition = Vector2.zero;
        inIconRT.sizeDelta = new Vector2(34, 34);

        Image inIcon = inIconGO.GetComponent<Image>();
        inIcon.preserveAspect = true;
        if (trade.inputSeed != null) inIcon.sprite = trade.inputSeed.itemIcon;

        // Trade Details Text (Input -> Output)
        GameObject descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(cardGO.transform, false);
        descGO.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 48);

        TextMeshProUGUI descTMP = descGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(descTMP);
        descTMP.fontSize = 11;
        string inColor = haveInput >= trade.inputAmount ? "#55FF88" : "#FF7777";
        string inText = trade.inputSeed != null ? trade.inputSeed.itemName : "Seed";
        string outText = trade.outputSeed != null ? trade.outputSeed.itemName : "Seed";

        descTMP.text = $"<b>{trade.inputAmount}x {inText}</b> (<color={inColor}>have: {haveInput}</color>)\n-> Receive: <color=#FFD700><b>{trade.outputAmount}x {outText}</b></color>";

        // Output Seed Slot Box
        GameObject outSlotGO = new GameObject("OutSlot", typeof(RectTransform), typeof(Image));
        outSlotGO.transform.SetParent(cardGO.transform, false);
        outSlotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
        Image outSlotImg = outSlotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(outSlotImg, new Color(0.24f, 0.32f, 0.28f, 1f));

        GameObject outIconGO = new GameObject("OutIcon", typeof(RectTransform), typeof(Image));
        outIconGO.transform.SetParent(outSlotGO.transform, false);
        RectTransform outIconRT = outIconGO.GetComponent<RectTransform>();
        outIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        outIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        outIconRT.anchoredPosition = Vector2.zero;
        outIconRT.sizeDelta = new Vector2(34, 34);

        Image outIcon = outIconGO.GetComponent<Image>();
        outIcon.preserveAspect = true;
        if (trade.outputSeed != null) outIcon.sprite = trade.outputSeed.itemIcon;

        // Barter Button
        GameObject btnGO = new GameObject("BtnBarter", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(85, 36);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, canAfford ? new Color(0.2f, 0.55f, 0.35f, 1f) : new Color(0.26f, 0.28f, 0.34f, 0.8f));

        Button btn = btnGO.GetComponent<Button>();
        btn.interactable = canAfford;
        SeedTrade tradeRef = trade;
        btn.onClick.AddListener(() =>
        {
            bool success = currentMerchant.TradeSeed(tradeRef, backpackContainer);
            if (success)
            {
                SetFeedback($"<color=#55FF88>Bartered {tradeRef.inputAmount}x {tradeRef.inputSeed.itemName} for {tradeRef.outputAmount}x {tradeRef.outputSeed.itemName}!</color>");
                backpackContainer?.NotifyChange();
            }
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTMP);
        btnTMP.fontSize = 11;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.text = "Barter";
        btnTMP.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(85, 36);
    }

    private void CreateRationTradeCard(RationTrade trade)
    {
        bool canAfford = currentMerchant.CanTradeRation(trade, backpackContainer);
        int haveInput = GetItemCount(backpackContainer, trade.inputProduce);

        GameObject cardGO = new GameObject($"RationCard_{trade.tradeName}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        cardGO.transform.SetParent(waresContainer, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(490, 62);

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.14f, 0.16f, 0.22f, 0.98f));

        HorizontalLayoutGroup hlg = cardGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 6, 6);
        hlg.spacing = 8;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Input Produce Slot Box
        GameObject inSlotGO = new GameObject("InSlot", typeof(RectTransform), typeof(Image));
        inSlotGO.transform.SetParent(cardGO.transform, false);
        inSlotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
        Image inSlotImg = inSlotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(inSlotImg, new Color(0.24f, 0.26f, 0.35f, 1f));

        GameObject inIconGO = new GameObject("InIcon", typeof(RectTransform), typeof(Image));
        inIconGO.transform.SetParent(inSlotGO.transform, false);
        RectTransform inIconRT = inIconGO.GetComponent<RectTransform>();
        inIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        inIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        inIconRT.anchoredPosition = Vector2.zero;
        inIconRT.sizeDelta = new Vector2(34, 34);

        Image inIcon = inIconGO.GetComponent<Image>();
        inIcon.preserveAspect = true;
        if (trade.inputProduce != null) inIcon.sprite = trade.inputProduce.itemIcon;

        // Trade Details Text
        GameObject descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(cardGO.transform, false);
        descGO.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 48);

        TextMeshProUGUI descTMP = descGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(descTMP);
        descTMP.fontSize = 11;
        string inColor = haveInput >= trade.inputAmount ? "#55FF88" : "#FF7777";
        string inText = trade.inputProduce != null ? trade.inputProduce.itemName : "Crop";
        string outText = trade.outputFood != null ? trade.outputFood.itemName : "Ration";

        descTMP.text = $"<b>{trade.inputAmount}x {inText}</b> (<color={inColor}>have: {haveInput}</color>)\n-> Rations: <color=#77CCFF><b>{trade.outputAmount}x {outText}</b></color>";

        // Output Food Slot Box
        GameObject outSlotGO = new GameObject("OutSlot", typeof(RectTransform), typeof(Image));
        outSlotGO.transform.SetParent(cardGO.transform, false);
        outSlotGO.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
        Image outSlotImg = outSlotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(outSlotImg, new Color(0.22f, 0.30f, 0.40f, 1f));

        GameObject outIconGO = new GameObject("OutIcon", typeof(RectTransform), typeof(Image));
        outIconGO.transform.SetParent(outSlotGO.transform, false);
        RectTransform outIconRT = outIconGO.GetComponent<RectTransform>();
        outIconRT.anchorMin = new Vector2(0.5f, 0.5f);
        outIconRT.anchorMax = new Vector2(0.5f, 0.5f);
        outIconRT.anchoredPosition = Vector2.zero;
        outIconRT.sizeDelta = new Vector2(34, 34);

        Image outIcon = outIconGO.GetComponent<Image>();
        outIcon.preserveAspect = true;
        if (trade.outputFood != null) outIcon.sprite = trade.outputFood.itemIcon;

        // Buy Button
        GameObject btnGO = new GameObject("BtnBuy", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(cardGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(85, 36);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, canAfford ? new Color(0.2f, 0.48f, 0.72f, 1f) : new Color(0.26f, 0.28f, 0.34f, 0.8f));

        Button btn = btnGO.GetComponent<Button>();
        btn.interactable = canAfford;
        RationTrade tradeRef = trade;
        btn.onClick.AddListener(() =>
        {
            bool success = currentMerchant.TradeRation(tradeRef, backpackContainer);
            if (success)
            {
                SetFeedback($"<color=#77CCFF>Traded for {tradeRef.outputAmount}x {tradeRef.outputFood.itemName}!</color>");
                backpackContainer?.NotifyChange();
            }
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTMP);
        btnTMP.fontSize = 11;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.text = "Trade";
        btnTMP.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        btnTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(85, 36);
    }

    private void BuildBackpackSlots(ItemContainer container)
    {
        if (backpackSlotsParent == null || container == null) return;

        EnsureSlotPrefab();
        Canvas currentCanvas = GetCanvas();

        if (backpackSlots.Count == container.maxSlots && backpackSlots.TrueForAll(s => s != null))
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

        foreach (InventoryButton slot in backpackSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        backpackSlots.Clear();

        if (slotPrefab == null)
        {
            Debug.LogWarning("[MerchantUI] slotPrefab is null, cannot build backpack slots!");
            return;
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, backpackSlotsParent);
            slotObj.name = $"Slot_{i}";
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
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        return canvas;
    }

    private void EnsureUIBuilt()
    {
        if (merchantPanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[MerchantUI] No Canvas found in scene!");
            return;
        }

        // 1. Fullscreen Dark Overlay
        GameObject overlayGO = new GameObject("MerchantPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        merchantPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        // 2. Central Window Panel with 9-Sliced Frame
        GameObject windowGO = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowGO.transform.SetParent(overlayGO.transform, false);
        RectTransform windowRT = windowGO.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(980, 600);

        Image windowBg = windowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(windowBg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Header Section
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(windowGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -14);
        titleRT.sizeDelta = new Vector2(-60, 30);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "WANDERING MERCHANT - DUNGEON OUTPOST";
        titleText.color = new Color(1f, 0.85f, 0.35f);

        GameObject subtitleGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.transform.SetParent(windowGO.transform, false);
        RectTransform subtitleRT = subtitleGO.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0, 1);
        subtitleRT.anchorMax = new Vector2(1, 1);
        subtitleRT.pivot = new Vector2(0.5f, 1);
        subtitleRT.anchoredPosition = new Vector2(0, -44);
        subtitleRT.sizeDelta = new Vector2(-60, 20);

        subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 11;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "A mysterious trader braving the dungeon. Barter seeds, recipes, and survival rations.";
        subtitleText.color = new Color(0.78f, 0.82f, 0.92f);

        // 4. Feedback Label
        GameObject feedbackGO = new GameObject("Feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.transform.SetParent(windowGO.transform, false);
        RectTransform feedbackRT = feedbackGO.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 1);
        feedbackRT.anchorMax = new Vector2(1, 1);
        feedbackRT.pivot = new Vector2(0.5f, 1);
        feedbackRT.anchoredPosition = new Vector2(0, -66);
        feedbackRT.sizeDelta = new Vector2(-60, 22);

        feedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(feedbackText);
        feedbackText.fontSize = 12;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.text = "Welcome traveler! What would you like to barter today?";
        feedbackText.color = new Color(0.85f, 0.92f, 1f);

        // 5. Close Button (X)
        GameObject closeGO = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGO.transform.SetParent(windowGO.transform, false);
        RectTransform closeRT = closeGO.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-14, -14);
        closeRT.sizeDelta = new Vector2(30, 30);

        Image closeBg = closeGO.GetComponent<Image>();
        closeButton = closeGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(closeButton, closeBg, new Color(0.6f, 0.18f, 0.18f, 1f), new Color(0.85f, 0.25f, 0.25f, 1f));
        closeButton.onClick.AddListener(Close);

        GameObject xTextGO = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.transform.SetParent(closeGO.transform, false);
        TextMeshProUGUI xTMP = xTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(xTMP);
        xTMP.fontSize = 15;
        xTMP.fontStyle = FontStyles.Bold;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.text = "X";
        xTMP.color = Color.white;
        xTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);

        // 6. Left Column: Merchant Wares (Scrollable) with 9-Sliced Frame
        GameObject waresColGO = new GameObject("WaresColumn", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        waresColGO.transform.SetParent(windowGO.transform, false);
        RectTransform waresColRT = waresColGO.GetComponent<RectTransform>();
        waresColRT.anchorMin = new Vector2(0, 0);
        waresColRT.anchorMax = new Vector2(0, 1);
        waresColRT.pivot = new Vector2(0, 0.5f);
        waresColRT.anchoredPosition = new Vector2(20, -18);
        waresColRT.sizeDelta = new Vector2(530, -140);

        Image waresColBg = waresColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(waresColBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        // Viewport
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(waresColGO.transform, false);
        RectTransform vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = new Vector2(-12, -12);

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(-8, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 8, 8);
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = waresColGO.GetComponent<ScrollRect>();
        sr.viewport = vpRT;
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        waresContainer = contentGO.transform;

        // 7. Right Column: Backpack Section with 9-Sliced Frame
        GameObject backpackColGO = new GameObject("BackpackColumn", typeof(RectTransform), typeof(Image));
        backpackColGO.transform.SetParent(windowGO.transform, false);
        RectTransform bpColRT = backpackColGO.GetComponent<RectTransform>();
        bpColRT.anchorMin = new Vector2(1, 0);
        bpColRT.anchorMax = new Vector2(1, 1);
        bpColRT.pivot = new Vector2(1, 0.5f);
        bpColRT.anchoredPosition = new Vector2(-20, -18);
        bpColRT.sizeDelta = new Vector2(390, -140);

        Image bpColBg = backpackColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bpColBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        // Backpack Column Header
        GameObject bpHeaderGO = new GameObject("BackpackHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        bpHeaderGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpHeaderRT = bpHeaderGO.GetComponent<RectTransform>();
        bpHeaderRT.anchorMin = new Vector2(0, 1);
        bpHeaderRT.anchorMax = new Vector2(1, 1);
        bpHeaderRT.pivot = new Vector2(0.5f, 1);
        bpHeaderRT.anchoredPosition = new Vector2(0, -8);
        bpHeaderRT.sizeDelta = new Vector2(-20, 24);

        backpackHeaderTitle = bpHeaderGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(backpackHeaderTitle);
        backpackHeaderTitle.fontSize = 13;
        backpackHeaderTitle.fontStyle = FontStyles.Bold;
        backpackHeaderTitle.alignment = TextAlignmentOptions.Center;
        backpackHeaderTitle.text = "PLAYER BACKPACK";
        backpackHeaderTitle.color = new Color(0.4f, 0.85f, 1f);

        // Backpack Grid Parent
        GameObject bpGridGO = new GameObject("BackpackSlots", typeof(RectTransform), typeof(GridLayoutGroup));
        bpGridGO.transform.SetParent(backpackColGO.transform, false);
        RectTransform bpGridRT = bpGridGO.GetComponent<RectTransform>();
        bpGridRT.anchorMin = new Vector2(0, 0);
        bpGridRT.anchorMax = new Vector2(1, 1);
        bpGridRT.sizeDelta = new Vector2(-20, -44);
        bpGridRT.anchoredPosition = new Vector2(0, -14);

        GridLayoutGroup bpGLG = bpGridGO.GetComponent<GridLayoutGroup>();
        bpGLG.cellSize = new Vector2(50, 50);
        bpGLG.spacing = new Vector2(8, 8);
        bpGLG.padding = new RectOffset(8, 8, 8, 8);
        bpGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGLG.constraintCount = 6;
        bpGLG.childAlignment = TextAnchor.UpperCenter;

        backpackSlotsParent = bpGridGO.transform;

        // 8. Footer: Continue Journey Button
        GameObject continueGO = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
        continueGO.transform.SetParent(windowGO.transform, false);
        RectTransform continueRT = continueGO.GetComponent<RectTransform>();
        continueRT.anchorMin = new Vector2(0.5f, 0);
        continueRT.anchorMax = new Vector2(0.5f, 0);
        continueRT.pivot = new Vector2(0.5f, 0);
        continueRT.anchoredPosition = new Vector2(0, 12);
        continueRT.sizeDelta = new Vector2(290, 38);

        Image continueBg = continueGO.GetComponent<Image>();
        continueButton = continueGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(continueButton, continueBg, new Color(0.18f, 0.55f, 0.32f, 1f), new Color(0.24f, 0.70f, 0.40f, 1f));
        continueButton.onClick.AddListener(Close);

        GameObject continueTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        continueTextGO.transform.SetParent(continueGO.transform, false);
        TextMeshProUGUI continueTMP = continueTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(continueTMP);
        continueTMP.fontSize = 13;
        continueTMP.fontStyle = FontStyles.Bold;
        continueTMP.alignment = TextAlignmentOptions.Center;
        continueTMP.text = "CONTINUE EXPEDITION >>";
        continueTMP.color = Color.white;
        continueTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(290, 38);
    }

    private int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null || container.itemSlots == null) return 0;
        int count = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i] != null && container.itemSlots[i].itemData == item)
            {
                count += container.itemSlots[i].amount;
            }
        }
        return count;
    }
}
