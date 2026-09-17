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

    private void Start()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentAP = maxAP;
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
    }

    #endregion

    #region GetCurrentStats

    public int GetCurrentATK()
    {
        int value = ATK;

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

        if (currentHealth < 0)
        {
            currentHealth = 0;
            Debug.Log("Ngu");
        }
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

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
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