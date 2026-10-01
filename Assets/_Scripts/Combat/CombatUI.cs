using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatUI : MonoBehaviour
{
    public static CombatUI Instance { get; private set; }

    [Header("Core References")]
    public CombatManager combatManager;
    public PlayerStats playerStats;
    public TurnManager turnManager;
    public EnemyTargetSelector targetSelector;
    public WaveManager waveManager;

    [Header("Player Status HUD")]
    public TextMeshProUGUI playerNameText;
    public Image hpFillImage;
    public TextMeshProUGUI hpText;
    public Image hungerFillImage;
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI buffsText;

    [Header("Action Buttons")]
    public Button attackButton;
    public Button defendButton;
    public Button eatButton;
    public Button endTurnButton;

    [Header("Turn & Log Banner")]
    public Image turnBannerBg;
    public TextMeshProUGUI turnBannerText;
    public TextMeshProUGUI combatLogText;
    public TextMeshProUGUI stageProgressText;

    [Header("Target Info HUD")]
    public GameObject targetInfoPanel;
    public TextMeshProUGUI targetNameText;
    public Image targetHpFill;
    public TextMeshProUGUI targetHpText;
    public TextMeshProUGUI targetIntentText;
    public Transform targetReticle; // Con trỏ chỉ mục tiêu trong world space

    [Header("Food Selection Panel")]
    public GameObject foodPanel;
    public Transform foodListContainer;
    public GameObject foodItemPrefab;
    public Button closeFoodPanelButton;
    public TextMeshProUGUI noFoodText;

    [Header("Victory & Defeat Panels")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryText;
    public Button victoryReturnButton;
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatText;
    public Button defeatReturnButton;

    [Header("Reward Panel")]
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardDescriptionText;
    public Button rewardClaimButton;

    private List<GameObject> activeFoodButtons = new List<GameObject>();

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
        FindReferences();
        BindButtons();
        SubscribeEvents();

        if (foodPanel != null) foodPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);

        RefreshAll();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    public void FindReferences()
    {
        if (combatManager == null) combatManager = FindFirstObjectByType<CombatManager>();
        if (playerStats == null)
        {
            if (combatManager != null && combatManager.playerStats != null)
                playerStats = combatManager.playerStats;
            else
                playerStats = FindFirstObjectByType<PlayerStats>();
        }
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (targetSelector == null) targetSelector = FindFirstObjectByType<EnemyTargetSelector>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
    }

    private void BindButtons()
    {
        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(OnAttackClicked);
        }

        if (defendButton != null)
        {
            defendButton.onClick.RemoveAllListeners();
            defendButton.onClick.AddListener(OnDefendClicked);
        }

        if (eatButton != null)
        {
            eatButton.onClick.RemoveAllListeners();
            eatButton.onClick.AddListener(OnEatClicked);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        if (closeFoodPanelButton != null)
        {
            closeFoodPanelButton.onClick.RemoveAllListeners();
            closeFoodPanelButton.onClick.AddListener(() =>
            {
                if (foodPanel != null) foodPanel.SetActive(false);
            });
        }

        if (victoryReturnButton != null)
        {
            victoryReturnButton.onClick.RemoveAllListeners();
            victoryReturnButton.onClick.AddListener(OnReturnToBaseClicked);
        }

        if (defeatReturnButton != null)
        {
            defeatReturnButton.onClick.RemoveAllListeners();
            defeatReturnButton.onClick.AddListener(OnReturnToBaseAfterDefeatClicked);
        }

        if (rewardClaimButton != null)
        {
            rewardClaimButton.onClick.RemoveAllListeners();
            rewardClaimButton.onClick.AddListener(OnRewardClaimClicked);
        }
    }

    private void SubscribeEvents()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatLog += LogMessage;
            combatManager.OnTurnChanged += HandleTurnChanged;
            combatManager.OnCombatPreparationCountdown += HandleCombatPreparationCountdown;
            combatManager.OnCombatStarted += HandleCombatStarted;
        }

        if (playerStats != null)
        {
            playerStats.OnPlayerDeath += ShowDefeatScreen;
        }

        if (targetSelector != null)
        {
            targetSelector.OnTargetChanged += UpdateTargetInfo;
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnRunEnded += HandleRunEnded;
        }
    }

    private void UnsubscribeEvents()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatLog -= LogMessage;
            combatManager.OnTurnChanged -= HandleTurnChanged;
            combatManager.OnCombatPreparationCountdown -= HandleCombatPreparationCountdown;
            combatManager.OnCombatStarted -= HandleCombatStarted;
        }

        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= ShowDefeatScreen;
        }

        if (targetSelector != null)
        {
            targetSelector.OnTargetChanged -= UpdateTargetInfo;
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnRunEnded -= HandleRunEnded;
        }
    }

    private void Update()
    {
        // Cập nhật vị trí con trỏ mục tiêu nếu có
        if (targetReticle != null)
        {
            if (targetSelector != null && targetSelector.selectedEnemy != null && targetSelector.selectedEnemy.currentHealth > 0)
            {
                targetReticle.gameObject.SetActive(true);
                targetReticle.position = targetSelector.selectedEnemy.transform.position + new Vector3(0, 1.3f, 0);
            }
            else
            {
                targetReticle.gameObject.SetActive(false);
            }
        }

        // Bắt phím tắt nhanh (chỉ khi combat đang hoạt động và không trong lúc đếm ngược chuẩn bị)
        bool canUseShortcuts = combatManager != null && combatManager.isCombatActive && !combatManager.isPreparingCombat && combatManager.currentTurn == CombatManager.Turn.Player && playerStats != null && playerStats.currentHealth > 0;
        if (canUseShortcuts)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                OnAttackClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                OnDefendClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                OnEatClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnEndTurnClicked();
            }
        }

        // Cho phép click chuột trái lên kẻ địch để chọn mục tiêu (chỉ khi không click vào UI)
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                CheckMouseClickEnemy();
            }
        }

        UpdatePlayerHUD();
        UpdateActionButtons();
    }

    private void CheckMouseClickEnemy()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        if (hit.collider != null)
        {
            EnemyStats enemy = hit.collider.GetComponent<EnemyStats>();
            if (enemy != null && enemy.currentHealth > 0 && targetSelector != null)
            {
                targetSelector.SelectEnemy(enemy);
            }
        }
    }

    public void RefreshAll()
    {
        UpdatePlayerHUD();
        UpdateActionButtons();
        UpdateTurnBanner();
        if (targetSelector != null) UpdateTargetInfo(targetSelector.selectedEnemy);
        UpdateStageProgress();

        if (CampUI.Instance != null && CampUI.Instance.camp != null && CampUI.Instance.camp.isCampOpen)
        {
            CampUI.Instance.UpdatePlayerStatusUI();
        }
    }

    public void UpdatePlayerHUD()
    {
        if (playerStats == null) return;

        // Tên
        if (playerNameText != null)
        {
            playerNameText.text = "DŨNG SĨ (PLAYER)";
        }

        // HP
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = playerStats.maxHealth > 0 ? (float)playerStats.currentHealth / playerStats.maxHealth : 0f;
        }
        if (hpText != null)
        {
            hpText.text = $"HP: {playerStats.currentHealth} / {playerStats.maxHealth}";
        }

        // Hunger
        if (hungerFillImage != null)
        {
            hungerFillImage.fillAmount = playerStats.maxHunger > 0 ? (float)playerStats.currentHunger / playerStats.maxHunger : 0f;
        }
        if (hungerText != null)
        {
            hungerText.text = $"🍗 Độ no: {playerStats.currentHunger} / {playerStats.maxHunger}";
        }

        // AP
        if (apText != null)
        {
            apText.text = $"⚡ AP: {playerStats.currentAP} / {playerStats.maxAP}";
        }

        // Stats
        if (statsText != null)
        {
            int atk = playerStats.GetCurrentATK();
            int def = playerStats.GetCurrentDEF();
            int spd = playerStats.GetCurrentSpeed();
            string shieldStr = playerStats.defendCount > 0 ? $" (+{playerStats.defendValue * playerStats.defendCount} Khiên)" : "";
            statsText.text = $"⚔️ ATK: {atk}    🛡️ DEF: {def}{shieldStr}    💨 SPD: {spd}";
        }

        // Buffs
        if (buffsText != null)
        {
            if (playerStats.activeBuffs == null || playerStats.activeBuffs.Count == 0)
            {
                buffsText.text = "Hiệu ứng: Không có";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("Hiệu ứng: ");
                for (int i = 0; i < playerStats.activeBuffs.Count; i++)
                {
                    ActiveBuff b = playerStats.activeBuffs[i];
                    if (b != null && b.buff != null)
                    {
                        sb.Append($"[{b.buff.buffName} +{b.buffValue} ({b.remainingDuration} {b.buffDurationType})] ");
                    }
                }
                buffsText.text = sb.ToString();
            }
        }
    }

    public void UpdateActionButtons()
    {
        if (combatManager == null || playerStats == null) return;

        bool isPlayerTurn = combatManager.isCombatActive && !combatManager.isPreparingCombat && combatManager.currentTurn == CombatManager.Turn.Player;
        bool hasAP = playerStats.currentAP > 0;
        bool isAlive = playerStats.currentHealth > 0;

        if (attackButton != null)
        {
            attackButton.interactable = isPlayerTurn && hasAP && isAlive;
        }

        if (defendButton != null)
        {
            defendButton.interactable = isPlayerTurn && hasAP && isAlive;
        }

        if (eatButton != null)
        {
            eatButton.interactable = isPlayerTurn && hasAP && isAlive;
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = isPlayerTurn && isAlive;
        }
    }

    public void UpdateTurnBanner()
    {
        if (combatManager == null) return;

        if (combatManager.isPreparingCombat)
        {
            int secs = Mathf.CeilToInt(combatManager.preparationTimeRemaining);
            if (turnBannerText != null)
            {
                turnBannerText.text = combatManager.isAmbushCurrent
                    ? $"⚠️ BỊ PHỤC KÍCH! ĐỊCH SẼ TẤN CÔNG SAU {secs}s"
                    : $"⚔️ CHUẨN BỊ CHIẾN ĐẤU ({secs}s)";
                turnBannerText.color = combatManager.isAmbushCurrent
                    ? new Color(1f, 0.4f, 0.2f)
                    : new Color(1f, 0.85f, 0.2f);
            }

            if (turnBannerBg != null)
            {
                turnBannerBg.color = combatManager.isAmbushCurrent
                    ? new Color(0.6f, 0.15f, 0.05f, 0.85f)
                    : new Color(0.5f, 0.35f, 0.05f, 0.85f);
            }
            return;
        }

        if (!combatManager.isCombatActive)
        {
            if (CampUI.Instance != null && CampUI.Instance.camp != null && CampUI.Instance.camp.isCampOpen)
            {
                if (turnBannerText != null)
                {
                    turnBannerText.text = "🏕️ KHU CẮM TRẠI (CAMP) - NGHỈ NGƠI & NẤU NƯỚNG";
                    turnBannerText.color = new Color(1f, 0.85f, 0.3f);
                }
                if (turnBannerBg != null)
                {
                    turnBannerBg.color = new Color(0.35f, 0.22f, 0.08f, 0.9f);
                }
                return;
            }

            if (turnBannerText != null)
            {
                turnBannerText.text = "AN TOÀN (KHÔNG CÓ KẺ ĐỊCH)";
                turnBannerText.color = new Color(0.7f, 0.9f, 0.7f);
            }
            if (turnBannerBg != null)
            {
                turnBannerBg.color = new Color(0.1f, 0.3f, 0.2f, 0.7f);
            }
            return;
        }

        bool isPlayer = combatManager.currentTurn == CombatManager.Turn.Player;
        if (turnBannerText != null)
        {
            turnBannerText.text = isPlayer ? "LƯỢT CỦA BẠN (PLAYER TURN)" : "LƯỢT KẺ ĐỊCH (ENEMY TURN)";
            turnBannerText.color = isPlayer ? new Color(0.2f, 0.9f, 1f) : new Color(1f, 0.3f, 0.3f);
        }

        if (turnBannerBg != null)
        {
            turnBannerBg.color = isPlayer ? new Color(0.1f, 0.3f, 0.5f, 0.85f) : new Color(0.5f, 0.1f, 0.1f, 0.85f);
        }
    }

    public void UpdateTargetInfo(EnemyStats enemy)
    {
        if (targetInfoPanel == null) return;

        if (enemy == null || enemy.currentHealth <= 0)
        {
            targetInfoPanel.SetActive(false);
            return;
        }

        targetInfoPanel.SetActive(true);

        if (targetNameText != null)
        {
            targetNameText.text = $"🎯 {enemy.enemyData.enemyName}";
        }

        if (targetHpFill != null)
        {
            targetHpFill.fillAmount = enemy.enemyData.maxHealth > 0 ? (float)enemy.currentHealth / enemy.enemyData.maxHealth : 0f;
        }

        if (targetHpText != null)
        {
            targetHpText.text = $"HP: {enemy.currentHealth} / {enemy.enemyData.maxHealth}";
        }

        if (targetIntentText != null)
        {
            string intentDesc = "";
            switch (enemy.currentIntent)
            {
                case EnemyData.EnemyIntent.Attack:
                    intentDesc = $"⚔️ Tấn công ({enemy.GetCurrentATK()} DMG)";
                    break;
                case EnemyData.EnemyIntent.Defend:
                    intentDesc = $"🛡️ Phòng thủ (+{enemy.enemyData.DEF} DEF)";
                    break;
                case EnemyData.EnemyIntent.Buff:
                    intentDesc = "✨ Kỹ năng Buff";
                    break;
                case EnemyData.EnemyIntent.Debuff:
                    intentDesc = "💀 Kỹ năng Làm suy yếu";
                    break;
            }
            targetIntentText.text = $"Ý định: {intentDesc}";
        }
    }

    public void UpdateStageProgress()
    {
        if (stageProgressText != null && waveManager != null)
        {
            int current = waveManager.GetCurrentWaveIndex() + 1;
            stageProgressText.text = $"🚩 Stage: {current}";
        }
    }

    public void LogMessage(string message)
    {
        if (combatLogText != null)
        {
            combatLogText.text = message;
        }
        Debug.Log($"[CombatUI] {message}");
    }

    private void HandleTurnChanged(CombatManager.Turn turn)
    {
        UpdateTurnBanner();
        UpdateActionButtons();
        UpdateStageProgress();

        if (targetSelector != null)
        {
            UpdateTargetInfo(targetSelector.selectedEnemy);
        }
    }

    private void HandleCombatPreparationCountdown(float remainingSeconds)
    {
        UpdateTurnBanner();
        UpdateActionButtons();
    }

    private void HandleCombatStarted()
    {
        UpdateTurnBanner();
        UpdateActionButtons();
    }

    #region Action Handlers

    public void OnAttackClicked()
    {
        if (combatManager == null) return;

        // Tự động chọn mục tiêu nếu chưa chọn
        if (targetSelector != null && (targetSelector.selectedEnemy == null || targetSelector.selectedEnemy.currentHealth <= 0))
        {
            targetSelector.AutoSelectTarget(combatManager.enemies);
        }

        if (targetSelector == null || targetSelector.selectedEnemy == null)
        {
            LogMessage("⚠️ Không có kẻ địch nào trong tầm ngắm!");
            return;
        }

        combatManager.PlayerAttack();
        RefreshAll();
    }

    public void OnDefendClicked()
    {
        if (combatManager == null) return;
        combatManager.PlayerDefend();
        RefreshAll();
    }

    public void OnEatClicked()
    {
        if (foodPanel == null) return;

        bool isOpen = foodPanel.activeSelf;
        foodPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            PopulateFoodPanel();
        }
    }

    public void OnEndTurnClicked()
    {
        if (combatManager == null) return;
        if (foodPanel != null) foodPanel.SetActive(false);
        combatManager.EndTurnPlayer();
        RefreshAll();
    }

    #endregion

    #region Food Selection

    public void PopulateFoodPanel()
    {
        if (foodListContainer == null) return;

        // Xóa danh sách cũ
        for (int i = 0; i < activeFoodButtons.Count; i++)
        {
            if (activeFoodButtons[i] != null)
            {
                Destroy(activeFoodButtons[i]);
            }
        }
        activeFoodButtons.Clear();

        if (playerStats == null || playerStats.itemContainer == null)
        {
            if (noFoodText != null) noFoodText.gameObject.SetActive(true);
            return;
        }

        ItemContainer inventory = playerStats.itemContainer;
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
                noFoodText.text = "Không có món ăn nào trong ba lô!\n(Hãy nấu ăn trước khi vào Dungeon)";
            }
            return;
        }

        if (noFoodText != null) noFoodText.gameObject.SetActive(false);

        for (int i = 0; i < foodSlots.Count; i++)
        {
            ItemSlot slot = foodSlots[i];
            FoodData food = slot.itemData as FoodData;
            int count = slot.amount;

            GameObject rowGO = CreateFoodRow(food, count);
            if (rowGO != null)
            {
                rowGO.transform.SetParent(foodListContainer, false);
                activeFoodButtons.Add(rowGO);
            }
        }
    }

    private GameObject CreateFoodRow(FoodData food, int count)
    {
        GameObject row = new GameObject($"Food_{food.itemName}", typeof(RectTransform), typeof(Image));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, 50);

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        // Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(row.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(30, 0);
        iconRT.sizeDelta = new Vector2(36, 36);
        Image iconImg = iconGO.GetComponent<Image>();
        if (food.itemIcon != null) iconImg.sprite = food.itemIcon;

        // Label Info
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(60, 5);
        labelRT.offsetMax = new Vector2(-90, -5);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 15;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = $"<b>{food.itemName}</b> x{count}\n<size=12><color=#88FF88>+{food.healthValue} HP</color> | <color=#FFAA44>+{food.hungerValue} No</color></size>";

        // Button Ăn
        GameObject btnGO = new GameObject("EatBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-45, 0);
        btnRT.sizeDelta = new Vector2(75, 34);

        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f, 1f);

        Button btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (combatManager != null)
            {
                combatManager.PlayerEat(food);
                RefreshAll();
                if (foodPanel != null) foodPanel.SetActive(false);
            }
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.fontSize = 14;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.text = "ĂN (1 AP)";

        return row;
    }

    #endregion

    #region Victory & Defeat

    public void ShowVictoryScreen(string details = "")
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryText != null)
            {
                victoryText.text = string.IsNullOrEmpty(details)
                    ? "🏆 CHIẾN THẮNG HẦM NGỤC!\n\nBạn đã dọn sạch hầm ngục và đánh bại Boss!\nToàn bộ chiến lợi phẩm và hạt giống đã được bảo toàn an toàn."
                    : details;
            }
        }
    }

    public void ShowDefeatScreen()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            if (defeatText != null)
            {
                defeatText.text = "💀 BẠN ĐÃ TỬ TRẬN!\n\nBạn đã bị đánh bại trong hầm ngục.\nToàn bộ thức ăn và chiến lợi phẩm nhặt được trong chuyến đi đã rơi mất.";
            }
        }
    }

    private void HandleRunEnded(bool isVictory)
    {
        if (isVictory)
        {
            ShowVictoryScreen();
        }
        else
        {
            ShowDefeatScreen();
        }
    }

    public void OnReturnToBaseClicked()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    public void OnReturnToBaseAfterDefeatClicked()
    {
        // Xóa loot theo quy tắc Roguelite
        if (playerStats != null && playerStats.itemContainer != null && ProgressionManager.Instance != null)
        {
            // ProgressionManager đã trừ loot khi chết
        }

        OnReturnToBaseClicked();
    }

    #endregion

    #region Reward Handling

    public void ShowRewardScreen(string rewardDesc)
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            if (rewardDescriptionText != null)
            {
                rewardDescriptionText.text = $"🎁 PHÒNG THƯỞNG!\n\n{rewardDesc}";
            }
        }
    }

    public void OnRewardClaimClicked()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        if (waveManager != null)
        {
            waveManager.Continue();
        }
    }

    #endregion
}
