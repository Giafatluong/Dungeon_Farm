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
        if (speedManager == null || combatManager == null)
        {
            Awake();
        }

        currentRoundNumber++;
        isAmbushRound = isAmbush;
        speedManager.CreateSpeedOrder(isAmbushRound);
        currentIndex = 0;

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
        if (speedManager == null || speedManager.speedOrder == null) return;

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

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public SpeedEntry GetCurrentEntry()
    {
        if (currentIndex >= speedManager.speedOrder.Count)
        {
            return null;
        }

        return speedManager.speedOrder[currentIndex];
    }
}