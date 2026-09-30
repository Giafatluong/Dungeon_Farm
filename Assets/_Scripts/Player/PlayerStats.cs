using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth;
    public int currentHealth;

    [Header("Combat Stats")]
    public int ATK;
    public int DEF;
    public int speed;
    public int defendCount;
    public int defendValue = 5;

    [Header("Action Point")]
    public int maxAP;
    public int currentAP;

    [Header("Hunger")]
    public int maxHunger;
    public int currentHunger;

    [Header("Buffs")]
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    public ItemContainer itemContainer;
    public HealthBar healthBar;

    public event System.Action OnPlayerDeath;
    public event System.Action<int> OnPlayerDamaged;
    public event System.Action<int> OnPlayerHealed;
    public event System.Action OnPlayerDefended;

    private void Start()
    {
        if (ProgressionManager.Instance != null)
        {
            maxHealth += ProgressionManager.Instance.permanentMaxHP;
        }

        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentAP = maxAP;

        if (healthBar != null)
        {
            healthBar.SetHealthBar(currentHealth, maxHealth);
        }
    }

    #region PlayerAction

    public void Eat(ItemData item)
    {
        FoodData food = item as FoodData;

        if (food == null)
        {
            Debug.Log("khong phai do an");
            return;
        }

        if (!itemContainer.HasItem(food))
        {
            Debug.Log("khong co do an");
            return;
        }

        if (currentHunger + food.hungerValue > maxHunger)
        {
            Debug.Log("No qua k an dc");
            return;
        }

        if (food.foodBuff.Length != 0)
        {
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                AddBuff(
                    food.foodBuff[i].buffs,
                    food.foodBuff[i].buffValue,
                    food.foodBuff[i].buffDuration,
                    food.foodBuff[i].buffDurationType
                );
            }
        }

        AddHunger(food.hungerValue);
        Heal(food.healthValue);
        itemContainer.RemoveItem(food, 1);

        Debug.Log("da an : " + food.itemName);
    }

    public void Defend()
    {
        defendCount++;
        OnPlayerDefended?.Invoke();
    }

    #endregion

    #region GetCurrentStats

    public int GetCurrentATK()
    {
        int value = ATK;

        if (ProgressionManager.Instance != null)
        {
            value += ProgressionManager.Instance.permanentATK;
        }

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buff.buffType == StatEffectData.BuffType.Attack)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        if(value <= 0) value = 0;

        return value;
    }

    public int GetCurrentDEF()
    {
        int value = DEF;

        if (ProgressionManager.Instance != null)
        {
            value += ProgressionManager.Instance.permanentDEF;
        }

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buff.buffType == StatEffectData.BuffType.Defense)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        if(value <= 0) value = 0;

        return value;
    }

    public int GetCurrentSpeed()
    {
        int value = speed;

        if (ProgressionManager.Instance != null)
        {
            value += ProgressionManager.Instance.permanentSpeed;
        }

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buff.buffType == StatEffectData.BuffType.Speed)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        if(value <= 0) value = 0;

        return value;
    }

    #endregion

    #region AddSomething

    public void AddBuff(
        StatEffectData buff,
        int value,
        int duration,
        FoodData.BuffDurationType durationType)
    {
        ActiveBuff newBuff = new()
        {
            buff = buff,
            buffValue = value,
            remainingDuration = duration,
            buffDurationType = durationType
        };
        for(int i=activeBuffs.Count-1; i>=0; i--)
        {
            if(activeBuffs[i].buff != newBuff.buff) continue;
            if(activeBuffs[i].buffDurationType != newBuff.buffDurationType) continue;

            activeBuffs[i].remainingDuration += newBuff.remainingDuration;
            return;
        }

        activeBuffs.Add(newBuff);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (healthBar != null)
            {
                healthBar.SetHealthBar(currentHealth, maxHealth);
            }

            Debug.Log("Player đã tử trận!");
            OnPlayerDeath?.Invoke();

            if (ProgressionManager.Instance != null)
            {
                if (CombatUI.Instance != null)
                {
                    ProgressionManager.Instance.HandlePlayerDeathWithoutReload(itemContainer);
                }
                else
                {
                    ProgressionManager.Instance.HandlePlayerDeath(itemContainer);
                }
            }
            return;
        }

        if (healthBar != null)
        {
            healthBar.SetHealthBar(currentHealth, maxHealth);
        }

        OnPlayerDamaged?.Invoke(damage);
    }

    public void OnStageTransition()
    {
        // GDD: Hunger giảm khi người chơi di chuyển giữa các Stage
        ReduceHunger(5);

        // GDD: Buff theo Turn cũng giảm mỗi khi người chơi di chuyển sang Stage mới
        ReduceTurnBuffDuration();
    }

    public void OnCombatComplete()
    {
        // GDD: Hunger giảm sau mỗi Combat
        ReduceHunger(10);

        // GDD: Buff theo Combat giảm sau khi hoàn thành một Combat
        ReduceCombatBuffDuration();
    }

    public void AddHunger(int amount)
    {
        if (currentHunger >= maxHunger)
        {
            Debug.Log("Khong an dc them");
            return;
        }

        currentHunger += amount;

        if (currentHunger > maxHunger)
        {
            currentHunger = maxHunger;
        }
    }

    public void ReduceHunger(int amount)
    {
        currentHunger -= amount;

        if (currentHunger <= 0)
        {
            currentHunger = 0;
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (healthBar != null)
        {
            healthBar.SetHealthBar(currentHealth, maxHealth);
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        OnPlayerHealed?.Invoke(amount);
    }

    #endregion

    #region ReduceBuff

    public void ReduceTurnBuffDuration()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].buffDurationType != FoodData.BuffDurationType.Turn)
                continue;

            activeBuffs[i].remainingDuration--;

            if (activeBuffs[i].remainingDuration <= 0)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void ReduceCombatBuffDuration()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].buffDurationType != FoodData.BuffDurationType.Combat)
                continue;

            activeBuffs[i].remainingDuration--;

            if (activeBuffs[i].remainingDuration <= 0)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public void ReduceFloorBuffDuration()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].buffDurationType != FoodData.BuffDurationType.Floor)
                continue;

            activeBuffs[i].remainingDuration--;

            if (activeBuffs[i].remainingDuration <= 0)
            {
                activeBuffs.RemoveAt(i);
            }
        }
    }

    #endregion
}