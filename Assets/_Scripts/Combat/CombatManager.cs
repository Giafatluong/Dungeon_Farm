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
    [Tooltip("Thời gian chờ (giây) sau khi quái xuất hiện trước khi bắt đầu lượt combat đầu tiên (tránh người chơi vào game bị mất máu bất ngờ)")]
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
        if (playerStats == null)
        {
            // Tự động tìm GameObject Player hoặc tạo PlayerCombat dự phòng
            GameObject pGO = GameObject.Find("PlayerCombat");
            if (pGO == null) pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null)
            {
                playerStats = pGO.GetComponent<PlayerStats>();
                if (playerStats == null) playerStats = pGO.AddComponent<PlayerStats>();
            }
        }

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

            string logMsg = $"💀 {enemy.enemyData.enemyName} tấn công bạn gây {damage} sát thương!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Defend)
        {
            enemy.Defend();

            string logMsg = $"🛡️ {enemy.enemyData.enemyName} vào thế phòng thủ (+{enemy.enemyData.DEF} DEF)!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Buff)
        {
            enemy.ApplyIntentBuff();
            ApplyAllyBuff(enemy);

            string logMsg = $"✨ {enemy.enemyData.enemyName} kích hoạt hiệu ứng Buff!";
            Debug.Log(logMsg);
            OnCombatLog?.Invoke(logMsg);
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Debuff)
        {
            ApplyPlayerDebuff(enemy);

            string logMsg = $"☠️ {enemy.enemyData.enemyName} sử dụng chiêu thức làm suy yếu bạn!";
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
            OnCombatLog?.Invoke("⚠️ Không đủ AP để tấn công!");
            return;
        }

        if (targetSelector.selectedEnemy == null || targetSelector.selectedEnemy.currentHealth <= 0)
        {
            targetSelector.AutoSelectTarget(enemies);
        }

        if (targetSelector.selectedEnemy == null)
        {
            OnCombatLog?.Invoke("⚠️ Chưa chọn kẻ địch để tấn công!");
            return;
        }

        EnemyStats target = targetSelector.selectedEnemy;

        int damage = playerStats.GetCurrentATK() - target.GetCurrentDEF();

        if (damage < 0)
            damage = 0;

        target.TakeDamage(damage);
        OnPlayerAttackAction?.Invoke(target, damage);

        string attackMsg = $"⚔️ Bạn tấn công {target.enemyData.enemyName} gây {damage} sát thương!";
        Debug.Log(attackMsg);
        OnCombatLog?.Invoke(attackMsg);

        if (target.currentHealth <= 0)
        {
            // 1. Thu thập chiến lợi phẩm rơi từ quái dựa trên loot table (tỉ lệ, số lượng, điều kiện mở khóa)
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
                            string dropMsg = $"🎁 {target.enemyData.enemyName} rơi {d.amount}x {d.itemData.itemName}!";
                            Debug.Log(dropMsg);
                            OnCombatLog?.Invoke(dropMsg);
                        }
                    }
                }
            }

            // 2. Kiểm tra nếu là Boss
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
                OnCombatLog?.Invoke($"👑 Chúc mừng! Bạn đã tiêu diệt trùm {boss.bossID}!");
            }

            // Tự động chuyển mục tiêu sang quái sống tiếp theo
            targetSelector.AutoSelectTarget(enemies);
        }

        playerStats.currentAP--;

        targetSelector.CheckTarget();

        if (AreAllEnemiesDead())
        {
            OnCombatLog?.Invoke("🎉 Đã quét sạch toàn bộ kẻ địch!");
            DestroyAllEnemies();
            isCombatActive = false;

            // Mở bảng LootUI để nhặt đồ, quản lý túi đồ trước khi đi tiếp
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
                // Fallback nếu hoàn toàn không có UI/Canvas: tự động nhặt vào túi đồ
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
            OnCombatLog?.Invoke("⚠️ Không đủ AP để phòng thủ!");
            return;
        }

        playerStats.Defend();
        playerStats.currentAP--;
        OnPlayerDefendAction?.Invoke();

        string defMsg = $"🛡️ Bạn vào thế phòng thủ! (Giảm {playerStats.defendValue * playerStats.defendCount} sát thương cho đến hết lượt địch)";
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
            OnCombatLog?.Invoke("⚠️ Không đủ AP để ăn uống!");
            return;
        }

        if (item == null)
            return;

        FoodData food = item as FoodData;

        if (food == null)
        {
            OnCombatLog?.Invoke("⚠️ Vật phẩm này không phải món ăn!");
            return;
        }

        if (playerStats.currentHunger + food.hungerValue > playerStats.maxHunger)
        {
            OnCombatLog?.Invoke("⚠️ Bạn quá no, không thể ăn thêm món này!");
            return;
        }

        int hungerBefore = playerStats.currentHunger;

        playerStats.Eat(food);
        OnPlayerEatAction?.Invoke(food);

        if (playerStats.currentHunger == hungerBefore && food.hungerValue > 0)
            return;

        playerStats.currentAP--;
        isEating = false;

        string eatMsg = $"🍖 Bạn đã ăn {food.itemName}! Hồi phục {food.healthValue} HP, {food.hungerValue} Độ no.";
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

        // Xóa triệt để toàn bộ thanh máu của kẻ địch khi quét sạch quái
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

        // Dọn dẹp thanh máu quái
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
                ? $"⚠️ CẢNH BÁO: Bị phục kích! Kẻ địch sẽ hành động trước sau {Mathf.CeilToInt(combatStartDelay)}s..."
                : $"⚔️ Kẻ địch đã xuất hiện! Trận chiến bắt đầu sau {Mathf.CeilToInt(combatStartDelay)}s...";

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

        OnCombatLog?.Invoke("💥 TRẬN ĐẤU CHÍNH THỨC BẮT ĐẦU!");
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
        OnCombatLog?.Invoke($"🌟 LƯỢT CỦA BẠN! Bạn có {playerStats.currentAP} AP để hành động.");

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
        OnCombatLog?.Invoke("⏳ Bạn đã kết thúc lượt.");

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