using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 50;
    public int currentHealth = 50;

    [Header("Combat Stats")]
    public int ATK = 10;
    public int DEF = 2;
    public int speed = 16;
    public int defendCount;
    public int defendValue = 5;

    [Header("Action Point")]
    public int maxAP = 3;
    public int currentAP = 3;

    [Header("Hunger")]
    public int maxHunger = 100;
    public int currentHunger = 100;

    [Header("Buffs")]
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    public ItemContainer itemContainer;
    public HealthBar healthBar;

    public event System.Action OnPlayerDeath;
    public event System.Action<int> OnPlayerDamaged;
    public event System.Action<int> OnPlayerHealed;
    public event System.Action OnPlayerDefended;

    private int baseMaxHealth;
    private int baseATK;
    private int baseDEF;
    private int baseSpeed;

    public static PlayerStats Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        baseMaxHealth = maxHealth;
        baseATK = ATK;
        baseDEF = DEF;
        baseSpeed = speed;

        ResolveItemContainer();
    }

    public void ResolveItemContainer()
    {
        if (itemContainer != null) return;

        ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
        for (int i = 0; i < containers.Length; i++)
        {
            if (containers[i] != null && !containers[i].name.ToLower().Contains("chest"))
            {
                itemContainer = containers[i];
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnProgressionChanged += RefreshStatsFromProgression;
        }
    }

    private void OnDisable()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnProgressionChanged -= RefreshStatsFromProgression;
        }
    }

    public void RefreshStatsFromProgression()
    {
        if (baseMaxHealth <= 0 && maxHealth > 0) baseMaxHealth = maxHealth;
        if (baseATK <= 0 && ATK > 0) baseATK = ATK;
        if (baseDEF <= 0 && DEF > 0) baseDEF = DEF;
        if (baseSpeed <= 0 && speed > 0) baseSpeed = speed;

        int permHP = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentMaxHP : 0;
        int permATK = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentATK : 0;
        int permDEF = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentDEF : 0;
        int permSpeed = ProgressionManager.Instance != null ? ProgressionManager.Instance.permanentSpeed : 0;

        maxHealth = baseMaxHealth + permHP;
        ATK = baseATK + permATK;
        DEF = baseDEF + permDEF;
        speed = baseSpeed + permSpeed;
    }

    private void Start()
    {
        RefreshStatsFromProgression();
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentAP = maxAP;

        // When at Base or outside Dungeon, clear all temporary buffs
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid() && (activeScene.name == "Base" || activeScene.name.Contains("Base")))
        {
            ClearTemporaryBuffs();
        }

        if (healthBar != null && !healthBar.gameObject.scene.IsValid())
        {
            healthBar = null;
        }

        if (healthBar != null)
        {
            healthBar.SetHealthBar(currentHealth, maxHealth);
        }
    }

    #region PlayerAction

    public bool Eat(ItemData item)
    {
        if (item == null) return false;
        ResolveItemContainer();

        bool hasRawVitality = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.RawVitality);
        bool hasEndlessFeast = ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.EndlessFeast);

        FoodData food = item as FoodData;

        // If raw produce or unprocessed food
        if (food == null)
        {
            if (item.itemType == ItemData.ItemType.Produce || item.itemType == ItemData.ItemType.Food || item.itemType == ItemData.ItemType.Seed)
            {
                if (itemContainer != null && !itemContainer.HasItem(item))
                {
                    Debug.Log("Crop not found in container");
                    return false;
                }

                if (currentHunger >= maxHunger && currentHealth >= maxHealth)
                {
                    Debug.Log("Both hunger and health are full, cannot eat more");
                    return false;
                }

                int hungerGain = hasRawVitality ? 20 : 15;
                int healGain = hasRawVitality ? 12 : 6;

                AddHunger(hungerGain);
                Heal(healGain);
                if (itemContainer != null)
                {
                    itemContainer.RemoveItem(item, 1);
                }
                string bonusMsg = hasRawVitality ? "Raw Food! +12 HP, +20 Hunger" : $"+{healGain} HP, +{hungerGain} Hunger";
                DamagePopupManager.ShowText(transform.position + Vector3.up * 1.1f, bonusMsg, new Color(0.4f, 0.9f, 0.3f), 4.5f);
                Debug.Log($"Ate produce/item {item.itemName} (+{healGain} HP, +{hungerGain} Hunger)!");
                return true;
            }

            Debug.Log("Item is not food");
            return false;
        }

        if (itemContainer != null && !itemContainer.HasItem(food))
        {
            Debug.Log("Food not found in container");
            return false;
        }

        bool hasBuffs = (food.foodBuff != null && food.foodBuff.Length > 0);
        if (currentHunger >= maxHunger && currentHealth >= maxHealth && !hasBuffs)
        {
            Debug.Log("Both hunger and health are full, and food has no buffs");
            return false;
        }

        if (food.foodBuff != null && food.foodBuff.Length > 0)
        {
            for (int i = 0; i < food.foodBuff.Length; i++)
            {
                if (food.foodBuff[i] != null && food.foodBuff[i].buffs != null)
                {
                    int duration = food.foodBuff[i].buffDuration;
                    if (hasEndlessFeast)
                    {
                        duration *= 2;
                    }

                    AddBuff(
                        food.foodBuff[i].buffs,
                        food.foodBuff[i].buffValue,
                        duration,
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
        return true;
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
            if (ProgressionManager.Instance.HasBlessing(BlessingType.BastionStrike))
            {
                value += Mathf.RoundToInt(GetCurrentDEF() * 0.5f);
            }

            // Blessing: Starving Fury (Hunger < 35% grants +6 ATK)
            if (ProgressionManager.Instance.HasBlessing(BlessingType.StarvingFury) && maxHunger > 0 && currentHunger <= maxHunger * 0.35f)
            {
                value += 6;
            }
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
            if (ProgressionManager.Instance.HasBlessing(BlessingType.GluttonousAegis) && maxHunger > 0 && currentHunger >= maxHunger * 0.70f)
            {
                value += 8;
            }
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
            // Blessing: Swift Momentum (+4 Speed)
            if (ProgressionManager.Instance.HasBlessing(BlessingType.SwiftMomentum))
            {
                value += 4;
            }
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
        int safeDuration = Mathf.Max(1, duration);

        ActiveBuff newBuff = new()
        {
            buff = buff,
            buffValue = finalValue,
            remainingDuration = safeDuration,
            buffDurationType = durationType
        };

        bool merged = false;
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
            merged = true;
            break;
        }

        if (!merged)
        {
            activeBuffs.Add(newBuff);
        }

        // Display visual floating notice above player head
        string sign = finalValue > 0 ? "+" : "";
        string durUnit = durationType switch
        {
            FoodData.BuffDurationType.Turn => "turns",
            FoodData.BuffDurationType.Combat => "battles",
            FoodData.BuffDurationType.Floor => "floors",
            _ => durationType.ToString()
        };
        Color popupColor = finalValue >= 0 ? new Color(0.3f, 0.95f, 1f) : new Color(1f, 0.4f, 0.4f);
        DamagePopupManager.ShowText(transform.position + Vector3.up * 1.3f, $"{sign}{finalValue} {CombatUI.GetBuffStatLabel(buff)} ({safeDuration} {durUnit})", popupColor, 4.5f);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            DamagePopupManager.ShowText(transform.position + Vector3.up * 1.1f, "BLOCKED!", new Color(0.4f, 0.85f, 1f), 4.2f);
            return;
        }

        currentHealth -= damage;

        // Damage Popup above player
        DamagePopupManager.ShowDamage(transform.position + Vector3.up * 1.1f, damage, isPlayer: true);

        if (CombatJuice.Instance != null && damage > 0)
        {
            CombatJuice.Instance.OnPlayerDamaged(transform.position, damage);
        }

        // Blessing: Gluttonous Aegis - Reflect 40% damage when well-fed (>70% Hunger)
        if (damage > 0 && ProgressionManager.Instance != null && ProgressionManager.Instance.HasBlessing(BlessingType.GluttonousAegis)
            && maxHunger > 0 && currentHunger >= maxHunger * 0.70f)
        {
            int reflected = Mathf.Max(1, Mathf.RoundToInt(damage * 0.4f));
            if (CombatManager.Instance != null && CombatManager.Instance.isCombatActive)
            {
                CombatManager.Instance.ReflectDamageToCurrentEnemy(reflected);
            }
        }

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
        ReduceTurnBuffDuration(); // Decrement turn-based buffs when player advances through stages
    }

    public void OnCombatComplete()
    {
        defendCount = 0;
        ReduceHunger(10);
        ReduceCombatBuffDuration();
        ClearTurnBuffs();
    }

    public void ClearTurnBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null || activeBuffs[i].buffDurationType == FoodData.BuffDurationType.Turn)
            {
                activeBuffs.RemoveAt(i);
            }
        }
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
                ActiveBuff b = activeBuffs[i];
                if (b != null && b.buff != null)
                {
                    DamagePopupManager.ShowText(transform.position + Vector3.up * 1.3f, $"{b.buff.buffName} Expired", new Color(0.75f, 0.75f, 0.75f), 3.2f);
                }
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
                ActiveBuff b = activeBuffs[i];
                if (b != null && b.buff != null)
                {
                    DamagePopupManager.ShowText(transform.position + Vector3.up * 1.3f, $"{b.buff.buffName} Expired", new Color(0.75f, 0.75f, 0.75f), 3.2f);
                }
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
                ActiveBuff b = activeBuffs[i];
                if (b != null && b.buff != null)
                {
                    DamagePopupManager.ShowText(transform.position + Vector3.up * 1.3f, $"{b.buff.buffName} Expired", new Color(0.75f, 0.75f, 0.75f), 3.2f);
                }
                activeBuffs.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Clears all temporary buffs (food, potions) when expedition ends or advancing to a new day.
    /// </summary>
    public void ClearTemporaryBuffs()
    {
        activeBuffs.Clear();
    }
    #endregion
}