using UnityEngine;
using System.Collections.Generic;

public class Boss : EnemyStats
{
    [Header("Boss Configuration")]
    public string bossName = "Dungeon Boss";
    public int phaseCount = 2;

    [Header("Phase Thresholds (% of max HP)")]
    public float[] phaseThresholds = { 0.5f }; // e.g. 50% HP triggers phase 2

    [Header("Phase Buff Effects")]
    public StatEffectData phaseATKBuff;
    public int phaseATKBuffValue = 5;
    public StatEffectData phaseSPDBuff;
    public int phaseSPDBuffValue = 2;

    private int currentPhase = 1;
    private bool[] phaseTriggered;

    protected override void Awake()
    {
        base.Awake();
        phaseTriggered = new bool[phaseThresholds != null ? phaseThresholds.Length : 0];
        currentPhase = 1;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        if (enemyData == null || enemyData.maxHealth <= 0) return;
        if (phaseThresholds == null) return;

        float hpRatio = (float)currentHealth / enemyData.maxHealth;

        for (int i = 0; i < phaseThresholds.Length; i++)
        {
            if (!phaseTriggered[i] && hpRatio <= phaseThresholds[i])
            {
                phaseTriggered[i] = true;
                TriggerPhase(i + 2);
            }
        }
    }

    private void TriggerPhase(int phase)
    {
        currentPhase = phase;
        Debug.Log($"[Boss] {bossName} entered Phase {phase}!");

        if (phaseATKBuff != null)
        {
            AddBuff(phaseATKBuff, phaseATKBuffValue, 999, FoodData.BuffDurationType.Combat);
        }

        if (phaseSPDBuff != null)
        {
            AddBuff(phaseSPDBuff, phaseSPDBuffValue, 999, FoodData.BuffDurationType.Combat);
        }

        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"{bossName} enters Phase {phase}! It's enraged!");
        }
    }

    public int GetCurrentPhase() => currentPhase;
}
