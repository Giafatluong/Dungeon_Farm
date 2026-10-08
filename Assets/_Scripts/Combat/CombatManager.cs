using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    #region Config
    [Header("Config")]
    public float preparationTime = 3f;
    public float enemyTurnDelay = 1.2f;
    #endregion

    #region Events
    public event System.Action<string> OnCombatLog;
    public event System.Action<Turn> OnTurnChanged;
    public event System.Action<float> OnCombatPreparationCountdown;
    public event System.Action OnCombatStarted;
    public event System.Action OnCombatEnded;
    public event System.Action<EnemyStats, int> OnPlayerAttackAction;
    #endregion

    private Coroutine preparationCoroutine;
    private Coroutine enemyTurnCoroutine;

    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (turnManager == null) turnManager = GetComponent<TurnManager>();
        if (speedManager == null) speedManager = GetComponent<SpeedManager>();
        if (targetSelector == null) targetSelector = GetComponent<EnemyTargetSelector>();
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void Start()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
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
        if (!isCombatActive || currentTurn != Turn.Player) return;

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

    #endregion

    #region Player Actions

    public void PlayerAttack()
    {
        if (!isCombatActive || currentTurn != Turn.Player) return;
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
        if (!isCombatActive || currentTurn != Turn.Player) return;
        if (playerStats == null || playerStats.currentAP <= 0) return;

        playerStats.Defend();
        playerStats.currentAP--;

        DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.1f, $"+{playerStats.defendValue} DEF", new Color(0.4f, 0.75f, 1f), 4.6f);
        LogMessage($"You take a defensive stance! (+{playerStats.defendValue} shield per stack, {playerStats.defendCount} stack(s))");
    }

    public void PlayerEat(FoodData food)
    {
        if (!isCombatActive || currentTurn != Turn.Player) return;
        if (playerStats == null) return;
        if (food == null) return;

        bool isFreeEat = false;
        // Blessing: Đại Tiệc Bất Tận (EndlessFeast) - Món ăn đầu tiên trong mỗi lượt không tốn AP
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast) && !hasEatenThisTurn)
        {
            isFreeEat = true;
            hasEatenThisTurn = true;
        }

        if (!isFreeEat && playerStats.currentAP <= 0)
        {
            LogMessage("Not enough AP to eat!");
            return;
        }

        playerStats.Eat(food);

        if (!isFreeEat)
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

        // Drop loot
        List<ItemSlot> drops = deadEnemy.GetLootDrops();
        if (playerStats != null && playerStats.itemContainer != null && drops != null)
        {
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i] != null && drops[i].itemData != null && drops[i].amount > 0)
                {
                    playerStats.itemContainer.AddItem(drops[i].itemData, drops[i].amount);
                    currentWaveLoot.Add(new ItemSlot { itemData = drops[i].itemData, amount = drops[i].amount });
                    LogMessage($"Obtained: {drops[i].itemData.itemName} x{drops[i].amount}");

                    // Floating text popup above dead enemy
                    Vector3 popupPos = deadEnemy.transform.position + Vector3.up * 1.4f + new Vector3(Random.Range(-0.25f, 0.25f), i * 0.45f, 0);
                    DamagePopupManager.ShowText(popupPos, $"+{drops[i].amount} {drops[i].itemData.itemName}", new Color(0.35f, 0.95f, 0.55f), 4.6f);
                }
            }
        }

        CheckAllEnemiesDead();
    }

    private void CheckAllEnemiesDead()
    {
        if (enemies == null) return;

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
            EndCombat(victory: true);
        }
    }

    private void EndCombat(bool victory)
    {
        if (!isCombatActive && !isPreparingCombat) return;

        isCombatActive = false;
        isPreparingCombat = false;

        if (preparationCoroutine != null) StopCoroutine(preparationCoroutine);
        if (enemyTurnCoroutine != null) StopCoroutine(enemyTurnCoroutine);

        if (playerStats != null)
        {
            playerStats.OnCombatComplete();
        }

        OnCombatEnded?.Invoke();

        if (victory)
        {
            LogMessage("Victory! All enemies defeated.");

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null && isAmbushCurrent)
            {
                waveManager.HandlePostAmbushCamp();
            }
            else
            {
                int waveIdx = waveManager != null ? waveManager.GetCurrentWaveIndex() : 0;
                if (CombatUI.Instance != null)
                {
                    CombatUI.Instance.ShowWaveVictoryScreen(waveIdx, currentWaveLoot);
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
