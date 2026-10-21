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
    public float enemyTurnDelay = 0.35f;
    public float playerActionDelay = 0.35f;
    [Tooltip("Delay time after final enemy dies before showing victory UI")]
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
    public bool isVictoryPending => victoryCoroutine != null;

    private Coroutine preparationCoroutine;
    private Coroutine enemyTurnCoroutine;
    private Coroutine playerActionDelayCoroutine;
    private Coroutine victoryCoroutine;

    private static readonly WaitForSeconds waitFallbackVisual = new(0.48f);
    private static readonly Dictionary<float, WaitForSeconds> _waitCache = new();
    private static WaitForSeconds GetCachedWait(float seconds)
    {
        if (!_waitCache.TryGetValue(seconds, out var w))
        {
            w = new WaitForSeconds(seconds);
            _waitCache[seconds] = w;
        }
        return w;
    }

    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (turnManager == null) turnManager = GetComponent<TurnManager>();
        if (speedManager == null) speedManager = GetComponent<SpeedManager>();
        if (targetSelector == null) targetSelector = GetComponent<EnemyTargetSelector>();
        if (playerStats == null) playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
    }

    private void Start()
    {
        if (playerStats == null) playerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
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
                e.OnEnemyDeath -= HandleEnemyDeath;
                e.OnEnemyDeath += HandleEnemyDeath;
                e.defenseReduction = 0;
                e.hasActedThisRound = false;
                e.SelectNextIntent();
            }
        }

        if (speedManager != null)
        {
            speedManager.SetEnemies(enemies);
        }

        if (targetSelector != null)
        {
            targetSelector.AutoSelectTarget(enemies);
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

        if (targetSelector != null)
        {
            targetSelector.AutoSelectTarget(enemies);
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
    private bool hasDefendedThisTurn = false;
    public EnemyStats currentActingEnemy { get; private set; }

    public bool CanPlayerEatFree()
    {
        return ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast) && !hasEatenThisTurn;
    }

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
        currentActingEnemy = null;
        hasEatenThisTurn = false;
        hasDefendedThisTurn = false;

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

        currentActingEnemy = enemy;

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
        yield return GetCachedWait(enemyTurnDelay);

        EnemyData.EnemyIntent actionTaken = EnemyData.EnemyIntent.Attack;
        if (enemy != null && enemy.currentHealth > 0 && playerStats != null && playerStats.currentHealth > 0)
        {
            actionTaken = enemy.currentIntent;
            enemy.PerformIntentAction(playerStats);
            LogMessage($"{enemy.enemyData?.enemyName ?? "Enemy"} used {actionTaken}!");
        }

        // Allow enemy action animation to complete before advancing turn
        EnemyCombatVisual visual = enemy != null ? enemy.GetComponent<EnemyCombatVisual>() : null;
        if (visual != null)
        {
            float maxWait = 1.8f;
            while (visual != null && visual.IsVisualActionActive && maxWait > 0f)
            {
                maxWait -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return waitFallbackVisual;
        }

        if (!isCombatActive || playerStats == null || playerStats.currentHealth <= 0 || isVictoryPending)
        {
            yield break;
        }

        // Reduce enemy turn buffs (not reduced on the same turn it was cast)
        if (enemy != null && actionTaken != EnemyData.EnemyIntent.Buff)
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
        yield return GetCachedWait(delay);
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

        StartPlayerActionCooldown(0.48f);

        int atk = playerStats.GetCurrentATK();

        // Blessing: Swift Momentum - Deals extra damage when speed is higher than enemy
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.SwiftMomentum))
        {
            int speedDiff = Mathf.Max(0, playerStats.GetCurrentSpeed() - target.GetCurrentSpeed());
            if (speedDiff > 0)
            {
                atk += speedDiff;
                DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.5f, $"+{speedDiff} Speed Surge!", new Color(0.2f, 1f, 0.5f), 4.5f);
            }
        }

        // Blessing: Starving Fury - 1.5x crit damage when hunger < 35%
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.StarvingFury)
            && playerStats.maxHunger > 0 && playerStats.currentHunger <= playerStats.maxHunger * 0.35f)
        {
            if (Random.value < 0.35f)
            {
                atk = Mathf.RoundToInt(atk * 1.5f);
                DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.6f, "HUNGER CRIT!", Color.red, 5f);
            }
        }

        int dealtDamage = target.TakeDamage(atk, showFeedback: false);
        playerStats.currentAP--;
        OnPlayerAttackAction?.Invoke(target, dealtDamage);

        LogMessage($"You attacked {target.enemyData?.enemyName ?? "enemy"} for {dealtDamage} damage!");

        // Blessing: Bitter Decay - Reduces 2 DEF and inflicts direct poison damage
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.BitterDecay) && target.currentHealth > 0)
        {
            target.ReduceDEF(2);
            int poisonDmg = target.TakeDamage(4); // Extra 4 direct poison damage
            DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.2f, $"Decay Poison! -{poisonDmg} HP", new Color(0.6f, 0.3f, 0.9f), 4.5f);
            LogMessage($"[Blessing: BitterDecay] Corroded 2 DEF and dealt {poisonDmg} poison damage to {target.enemyData?.enemyName ?? "monster"}!");
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayAttackAnimationByDamage(target.transform.position, poisonDmg, false, isPoison: true);
            }
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

        bool hasBastion = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.BastionStrike);
        bool isFirstDefend = !hasDefendedThisTurn;

        playerStats.Defend();
        playerStats.currentAP--;

        // Blessing: Bastion Strike - First defend in a turn restores 1 AP
        if (hasBastion && isFirstDefend)
        {
            hasDefendedThisTurn = true;
            playerStats.currentAP = Mathf.Min(playerStats.maxAP, playerStats.currentAP + 1);
            DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.5f, "+1 AP (Bastion)!", new Color(1f, 0.8f, 0.2f), 4.5f);
            LogMessage("[Blessing: BastionStrike] Bastion Strike: First defend restored +1 AP!");
        }

        DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.1f, $"+{playerStats.defendValue} DEF", new Color(0.4f, 0.75f, 1f), 4.6f);
        LogMessage($"You take a defensive stance! (+{playerStats.defendValue} shield per stack, {playerStats.defendCount} stack(s))");
    }

    public void PlayerEat(ItemData food)
    {
        if (playerStats == null || food == null || isPlayerActing) return;

        bool isAtEntrance = WaveManager.Instance != null && WaveManager.Instance.isAtEntrance;

        if (!isAtEntrance && (!isCombatActive || currentTurn != Turn.Player)) return;

        bool isFreeEat = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast) && !hasEatenThisTurn;

        if (!isAtEntrance && !isFreeEat && playerStats.currentAP <= 0)
        {
            LogMessage("Not enough AP to eat!");
            return;
        }

        // Attempt to eat the food item
        if (!playerStats.Eat(food))
        {
            if (playerStats.currentHunger >= playerStats.maxHunger && playerStats.currentHealth >= playerStats.maxHealth)
            {
                LogMessage("Health and fullness are full, no need to eat!");
                DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.3f, "FULL HP & FULLNESS!", Color.yellow, 4.5f);
            }
            return;
        }

        StartPlayerActionCooldown();

        if (isFreeEat)
        {
            hasEatenThisTurn = true;
            DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.4f, "Feast: 0 AP!", new Color(1f, 0.5f, 0.8f), 4.5f);
            LogMessage($"[Blessing: EndlessFeast] Endless Feast: Ate {food.itemName} without consuming AP!");
        }
        else if (!isAtEntrance)
        {
            playerStats.currentAP--;
        }

        if (food is FoodData foodData && foodData.foodBuff != null && foodData.foodBuff.Length > 0)
        {
            List<string> bLogs = new();
            bool hasFeast = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast);
            for (int i = 0; i < foodData.foodBuff.Length; i++)
            {
                var fb = foodData.foodBuff[i];
                if (fb != null && fb.buffs != null)
                {
                    string stat = CombatUI.GetBuffStatLabel(fb.buffs);
                    int dur = hasFeast ? fb.buffDuration * 2 : fb.buffDuration;
                    string durUnit = fb.buffDurationType switch
                    {
                        FoodData.BuffDurationType.Turn => dur == 1 ? "turn" : "turns",
                        FoodData.BuffDurationType.Combat => dur == 1 ? "battle" : "battles",
                        FoodData.BuffDurationType.Floor => dur == 1 ? "floor" : "floors",
                        _ => fb.buffDurationType.ToString()
                    };
                    string sign = fb.buffValue > 0 ? "+" : "";
                    bLogs.Add($"{sign}{fb.buffValue} {stat} ({dur} {durUnit})");
                }
            }
            if (bLogs.Count > 0)
            {
                LogMessage($"You ate {food.itemName}! (Buffs: {string.Join(", ", bLogs)})");
                return;
            }
        }

        LogMessage($"You ate {food.itemName}!");
    }

    /// <summary>
    /// Reflects damage back to monsters when player has Gluttonous Aegis blessing
    /// </summary>
    public void ReflectDamageToCurrentEnemy(int reflectedDamage)
    {
        if (reflectedDamage <= 0 || enemies == null) return;

        EnemyStats target = currentActingEnemy != null && currentActingEnemy.currentHealth > 0
            ? currentActingEnemy
            : null;

        if (target == null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].currentHealth > 0)
                {
                    target = enemies[i];
                    break;
                }
            }
        }

        if (target != null)
        {
            int reflectDmg = target.TakeDamage(reflectedDamage);
            DamagePopupManager.ShowText(target.transform.position + Vector3.up * 1.3f, $"Reflected {reflectDmg} DMG!", new Color(0.9f, 0.8f, 0.2f), 4.5f);
            LogMessage($"[Blessing: GluttonousAegis] Reflected {reflectDmg} damage to {target.enemyData?.enemyName ?? "enemy"}!");
            CheckAllEnemiesDead();
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

                    LogMessage($"Loot drop: {drops[i].itemData.itemName} x{drops[i].amount}");
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
        // Lock player actions while waiting for monsters to fade
        SetPlayerActing(true);

        if (enemyTurnCoroutine != null)
        {
            StopCoroutine(enemyTurnCoroutine);
            enemyTurnCoroutine = null;
        }

        if (victoryDelay > 0f)
        {
            yield return GetCachedWait(victoryDelay);
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

            WaveManager waveManager = WaveManager.Instance != null ? WaveManager.Instance : FindFirstObjectByType<WaveManager>();
            int waveIdx = waveManager != null ? waveManager.GetCurrentWaveIndex() : 0;

            // Player manually collects items into inventory rather than automatic collection
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
