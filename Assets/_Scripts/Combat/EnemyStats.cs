using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class EnemyStats : MonoBehaviour
{
    public EnemyData enemyData;
    public EnemyTargetSelector targetSelector;

    public int currentHealth;
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    public bool isDefending;

    public EnemyData.EnemyIntent currentIntent;
    public HealthBar healthBar;

    private void Start()
    {
        currentHealth = enemyData.maxHealth;
        healthBar.SetHealthBar(enemyData.maxHealth, enemyData.maxHealth);

        if (enemyData.possibleIntents.Length > 0)
        {
            currentIntent = enemyData.possibleIntents[
                Random.Range(0, enemyData.possibleIntents.Length)
            ];
        }
    }
#region GetSomeStats
    public int GetCurrentATK()
    {
        int value = enemyData.ATK;

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buff.buffType == StatEffectData.BuffType.Attack)
            {
                value += activeBuffs[i].buffValue;
            }
        }

        return value;
    }

    public int GetCurrentDEF()
    {
        int value = enemyData.DEF;

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buff.buffType == StatEffectData.BuffType.Defense)
            {
                value += activeBuffs[i].buffValue;
            }
        }

        if (isDefending)
        {
            value += enemyData.DEF;
        }

        return value;
    }

    public int GetCurrentSpeed()
    {
        int value = enemyData.speed;

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buff.buffType == StatEffectData.BuffType.Speed)
            {
                value += activeBuffs[i].buffValue;
            }
        }

        return value;
    }
#endregion




#region Buff
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
            buffDurationType = durationType,
            remainingDuration = duration
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
#endregion
    



#region Actions
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealthBar(currentHealth, enemyData.maxHealth);

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Dead();
    }

    public bool Dead()
    {
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
            return true;
        }
        activeBuffs.RemoveRange(0, activeBuffs.Count);

        return false;
    }

    public void Defend()
    {
        isDefending = true;
    }

    public void ResetDefend()
    {
        isDefending = false;
    }

    public void SelectIntent()
    {
        if (enemyData.possibleIntents.Length == 0) return;

        currentIntent = enemyData.possibleIntents[
            Random.Range(0, enemyData.possibleIntents.Length)
        ];
    }

    public void ApplyIntentBuff()
    {
        for (int i = 0; i < enemyData.intentEffects.Length; i++)
        {
            EnemyData.IntentEffect effect = enemyData.intentEffects[i];

            if (effect.target != EnemyData.EffectTarget.Self)
            {
                continue;
            }

            AddBuff(
                effect.effect,
                effect.value,
                effect.duration,
                effect.durationType
            );
        }
    }
#endregion

    private void OnMouseDown()
    {
        Debug.Log("Clicked: " + enemyData.enemyName);

        if (targetSelector != null)
        {
            targetSelector.SelectEnemy(this);
        }
        else
        {
            Debug.Log("Target Selector is NULL");
        }
    }
}