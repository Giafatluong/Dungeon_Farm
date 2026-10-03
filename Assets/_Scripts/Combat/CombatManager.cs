using UnityEngine;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public PlayerStats playerStats;
    public EnemyStats enemyStats;
    public EnemyTargetSelector targetSelector;
    public EnemyStats[] enemies;
    public SpeedManager speedManager;
    public TurnManager turnManager;
    public HealthBarManager healthBarManager;

    [SerializeField] private WaveManager waveManager;

    public event System.Action<string> OnCombatLog;
    public event System.Action<Turn> OnTurnChanged;
    public event System.Action<EnemyStats, int> OnPlayerAttackAction;
    public event System.Action OnPlayerDefendAction;
    public event System.Action<FoodData> OnPlayerEatAction;
    public event System.Action<float> OnCombatPreparationCountdown;
    public event System.Action OnCombatStarted;

    [Header("Combat Delay Settings")]
    [Tooltip("Preparation time (seconds) after monsters appear before first combat turn starts")]
    public float combatStartDelay = 3.0f;

    public bool isCombatActive { get; private set; } = false;
    public bool isPreparingCombat { get; private set; } = false;
    public bool isAmbushCurrent { get; private set; } = false;
    public float preparationTimeRemaining { get; private set; } = 0f;

    private Coroutine startCombatCoroutine;
    private List<ItemSlot> currentCombatLoot = new List<ItemSlot>();

    public enum Turn
    {
        Player,
        Enemy
    }

    public Turn currentTurn;

    private bool isEating;

    private void Awake()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();

        if (targetSelector == null) targetSelector = FindFirstObjectByType<EnemyTargetSelector>();
        if (speedManager == null) speedManager = FindFirstObjectByType<SpeedManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (healthBarManager == null) healthBarManager = FindFirstObjectByType<HealthBarManager>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
    }

    private void OnEnable()
    {
        InventoryButton.OnItemSelected += OnItemSelected;
    }

    private void OnDisable()
    {
        InventoryButton.OnItemSelected -= OnItemSelected;

        if (startCombatCoroutine != null)
        {
            StopCoroutine(startCombatCoroutine);
            startCombatCoroutine = null;
        }
    }

    private void OnItemSelected(ItemData item)
    {
        if (!isEating)
            return;

        if (currentTurn != Turn.Player)
            return;

        PlayerEat(item);
    }

    #region Enemy

    public void SetEnemies(EnemyStats[] newEnemies)
    {
        enemies = newEnemies;
        if (speedManager != null)
        {
            speedManager.SetEnemies(newEnemies);
        }
    }

    public void EnemyAction(EnemyStats enemy)
    {
        if (!isCombatActive || isPreparingCombat)
            return;

        if (currentTurn != Turn.Enemy)
            return;

        if (enemy == null)
            return;

        if (enemy.currentHealth <= 0)
        {
            turnManager.NextTurn();
            return;
        }

        if (enemy.currentIntent == EnemyData.EnemyIntent.Attack)
        {
            int damage = enemy.GetCurrentATK() - playerStats.GetCurrentDEF();

            if (playerStats.defendCount > 0)
            {
                damage -= playerStats.defendValue * playerStats.defendCount;
            }

            if (damage < 0)
                damage = 0;

            playerStats.TakeDamage(damage);

            string logMsg = $"{enemy.enemyData.enemyName} attacks you for {damage} damage!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Defend)
        {
            enemy.Defend();

            string logMsg = $"{enemy.enemyData.enemyName} takes a defensive stance (+{enemy.enemyData.DEF} DEF)!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Buff)
        {
            enemy.ApplyIntentBuff();
            ApplyAllyBuff(enemy);

            string logMsg = $"{enemy.enemyData.enemyName} casts a Buff skill!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Debuff)
        {
            ApplyPlayerDebuff(enemy);

            string logMsg = $"{enemy.enemyData.enemyName} casts a Debuff on you!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }

        enemy.SelectIntent();
    }

    public void ApplyPlayerDebuff(EnemyStats enemy)
    {
        if (enemy == null)
            return;

        if (enemy.enemyData == null)
            return;

        for (int i = 0; i < enemy.enemyData.intentEffects.Length; i++)
        {
            EnemyData.IntentEffect effect = enemy.enemyData.intentEffects[i];

            if (effect.target != EnemyData.EffectTarget.Player)
                continue;

            playerStats.AddBuff(
                effect.effect,
                -effect.value,
                effect.duration,
                effect.durationType
            );
        }
    }

    public void ApplyAllyBuff(EnemyStats enemy)
    {
        if (enemy == null)
            return;

        if (enemy.enemyData == null)
            return;

        if (enemies == null)
            return;

        for (int i = 0; i < enemy.enemyData.intentEffects.Length; i++)
        {
            EnemyData.IntentEffect effect = enemy.enemyData.intentEffects[i];

            if (effect.target != EnemyData.EffectTarget.Ally)
                continue;

            for (int j = 0; j < enemies.Length; j++)
            {
                if (enemies[j] == null)
                    continue;

                if (enemies[j] == enemy)
                    continue;

                if (enemies[j].currentHealth <= 0)
                    continue;

                enemies[j].AddBuff(
                    effect.effect,
                    effect.value,
                    effect.duration,
                    effect.durationType
                );
            }
        }
    }

    #endregion

    #region Player

    public void PlayerAttack()
    {
        if (!isCombatActive || isPreparingCombat)
            return;

        if (currentTurn != Turn.Player)
            return;

        if (playerStats.currentAP <= 0)
        {
            OnCombatLog?.Invoke("Not enough AP to attack!");
            return;
        }

        if (targetSelector.selectedEnemy == null || targetSelector.selectedEnemy.currentHealth <= 0)
        {
            targetSelector.AutoSelectTarget(enemies);
        }

        if (targetSelector.selectedEnemy == null)
        {
            OnCombatLog?.Invoke("No enemy targeted to attack!");
            return;
        }

        EnemyStats target = targetSelector.selectedEnemy;

        int damage = playerStats.GetCurrentATK() - target.GetCurrentDEF();

        if (damage < 0)
            damage = 0;

        target.TakeDamage(damage);
        OnPlayerAttackAction?.Invoke(target, damage);

        string attackMsg = $"You attack {target.enemyData.enemyName} for {damage} damage!";
        Debug.Log(attackMsg);
        OnCombatLog?.Invoke(attackMsg);

        if (target.currentHealth <= 0)
        {
            if (target.enemyData != null)
            {
                List<ItemSlot> drops = target.enemyData.RollDrops();
                if (drops != null && drops.Count > 0)
                {
                    foreach (var d in drops)
                    {
                        if (d != null && d.itemData != null && d.amount > 0)
                        {
                            currentCombatLoot.Add(d);
                            string dropMsg = $"{target.enemyData.enemyName} dropped {d.amount}x {d.itemData.itemName}!";
                            Debug.Log(dropMsg);
                            OnCombatLog?.Invoke(dropMsg);
                        }
                    }
                }
            }

            Boss boss = target.GetComponent<Boss>();
            if (boss != null)
            {
                if (boss.rewardItems != null)
                {
                    for (int i = 0; i < boss.rewardItems.Length; i++)
                    {
                        if (boss.rewardItems[i].item != null && boss.rewardItems[i].amount > 0)
                        {
                            currentCombatLoot.Add(new ItemSlot { itemData = boss.rewardItems[i].item, amount = boss.rewardItems[i].amount });
                        }
                    }
                }
                if (boss.rareSeedReward != null)
                {
                    currentCombatLoot.Add(new ItemSlot { itemData = boss.rareSeedReward, amount = 1 });
                    if (ProgressionManager.Instance != null)
                    {
                        ProgressionManager.Instance.UnlockSeed(boss.rareSeedReward);
                    }
                }
                boss.isDefeated = true;
                OnCombatLog?.Invoke($"Boss Defeated! You vanquished {boss.bossID}!");
            }

            targetSelector.AutoSelectTarget(enemies);
        }

        playerStats.currentAP--;

        targetSelector.CheckTarget();

        if (AreAllEnemiesDead())
        {
            OnCombatLog?.Invoke("All enemies defeated!");
            DestroyAllEnemies();
            isCombatActive = false;

            LootUI lootUI = LootUI.EnsureInstance();

            if (lootUI != null)
            {
                lootUI.Open(currentCombatLoot, playerStats != null ? playerStats.itemContainer : null, () =>
                {
                    currentCombatLoot.Clear();
                    EndCombat();
                });
            }
            else
            {
                if (playerStats != null && playerStats.itemContainer != null)
                {
                    foreach (var loot in currentCombatLoot)
                    {
                        if (loot != null && loot.itemData != null)
                            playerStats.itemContainer.AddItem(loot.itemData, loot.amount);
                    }
                }
                currentCombatLoot.Clear();
                EndCombat();
            }
        }
    }

    public void PlayerDefend()
    {
        if (!isCombatActive || isPreparingCombat)
            return;

        if (currentTurn != Turn.Player)
            return;

        if (playerStats.currentAP <= 0)
        {
            OnCombatLog?.Invoke("Not enough AP to defend!");
            return;
        }

        playerStats.Defend();
        playerStats.currentAP--;
        OnPlayerDefendAction?.Invoke();

        string defMsg = $"You take a defensive stance! (Reduces damage by {playerStats.defendValue * playerStats.defendCount} until next turn)";
        Debug.Log(defMsg);
        OnCombatLog?.Invoke(defMsg);
    }

    public void SelectEat()
    {
        if (!isCombatActive || isPreparingCombat)
            return;

        if (currentTurn != Turn.Player)
            return;

        if (playerStats.currentAP <= 0)
            return;

        isEating = true;
    }

    public void PlayerEat(ItemData item)
    {
        if (!isCombatActive || isPreparingCombat)
            return;

        if (currentTurn != Turn.Player)
            return;

        if (playerStats.currentAP <= 0)
        {
            OnCombatLog?.Invoke("Not enough AP to eat!");
            return;
        }

        if (item == null)
            return;

        FoodData food = item as FoodData;

        if (food == null)
        {
            OnCombatLog?.Invoke("This item is not edible food!");
            return;
        }

        if (playerStats.currentHunger + food.hungerValue > playerStats.maxHunger)
        {
            OnCombatLog?.Invoke("You are too full to eat this food!");
            return;
        }

        int hungerBefore = playerStats.currentHunger;

        playerStats.Eat(food);
        OnPlayerEatAction?.Invoke(food);

        if (playerStats.currentHunger == hungerBefore && food.hungerValue > 0)
            return;

        playerStats.currentAP--;
        isEating = false;

        string eatMsg = $"You ate {food.itemName}! Recovered {food.healthValue} HP, {food.hungerValue} Fullness.";
        Debug.Log(eatMsg);
        OnCombatLog?.Invoke(eatMsg);
    }

    #endregion

    public bool AreAllEnemiesDead()
    {
        if (enemies == null || enemies.Length == 0)
            return false;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            if (enemies[i].currentHealth > 0)
                return false;
        }

        return true;
    }

    public void DestroyAllEnemies()
    {
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                {
                    Destroy(enemies[i].gameObject);
                }
            }
        }

        if (healthBarManager != null)
        {
            healthBarManager.ClearEnemyHealthBars();
        }
        else
        {
            HealthBarManager hbm = FindFirstObjectByType<HealthBarManager>();
            if (hbm != null)
            {
                hbm.ClearEnemyHealthBars();
            }
        }
    }

    public void EndCombat()
    {
        Debug.Log("Combat End");

        if (healthBarManager != null)
        {
            healthBarManager.ClearEnemyHealthBars();
        }
        else
        {
            HealthBarManager hbm = FindFirstObjectByType<HealthBarManager>();
            if (hbm != null)
            {
                hbm.ClearEnemyHealthBars();
            }
        }

        if (startCombatCoroutine != null)
        {
            StopCoroutine(startCombatCoroutine);
            startCombatCoroutine = null;
        }

        isPreparingCombat = false;
        isCombatActive = false;
        preparationTimeRemaining = 0f;

        currentTurn = Turn.Player;

        if (targetSelector != null)
        {
            targetSelector.ClearTarget();
        }

        if (playerStats != null)
        {
            playerStats.OnCombatComplete();
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null)
                    continue;

                enemies[i].ReduceCombatBuffDuration();
            }
        }

        if (waveManager != null)
        {
            if (waveManager.camp != null && waveManager.camp.mustContinue)
            {
                waveManager.HandlePostAmbushCamp();
            }
            else
            {
                waveManager.Continue();
            }
        }
    }

    #region TurnControl

    public void StartCombat(bool isAmbush = false)
    {
        if (enemies == null || enemies.Length == 0)
        {
            Debug.Log("Cannot start combat: No enemies");
            return;
        }

        if (turnManager == null)
        {
            Debug.Log("Cannot start combat: TurnManager is NULL");
            return;
        }

        if (startCombatCoroutine != null)
        {
            StopCoroutine(startCombatCoroutine);
        }

        startCombatCoroutine = StartCoroutine(StartCombatRoutine(isAmbush));
    }

    private System.Collections.IEnumerator StartCombatRoutine(bool isAmbush)
    {
        currentCombatLoot.Clear();

        if (combatStartDelay > 0f)
        {
            isPreparingCombat = true;
            isCombatActive = false;
            isAmbushCurrent = isAmbush;
            preparationTimeRemaining = combatStartDelay;

            string initialMsg = isAmbush
                ? $"WARNING: Ambushed! Enemies attack first in {Mathf.CeilToInt(combatStartDelay)}s..."
                : $"Enemies appeared! Battle begins in {Mathf.CeilToInt(combatStartDelay)}s...";

            Debug.Log(initialMsg);
            OnCombatLog?.Invoke(initialMsg);

            while (preparationTimeRemaining > 0f)
            {
                OnCombatPreparationCountdown?.Invoke(preparationTimeRemaining);
                float step = Mathf.Min(1.0f, preparationTimeRemaining);
                yield return new WaitForSeconds(step);
                preparationTimeRemaining -= step;
            }
        }

        preparationTimeRemaining = 0f;
        isPreparingCombat = false;
        isCombatActive = true;
        startCombatCoroutine = null;

        OnCombatLog?.Invoke("BATTLE HAS BEGUN!");
        OnCombatStarted?.Invoke();

        turnManager.StartRound(isAmbush);
    }

    public void StartPlayerTurn()
    {
        currentTurn = Turn.Player;

        playerStats.defendCount = 0;
        playerStats.currentAP = playerStats.maxAP;

        if (targetSelector != null)
        {
            targetSelector.AutoSelectTarget(enemies);
        }

        OnTurnChanged?.Invoke(Turn.Player);
        OnCombatLog?.Invoke($"YOUR TURN! You have {playerStats.currentAP} AP.");

        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            if (enemies[i].currentHealth <= 0)
                continue;

            Debug.Log(
                "Enemy Intent: " +
                enemies[i].enemyData.enemyName +
                " - " +
                enemies[i].currentIntent
            );
        }
    }

    public void EndTurnPlayer()
    {
        if (!isCombatActive || isPreparingCombat)
            return;

        if (currentTurn != Turn.Player)
            return;

        playerStats.ReduceTurnBuffDuration();
        OnCombatLog?.Invoke("Turn ended.");

        turnManager.NextTurn();
    }

    public void StartEnemyTurn(EnemyStats enemy)
    {
        currentTurn = Turn.Enemy;
        OnTurnChanged?.Invoke(Turn.Enemy);

        if (enemy == null)
        {
            turnManager.NextTurn();
            return;
        }

        enemy.ResetDefend();

        if (enemy.currentHealth <= 0)
        {
            turnManager.NextTurn();
            return;
        }

        StartCoroutine(EnemyTurnRoutine(enemy));
    }

    private System.Collections.IEnumerator EnemyTurnRoutine(EnemyStats enemy)
    {
        yield return new WaitForSeconds(0.4f);
        if (enemy != null && enemy.currentHealth > 0)
        {
            EnemyAction(enemy);
        }
        yield return new WaitForSeconds(0.4f);
        turnManager.NextTurn();
    }

    #endregion
}