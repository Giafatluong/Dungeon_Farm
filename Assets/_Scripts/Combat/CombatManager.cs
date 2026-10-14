using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public enum Turn { Player, Enemy }

    #region References
    [Header("References")]
    public PlayerStats playerStats;
    public TurnManager turnManager;
    public SpeedManager speedManager;
    public EnemyTargetSelector targetSelector;
    #endregion

    #region State
    [Header("State")]
    public bool isCombatActive = false;
    public bool isPreparingCombat = false;
    public bool isAmbushCurrent = false;
    public float preparationTimeRemaining = 0f;
    public Turn currentTurn = Turn.Player;
    public EnemyStats[] enemies = new EnemyStats[0];
    public readonly List<ItemSlot> currentWaveLoot = new();
    #endregion

    [Header("Config")]
    public float preparationTime = 3f;
    public float enemyTurnDelay = 1.2f;
    public float playerActionDelay = 0.35f;
    [Tooltip("Thời gian chờ sau khi quái cuối cùng chết trước khi hiện UI kết thúc")]
    public float victoryDelay = 1.0f;

    #region Events
    public event System.Action<string> OnCombatLog;
    public event System.Action<Turn> OnTurnChanged;
    public event System.Action<float> OnCombatPreparationCountdown;
    public event System.Action OnCombatStarted;
    public event System.Action OnCombatEnded;
    public event System.Action<EnemyStats, int> OnPlayerAttackAction;
    public event System.Action<bool> OnPlayerActingChanged;
    #endregion

    public bool isPlayerActing { get; private set; } = false;

    private Coroutine preparationCoroutine;
    private Coroutine enemyTurnCoroutine;
    private Coroutine playerActionDelayCoroutine;
    private Coroutine victoryCoroutine;

    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (turnManager == null) turnManager = GetComponent<TurnManager>();
        if (speedManager == null) speedManager = GetComponent<SpeedManager>();
        if (targetSelector == null) targetSelector = GetComponent<EnemyTargetSelector>();
        if (playerStats == null) playerStats = PlayerStats.Instance ?? FindFirstObjectByType<PlayerStats>();
    }

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance ?? FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        if (enemies != null)
        {
            foreach (var e in enemies)
            {
                if (e != null) e.OnEnemyDeath -= HandleEnemyDeath;
            }
        }

        if (victoryCoroutine != null)
        {
            StopCoroutine(victoryCoroutine);
            victoryCoroutine = null;
        }

        if (Instance == this) Instance = null;
    }

    private void HandlePlayerDeath()
    {
        EndCombat(victory: false);
    }

    #region Setup

    public void SetEnemies(EnemyStats[] newEnemies)
    {
        if (enemies != null)
        {
            foreach (var e in enemies)
            {
                if (e != null) e.OnEnemyDeath -= HandleEnemyDeath;
            }
        }

        enemies = newEnemies ?? new EnemyStats[0];

        foreach (var e in enemies)
        {
            if (e != null)
            {
                e.OnEnemyDeath += HandleEnemyDeath;
                e.hasActedThisRound = false;
                e.SelectNextIntent();
            }
        }

        if (speedManager != null)
        {
            speedManager.SetEnemies(enemies);
        }
    }

    public void StartCombat(bool isAmbush = false)
    {
        if (preparationCoroutine != null) StopCoroutine(preparationCoroutine);
        if (victoryCoroutine != null)
        {
            StopCoroutine(victoryCoroutine);
            victoryCoroutine = null;
        }
        isAmbushCurrent = isAmbush;
        isPreparingCombat = true;
        isCombatActive = false;
        preparationTimeRemaining = preparationTime;
        currentWaveLoot.Clear();

        if (playerStats != null)
        {
            playerStats.defendCount = 0;
        }

        if (enemies != null)
        {
            foreach (var e in enemies)
            {
                if (e != null && e.currentHealth > 0)
                {
                    e.hasActedThisRound = false;
                    e.SelectNextIntent();
                }
            }
        }

        if (turnManager != null)
        {
            turnManager.ResetCombatRounds();
        }

        preparationCoroutine = StartCoroutine(PreparationCountdown(isAmbush));
    }

    private IEnumerator PreparationCountdown(bool isAmbush)
    {
        while (preparationTimeRemaining > 0f)
        {
            preparationTimeRemaining -= Time.deltaTime;
            OnCombatPreparationCountdown?.Invoke(preparationTimeRemaining);
            yield return null;
        }

        preparationTimeRemaining = 0f;
        isPreparingCombat = false;
        isCombatActive = true;

        OnCombatStarted?.Invoke();
        LogMessage("Combat started!");

        if (turnManager != null)
        {
            turnManager.StartRound(isAmbush);
        }
    }

    private bool hasEatenThisTurn = false;

    #endregion

    #region Turn Management

    public void StartPlayerTurn()
    {
        if (playerActionDelayCoroutine != null)
        {
            StopCoroutine(playerActionDelayCoroutine);
            playerActionDelayCoroutine = null;
        }
        SetPlayerActing(false);

        currentTurn = Turn.Player;
        hasEatenThisTurn = false;

        if (playerStats != null)
        {
            playerStats.currentAP = playerStats.maxAP;
            playerStats.defendCount = 0;
        }

        OnTurnChanged?.Invoke(currentTurn);
        LogMessage("Your turn! Choose your action.");
    }

    public void StartEnemyTurn(EnemyStats enemy)
    {
        if (playerActionDelayCoroutine != null)
        {
            StopCoroutine(playerActionDelayCoroutine);
            playerActionDelayCoroutine = null;
        }
        SetPlayerActing(false);

        if (enemy == null || enemy.currentHealth <= 0)
        {
            if (turnManager != null) turnManager.NextTurn();
            return;
        }

        currentTurn = Turn.Enemy;
        OnTurnChanged?.Invoke(currentTurn);
        LogMessage($"{enemy.enemyData?.enemyName ?? "Enemy"} is taking its turn...");

        if (enemyTurnCoroutine != null) StopCoroutine(enemyTurnCoroutine);
        enemyTurnCoroutine = StartCoroutine(EnemyTurnRoutine(enemy));
    }

    private IEnumerator EnemyTurnRoutine(EnemyStats enemy)
    {
        yield return new WaitForSeconds(enemyTurnDelay);

        if (enemy != null && enemy.currentHealth > 0 && playerStats != null && playerStats.currentHealth > 0)
        {
            EnemyData.EnemyIntent actionTaken = enemy.currentIntent;
            enemy.PerformIntentAction(playerStats);
            LogMessage($"{enemy.enemyData?.enemyName ?? "Enemy"} used {actionTaken}!");
        }

        // Cho animation (tan cong / ho tro) cua quai hoan tat truoc khi chuyen turn
        yield return new WaitForSeconds(0.85f);

        if (!isCombatActive || playerStats == null || playerStats.currentHealth <= 0)
        {
            yield break;
        }

        // Reduce enemy turn buffs
        if (enemy != null)
        {
            enemy.ReduceTurnBuffDuration();
        }

        if (turnManager != null)
        {
            turnManager.NextTurn();
        }
    }

    public void EndTurnPlayer()
    {
        if (!isCombatActive || currentTurn != Turn.Player || isPlayerActing) return;

        // Reduce player turn-based buff durations
        if (playerStats != null)
        {
            playerStats.ReduceTurnBuffDuration();
        }

        LogMessage("Turn ended.");

        if (turnManager != null)
        {
            turnManager.EndPlayerTurn();
        }
    }

    public void StartPlayerActionCooldown(float delay = -1f)
    {
        float actualDelay = delay > 0f ? delay : playerActionDelay;
        if (playerActionDelayCoroutine != null)
        {
            StopCoroutine(playerActionDelayCoroutine);
        }
        playerActionDelayCoroutine = StartCoroutine(PlayerActionDelayRoutine(actualDelay));
    }

    private IEnumerator PlayerActionDelayRoutine(float delay)
    {
        SetPlayerActing(true);
        yield return new WaitForSeconds(delay);
        SetPlayerActing(false);
        playerActionDelayCoroutine = null;
    }

    private void SetPlayerActing(bool acting)
    {
        if (isPlayerActing != acting)
        {
            isPlayerActing = acting;
            OnPlayerActingChanged?.Invoke(isPlayerActing);
        }
    }

    #endregion

    #region Player Actions

    public void PlayerAttack()
    {
        if (!isCombatActive || currentTurn != Turn.Player || isPlayerActing) return;
        if (playerStats == null || playerStats.currentAP <= 0) return;

        if (targetSelector == null || targetSelector.selectedEnemy == null)
        {
            LogMessage("No target selected!");
            return;
        }

        EnemyStats target = targetSelector.selectedEnemy;
        if (target.currentHealth <= 0)
        {
            LogMessage("Target is already dead!");
            return;
        }

        StartPlayerActionCooldown();

        int atk = playerStats.GetCurrentATK();

        // Blessing: Động Lực Tốc Độ (SwiftMomentum) - Gây thêm sát thương nếu tốc độ cao hơn địch
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.SwiftMomentum))
        {
            int speedDiff = Mathf.Max(0, playerStats.GetCurrentSpeed() - target.GetCurrentSpeed());
            if (speedDiff > 0)
            {
                atk += speedDiff;
                DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.5f, $"+{speedDiff} Xung Lực Tốc Độ!", new Color(0.2f, 1f, 0.5f), 4.5f);
            }
        }

        // Blessing: Bụng Rỗng Cuồng Nộ (StarvingFury) - Tỷ lệ bạo kích x1.5 sát thương khi đói (<35%)
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.StarvingFury)
            && playerStats.maxHunger > 0 && playerStats.currentHunger <= playerStats.maxHunger * 0.35f)
        {
            if (Random.value < 0.35f)
            {
                atk = Mathf.RoundToInt(atk * 1.5f);
                DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.6f, "BẠO KÍCH ĐÓI CỒN CÀO!", Color.red, 5f);
            }
        }

        target.TakeDamage(atk);
        playerStats.currentAP--;
        OnPlayerAttackAction?.Invoke(target, atk);

        LogMessage($"You attacked {target.enemyData?.enemyName ?? "enemy"} for {atk} damage!");

        // Blessing: Món Đắng Bào Mòn (BitterDecay) - Bào mòn giáp và gây độc trực tiếp lên địch
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.BitterDecay) && target.currentHealth > 0)
        {
            target.TakeDamage(4); // Thêm 4 sát thương độc trực tiếp
            DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.2f, "Độc Bào Mòn! -4 HP", new Color(0.6f, 0.3f, 0.9f), 4.5f);
            LogMessage($"[Blessing: BitterDecay] Gây 4 sát thương độc bào mòn lên {target.enemyData?.enemyName ?? "quái vật"}!");
        }

        if (target.currentHealth <= 0)
        {
            targetSelector.AutoSelectTarget(enemies);
        }

        CheckAllEnemiesDead();
    }

    public void PlayerDefend()
    {
        if (!isCombatActive || currentTurn != Turn.Player || isPlayerActing) return;
        if (playerStats == null || playerStats.currentAP <= 0) return;

        StartPlayerActionCooldown();

        playerStats.Defend();
        playerStats.currentAP--;

        DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.1f, $"+{playerStats.defendValue} DEF", new Color(0.4f, 0.75f, 1f), 4.6f);
        LogMessage($"You take a defensive stance! (+{playerStats.defendValue} shield per stack, {playerStats.defendCount} stack(s))");
    }

    public void PlayerEat(FoodData food)
    {
        if (playerStats == null || food == null || isPlayerActing) return;

        bool isAtEntrance = WaveManager.Instance != null && WaveManager.Instance.isAtEntrance;

        if (!isAtEntrance && (!isCombatActive || currentTurn != Turn.Player)) return;

        bool isFreeEat = false;
        // Blessing: Đại Tiệc Bất Tận (EndlessFeast) - Món ăn đầu tiên trong mỗi lượt không tốn AP
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast) && !hasEatenThisTurn)
        {
            isFreeEat = true;
            hasEatenThisTurn = true;
        }

        if (!isAtEntrance && !isFreeEat && playerStats.currentAP <= 0)
        {
            LogMessage("Not enough AP to eat!");
            return;
        }

        // Attempt to eat the food item
        if (!playerStats.Eat(food))
        {
            if (playerStats.currentHunger >= playerStats.maxHunger)
            {
                LogMessage("Bụng đã quá no, không thể ăn thêm!");
                DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.3f, "BỤNG ĐÃ QUÁ NO!", Color.yellow, 4.5f);
            }
            return;
        }

        StartPlayerActionCooldown();

        if (!isAtEntrance && !isFreeEat)
        {
            playerStats.currentAP--;
        }
        else
        {
            DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.4f, "Đại Tiệc: 0 AP!", new Color(1f, 0.5f, 0.8f), 4.5f);
            LogMessage($"[Blessing: EndlessFeast] Đại Tiệc Bất Tận: Ăn {food.itemName} không tốn AP!");
        }

        LogMessage($"You ate {food.itemName}!");
    }

    /// <summary>
    /// Phản lại sát thương lên quái vật khi người chơi có phước lành No Căng Phản Đòn (GluttonousAegis)
    /// </summary>
    public void ReflectDamageToCurrentEnemy(int reflectedDamage)
    {
        if (reflectedDamage <= 0 || enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].currentHealth > 0)
            {
                enemies[i].TakeDamage(reflectedDamage);
                DamagePopupManager.ShowText(enemies[i].transform.position + Vector3.up * 1.3f, $"Phản {reflectedDamage} DMG!", new Color(0.9f, 0.8f, 0.2f), 4.5f);
                LogMessage($"[Blessing: GluttonousAegis] Phản đòn {reflectedDamage} sát thương lên {enemies[i].enemyData?.enemyName ?? "kẻ địch"}!");
                CheckAllEnemiesDead();
                break;
            }
        }
    }

    #endregion

    #region Enemy Death & Victory

    private void HandleEnemyDeath(EnemyStats deadEnemy)
    {
        if (deadEnemy == null) return;

        LogMessage($"{deadEnemy.enemyData?.enemyName ?? "Enemy"} was defeated!");

        // Drop loot - collect into currentWaveLoot for player to loot via UI upon victory
        List<ItemSlot> drops = deadEnemy.GetLootDrops();
        if ((drops == null || drops.Count == 0) && deadEnemy.enemyData != null && deadEnemy.enemyData.lootDrops != null && deadEnemy.enemyData.lootDrops.Length > 0)
        {
            var fallback = deadEnemy.enemyData.lootDrops[0];
            if (fallback != null && fallback.item != null)
            {
                drops = new List<ItemSlot> { new ItemSlot { itemData = fallback.item, amount = Mathf.Max(1, fallback.minAmount) } };
            }
        }

        if (drops != null)
        {
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i] != null && drops[i].itemData != null && drops[i].amount > 0)
                {
                    bool stacked = false;
                    for (int s = 0; s < currentWaveLoot.Count; s++)
                    {
                        if (currentWaveLoot[s] != null && ItemContainer.IsItemMatch(currentWaveLoot[s].itemData, drops[i].itemData))
                        {
                            currentWaveLoot[s].amount += drops[i].amount;
                            stacked = true;
                            break;
                        }
                    }
                    if (!stacked)
                    {
                        currentWaveLoot.Add(new ItemSlot { itemData = drops[i].itemData, amount = drops[i].amount });
                    }

                    LogMessage($"Rơi vật phẩm: {drops[i].itemData.itemName} x{drops[i].amount}");
                }
            }
        }

        CheckAllEnemiesDead();
    }

    private void CheckAllEnemiesDead()
    {
        if (!isCombatActive || victoryCoroutine != null || enemies == null) return;

        bool anyAlive = false;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].currentHealth > 0)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            if (CombatJuice.Instance != null)
            {
                CombatJuice.Instance.PlayFinisherSlowMo(0.25f, 0.45f);
            }
            victoryCoroutine = StartCoroutine(DelayedVictoryRoutine());
        }
    }

    private IEnumerator DelayedVictoryRoutine()
    {
        // Khóa hành động người chơi trong lúc chờ quái tan biến
        SetPlayerActing(true);

        if (victoryDelay > 0f)
        {
            yield return new WaitForSeconds(victoryDelay);
        }

        victoryCoroutine = null;
        EndCombat(victory: true);
    }

    private void EndCombat(bool victory)
    {
        if (!isCombatActive && !isPreparingCombat) return;

        isCombatActive = false;
        isPreparingCombat = false;

        if (preparationCoroutine != null) StopCoroutine(preparationCoroutine);
        if (enemyTurnCoroutine != null) StopCoroutine(enemyTurnCoroutine);
        if (victoryCoroutine != null)
        {
            StopCoroutine(victoryCoroutine);
            victoryCoroutine = null;
        }
        if (playerActionDelayCoroutine != null)
        {
            StopCoroutine(playerActionDelayCoroutine);
            playerActionDelayCoroutine = null;
        }
        SetPlayerActing(false);

        if (playerStats != null)
        {
            playerStats.OnCombatComplete();
        }

        OnCombatEnded?.Invoke();

        if (victory)
        {
            LogMessage("Victory! All enemies defeated.");

            WaveManager waveManager = WaveManager.Instance ?? FindFirstObjectByType<WaveManager>();
            int waveIdx = waveManager != null ? waveManager.GetCurrentWaveIndex() : 0;

            // Người chơi sẽ kéo các vật phẩm vào inventory chứ không tự động vào inven
            ItemContainer backpack = playerStats != null ? playerStats.itemContainer : null;
            if (backpack == null)
            {
                InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
                if (invPanel != null && invPanel.ItemContainer != null) backpack = invPanel.ItemContainer;
            }
            if (backpack == null)
            {
                ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
                for (int i = 0; i < containers.Length; i++)
                {
                    if (containers[i] != null && !containers[i].name.ToLower().Contains("chest") && !containers[i].name.ToLower().Contains("loot"))
                    {
                        backpack = containers[i];
                        break;
                    }
                }
            }

            bool hasLoot = false;
            if (currentWaveLoot != null)
            {
                for (int i = 0; i < currentWaveLoot.Count; i++)
                {
                    if (currentWaveLoot[i] != null && currentWaveLoot[i].itemData != null && currentWaveLoot[i].amount > 0)
                    {
                        hasLoot = true;
                        break;
                    }
                }
            }

            System.Action onLootFinished = () =>
            {
                if (currentWaveLoot != null) currentWaveLoot.Clear();

                if (waveManager != null && isAmbushCurrent)
                {
                    waveManager.HandlePostAmbushCamp();
                }
                else if (waveManager != null)
                {
                    waveManager.Continue();
                }
            };

            LootUI lootUI = LootUI.EnsureInstance();
            if (hasLoot && lootUI != null && backpack != null)
            {
                if (CombatUI.Instance != null && CombatUI.Instance.rewardPanel != null)
                {
                    CombatUI.Instance.rewardPanel.SetActive(false);
                }

                List<ItemSlot> lootToPass = new(currentWaveLoot);
                currentWaveLoot.Clear();

                lootUI.Open(lootToPass, backpack, onLootFinished);
            }
            else
            {
                if (currentWaveLoot != null) currentWaveLoot.Clear();

                if (waveManager != null && isAmbushCurrent)
                {
                    waveManager.HandlePostAmbushCamp();
                }
                else if (CombatUI.Instance != null && CombatUI.Instance.rewardPanel != null)
                {
                    CombatUI.Instance.ShowWaveVictoryScreen(waveIdx, null);
                }
                else if (waveManager != null)
                {
                    waveManager.Continue();
                }
            }
        }
        else
        {
            LogMessage("Defeated...");
        }
    }

    #endregion

    #region Utility

    public void LogMessage(string message)
    {
        Debug.Log($"[CombatManager] {message}");
        OnCombatLog?.Invoke(message);
    }

    #endregion
}
