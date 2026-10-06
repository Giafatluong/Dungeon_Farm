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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

    #endregion

    #region Turn Management

    public void StartPlayerTurn()
    {
        currentTurn = Turn.Player;

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

        if (enemy != null && enemy.currentHealth > 0 && playerStats != null)
        {
            EnemyData.EnemyIntent actionTaken = enemy.currentIntent;
            enemy.PerformIntentAction(playerStats);
            LogMessage($"{enemy.enemyData?.enemyName ?? "Enemy"} used {actionTaken}!");
        }

        yield return new WaitForSeconds(0.5f);

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
        target.TakeDamage(atk);
        playerStats.currentAP--;
        OnPlayerAttackAction?.Invoke(target, atk);

        LogMessage($"You attacked {target.enemyData?.enemyName ?? "enemy"} for {atk} damage!");

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
        if (playerStats == null || playerStats.currentAP <= 0) return;
        if (food == null) return;

        playerStats.Eat(food);
        playerStats.currentAP--;

        LogMessage($"You ate {food.itemName}!");
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
                    LogMessage($"Obtained: {drops[i].itemData.itemName} x{drops[i].amount}");
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
            if (waveManager != null)
            {
                waveManager.Continue();
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
