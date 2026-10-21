using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public ScrollRect foodScrollRect;
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
    private EnemyStats currentlyBoundTarget = null;

    private Camera cachedCamera;
    private Camera MainCamera
    {
        get
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            return cachedCamera;
        }
    }
    private static readonly Collider2D[] ClickHitBuffer = new Collider2D[16];
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

        EnsureFoodPanelScrollRect();
        if (foodPanel != null) foodPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);

        ApplyFigmaGOTYStyling();
        RefreshAll();
    }

    private void ApplyFigmaGOTYStyling()
    {
        // 1. Action Buttons
        if (attackButton != null)
            DungeonUIAssetHelper.StyleTactileButton(attackButton, attackButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.Attack);
        if (defendButton != null)
            DungeonUIAssetHelper.StyleTactileButton(defendButton, defendButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.Defend);
        if (eatButton != null)
            DungeonUIAssetHelper.StyleTactileButton(eatButton, eatButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.Eat);
        if (endTurnButton != null)
            DungeonUIAssetHelper.StyleTactileButton(endTurnButton, endTurnButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.EndTurn);

        // 2. Secondary & Flow Buttons
        if (closeFoodPanelButton != null)
            DungeonUIAssetHelper.StyleTactileButton(closeFoodPanelButton, closeFoodPanelButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        if (victoryReturnButton != null)
            DungeonUIAssetHelper.StyleTactileButton(victoryReturnButton, victoryReturnButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.Victory);
        if (defeatReturnButton != null)
            DungeonUIAssetHelper.StyleTactileButton(defeatReturnButton, defeatReturnButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.WoodRustic);
        if (rewardClaimButton != null)
            DungeonUIAssetHelper.StyleTactileButton(rewardClaimButton, rewardClaimButton.GetComponent<Image>(), DungeonUIAssetHelper.ButtonStyle.PrimaryCook);

        // 3. Status Bar Fills
        if (hpFillImage != null)
        {
            Sprite hpSprite = DungeonUIAssetHelper.GetHpFillSprite();
            if (hpSprite != null) { hpFillImage.sprite = hpSprite; hpFillImage.type = Image.Type.Sliced; hpFillImage.color = Color.white; }
        }
        if (hungerFillImage != null)
        {
            Sprite hungerSprite = DungeonUIAssetHelper.GetHungerFillSprite();
            if (hungerSprite != null) { hungerFillImage.sprite = hungerSprite; hungerFillImage.type = Image.Type.Sliced; hungerFillImage.color = Color.white; }
        }
        if (targetHpFill != null)
        {
            Sprite hpSprite = DungeonUIAssetHelper.GetHpFillSprite();
            if (hpSprite != null) { targetHpFill.sprite = hpSprite; targetHpFill.type = Image.Type.Sliced; targetHpFill.color = Color.white; }
        }

        // 4. Modals & Panels
        if (targetInfoPanel != null)
        {
            Image panelImg = targetInfoPanel.GetComponent<Image>();
            if (panelImg != null) DungeonUIAssetHelper.StyleSlicedPanel(panelImg, DungeonUIAssetHelper.PanelStyle.Wood);
        }
        if (foodPanel != null)
        {
            Image panelImg = foodPanel.GetComponent<Image>();
            if (panelImg != null) DungeonUIAssetHelper.StyleSlicedPanel(panelImg, DungeonUIAssetHelper.PanelStyle.Wood);
        }
        if (victoryPanel != null)
        {
            Image panelImg = victoryPanel.GetComponent<Image>();
            if (panelImg != null) DungeonUIAssetHelper.StyleSlicedPanel(panelImg, DungeonUIAssetHelper.PanelStyle.Wood);
        }
        if (defeatPanel != null)
        {
            Image panelImg = defeatPanel.GetComponent<Image>();
            if (panelImg != null) DungeonUIAssetHelper.StyleSlicedPanel(panelImg, DungeonUIAssetHelper.PanelStyle.Wood);
        }
        if (rewardPanel != null)
        {
            Image panelImg = rewardPanel.GetComponent<Image>();
            if (panelImg != null) DungeonUIAssetHelper.StyleSlicedPanel(panelImg, DungeonUIAssetHelper.PanelStyle.Wood);
        }
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
            CheckMouseClickEnemy();
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
            combatManager.OnPlayerActingChanged += HandlePlayerActingChanged;
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
            combatManager.OnPlayerActingChanged -= HandlePlayerActingChanged;
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

        if (currentlyBoundTarget != null)
        {
            currentlyBoundTarget.OnIntentChanged -= HandleTargetIntentChanged;
            currentlyBoundTarget = null;
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
        // When wave victory / continue panel is active, Space or Enter triggers Continue
        if (rewardPanel != null && rewardPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnRewardClaimClicked();
                return;
            }
        }

        // Target cycling is allowed whenever combat is active or preparing
        if (targetSelector != null && combatManager != null && combatManager.enemies != null && combatManager.enemies.Length > 1)
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                targetSelector.CycleTarget(combatManager.enemies, !reverse);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                targetSelector.CycleTarget(combatManager.enemies, forward: false);
            }
        }

        bool isAtEntrance = waveManager != null && waveManager.isAtEntrance;
        bool isPlayerAlive = playerStats != null && playerStats.currentHealth > 0;
        bool isActing = combatManager != null && combatManager.isPlayerActing;

        // Allow toggling food panel at dungeon entrance or during player turn
        if ((isAtEntrance || (combatManager != null && combatManager.isCombatActive && !combatManager.isPreparingCombat && combatManager.currentTurn == CombatManager.Turn.Player))
            && isPlayerAlive && !isActing)
        {
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                OnEatClicked();
                return;
            }
        }

        // Close food panel on Escape
        if (foodPanel != null && foodPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                foodPanel.SetActive(false);
                return;
            }
            // Do not trigger attack/defend/end turn while browsing food
            return;
        }

        bool canUseShortcuts = combatManager != null && combatManager.isCombatActive && !combatManager.isPreparingCombat && combatManager.currentTurn == CombatManager.Turn.Player && isPlayerAlive && !isActing;
        if (!canUseShortcuts) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            OnAttackClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            OnDefendClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnEndTurnClicked();
        }
    }

    private void CheckMouseClickEnemy()
    {
        if (targetSelector == null) return;
        Camera cam = MainCamera;
        if (cam == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreen);

        // 1. Direct point collider check (zero alloc)
        int hitCount = Physics2D.OverlapPoint(mouseWorldPos, default, ClickHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (ClickHitBuffer[i] == null) continue;
            EnemyStats enemy = ClickHitBuffer[i].GetComponentInParent<EnemyStats>();
            if (enemy != null && enemy.currentHealth > 0)
            {
                targetSelector.SelectEnemy(enemy);
                return;
            }
        }

        // 2. Generous circle area check around mouse position (1.5 units, zero alloc)
        int circleCount = Physics2D.OverlapCircle(mouseWorldPos, 1.5f, default, ClickHitBuffer);
        EnemyStats nearestEnemy = null;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < circleCount; i++)
        {
            if (ClickHitBuffer[i] == null) continue;
            EnemyStats enemy = ClickHitBuffer[i].GetComponentInParent<EnemyStats>();
            if (enemy != null && enemy.currentHealth > 0)
            {
                float dist = Vector2.Distance(mouseWorldPos, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestEnemy = enemy;
                }
            }
        }

        if (nearestEnemy != null)
        {
            targetSelector.SelectEnemy(nearestEnemy);
            return;
        }

        // 3. Direct distance fallback against all known combat enemies (within 1.8 units)
        if (combatManager != null && combatManager.enemies != null)
        {
            for (int i = 0; i < combatManager.enemies.Length; i++)
            {
                EnemyStats e = combatManager.enemies[i];
                if (e != null && e.currentHealth > 0)
                {
                    float dist = Vector2.Distance(mouseWorldPos, e.transform.position);
                    if (dist < 1.8f && dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestEnemy = e;
                    }
                }
            }
            if (nearestEnemy != null)
            {
                targetSelector.SelectEnemy(nearestEnemy);
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
    private bool? cachedIsActing = null;
    private bool? cachedCanFreeEat = null;

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
        cachedIsActing = null;
        cachedCanFreeEat = null;
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
                                FoodData.BuffDurationType.Turn => b.remainingDuration == 1 ? "Turn" : "Turns",
                                FoodData.BuffDurationType.Combat => b.remainingDuration == 1 ? "Battle" : "Battles",
                                FoodData.BuffDurationType.Floor => b.remainingDuration == 1 ? "Floor" : "Floors",
                                _ => b.buffDurationType.ToString()
                            };
                            string bColor = b.buffValue >= 0 ? "#66CCFF" : "#FF7777";
                            string statLabel = GetBuffStatLabel(b.buff);
                            sb.Append($"<color={bColor}>[{sign}{b.buffValue} {statLabel} ({b.remainingDuration} {durType})]</color> ");
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
        bool isActing = combatManager.isPlayerActing;
        bool canFreeEat = isPlayerTurn && combatManager.CanPlayerEatFree();

        if (isPlayerTurn == cachedIsPlayerTurn && hasAP == cachedHasAP && isAlive == cachedIsAlive && isActing == cachedIsActing && canFreeEat == cachedCanFreeEat)
            return;

        cachedIsPlayerTurn = isPlayerTurn;
        cachedHasAP = hasAP;
        cachedIsAlive = isAlive;
        cachedIsActing = isActing;
        cachedCanFreeEat = canFreeEat;

        bool isAtEntrance = waveManager != null && waveManager.isAtEntrance;
        bool canAct = isPlayerTurn && hasAP && isAlive && !isActing;
        bool canEat = isAlive && !isActing && (canAct || (isPlayerTurn && canFreeEat) || isAtEntrance);

        if (attackButton != null) attackButton.interactable = canAct;
        if (defendButton != null) defendButton.interactable = canAct;
        if (eatButton != null) eatButton.interactable = canEat;
        if (endTurnButton != null) endTurnButton.interactable = isPlayerTurn && isAlive && !isActing;
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
                DungeonUIAssetHelper.StylePlaque(turnBannerBg, combatManager.isAmbushCurrent
                    ? DungeonUIAssetHelper.PlaqueStyle.Defeat
                    : DungeonUIAssetHelper.PlaqueStyle.Gold);
            }
            return;
        }

        if (!combatManager.isCombatActive)
        {
            if (waveManager != null && waveManager.isAtEntrance)
            {
                if (turnBannerText != null)
                {
                    int floor = ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1;
                    turnBannerText.text = $"DUNGEON ENTRANCE - FLOOR {floor} (LOBBY)";
                    turnBannerText.color = new Color(1f, 0.88f, 0.35f);
                }
                if (turnBannerBg != null)
                {
                    DungeonUIAssetHelper.StylePlaque(turnBannerBg, DungeonUIAssetHelper.PlaqueStyle.Gold);
                }
                return;
            }

            if (CampUI.Instance != null && CampUI.Instance.camp != null && CampUI.Instance.camp.isCampOpen)
            {
                if (turnBannerText != null)
                {
                    turnBannerText.text = "CAMPFIRE - REST & RECOVER";
                    turnBannerText.color = new Color(1f, 0.85f, 0.3f);
                }
                if (turnBannerBg != null)
                {
                    DungeonUIAssetHelper.StylePlaque(turnBannerBg, DungeonUIAssetHelper.PlaqueStyle.Wood);
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
                DungeonUIAssetHelper.StylePlaque(turnBannerBg, DungeonUIAssetHelper.PlaqueStyle.Victory);
            }
            return;
        }

        bool isPlayer = combatManager.currentTurn == CombatManager.Turn.Player;
        if (turnBannerText != null)
        {
            turnBannerText.text = isPlayer ? "YOUR TURN (PLAYER TURN)" : "ENEMY TURN";
            turnBannerText.color = isPlayer ? new Color(1f, 0.95f, 0.45f) : new Color(1f, 0.4f, 0.4f);
        }

        if (turnBannerBg != null)
        {
            DungeonUIAssetHelper.StylePlaque(turnBannerBg, isPlayer
                ? DungeonUIAssetHelper.PlaqueStyle.Gold
                : DungeonUIAssetHelper.PlaqueStyle.Defeat);
        }
    }

    private void HandleTargetIntentChanged(EnemyStats enemy, EnemyData.EnemyIntent intent)
    {
        if (targetSelector != null && targetSelector.selectedEnemy == enemy)
        {
            UpdateTargetInfo(enemy);
        }
    }

    public void UpdateTargetInfo(EnemyStats enemy)
    {
        if (targetInfoPanel == null) return;

        if (currentlyBoundTarget != enemy)
        {
            if (currentlyBoundTarget != null)
            {
                currentlyBoundTarget.OnIntentChanged -= HandleTargetIntentChanged;
            }
            currentlyBoundTarget = enemy;
            if (currentlyBoundTarget != null)
            {
                currentlyBoundTarget.OnIntentChanged += HandleTargetIntentChanged;
            }
        }

        if (enemy == null || enemy.currentHealth <= 0)
        {
            targetInfoPanel.SetActive(false);
            return;
        }

        targetInfoPanel.SetActive(true);

        if (targetNameText != null)
        {
            if (enemy is Boss boss)
            {
                targetNameText.text = $"TARGET: <color=#FF5533>[BOSS]</color> {boss.bossName} (Phase {boss.GetCurrentPhase()})";
            }
            else
            {
                targetNameText.text = $"TARGET: {enemy.enemyData?.enemyName ?? "Enemy"}";
            }
        }

        int maxHp = enemy.enemyData != null ? enemy.enemyData.maxHealth : 1;
        if (targetHpFill != null)
        {
            targetHpFill.fillAmount = maxHp > 0 ? (float)enemy.currentHealth / maxHp : 0f;
        }

        if (targetHpText != null)
        {
            targetHpText.text = $"HP: {enemy.currentHealth} / {maxHp}";
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
            if (waveManager.isAtEntrance)
            {
                int total = waveManager.GetTotalStages();
                stageProgressText.text = total > 0 ? $"Entrance (0/{total})" : "Entrance";
            }
            else
            {
                int current = waveManager.GetCurrentWaveIndex() + 1;
                int total = waveManager.GetTotalStages();
                stageProgressText.text = total > 0 ? $"Stage: {current}/{total}" : $"Stage: {current}";
            }
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

    private void HandlePlayerActingChanged(bool acting)
    {
        UpdateActionButtons();
    }
    #endregion

    #region Action Button Handlers
    public void OnAttackClicked()
    {
        if (combatManager == null || combatManager.isPlayerActing) return;

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
        if (combatManager == null || combatManager.isPlayerActing) return;
        combatManager.PlayerDefend();
        RefreshAll();
    }

    public void OnEatClicked()
    {
        if (foodPanel == null || (combatManager != null && combatManager.isPlayerActing)) return;

        bool isOpen = foodPanel.activeSelf;
        foodPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            PopulateFoodPanel();
        }
    }

    public void OnEndTurnClicked()
    {
        if (combatManager == null || combatManager.isPlayerActing) return;
        if (foodPanel != null) foodPanel.SetActive(false);
        combatManager.EndTurnPlayer();
        RefreshAll();
    }
    #endregion

    #region Food Selection Panel
    private void EnsureFoodPanelScrollRect()
    {
        if (foodListContainer == null) return;

        if (foodScrollRect != null && foodScrollRect.verticalScrollbar != null)
        {
            return;
        }

        foodScrollRect = foodListContainer.GetComponentInParent<ScrollRect>();
        if (foodScrollRect != null)
        {
            if (foodScrollRect.verticalScrollbar == null)
            {
                DungeonUIAssetHelper.CreateVerticalScrollbar(foodScrollRect.transform, foodScrollRect, new Color(0.85f, 0.65f, 0.2f, 1f));
            }
            return;
        }

        // Automatically wrap foodListContainer inside a standard ScrollView with Mask and vertical scrollbar
        Transform origParent = foodListContainer.parent;
        int siblingIdx = foodListContainer.GetSiblingIndex();

        GameObject scrollGO = new("FoodScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(origParent, false);
        scrollGO.transform.SetSiblingIndex(siblingIdx);

        RectTransform origRT = foodListContainer.GetComponent<RectTransform>();
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        if (origRT != null)
        {
            scrollRT.anchorMin = origRT.anchorMin;
            scrollRT.anchorMax = origRT.anchorMax;
            scrollRT.pivot = origRT.pivot;
            scrollRT.anchoredPosition = origRT.anchoredPosition;
            scrollRT.sizeDelta = origRT.sizeDelta;
        }
        else
        {
            scrollRT.anchorMin = new Vector2(0.05f, 0.18f);
            scrollRT.anchorMax = new Vector2(0.95f, 0.84f);
            scrollRT.sizeDelta = Vector2.zero;
        }

        foodScrollRect = scrollGO.GetComponent<ScrollRect>();
        foodScrollRect.horizontal = false;
        foodScrollRect.vertical = true;
        foodScrollRect.scrollSensitivity = 25f;
        foodScrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject vpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-16, 0); // Reserve 16px for vertical scrollbar on the right

        foodListContainer.SetParent(vpGO.transform, false);
        if (origRT != null)
        {
            origRT.anchorMin = new Vector2(0, 1);
            origRT.anchorMax = new Vector2(1, 1);
            origRT.pivot = new Vector2(0.5f, 1);
            origRT.anchoredPosition = Vector2.zero;
            origRT.sizeDelta = new Vector2(0, 0);
        }

        VerticalLayoutGroup vlg = foodListContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = foodListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter csf = foodListContainer.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = foodListContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foodScrollRect.viewport = vpRT;
        foodScrollRect.content = origRT;

        // Create vertical scrollbar with sleek golden styling
        DungeonUIAssetHelper.CreateVerticalScrollbar(scrollGO.transform, foodScrollRect, new Color(0.85f, 0.65f, 0.2f, 1f));
    }

    public void PopulateFoodPanel()
    {
        EnsureFoodPanelScrollRect();
        if (foodListContainer == null) return;

        for (int i = 0; i < activeFoodButtons.Count; i++)
        {
            if (activeFoodButtons[i] != null)
            {
                Destroy(activeFoodButtons[i]);
            }
        }
        activeFoodButtons.Clear();

        if (playerStats == null) FindReferences();
        if (playerStats != null && playerStats.itemContainer == null)
        {
            playerStats.ResolveItemContainer();
        }

        ItemContainer inventory = playerStats != null ? playerStats.itemContainer : null;
        if (inventory == null)
        {
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null && invPanel.ItemContainer != null) inventory = invPanel.ItemContainer;
        }

        if (inventory == null)
        {
            if (noFoodText != null) noFoodText.gameObject.SetActive(true);
            return;
        }

        List<ItemSlot> foodSlots = new();

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
                noFoodText.text = "No food or crops in inventory!";
            }
            return;
        }

        if (noFoodText != null) noFoodText.gameObject.SetActive(false);

        for (int i = 0; i < foodSlots.Count; i++)
        {
            ItemSlot slot = foodSlots[i];
            ItemData food = slot.itemData;
            int count = slot.amount;

            GameObject rowGO = null;
            if (foodItemPrefab != null)
            {
                rowGO = Instantiate(foodItemPrefab, foodListContainer);
                Button btn = rowGO.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        if (combatManager != null && !combatManager.isPlayerActing)
                        {
                            combatManager.PlayerEat(food);
                            RefreshAll();
                            PopulateFoodPanel();
                        }
                    });
                }
                BindPrefabFoodData(rowGO, food, count);
            }
            else
            {
                rowGO = CreateCombatFoodCard(food, count);
            }

            if (rowGO != null)
            {
                activeFoodButtons.Add(rowGO);
            }
        }

        if (foodScrollRect != null)
        {
            foodScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private GameObject CreateCombatFoodCard(ItemData food, int count)
    {
        if (food == null || foodListContainer == null) return null;

        bool hasRawVitality = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.RawVitality);
        bool hasEndlessFeast = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast);

        string statInfo;
        string buffInfo = "";
        int buffCount = 0;

        if (food is FoodData fd)
        {
            statInfo = $"<color=#66FF66>+{fd.healthValue} HP</color> | <color=#FFAA33>+{fd.hungerValue} Fullness</color>";

            if (fd.foodBuff != null && fd.foodBuff.Length > 0)
            {
                List<string> bList = new();
                for (int b = 0; b < fd.foodBuff.Length; b++)
                {
                    Foodbuff fb = fd.foodBuff[b];
                    if (fb != null && fb.buffs != null)
                    {
                        string token = FormatFoodBuff(fb, hasEndlessFeast);
                        if (!string.IsNullOrEmpty(token))
                        {
                            bList.Add(token);
                            buffCount++;
                        }
                    }
                }
                if (bList.Count > 0)
                {
                    buffInfo = string.Join(" | ", bList);
                }
            }
        }
        else
        {
            int hp = hasRawVitality ? 12 : 6;
            int hunger = hasRawVitality ? 20 : 15;
            string bonus = hasRawVitality ? " <color=#A8E6CF>(Raw Vitality)</color>" : " <color=#88CCFF>(Raw Crop)</color>";
            statInfo = $"<color=#66FF66>+{hp} HP</color> | <color=#FFAA33>+{hunger} Fullness</color>{bonus}";
        }

        float rowHeight = buffCount >= 3 ? 82f : (buffCount > 0 ? 66f : 52f);

        GameObject row = new GameObject($"Food_{food.itemName}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(foodListContainer, false);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, rowHeight);

        LayoutElement le = row.GetComponent<LayoutElement>();
        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        le.flexibleWidth = 1;

        Image bg = row.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.14f, 0.17f, 0.24f, 0.95f));

        // Icon
        if (food.itemIcon != null)
        {
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(row.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0, 0.5f);
            iconRT.anchorMax = new Vector2(0, 0.5f);
            iconRT.pivot = new Vector2(0, 0.5f);
            iconRT.anchoredPosition = new Vector2(8, 0);
            float iconSize = buffCount > 0 ? 40f : 36f;
            iconRT.sizeDelta = new Vector2(iconSize, iconSize);
            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = food.itemIcon;
            iconImg.preserveAspect = true;
        }

        // Label Info
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(54, 4);
        labelRT.offsetMax = new Vector2(-88, -4);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 12;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.lineSpacing = -8;

        if (!string.IsNullOrEmpty(buffInfo))
        {
            string feastTag = hasEndlessFeast ? " <color=#FFAAEE>[Feast: 2x Dur]</color>" : "";
            label.text = $"<b>{food.itemName}</b> <color=#FFD700>x{count}</color>\n<size=10.5>{statInfo}</size>\n<size=9.5><b>Buff:</b> {buffInfo}{feastTag}</size>";
        }
        else if (food is not FoodData && hasRawVitality)
        {
            label.text = $"<b>{food.itemName}</b> <color=#FFD700>x{count}</color>\n<size=10.5>{statInfo}</size>\n<size=9.5><color=#A8E6CF>Passive: +100% HP & +33% Fullness</color></size>";
        }
        else
        {
            label.text = $"<b>{food.itemName}</b> <color=#FFD700>x{count}</color>\n<size=10.5>{statInfo}</size>";
        }

        // Eat Button
        GameObject btnGO = new GameObject("EatBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(row.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.pivot = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = new Vector2(-8, 0);
        btnRT.sizeDelta = new Vector2(75, 34);

        Image btnBg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnBg, new Color(0.2f, 0.55f, 0.28f, 1f));

        Button btn = btnGO.GetComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(() =>
        {
            if (combatManager != null && !combatManager.isPlayerActing)
            {
                combatManager.PlayerEat(food);
                RefreshAll();
                PopulateFoodPanel();
            }
        });

        GameObject btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.text = "EAT";
        btnText.fontSize = 13;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        return row;
    }

    private void BindPrefabFoodData(GameObject rowGO, ItemData food, int count)
    {
        if (rowGO == null || food == null) return;

        bool hasRawVitality = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.RawVitality);
        bool hasEndlessFeast = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast);

        string statInfo;
        string buffInfo = "";
        if (food is FoodData fd)
        {
            statInfo = $"<color=#66FF66>+{fd.healthValue} HP</color> | <color=#FFAA33>+{fd.hungerValue} Fullness</color>";
            if (fd.foodBuff != null && fd.foodBuff.Length > 0)
            {
                List<string> bList = new();
                for (int b = 0; b < fd.foodBuff.Length; b++)
                {
                    Foodbuff fb = fd.foodBuff[b];
                    if (fb != null && fb.buffs != null)
                    {
                        string token = FormatFoodBuff(fb, hasEndlessFeast);
                        if (!string.IsNullOrEmpty(token)) bList.Add(token);
                    }
                }
                if (bList.Count > 0) buffInfo = string.Join(" | ", bList);
            }
        }
        else
        {
            int hp = hasRawVitality ? 12 : 6;
            int hunger = hasRawVitality ? 20 : 15;
            string bonus = hasRawVitality ? " (Raw Vitality)" : " (Raw Crop)";
            statInfo = $"<color=#66FF66>+{hp} HP</color> | <color=#FFAA33>+{hunger} Fullness</color>{bonus}";
        }

        var tmpTexts = rowGO.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmpTexts != null && tmpTexts.Length > 0)
        {
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                string n = tmpTexts[i].gameObject.name.ToLower();
                if (n.Contains("name") || n.Contains("title"))
                {
                    tmpTexts[i].text = $"{food.itemName} x{count}";
                }
                else if (n.Contains("stat") || n.Contains("desc") || n.Contains("info") || n.Contains("buff"))
                {
                    tmpTexts[i].text = !string.IsNullOrEmpty(buffInfo) ? $"{statInfo}\n<b>Buff:</b> {buffInfo}" : statInfo;
                }
            }
        }

        var images = rowGO.GetComponentsInChildren<Image>(true);
        if (images != null)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name.ToLower().Contains("icon") && food.itemIcon != null)
                {
                    images[i].sprite = food.itemIcon;
                    break;
                }
            }
        }
    }

    public static string GetBuffStatLabel(StatEffectData buff)
    {
        if (buff == null) return "Buff";

        string stat = buff.buffType switch
        {
            StatEffectData.BuffType.Attack => "ATK",
            StatEffectData.BuffType.Defense => "DEF",
            StatEffectData.BuffType.Speed => "SPD",
            StatEffectData.BuffType.Health => "Max HP",
            _ => buff.buffType.ToString()
        };

        if (!string.IsNullOrEmpty(buff.buffName))
        {
            string raw = buff.buffName.Trim();
            if (raw.IndexOf("ATK", System.StringComparison.OrdinalIgnoreCase) >= 0) return "ATK";
            if (raw.IndexOf("DEF", System.StringComparison.OrdinalIgnoreCase) >= 0) return "DEF";
            if (raw.IndexOf("Speed", System.StringComparison.OrdinalIgnoreCase) >= 0 || raw.IndexOf("SPD", System.StringComparison.OrdinalIgnoreCase) >= 0) return "SPD";
            if (raw.IndexOf("Health", System.StringComparison.OrdinalIgnoreCase) >= 0 || raw.IndexOf("HP", System.StringComparison.OrdinalIgnoreCase) >= 0 || raw.IndexOf("M\u00e1u", System.StringComparison.OrdinalIgnoreCase) >= 0) return "HP";

            if (raw.StartsWith("T\u0103ng ", System.StringComparison.OrdinalIgnoreCase) || raw.StartsWith("Tang ", System.StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(5).Trim();
            else if (raw.StartsWith("Gi\u1ea3m ", System.StringComparison.OrdinalIgnoreCase) || raw.StartsWith("Giam ", System.StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(5).Trim();
            else if (raw.StartsWith("Increase ", System.StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(9).Trim();
            else if (raw.StartsWith("Decrease ", System.StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(9).Trim();

            if (!string.IsNullOrEmpty(raw)) stat = raw;
        }

        return stat;
    }

    public static string FormatFoodBuff(Foodbuff fb, bool hasEndlessFeast)
    {
        if (fb == null || fb.buffs == null) return null;

        string stat = GetBuffStatLabel(fb.buffs);
        int val = fb.buffValue;
        if (fb.buffs.IsDebuff && val > 0) val = -val;

        string sign = val > 0 ? "+" : "";
        int duration = fb.buffDuration;
        if (hasEndlessFeast) duration *= 2;

        string durUnit = fb.buffDurationType switch
        {
            FoodData.BuffDurationType.Turn => duration == 1 ? "Turn" : "Turns",
            FoodData.BuffDurationType.Combat => duration == 1 ? "Battle" : "Battles",
            FoodData.BuffDurationType.Floor => duration == 1 ? "Floor" : "Floors",
            _ => fb.buffDurationType.ToString()
        };

        string colorHex = val >= 0 ? "#55DDFF" : "#FF7777";
        return $"<color={colorHex}>{sign}{val} {stat} ({duration} {durUnit})</color>";
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
        DungeonSummaryUI summary = DungeonSummaryUI.EnsureInstance();
        if (summary != null)
        {
            summary.Show(DungeonSummaryUI.ExpeditionResult.Victory, PerformReturnToBase);
            return;
        }

        PerformReturnToBase();
    }

    public void OnReturnToBaseAfterDefeatClicked()
    {
        DungeonSummaryUI summary = DungeonSummaryUI.EnsureInstance();
        if (summary != null)
        {
            summary.Show(DungeonSummaryUI.ExpeditionResult.Defeat, PerformReturnToBase);
            return;
        }

        PerformReturnToBase();
    }

    private void PerformReturnToBase()
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

    public void ShowRewardScreen(string rewardDesc)
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            rewardPanel.transform.SetAsLastSibling();

            TextMeshProUGUI titleText = rewardPanel.transform.Find("Box/Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = "TREASURE ROOM REWARD!";
            }

            if (rewardDescriptionText != null)
            {
                rewardDescriptionText.text = $"TREASURE ROOM!\n\n{rewardDesc}";
            }
        }
    }

    public void ShowWaveVictoryScreen(int waveIndex, List<ItemSlot> waveLoot)
    {
        if (rewardPanel == null)
        {
            if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null) waveManager.Continue();
            return;
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            rewardPanel.transform.SetAsLastSibling();

            TextMeshProUGUI titleText = rewardPanel.transform.Find("Box/Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = $"WAVE {waveIndex + 1} CLEARED!";
            }

            if (rewardDescriptionText != null)
            {
                System.Text.StringBuilder sb = new();
                sb.AppendLine("<size=18><color=#B0BEC5>All enemies defeated!</color></size>\n");

                if (waveLoot != null && waveLoot.Count > 0)
                {
                    sb.AppendLine("<size=20><color=#FFD700>Loot dropped from monsters:</color></size>");
                    Dictionary<ItemData, int> combined = new();
                    foreach (var slot in waveLoot)
                    {
                        if (slot == null || slot.itemData == null || slot.amount <= 0) continue;
                        if (combined.ContainsKey(slot.itemData)) combined[slot.itemData] += slot.amount;
                        else combined[slot.itemData] = slot.amount;
                    }

                    foreach (var kvp in combined)
                    {
                        sb.AppendLine($"* <color=#FFFFFF>{kvp.Key.itemName}</color> <color=#A8E6CF>x{kvp.Value}</color>");
                    }
                }
                else
                {
                    sb.AppendLine("<color=#888888>(No loot dropped this wave)</color>");
                }

                rewardDescriptionText.text = sb.ToString();
            }

            if (rewardClaimButton != null)
            {
                TextMeshProUGUI btnText = rewardClaimButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = "CONTINUE";
                }
            }
        }
    }

    public void OnRewardClaimClicked()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.Continue();
        }
    }
    #endregion
}
