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

        if (itemContainer != null && !itemContainer.HasItem(food))
        {
            Debug.Log("khong co do an");
            return;
        }

        if (currentHunger >= maxHunger)
        {
            Debug.Log("No qua k an dc");
            return;
        }

        bool appliedAnyBuff = false;
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
                    appliedAnyBuff = true;
                }
            }
        }

        if (!appliedAnyBuff)
        {
            ApplyDefaultFoodBuff(food);
        }

        AddHunger(food.hungerValue);
        Heal(food.healthValue);
        if (itemContainer != null)
        {
            itemContainer.RemoveItem(food, 1);
        }

        Debug.Log("da an : " + food.itemName);
    }

    private void ApplyDefaultFoodBuff(FoodData food)
    {
        if (food == null) return;

        StatEffectData buffToApply = null;
        int value = 2;
        int duration = 3;

        StatEffectData[] loadedBuffs = Resources.FindObjectsOfTypeAll<StatEffectData>();
        string fname = (food.itemName ?? food.name ?? "").ToLower();

        StatEffectData atkBuff = null, defBuff = null, spdBuff = null;
        if (loadedBuffs != null)
        {
            for (int i = 0; i < loadedBuffs.Length; i++)
            {
                var b = loadedBuffs[i];
                if (b == null) continue;
                if (b.buffType == StatEffectData.BuffType.Attack) atkBuff = b;
                else if (b.buffType == StatEffectData.BuffType.Defense) defBuff = b;
                else if (b.buffType == StatEffectData.BuffType.Speed) spdBuff = b;
            }
        }

        if (fname.Contains("chilli") || fname.Contains("cay") || fname.Contains("spicy") || fname.Contains("meat") || fname.Contains("thit"))
        {
            buffToApply = atkBuff;
            value = 3;
        }
        else if (fname.Contains("corn") || fname.Contains("bap") || fname.Contains("speed") || fname.Contains("carrot") || fname.Contains("rot"))
        {
            buffToApply = spdBuff;
            value = 2;
        }
        else
        {
            buffToApply = defBuff;
            value = 2;
        }

        if (buffToApply != null)
        {
            AddBuff(buffToApply, value, duration, FoodData.BuffDurationType.Turn);
        }
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

        ActiveBuff newBuff = new()
        {
            buff = buff,
            buffValue = value,
            remainingDuration = duration,
            buffDurationType = durationType
        };

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null || activeBuffs[i].buff == null) continue;
            if (activeBuffs[i].buff.buffType != newBuff.buff.buffType) continue;
            if (activeBuffs[i].buffDurationType != newBuff.buffDurationType) continue;

            activeBuffs[i].remainingDuration += newBuff.remainingDuration;
            activeBuffs[i].buffValue = Mathf.Max(activeBuffs[i].buffValue, newBuff.buffValue);
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