using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the Home Kitchen Station at Base.
/// Allows cooking recipes using ingredients from both the player's Backpack and Home Chest.
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
    [SerializeField] private RectTransform recipeListContainer;
    [SerializeField] private Canvas canvas;

    [Header("Labels & Feedback")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI feedbackToastText;
    [SerializeField] private Button closeButton;

    private ItemContainer backpackContainer;
    private ItemContainer chestContainer;
    private KitchenStation currentStation;

    private readonly List<GameObject> activeCards = new List<GameObject>();
    private float toastTimer = 0f;

    private bool isExplicitlyOpened = false;
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
        // Only hide on start if not already opened by player interaction.
        // If Open() was called upon dynamic creation or inactive state, Start() runs afterward; do not close it!
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

        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
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
        RefreshRecipes();

        if (feedbackToastText != null)
        {
            feedbackToastText.gameObject.SetActive(false);
        }
    }

    public void Close()
    {
        isExplicitlyOpened = false;
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
        if (backpackContainer != null) backpackContainer.OnInventoryChange += RefreshRecipes;
    }

    private void UnsubscribeContainerEvents()
    {
        if (backpackContainer != null) backpackContainer.OnInventoryChange -= RefreshRecipes;
    }

    private void OnDestroy()
    {
        UnsubscribeContainerEvents();
    }

    public void RefreshRecipes()
    {
        if (recipeListContainer == null) return;

        for (int i = 0; i < activeCards.Count; i++)
        {
            if (activeCards[i] != null) Destroy(activeCards[i]);
        }
        activeCards.Clear();

        List<RecipeData> recipes = CookingManager.Instance != null
            ? CookingManager.Instance.GetAllAvailableRecipes()
            : new List<RecipeData>();

        if (recipes == null || recipes.Count == 0)
        {
            if (statusText != null)
            {
                statusText.text = "Chua co cong thuc nau an nao duoc mo khoa!";
            }
            return;
        }

        ItemContainer[] containers = GetActiveContainers();

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData r = recipes[i];
            if (r == null) continue;

            GameObject cardGO = CreateRecipeCard(r, containers);
            if (cardGO != null)
            {
                cardGO.transform.SetParent(recipeListContainer, false);
                activeCards.Add(cardGO);
            }
        }
    }

    private ItemContainer[] GetActiveContainers()
    {
        if (backpackContainer != null)
        {
            return new ItemContainer[] { backpackContainer };
        }
        return new ItemContainer[0];
    }

    private GameObject CreateRecipeCard(RecipeData recipe, ItemContainer[] containers)
    {
        GameObject card = new GameObject($"Card_{recipe.recipeName}", typeof(RectTransform), typeof(Image));
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(620, 92);

        Image cardBg = card.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(cardBg, new Color(0.12f, 0.14f, 0.19f, 0.95f));

        // 1. Food Icon Slot
        GameObject slotGO = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(card.transform, false);
        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0, 0.5f);
        slotRT.anchorMax = new Vector2(0, 0.5f);
        slotRT.anchoredPosition = new Vector2(40, 0);
        slotRT.sizeDelta = new Vector2(58, 58);
        Image slotImg = slotGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(slotImg, new Color(0.18f, 0.22f, 0.30f, 1f));

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(6, 6);
        iconRT.offsetMax = new Vector2(-6, -6);
        Image iconImg = iconGO.GetComponent<Image>();
        if (recipe.resultFood != null && recipe.resultFood.itemIcon != null)
        {
            iconImg.sprite = recipe.resultFood.itemIcon;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = Color.clear;
        }

        // 2. Info Text (Title + Buffs + Ingredients)
        GameObject infoGO = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoGO.transform.SetParent(card.transform, false);
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0, 0);
        infoRT.anchorMax = new Vector2(1, 1);
        infoRT.offsetMin = new Vector2(80, 6);
        infoRT.offsetMax = new Vector2(-155, -6);

        TextMeshProUGUI infoText = infoGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(infoText);
        infoText.fontSize = 13;
        infoText.color = Color.white;
        infoText.alignment = TextAlignmentOptions.MidlineLeft;

        // Build Buff Summary
        string buffSummary = GetFoodBuffSummary(recipe.resultFood);
        string buffLine = !string.IsNullOrEmpty(buffSummary) ? $"  <size=11>{buffSummary}</size>" : "";

        // Build Ingredients Summary
        System.Text.StringBuilder ingSb = new System.Text.StringBuilder();
        bool canCook = CookingManager.Instance != null && CookingManager.Instance.CanCook(recipe, containers);

        if (recipe.ingredients != null)
        {
            for (int i = 0; i < recipe.ingredients.Length; i++)
            {
                ItemRequirement req = recipe.ingredients[i];
                if (req.item == null) continue;

                int hasTotal = CookingManager.Instance != null ? CookingManager.Instance.GetTotalItemCount(req.item, containers) : 0;
                string tagColor = hasTotal >= req.amount ? "#88FF88" : "#FF6666";
                ingSb.Append($"<color={tagColor}>{req.item.itemName} {hasTotal}/{req.amount}</color>  ");
            }
        }

        infoText.text = $"<b><color=#FFD700>{recipe.recipeName}</color></b>{buffLine}\n<size=11><color=#CCCCCC>Nguyen lieu:</color> {ingSb.ToString().TrimEnd()}</size>";

        // 3. Cook Button
        GameObject btnGO = new GameObject("CookButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(card.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-75, 0);
        btnRT.sizeDelta = new Vector2(125, 46);

        Image btnImg = btnGO.GetComponent<Image>();
        Button btn = btnGO.GetComponent<Button>();
        btn.interactable = canCook;

        Color btnColor = canCook ? new Color(0.18f, 0.55f, 0.28f, 0.95f) : new Color(0.28f, 0.30f, 0.34f, 0.6f);
        DungeonUIAssetHelper.StyleButton(btn, btnImg, btnColor);

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTxt = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTxt);
        btnTxt.fontSize = 12;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = canCook ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        btnTxt.text = canCook ? "NAU MON\n<size=9>(COOK)</size>" : "THIEU DO\n<size=9>(LACK)</size>";

        btn.onClick.AddListener(() =>
        {
            ExecuteCook(recipe);
        });

        return card;
    }

    private void ExecuteCook(RecipeData recipe)
    {
        if (recipe == null || CookingManager.Instance == null || backpackContainer == null) return;

        bool success = CookingManager.Instance.Cook(recipe, backpackContainer, backpackContainer);
        if (success)
        {
            string foodName = recipe.resultFood != null ? recipe.resultFood.itemName : "Mon an";
            ShowToast($"Nau thanh cong: <color=#88FF88>{foodName} x{recipe.resultAmount}</color>! Mon da duoc chuyen vao Balo.");
            RefreshRecipes();
        }
        else
        {
            ShowToast("<color=#FF6666>Khong du nguyen lieu trong Balo hoac khong the nau!</color>");
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

        if (food.healthValue > 0)
        {
            sb.Append($"<color=#66FF88>+{food.healthValue} HP</color> ");
        }
        if (food.hungerValue > 0)
        {
            sb.Append($"<color=#FFCC66>+{food.hungerValue} Do no</color> ");
        }

        if (food.foodBuff != null && food.foodBuff.Length > 0)
        {
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                var fb = food.foodBuff[i];
                if (fb != null && fb.buffs != null)
                {
                    string bName = fb.buffs.buffName;
                    if (string.IsNullOrEmpty(bName)) bName = fb.buffs.buffType.ToString();

                    string statLabel = bName;
                    if (statLabel.StartsWith("Tang ")) statLabel = statLabel.Substring(5);
                    else if (statLabel.StartsWith("Increase ")) statLabel = statLabel.Substring(9);

                    string sign = fb.buffValue > 0 ? "+" : "";
                    string durType = fb.buffDurationType switch
                    {
                        FoodData.BuffDurationType.Turn => "Turn",
                        FoodData.BuffDurationType.Combat => "Battle",
                        FoodData.BuffDurationType.Floor => "Floor",
                        _ => fb.buffDurationType.ToString()
                    };
                    sb.Append($"<color=#66CCFF>{sign}{fb.buffValue} {statLabel} ({fb.buffDuration} {durType})</color> ");
                }
            }
        }

        return sb.ToString().Trim();
    }

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
        rootRT.sizeDelta = new Vector2(700, 520);
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
        titleText.text = "BEP NAU AN GIA DINH (HOME KITCHEN)";

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
        subtitleText.text = "Che bien nong san thanh mon an tang luc  |  Chi dung nguyen lieu trong Balo";

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

        // 3. Scroll Area for Recipes
        GameObject scrollGO = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGO.transform.SetParent(kitchenPanel.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(18, 48);
        scrollRT.offsetMax = new Vector2(-18, -76);

        Image scrollBg = scrollGO.GetComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.07f, 0.1f, 0.5f);
        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;

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

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        recipeListContainer = contentGO.GetComponent<RectTransform>();
        recipeListContainer.anchorMin = new Vector2(0, 1);
        recipeListContainer.anchorMax = new Vector2(1, 1);
        recipeListContainer.pivot = new Vector2(0.5f, 1);
        recipeListContainer.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT;
        sr.content = recipeListContainer;

        // 4. Footer Toast Bar
        GameObject footerGO = new GameObject("Footer", typeof(RectTransform));
        footerGO.transform.SetParent(kitchenPanel.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.sizeDelta = new Vector2(0, 42);
        footerRT.anchoredPosition = Vector2.zero;

        GameObject toastGO = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastGO.transform.SetParent(footerGO.transform, false);
        RectTransform toastRT = toastGO.GetComponent<RectTransform>();
        toastRT.anchorMin = Vector2.zero;
        toastRT.anchorMax = Vector2.one;
        toastRT.sizeDelta = Vector2.zero;
        feedbackToastText = toastGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(feedbackToastText);
        feedbackToastText.fontSize = 12;
        feedbackToastText.alignment = TextAlignmentOptions.Center;
        feedbackToastText.color = new Color(0.9f, 0.95f, 1f);
        feedbackToastText.gameObject.SetActive(false);

        // Status empty label
        GameObject statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(kitchenPanel.transform, false);
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0.5f);
        statusRT.anchorMax = new Vector2(1, 0.5f);
        statusRT.sizeDelta = new Vector2(0, 40);
        statusText = statusGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statusText);
        statusText.fontSize = 13;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = new Color(0.8f, 0.8f, 0.8f);
        statusText.text = "";
    }
}
