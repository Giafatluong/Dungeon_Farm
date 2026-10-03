using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages Camp Encounter UI and interactions.
/// Supports Eating, Cooking, Exercising, Resting, Continuing forward, and Returning Home safely,
/// along with an Ambush Risk Gauge and alert systems.
/// </summary>
public class CampUI : MonoBehaviour
{
    public static CampUI Instance { get; private set; }

    [Header("Core References")]
    public Camp camp;
    public PlayerStats playerStats;
    public WaveManager waveManager;

    [Header("Main Camp Panels")]
    public GameObject mainCampPanel;
    public TextMeshProUGUI campTitleText;
    public TextMeshProUGUI campDescText;

    [Header("Ambush Risk Gauge")]
    public TextMeshProUGUI ambushRiskPercentText;
    public TextMeshProUGUI ambushRiskStatusText;
    public Image ambushRiskFillImage;
    public GameObject ambushAlertBanner;
    public TextMeshProUGUI ambushAlertText;

    [Header("Player Status UI")]
    public TextMeshProUGUI playerStatusText;
    public TextMeshProUGUI playerBuffsText;

    [Header("Camp Action Buttons")]
    public Button eatButton;
    public Button cookButton;
    public Button exerciseButton;
    public Button restButton;
    public Button continueButton;
    public Button returnHomeButton;

    [Header("Food Modal")]
    public GameObject foodModal;
    public Transform foodListContainer;
    public TextMeshProUGUI noFoodText;
    public Button closeFoodModalButton;

    [Header("Cook Modal")]
    public GameObject cookModal;
    public Transform recipeListContainer;
    public TextMeshProUGUI noRecipeText;
    public Button closeCookModalButton;

    private List<GameObject> activeFoodCards = new List<GameObject>();
    private List<GameObject> activeRecipeCards = new List<GameObject>();

    private bool isInitialized = false;
    private bool isSubscribed = false;

    public static CampUI EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.InitializeCampUI();
            return Instance;
        }

        CampUI existing = FindFirstObjectByType<CampUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            Instance.InitializeCampUI();
            return Instance;
        }

        GameObject go = new GameObject("CampUI_Runtime");
        Instance = go.AddComponent<CampUI>();
        Instance.InitializeCampUI();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeCampUI();

        if (camp != null && camp.isCampOpen)
        {
            ShowCampScreen();
        }
        else
        {
            HideCampScreen();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    public void InitializeCampUI()
    {
        if (isInitialized) return;
        FindReferences();
        EnsureUIConstructed();
        BindButtons();
        SubscribeEvents();
        isInitialized = true;
    }

    public void FindReferences()
    {
        if (camp == null) camp = FindFirstObjectByType<Camp>(FindObjectsInactive.Include);
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (playerStats == null)
        {
            CombatManager cm = FindFirstObjectByType<CombatManager>();
            if (cm != null && cm.playerStats != null) playerStats = cm.playerStats;
            else playerStats = FindFirstObjectByType<PlayerStats>();
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed) return;

        if (camp != null)
        {
            camp.OnCampOpened += HandleCampOpened;
            camp.OnCampClosed += HandleCampClosed;
            camp.OnCampAction += HandleCampAction;
            camp.OnAmbushTriggered += HandleAmbushTriggered;
            isSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (camp != null)
        {
            camp.OnCampOpened -= HandleCampOpened;
            camp.OnCampClosed -= HandleCampClosed;
            camp.OnCampAction -= HandleCampAction;
            camp.OnAmbushTriggered -= HandleAmbushTriggered;
            isSubscribed = false;
        }
    }

    /// <summary>
    /// If UI elements are not assigned in Inspector, delegates to CampUIBuilder to build the UI at runtime.
    /// </summary>
    private void EnsureUIConstructed()
    {
        if (mainCampPanel != null) return;
        CampUIBuilder.Build(this);
    }

    private void BindButtons()
    {
        if (eatButton != null)
        {
            eatButton.onClick.RemoveAllListeners();
            eatButton.onClick.AddListener(OnEatButtonClicked);
        }

        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(OnCookButtonClicked);
        }

        if (exerciseButton != null)
        {
            exerciseButton.onClick.RemoveAllListeners();
            exerciseButton.onClick.AddListener(OnExerciseButtonClicked);
        }

        if (restButton != null)
        {
            restButton.onClick.RemoveAllListeners();
            restButton.onClick.AddListener(OnRestButtonClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        if (returnHomeButton != null)
        {
            returnHomeButton.onClick.RemoveAllListeners();
            returnHomeButton.onClick.AddListener(OnReturnHomeButtonClicked);
        }

        if (closeFoodModalButton != null)
        {
            closeFoodModalButton.onClick.RemoveAllListeners();
            closeFoodModalButton.onClick.AddListener(() =>
            {
                if (foodModal != null) foodModal.SetActive(false);
            });
        }

        if (closeCookModalButton != null)
        {
            closeCookModalButton.onClick.RemoveAllListeners();
            closeCookModalButton.onClick.AddListener(() =>
            {
                if (cookModal != null) cookModal.SetActive(false);
            });
        }
    }

    #region Public Screen Controls

    public void ShowCampScreen()
    {
        InitializeCampUI();

        if (mainCampPanel != null)
        {
            mainCampPanel.SetActive(true);
            mainCampPanel.transform.SetAsLastSibling();
        }
        if (foodModal != null) foodModal.SetActive(false);
        if (cookModal != null) cookModal.SetActive(false);
        if (ambushAlertBanner != null) ambushAlertBanner.SetActive(false);

        if (camp != null && (camp.mustContinue || camp.ambushState))
        {
            ShowPostAmbushCampScreen();
            return;
        }

        if (campTitleText != null)
        {
            campTitleText.text = "CAMP ENCOUNTER";
            campTitleText.color = new Color(1f, 0.85f, 0.35f);
        }

        if (campDescText != null)
        {
            campDescText.text = "A warm campfire burns in the dungeon. Rest, eat, or cook to recover.\n<color=#FFAA55>Warning: Activities create noise and aroma that attract wandering monsters!</color>";
            campDescText.color = new Color(0.85f, 0.88f, 0.95f);
        }

        if (continueButton != null)
        {
            TextMeshProUGUI continueTxt = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (continueTxt != null)
            {
                continueTxt.text = "CONTINUE FORWARD\n(Next Stage)";
            }
        }

        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
            if (CombatUI.Instance.turnBannerText != null)
            {
                CombatUI.Instance.turnBannerText.text = "CAMPFIRE - REST & RECOVER";
                CombatUI.Instance.turnBannerText.color = new Color(1f, 0.85f, 0.3f);
            }
            if (CombatUI.Instance.turnBannerBg != null)
            {
                CombatUI.Instance.turnBannerBg.color = new Color(0.35f, 0.22f, 0.08f, 0.9f);
            }
        }

        SetButtonsInteractable(true);
        UpdateAmbushRiskUI(camp != null ? camp.encounterChance : 0f);
        UpdatePlayerStatusUI();
    }

    public void ShowPostAmbushCampScreen()
    {
        InitializeCampUI();

        if (mainCampPanel != null)
        {
            mainCampPanel.SetActive(true);
            mainCampPanel.transform.SetAsLastSibling();
        }
        if (foodModal != null) foodModal.SetActive(false);
        if (cookModal != null) cookModal.SetActive(false);
        if (ambushAlertBanner != null) ambushAlertBanner.SetActive(false);

        if (campTitleText != null)
        {
            campTitleText.text = "CAMP (AMBUSH REPELLED)";
            campTitleText.color = new Color(1f, 0.5f, 0.3f);
        }

        if (campDescText != null)
        {
            campDescText.text = "<color=#88FF88>You defeated the ambushers!</color>\n<color=#FFAA55>The campsite has been compromised and attracted nearby monsters. You must advance!</color>";
            campDescText.color = Color.white;
        }

        if (ambushRiskPercentText != null)
        {
            ambushRiskPercentText.text = "Camp Compromised: <b>100%</b>";
            ambushRiskPercentText.color = new Color(1f, 0.4f, 0.3f);
        }

        if (ambushRiskFillImage != null)
        {
            ambushRiskFillImage.fillAmount = 1f;
            ambushRiskFillImage.color = new Color(0.9f, 0.2f, 0.2f);
        }

        if (ambushRiskStatusText != null)
        {
            ambushRiskStatusText.text = "Cannot rest here any longer. Move forward now!";
            ambushRiskStatusText.color = new Color(1f, 0.4f, 0.4f);
        }

        UpdatePlayerStatusUI();

        if (eatButton != null) eatButton.interactable = false;
        if (cookButton != null) cookButton.interactable = false;
        if (exerciseButton != null) exerciseButton.interactable = false;
        if (restButton != null) restButton.interactable = false;
        if (returnHomeButton != null) returnHomeButton.interactable = false;

        if (continueButton != null)
        {
            continueButton.interactable = true;
            TextMeshProUGUI continueTxt = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (continueTxt != null)
            {
                continueTxt.text = "CONTINUE FORWARD\n(MANDATORY)";
            }
        }

        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
            if (CombatUI.Instance.turnBannerText != null)
            {
                CombatUI.Instance.turnBannerText.text = "AMBUSH DEFEATED - ADVANCE FORWARD";
                CombatUI.Instance.turnBannerText.color = new Color(1f, 0.85f, 0.3f);
            }
            if (CombatUI.Instance.turnBannerBg != null)
            {
                CombatUI.Instance.turnBannerBg.color = new Color(0.4f, 0.2f, 0.05f, 0.9f);
            }
        }
    }

    public void HideCampScreen()
    {
        if (mainCampPanel != null) mainCampPanel.SetActive(false);
        if (foodModal != null) foodModal.SetActive(false);
        if (cookModal != null) cookModal.SetActive(false);
        if (ambushAlertBanner != null) ambushAlertBanner.SetActive(false);
    }

    #endregion

    #region Event Handlers

    private void HandleCampOpened()
    {
        ShowCampScreen();
    }

    private void HandleCampClosed()
    {
        if (ambushAlertBanner != null && ambushAlertBanner.activeSelf)
        {
            return;
        }

        HideCampScreen();
    }

    private void HandleCampAction(Camp.ActionType action, float newRisk)
    {
        UpdateAmbushRiskUI(newRisk);
        UpdatePlayerStatusUI();

        if (foodModal != null && foodModal.activeSelf)
        {
            PopulateFoodModal();
        }
        if (cookModal != null && cookModal.activeSelf)
        {
            PopulateCookModal();
        }
    }

    private void HandleAmbushTriggered()
    {
        SetButtonsInteractable(false);

        if (foodModal != null) foodModal.SetActive(false);
        if (cookModal != null) cookModal.SetActive(false);

        if (ambushAlertBanner != null)
        {
            ambushAlertBanner.SetActive(true);
            ambushAlertBanner.transform.SetAsLastSibling();
            if (ambushAlertText != null)
            {
                ambushAlertText.text = "AMBUSH! Monsters detected your campsite!";
            }
        }

        UpdateAmbushRiskUI(1f);
        StartCoroutine(CloseCampAfterAmbushDelay());
    }

    private System.Collections.IEnumerator CloseCampAfterAmbushDelay()
    {
        yield return new WaitForSeconds(1.2f);
        HideCampScreen();
    }

    #endregion

    #region Action Buttons Logic

    private void OnEatButtonClicked()
    {
        if (foodModal == null) return;
        bool isOpen = foodModal.activeSelf;
        foodModal.SetActive(!isOpen);
        if (cookModal != null) cookModal.SetActive(false);

        if (!isOpen)
        {
            foodModal.transform.SetAsLastSibling();
            PopulateFoodModal();
        }
    }

    private void OnCookButtonClicked()
    {
        if (cookModal == null) return;
        bool isOpen = cookModal.activeSelf;
        cookModal.SetActive(!isOpen);
        if (foodModal != null) foodModal.SetActive(false);

        if (!isOpen)
        {
            cookModal.transform.SetAsLastSibling();
            PopulateCookModal();
        }
    }

    private void OnExerciseButtonClicked()
    {
        if (camp == null) return;

        if (playerStats != null && playerStats.currentHunger <= 0)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.LogMessage("Fullness is already 0, cannot exercise further!");
            }
            return;
        }

        bool success = camp.PerformAction(Camp.ActionType.Exercise);
        if (success && CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"Exercised! Reduced {camp.exerciseHungerReduction} fullness.");
        }
    }

    private void OnRestButtonClicked()
    {
        if (camp == null) return;

        if (playerStats != null && playerStats.currentHealth >= playerStats.maxHealth)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.LogMessage("Health is already full, no need to rest!");
            }
            return;
        }

        bool success = camp.PerformAction(Camp.ActionType.Rest);
        if (success && CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"Rested by the campfire! Recovered {camp.restHealAmount} HP.");
        }
    }

    private void OnContinueButtonClicked()
    {
        if (camp == null) return;
        camp.PerformAction(Camp.ActionType.Continue);
    }

    private void OnReturnHomeButtonClicked()
    {
        if (camp == null) return;
        camp.PerformAction(Camp.ActionType.ReturnHome);
    }

    #endregion

    #region UI Updates

    public void UpdateAmbushRiskUI(float risk)
    {
        float percent = Mathf.Clamp01(risk);
        int percentInt = Mathf.RoundToInt(percent * 100f);

        if (ambushRiskPercentText != null)
        {
            ambushRiskPercentText.text = $"Ambush Risk: <b>{percentInt}%</b>";
        }

        if (ambushRiskFillImage != null)
        {
            ambushRiskFillImage.fillAmount = percent;

            if (percent < 0.25f)
            {
                ambushRiskFillImage.color = new Color(0.2f, 0.85f, 0.4f);
            }
            else if (percent < 0.50f)
            {
                ambushRiskFillImage.color = new Color(0.95f, 0.8f, 0.15f);
            }
            else if (percent < 0.75f)
            {
                ambushRiskFillImage.color = new Color(0.95f, 0.5f, 0.15f);
            }
            else
            {
                ambushRiskFillImage.color = new Color(0.9f, 0.2f, 0.2f);
            }
        }

        if (ambushRiskStatusText != null)
        {
            if (percent == 0f)
            {
                ambushRiskStatusText.text = "The campsite is quiet and safe.";
                ambushRiskStatusText.color = new Color(0.7f, 1f, 0.7f);
            }
            else if (percent < 0.35f)
            {
                ambushRiskStatusText.text = "Food aroma and camp noise begin spreading...";
                ambushRiskStatusText.color = new Color(1f, 0.9f, 0.6f);
            }
            else if (percent < 0.70f)
            {
                ambushRiskStatusText.text = "Footsteps and monster growls heard nearby!";
                ambushRiskStatusText.color = new Color(1f, 0.6f, 0.3f);
            }
            else
            {
                ambushRiskStatusText.text = "EXTREME DANGER! Monsters may ambush at any moment!";
                ambushRiskStatusText.color = new Color(1f, 0.3f, 0.3f);
            }
        }
    }

    public void UpdatePlayerStatusUI()
    {
        if (playerStats == null)
        {
            FindReferences();
        }

        if (playerStats != null)
        {
            if (playerStatusText != null)
            {
                playerStatusText.text = $"HP: <color=#66FF66>{playerStats.currentHealth}/{playerStats.maxHealth}</color>   |   Fullness: <color=#FFAA33>{playerStats.currentHunger}/{playerStats.maxHunger}</color>";
            }

            if (playerBuffsText != null)
            {
                if (playerStats.activeBuffs == null || playerStats.activeBuffs.Count == 0)
                {
                    playerBuffsText.text = "Buffs: None";
                    playerBuffsText.color = new Color(0.7f, 0.7f, 0.7f);
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder("Buffs: ");
                    for (int i = 0; i < playerStats.activeBuffs.Count; i++)
                    {
                        ActiveBuff b = playerStats.activeBuffs[i];
                        if (b != null && b.buff != null)
                        {
                            sb.Append($"[{b.buff.buffName} +{b.buffValue} ({b.remainingDuration} {b.buffDurationType})] ");
                        }
                    }
                    playerBuffsText.text = sb.ToString();
                    playerBuffsText.color = new Color(0.6f, 1f, 0.6f);
                }
            }
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (eatButton != null) eatButton.interactable = interactable;
        if (cookButton != null) cookButton.interactable = interactable;
        if (exerciseButton != null) exerciseButton.interactable = interactable;
        if (restButton != null) restButton.interactable = interactable;
        if (continueButton != null) continueButton.interactable = interactable;
        if (returnHomeButton != null) returnHomeButton.interactable = interactable;
    }

    #endregion

    #region Food Modal Population

    public void PopulateFoodModal()
    {
        if (foodListContainer == null) return;

        for (int i = 0; i < activeFoodCards.Count; i++)
        {
            if (activeFoodCards[i] != null) Destroy(activeFoodCards[i]);
        }
        activeFoodCards.Clear();

        ItemContainer inventory = camp != null ? camp.itemContainer : (playerStats != null ? playerStats.itemContainer : null);
        if (inventory == null)
        {
            if (noFoodText != null)
            {
                noFoodText.gameObject.SetActive(true);
                noFoodText.text = "Backpack not found!";
            }
            return;
        }

        List<ItemSlot> foodSlots = new List<ItemSlot>();
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            ItemSlot slot = inventory.itemSlots[i];
            if (slot != null && slot.itemData is FoodData && slot.amount > 0)
            {
                foodSlots.Add(slot);
            }
        }

        if (foodSlots.Count == 0)
        {
            if (noFoodText != null)
            {
                noFoodText.gameObject.SetActive(true);
                noFoodText.text = "No food in backpack!\n(Cook meals or harvest food from farm)";
            }
            return;
        }

        if (noFoodText != null) noFoodText.gameObject.SetActive(false);

        for (int i = 0; i < foodSlots.Count; i++)
        {
            ItemSlot slot = foodSlots[i];
            FoodData food = slot.itemData as FoodData;
            int count = slot.amount;

            GameObject cardGO = CreateFoodCard(food, count);
            if (cardGO != null)
            {
                cardGO.transform.SetParent(foodListContainer, false);
                activeFoodCards.Add(cardGO);
            }
        }
    }

    private GameObject CreateFoodCard(FoodData food, int count)
    {
        GameObject row = new GameObject($"Food_{food.itemName}", typeof(RectTransform), typeof(Image));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(440, 56);

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(row.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(28, 0);
        iconRT.sizeDelta = new Vector2(40, 40);
        Image iconImg = iconGO.GetComponent<Image>();
        if (food.itemIcon != null) iconImg.sprite = food.itemIcon;

        // Label Info
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(60, 4);
        labelRT.offsetMax = new Vector2(-110, -4);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 13;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        string buffSummary = GetFoodBuffSummary(food);
        string buffLine = !string.IsNullOrEmpty(buffSummary) ? $" | {buffSummary}" : "";
        label.text = $"<b>{food.itemName}</b> x{count}\n<size=11><color=#66FF66>+{food.healthValue} HP</color> | <color=#FFAA33>+{food.hungerValue} Full</color>{buffLine}</size>";

        // Eat Button
        GameObject btnGO = new GameObject("EatBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-55, 0);
        btnRT.sizeDelta = new Vector2(95, 36);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.35f, 1f);

        Button btn = btnGO.GetComponent<Button>();
        bool canEat = playerStats != null && playerStats.currentHunger < playerStats.maxHunger;
        btn.interactable = canEat;

        btn.onClick.AddListener(() =>
        {
            if (camp != null)
            {
                bool success = camp.PerformAction(Camp.ActionType.Eat, food);
                if (success)
                {
                    PopulateFoodModal();
                }
            }
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.fontSize = 11;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.text = canEat ? "EAT (+15%)" : "FULL";

        return row;
    }

    #endregion

    #region Cook Modal Population

    public void PopulateCookModal()
    {
        if (recipeListContainer == null) return;

        for (int i = 0; i < activeRecipeCards.Count; i++)
        {
            if (activeRecipeCards[i] != null) Destroy(activeRecipeCards[i]);
        }
        activeRecipeCards.Clear();

        List<RecipeData> recipes = CookingManager.Instance != null
            ? CookingManager.Instance.GetAllAvailableRecipes()
            : new List<RecipeData>();

        if (recipes.Count == 0)
        {
            if (noRecipeText != null)
            {
                noRecipeText.gameObject.SetActive(true);
                noRecipeText.text = "No cooking recipes unlocked!\n(Visit Merchants or Statues to unlock recipes)";
            }
            return;
        }

        if (noRecipeText != null) noRecipeText.gameObject.SetActive(false);

        ItemContainer container = camp != null ? camp.itemContainer : (playerStats != null ? playerStats.itemContainer : null);

        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeData recipe = recipes[i];
            if (recipe == null) continue;

            GameObject cardGO = CreateRecipeCard(recipe, container);
            if (cardGO != null)
            {
                cardGO.transform.SetParent(recipeListContainer, false);
                activeRecipeCards.Add(cardGO);
            }
        }
    }

    private GameObject CreateRecipeCard(RecipeData recipe, ItemContainer container)
    {
        GameObject row = new GameObject($"Recipe_{recipe.recipeName}", typeof(RectTransform), typeof(Image));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(450, 68);

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0.13f, 0.15f, 0.20f, 0.95f);

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(row.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(30, 0);
        iconRT.sizeDelta = new Vector2(44, 44);
        Image iconImg = iconGO.GetComponent<Image>();
        if (recipe.resultFood != null && recipe.resultFood.itemIcon != null)
        {
            iconImg.sprite = recipe.resultFood.itemIcon;
        }

        // Label Info
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(65, 4);
        labelRT.offsetMax = new Vector2(-120, -4);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 12;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        System.Text.StringBuilder ingSb = new System.Text.StringBuilder();
        bool canCook = CookingManager.Instance != null && CookingManager.Instance.CanCook(recipe, container);

        if (recipe.ingredients != null)
        {
            for (int j = 0; j < recipe.ingredients.Length; j++)
            {
                ItemRequirement req = recipe.ingredients[j];
                if (req.item == null) continue;
                int has = CookingManager.Instance != null ? CookingManager.Instance.GetItemCount(container, req.item) : 0;
                string colorTag = has >= req.amount ? "<color=#88FF88>" : "<color=#FF6666>";
                ingSb.Append($"{colorTag}{req.item.itemName} {has}/{req.amount}</color> ");
            }
        }

        string rBuff = recipe.resultFood != null ? GetFoodBuffSummary(recipe.resultFood) : "";
        string rBuffLine = !string.IsNullOrEmpty(rBuff) ? $"\n<size=10>Buff: {rBuff}</size>" : "";
        label.text = $"<b>{recipe.recipeName}</b>{rBuffLine}\n<size=10>Needs: {ingSb.ToString()}</size>";

        // Cook Button
        GameObject btnGO = new GameObject("CookBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-60, 0);
        btnRT.sizeDelta = new Vector2(105, 38);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = canCook ? new Color(0.85f, 0.45f, 0.1f, 1f) : new Color(0.35f, 0.35f, 0.35f, 0.6f);

        Button btn = btnGO.GetComponent<Button>();
        btn.interactable = canCook;

        btn.onClick.AddListener(() =>
        {
            if (camp != null)
            {
                bool success = camp.PerformAction(Camp.ActionType.Cook, recipe);
                if (success)
                {
                    PopulateCookModal();
                }
            }
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.fontSize = 11;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.text = canCook ? "COOK (+20%)" : "LACK ITEMS";

        return row;
    }

    private string GetFoodBuffSummary(FoodData food)
    {
        if (food == null) return "";
        if (food.foodBuff != null && food.foodBuff.Length > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                var fb = food.foodBuff[i];
                if (fb != null && fb.buffs != null)
                {
                    string bName = fb.buffs.buffName;
                    if (string.IsNullOrEmpty(bName)) bName = fb.buffs.buffType.ToString();
                    sb.Append($"<color=#66CCFF>+{fb.buffValue} {bName} ({fb.buffDuration} {fb.buffDurationType})</color> ");
                }
            }
            return sb.ToString().Trim();
        }
        else
        {
            string fname = (food.itemName ?? food.name ?? "").ToLower();
            if (fname.Contains("chilli") || fname.Contains("cay") || fname.Contains("spicy") || fname.Contains("meat"))
                return "<color=#FF6666>+3 ATK (3 Turns)</color>";
            if (fname.Contains("corn") || fname.Contains("bap") || fname.Contains("speed") || fname.Contains("carrot"))
                return "<color=#FFFF66>+2 Speed (3 Turns)</color>";
            return "<color=#66FF66>+2 DEF (3 Turns)</color>";
        }
    }

    #endregion
}
