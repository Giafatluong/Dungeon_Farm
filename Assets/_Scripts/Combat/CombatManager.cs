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

            // GDD: Tăng khả năng phòng thủ cho đến hết Enemy Turn
            if (playerStats.defendCount > 0)
            {
                damage -= (playerStats.defendValue * playerStats.defendCount);
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
            TryDropUnlockedSeed(target.transform.position);

            // Kiểm tra nếu là Boss
            Boss boss = target.GetComponent<Boss>();
            if (boss != null)
            {
                boss.OnBossDefeated(playerStats != null ? playerStats.itemContainer : null);
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
            EndCombat();
        }
    }

    private void TryDropUnlockedSeed(Vector3 dropPosition)
    {
        if (ProgressionManager.Instance == null) return;

        // Chỉ lọc các hạt giống ĐÃ MỞ KHÓA và KHÔNG PHẢI HẠT HIẾM (trừ hạt hiếm chỉ boss mới rơi)
        List<ItemData> dropCandidates = new List<ItemData>();
        for (int i = 0; i < ProgressionManager.Instance.unlockedSeeds.Count; i++)
        {
            ItemData seed = ProgressionManager.Instance.unlockedSeeds[i];
            if (seed != null && seed.itemType == ItemData.ItemType.Seed && !seed.isRare)
            {
                dropCandidates.Add(seed);
            }
        }

        if (dropCandidates.Count == 0) return;

        // Tỷ lệ quái thường rơi hạt giống đã mở khóa (ví dụ 40%)
        float dropChance = 0.40f;
        if (Random.value < dropChance)
        {
            ItemData chosenSeed = dropCandidates[Random.Range(0, dropCandidates.Count)];
            Debug.Log($"Quái thường rơi hạt giống đã mở khóa: {chosenSeed.itemName}!");

            if (playerStats != null && playerStats.itemContainer != null)
            {
                playerStats.itemContainer.AddItem(chosenSeed, 1);
            }
        }
    }

    public void PlayerDefend()
    {
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
        if (currentTurn != Turn.Player)
            return;

        if (playerStats.currentAP <= 0)
            return;

        isEating = true;
    }

    public void PlayerEat(ItemData item)
    {
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
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                Destroy(enemies[i].gameObject);
            }
        }
    }

    public void EndCombat()
    {
        Debug.Log("Combat End");

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
            waveManager.Continue();
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