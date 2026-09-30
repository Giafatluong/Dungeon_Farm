using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private CombatManager combatManager;

    private int currentIndex;
    private bool isAmbushRound;

    public void StartRound(bool isAmbush = false)
    {
        isAmbushRound = isAmbush;
        speedManager.CreateSpeedOrder(isAmbushRound);
        currentIndex = 0;

        StartCurrentTurn();
    }

    public void StartCurrentTurn()
    {
        if (currentIndex >= speedManager.speedOrder.Count)
        {
            isAmbushRound = false;
            StartRound(false);
            return;
        }

        SpeedEntry currentEntry = speedManager.speedOrder[currentIndex];

        if (currentEntry.character == combatManager.playerStats.gameObject)
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