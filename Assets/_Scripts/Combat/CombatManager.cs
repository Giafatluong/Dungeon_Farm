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

    public enum Turn
    {
        Player,
        Enemy
    }

    public Turn currentTurn;

    private bool isEating;

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

            Debug.Log(
                enemy.enemyData.enemyName +
                " Attack Player: " +
                damage +
                " damage"
            );
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Defend)
        {
            enemy.Defend();

            Debug.Log(
                enemy.enemyData.enemyName +
                " Defend"
            );
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Buff)
        {
            enemy.ApplyIntentBuff();
            ApplyAllyBuff(enemy);

            Debug.Log(
                enemy.enemyData.enemyName +
                " Buff"
            );
        }
        else if (enemy.currentIntent == EnemyData.EnemyIntent.Debuff)
        {
            ApplyPlayerDebuff(enemy);

            Debug.Log(
                enemy.enemyData.enemyName +
                " Debuff Player"
            );
        }

        enemy.SelectIntent();

        turnManager.NextTurn();
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
            return;

        if (targetSelector.selectedEnemy == null)
            return;

        if (targetSelector.selectedEnemy.currentHealth <= 0)
            return;

        EnemyStats target = targetSelector.selectedEnemy;

        int damage = playerStats.GetCurrentATK() - target.GetCurrentDEF();

        if (damage < 0)
            damage = 0;

        target.TakeDamage(damage);

        if (target.currentHealth <= 0)
        {
            TryDropUnlockedSeed(target.transform.position);
        }

        playerStats.currentAP--;

        targetSelector.CheckTarget();

        Debug.Log(
            "Player Attack " +
            target.enemyData.enemyName +
            ": " +
            damage +
            " damage"
        );

        if (AreAllEnemiesDead())
        {
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
            return;

        playerStats.Defend();

        playerStats.currentAP--;

        Debug.Log(
            "Player Defend: " +
            playerStats.defendCount
        );
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
        if (!isEating)
            return;

        if (currentTurn != Turn.Player)
            return;

        if (playerStats.currentAP <= 0)
            return;

        if (item == null)
            return;

        FoodData food = item as FoodData;

        if (food == null)
        {
            Debug.Log("Khong phai do an");
            return;
        }

        int hungerBefore = playerStats.currentHunger;

        playerStats.Eat(food);

        if (playerStats.currentHunger == hungerBefore)
            return;

        playerStats.currentAP--;
        isEating = false;

        Debug.Log(
            "Player Eat: " +
            food.itemName +
            " | AP: " +
            playerStats.currentAP
        );
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

        turnManager.NextTurn();
    }

    public void StartEnemyTurn(EnemyStats enemy)
    {
        currentTurn = Turn.Enemy;

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

        EnemyAction(enemy);
    }

    #endregion
}