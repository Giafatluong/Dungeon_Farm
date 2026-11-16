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
    public Image ambushRiskBgImage;
    public Image ambushRiskFillImage;
    public GameObject ambushAlertBanner;
    public TextMeshProUGUI ambushAlertText;

    [Header("Ambush Risk Gauge Sprites")]
    public Sprite riskBgSafeSprite;
    public Sprite riskBgCautionSprite;
    public Sprite riskBgDangerSprite;
    public Sprite riskBgCompromisedSprite;

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

        return null;
    }

    public bool IsOpen => (mainCampPanel != null && mainCampPanel.activeInHierarchy) || (camp != null && camp.isCampOpen);

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (foodModal != null && foodModal.activeSelf)
            {
                UIEscapeManager.ConsumeEscape();
                foodModal.SetActive(false);
                return;
            }
            if (cookModal != null && cookModal.activeSelf)
            {
                UIEscapeManager.ConsumeEscape();
                cookModal.SetActive(false);
                return;
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (Instance == this) Instance = null;
    }

    public void InitializeCampUI()
    {
        if (isInitialized) return;
        FindReferences();
        BindButtons();
        SubscribeEvents();
        ApplyCampGOTYStyling();
        isInitialized = true;
    }

    private void ApplyCampGOTYStyling()
    {
        if (foodModal != null)
        {
            Image fmImg = foodModal.GetComponent<Image>();
            if (fmImg != null) DungeonUIAssetHelper.ApplyCompSprite(fmImg, "Comp_Camp_SubModalBase", sliced: true, Color.white);
        }
        if (cookModal != null)
        {
            Image cmImg = cookModal.GetComponent<Image>();
            if (cmImg != null) DungeonUIAssetHelper.ApplyCompSprite(cmImg, "Comp_Camp_SubModalBase", sliced: true, Color.white);
        }

        StyleModalCloseButton(closeFoodModalButton);
        StyleModalCloseButton(closeCookModalButton);

        ConfigureCampAutoSize(campTitleText, 14f, 20f, wrap: false);
        ConfigureCampAutoSize(campDescText, 10f, 13f, wrap: true);
        ConfigureCampAutoSize(playerStatusText, 10f, 13f, wrap: false);
        ConfigureCampAutoSize(playerBuffsText, 9.5f, 12.5f, wrap: false);
        ConfigureCampAutoSize(ambushRiskPercentText, 10f, 13f, wrap: false);
        ConfigureCampAutoSize(ambushRiskStatusText, 9.5f, 12.5f, wrap: true);

        EnforceRectangularCampButtons();
    }

    private void EnforceRectangularCampButtons()
    {
        Button[] row1 = new Button[] { eatButton, cookButton, exerciseButton };
        Button[] row2 = new Button[] { restButton, continueButton, returnHomeButton };
        string[] row1Sprites = new string[] { "Btn_NEW", "Btn_NEW", "Btn_QUIT" };
        string[] row2Sprites = new string[] { "Btn_HOW", "Btn_CONTINUE", "Btn_HOW" };
        float[] xPositions = new float[] { -210f, 0f, 210f };
        Vector2 rectSize = new Vector2(190f, 52f);

        for (int i = 0; i < 3; i++)
        {
            ApplyRectangularCampBtn(row1[i], new Vector2(xPositions[i], 30f), rectSize, row1Sprites[i]);
            ApplyRectangularCampBtn(row2[i], new Vector2(xPositions[i], -30f), rectSize, row2Sprites[i]);
        }
    }

    private static void ApplyRectangularCampBtn(Button btn, Vector2 pos, Vector2 size, string spriteName = null)
    {
        if (btn == null) return;
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            if (!string.IsNullOrEmpty(spriteName))
            {
                Sprite sp = DungeonUIAssetHelper.FindSprite(spriteName);
                if (sp != null) img.sprite = sp;
            }
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            img.color = Color.white;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 9f;
            tmp.fontSizeMax = 12f;
            tmp.margin = new Vector4(6f, 4f, 6f, 4f);
            tmp.lineSpacing = -5f;
            DungeonUIAssetHelper.ApplyFont(tmp, withOutline: true);
        }
    }

    private static void StyleModalCloseButton(Button btn)
    {
        if (btn == null) return;
        RectTransform rt = btn.GetComponent<RectTransform>();
        Image img = btn.GetComponent<Image>();
        bool isSmallIcon = rt != null && rt.sizeDelta.x > 0f && rt.sizeDelta.x < 64f && rt.sizeDelta.y > 0f && rt.sizeDelta.y < 64f;
        string compName = isSmallIcon ? "Comp_Statue_BtnCloseX" : "Comp_Merchant_TradeBtn";
        if (DungeonUIAssetHelper.ApplyCompButton(btn, img, compName, Color.white) && isSmallIcon && img != null)
        {
            img.type = Image.Type.Simple;
        }
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 9f;
            tmp.fontSizeMax = 13f;
        }
    }

    private static void ConfigureCampAutoSize(TextMeshProUGUI tmp, float minSize, float maxSize, bool wrap)
    {
        if (tmp == null) return;
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
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

        if (camp != null && (camp.mustContinue || camp.ambushState))
        {
            ShowPostAmbushCampScreen();
            return;
        }

        if (campTitleText != null)
        {
            campTitleText.text = "TRẠM NGHỈ LỬA TRẠI";
            campTitleText.color = new Color(1f, 0.85f, 0.35f);
        }

        if (campDescText != null)
        {
            campDescText.text = "Ngọn lửa trại ấm áp đang cháy sáng trong hầm ngục. Hãy nghỉ ngơi, ăn uống hoặc nấu nướng để hồi phục.\n<color=#FFAA55>Cảnh báo: Các hoạt động sẽ tạo ra tiếng ồn và mùi hương thu hút quái vật xung quanh!</color>";
            campDescText.color = new Color(0.85f, 0.88f, 0.95f);
        }

        if (continueButton != null)
        {
            TextMeshProUGUI continueTxt = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (continueTxt != null)
            {
                continueTxt.text = "TIẾP TỤC TIẾN LÊN\n(Chặng Tiếp Theo)";
            }
        }

        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
            if (CombatUI.Instance.turnBannerText != null)
            {
                CombatUI.Instance.turnBannerText.text = "LỬA TRẠI - NGHỈ NGƠI & HỒI PHỤC";
                CombatUI.Instance.turnBannerText.color = new Color(1f, 0.85f, 0.3f);
            }
            if (CombatUI.Instance.turnBannerBg != null && CombatUI.Instance.turnBannerBg.sprite == null)
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
            campTitleText.text = "TRẠM NGHỈ (ĐÃ ĐẨY LÙI PHỤC KÍCH)";
            campTitleText.color = new Color(1f, 0.5f, 0.3f);
        }

        if (campDescText != null)
        {
            campDescText.text = "<color=#88FF88>Bạn đã đánh bại toán quái phục kích!</color>\n<color=#FFAA55>Khu vực lửa trại đã bị lộ và thu hút quái vật xung quanh. Bạn buộc phải rời đi ngay!</color>";
            campDescText.color = Color.white;
        }

        if (ambushRiskPercentText != null)
        {
            ambushRiskPercentText.text = "Trại Bị Lộ: <b>100%</b>";
            ambushRiskPercentText.color = new Color(1f, 0.4f, 0.3f);
        }

        if (ambushRiskBgImage != null)
        {
            Sprite compBg = riskBgCompromisedSprite != null ? riskBgCompromisedSprite : DungeonUIAssetHelper.GetRiskGaugeBgSprite(3);
            if (compBg != null)
            {
                ambushRiskBgImage.sprite = compBg;
                ambushRiskBgImage.color = Color.white;
            }
        }

        if (ambushRiskFillImage != null)
        {
            if (ambushRiskFillImage.sprite == null)
            {
                Sprite fillSpr = DungeonUIAssetHelper.GetRiskGaugeFillSprite();
                if (fillSpr != null)
                {
                    ambushRiskFillImage.sprite = fillSpr;
                    ambushRiskFillImage.type = Image.Type.Filled;
                    ambushRiskFillImage.fillMethod = Image.FillMethod.Horizontal;
                }
            }
            ambushRiskFillImage.fillAmount = 1f;
            ambushRiskFillImage.color = new Color(0.95f, 0.25f, 0.25f);
        }

        if (ambushRiskStatusText != null)
        {
            ambushRiskStatusText.text = "Không thể nghỉ ngơi ở đây nữa. Hãy tiếp tục tiến lên!";
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
                continueTxt.text = "TIẾP TỤC TIẾN LÊN\n(BẮT BUỘC)";
            }
        }

        if (CombatUI.Instance != null)
        {
            if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
            if (CombatUI.Instance.turnBannerText != null)
            {
                CombatUI.Instance.turnBannerText.text = "ĐÃ ĐẨY LÙI PHỤC KÍCH - TIẾN LÊN";
                CombatUI.Instance.turnBannerText.color = new Color(1f, 0.85f, 0.3f);
            }
            if (CombatUI.Instance.turnBannerBg != null && CombatUI.Instance.turnBannerBg.sprite == null)
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
                ambushAlertText.text = "BỊ PHỤC KÍCH! Quái vật đã phát hiện ra lửa trại của bạn!";
            }
        }

        UpdateAmbushRiskUI(1f);
        StopAllCoroutines();
        StartCoroutine(CloseCampAfterAmbushDelay());
    }

    private System.Collections.IEnumerator CloseCampAfterAmbushDelay()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        HideCampScreen();
    }

    #endregion

    #region Action Buttons Logic

    public void OnEatButtonClicked()
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

    public void CloseFoodModal()
    {
        if (foodModal != null) foodModal.SetActive(false);
    }

    public void OnCookButtonClicked()
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

    public void CloseCookModal()
    {
        if (cookModal != null) cookModal.SetActive(false);
    }

    public void OnExerciseButtonClicked()
    {
        if (camp == null) return;

        if (playerStats != null && playerStats.currentHunger <= 0)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.LogMessage("Độ no đã là 0, không thể tập luyện thêm!");
            }
            return;
        }

        bool success = camp.PerformAction(Camp.ActionType.Exercise);
        if (success && CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"Đã tập luyện! Giảm {camp.exerciseHungerReduction} độ no.");
        }
    }

    public void OnRestButtonClicked()
    {
        if (camp == null) return;

        if (playerStats != null && playerStats.currentHealth >= playerStats.maxHealth)
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.LogMessage("Máu đã đầy, không cần nghỉ ngơi!");
            }
            return;
        }

        bool success = camp.PerformAction(Camp.ActionType.Rest);
        if (success && CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"Đã nghỉ ngơi bên lửa trại! Hồi phục {camp.restHealAmount} Máu.");
        }
    }

    public void OnContinueButtonClicked()
    {
        if (camp == null) return;
        camp.PerformAction(Camp.ActionType.Continue);
    }

    public void OnReturnHomeButtonClicked()
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
            ambushRiskPercentText.text = $"Nguy Cơ Bị Phục Kích: <b>{percentInt}%</b>";
        }

        if (ambushRiskBgImage != null)
        {
            Sprite bgSpr = DungeonUIAssetHelper.GetRiskGaugeBgSprite(0);
            if (bgSpr != null)
            {
                ambushRiskBgImage.sprite = bgSpr;
                ambushRiskBgImage.color = Color.white;
            }
        }

        if (ambushRiskFillImage != null)
        {
            Sprite fillSpr = DungeonUIAssetHelper.GetRiskGaugeFillSprite();
            if (fillSpr != null)
            {
                ambushRiskFillImage.sprite = fillSpr;
            }
            ambushRiskFillImage.type = Image.Type.Filled;
            ambushRiskFillImage.fillMethod = Image.FillMethod.Horizontal;
            ambushRiskFillImage.fillAmount = percent;
            ambushRiskFillImage.gameObject.SetActive(percent > 0.001f);

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
                ambushRiskStatusText.text = "Khu trại đang yên tĩnh và an toàn.";
                ambushRiskStatusText.color = new Color(0.7f, 1f, 0.7f);
            }
            else if (percent < 0.35f)
            {
                ambushRiskStatusText.text = "Mùi thức ăn và tiếng động bắt đầu lan tỏa...";
                ambushRiskStatusText.color = new Color(1f, 0.9f, 0.6f);
            }
            else if (percent < 0.70f)
            {
                ambushRiskStatusText.text = "Nghe thấy tiếng bước chân và tiếng gầm gừ gần đây!";
                ambushRiskStatusText.color = new Color(1f, 0.6f, 0.3f);
            }
            else
            {
                ambushRiskStatusText.text = "CỰC KỲ NGUY HIỂM! Quái vật có thể ập tới bất cứ lúc nào!";
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
                PlayerStats.FullnessTier tier = playerStats.GetCurrentFullnessTier();
                string tierTag = tier switch
                {
                    PlayerStats.FullnessTier.Hungry => " <color=#FF6644>[Đói: -2 Công, -3 Tốc]</color>",
                    PlayerStats.FullnessTier.Satiated => " <color=#55FF55>[No: +2 Công, +1 Thủ, +2 Tốc]</color>",
                    PlayerStats.FullnessTier.Overstuffed => " <color=#FFCC00>[Bội Thực: +3 Thủ, -4 Tốc, -1 Công, Hồi Máu]</color>",
                    _ => ""
                };
                playerStatusText.text = $"Máu: <color=#66FF66>{playerStats.currentHealth}/{playerStats.maxHealth}</color>   |   Độ No: <color=#FFAA33>{playerStats.currentHunger}/{playerStats.maxHunger}</color>{tierTag}";
            }

            if (playerBuffsText != null)
            {
                if (playerStats.activeBuffs == null || playerStats.activeBuffs.Count == 0)
                {
                    playerBuffsText.text = "Hiệu ứng: Không có";
                    playerBuffsText.color = new Color(0.7f, 0.7f, 0.7f);
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder("Hiệu ứng: ");
                    for (int i = 0; i < playerStats.activeBuffs.Count; i++)
                    {
                        ActiveBuff b = playerStats.activeBuffs[i];
                        if (b != null && b.buff != null)
                        {
                            string sign = b.buffValue > 0 ? "+" : "";
                            string durType = b.buffDurationType switch
                            {
                                FoodData.BuffDurationType.Turn => "Lượt",
                                FoodData.BuffDurationType.Combat => "Trận",
                                FoodData.BuffDurationType.Floor => "Tầng",
                                _ => b.buffDurationType.ToString()
                            };
                            sb.Append($"[{b.buff.buffName} {sign}{b.buffValue} ({b.remainingDuration} {durType})] ");
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
            if (playerStats == null) FindReferences();
            if (playerStats != null)
            {
                playerStats.ResolveItemContainer();
                inventory = playerStats.itemContainer;
                if (camp != null) camp.itemContainer = inventory;
            }
        }

        if (inventory == null)
        {
            if (noFoodText != null)
            {
                noFoodText.gameObject.SetActive(true);
                noFoodText.text = "Không tìm thấy túi đồ!";
            }
            return;
        }

        List<ItemSlot> foodSlots = new List<ItemSlot>();
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            ItemSlot slot = inventory.itemSlots[i];
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                if (slot.itemData is FoodData || 
                    slot.itemData.itemType == ItemData.ItemType.Food || 
                    slot.itemData.itemType == ItemData.ItemType.Produce)
                {
                    foodSlots.Add(slot);
                }
            }
        }

        if (foodSlots.Count == 0)
        {
            if (noFoodText != null)
            {
                noFoodText.gameObject.SetActive(true);
                noFoodText.text = "Không có thức ăn hoặc nông sản trong túi!";
            }
            return;
        }

        if (noFoodText != null) noFoodText.gameObject.SetActive(false);

        for (int i = 0; i < foodSlots.Count; i++)
        {
            ItemSlot slot = foodSlots[i];
            ItemData food = slot.itemData;
            int count = slot.amount;

            GameObject cardGO = CreateFoodCard(food, count);
            if (cardGO != null)
            {
                cardGO.transform.SetParent(foodListContainer, false);
                activeFoodCards.Add(cardGO);
            }
        }
    }

    private GameObject CreateFoodCard(ItemData food, int count)
    {
        if (food == null) return null;

        string labelStr;
        if (food is FoodData fd)
        {
            string buffSummary = GetFoodBuffSummary(fd);
            string buffLine = !string.IsNullOrEmpty(buffSummary) ? $" | {buffSummary}" : "";
            labelStr = $"<b>{food.itemName}</b> x{count}\n<size=11><color=#FF6B6B><b>+{fd.healthValue} Máu</b></color> | <color=#FFD043><b>+{fd.hungerValue} Độ No</b></color>{buffLine}</size>";
        }
        else
        {
            labelStr = $"<b>{food.itemName}</b> x{count}\n<size=11><color=#FF6B6B><b>+6 Máu</b></color> | <color=#FFD043><b>+15 Độ No</b></color> <color=#94A3B8>(Ăn Thô)</color></size>";
        }

        bool canEat = playerStats != null && playerStats.currentHunger < playerStats.maxHunger;
        return BuildCampActionRow(
            rowName: $"Food_{food.itemName}",
            height: 66f,
            iconSprite: food.itemIcon,
            labelText: labelStr,
            btnName: "EatBtn",
            btnWidth: 98f,
            btnTextStr: canEat ? "ĂN (+15%)" : "NO",
            interactable: canEat,
            enabledColor: new Color(0.2f, 0.7f, 0.35f, 1f),
            onClick: () =>
            {
                if (camp != null && camp.PerformAction(Camp.ActionType.Eat, food))
                {
                    PopulateFoodModal();
                }
            });
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
            ? CookingManager.Instance.GetUnlockedRecipes()
            : new List<RecipeData>();

        if (recipes.Count == 0)
        {
            if (noRecipeText != null)
            {
                noRecipeText.gameObject.SetActive(true);
                noRecipeText.text = "Chưa mở khóa công thức nấu ăn nào!\n(Gặp Thương Nhân hoặc Pho Tượng để mở khóa công thức)";
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
        System.Text.StringBuilder ingSb = new System.Text.StringBuilder();
        bool canCook = CookingManager.Instance != null && CookingManager.Instance.CanCook(recipe, container);

        if (recipe.ingredients != null)
        {
            for (int j = 0; j < recipe.ingredients.Length; j++)
            {
                ItemRequirement req = recipe.ingredients[j];
                if (req.item == null) continue;
                int has = CookingManager.Instance != null ? CookingManager.Instance.GetItemCount(container, req.item) : 0;
                string colorTag = has >= req.amount ? "<color=#4ADE80><b>" : "<color=#FF6B6B><b>";
                ingSb.Append($"{colorTag}{req.item.itemName} {has}/{req.amount}</b></color> ");
            }
        }

        string rBuff = recipe.resultFood != null ? GetFoodBuffSummary(recipe.resultFood) : "";
        string rBuffLine = !string.IsNullOrEmpty(rBuff) ? $"\n<size=10.5><color=#CBD5E1>Buff:</color> {rBuff}</size>" : "";
        string labelStr = $"<b>{recipe.DisplayName}</b>{rBuffLine}\n<size=10.5><color=#CBD5E1>Cần:</color> {ingSb}</size>";

        return BuildCampActionRow(
            rowName: $"Recipe_{recipe.recipeName}",
            height: 76f,
            iconSprite: recipe.resultFood != null ? recipe.resultFood.itemIcon : null,
            labelText: labelStr,
            btnName: "CookBtn",
            btnWidth: 104f,
            btnTextStr: canCook ? "NẤU (+20%)" : "THIẾU ĐỒ",
            interactable: canCook,
            enabledColor: new Color(0.85f, 0.45f, 0.1f, 1f),
            onClick: () =>
            {
                if (camp != null && camp.PerformAction(Camp.ActionType.Cook, recipe))
                {
                    PopulateCookModal();
                }
            });
    }

    private GameObject BuildCampActionRow(
        string rowName,
        float height,
        Sprite iconSprite,
        string labelText,
        string btnName,
        float btnWidth,
        string btnTextStr,
        bool interactable,
        Color enabledColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject row = new GameObject(rowName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(460f, height);

        LayoutElement le = row.GetComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0f;

        Image bg = row.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(bg, "Comp_Merchant_TradeRowBase", sliced: true, Color.white))
        {
            Sprite cardSprite = DungeonUIAssetHelper.GetFoodCardSprite();
            if (cardSprite != null)
            {
                bg.sprite = cardSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
            }
        }

        // Framed Item Slot + Icon
        GameObject slotGO = new GameObject("IconSlot", typeof(RectTransform), typeof(Image));
        slotGO.transform.SetParent(row.transform, false);
        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0, 0.5f);
        slotRT.anchorMax = new Vector2(0, 0.5f);
        slotRT.pivot = new Vector2(0, 0.5f);
        slotRT.anchoredPosition = new Vector2(12, 0);
        slotRT.sizeDelta = new Vector2(46, 46);
        Image slotImg = slotGO.GetComponent<Image>();
        if (!DungeonUIAssetHelper.ApplyCompSprite(slotImg, "Comp_Merchant_ItemSlot", sliced: true, Color.white))
        {
            slotImg.color = new Color(0.12f, 0.08f, 0.06f, 0.9f);
        }

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(5, 5);
        iconRT.offsetMax = new Vector2(-5, -5);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        if (iconSprite != null)
        {
            iconImg.sprite = iconSprite;
        }
        else
        {
            iconImg.enabled = false;
        }

        // Label Info (auto-sized with safe clearance from icon slot and action button)
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(68, 6);
        labelRT.offsetMax = new Vector2(-(btnWidth + 22f), -6);

        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(label, withOutline: true);
        label.fontSize = 12f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9.5f;
        label.fontSizeMax = 12.5f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = labelText;

        // Action Button
        GameObject btnGO = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.pivot = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-12, 0);
        btnRT.sizeDelta = new Vector2(btnWidth, 38);

        Image btnImg = btnGO.GetComponent<Image>();
        Button btn = btnGO.GetComponent<Button>();
        Color btnTint = interactable ? Color.white : new Color(0.55f, 0.55f, 0.58f, 0.8f);
        if (!DungeonUIAssetHelper.ApplyCompButton(btn, btnImg, "Comp_Merchant_TradeBtn", btnTint))
        {
            btnImg.color = interactable ? enabledColor : new Color(0.35f, 0.35f, 0.35f, 0.6f);
        }
        btn.interactable = interactable;
        btn.onClick.AddListener(onClick);

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = new Vector2(6, 2);
        btnTextRT.offsetMax = new Vector2(-6, -2);

        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnText, withOutline: true);
        btnText.fontSize = 11.5f;
        btnText.enableAutoSizing = true;
        btnText.fontSizeMin = 9f;
        btnText.fontSizeMax = 11.5f;
        btnText.fontStyle = FontStyles.Bold;
        btnText.textWrappingMode = TextWrappingModes.NoWrap;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = interactable ? Color.white : new Color(0.75f, 0.75f, 0.75f);
        btnText.text = btnTextStr;

        return row;
    }

    private string GetFoodBuffSummary(FoodData food)
    {
        if (food == null) return "";
        if (food.foodBuff != null && food.foodBuff.Length > 0)
        {
            bool hasEndlessFeast = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                var fb = food.foodBuff[i];
                if (fb != null && fb.buffs != null)
                {
                    string statLabel = CombatUI.GetBuffStatLabel(fb.buffs);
                    int dur = hasEndlessFeast ? fb.buffDuration * 2 : fb.buffDuration;
                    string durType = fb.buffDurationType switch
                    {
                        FoodData.BuffDurationType.Turn => "Lượt",
                        FoodData.BuffDurationType.Combat => "Trận",
                        FoodData.BuffDurationType.Floor => "Tầng",
                        _ => fb.buffDurationType.ToString()
                    };
                    string sign = fb.buffValue > 0 ? "+" : "";
                    sb.Append($"<color=#50D8FF><b>{sign}{fb.buffValue} {statLabel} ({dur} {durType})</b></color> ");
                }
            }
            return sb.ToString().Trim();
        }
        else
        {
            string fname = (food.itemName ?? food.name ?? "").ToLower();
            if (fname.Contains("chilli") || fname.Contains("cay") || fname.Contains("spicy") || fname.Contains("meat"))
                return "<color=#FF5252><b>+3 ATK (3 Lượt)</b></color>";
            if (fname.Contains("corn") || fname.Contains("bap") || fname.Contains("speed") || fname.Contains("carrot"))
                return "<color=#FBBF24><b>+2 SPD (3 Lượt)</b></color>";
            return "<color=#50D8FF><b>+2 DEF (3 Lượt)</b></color>";
        }
    }

    #endregion
}
