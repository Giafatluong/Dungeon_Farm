using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quản lý giao diện và tương tác tại Khu Cắm Trại (Camp Encounter).
/// Hỗ trợ đầy đủ: Ăn uống, Nấu ăn, Tập thể dục, Nghỉ ngơi, Đi tiếp, Rút lui về nhà an toàn,
/// cùng hệ thống đo lường Nguy cơ bị phục kích (Ambush Risk Gauge) và cảnh báo Ambush trực quan.
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

        // Tìm Canvas thích hợp trong scene (ưu tiên CombatCanvas hoặc ScreenSpaceOverlay)
        Canvas targetCanvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.name.Contains("Combat") || c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        GameObject host = targetCanvas != null ? targetCanvas.gameObject : new GameObject("CampUI_Host", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        CampUI newUI = host.AddComponent<CampUI>();
        Instance = newUI;
        newUI.InitializeCampUI();
        return newUI;
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
        EnsureUIHierarchy();
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

        // Khôi phục tiêu đề và mô tả về trạng thái cắm trại bình thường
        if (campTitleText != null)
        {
            campTitleText.text = "🏕️ KHU CẮM TRẠI (CAMP ENCOUNTER)";
            campTitleText.color = new Color(1f, 0.85f, 0.35f);
        }

        if (campDescText != null)
        {
            campDescText.text = "Ngọn lửa trại bập bùng giữa hầm ngục. Hãy ăn uống, nấu nướng và hồi phục trước khi đi tiếp!\n<color=#FFAA55>⚠️ Chú ý: Mọi hoạt động nấu nướng, ăn uống sẽ tạo ra mùi hương và tiếng ồn thu hút quái vật!</color>";
            campDescText.color = new Color(0.85f, 0.88f, 0.95f);
        }

        if (continueButton != null)
        {
            TextMeshProUGUI continueTxt = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (continueTxt != null)
            {
                continueTxt.text = "🚪 ĐI TIẾP SANG STAGE TIẾP THEO";
            }
        }

        // Cập nhật turn banner của CombatUI
        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
            if (CombatUI.Instance.turnBannerText != null)
            {
                CombatUI.Instance.turnBannerText.text = "🏕️ KHU CẮM TRẠI (CAMP) - NGHỈ NGƠI & NẤU NƯỚNG";
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

    /// <summary>
    /// Hiển thị lại bảng Camp sau khi người chơi đánh bại quái vật phục kích.
    /// Theo yêu cầu: Chỉ có nút Đi Tiếp được phép ấn.
    /// </summary>
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

        // Cập nhật tiêu đề và mô tả trạng thái sau phục kích
        if (campTitleText != null)
        {
            campTitleText.text = "🏕️ KHU CẮM TRẠI (ĐÃ ĐẨY LÙI PHỤC KÍCH)";
            campTitleText.color = new Color(1f, 0.5f, 0.3f);
        }

        if (campDescText != null)
        {
            campDescText.text = "<color=#88FF88>⚔️ Bạn đã tiêu diệt toàn bộ kẻ địch phục kích thành công!</color>\n<color=#FFAA55>⚠️ Khu cắm trại này đã bị lộ và thu hút quái vật xung quanh. Bạn bắt buộc phải di chuyển tiếp!</color>";
            campDescText.color = Color.white;
        }

        // Cập nhật thanh đo nguy cơ: 100% Đỏ
        if (ambushRiskPercentText != null)
        {
            ambushRiskPercentText.text = "⚠️ Vị trí cắm trại đã bị lộ: <b>100%</b>";
            ambushRiskPercentText.color = new Color(1f, 0.4f, 0.3f);
        }

        if (ambushRiskFillImage != null)
        {
            ambushRiskFillImage.fillAmount = 1f;
            ambushRiskFillImage.color = new Color(0.9f, 0.2f, 0.2f);
        }

        if (ambushRiskStatusText != null)
        {
            ambushRiskStatusText.text = "Không thể tiếp tục nghỉ lại đây. Hãy di chuyển tiếp ngay!";
            ambushRiskStatusText.color = new Color(1f, 0.4f, 0.4f);
        }

        UpdatePlayerStatusUI();

        // CHỈ CÓ NÚT ĐI TIẾP ẤN ĐƯỢC
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
                continueTxt.text = "🚪 ĐI TIẾP SANG STAGE TIẾP THEO (BẮT BUỘC)";
            }
        }

        // Cập nhật banner trên CombatUI
        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
            if (CombatUI.Instance.turnBannerText != null)
            {
                CombatUI.Instance.turnBannerText.text = "🚪 ĐÃ ĐẨY LÙI PHỤC KÍCH - HÃY ĐI TIẾP!";
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
                ambushAlertText.text = "🚨 BỊ PHỤC KÍCH! KẺ ĐỊCH PHÁT HIỆN RA KHU CẮM TRẠI!";
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
                CombatUI.Instance.LogMessage("⚠️ Độ no đã bằng 0, không thể tập thể dục thêm!");
            }
            return;
        }

        bool success = camp.PerformAction(Camp.ActionType.Exercise);
        if (success && CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"🏃 Bạn đã tập thể dục! Tiêu hao {camp.exerciseHungerReduction} độ no.");
        }
    }

    private void OnRestButtonClicked()
    {
        if (camp == null) return;

        if (playerStats != null && playerStats.currentHealth >= playerStats.maxHealth)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.LogMessage("⚠️ Máu đã đầy, không cần nghỉ ngơi!");
            }
            return;
        }

        bool success = camp.PerformAction(Camp.ActionType.Rest);
        if (success && CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"⛺ Bạn nghỉ ngơi bên đống lửa! Hồi phục {camp.restHealAmount} HP.");
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
            ambushRiskPercentText.text = $"⚠️ Nguy cơ bị phục kích: <b>{percentInt}%</b>";
        }

        if (ambushRiskFillImage != null)
        {
            ambushRiskFillImage.fillAmount = percent;

            // Chuyển màu theo cấp độ nguy hiểm
            if (percent < 0.25f)
            {
                ambushRiskFillImage.color = new Color(0.2f, 0.85f, 0.4f); // Xanh lá
            }
            else if (percent < 0.50f)
            {
                ambushRiskFillImage.color = new Color(0.95f, 0.8f, 0.15f); // Vàng
            }
            else if (percent < 0.75f)
            {
                ambushRiskFillImage.color = new Color(0.95f, 0.5f, 0.15f); // Cam
            }
            else
            {
                ambushRiskFillImage.color = new Color(0.9f, 0.2f, 0.2f); // Đỏ rực
            }
        }

        if (ambushRiskStatusText != null)
        {
            if (percent == 0f)
            {
                ambushRiskStatusText.text = "Khu vực hoàn toàn yên tĩnh. Hãy chọn hành động hợp lý.";
                ambushRiskStatusText.color = new Color(0.7f, 1f, 0.7f);
            }
            else if (percent < 0.35f)
            {
                ambushRiskStatusText.text = "Mùi thức ăn và tiếng động bắt đầu lan tỏa...";
                ambushRiskStatusText.color = new Color(1f, 0.9f, 0.6f);
            }
            else if (percent < 0.70f)
            {
                ambushRiskStatusText.text = "Có tiếng bước chân và tiếng gầm gừ quái vật gần đây!";
                ambushRiskStatusText.color = new Color(1f, 0.6f, 0.3f);
            }
            else
            {
                ambushRiskStatusText.text = "CỰC KỲ NGUY HIỂM! Kẻ địch có thể xông vào bất cứ lúc nào!";
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
                playerStatusText.text = $"❤️ HP: <color=#66FF66>{playerStats.currentHealth}/{playerStats.maxHealth}</color>   |   🍗 Độ no: <color=#FFAA33>{playerStats.currentHunger}/{playerStats.maxHunger}</color>";
            }

            if (playerBuffsText != null)
            {
                if (playerStats.activeBuffs == null || playerStats.activeBuffs.Count == 0)
                {
                    playerBuffsText.text = "✨ Hiệu ứng: Chưa có buff";
                    playerBuffsText.color = new Color(0.7f, 0.7f, 0.7f);
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder("✨ Buff: ");
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

        // Dọn dẹp danh sách cũ
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
                noFoodText.text = "Không tìm thấy ba lô!";
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
                noFoodText.text = "Không có thức ăn trong ba lô!\n(Hãy nấu ăn hoặc mang theo món ăn nấu từ trang trại)";
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
        rt.sizeDelta = new Vector2(420, 56);

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
        labelRT.offsetMax = new Vector2(-105, -4);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 14;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        string buffSummary = GetFoodBuffSummary(food);
        string buffLine = !string.IsNullOrEmpty(buffSummary) ? $" | {buffSummary}" : "";
        label.text = $"<b>{food.itemName}</b> x{count}\n<size=11><color=#66FF66>+{food.healthValue} HP</color> | <color=#FFAA33>+{food.hungerValue} No</color>{buffLine}</size>";

        // Button Ăn
        GameObject btnGO = new GameObject("EatBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-55, 0);
        btnRT.sizeDelta = new Vector2(90, 36);

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
        btnText.fontSize = 12;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.text = canEat ? "ĂN (+15%)" : "ĐÃ NO";

        return row;
    }

    #endregion

    #region Cook Modal Population

    public void PopulateCookModal()
    {
        if (recipeListContainer == null) return;

        // Dọn dẹp danh sách cũ
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
                noRecipeText.text = "Chưa có công thức nấu ăn nào!\n(Gặp Thương Nhân hoặc Cúng Tế để mở khóa công thức)";
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
        rt.sizeDelta = new Vector2(430, 68);

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

        // Tên và Nguyên liệu
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(65, 4);
        labelRT.offsetMax = new Vector2(-115, -4);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 13;
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
        string rBuffLine = !string.IsNullOrEmpty(rBuff) ? $"\n<size=11>Hiệu ứng: {rBuff}</size>" : "";
        label.text = $"<b>{recipe.recipeName}</b>{rBuffLine}\n<size=11>Cần: {ingSb.ToString()}</size>";

        // Button Nấu
        GameObject btnGO = new GameObject("CookBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-60, 0);
        btnRT.sizeDelta = new Vector2(100, 38);

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
        btnText.fontSize = 12;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.text = canCook ? "NẤU (+20%)" : "THIẾU ĐỒ";

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
                return "<color=#FF6666>+3 ATK (3 Turn)</color>";
            if (fname.Contains("corn") || fname.Contains("bap") || fname.Contains("speed") || fname.Contains("carrot"))
                return "<color=#FFFF66>+2 Speed (3 Turn)</color>";
            return "<color=#66FF66>+2 DEF (3 Turn)</color>";
        }
    }

    #endregion

    #region Dynamic UI Builder (Fallback if Inspector not wired)

    public void EnsureUIHierarchy()
    {
        if (mainCampPanel != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = GetComponent<Canvas>();
        }
        if (parentCanvas == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.name.Contains("Combat") || c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    parentCanvas = c;
                    break;
                }
            }
        }

        if (parentCanvas == null) return;

        // 1. Root Camp Panel
        mainCampPanel = new GameObject("CampEncounterPanel", typeof(RectTransform), typeof(Image));
        mainCampPanel.transform.SetParent(parentCanvas.transform, false);
        RectTransform mainRT = mainCampPanel.GetComponent<RectTransform>();
        mainRT.anchorMin = new Vector2(0.5f, 0.5f);
        mainRT.anchorMax = new Vector2(0.5f, 0.5f);
        mainRT.pivot = new Vector2(0.5f, 0.5f);
        mainRT.sizeDelta = new Vector2(760, 560);

        Image mainBg = mainCampPanel.GetComponent<Image>();
        mainBg.color = new Color(0.08f, 0.10f, 0.15f, 0.97f);

        // Header Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(mainCampPanel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.88f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        campTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        campTitleText.fontSize = 24;
        campTitleText.fontStyle = FontStyles.Bold;
        campTitleText.alignment = TextAlignmentOptions.Center;
        campTitleText.text = "🏕️ KHU CẮM TRẠI (CAMP ENCOUNTER)";
        campTitleText.color = new Color(1f, 0.85f, 0.35f);

        // Subtitle Lore
        GameObject descGO = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(mainCampPanel.transform, false);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.05f, 0.79f);
        descRT.anchorMax = new Vector2(0.95f, 0.88f);
        descRT.offsetMin = Vector2.zero;
        descRT.offsetMax = Vector2.zero;
        campDescText = descGO.GetComponent<TextMeshProUGUI>();
        campDescText.fontSize = 14;
        campDescText.alignment = TextAlignmentOptions.Center;
        campDescText.text = "Ngọn lửa trại bập bùng giữa hầm ngục. Hãy ăn uống, nấu nướng và hồi phục trước khi đi tiếp!\n<color=#FFAA55>⚠️ Chú ý: Mọi hoạt động nấu nướng, ăn uống sẽ tạo ra mùi hương và tiếng ồn thu hút quái vật!</color>";
        campDescText.color = new Color(0.85f, 0.88f, 0.95f);

        // Ambush Risk Gauge Root
        GameObject riskRoot = new GameObject("AmbushGauge", typeof(RectTransform), typeof(Image));
        riskRoot.transform.SetParent(mainCampPanel.transform, false);
        RectTransform riskRT = riskRoot.GetComponent<RectTransform>();
        riskRT.anchorMin = new Vector2(0.08f, 0.62f);
        riskRT.anchorMax = new Vector2(0.92f, 0.76f);
        riskRT.offsetMin = Vector2.zero;
        riskRT.offsetMax = Vector2.zero;
        Image riskBg = riskRoot.GetComponent<Image>();
        riskBg.color = new Color(0.14f, 0.16f, 0.22f, 0.9f);

        // Risk Text
        GameObject riskTxtGO = new GameObject("RiskText", typeof(RectTransform), typeof(TextMeshProUGUI));
        riskTxtGO.transform.SetParent(riskRoot.transform, false);
        RectTransform riskTxtRT = riskTxtGO.GetComponent<RectTransform>();
        riskTxtRT.anchorMin = new Vector2(0.03f, 0.52f);
        riskTxtRT.anchorMax = new Vector2(0.97f, 0.98f);
        riskTxtRT.offsetMin = Vector2.zero;
        riskTxtRT.offsetMax = Vector2.zero;
        ambushRiskPercentText = riskTxtGO.GetComponent<TextMeshProUGUI>();
        ambushRiskPercentText.fontSize = 16;
        ambushRiskPercentText.fontStyle = FontStyles.Bold;
        ambushRiskPercentText.text = "⚠️ Nguy cơ bị phục kích: 0%";
        ambushRiskPercentText.color = new Color(1f, 0.9f, 0.3f);

        // Risk Bar Background & Fill
        GameObject barBgGO = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBgGO.transform.SetParent(riskRoot.transform, false);
        RectTransform barBgRT = barBgGO.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0.03f, 0.15f);
        barBgRT.anchorMax = new Vector2(0.97f, 0.45f);
        barBgRT.offsetMin = Vector2.zero;
        barBgRT.offsetMax = Vector2.zero;
        Image barBg = barBgGO.GetComponent<Image>();
        barBg.color = new Color(0.05f, 0.05f, 0.08f, 1f);

        GameObject barFillGO = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFillGO.transform.SetParent(barBgGO.transform, false);
        RectTransform barFillRT = barFillGO.GetComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = Vector2.one;
        barFillRT.sizeDelta = Vector2.zero;
        ambushRiskFillImage = barFillGO.GetComponent<Image>();
        ambushRiskFillImage.type = Image.Type.Filled;
        ambushRiskFillImage.fillMethod = Image.FillMethod.Horizontal;
        ambushRiskFillImage.fillAmount = 0f;
        ambushRiskFillImage.color = new Color(0.2f, 0.85f, 0.4f);

        // Status Text
        GameObject statusTxtGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusTxtGO.transform.SetParent(mainCampPanel.transform, false);
        RectTransform statusTxtRT = statusTxtGO.GetComponent<RectTransform>();
        statusTxtRT.anchorMin = new Vector2(0.08f, 0.54f);
        statusTxtRT.anchorMax = new Vector2(0.92f, 0.61f);
        statusTxtRT.offsetMin = Vector2.zero;
        statusTxtRT.offsetMax = Vector2.zero;
        ambushRiskStatusText = statusTxtGO.GetComponent<TextMeshProUGUI>();
        ambushRiskStatusText.fontSize = 13;
        ambushRiskStatusText.alignment = TextAlignmentOptions.Center;
        ambushRiskStatusText.text = "Khu vực đang yên tĩnh...";
        ambushRiskStatusText.color = new Color(0.7f, 1f, 0.7f);

        // Player Status Card
        GameObject pCardGO = new GameObject("PlayerCard", typeof(RectTransform), typeof(Image));
        pCardGO.transform.SetParent(mainCampPanel.transform, false);
        RectTransform pCardRT = pCardGO.GetComponent<RectTransform>();
        pCardRT.anchorMin = new Vector2(0.08f, 0.38f);
        pCardRT.anchorMax = new Vector2(0.92f, 0.52f);
        pCardRT.offsetMin = Vector2.zero;
        pCardRT.offsetMax = Vector2.zero;
        Image pCardBg = pCardGO.GetComponent<Image>();
        pCardBg.color = new Color(0.12f, 0.15f, 0.20f, 0.9f);

        GameObject pStatTxtGO = new GameObject("PStats", typeof(RectTransform), typeof(TextMeshProUGUI));
        pStatTxtGO.transform.SetParent(pCardGO.transform, false);
        RectTransform pStatTxtRT = pStatTxtGO.GetComponent<RectTransform>();
        pStatTxtRT.anchorMin = new Vector2(0.03f, 0.5f);
        pStatTxtRT.anchorMax = new Vector2(0.97f, 0.95f);
        pStatTxtRT.offsetMin = Vector2.zero;
        pStatTxtRT.offsetMax = Vector2.zero;
        playerStatusText = pStatTxtGO.GetComponent<TextMeshProUGUI>();
        playerStatusText.fontSize = 15;
        playerStatusText.alignment = TextAlignmentOptions.MidlineLeft;
        playerStatusText.text = "❤️ HP: 100/100   |   🍗 Độ no: 100/100";

        GameObject pBuffTxtGO = new GameObject("PBuffs", typeof(RectTransform), typeof(TextMeshProUGUI));
        pBuffTxtGO.transform.SetParent(pCardGO.transform, false);
        RectTransform pBuffTxtRT = pBuffTxtGO.GetComponent<RectTransform>();
        pBuffTxtRT.anchorMin = new Vector2(0.03f, 0.05f);
        pBuffTxtRT.anchorMax = new Vector2(0.97f, 0.48f);
        pBuffTxtRT.offsetMin = Vector2.zero;
        pBuffTxtRT.offsetMax = Vector2.zero;
        playerBuffsText = pBuffTxtGO.GetComponent<TextMeshProUGUI>();
        playerBuffsText.fontSize = 13;
        playerBuffsText.alignment = TextAlignmentOptions.MidlineLeft;
        playerBuffsText.text = "✨ Hiệu ứng: Chưa có";
        playerBuffsText.color = new Color(0.7f, 1f, 0.7f);

        // 4 Action Buttons Grid (Ăn, Nấu, Tập thể dục, Nghỉ ngơi)
        eatButton = CreateCampButton(mainCampPanel, "BtnEat", "🍖 ĂN UỐNG\n<size=11>(+15% Nguy cơ)</size>", new Vector2(-225, -95), new Vector2(140, 52), new Color(0.2f, 0.65f, 0.35f));
        cookButton = CreateCampButton(mainCampPanel, "BtnCook", "🍳 NẤU ĂN\n<size=11>(+20% Nguy cơ)</size>", new Vector2(-75, -95), new Vector2(140, 52), new Color(0.85f, 0.45f, 0.15f));
        exerciseButton = CreateCampButton(mainCampPanel, "BtnExercise", "🏃 TẬP THỂ DỤC\n<size=11>(-25 No, +35%)</size>", new Vector2(75, -95), new Vector2(140, 52), new Color(0.2f, 0.5f, 0.8f));
        restButton = CreateCampButton(mainCampPanel, "BtnRest", "⛺ NGHỈ NGƠI\n<size=11>(+25 HP, +10%)</size>", new Vector2(225, -95), new Vector2(140, 52), new Color(0.55f, 0.3f, 0.75f));

        // 2 Exit Buttons (Đi tiếp, Rút lui an toàn)
        continueButton = CreateCampButton(mainCampPanel, "BtnContinue", "🚪 ĐI TIẾP SANG STAGE TIẾP THEO", new Vector2(-150, -185), new Vector2(280, 52), new Color(0.25f, 0.35f, 0.45f));
        returnHomeButton = CreateCampButton(mainCampPanel, "BtnReturnHome", "🏠 RÚT LUI VỀ LÀNG AN TOÀN (GIỮ 100% ĐỒ)", new Vector2(150, -185), new Vector2(300, 52), new Color(0.75f, 0.55f, 0.15f));

        // Ambush Warning Banner
        ambushAlertBanner = new GameObject("AmbushAlertBanner", typeof(RectTransform), typeof(Image));
        ambushAlertBanner.transform.SetParent(mainCampPanel.transform, false);
        RectTransform alertRT = ambushAlertBanner.GetComponent<RectTransform>();
        alertRT.anchorMin = new Vector2(0.05f, 0.45f);
        alertRT.anchorMax = new Vector2(0.95f, 0.65f);
        alertRT.offsetMin = Vector2.zero;
        alertRT.offsetMax = Vector2.zero;
        Image alertBg = ambushAlertBanner.GetComponent<Image>();
        alertBg.color = new Color(0.75f, 0.1f, 0.1f, 0.98f);

        GameObject alertTxtGO = new GameObject("AlertText", typeof(RectTransform), typeof(TextMeshProUGUI));
        alertTxtGO.transform.SetParent(ambushAlertBanner.transform, false);
        RectTransform alertTxtRT = alertTxtGO.GetComponent<RectTransform>();
        alertTxtRT.anchorMin = Vector2.zero;
        alertTxtRT.anchorMax = Vector2.one;
        alertTxtRT.sizeDelta = Vector2.zero;
        ambushAlertText = alertTxtGO.GetComponent<TextMeshProUGUI>();
        ambushAlertText.fontSize = 20;
        ambushAlertText.fontStyle = FontStyles.Bold;
        ambushAlertText.alignment = TextAlignmentOptions.Center;
        ambushAlertText.text = "🚨 BỊ PHỤC KÍCH! KẺ ĐỊCH PHÁT HIỆN RA KHU CẮM TRẠI!";
        ambushAlertText.color = Color.white;
        ambushAlertBanner.SetActive(false);

        // Build Modals
        BuildFoodModal(parentCanvas.gameObject);
        BuildCookModal(parentCanvas.gameObject);

        mainCampPanel.transform.SetAsLastSibling();
    }

    private void BuildFoodModal(GameObject parentCanvas)
    {
        foodModal = new GameObject("CampFoodModal", typeof(RectTransform), typeof(Image));
        foodModal.transform.SetParent(parentCanvas.transform, false);
        RectTransform rt = foodModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480, 480);
        Image bg = foodModal.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.18f, 0.98f);

        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(foodModal.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.86f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI title = titleGO.GetComponent<TextMeshProUGUI>();
        title.fontSize = 20;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.text = "🍖 CHỌN MÓN ĂN TẠI CAMP";
        title.color = new Color(1f, 0.85f, 0.35f);

        // Scroll View Container
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(foodModal.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.04f, 0.14f);
        scrollRT.anchorMax = new Vector2(0.96f, 0.85f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        // Viewport with Mask
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewportRT;
        sr.content = contentRT;
        foodListContainer = contentGO.transform;

        GameObject noTxtGO = new GameObject("NoFoodText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noTxtGO.transform.SetParent(foodModal.transform, false);
        RectTransform noTxtRT = noTxtGO.GetComponent<RectTransform>();
        noTxtRT.anchorMin = new Vector2(0.1f, 0.3f);
        noTxtRT.anchorMax = new Vector2(0.9f, 0.7f);
        noTxtRT.offsetMin = Vector2.zero;
        noTxtRT.offsetMax = Vector2.zero;
        noFoodText = noTxtGO.GetComponent<TextMeshProUGUI>();
        noFoodText.fontSize = 16;
        noFoodText.alignment = TextAlignmentOptions.Center;
        noFoodText.text = "Không có món ăn trong ba lô!";
        noFoodText.color = new Color(0.8f, 0.8f, 0.8f);

        closeFoodModalButton = CreateCampButton(foodModal, "CloseBtn", "ĐÓNG", new Vector2(0, -205), new Vector2(120, 38), new Color(0.4f, 0.2f, 0.2f));
        foodModal.SetActive(false);
    }

    private void BuildCookModal(GameObject parentCanvas)
    {
        cookModal = new GameObject("CampCookModal", typeof(RectTransform), typeof(Image));
        cookModal.transform.SetParent(parentCanvas.transform, false);
        RectTransform rt = cookModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480, 480);
        Image bg = cookModal.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.18f, 0.98f);

        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(cookModal.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.86f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI title = titleGO.GetComponent<TextMeshProUGUI>();
        title.fontSize = 20;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.text = "🍳 NẤU ĂN BÊN ĐỐNG LỬA";
        title.color = new Color(1f, 0.85f, 0.35f);

        // Scroll View Container
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(cookModal.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.04f, 0.14f);
        scrollRT.anchorMax = new Vector2(0.96f, 0.85f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 25f;

        // Viewport with Mask
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewportRT;
        sr.content = contentRT;
        recipeListContainer = contentGO.transform;

        GameObject noTxtGO = new GameObject("NoRecipeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noTxtGO.transform.SetParent(cookModal.transform, false);
        RectTransform noTxtRT = noTxtGO.GetComponent<RectTransform>();
        noTxtRT.anchorMin = new Vector2(0.1f, 0.3f);
        noTxtRT.anchorMax = new Vector2(0.9f, 0.7f);
        noTxtRT.offsetMin = Vector2.zero;
        noTxtRT.offsetMax = Vector2.zero;
        noRecipeText = noTxtGO.GetComponent<TextMeshProUGUI>();
        noRecipeText.fontSize = 16;
        noRecipeText.alignment = TextAlignmentOptions.Center;
        noRecipeText.text = "Chưa có công thức nấu ăn nào!";
        noRecipeText.color = new Color(0.8f, 0.8f, 0.8f);

        closeCookModalButton = CreateCampButton(cookModal, "CloseBtn", "ĐÓNG", new Vector2(0, -205), new Vector2(120, 38), new Color(0.4f, 0.2f, 0.2f));
        cookModal.SetActive(false);
    }

    private Button CreateCampButton(GameObject parent, string name, string text, Vector2 pos, Vector2 size, Color btnColor)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent.transform, false);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnGO.GetComponent<Image>();
        img.color = btnColor;

        Button btn = btnGO.GetComponent<Button>();

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(btnGO.transform, false);
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tm = txtGO.GetComponent<TextMeshProUGUI>();
        tm.fontSize = 13;
        tm.fontStyle = FontStyles.Bold;
        tm.alignment = TextAlignmentOptions.Center;
        tm.text = text;
        tm.color = Color.white;

        return btn;
    }

    #endregion
}
