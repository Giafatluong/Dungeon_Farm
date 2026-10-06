using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatUI : MonoBehaviour
{
    #region Singleton & Core References
    public static CombatUI Instance { get; private set; }

    [Header("Core Dependencies")]
    public CombatManager combatManager;
    public PlayerStats playerStats;
    public TurnManager turnManager;
    public EnemyTargetSelector targetSelector;
    public WaveManager waveManager;
    #endregion

    #region Inspector HUD References
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
    public Transform targetReticle;

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

    private readonly List<GameObject> activeFoodButtons = new();
    #endregion

    #region Unity Lifecycle
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

    private void Update()
    {
        UpdateReticle();
        HandleKeyboardShortcuts();

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
    #endregion

    #region Initialization & Bindings
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
        BindButton(attackButton, OnAttackClicked);
        BindButton(defendButton, OnDefendClicked);
        BindButton(eatButton, OnEatClicked);
        BindButton(endTurnButton, OnEndTurnClicked);

        if (closeFoodPanelButton != null)
        {
            closeFoodPanelButton.onClick.RemoveAllListeners();
            closeFoodPanelButton.onClick.AddListener(() =>
            {
                if (foodPanel != null) foodPanel.SetActive(false);
            });
        }

        BindButton(victoryReturnButton, OnReturnToBaseClicked);
        BindButton(defeatReturnButton, OnReturnToBaseAfterDefeatClicked);
        BindButton(rewardClaimButton, OnRewardClaimClicked);
    }

    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
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
    #endregion

    #region Input & Targeting
    private void UpdateReticle()
    {
        if (targetReticle == null) return;

        bool hasValidTarget = targetSelector != null && targetSelector.selectedEnemy != null && targetSelector.selectedEnemy.currentHealth > 0;
        targetReticle.gameObject.SetActive(hasValidTarget);

        if (hasValidTarget)
        {
            targetReticle.position = targetSelector.selectedEnemy.transform.position + new Vector3(0, 1.3f, 0);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        bool canUseShortcuts = combatManager != null && combatManager.isCombatActive && !combatManager.isPreparingCombat && combatManager.currentTurn == CombatManager.Turn.Player && playerStats != null && playerStats.currentHealth > 0;
        if (!canUseShortcuts) return;

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

    private void CheckMouseClickEnemy()
    {
        if (Camera.main == null) return;
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
    #endregion

    #region HUD Refresh & Status Display
    private int cachedHp = -1, cachedMaxHp = -1;
    private int cachedHunger = -1, cachedMaxHunger = -1;
    private int cachedAP = -1, cachedMaxAP = -1;
    private int cachedAtk = -1, cachedDef = -1, cachedSpd = -1, cachedDefendCount = -1;
    private int cachedBuffCount = -1;

    private bool? cachedIsPlayerTurn = null;
    private bool? cachedHasAP = null;
    private bool? cachedIsAlive = null;

    public void InvalidateHUDCache()
    {
        cachedHp = -1; cachedMaxHp = -1;
        cachedHunger = -1; cachedMaxHunger = -1;
        cachedAP = -1; cachedMaxAP = -1;
        cachedAtk = -1; cachedDef = -1; cachedSpd = -1; cachedDefendCount = -1;
        cachedBuffCount = -1;
        cachedIsPlayerTurn = null;
        cachedHasAP = null;
        cachedIsAlive = null;
    }

    public void RefreshAll()
    {
        InvalidateHUDCache();
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

        if (playerNameText != null && playerNameText.text != "HERO (PLAYER)")
        {
            playerNameText.text = "HERO (PLAYER)";
        }

        int curHp = playerStats.currentHealth;
        int maxHp = playerStats.maxHealth;
        if (curHp != cachedHp || maxHp != cachedMaxHp)
        {
            cachedHp = curHp;
            cachedMaxHp = maxHp;
            if (hpFillImage != null) hpFillImage.fillAmount = maxHp > 0 ? (float)curHp / maxHp : 0f;
            if (hpText != null) hpText.text = $"HP: {curHp} / {maxHp}";
        }

        int curHunger = playerStats.currentHunger;
        int maxHunger = playerStats.maxHunger;
        if (curHunger != cachedHunger || maxHunger != cachedMaxHunger)
        {
            cachedHunger = curHunger;
            cachedMaxHunger = maxHunger;
            if (hungerFillImage != null) hungerFillImage.fillAmount = maxHunger > 0 ? (float)curHunger / maxHunger : 0f;
            if (hungerText != null) hungerText.text = $"Fullness: {curHunger} / {maxHunger}";
        }

        int curAP = playerStats.currentAP;
        int maxAP = playerStats.maxAP;
        if (curAP != cachedAP || maxAP != cachedMaxAP)
        {
            cachedAP = curAP;
            cachedMaxAP = maxAP;
            if (apText != null) apText.text = $"AP: {curAP} / {maxAP}";
        }

        int atk = playerStats.GetCurrentATK();
        int def = playerStats.GetCurrentDEF();
        int spd = playerStats.GetCurrentSpeed();
        int defCount = playerStats.defendCount;
        if (atk != cachedAtk || def != cachedDef || spd != cachedSpd || defCount != cachedDefendCount)
        {
            cachedAtk = atk;
            cachedDef = def;
            cachedSpd = spd;
            cachedDefendCount = defCount;
            if (statsText != null)
            {
                string shieldStr = defCount > 0 ? $" (+{playerStats.defendValue * defCount} Shield)" : "";
                statsText.text = $"ATK: {atk}    DEF: {def}{shieldStr}    SPD: {spd}";
            }
        }

        int buffHash = 0;
        int buffCount = 0;
        if (playerStats.activeBuffs != null)
        {
            buffCount = playerStats.activeBuffs.Count;
            for (int i = 0; i < playerStats.activeBuffs.Count; i++)
            {
                ActiveBuff b = playerStats.activeBuffs[i];
                if (b != null && b.buff != null)
                {
                    buffHash = unchecked(buffHash * 397 ^ (b.buffValue * 17 + b.remainingDuration * 31 + (int)b.buffDurationType));
                }
            }
        }

        if (buffHash != cachedBuffCount)
        {
            cachedBuffCount = buffHash;
            if (buffsText != null)
            {
                if (buffCount == 0)
                {
                    buffsText.text = "Buffs: None";
                }
                else
                {
                    System.Text.StringBuilder sb = new("Buffs: ");
                    for (int i = 0; i < playerStats.activeBuffs.Count; i++)
                    {
                        ActiveBuff b = playerStats.activeBuffs[i];
                        if (b != null && b.buff != null)
                        {
                            string sign = b.buffValue > 0 ? "+" : "";
                            string durType = b.buffDurationType switch
                            {
                                FoodData.BuffDurationType.Turn => "Turn",
                                FoodData.BuffDurationType.Combat => "Battle",
                                FoodData.BuffDurationType.Floor => "Floor",
                                _ => b.buffDurationType.ToString()
                            };
                            string bColor = b.buffValue >= 0 ? "#66CCFF" : "#FF7777";
                            sb.Append($"<color={bColor}>[{b.buff.buffName} {sign}{b.buffValue} ({b.remainingDuration} {durType})]</color> ");
                        }
                    }
                    buffsText.text = sb.ToString().TrimEnd();
                }
            }
        }
    }

    public void UpdateActionButtons()
    {
        if (combatManager == null || playerStats == null) return;

        bool isPlayerTurn = combatManager.isCombatActive && !combatManager.isPreparingCombat && combatManager.currentTurn == CombatManager.Turn.Player;
        bool hasAP = playerStats.currentAP > 0;
        bool isAlive = playerStats.currentHealth > 0;

        if (isPlayerTurn == cachedIsPlayerTurn && hasAP == cachedHasAP && isAlive == cachedIsAlive)
            return;

        cachedIsPlayerTurn = isPlayerTurn;
        cachedHasAP = hasAP;
        cachedIsAlive = isAlive;

        bool canAct = isPlayerTurn && hasAP && isAlive;
        if (attackButton != null) attackButton.interactable = canAct;
        if (defendButton != null) defendButton.interactable = canAct;
        if (eatButton != null) eatButton.interactable = canAct;
        if (endTurnButton != null) endTurnButton.interactable = isPlayerTurn && isAlive;
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
                    ? $"AMBUSH! ENEMIES ATTACK IN {secs}s"
                    : $"PREPARING COMBAT ({secs}s)";
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
                    turnBannerText.text = "CAMPFIRE - REST & RECOVER";
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
                turnBannerText.text = "SAFE (NO ENEMIES)";
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
            turnBannerText.text = isPlayer ? "YOUR TURN (PLAYER TURN)" : "ENEMY TURN";
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
            targetNameText.text = $"TARGET: {enemy.enemyData.enemyName}";
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
            int defValue = (enemy.enemyData != null) ? Mathf.Max(5, enemy.enemyData.DEF) : 5;
            string buffSkillInfo = "";
            string debuffSkillInfo = "";
            if (enemy.enemyData != null && enemy.enemyData.intentEffects != null)
            {
                foreach (var eff in enemy.enemyData.intentEffects)
                {
                    if (eff == null || eff.effect == null) continue;
                    string effName = !string.IsNullOrEmpty(eff.effect.buffName) ? eff.effect.buffName : eff.effect.buffType.ToString();
                    if (eff.target == EnemyData.EffectTarget.Player)
                    {
                        int v = (eff.effect.IsDebuff && eff.value > 0) ? -eff.value : eff.value;
                        string sign = v > 0 ? "+" : "";
                        debuffSkillInfo = $" ({sign}{v} {effName})";
                    }
                    else
                    {
                        string sign = eff.value > 0 ? "+" : "";
                        buffSkillInfo = $" ({sign}{eff.value} {effName})";
                    }
                }
            }

            string intentDesc = enemy.currentIntent switch
            {
                EnemyData.EnemyIntent.Attack => $"<color=#FF7777>Attack ({enemy.GetCurrentATK()} DMG)</color>",
                EnemyData.EnemyIntent.Defend => $"<color=#77B5FE>Defend (+{defValue} DEF)</color>",
                EnemyData.EnemyIntent.Buff => $"<color=#FFD700>Buff{buffSkillInfo}</color>",
                EnemyData.EnemyIntent.Debuff => $"<color=#DA70D6>Debuff{debuffSkillInfo}</color>",
                _ => "<color=#AAAAAA>Attack</color>"
            };

            string activeBuffsDesc = "";
            if (enemy.activeBuffs != null && enemy.activeBuffs.Count > 0)
            {
                System.Text.StringBuilder ebSb = new("\n<size=10><color=#B0BEC5>Effects: ");
                for (int i = 0; i < enemy.activeBuffs.Count; i++)
                {
                    ActiveBuff eb = enemy.activeBuffs[i];
                    if (eb != null && eb.buff != null)
                    {
                        string sign = eb.buffValue > 0 ? "+" : "";
                        string ebColor = eb.buffValue >= 0 ? "#66CCFF" : "#FF7777";
                        ebSb.Append($"<color={ebColor}>[{sign}{eb.buffValue} {eb.buff.buffType} ({eb.remainingDuration}T)]</color> ");
                    }
                }
                ebSb.Append("</color></size>");
                activeBuffsDesc = ebSb.ToString();
            }

            targetIntentText.text = $"Intent: {intentDesc}{activeBuffsDesc}";
        }
    }

    public void UpdateStageProgress()
    {
        if (stageProgressText != null && waveManager != null)
        {
            int current = waveManager.GetCurrentWaveIndex() + 1;
            stageProgressText.text = $"Stage: {current}";
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
    #endregion

    #region Action Button Handlers
    public void OnAttackClicked()
    {
        if (combatManager == null) return;

        if (targetSelector != null && (targetSelector.selectedEnemy == null || targetSelector.selectedEnemy.currentHealth <= 0))
        {
            targetSelector.AutoSelectTarget(combatManager.enemies);
        }

        if (targetSelector == null || targetSelector.selectedEnemy == null)
        {
            LogMessage("No enemy in target!");
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

    #region Food Selection Panel
    public void PopulateFoodPanel()
    {
        if (foodListContainer == null) return;

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
        List<ItemSlot> foodSlots = new();

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
                noFoodText.text = "No food in backpack!\n(Cook meals before entering Dungeon)";
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
        GameObject row = new($"Food_{food.itemName}", typeof(RectTransform), typeof(Image));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, 50);

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        GameObject iconGO = new("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(row.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(30, 0);
        iconRT.sizeDelta = new Vector2(36, 36);
        Image iconImg = iconGO.GetComponent<Image>();
        if (food.itemIcon != null) iconImg.sprite = food.itemIcon;

        GameObject labelGO = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(60, 5);
        labelRT.offsetMax = new Vector2(-90, -5);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 13;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = $"<b>{food.itemName}</b> x{count}\n<size=11><color=#88FF88>+{food.healthValue} HP</color> | <color=#FFAA44>+{food.hungerValue} Full</color></size>";

        GameObject btnGO = new("EatBtn", typeof(RectTransform), typeof(Image), typeof(Button));
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

        GameObject btnTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.fontSize = 11;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.text = "EAT (1 AP)";

        return row;
    }
    #endregion

    #region Victory, Defeat & Flow Screens
    public void ShowVictoryScreen(string details = "")
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryText != null)
            {
                victoryText.text = string.IsNullOrEmpty(details)
                    ? "DUNGEON VICTORY!\n\nYou cleared the dungeon and defeated the Boss!\nAll loot and crops have been secured."
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
                defeatText.text = "DEFEATED IN BATTLE!\n\nYou were defeated in the dungeon.\nLoot collected during this expedition has been lost.";
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
        OnReturnToBaseClicked();
    }

    public void ShowRewardScreen(string rewardDesc)
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            if (rewardDescriptionText != null)
            {
                rewardDescriptionText.text = $"TREASURE ROOM!\n\n{rewardDesc}";
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
