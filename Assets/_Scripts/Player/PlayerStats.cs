using System.Collections.Generic;
using UnityEngine;

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

    private int baseMaxHealth;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
    }

    private void Start()
    {
        maxHealth = baseMaxHealth + (ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentMaxHP : 0);
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
            Debug.Log("Item is not food");
            return;
        }

        if (itemContainer != null && !itemContainer.HasItem(food))
        {
            Debug.Log("Food not found in container");
            return;
        }

        if (currentHunger >= maxHunger)
        {
            Debug.Log("Too full to eat");
            return;
        }

        if (food.foodBuff != null && food.foodBuff.Length > 0)
        {
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                if (food.foodBuff[i] != null && food.foodBuff[i].buffs != null)
                {
                    AddBuff(
                        food.foodBuff[i].buffs,
                        food.foodBuff[i].buffValue,
                        food.foodBuff[i].buffDuration,
                        food.foodBuff[i].buffDurationType
                    );
                }
            }
        }

        AddHunger(food.hungerValue);
        Heal(food.healthValue);
        if (itemContainer != null)
        {
            itemContainer.RemoveItem(food, 1);
        }

        Debug.Log("Ate: " + food.itemName);
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
            if (activeBuffs[i] != null && activeBuffs[i].buff != null && activeBuffs[i].buff.buffType == StatEffectData.BuffType.Attack)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        if (value <= 0) value = 0;

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
            if (activeBuffs[i] != null && activeBuffs[i].buff != null && activeBuffs[i].buff.buffType == StatEffectData.BuffType.Defense)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        if (value <= 0) value = 0;

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
            if (activeBuffs[i] != null && activeBuffs[i].buff != null && activeBuffs[i].buff.buffType == StatEffectData.BuffType.Speed)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        if (value <= 0) value = 0;

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
        if (buff == null) return;

        int finalValue = (buff.IsDebuff && value > 0) ? -value : value;

        ActiveBuff newBuff = new()
        {
            buff = buff,
            buffValue = finalValue,
            remainingDuration = duration,
            buffDurationType = durationType
        };

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null || activeBuffs[i].buff == null) continue;
            if (activeBuffs[i].buff != newBuff.buff) continue;
            if (activeBuffs[i].buffDurationType != newBuff.buffDurationType) continue;

            if (buff.stackable == StatEffectData.Stackable.Yes)
            {
                activeBuffs[i].buffValue += newBuff.buffValue;
                activeBuffs[i].remainingDuration = Mathf.Max(activeBuffs[i].remainingDuration, newBuff.remainingDuration);
            }
            else
            {
                if (newBuff.buffValue >= 0)
                    activeBuffs[i].buffValue = Mathf.Max(activeBuffs[i].buffValue, newBuff.buffValue);
                else
                    activeBuffs[i].buffValue = Mathf.Min(activeBuffs[i].buffValue, newBuff.buffValue);

                activeBuffs[i].remainingDuration = Mathf.Max(activeBuffs[i].remainingDuration, newBuff.remainingDuration);
            }
            return;
        }

        activeBuffs.Add(newBuff);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Damage Popup above player
        DamagePopupManager.ShowDamage(transform.position + Vector3.up * 1.1f, damage, isPlayer: true);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (healthBar != null)
            {
                healthBar.SetHealthBar(currentHealth, maxHealth);
            }

            Debug.Log("Player has died!");
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
        ReduceHunger(5);
        ReduceTurnBuffDuration();
    }

    public void OnCombatComplete()
    {
        ReduceHunger(10);
        ReduceCombatBuffDuration();
    }

    public void AddHunger(int amount)
    {
        if (currentHunger >= maxHunger)
        {
            Debug.Log("Cannot eat more");
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
        if (amount <= 0) return;
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // Heal Popup above player
        DamagePopupManager.ShowHeal(transform.position + Vector3.up * 1.1f, amount);

        if (healthBar != null)
        {
            healthBar.SetHealthBar(currentHealth, maxHealth);
        }

        OnPlayerHealed?.Invoke(amount);
    }

    #endregion

    #region ReduceBuff

    public void ReduceTurnBuffDuration()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

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
            if (activeBuffs[i] == null)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

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
            if (activeBuffs[i] == null)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

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