using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Enemy Configuration")]
    public EnemyData enemyData;

    [Header("Runtime State")]
    public int currentHealth;
    public EnemyData.EnemyIntent currentIntent;
    public bool hasActedThisRound = false;
    public bool isDefending = false;
    public int defendBonus = 5;
    public int defenseReduction = 0;
    public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    [Header("Placement Configuration")]
    public bool isFlying = false;
    public float yOffset = 0f;
    [Header("Combat Style")]
    public bool isRanged = false;

    public float GetSpawnYOffset()
    {
        if (yOffset != 0f) return yOffset;
        if (isFlying) return 1.1f;
        return 0f;
    }

    public EnemyTargetSelector targetSelector;
    public Collider2D CachedCollider { get; private set; }
    public EnemyCombatVisual CombatVisual { get; private set; }

    public event System.Action<EnemyStats> OnEnemyDeath;
    public event System.Action<EnemyStats, EnemyData.EnemyIntent> OnIntentChanged;

    protected virtual void Awake()
    {
        CachedCollider = GetComponent<Collider2D>();
        CombatVisual = GetComponent<EnemyCombatVisual>();
        defenseReduction = 0;
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            if (enemyData.isRanged) isRanged = true;
        }

        SelectNextIntent();
    }

    protected virtual void Start()
    {
        if (enemyData != null && currentHealth <= 0)
        {
            currentHealth = enemyData.maxHealth;
        }

        if (targetSelector == null)
        {
            targetSelector = FindFirstObjectByType<EnemyTargetSelector>();
        }
    }

    private void OnMouseDown()
    {
        if (currentHealth <= 0) return;

        if (targetSelector == null)
        {
            targetSelector = FindFirstObjectByType<EnemyTargetSelector>();
        }

        if (targetSelector != null)
        {
            targetSelector.SelectEnemy(this);
        }
    }

    #region Stats

    public int GetCurrentATK()
    {
        int value = enemyData != null ? enemyData.ATK : 0;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i] != null && activeBuffs[i].buff != null &&
                activeBuffs[i].buff.buffType == StatEffectData.BuffType.Attack)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        return Mathf.Max(0, value);
    }

    public int GetCurrentDEF()
    {
        int value = enemyData != null ? enemyData.DEF : 0;
        if (isDefending) value += defendBonus;
        value -= defenseReduction;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i] != null && activeBuffs[i].buff != null &&
                activeBuffs[i].buff.buffType == StatEffectData.BuffType.Defense)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        return Mathf.Max(0, value);
    }

    public void ReduceDEF(int amount)
    {
        if (amount <= 0) return;
        defenseReduction += amount;
        DamagePopupManager.ShowText(transform.position + Vector3.up * 1.5f, $"-{amount} DEF", new Color(0.8f, 0.4f, 0.9f), 4.2f);
    }

    public int GetCurrentSpeed()
    {
        int value = enemyData != null ? enemyData.speed : 0;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i] != null && activeBuffs[i].buff != null &&
                activeBuffs[i].buff.buffType == StatEffectData.BuffType.Speed)
            {
                value += activeBuffs[i].buffValue;
            }
        }
        return Mathf.Max(0, value);
    }

    #endregion

    #region Intent

    public void SelectNextIntent()
    {
        if (enemyData == null || enemyData.possibleIntents == null || enemyData.possibleIntents.Length == 0)
        {
            currentIntent = EnemyData.EnemyIntent.Attack;
            OnIntentChanged?.Invoke(this, currentIntent);
            return;
        }

        int index = Random.Range(0, enemyData.possibleIntents.Length);
        currentIntent = enemyData.possibleIntents[index];
        OnIntentChanged?.Invoke(this, currentIntent);
    }

    #endregion

    #region Combat Actions

    public virtual int TakeDamage(int damage)
    {
        return TakeDamage(damage, true, false);
    }

    public virtual int TakeDamage(int damage, bool showFeedback)
    {
        return TakeDamage(damage, showFeedback, false);
    }

    public virtual int TakeDamage(int damage, bool showFeedback, bool ignoreDefense)
    {
        if (currentHealth <= 0) return 0;

        int def = ignoreDefense ? 0 : GetCurrentDEF();
        int reducedDamage = Mathf.Max(1, damage - def);
        currentHealth -= reducedDamage;

        if (showFeedback)
        {
            TriggerDamageFeedback(reducedDamage, damage);
        }

        Debug.Log($"[EnemyStats] {(enemyData != null ? enemyData.enemyName : "Enemy")} took {reducedDamage} damage (raw: {damage}, DEF: {def}). HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Dead();
        }

        return reducedDamage;
    }

    /// <summary>
    /// Triggers damage feedback (damage popup, camera shake, recoil, and red flash).
    /// Called at the exact impact moment of Player's attack for authentic combat feel.
    /// </summary>
    public void TriggerDamageFeedback(int dealtDamage, int rawDamage = -1)
    {
        if (rawDamage < 0) rawDamage = dealtDamage;
        int def = GetCurrentDEF();
        bool isCrit = rawDamage >= 20 || (def > 0 && rawDamage >= def * 2 && rawDamage >= 12);
        int maxHp = enemyData != null ? Mathf.Max(1, enemyData.maxHealth) : 1;
        float damagePercent = (float)dealtDamage / maxHp;
        bool isLethal = currentHealth <= 0;

        Collider2D col = CachedCollider != null ? CachedCollider : (CachedCollider = GetComponent<Collider2D>());
        float popupY = col != null ? (col.bounds.max.y - transform.position.y + 0.35f) : 1.2f;
        Vector3 popupPos = transform.position + new Vector3(0, popupY, 0);

        DamagePopupManager.ShowDamage(popupPos, dealtDamage, isPlayer: false, isCrit: isCrit, damagePercent: damagePercent, isLethal: isLethal);

        if (CombatJuice.Instance != null)
        {
            CombatJuice.Instance.OnEnemyDamaged(transform.position, dealtDamage, damagePercent, isLethal);
        }

        EnemyCombatVisual visual = CombatVisual != null ? CombatVisual : (CombatVisual = GetComponent<EnemyCombatVisual>());
        if (visual != null) visual.PlayHitAnimation();
    }

    public void Heal(int amount)
    {
        if (enemyData == null || amount <= 0) return;
        currentHealth = Mathf.Min(currentHealth + amount, enemyData.maxHealth);
        DamagePopupManager.ShowHeal(transform.position + Vector3.up * 1.2f, amount);
    }

    public void AddBuff(StatEffectData buff, int value, int duration, FoodData.BuffDurationType durationType)
    {
        if (buff == null) return;

        int finalValue = (buff.IsDebuff && value > 0) ? -value : value;

        ActiveBuff newBuff = new ActiveBuff
        {
            buff = buff,
            buffValue = finalValue,
            remainingDuration = duration,
            buffDurationType = durationType
        };

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null || activeBuffs[i].buff != buff) continue;
            if (activeBuffs[i].buffDurationType != durationType) continue;

            if (buff.stackable == StatEffectData.Stackable.Yes)
            {
                activeBuffs[i].buffValue += newBuff.buffValue;
                activeBuffs[i].remainingDuration = Mathf.Max(activeBuffs[i].remainingDuration, duration);
            }
            else
            {
                if (newBuff.buffValue >= 0)
                    activeBuffs[i].buffValue = Mathf.Max(activeBuffs[i].buffValue, newBuff.buffValue);
                else
                    activeBuffs[i].buffValue = Mathf.Min(activeBuffs[i].buffValue, newBuff.buffValue);

                activeBuffs[i].remainingDuration = Mathf.Max(activeBuffs[i].remainingDuration, duration);
            }
            return;
        }

        activeBuffs.Add(newBuff);
    }

    public void ReduceTurnBuffDuration()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i] == null)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

            if (activeBuffs[i].buffDurationType != FoodData.BuffDurationType.Turn) continue;
            activeBuffs[i].remainingDuration--;
            if (activeBuffs[i].remainingDuration <= 0)
                activeBuffs.RemoveAt(i);
        }
    }

    public void ResetDefend()
    {
        isDefending = false;
    }

    public void PerformIntentAction(PlayerStats playerStats)
    {
        if (playerStats == null || currentHealth <= 0) return;

        EnemyCombatVisual visual = CombatVisual != null ? CombatVisual : (CombatVisual = GetComponent<EnemyCombatVisual>());

        switch (currentIntent)
        {
            case EnemyData.EnemyIntent.Attack:
                int dmg = Mathf.Max(0, GetCurrentATK() - playerStats.GetCurrentDEF() - (playerStats.defendCount * playerStats.defendValue));
                Debug.Log($"[EnemyStats] {enemyData?.enemyName} attacks for {dmg} damage!");

                if (visual != null)
                {
                    visual.PlayAttackAnimation(playerStats, () => playerStats.TakeDamage(dmg));
                }
                else
                {
                    playerStats.TakeDamage(dmg);
                }
                break;

            case EnemyData.EnemyIntent.Defend:
                int defVal = enemyData != null ? Mathf.Max(5, enemyData.DEF) : 5;
                defendBonus = defVal;
                isDefending = true;
                DamagePopupManager.ShowText(transform.position + Vector3.up * 1.2f, $"+{defVal} DEF", new Color(0.4f, 0.75f, 1f), 4.6f);
                Debug.Log($"[EnemyStats] {enemyData?.enemyName} defends (+{defVal} DEF this turn).");

                if (visual != null) StartCoroutine(visual.PlayDefendRoutine(null));
                break;

            case EnemyData.EnemyIntent.Buff:
                ApplyIntentEffect(this, playerStats, EnemyData.EnemyIntent.Buff);
                if (visual != null) StartCoroutine(visual.PlayBuffRoutine(null));
                break;

            case EnemyData.EnemyIntent.Debuff:
                ApplyIntentEffect(this, playerStats, EnemyData.EnemyIntent.Debuff);
                if (visual != null) StartCoroutine(visual.PlayDebuffRoutine(playerStats.transform, null));
                break;
        }

        hasActedThisRound = true;
        SelectNextIntent();
    }

    private void ApplyIntentEffect(EnemyStats self, PlayerStats playerStats, EnemyData.EnemyIntent intent)
    {
        if (enemyData == null || enemyData.intentEffects == null) return;

        foreach (var effect in enemyData.intentEffects)
        {
            if (effect == null || effect.effect == null) continue;

            // When executing Buff intent, only execute buffs targeting Self or Ally
            if (intent == EnemyData.EnemyIntent.Buff && effect.target == EnemyData.EffectTarget.Player)
                continue;

            // When executing Debuff intent, only execute debuffs targeting Player
            if (intent == EnemyData.EnemyIntent.Debuff && effect.target != EnemyData.EffectTarget.Player)
                continue;

            string bName = !string.IsNullOrEmpty(effect.effect.buffName) ? effect.effect.buffName : effect.effect.buffType.ToString();
            int finalValue = (effect.effect.IsDebuff && effect.value > 0) ? -effect.value : effect.value;
            string sign = finalValue > 0 ? "+" : "";

            switch (effect.target)
            {
                case EnemyData.EffectTarget.Self:
                    self.AddBuff(effect.effect, finalValue, effect.duration, effect.durationType);
                    Color selfColor = finalValue >= 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.4f, 0.9f);
                    DamagePopupManager.ShowText(self.transform.position + Vector3.up * 1.2f, $"{sign}{finalValue} {bName}", selfColor, 4.5f);
                    break;

                case EnemyData.EffectTarget.Ally:
                    CombatManager cm = CombatManager.Instance != null ? CombatManager.Instance : FindFirstObjectByType<CombatManager>();
                    if (cm != null && cm.enemies != null)
                    {
                        foreach (var ally in cm.enemies)
                        {
                            if (ally != null && ally.currentHealth > 0)
                            {
                                ally.AddBuff(effect.effect, finalValue, effect.duration, effect.durationType);
                                Color allyColor = finalValue >= 0 ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.4f, 0.9f);
                                DamagePopupManager.ShowText(ally.transform.position + Vector3.up * 1.2f, $"{sign}{finalValue} {bName}", allyColor, 4.5f);
                            }
                        }
                    }
                    break;

                case EnemyData.EffectTarget.Player:
                    if (playerStats != null)
                    {
                        playerStats.AddBuff(effect.effect, finalValue, effect.duration, effect.durationType);
                    }
                    break;
            }
        }
    }

    public List<ItemSlot> GetLootDrops()
    {
        if (enemyData == null) return new List<ItemSlot>();
        return enemyData.RollDrops();
    }

    private void Dead()
    {
        Debug.Log($"[EnemyStats] {(enemyData != null ? enemyData.enemyName : "Enemy")} died!");

        OnEnemyDeath?.Invoke(this);

        if (targetSelector != null && targetSelector.selectedEnemy == this)
        {
            targetSelector.ClearTarget();
        }

        EnemyCombatVisual visual = CombatVisual != null ? CombatVisual : (CombatVisual = GetComponent<EnemyCombatVisual>());
        if (visual != null)
        {
            visual.PlayDeathAnimation();
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    #endregion
}
