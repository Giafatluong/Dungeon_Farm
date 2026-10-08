using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private CombatManager combatManager;

    private int currentIndex;
    private bool isAmbushRound;
    public int currentRoundNumber = 0;

    public void ResetCombatRounds()
    {
        currentRoundNumber = 0;
    }

    private void Awake()
    {
        if (speedManager == null) speedManager = GetComponent<SpeedManager>();
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
    }

    public void StartRound(bool isAmbush = false)
    {
        if (combatManager != null && (!combatManager.isCombatActive && !combatManager.isPreparingCombat))
        {
            return;
        }

        if (combatManager != null && combatManager.playerStats != null && combatManager.playerStats.currentHealth <= 0)
        {
            return;
        }

        currentRoundNumber++;
        isAmbushRound = isAmbush;
        speedManager.CreateSpeedOrder(isAmbushRound);
        currentIndex = 0;

        if (speedManager.speedOrder == null || speedManager.speedOrder.Count == 0)
        {
            return;
        }

        // Reset round state for all active enemies
        if (combatManager != null && combatManager.enemies != null)
        {
            foreach (var enemy in combatManager.enemies)
            {
                if (enemy != null && enemy.currentHealth > 0)
                {
                    enemy.ResetDefend();
                    enemy.hasActedThisRound = false;
                    enemy.SelectNextIntent();
                }
            }
        }

        StartCurrentTurn();
    }

    public void StartCurrentTurn()
    {
        if (speedManager == null || speedManager.speedOrder == null || speedManager.speedOrder.Count == 0) return;
        if (combatManager != null && !combatManager.isCombatActive && !combatManager.isPreparingCombat) return;
        if (combatManager != null && combatManager.playerStats != null && combatManager.playerStats.currentHealth <= 0) return;

        if (currentIndex >= speedManager.speedOrder.Count)
        {
            isAmbushRound = false;
            StartRound(false);
            return;
        }

        SpeedEntry currentEntry = speedManager.speedOrder[currentIndex];

        if (currentEntry == null || currentEntry.character == null)
        {
            NextTurn();
            return;
        }

        if (combatManager != null && combatManager.playerStats != null && currentEntry.character == combatManager.playerStats.gameObject)
        {
            if (combatManager.playerStats.currentHealth <= 0)
            {
                return;
            }

            combatManager.StartPlayerTurn();
        }
        else
        {
            EnemyStats enemy = currentEntry.character.GetComponent<EnemyStats>();

            if (enemy == null || enemy.currentHealth <= 0)
            {
                NextTurn();
                return;
            }

            combatManager.StartEnemyTurn(enemy);
        }
    }

    public void NextTurn()
    {
        if (combatManager != null && (!combatManager.isCombatActive && !combatManager.isPreparingCombat))
        {
            return;
        }

        if (combatManager != null && combatManager.playerStats != null && combatManager.playerStats.currentHealth <= 0)
        {
            return;
        }

        if (speedManager == null || speedManager.speedOrder == null || speedManager.speedOrder.Count == 0)
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= speedManager.speedOrder.Count)
        {
            isAmbushRound = false;
            StartRound(false);
            return;
        }

        StartCurrentTurn();
    }

    public void EndPlayerTurn()
    {
        NextTurn();
    }
}